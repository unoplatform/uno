using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Tracking;

/// <summary>
/// Tracks the global state and current operation of hot reload functionality for an application, and coordinates
/// reporting and state updates.
/// </summary>
/// <remarks>HotReloadTracker manages the lifecycle of hot reload operations, including starting, aborting, and
/// reporting status changes. It is thread-safe and intended for use in environments where hot reload support and
/// diagnostics are required. The tracker delegates reporting to the specified reporter and communicates state changes
/// via the provided sendState callback.</remarks>
/// <param name="sendState">A delegate that asynchronously receives hot reload status updates. Called whenever the global state or operation
/// list changes.</param>
/// <param name="tryRequestHotReload">An optional delegate that attempts to initiate a hot reload operation. Returns <see langword="true"/> if a hot
/// reload was successfully requested; otherwise, <see langword="false"/>.</param>
/// <param name="reporter">An optional reporter used to output diagnostic, warning, and error messages related to hot reload operations. If not
/// specified, a default console reporter is used.</param>
public sealed class HotReloadTracker(
	Func<HotReloadStatusInfo, CancellationToken, ValueTask> sendState,
	Func<CancellationToken, ValueTask<bool>>? tryRequestHotReload = null,
	IReporter? reporter = null) : IHotReloadTracker
{
	private readonly IReporter _reporter = reporter ?? new ConsoleReporter();
	private readonly Func<CancellationToken, ValueTask<bool>> _tryRequestHotReload = tryRequestHotReload ?? (_ => ValueTask.FromResult(false));

	private HotReloadState _globalState; // This actually contains only the initializing stat (i.e. Disabled, Initializing, Idle). Processing state is _current != null.
	private HotReloadOperation? _current;// I.e. head of the operation chain list

	/// <summary>
	/// Gets the current global hot reload state for the application.
	/// </summary>
	public HotReloadState State => _globalState;

	/// <summary>
	/// Gets the current hot-reload operation, if any.
	/// </summary>
	public HotReloadOperation? Current => _current;

	public async ValueTask SetStateAsync(HotReloadState state)
	{
		_globalState = state;

		if (state is HotReloadState.Disabled)
		{
			await AbortHotReload();
		}
		else
		{
			await SendUpdate();
		}
	}

	public async ValueTask EnsureHotReloadStarted()
	{
		if (Current is null)
		{
			await StartHotReload(null);
		}
	}

	internal ValueTask<bool> TryRequestHotReload(CancellationToken ct)
		=> _tryRequestHotReload(ct);

	public async ValueTask<HotReloadOperation> StartHotReload(ImmutableHashSet<string>? filesPaths)
	{
		var previous = Current;
		HotReloadOperation? @new;
		while (true)
		{
			@new = new HotReloadOperation(this, previous, filesPaths);
			var current = Interlocked.CompareExchange(ref _current, @new, previous);
			if (current == previous)
			{
				break;
			}
			else
			{
				previous = current;
			}
		}

		// Notify the start of new hot-reload operation
		await SendUpdate();

		return @new;
	}

	public async ValueTask<HotReloadOperation> StartOrContinueHotReload(ImmutableHashSet<string>? filesPaths = null)
		=> Current is { } current && (filesPaths is null || current.TryMerge(filesPaths))
			? current
			: await StartHotReload(filesPaths);

	public ValueTask AbortHotReload()
		=> Current?.Complete(HotReloadOperationResult.Aborted) ?? SendUpdate();

	internal void ResignCurrent(HotReloadOperation operation)
		=> Interlocked.CompareExchange(ref _current, null, operation);

	public async ValueTask SendUpdate(HotReloadOperation? completing = null, string? serverError = null)
	{
		var state = _globalState;
		var operations = ImmutableList<HotReloadOperationInfo>.Empty;

		if (state is not HotReloadState.Disabled && (Current ?? completing) is { } current)
		{
			var infos = ImmutableList.CreateBuilder<HotReloadOperationInfo>();
			var foundCompleting = completing is null;
			LoadInfos(current);
			if (!foundCompleting)
			{
				LoadInfos(completing);
			}

			operations = infos.ToImmutable();

			void LoadInfos(HotReloadOperation? operation)
			{
				while (operation is not null)
				{
					if (operation.Result is null)
					{
						state = HotReloadState.Processing;
					}

					var diagnosticsResult = operation
						.Diagnostics
						?.Where(d => d.Severity >= DiagnosticSeverity.Warning)
						.Select(d => CSharpDiagnosticFormatter.Instance.Format(d, CultureInfo.InvariantCulture))
						.ToImmutableList() ?? ImmutableList<string>.Empty;
					var files = operation.ConsideredFilePaths.Except(operation.IgnoredFilePaths);

					foundCompleting |= operation == completing;
					infos.Add(new(operation.Id, operation.StartTime, files, operation.IgnoredFilePaths, operation.CompletionTime, operation.Result, diagnosticsResult));
					operation = operation.Previous!;
				}
			}
		}

		await sendState(new(state, operations, serverError), default);
	}

	/// <inheritdoc />
	void IReporter.Verbose(string message)
		=> _reporter.Verbose(message);

	/// <inheritdoc />
	void IReporter.Output(string message)
		=> _reporter.Output(message);

	/// <inheritdoc />
	void IReporter.Warn(string message)
		=> _reporter.Warn(message);

	/// <inheritdoc />
	void IReporter.Error(string message)
		=> _reporter.Error(message);
}
