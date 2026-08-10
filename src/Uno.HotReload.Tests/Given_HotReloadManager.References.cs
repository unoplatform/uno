using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

/// <summary>
/// End-to-end coverage of issue #24023 through the real EnC pipeline: a solution updater that
/// re-binds metadata references mid-session — what a <c>PackageReference</c> add does when the
/// package's transitive closure overlaps the running application's graph — must not turn the
/// cycle into a rude edit. The manager pins re-bound identities back to the session baseline
/// before the emit; genuinely new assemblies keep flowing through.
/// </summary>
[TestClass]
public sealed class Given_HotReloadManager_References
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"The #24023 scenario: one cycle adds a brand-new assembly (the added package), blindly " +
		"re-binds an already-referenced assembly at another version (an overlapping transitive) " +
		"and edits a document to use the new package's type. Roslyn 5.x alone refuses the emit " +
		"with ENC1099/0 deltas; with baseline-identity pinning the delta must be produced.")]
	public async Task When_PackageAddRebindsExistingAssembly_Then_UpdateIsStillEmitted()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var enc = await EnCHarness.CreateAsync(temp, ct);

		var packageLib = EnCHarness.EmitLibrary(temp, "FreshLib", "1.0.0.0", """
			namespace Fresh;
			public static class Info
			{
				public static string Name => "fresh";
			}
			""");
		var conflictV2 = EnCHarness.EmitLibrary(temp, "ConflictLib", "2.0.0.0", EnCHarness.ConflictLibSource, subdir: "v2");

		// What the Studio Live updater does on a PackageReference add: every assembly of the
		// resolved closure is bound onto the project (no same-name filtering), and the document
		// edit lands on top of the re-bound solution.
		var updater = new DelegateSolutionUpdater(solution => new SolutionUpdateResult(
			solution
				.AddMetadataReference(enc.ProjectId, MetadataReference.CreateFromFile(packageLib))
				.AddMetadataReference(enc.ProjectId, MetadataReference.CreateFromFile(conflictV2))
				.WithDocumentText(enc.DocumentId, EnCHarness.AppText("Fresh.Info.Name + Conflict.Info.Version")),
			ChangeSet.IgnoreAll([])));

		var reporter = new RecordingReporter();
		var handler = new HotReloadManagerHarness.RecordingHandler();
		using var manager = new HotReloadManager(
			enc.Workspace,
			enc.Watch,
			handler,
			new DelegateChangesDetector(),
			updater,
			new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter),
			enc.Solution);

		await manager.ProcessFileChanges(
			Task.FromResult(ImmutableHashSet.Create("/work/MainPage.xaml")),
			ct);

		var (result, update) = handler.Calls.Should().ContainSingle().Subject;
		result.Should().Be(
			HotReloadOperationResult.Success,
			"a package add whose closure overlaps the app's graph must hot reload, not rude-edit (#24023)");
		update.Deltas.Should().HaveCount(1, "the document edit compiles against the added assembly");
		update.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
		update.PinnedReferences.Should().ContainSingle(
			"the handler must be able to exclude the conflicting file from staging")
			.Which.AssemblyName.Should().Be("ConflictLib");

		reporter.Outputs.Should().Contain(
			o => o.Contains("ConflictLib", StringComparison.Ordinal) && o.Contains("rebuild", StringComparison.Ordinal),
			"the pinned re-bind must be surfaced to the user, naming the pinned assembly");
	}

	[TestMethod]
	[Description(
		"The re-bind persists in the manager's CurrentSolution while EnC committed the PINNED " +
		"solution — the seam at the center of the fix. A later plain document edit must re-pin " +
		"and emit cleanly (no ENC1099 against the committed baseline), with the pin surfaced on " +
		"the update but the Output summary printed only once for the unchanged pin set.")]
	public async Task When_RebindPersistsAcrossCycles_Then_NextEditStillEmits()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		using var temp = new TempDirectory();
		using var enc = await EnCHarness.CreateAsync(temp, ct);

		var packageLib = EnCHarness.EmitLibrary(temp, "FreshLib", "1.0.0.0", """
			namespace Fresh;
			public static class Info
			{
				public static string Name => "fresh";
			}
			""");
		var conflictV2 = EnCHarness.EmitLibrary(temp, "ConflictLib", "2.0.0.0", EnCHarness.ConflictLibSource, subdir: "v2");

		// Cycle 1 re-binds (package add); cycle 2 is a plain document edit on top of the
		// persisting re-bound solution.
		var cycle = 0;
		var updater = new DelegateSolutionUpdater(solution => new SolutionUpdateResult(
			++cycle == 1
				? solution
					.AddMetadataReference(enc.ProjectId, MetadataReference.CreateFromFile(packageLib))
					.AddMetadataReference(enc.ProjectId, MetadataReference.CreateFromFile(conflictV2))
					.WithDocumentText(enc.DocumentId, EnCHarness.AppText("Fresh.Info.Name + Conflict.Info.Version"))
				: solution
					.WithDocumentText(enc.DocumentId, EnCHarness.AppText("Fresh.Info.Name + \"!\" + Conflict.Info.Version")),
			ChangeSet.IgnoreAll([])));

		var reporter = new RecordingReporter();
		var handler = new HotReloadManagerHarness.RecordingHandler();
		using var manager = new HotReloadManager(
			enc.Workspace,
			enc.Watch,
			handler,
			new DelegateChangesDetector(),
			updater,
			new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: reporter),
			enc.Solution);

		await manager.ProcessFileChanges(Task.FromResult(ImmutableHashSet.Create("/work/MainPage.xaml")), ct);
		await manager.ProcessFileChanges(Task.FromResult(ImmutableHashSet.Create("/work/MainPage.xaml")), ct);

		handler.Calls.Should().HaveCount(2);
		foreach (var (result, update) in handler.Calls)
		{
			result.Should().Be(HotReloadOperationResult.Success, "both cycles must emit against the pinned baseline");
			update.Deltas.Should().HaveCount(1);
			update.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
			update.PinnedReferences.Should().ContainSingle("the persisting re-bind is re-pinned on every emit");
		}

		reporter.Outputs
			.Count(o => o.Contains("ConflictLib", StringComparison.Ordinal) && o.Contains("rebuild", StringComparison.Ordinal))
			.Should().Be(1, "an unchanged pin set must not repeat the Output summary on every cycle");
	}

	private sealed class DelegateChangesDetector : IChangesDetector
	{
		public ValueTask<ChangeSet> DiscoverChangesAsync(Solution solution, ImmutableHashSet<string> files, CancellationToken ct)
			=> ValueTask.FromResult(ChangeSet.IgnoreAll([]));
	}

	private sealed class DelegateSolutionUpdater(Func<Solution, SolutionUpdateResult> update) : ISolutionUpdater
	{
		public ValueTask<SolutionUpdateResult> UpdateAsync(Solution solution, ChangeSet changeSet, CancellationToken ct)
			=> ValueTask.FromResult(update(solution));
	}
}
