using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

		var watch = await WatchHotReloadService.CreateAsync(solution, metadataUpdateCapabilities, tracker, ct).ConfigureAwait(false);

		return new HotReloadManager(solution.Workspace, watch, handler, changesDetector, solutionUpdater, tracker, solution);
	}

	private readonly FastAsyncLock _solutionUpdateGate = new();
	private readonly Workspace _innerWorkspace;
	private readonly IWatchHotReloadService _watchService;
	private readonly IHotReloadHandler _handler;
	private readonly IHotReloadTracker _tracker;
	private readonly IChangesDetector _changesDetector;
	private readonly ISolutionUpdater _solutionUpdater;
	private readonly ImmutableDictionary<ProjectId, ImmutableDictionary<string, PortableExecutableReference>> _baselineReferences;

	// The last pin set reported at Output level; guarded by _solutionUpdateGate (single-flight passes).
	private ImmutableHashSet<PinnedReference>? _reportedPins;

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

		// The reference set the EnC baseline of each project's module captures at session start;
		// every emit is pinned back onto it (see the alignment step in ProcessSolutionChanged).
		_baselineReferences = CurrentSolution.SnapshotReferenceIdentities(out var multiVersionNames);
		foreach (var name in multiVersionNames)
		{
			_tracker.Verbose(
				$"Reference identity pinning is partially disabled for '{name}': one or more projects in the " +
				"session baseline reference multiple identities of it (a re-bind of it in those projects " +
				"during hot reload requires a rebuild).");
		}
	}

	public Solution CurrentSolution { get; private set; }

	public async Task ProcessFileChanges(Task<ImmutableHashSet<string>> filesAsync, CancellationToken ct)
	{
		// Notify the start of the hot-reload processing as soon as possible, even before the buffering of file change is completed
		var hotReload = await _tracker.StartOrContinueHotReload().ConfigureAwait(false);
		var files = await filesAsync.ConfigureAwait(false);

		// Hold the solution-update gate across the WHOLE pass, including the catch: an operation is
		// completed by the pass that processes it, and that completion — the terminal outcome on
		// success, or the catch's InternalError on failure — must happen under the gate. Were the
		// gate released before the catch completed (the failure path releases it as the exception
		// unwinds out of the try), a queued batch could acquire it, merge into the still-open
		// operation (TryMerge sees _result == -1) and complete it first, dropping this pass's
		// outcome — reporting a failed pass as a clean reload.
		using var _ = await _solutionUpdateGate.LockAsync(ct).ConfigureAwait(false);

		// Process the batch of files (sequentially!)
		try
		{
			// The merge decision is made under the gate (see above): deciding it earlier would let a
			// batch merge into an operation that completes before the batch's own pass runs.
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
		var diagnosticsReporter = new DiagnosticsReporter(_tracker);

		// Detects the changes and try to update the solution
		var originalSolution = workspace.CurrentSolution;
		var changeSet = await _changesDetector.DiscoverChangesAsync(originalSolution, files, ct).ConfigureAwait(false);
		var result = await _solutionUpdater.UpdateAsync(originalSolution, changeSet, ct).ConfigureAwait(false);

		// Surface the updater's own diagnostics (csproj re-evaluation, package resolution, …) as verbose
		// detail. The updater returns them in the result for the caller to handle — neither it nor the
		// change detector reports them (both in uno and Studio Live's AdhocSolutionUpdater), so this is the
		// single reporting point; the Failed-outcome summary for error diagnostics is handled below.
		diagnosticsReporter.ReportSolutionUpdateDiagnostics(result.Diagnostics);

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
		var pinnedReferences = ImmutableArray<PinnedReference>.Empty;

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
			// Roslyn 5.x refuses to emit any EnC update for a project that references an assembly at
			// another identity than the one its baseline captured (ENC1099, a rude edit with zero
			// deltas) — 4.x had no such check, and the runtime accepts deltas binding assemblies the
			// baseline never referenced. A package added mid-session routinely re-binds part of its
			// transitive closure onto assemblies the application was already built against (#24023),
			// so pin those back to their baseline identity for this pass: genuinely new assemblies
			// flow through (their files are staged by the handler), and the emitted delta binds
			// against the identities the running application actually loaded.
			var alignedSolution = result.Solution.WithBaselineReferenceIdentities(_baselineReferences, out pinnedReferences);
			if (!pinnedReferences.IsEmpty)
			{
				// The updater's re-bind persists in CurrentSolution, so the same pins recur on every
				// later cycle; the Output summary would repeat verbatim for the rest of the session
				// (unlike the generator-error report below, the condition is not fixable mid-session).
				// Report it at Output only when the pin set changes — a NEW pin must not drown in the
				// repetition — and keep the per-cycle detail at Verbose.
				var pins = pinnedReferences.ToImmutableHashSet();
				if (_reportedPins is null || !_reportedPins.SetEquals(pins))
				{
					_reportedPins = pins;
					_tracker.Output(
						$"Hot reload keeps {FormatPinnedReferences(pinnedReferences)} at the identity the " +
						"application was built with; changing a referenced assembly requires a rebuild.");
				}

				foreach (var pin in pinnedReferences)
				{
					_tracker.Verbose($"{pin.AssemblyName} ({pin.ProjectName}): '{pin.ConflictingPath}' -> '{pin.BaselinePath}'");
				}
			}
			else
			{
				// A pin-free emit means the re-bind is gone (e.g. the csproj edit was reverted):
				// forget the last-reported set so a later identical re-bind reports at Output again.
				_reportedPins = null;
			}

			// The projects owning the pass's changed files (spec 054) — resolved once and shared by the
			// generator-error suppression below and the blocked-compilation audit in the (true, true)
			// branch, so neither judges a project the pass never touched.
			var auditedProjects = ResolveAuditProjects(alignedSolution, files);

			// A source generator can report an Error WITHOUT the C# compilation failing: a corrupt .resw
			// makes the Uno XAML generator emit UXAML0003 (Error) while the assembly still compiles. Roslyn
			// 5.x's EnC refuses to emit ANY metadata update while such a generator error is present (4.x did
			// not), so a single broken generator input freezes hot reload for every later edit — including
			// valid edits to unrelated files — until it is fixed. Generator diagnostics never reach
			// compilation.GetDiagnostics() (so the GetCompilationErrors audit below cannot see them either),
			// so read them from the change-set's projects and suppress them for the emit: a non-fatal
			// generator error then degrades to stale generated output instead of blocking hot reload.
			// Generated code that genuinely does NOT compile still reports a C# error and is handled by the
			// (true, true) audit / rude-edit branches below. The suppression is applied to the emit itself,
			// not as a retry after a blocked emit: EmitSolutionUpdateAsync advances the EnC baseline as a
			// side effect, so a throwaway first emit would consume the change and leave a second nothing to
			// apply.
			var generatorErrors = await GetGeneratorErrorsAsync(auditedProjects, ct).ConfigureAwait(false);
			if (!generatorErrors.IsEmpty)
			{
				_tracker.Output(
					$"Hot reload is continuing despite {generatorErrors.Length} source generator error(s); " +
					"the affected generated code stays stale until they are fixed.");
				diagnosticsReporter.ReportAnalyzerDiagnostics(generatorErrors);
			}
			// Attempt to emit anyway ...
			var emitSolution = alignedSolution.WithSuppressedDiagnostics(generatorErrors);

			// Compile the solution with those generator errors suppressed, and get deltas.
			var emit = await _watchService.EmitSolutionUpdateAsync(emitSolution, ct).ConfigureAwait(false);
			var updates = emit.Deltas;
			var emitDiagnostics = emit.Diagnostics;

			_tracker.Verbose($"Emit status: {emit.Status}");

			// A project the engine cannot update in place will NOT be brought back in line by any
			// later delta: until it is rebuilt, the application keeps running the code it was built
			// with while the sources say something else. Say so explicitly — the emit diagnostics
			// name the rude edit only on the cycle that introduced it, so a session that carries on
			// afterwards would otherwise look healthy while silently diverging.
			if (emit.RequiresRebuildOrRestart)
			{
				// Roslyn 4.x reports the situation without naming any project (see
				// HotReloadEmitResult.RequiresRebuildOrRestart), so the subject stays generic there.
				var subject = emit.ProjectsRequiringRebuildOrRestart is { IsEmpty: false } projects
					? string.Join(", ", projects)
					: "the application";

				_tracker.Output(
					$"Hot reload cannot update {subject}: it must be rebuilt (or restarted) before it matches " +
					"its sources again.");
			}
			// emitDiagnostics currently includes semantic Warnings and Errors for types being updated. We want to limit rude edits to the class
			// of unrecoverable errors that a user cannot fix and requires an app rebuild.
			var rudeEdits = emitDiagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Warning || !d.Descriptor.Id.StartsWith("ENC", StringComparison.Ordinal));

			_tracker.Output($"Found {updates.Length} metadata updates after {sw.Elapsed}");

			// Emit (EnC) diagnostics carry on every outcome of this branch; deltas are populated on Success only.
			//
			// The suppressed generator errors carry too. Suppressing them keeps hot reload running (that is
			// the point), but they remain the reason the generated code is stale, and for the file the user
			// just edited they are the ONLY actionable message: invalid XAML reports UXAML0001 here, while
			// the emit sees a degraded generated tree and reports something like ENC0020 naming a generated
			// member the user never wrote. Reporting them to the console alone left consumers with the
			// misleading half.
			//
			// No dedup needed, unlike the compilation-error case below: generator diagnostics never appear in
			// compilation.GetDiagnostics(), so they cannot already be present in emitDiagnostics. They are
			// also already scoped to the change set's projects via auditedProjects.
			diagnostics = emitDiagnostics.AddRange(generatorErrors);

			switch (rudeEdits.IsEmpty, updates.IsEmpty)
			{
				// A rude edit is unrecoverable: the user must rebuild. Surface every diagnostic.
				case (false, _):
					_tracker.Output("Unable to apply hot reload because of a rude edit.");
					diagnosticsReporter.ReportEmitDiagnostics(emitDiagnostics);
					outcome = HotReloadOperationResult.RudeEdit;
					break;

				// Metadata updates were produced and are applicable.
				case (true, false):
					outcome = HotReloadOperationResult.Success;
					deltas = updates;
					break;

				// No metadata updates, but the solution does not compile: the reload is blocked, not a no-op.
				// The audit is scoped to the projects owning this pass's changed files (spec 054): a pass must
				// never complete Failed on errors from projects it never touched. When the change-set resolves
				// to no project the audit yields no errors and we fall through to NoChanges below.
				// FIXME: these compilation errors are reported to the console but not propagated into the
				// operation's `diagnostics`; consumers see Failed without the reason (asymmetric with rude
				// edits). Needs dedup before fixing.
				case (true, true) when GetCompilationErrors(auditedProjects, ct) is { Errors.IsEmpty: false } audit:
					_tracker.Output(
						$"Hot reload blocked by {audit.Errors.Length} compilation error(s) in " +
						$"{string.Join(", ", audit.BlockingProjects)} " +
						$"(edited: {FormatEditedFiles(files)}).");
					diagnosticsReporter.ReportCompilationDiagnostics(audit.Errors);
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
		var update = new HotReloadUpdate(files, changeSet, result, diagnostics, deltas, pinnedReferences);
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

	private static CompilationAudit GetCompilationErrors(ImmutableArray<Project> auditedProjects, CancellationToken cancellationToken)
	{
		// auditedProjects is the change-set scope resolved by the caller (spec 054): a pass never judges a
		// project it did not touch, and an empty scope simply yields an empty audit (the loop below does
		// not run) so the caller falls through to NoChanges instead of failing on foreign projects. This is
		// a pure read — TryGetCompilation serves whatever compilation is already cached and never forces a
		// compile; formatting/capping of the returned errors is the caller's job (DiagnosticsReporter). Only
		// the projects that actually produce an error are named (a scoped project can be clean), ordered so
		// the blocked line is deterministic.
		var errors = ImmutableArray.CreateBuilder<Diagnostic>();
		var blockingProjects = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var project in auditedProjects)
		{
			if (!project.TryGetCompilation(out var compilation))
			{
				continue;
			}

			foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
			{
				if (diagnostic.Severity == DiagnosticSeverity.Error)
				{
					errors.Add(diagnostic);
					blockingProjects.Add(project.Name);
				}
			}
		}

		return new CompilationAudit(errors.ToImmutable(), [.. blockingProjects]);
	}

	/// <summary>
	/// The Error-severity diagnostics reported by the source generators of the given
	/// <paramref name="auditedProjects"/> (the pass's change-set scope). Source-generator diagnostics are
	/// absent from <see cref="Compilation.GetDiagnostics(CancellationToken)"/> — reachable only via
	/// <see cref="RoslynExtensions.GetSourceGeneratorDiagnosticsAsync"/> — yet Roslyn 5.x's EnC refuses to
	/// emit a metadata update while any of them is present. <see cref="ProcessSolutionChanged"/> suppresses
	/// these for the emit so a non-fatal generator error (e.g. a corrupt <c>.resw</c> → UXAML0003) does not
	/// block hot reload for otherwise-valid edits. Empty when there are no audited projects or when the
	/// reflection-bound API is unavailable, so the behavior degrades to the pre-fix path.
	/// </summary>
	private static async ValueTask<ImmutableArray<Diagnostic>> GetGeneratorErrorsAsync(
		ImmutableArray<Project> auditedProjects,
		CancellationToken cancellationToken)
	{
		var errors = ImmutableArray.CreateBuilder<Diagnostic>();
		foreach (var project in auditedProjects)
		{
			foreach (var diagnostic in await project.GetSourceGeneratorDiagnosticsAsync(cancellationToken).ConfigureAwait(false))
			{
				if (diagnostic.Severity == DiagnosticSeverity.Error)
				{
					errors.Add(diagnostic);
				}
			}
		}

		return errors.ToImmutable();
	}

	/// <summary>
	/// Resolves the pass's change-set to the distinct set of projects that own those files — as source
	/// <see cref="Document"/>s or as <see cref="AdditionalDocument"/>s (XAML and other generator inputs,
	/// whose edits can block compilation exactly like a source edit). Both document kinds are matched
	/// through <see cref="PathComparer"/> — the same separator/case-agnostic comparison the rest of the
	/// hot-reload pipeline uses — so a document never escapes the scope because the workspace stored its
	/// path with a different separator or casing than the change event carried. A file belonging to no
	/// project contributes nothing, so a change-set touching only foreign/out-of-solution files resolves
	/// to an empty set.
	/// </summary>
	private static ImmutableArray<Project> ResolveAuditProjects(Solution solution, ImmutableHashSet<string> files)
	{
		// Index the change-set once through the pipeline's path comparer so each project's documents are
		// matched with a single O(1) lookup instead of an O(files) scan per document.
		var changeSet = files.ToHashSet(PathComparer.PathEqualityComparer);

		return solution
			.Projects
			.Where(project =>
				project.Documents.Any(document => document.FilePath is { } path && changeSet.Contains(path))
				|| project.AdditionalDocuments.Any(document => document.FilePath is { } path && changeSet.Contains(path)))
			.ToImmutableArray();
	}

	/// <summary>
	/// Names the pinned assemblies for the pin summary line: distinct simple names, ordered for a
	/// deterministic message, capped to the first 3 with a <c>+N more</c> suffix beyond (mirrors
	/// <see cref="FormatEditedFiles"/>).
	/// </summary>
	private static string FormatPinnedReferences(ImmutableArray<PinnedReference> pinned)
	{
		const int maxNamed = 3;
		var names = pinned
			.Select(pin => pin.AssemblyName)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		var named = string.Join(", ", names.Take(maxNamed));

		return names.Length > maxNamed
			? $"{named} +{names.Length - maxNamed} more"
			: named;
	}

	/// <summary>
	/// Formats the pass's edited file names for the blocked-compilation output line: base names only
	/// (<see cref="Path.GetFileName(string)"/>), ordered for a deterministic message, capped to the first
	/// 3 with a <c>+N more</c> suffix beyond.
	/// </summary>
	private static string FormatEditedFiles(ImmutableHashSet<string> files)
	{
		const int maxNamed = 3;
		var names = files
			.Select(Path.GetFileName)
			.OfType<string>()
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray();
		var named = string.Join(", ", names.Take(maxNamed));

		return names.Length > maxNamed
			? $"{named} +{names.Length - maxNamed} more"
			: named;
	}

	/// <summary>
	/// Outcome of the change-set-scoped blocked-compilation audit: the compilation errors found in the
	/// audited projects, and the distinct, ordered names of the projects that actually produced those
	/// errors (named in the blocked output line). Formatting/reporting is the caller's job.
	/// </summary>
	private readonly record struct CompilationAudit(ImmutableArray<Diagnostic> Errors, ImmutableArray<string> BlockingProjects);

	/// <inheritdoc />
	public void Dispose()
	{
		_watchService.EndSession();
		_innerWorkspace.Dispose();
	}
}
