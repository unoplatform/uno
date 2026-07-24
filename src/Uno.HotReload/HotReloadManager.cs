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
	/// <summary>
	/// Creates a manager from a solution provider — the provider returns the solution snapshot
	/// to operate on (typically a freshly-loaded workspace solution, possibly restricted to the
	/// application's target framework), while the snapshot's originating
	/// <see cref="Solution.Workspace"/> remains the services and dispose owner.
	/// </summary>
	public static async ValueTask<HotReloadManager> CreateAsync(
		Func<CancellationToken, ValueTask<Solution>> solutionProvider,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
		=> await CreateAsync(
			await solutionProvider(ct).ConfigureAwait(false),
			metadataUpdateCapabilities,
			handler,
			new ChangesDetector(new TemporarySolutionAddDetector(solutionProvider, tracker), tracker),
			tracker,
			ct,
			forceEmitCompilationOutput);

	/// <summary>
	/// Creates a manager operating on the given solution snapshot with the default
	/// <see cref="SolutionUpdater"/>. Use the other overload to plug in a custom
	/// <see cref="ISolutionUpdater"/>.
	/// </summary>
	public static ValueTask<HotReloadManager> CreateAsync(
		Solution solution,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
		=> CreateAsync(
			solution,
			metadataUpdateCapabilities,
			handler,
			changesDetector,
			new SolutionUpdater(),
			tracker,
			ct,
			forceEmitCompilationOutput);

	/// <summary>
	/// Creates a manager operating on the given solution snapshot. The snapshot may differ from
	/// its workspace's <see cref="Workspace.CurrentSolution"/> (e.g. restricted to the running
	/// application's target framework); the originating <see cref="Solution.Workspace"/> is only
	/// used for services and ownership (disposed with the manager).
	/// </summary>
	public static async ValueTask<HotReloadManager> CreateAsync(
		Solution solution,
		string[] metadataUpdateCapabilities,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		ISolutionUpdater solutionUpdater,
		IHotReloadTracker tracker,
		CancellationToken ct,
		bool forceEmitCompilationOutput = false)
	{
		if (forceEmitCompilationOutput
			|| solution.Projects.Any(project => !File.Exists(project.CompilationOutputInfo.AssemblyPath)))
		{
			var result = await solution.EmitCompilationOutputAsync(ct).ConfigureAwait(false);
			result.EnsureSuccess();
		}

		var watch = await WatchHotReloadService.CreateAsync(solution, metadataUpdateCapabilities, ct).ConfigureAwait(false);

		return new HotReloadManager(solution.Workspace, watch, handler, changesDetector, solutionUpdater, tracker, solution);
	}

	private readonly FastAsyncLock _solutionUpdateGate = new();
	private readonly Workspace _innerWorkspace;
	private readonly IWatchHotReloadService _watchService;
	private readonly IHotReloadHandler _handler;
	private readonly IHotReloadTracker _tracker;
	private readonly IChangesDetector _changesDetector;
	private readonly ISolutionUpdater _solutionUpdater;

	// Internal (not private) so unit tests can drive the manager with a stub
	// IWatchHotReloadService; production code goes through CreateAsync.
	internal HotReloadManager(
		Workspace innerWorkspace,
		IWatchHotReloadService watchService,
		IHotReloadHandler handler,
		IChangesDetector changesDetector,
		ISolutionUpdater solutionUpdater,
		IHotReloadTracker tracker,
		Solution? initialSolution = null)
	{
		_innerWorkspace = innerWorkspace;
		_watchService = watchService;
		_handler = handler;
		_tracker = tracker;
		_changesDetector = changesDetector;
		_solutionUpdater = solutionUpdater;

		CurrentSolution = initialSolution ?? innerWorkspace.CurrentSolution;
	}

	public Solution CurrentSolution { get; private set; }

	public async Task ProcessFileChanges(Task<ImmutableHashSet<string>> filesAsync, CancellationToken ct)
	{
		// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
		var hotReload = await _tracker.StartOrContinueHotReload().ConfigureAwait(false);
		var files = await filesAsync.ConfigureAwait(false);

		// Process the batch of files (sequentially!)
		try
		{
			using var _ = await _solutionUpdateGate.LockAsync(ct).ConfigureAwait(false);

			// The merge decision must be made under the gate: operations are completed by the
			// pass that processes them, which runs while holding this gate. Deciding earlier
			// allows a batch to merge into an operation that completes before the batch's own
			// pass runs — that pass's outcome (e.g. Failed + diagnostics) would then be dropped
			// by the already-completed operation, reporting broken content as a clean reload.
			if (!hotReload.TryMerge(files))
			{
				hotReload = await _tracker.StartHotReload(files).ConfigureAwait(false);
			}

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

		// Up-to-date entries were consumed (their content is already in the solution) — not
		// ignored. Surface them for diagnosability only: they are how a re-observation of
		// content the pipeline just applied resolves to a plain NoChanges instead of forking.
		// Guard on the cheap struct-field check so the common (nothing-skipped) path builds
		// neither the enumerator nor the array.
		if (!result.UpToDateChanges.EditedDocuments.IsEmpty || !result.UpToDateChanges.EditedAdditionalDocuments.IsEmpty)
		{
			var upToDate = result.UpToDateChanges.GetAllPaths().ToImmutableArray();
			_tracker.Verbose($"{upToDate.Length} file(s) already up to date ({string.Join(", ", upToDate.Select(Path.GetFileName))})");
		}

		// Commit unconditionally, ahead of every terminal branch (spec 045 §2): an updater may have
		// rebound metadata/analyzer references (e.g. newly resolved packages) onto result.Solution.
		// If a cycle exited early without committing, those references would be lost and the next
		// cycle would restart from the stale originalSolution. The == originalSolution branch below
		// still compares against the captured snapshot, so its NoChanges decision is unchanged.
		workspace.CurrentSolution = result.Solution;

		// Converge every terminal outcome onto a single handler call + completion (spec 045 §1) so a
		// handler can perform delta-independent work (e.g. staging resolved package assemblies) on a
		// no-delta cycle, not only on Success. Deltas are non-empty only on Success.
		HotReloadOperationResult outcome;
		var deltas = ImmutableArray<Update>.Empty;
		var diagnostics = ImmutableArray<Diagnostic>.Empty;

		if (result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
		{
			// Updater encountered a fatal condition (typically a csproj re-evaluation error). The
			// manager — not the updater — owns the operation lifecycle, so we carry the diagnostics.
			_tracker.Output($"Hot reload failed: solution updater reported {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} error diagnostic(s).");
			outcome = HotReloadOperationResult.Failed;
			diagnostics = result.Diagnostics;
		}
		else if (result.Solution == originalSolution)
		{
			_tracker.Output($"No changes found in {string.Join(",", files.Select(Path.GetFileName))}");
			outcome = HotReloadOperationResult.NoChanges;
		}
		else
		{
			// Compile the solution and get deltas.
			var (updates, emitDiagnostics) = await _watchService.EmitSolutionUpdateAsync(result.Solution, ct).ConfigureAwait(false);
			// emitDiagnostics currently includes semantic Warnings and Errors for types being updated. We want to limit rude edits to the class
			// of unrecoverable errors that a user cannot fix and requires an app rebuild.
			var rudeEdits = emitDiagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

			_tracker.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");

			// Emit (EnC) diagnostics carry on every outcome of this branch; deltas are populated on Success only.
			diagnostics = emitDiagnostics;

			switch (rudeEdits.IsEmpty, updates.IsEmpty)
			{
				// A rude edit is unrecoverable: the user must rebuild. Surface every diagnostic.
				case (false, _):
					_tracker.Output("Unable to apply hot reload because of a rude edit.");
					foreach (var diagnostic in emitDiagnostics)
					{
						_tracker.Verbose(CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture));
					}

					outcome = HotReloadOperationResult.RudeEdit;
					break;

				// Metadata updates were produced and are applicable.
				case (true, false):
					outcome = HotReloadOperationResult.Success;
					deltas = updates;
					break;

				// No metadata updates, but the solution does not compile: the reload is blocked, not a no-op.
				// FIXME: GetCompilationErrors side-effects into _tracker but doesn't populate `diagnostics`;
				// consumers see Failed without the reason (asymmetric with rude edits). Needs dedup before fixing.
				case (true, true) when GetCompilationErrors(result.Solution, ct) is { IsEmpty: false } compilationErrors:
					_tracker.Output($"Hot reload blocked by {compilationErrors.Length} compilation error(s).");
					outcome = HotReloadOperationResult.Failed;
					break;

				// (true, true) with a clean compile: genuinely nothing to apply.
				default:
					_tracker.Output("No hot reload changes to apply.");
					outcome = HotReloadOperationResult.NoChanges;
					break;
			}
		}

		sw.Stop();

		// The handler now runs real side-effects (staging assemblies, applying deltas — possibly
		// across a worker→main boundary). A handler exception is a hot-reload failure for THIS
		// operation, distinct from a manager-internal fault (spec 045 §3). Cancellation is not a
		// failure and propagates to the ProcessFileChanges catch.
		var update = new HotReloadUpdate(files, changeSet, result, diagnostics, deltas);
		try
		{
			await _handler.OnHotReloadAsync(outcome, update, ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			// Mirror the manager-internal catch (ProcessFileChanges) so a handler-side failure (e.g. a
			// staging error) is traceable in the console, not only via the operation's result.
			_tracker.Warn($"Hot reload handler failed ({e.Message}).");
			_tracker.Verbose(e.ToString());
			await hotReload.Complete(HotReloadOperationResult.Failed, e, diagnostics).ConfigureAwait(false);
			return;
		}

		await hotReload.Complete(outcome, diagnostics: diagnostics).ConfigureAwait(false);
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
