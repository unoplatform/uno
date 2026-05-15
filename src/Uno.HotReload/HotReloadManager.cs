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
using Uno.HotReload.Diffing;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;
using Uno.Threading;

namespace Uno.HotReload;

public sealed class HotReloadManager : IDisposable
{
	public static async ValueTask<HotReloadManager> CreateAsync(
		Func<CancellationToken, ValueTask<Workspace>> workspaceProvider,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
		=> await CreateAsync(
			await workspaceProvider(ct).ConfigureAwait(false),
			metadataUpdateCapabilities,
			handler,
			new ChangesDetector(new TemporaryWorkspaceAddDetector(workspaceProvider, tracker), tracker),
			tracker,
			ct,
			forceEmitCompilationOutput);

	/// <summary>
	/// Creates a manager with the default <see cref="SolutionUpdater"/>. Use the
	/// other overload to plug in a custom <see cref="ISolutionUpdater"/>.
	/// </summary>
	public static ValueTask<HotReloadManager> CreateAsync(
		Workspace workspace,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
		=> CreateAsync(
			workspace,
			metadataUpdateCapabilities,
			handler,
			changesDetector,
			new SolutionUpdater(),
			tracker,
			ct,
			forceEmitCompilationOutput);

	public static async ValueTask<HotReloadManager> CreateAsync(
		Workspace workspace,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		ISolutionUpdater solutionUpdater,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
	{
		if (forceEmitCompilationOutput
			|| workspace.CurrentSolution.Projects.Any(project => !File.Exists(project.CompilationOutputInfo.AssemblyPath)))
		{
			var result = await workspace.EmitCompilationOutputAsync(ct).ConfigureAwait(false);
			result.EnsureSuccess();
		}

		var watch = await WatchHotReloadService.CreateAsync(workspace, metadataUpdateCapabilities, ct).ConfigureAwait(false);

		return new HotReloadManager(workspace, watch, handler, changesDetector, solutionUpdater, tracker);
	}

	private readonly FastAsyncLock _solutionUpdateGate = new();
	private readonly Workspace _innerWorkspace;
	private readonly WatchHotReloadService _watchService;
	private readonly IHotReloadHandler _handler;
	private readonly IHotReloadTracker _tracker;
	private readonly IChangesDetector _changesDetector;
	private readonly ISolutionUpdater _solutionUpdater;

	private HotReloadManager(
		Workspace innerWorkspace,
		WatchHotReloadService watchService,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		ISolutionUpdater solutionUpdater,
		IHotReloadTracker tracker)
	{
		_innerWorkspace = innerWorkspace;
		_watchService = watchService;
		_handler = handler;
		_tracker = tracker;
		_changesDetector = changesDetector;
		_solutionUpdater = solutionUpdater;

		CurrentSolution = innerWorkspace.CurrentSolution;
	}

	public Solution CurrentSolution { get; private set; }

	public async Task ProcessFileChanges(Task<ImmutableHashSet<string>> filesAsync, CancellationToken ct)
	{
		// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
		var hotReload = await _tracker.StartOrContinueHotReload().ConfigureAwait(false);
		var files = await filesAsync.ConfigureAwait(false);
		if (!hotReload.TryMerge(files))
		{
			hotReload = await _tracker.StartHotReload(files).ConfigureAwait(false);
		}

		// Process the batch of files (sequentially!)
		try
		{
			using var _ = await _solutionUpdateGate.LockAsync(ct).ConfigureAwait(false);
			await ProcessSolutionChanged(hotReload, files, ct).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			_tracker.Warn($"Internal error while processing hot-reload ({e.Message}).");
			_tracker.Verbose(e.ToString());

			await hotReload.Complete(HotReloadOperationResult.InternalError, e).ConfigureAwait(false);
		}
	}

	private async ValueTask ProcessSolutionChanged(HotReloadOperation hotReload, ImmutableHashSet<string> files, CancellationToken ct)
	{
		var workspace = this;
		var sw = Stopwatch.StartNew();

		// Detects the changes and try to update the solution
		var originalSolution = workspace.CurrentSolution;
		var changeSet = await _changesDetector.DiscoverChangesAsync(originalSolution, files, ct).ConfigureAwait(false);
		var result = await _solutionUpdater.UpdateAsync(originalSolution, changeSet, ct).ConfigureAwait(false);

		// Updaters report what they did not consume; surface that to the operation
		// before any short-circuit so the report reflects skipped inputs.
		hotReload.NotifyIgnored(result.IgnoredChanges.GetAllPaths());

		// Updater encountered a fatal condition (typically a csproj re-evaluation
		// error). The manager — not the updater — owns the operation lifecycle, so
		// we complete the operation here with the diagnostics and skip the rest of
		// the cycle.
		if (result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
		{
			await hotReload.Complete(HotReloadOperationResult.Failed, diagnostics: result.Diagnostics).ConfigureAwait(false);
			return;
		}

		if (result.Solution == originalSolution)
		{
			_tracker.Output($"No changes found in {string.Join(",", files.Select(Path.GetFileName))}");

			await hotReload.Complete(HotReloadOperationResult.NoChanges).ConfigureAwait(false);
			return;
		}

		// No matter if the build will succeed or not, we update the _currentSolution.
		// Files needs to be updated again to fix the compilation errors.
		workspace.CurrentSolution = result.Solution;

		// Compile the solution and get deltas
		var (updates, hotReloadDiagnostics) = await _watchService.EmitSolutionUpdateAsync(result.Solution, ct).ConfigureAwait(false);
		// hotReloadDiagnostics currently includes semantic Warnings and Errors for types being updated. We want to limit rude edits to the class
		// of unrecoverable errors that a user cannot fix and requires an app rebuild.
		var rudeEdits = hotReloadDiagnostics.RemoveAll(d => d.Severity == DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

		_tracker.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");
		sw.Stop();

		if (rudeEdits.IsEmpty && updates.IsEmpty)
		{
			var compilationErrors = GetCompilationErrors(result.Solution, ct);
			if (compilationErrors.IsEmpty)
			{
				_tracker.Output("No hot reload changes to apply.");
				await hotReload.Complete(HotReloadOperationResult.NoChanges).ConfigureAwait(false);
			}
			else
			{
				await hotReload.Complete(HotReloadOperationResult.Failed, diagnostics: hotReloadDiagnostics).ConfigureAwait(false);
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

			await hotReload.Complete(HotReloadOperationResult.RudeEdit, diagnostics: hotReloadDiagnostics).ConfigureAwait(false);
			return;
		}

		var update = new HotReloadUpdate(
			files,
			changeSet,
			result,
			hotReloadDiagnostics,
			updates);
		await _handler.SendAsync(update, ct).ConfigureAwait(false);

		await hotReload.Complete(HotReloadOperationResult.Success).ConfigureAwait(false);
	}

	/// <summary>
	/// Maximum number of compilation errors to format and report per hot-reload cycle.
	/// Formatting diagnostics allocates strings via <see cref="CSharpDiagnosticFormatter"/>;
	/// capping prevents unbounded allocations when the code is heavily broken.
	/// </summary>
	private const int MaxCompilationErrorsPerCycle = 20;

	private ImmutableArray<string> GetCompilationErrors(Solution solution, CancellationToken cancellationToken)
	{
		// ALC-hosted workspaces always have a single project — sequential iteration avoids
		// Parallel.ForEach overhead (thread pool work items, partitioner, lambda closures).
		var builder = ImmutableArray.CreateBuilder<string>();
		foreach (var project in solution.Projects)
		{
			if (!project.TryGetCompilation(out var compilation))
			{
				continue;
			}

			var compilationDiagnostics = compilation.GetDiagnostics(cancellationToken);
			if (compilationDiagnostics.IsEmpty)
			{
				continue;
			}

			foreach (var item in compilationDiagnostics)
			{
				if (item.Severity == DiagnosticSeverity.Error)
				{
					var diagnostic = CSharpDiagnosticFormatter.Instance.Format(item, CultureInfo.InvariantCulture);
					_tracker.Output("\x1B[40m\x1B[31m" + diagnostic);
					builder.Add(diagnostic);

					// On WASM, memory.grow() is irreversible — cap diagnostic formatting
					// to avoid unbounded string allocations when code is heavily broken.
					// On desktop, report all errors for full IDE-like diagnostics.
					if (OperatingSystem.IsBrowser() && builder.Count >= MaxCompilationErrorsPerCycle)
					{
						_tracker.Output($"... and more errors (capped at {MaxCompilationErrorsPerCycle}).");
						return builder.ToImmutable();
					}
				}
			}
		}

		return builder.ToImmutable();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_watchService.EndSession();
		_innerWorkspace.Dispose();
	}
}
