using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;
using Uno.HotReload.Microsoft;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests.TestUtils;

/// <summary>
/// Minimal harness around <see cref="HotReloadManager"/>: an in-memory
/// workspace with a single empty C# project, a real <see cref="HotReloadTracker"/>,
/// and stubbed detector / updater / emitter so tests control exactly what each
/// hot-reload pass produces.
/// </summary>
internal sealed class HotReloadManagerHarness : IDisposable
{
	private readonly AdhocWorkspace _workspace;
	private int _updateCount;

	public HotReloadManagerHarness(
		Func<int, Task<(ImmutableArray<Update> Updates, ImmutableArray<Diagnostic> Diagnostics)>> onEmit,
		Func<Solution, SolutionUpdateResult>? onUpdate = null)
	{
		_workspace = new AdhocWorkspace();
		var project = _workspace.AddProject("TestProject", LanguageNames.CSharp);

		Reporter = new RecordingReporter();
		Tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: Reporter);
		Handler = new RecordingHandler();

		var projectId = project.Id;
		var detector = new StubChangesDetector();
		// Default updater: each pass yields a different solution snapshot, otherwise the manager
		// short-circuits with NoChanges before reaching the emitter. Tests pass onUpdate to control
		// the result (mutate or not, attach diagnostics, return a SolutionUpdateResult subtype).
		var update = onUpdate ?? (solution => new SolutionUpdateResult(
			solution.WithProjectAssemblyName(projectId, $"TestProject{Interlocked.Increment(ref _updateCount)}"),
			ChangeSet.IgnoreAll([])));
		var updater = new StubSolutionUpdater(update);

		Manager = new HotReloadManager(
			_workspace,
			new StubWatchService(onEmit),
			Handler,
			detector,
			updater,
			Tracker);
	}

	public HotReloadManager Manager { get; }

	public HotReloadTracker Tracker { get; }

	public RecordingReporter Reporter { get; }

	public RecordingHandler Handler { get; }

	public static Update CreateEmptyUpdate()
		=> new(Guid.NewGuid(), [], [], [], []);

	public static Diagnostic CreateErrorDiagnostic(string id = "TEST0001")
		=> Diagnostic.Create(
			new DiagnosticDescriptor(
				id,
				"Updater error",
				"Simulated updater failure (e.g. csproj re-evaluation error)",
				"Build",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true),
			Location.None);

	public static Diagnostic CreateRudeEditDiagnostic()
		=> Diagnostic.Create(
			new DiagnosticDescriptor(
				"ENC0033",
				"Rude edit",
				"Deleting a method requires restarting the application",
				"EditAndContinue",
				DiagnosticSeverity.Error,
				isEnabledByDefault: true),
			Location.None);

	public void Dispose()
		=> Manager.Dispose();

	internal sealed class RecordingHandler : IHotReloadHandler
	{
		private readonly List<(HotReloadOperationResult Result, HotReloadUpdate Update)> _calls = [];
		private readonly Lock _gate = new();

		/// <summary>When set, the handler throws this after recording the call (to test §3 failure handling).</summary>
		public Exception? ThrowAfterRecording { get; set; }

		/// <summary>Every (outcome, update) the manager handed to the handler, in order.</summary>
		public IReadOnlyList<(HotReloadOperationResult Result, HotReloadUpdate Update)> Calls
		{
			get
			{
				lock (_gate)
				{
					return [.. _calls];
				}
			}
		}

		/// <summary>The updates only (back-compat with success-oriented assertions).</summary>
		public IReadOnlyList<HotReloadUpdate> Updates
		{
			get
			{
				lock (_gate)
				{
					return _calls.ConvertAll(c => c.Update);
				}
			}
		}

		public ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct)
		{
			lock (_gate)
			{
				_calls.Add((result, update));
			}

			if (ThrowAfterRecording is { } ex)
			{
				throw ex;
			}

			return ValueTask.CompletedTask;
		}
	}

	private sealed class StubWatchService(
		Func<int, Task<(ImmutableArray<Update> Updates, ImmutableArray<Diagnostic> Diagnostics)>> onEmit) : IWatchHotReloadService
	{
		private int _calls;

		public Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
			=> onEmit(Interlocked.Increment(ref _calls));

		public void EndSession()
		{
		}
	}

	private sealed class StubChangesDetector : IChangesDetector
	{
		public ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
			=> ValueTask.FromResult(ChangeSet.IgnoreAll([]));
	}

	private sealed class StubSolutionUpdater(Func<Solution, SolutionUpdateResult> update) : ISolutionUpdater
	{
		public ValueTask<SolutionUpdateResult> UpdateAsync(Solution solution, ChangeSet changeSet, CancellationToken ct)
			=> ValueTask.FromResult(update(solution));
	}
}
