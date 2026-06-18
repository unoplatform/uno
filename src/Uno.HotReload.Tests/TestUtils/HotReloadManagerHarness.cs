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

	public HotReloadManagerHarness(Func<int, Task<(ImmutableArray<Update> Updates, ImmutableArray<Diagnostic> Diagnostics)>> onEmit)
	{
		_workspace = new AdhocWorkspace();
		var project = _workspace.AddProject("TestProject", LanguageNames.CSharp);

		Reporter = new RecordingReporter();
		Tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: Reporter);
		Handler = new RecordingHandler();

		var projectId = project.Id;
		var detector = new StubChangesDetector();
		// Each pass must yield a different solution snapshot, otherwise the
		// manager short-circuits with NoChanges before reaching the emitter.
		var updater = new StubSolutionUpdater(solution =>
			solution.WithProjectAssemblyName(projectId, $"TestProject{Interlocked.Increment(ref _updateCount)}"));

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
		private readonly List<HotReloadUpdate> _updates = [];
		private readonly Lock _gate = new();

		public IReadOnlyList<HotReloadUpdate> Updates
		{
			get
			{
				lock (_gate)
				{
					return [.. _updates];
				}
			}
		}

		public ValueTask SendAsync(HotReloadUpdate update, CancellationToken ct)
		{
			lock (_gate)
			{
				_updates.Add(update);
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

	private sealed class StubSolutionUpdater(Func<Solution, Solution> mutate) : ISolutionUpdater
	{
		public ValueTask<SolutionUpdateResult> UpdateAsync(Solution solution, ChangeSet changeSet, CancellationToken ct)
			=> ValueTask.FromResult(new SolutionUpdateResult(mutate(solution), ChangeSet.IgnoreAll([])));
	}
}
