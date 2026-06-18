using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Tests;

[TestClass]
public class Given_HotReloadManager
{
	private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(30);

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
		harness.Handler.Updates.Should().HaveCount(1);
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
		"Spec 045 §3: OperationCanceledException from the handler is not a hot-reload failure — it propagates " +
		"to the manager-internal catch and must not complete the operation as Failed.")]
	public async Task When_HandlerThrowsOperationCanceled_Then_NotCompletedAsFailed()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));
		harness.Handler.ThrowAfterRecording = new OperationCanceledException();

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().NotBe(
			HotReloadOperationResult.Failed,
			"cancellation is not a handler failure; it propagates to the manager-internal catch");
	}

	private sealed record MarkerSolutionUpdateResult(Solution Solution, ChangeSet IgnoredChanges, string Marker)
		: SolutionUpdateResult(Solution, IgnoredChanges, []);
}
