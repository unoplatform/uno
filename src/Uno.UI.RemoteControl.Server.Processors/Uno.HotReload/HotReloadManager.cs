using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Diffing;
using Uno.Threading;
using Uno.UI.RemoteControl.Host.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.HotReload;

public delegate ValueTask SendUpdatesAsync(ImmutableHashSet<string> files, ImmutableArray<Update> updates, CancellationToken ct);

internal sealed class HotReloadManager : IDisposable
{
	public static async ValueTask<HotReloadManager> CreateAsync(
		Func<CancellationToken, ValueTask<Workspace>> workspaceProvider,
		string[] metadataUpdateCapabilities,
		SendUpdatesAsync sendUpdates,
		IHotReloadTracker tracker)
	{
		var ct = new CancellationTokenSource();
		var initialWorkspace = await workspaceProvider(ct.Token);
		var watch = await WatchHotReloadService.CreateAsync(initialWorkspace, metadataUpdateCapabilities, ct.Token);
		var detector = new ChangesDetector(workspaceProvider, tracker);

		return new HotReloadManager(initialWorkspace, watch, [] /* TODO */, sendUpdates, tracker, detector);
	}

	private readonly FastAsyncLock _solutionUpdateGate = new();
	private readonly Workspace _innerWorkspace;
	private readonly WatchHotReloadService _watchService;
	private readonly SendUpdatesAsync _sendUpdates;
	private readonly IHotReloadTracker _tracker;
	private readonly ChangesDetector _changesDetector;

	private HotReloadManager(
		Workspace innerWorkspace,
		WatchHotReloadService watchService,
		string?[] outputPaths, // TODO: Remove
		SendUpdatesAsync sendUpdates,
		IHotReloadTracker tracker,
		ChangesDetector changesDetector)
	{
		_innerWorkspace = innerWorkspace;
		_watchService = watchService;
		_sendUpdates = sendUpdates;
		_tracker = tracker;
		_changesDetector = changesDetector;
		CurrentSolution = innerWorkspace.CurrentSolution;
		OutputPaths = outputPaths;
	}

	public Solution CurrentSolution { get; private set; }
	public string?[] OutputPaths { get; init; }

	/// <inheritdoc />
	public void Dispose()
	{
		_watchService.EndSession();
		_innerWorkspace.Dispose();
	}

	public async Task ProcessFileChanges(Task<ImmutableHashSet<string>> filesAsync, CancellationToken ct)
	{
		// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
		var hotReload = await _tracker.StartOrContinueHotReload();
		var files = await filesAsync;
		if (!hotReload.TryMerge(files))
		{
			hotReload = await _tracker.StartHotReload(files);
		}

		// Process the batch of files (sequentially!)
		try
		{
			using var _ = await _solutionUpdateGate.LockAsync(ct);
			await ProcessSolutionChanged(hotReload, files, ct);
		}
		catch (Exception e)
		{
			_tracker.Warn($"Internal error while processing hot-reload ({e.Message}).");
			_tracker.Verbose(e.ToString());

			await hotReload.Complete(HotReloadServerResult.InternalError, e);
		}
	}

	private async ValueTask ProcessSolutionChanged(HotReloadTracker.HotReloadServerOperation hotReload, ImmutableHashSet<string> files, CancellationToken ct)
	{
		var workspace = this;
		var sw = Stopwatch.StartNew();

		// Detects the changes and try to update the solution
		var originalSolution = workspace.CurrentSolution;
		var changeSet = await _changesDetector.DiscoverChangesAsync(originalSolution, files, ct);
		var solution = await originalSolution.ApplyAsync(changeSet, hotReload, ct);

		if (solution == originalSolution)
		{
			_tracker.Output($"No changes found in {string.Join(",", files.Select(Path.GetFileName))}");

			await hotReload.Complete(HotReloadServerResult.NoChanges);
			return;
		}

		// No matter if the build will succeed or not, we update the _currentSolution.
		// Files needs to be updated again to fix the compilation errors.
		workspace.CurrentSolution = solution;

		// Compile the solution and get deltas
		var (updates, hotReloadDiagnostics) = await _watchService.EmitSolutionUpdateAsync(solution, ct);
		// hotReloadDiagnostics currently includes semantic Warnings and Errors for types being updated. We want to limit rude edits to the class
		// of unrecoverable errors that a user cannot fix and requires an app rebuild.
		var rudeEdits = hotReloadDiagnostics.RemoveAll(d => d.Severity == DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

		_tracker.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");
		sw.Stop();

		if (rudeEdits.IsEmpty && updates.IsEmpty)
		{
			var compilationErrors = GetCompilationErrors(solution, ct);
			if (compilationErrors.IsEmpty)
			{
				_tracker.Output("No hot reload changes to apply.");
				await hotReload.Complete(HotReloadServerResult.NoChanges);
			}
			else
			{
				await hotReload.Complete(HotReloadServerResult.Failed, diagnostics: hotReloadDiagnostics);
			}

			return;
		}

		if (!rudeEdits.IsEmpty)
		{
			// Rude edit.
			_tracker.Output("Unable to apply hot reload because of a rude edit.");
			foreach (var diagnostic in hotReloadDiagnostics)
			{
				_tracker.Verbose(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
			}

			await hotReload.Complete(HotReloadServerResult.RudeEdit, diagnostics: hotReloadDiagnostics);
			return;
		}

		await _sendUpdates(files, updates, ct);

		await hotReload.Complete(HotReloadServerResult.Success);
	}

	private ImmutableArray<string> GetCompilationErrors(Solution solution, CancellationToken cancellationToken)
	{
		var @lock = new object();
		var builder = ImmutableArray<string>.Empty;
		Parallel.ForEach(solution.Projects,
			project =>
			{
				if (!project.TryGetCompilation(out var compilation))
				{
					return;
				}

				var compilationDiagnostics = compilation.GetDiagnostics(cancellationToken);
				if (compilationDiagnostics.IsEmpty)
				{
					return;
				}

				var projectDiagnostics = ImmutableArray<string>.Empty;
				foreach (var item in compilationDiagnostics)
				{
					if (item.Severity == DiagnosticSeverity.Error)
					{
						var diagnostic = CSharpDiagnosticFormatter.Instance.Format(item, CultureInfo.InvariantCulture);
						_tracker.Output("\x1B[40m\x1B[31m" + diagnostic);
						projectDiagnostics = projectDiagnostics.Add(diagnostic);
					}
				}

				lock (@lock)
				{
					builder = builder.AddRange(projectDiagnostics);
				}
			});

		return builder;
	}
}
