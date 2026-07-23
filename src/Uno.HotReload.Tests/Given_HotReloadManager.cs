using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

[TestClass]
public class Given_HotReloadManager
{
	private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(30);

	// A single core reference is enough for the trivial types these tests compile; used to build
	// a project that genuinely compiles clean (so the scoped audit finds no errors in it).
	private static readonly MetadataReference _coreLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

	[TestMethod]
	[Description(
		"A batch whose files intersect an in-flight operation merges into it, but its compile " +
		"pass runs after the operation was completed by the first pass. The late pass's outcome " +
		"(here a rude edit with diagnostics) must surface on a tracked operation — not be " +
		"silently dropped because the merged-into operation already completed.")]
	public async Task When_BatchMergedIntoCompletedOperation_Then_ItsResultIsTracked()
	{
		var firstEmitEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirstEmit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		using var harness = new HotReloadManagerHarness(async call =>
		{
			if (call == 1)
			{
				firstEmitEntered.SetResult();
				await releaseFirstEmit.Task;
				return ([HotReloadManagerHarness.CreateEmptyUpdate()], []);
			}

			// Second pass: the (corrupted) content fails to compile.
			return ([], [HotReloadManagerHarness.CreateRudeEditDiagnostic()]);
		});

		var firstBatch = ImmutableHashSet.Create("/work/Model.cs", "/work/Page.xaml");
		var secondBatch = ImmutableHashSet.Create("/work/Page.xaml"); // intersects → merges

		// Pass 1 enters the emitter (holding the solution-update gate) and blocks there.
		var firstProcess = harness.Manager.ProcessFileChanges(Task.FromResult(firstBatch), CancellationToken.None);
		await firstEmitEntered.Task.WaitAsync(_testTimeout);

		// Batch 2 arrives while the operation is in flight, then queues on the gate.
		var secondProcess = harness.Manager.ProcessFileChanges(Task.FromResult(secondBatch), CancellationToken.None);

		// Pass 1 completes the operation with Success; pass 2 then runs and fails.
		releaseFirstEmit.SetResult();
		await firstProcess.WaitAsync(_testTimeout);
		await secondProcess.WaitAsync(_testTimeout);

		harness.Tracker.Current.Should().BeNull();
		var last = harness.Tracker.Last;
		last.Should().NotBeNull();
		last!.Result.Should().Be(
			HotReloadOperationResult.RudeEdit,
			"the most recently processed batch failed to compile, and that outcome must be visible to status consumers");
		last.Diagnostics.Should().NotBeNull("the failing pass produced diagnostics that must not be dropped");
		last.Diagnostics!.Value.Should().Contain(d => d.Id == "ENC0033");
	}

