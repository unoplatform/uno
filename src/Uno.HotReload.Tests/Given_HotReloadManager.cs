using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
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
		"Merge fast-path guard: a batch arriving while the operation exists but has not been " +
		"processed yet merges into it and is processed as that same single operation.")]
	public async Task When_BatchMergedIntoPendingOperation_Then_SingleOperationTracksIt()
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
}
