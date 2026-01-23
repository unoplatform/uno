using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload;
using Uno.HotReload.Utils;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host.HotReload;

partial class HotReloadTracker(Func<HotReloadStatusMessage, CancellationToken, ValueTask> SendState, IReporter reporter, Func<CancellationToken, ValueTask<bool>> tryRequestHotReload) : IHotReloadTracker
{
	#region Hot-reload state
	public HotReloadState _globalState; // This actually contains only the initializing stat (i.e. Disabled, Initializing, Idle). Processing state is _current != null.
	public HotReloadServerOperation? _current; // I.e. head of the operation chain list

	public async ValueTask EnsureHotReloadStarted()
	{
		if (_current is null)
		{
			await StartHotReload(null);
		}
	}

	internal ValueTask<bool> TryRequestHotReload(CancellationToken ct)
		=> tryRequestHotReload(ct);

	public async ValueTask<HotReloadServerOperation> StartHotReload(ImmutableHashSet<string>? filesPaths)
	{
		var previous = _current;
		HotReloadServerOperation? @new;
		while (true)
		{
			@new = new HotReloadServerOperation(this, previous, filesPaths);
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

	public async ValueTask<HotReloadServerOperation> StartOrContinueHotReload(ImmutableHashSet<string>? filesPaths = null)
		=> _current is { } current && (filesPaths is null || current.TryMerge(filesPaths))
			? current
			: await StartHotReload(filesPaths);

	public ValueTask AbortHotReload()
		=> _current?.Complete(HotReloadServerResult.Aborted) ?? SendUpdate();

	public async ValueTask SendUpdate(HotReloadServerOperation? completing = null, string? serverError = null)
	{
		var state = _globalState;
		var operations = ImmutableList<HotReloadServerOperationData>.Empty;

		if (state is not HotReloadState.Disabled && (_current ?? completing) is { } current)
		{
			var infos = ImmutableList.CreateBuilder<HotReloadServerOperationData>();
			var foundCompleting = completing is null;
			LoadInfos(current);
			if (!foundCompleting)
			{
				LoadInfos(completing);
			}

			operations = infos.ToImmutable();

			void LoadInfos(HotReloadServerOperation? operation)
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

		await SendState(new HotReloadStatusMessage(state, operations, serverError), default);
	}

	/// <summary>
	/// A hot-reload operation that is in progress.
	/// </summary>
	public class HotReloadServerOperation
	{
		private static readonly int DefaultAutoRetryIfNoChangesAttempts = 3;

		public static readonly TimeSpan DefaultAutoRetryIfNoChangesDelay = TimeSpan.FromMilliseconds(500);

		// Delay to wait without any update to consider operation was aborted.
		private static readonly TimeSpan _timeoutDelay = TimeSpan.FromSeconds(30);

		private static readonly ImmutableHashSet<string> _empty = ImmutableHashSet<string>.Empty.WithComparer(PathComparer.Comparer);
		private static long _count;

		private readonly HotReloadTracker _owner;
		private readonly HotReloadServerOperation? _previous;
		private readonly Timer _timeout;

		private ImmutableHashSet<string> _consideredFilePaths; // All files that have been considered for this operation.
		private ImmutableHashSet<string> _ignoredFilePaths = _empty; // The files that have been ignored by the compilator for this operation (basically because they are not part of the solution).

		private int /* HotReloadResult */ _result = -1;

		private CancellationTokenSource? _deferredCompletion;
		private ImmutableArray<Diagnostic>? _diagnostics;

		// In VS we forcefully request to VS to hot-reload application, but in some cases the changes are not detected by VS and it returns a NoChanges result.
		// In such cases we can retry the hot-reload request to VS to let it process the file updates.
		private int _noChangesRetry;
		private TimeSpan _noChangesRetryDelay = DefaultAutoRetryIfNoChangesDelay;

		public long Id { get; } = Interlocked.Increment(ref _count);

		public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;

		public DateTimeOffset? CompletionTime { get; private set; }

		public HotReloadServerOperation? Previous => _previous;

		/// <summary>
		/// List of all file paths that have been considered for this hot-reload operation.
		/// </summary>
		/// <remarks>This **includes** the <see cref="IgnoredFilePaths"/>.</remarks>
		public ImmutableHashSet<string> ConsideredFilePaths => _consideredFilePaths;

		/// <summary>
		/// Gets the collection of file paths that are excluded from processing.
		/// </summary>
		/// <remarks>
		/// Files are typically ignored when they do not yet exist in the current solution.
		/// </remarks>
		public ImmutableHashSet<string> IgnoredFilePaths => _ignoredFilePaths;

		public ImmutableArray<Diagnostic>? Diagnostics => _diagnostics;

		public HotReloadServerResult? Result => _result is -1 ? null : (HotReloadServerResult)_result;

		/// <param name="previous">The previous hot-reload operation which has to be considered as aborted when this new one completes.</param>
		public HotReloadServerOperation(HotReloadTracker owner, HotReloadServerOperation? previous, ImmutableHashSet<string>? filePaths = null)
		{
			_owner = owner;
			_previous = previous;
			_consideredFilePaths = filePaths ?? _empty;

			_timeout = new Timer(
				static that => _ = ((HotReloadServerOperation)that!).Complete(HotReloadServerResult.Aborted),
				this,
				_timeoutDelay,
				Timeout.InfiniteTimeSpan);
		}

		/// <summary>
		/// Attempts to update the <see cref="ConsideredFilePaths"/> if we determine that the provided paths are corresponding to this operation.
		/// </summary>
		/// <returns>
		/// True if this operation should be considered as valid for the given file paths (and has been merged with original paths),
		/// false if the given paths does not belong to this operation.
		/// </returns>
		public bool TryMerge(ImmutableHashSet<string> filePaths)
		{
			if (_result is not -1)
			{
				return false;
			}

			var original = _consideredFilePaths;
			while (true)
			{
				ImmutableHashSet<string> updated;
				if (original.IsEmpty)
				{
					updated = filePaths;
				}
				else if (original.Any(filePaths.Contains))
				{
					updated = original.Union(filePaths);
				}
				else
				{
					return false;
				}

				var current = Interlocked.CompareExchange(ref _consideredFilePaths, updated, original);
				if (current == original)
				{
					_timeout.Change(_timeoutDelay, Timeout.InfiniteTimeSpan);
					return true;
				}
				else
				{
					original = current;
				}
			}
		}

		/// <summary>
		/// Configure a simple auto-retry strategy if no changes are detected.
		/// </summary>
		public void EnableAutoRetryIfNoChanges(int? attempts, TimeSpan? delay)
		{
			_noChangesRetry = attempts ?? DefaultAutoRetryIfNoChangesAttempts;
			_noChangesRetryDelay = delay ?? DefaultAutoRetryIfNoChangesDelay;
		}

		/// <summary>
		/// Notifies a file has been ignored for this hot-reload operation.
		/// </summary>
		/// <param name="file"></param>
		public void NotifyIgnored(string file)
			=> ImmutableInterlocked.Update(ref _ignoredFilePaths, static (files, file) => files.Add(file), file);

		/// <summary>
		/// Notifies multiple files have been ignored for this hot-reload operation.
		/// Use this overload to ignore several files at once, as opposed to the single-file overload.
		/// </summary>
		/// <param name="files">The collection of file paths to mark as ignored.</param>
		public void NotifyIgnored(IEnumerable<string> files)
			=> ImmutableInterlocked.Update(ref _ignoredFilePaths, static (files, ignored) => files.Union(ignored), files);

		/// <summary>
		/// As errors might get a bit after the complete from the IDE, we can defer the completion of the operation.
		/// </summary>
		public async ValueTask DeferComplete(HotReloadServerResult result, Exception? exception = null)
		{
			Debug.Assert(result != HotReloadServerResult.InternalError || exception is not null); // For internal error we should always provide an exception!

			if (Interlocked.CompareExchange(ref _deferredCompletion, new CancellationTokenSource(), null) is null)
			{
				_timeout.Change(_timeoutDelay, Timeout.InfiniteTimeSpan);
				await Task.Delay(TimeSpan.FromSeconds(1), _deferredCompletion.Token);
				if (!_deferredCompletion.IsCancellationRequested)
				{
					await Complete(result, exception);
				}
			}
		}

		public ValueTask Complete(HotReloadServerResult result, Exception? exception = null, ImmutableArray<Diagnostic>? diagnostics = null)
			=> Complete(result, exception, isFromNext: false, diagnostics: diagnostics);

		private async ValueTask Complete(HotReloadServerResult result, Exception? exception, bool isFromNext, ImmutableArray<Diagnostic>? diagnostics)
		{
			if (_result is -1
				&& result is HotReloadServerResult.NoChanges
				&& Interlocked.Decrement(ref _noChangesRetry) >= 0)
			{
				if (_noChangesRetryDelay is { TotalMilliseconds: > 0 })
				{
					await Task.Delay(_noChangesRetryDelay);
				}

				if (await _owner.TryRequestHotReload(default))
				{
					return;
				}
			}

			Debug.Assert(result != HotReloadServerResult.InternalError || exception is not null); // For internal error we should always provide an exception!

			// Remove this from current
			Interlocked.CompareExchange(ref _owner._current, null, this);
			_deferredCompletion?.Cancel(false); // No matter if already completed

			// Check if not already disposed
			if (Interlocked.CompareExchange(ref _result, (int)result, -1) is not -1)
			{
				return; // Already completed
			}

			_diagnostics = diagnostics;

			CompletionTime = DateTimeOffset.Now;
			await _timeout.DisposeAsync();

			// Consider previous hot-reload operation(s) as aborted (this is actually a chain list)
			if (_previous is not null)
			{
				await _previous.Complete(
					HotReloadServerResult.Aborted,
					new TimeoutException("An more recent hot-reload operation has completed."),
					isFromNext: true,
					diagnostics: null);
			}

			if (!isFromNext) // Only the head of the list should request update
			{
				await _owner.SendUpdate(this);
			}
		}
	}
	#endregion

	/// <inheritdoc />
	void IReporter.Verbose(string message)
		=> reporter.Verbose(message);

	/// <inheritdoc />
	void IReporter.Output(string message)
		=> reporter.Output(message);

	/// <inheritdoc />
	void IReporter.Warn(string message)
		=> reporter.Warn(message);

	/// <inheritdoc />
	void IReporter.Error(string message)
		=> reporter.Error(message);
}