	[TestMethod]
	[Description(
		"Happy path guard: a single batch flows through detector → updater → emitter and " +
		"completes its operation with Success, shipping the deltas to the handler.")]
	public async Task When_SingleBatch_Then_OperationCompletesSuccess()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Current.Should().BeNull();
		harness.Tracker.Last.Should().NotBeNull();
		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success);
		harness.Handler.Calls.Should().ContainSingle()
			.Which.Result.Should().Be(HotReloadOperationResult.Success);
	}

	[TestMethod]
	[Description(
		"Merge fast-path guard: a batch arriving while an operation exists but has not been processed yet should be processed by that existing operation (i.e., the second caller should not start a new operation).")]
	public async Task When_BatchMergedIntoPendingOperation_Then_SecondCallerProcessesExistingOperation()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));

		// First call creates the operation, then suspends awaiting its (still pending) batch.
		var pendingBatch = new TaskCompletionSource<ImmutableHashSet<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstProcess = harness.Manager.ProcessFileChanges(pendingBatch.Task, CancellationToken.None);

		var pendingOperation = harness.Tracker.Current;
		pendingOperation.Should().NotBeNull("the manager starts/continues an operation before awaiting the batch");

		// Second call merges its files into that pending operation and processes it.
		var batch = ImmutableHashSet.Create("/work/Page.xaml");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last.Should().BeSameAs(pendingOperation, "an unprocessed pending operation absorbs intersecting batches");
		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success);

		// Unblock the first call so the test ends clean.
		pendingBatch.SetResult(batch);
		await firstProcess.WaitAsync(_testTimeout);
	}

	// ── Spec 045: invoke the handler on every terminal outcome ──

	[TestMethod]
	[Description(
		"Spec 045 §1: a cycle that mutates the solution but emits no delta (e.g. a references-only " +
		"PackageReference add) now invokes the handler with NoChanges, carrying the updater's result — " +
		"so a handler can stage delta-independent state on the no-delta cycle, not via a carry-forward.")]
	public async Task When_NoDeltaCycle_Then_HandlerInvokedWithNoChanges()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])));

		var batch = ImmutableHashSet.Create("/work/App.csproj");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.NoChanges);
		harness.Handler.Calls.Should().HaveCount(1, "the handler is invoked on every terminal outcome, not just Success");
		harness.Handler.Calls[0].Result.Should().Be(HotReloadOperationResult.NoChanges);
		harness.Handler.Calls[0].Update.Deltas.Should().BeEmpty("there is no delta on a NoChanges cycle");
		harness.Handler.Calls[0].Update.SolutionUpdate.Should().NotBeNull("the updater's result is visible to the handler on the no-delta cycle");
	}

	[TestMethod]
	[Description("Spec 045 §1: a successful delta cycle invokes the handler with Success and the deltas.")]
	public async Task When_DeltaCycle_Then_HandlerInvokedWithSuccessAndDeltas()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Handler.Calls.Should().HaveCount(1);
		harness.Handler.Calls[0].Result.Should().Be(HotReloadOperationResult.Success);
		harness.Handler.Calls[0].Update.Deltas.Should().HaveCount(1);
	}

	[TestMethod]
	[Description("Spec 045 §1: a rude edit invokes the handler with RudeEdit so handlers can observe a non-applied edit.")]
	public async Task When_RudeEdit_Then_HandlerInvokedWithRudeEdit()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([], [HotReloadManagerHarness.CreateRudeEditDiagnostic()])));

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.RudeEdit);
		harness.Handler.Calls.Should().ContainSingle();
		harness.Handler.Calls[0].Result.Should().Be(HotReloadOperationResult.RudeEdit);
		harness.Handler.Calls[0].Update.Diagnostics.Should().Contain(d => d.Id == "ENC0033",
			"handler must receive the rude-edit diagnostics so consumers can surface them");
	}

	[TestMethod]
	[Description(
		"Spec 045 §1+§2: when the updater reports an error diagnostic the handler is invoked with Failed, " +
		"and the (rebound) solution is committed before the early exit so later cycles don't restart from " +
		"the stale solution. The error path short-circuits before EmitSolutionUpdateAsync.")]
	public async Task When_UpdaterReportsError_Then_HandlerInvokedWithFailed_AndSolutionCommitted()
	{
		var emitCalled = false;
		using var harness = new HotReloadManagerHarness(
			_ =>
			{
				emitCalled = true;
				return Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], []));
			},
			onUpdate: solution => new SolutionUpdateResult(
				solution.WithProjectAssemblyName(solution.ProjectIds[0], "Rebound"),
				ChangeSet.IgnoreAll([]),
				[HotReloadManagerHarness.CreateErrorDiagnostic()]));

		var original = harness.Manager.CurrentSolution;
		var batch = ImmutableHashSet.Create("/work/App.csproj");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Failed);
		harness.Handler.Calls.Should().ContainSingle(c => c.Result == HotReloadOperationResult.Failed);
		emitCalled.Should().BeFalse("the updater-error path short-circuits before EmitSolutionUpdateAsync");
		harness.Manager.CurrentSolution.Should().NotBeSameAs(original, "the rebound solution is committed even on the early Failed exit (§2)");
		harness.Handler.Calls[0].Update.Diagnostics.Should().Contain(d => d.Id == "TEST0001",
			"the updater error diagnostic must be forwarded to the handler so consumers see the failure reason");
	}

	[TestMethod]
	[Description(
		"Spec 045 motivating scenario: a references-only change returns a SolutionUpdateResult subtype " +
		"(carrying e.g. resolved packages); the handler receives it via HotReloadUpdate.SolutionUpdate on " +
		"the no-delta cycle and can downcast — removing the consumer's carry-forward workaround.")]
	public async Task When_ReferencesOnlyChange_Then_HandlerSeesSubtypeOnNoChanges()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			onUpdate: solution => new MarkerSolutionUpdateResult(
				solution.WithProjectAssemblyName(solution.ProjectIds[0], "WithPackage"),
				ChangeSet.IgnoreAll([]),
				"resolved-package"));

		var batch = ImmutableHashSet.Create("/work/App.csproj");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Handler.Calls.Should().ContainSingle();
		harness.Handler.Calls[0].Result.Should().Be(HotReloadOperationResult.NoChanges);
		harness.Handler.Calls[0].Update.SolutionUpdate.Should().BeOfType<MarkerSolutionUpdateResult>()
			.Which.Marker.Should().Be("resolved-package");
	}

	[TestMethod]
	[Description(
		"Spec 045 §3: a handler exception completes the operation as Failed carrying the exception, even on " +
		"a would-be Success cycle — the reload did not take effect on the consumer side.")]
	public async Task When_HandlerThrows_Then_OperationCompletesFailed()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));
		harness.Handler.ThrowAfterRecording = new InvalidOperationException("staging failed");

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Failed, "a handler failure fails the operation (§3)");
	}

	[TestMethod]
	[Description(
		"Spec 045 §3: OperationCanceledException from the handler is not a hot-reload failure — it re-throws " +
		"past the per-handler catch into the ProcessFileChanges manager-internal catch, which completes the " +
		"operation as InternalError (carrying the exception), not Failed.")]
	public async Task When_HandlerThrowsOperationCanceled_Then_CompletedAsInternalError()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));
		harness.Handler.ThrowAfterRecording = new OperationCanceledException();

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(
			HotReloadOperationResult.InternalError,
			"a handler cancellation is not a hot-reload failure; it re-throws into the manager-internal catch which records InternalError");
	}

	[TestMethod]
	[Description(
		"Spec 045 idempotency contract (IHotReloadHandler.OnHotReloadAsync remarks): a single logical edit can " +
		"drive the handler more than once. A host that re-runs a no-delta cycle (the no-changes auto-retry path, " +
		"which re-enters ProcessSolutionChanged) re-invokes the handler with the same NoChanges outcome each time, " +
		"so delta-independent side-effects must be idempotent. Re-processing the same no-delta batch is the " +
		"observable proxy for that re-run: the handler is invoked once per cycle, never coalesced.")]
	public async Task When_NoDeltaCycleReprocessed_Then_HandlerReinvokedPerCycle()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])));

		var batch = ImmutableHashSet.Create("/work/App.csproj");

		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Handler.Calls.Should().HaveCount(2, "each cycle re-invokes the handler — side-effects must be idempotent across re-runs of the same logical change");
		harness.Handler.Calls.Should().OnlyContain(c => c.Result == HotReloadOperationResult.NoChanges);
	}

	[TestMethod]
	[Description(
		"Spec 045 §1 + spec 054 R1/R2: an emitted cycle that produces no metadata updates and no rude edits, but " +
		"whose own change-set project does not compile, completes as Failed (not NoChanges). The manager audits the " +
		"committed solution's compilation via GetCompilationErrors, scoped to the changed file's project; the handler " +
		"is invoked with Failed and the output line names the audited project and edited file.")]
	public async Task When_CompilationErrors_Then_HandlerInvokedWithFailed()
	{
		using var harness = new HotReloadManagerHarness(
			// No metadata updates and no diagnostics: forces the updates-empty / no-rude-edit branch
			// that probes the committed solution's compilation for blocking errors.
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			// Mutate the solution (so it isn't the NoChanges fast-path) by adding a document with a
			// reference-independent syntax error (CS1513 '}' expected). Its file path matches the pass's
			// change-set, so the file resolves to this project and the scoped audit judges it.
			onUpdate: solution =>
			{
				var projectId = solution.ProjectIds[0];
				var broken = solution.AddDocument(DocumentId.CreateNewId(projectId), "Broken.cs", "public class Broken {", filePath: "/work/Broken.cs");
				return new SolutionUpdateResult(broken, ChangeSet.IgnoreAll([]));
			},
			// GetCompilationErrors reads TryGetCompilation (the cached compilation), so the result
			// solution's compilation must be computed before the manager probes it.
			warmCompilation: true);

		var batch = ImmutableHashSet.Create("/work/Broken.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Failed, "a solution that does not compile blocks the reload");
		harness.Handler.Calls.Should().ContainSingle()
			.Which.Result.Should().Be(HotReloadOperationResult.Failed);
		harness.Reporter.Outputs.Should().Contain(
			o => o.Contains("Hot reload blocked") && o.Contains("TestProject") && o.Contains("Broken.cs"),
			"the blocked-compilation line names the audited project and the edited file (spec 054 R2)");
	}

	// ── Spec 054: scope the blocked-compilation audit to the pass's change-set ──

	[TestMethod]
	[Description(
		"Spec 054 R1: a pass whose change-set touches only a healthy project must not complete Failed because an " +
		"unrelated project carries compilation errors. The audit is scoped to the change-set's projects, so a foreign " +
		"project's errors never block the reload.")]
	public async Task When_ChangeSetTouchesHealthyProject_Then_ForeignErrorsDoNotBlock()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			onUpdate: solution =>
			{
				// Project A (the default test project): a healthy, compiling library owning the edited file.
				var projectA = solution.ProjectIds[0];
				solution = solution
					.WithProjectCompilationOptions(projectA, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
					.AddMetadataReference(projectA, _coreLib)
					.AddDocument(DocumentId.CreateNewId(projectA), "A.cs", "namespace App { public class A { } }", filePath: "/work/A.cs");

				// Project B: unrelated to the pass and does not compile (CS0103). It must never be audited.
				var projectB = ProjectId.CreateNewId();
				solution = solution
					.AddProject(ProjectInfo.Create(
						projectB, VersionStamp.Default, "LibraryB", "LibraryB", LanguageNames.CSharp,
						compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
					.AddMetadataReference(projectB, _coreLib)
					.AddDocument(DocumentId.CreateNewId(projectB), "B.cs", "namespace Lib { public class B { public void M() { Missing(); } } }", filePath: "/lib/B.cs");

				return new SolutionUpdateResult(solution, ChangeSet.IgnoreAll([]));
			},
			// Realize both projects' compilations so the manager's TryGetCompilation audit could observe B's error.
			warmCompilation: true);

		var batch = ImmutableHashSet.Create("/work/A.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(
			HotReloadOperationResult.NoChanges,
			"the pass touched only the healthy project A; project B's errors are foreign and must not block the reload");
		harness.Reporter.Outputs.Should().NotContain(
			o => o.Contains("Hot reload blocked"),
			"no blocked-compilation line is emitted when the change-set's own projects compile");
	}

	[TestMethod]
	[Description(
		"Spec 054 R1: an edit to an AdditionalDocument (e.g. XAML, a generator input) resolves to its owning project, " +
		"which is then audited. When that project does not compile, the reload is blocked and the line names the edit.")]
	public async Task When_ChangeSetIsAdditionalDocument_Then_OwningProjectIsAudited()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			onUpdate: solution =>
			{
				var projectId = solution.ProjectIds[0];
				// The project does not compile (broken source)...
				solution = solution.AddDocument(DocumentId.CreateNewId(projectId), "Broken.cs", "public class Broken {", filePath: "/work/Broken.cs");
				// ...and the pass's changed file is one of its XAML additional documents.
				solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(projectId), "View.xaml", "<Page />", filePath: "/work/View.xaml");
				return new SolutionUpdateResult(solution, ChangeSet.IgnoreAll([]));
			},
			warmCompilation: true);

		var batch = ImmutableHashSet.Create("/work/View.xaml");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(
			HotReloadOperationResult.Failed,
			"the edited XAML file is an additional document of a project that does not compile, so the pass is blocked");
		harness.Reporter.Outputs.Should().Contain(
			o => o.Contains("Hot reload blocked") && o.Contains("View.xaml"),
			"the blocked line names the edited additional document");
	}

	[TestMethod]
	[Description(
		"Spec 054 R1: when the change-set resolves to no project in the solution, the audit is skipped entirely and " +
		"the pass completes NoChanges — even if an unrelated project in the solution does not compile.")]
	public async Task When_ChangeSetIsOutsideSolution_Then_AuditSkipped()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			onUpdate: solution =>
			{
				var projectId = solution.ProjectIds[0];
				// The (only) project does not compile, but the pass's file does not belong to it.
				var broken = solution.AddDocument(DocumentId.CreateNewId(projectId), "Broken.cs", "public class Broken {", filePath: "/work/Broken.cs");
				return new SolutionUpdateResult(broken, ChangeSet.IgnoreAll([]));
			},
			warmCompilation: true);

		var batch = ImmutableHashSet.Create("/elsewhere/Unknown.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(
			HotReloadOperationResult.NoChanges,
			"the change-set resolves to no project, so the audit is skipped and the broken untouched project does not fail the pass");
		harness.Reporter.Outputs.Should().NotContain(o => o.Contains("Hot reload blocked"));
	}

	private sealed record MarkerSolutionUpdateResult(Solution Solution, ChangeSet IgnoredChanges, string Marker)
		: SolutionUpdateResult(Solution, IgnoredChanges, []);
}
