using System.Collections.Concurrent;
using System.Collections.Immutable;
using AwesomeAssertions;
using Uno.HotReload.IO;

namespace Uno.HotReload.Tests.IO;

[TestClass]
public class Given_WorkspaceGatedFileUpdater
{
	private static readonly TimeSpan _flushTimeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	[Description(
		"An update request received while the workspace is initializing must not reach the " +
		"inner updater (i.e. must not touch the disk) before the workspace is ready — an early " +
		"edit would be folded into the baseline and never emitted as a delta.")]
	public async Task When_RequestWhileInitializing_Then_QueuedAndAppliedOnReady()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);

		await Task.Delay(50);
		pending.IsCompleted.Should().BeFalse("the request must wait for the workspace");
		inner.Requests.Should().BeEmpty("nothing may be applied before the baseline is captured");

		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);

		var response = await pending.WaitAsync(_flushTimeout);
		response.GlobalError.Should().BeNull();
		inner.Requests.Should().ContainSingle();
	}

	[TestMethod]
	[Description(
		"A request received before any configuration (mode not known yet) must be queued too: " +
		"the bare updater writes to disk from the constructor otherwise.")]
	public async Task When_RequestBeforeConfiguration_Then_QueuedUntilModeIsKnown()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);

		sut.State.Should().Be(HotReloadWorkspaceState.NotConfigured);
		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);
		inner.Requests.Should().BeEmpty();

		// First configuration resolves to IDE-driven hot reload (e.g. Visual Studio): flush as-is.
		sut.ReportWorkspaceState(HotReloadWorkspaceState.NoWorkspace);

		(await pending.WaitAsync(_flushTimeout)).GlobalError.Should().BeNull();
		inner.Requests.Should().ContainSingle();
	}

	[TestMethod]
	[Description(
		"IDE-driven mode (no workspace, e.g. Visual Studio) must remain strictly pass-through: " +
		"hot reload is active as soon as the app starts there.")]
	public async Task When_NoWorkspaceMode_Then_PassThrough()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.NoWorkspace);

		var response = await sut.UpdateAsync(Request("a.xaml"), CancellationToken.None).WaitAsync(_flushTimeout);

		response.GlobalError.Should().BeNull();
		inner.Requests.Should().ContainSingle();
	}

	[TestMethod]
	[Description("Queued requests must be flushed in arrival order (FIFO).")]
	public async Task When_MultipleRequestsQueued_Then_FlushedInArrivalOrder()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = new[]
		{
			sut.UpdateAsync(Request("first.xaml"), CancellationToken.None),
			sut.UpdateAsync(Request("second.xaml"), CancellationToken.None),
			sut.UpdateAsync(Request("third.xaml"), CancellationToken.None),
		};

		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);
		await Task.WhenAll(pending).WaitAsync(_flushTimeout);

		inner.Requests.Select(req => req.Edits.Single().FilePath)
			.Should().ContainInOrder("first.xaml", "second.xaml", "third.xaml");
	}

	[TestMethod]
	[Description(
		"When the workspace failed to initialize, queued and subsequent requests must fail fast " +
		"with an explicit error — never be applied, never wait forever.")]
	public async Task When_WorkspaceFails_Then_QueuedAndSubsequentRequestsAreRejected()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var queued = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Failed);

		var queuedResponse = await queued.WaitAsync(_flushTimeout);
		queuedResponse.GlobalError.Should().Contain("failed to initialize");
		queuedResponse.Results.Should().OnlyContain(result => result.Result == FileUpdateResult.NotAvailable);

		var lateResponse = await sut.UpdateAsync(Request("b.xaml"), CancellationToken.None).WaitAsync(_flushTimeout);
		lateResponse.GlobalError.Should().Contain("failed to initialize");

		inner.Requests.Should().BeEmpty("a failed workspace must never let an edit through");
	}

	[TestMethod]
	[Description(
		"Terminal states are sticky: a Ready reported after a failure must not resurrect the " +
		"gate on this connection (recovery is a new connection with a fresh processor).")]
	public async Task When_ReadyAfterFailed_Then_StillRejected()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Failed);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);

		sut.State.Should().Be(HotReloadWorkspaceState.Failed);
		var response = await sut.UpdateAsync(Request("a.xaml"), CancellationToken.None).WaitAsync(_flushTimeout);
		response.GlobalError.Should().NotBeNull();
		inner.Requests.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"A queued request must expire after the hard queue timeout with an explicit error, and a " +
		"late workspace Ready must NOT apply the expired edit: the requester gave up long ago and " +
		"a late disk write would mutate sources with nobody listening.")]
	public async Task When_QueueTimeoutExpires_Then_RequestFailsAndIsNeverAppliedLate()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner, queueTimeout: TimeSpan.FromMilliseconds(100));
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);

		var response = await pending.WaitAsync(_flushTimeout);
		response.GlobalError.Should().Contain("was not ready within");
		response.Results.Should().OnlyContain(result => result.Result == FileUpdateResult.NotAvailable);

		// The workspace becoming ready afterwards must not resurrect the expired request.
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);
		await Task.Delay(100);
		inner.Requests.Should().BeEmpty("an expired request must never be applied late");
	}

	[TestMethod]
	[Description(
		"An expired request sitting in the queue must not block later, still-live requests from " +
		"being flushed when the workspace becomes ready.")]
	public async Task When_SomeRequestsExpired_Then_RemainingOnesStillFlush()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner, queueTimeout: TimeSpan.FromMilliseconds(100));
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var expired = sut.UpdateAsync(Request("expired.xaml"), CancellationToken.None);
		(await expired.WaitAsync(_flushTimeout)).GlobalError.Should().NotBeNull();

		// This one is enqueued after the first expired; its own TTL has not elapsed yet.
		var live = sut.UpdateAsync(Request("live.xaml"), CancellationToken.None);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);

		(await live.WaitAsync(_flushTimeout)).GlobalError.Should().BeNull();
		inner.Requests.Should().ContainSingle(because: "only the live request may be applied")
			.Which.Edits.Single().FilePath.Should().Be("live.xaml");
	}

	[TestMethod]
	[Description(
		"Disposing the connection with a non-empty queue must fail all pending requests cleanly " +
		"(no orphaned awaiter), and reject any subsequent request.")]
	public async Task When_DisposedWithQueuedRequests_Then_AllPendingAreRejected()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Disposed);

		var response = await pending.WaitAsync(_flushTimeout);
		response.GlobalError.Should().Contain("closed");

		var late = await sut.UpdateAsync(Request("b.xaml"), CancellationToken.None).WaitAsync(_flushTimeout);
		late.GlobalError.Should().Contain("closed");
		inner.Requests.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"Re-entrant configuration: hot reload first resolved as IDE-driven, then a later " +
		"configuration enables the workspace — the gate must resume queueing during the " +
		"initialization without replaying requests already passed through.")]
	public async Task When_NoWorkspaceThenInitializing_Then_GatingResumes()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);

		sut.ReportWorkspaceState(HotReloadWorkspaceState.NoWorkspace);
		await sut.UpdateAsync(Request("direct.xaml"), CancellationToken.None).WaitAsync(_flushTimeout);
		inner.Requests.Should().ContainSingle();

		// A later ConfigureServer enables metadata updates: workspace init starts.
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var gated = sut.UpdateAsync(Request("gated.xaml"), CancellationToken.None);
		await Task.Delay(50);
		gated.IsCompleted.Should().BeFalse("gating must resume while the workspace initializes");

		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);
		(await gated.WaitAsync(_flushTimeout)).GlobalError.Should().BeNull();
		inner.Requests.Should().HaveCount(2, "the direct request must not be replayed");
	}

	[TestMethod]
	[Description(
		"Replacing the inner updater (the processor refines the editor on configuration) must " +
		"preserve the gate: lifecycle state and queued requests survive, and the flush goes " +
		"through the NEW inner.")]
	public async Task When_InnerReplacedWhileQueued_Then_FlushUsesNewInner()
	{
		var initialInner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(initialInner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);

		var replacementInner = new RecordingUpdater();
		sut.Inner = replacementInner;
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);

		await pending.WaitAsync(_flushTimeout);
		initialInner.Requests.Should().BeEmpty();
		replacementInner.Requests.Should().ContainSingle();
	}

	[TestMethod]
	[Description(
		"A request cancelled (e.g. connection tear-down) while queued must complete with an " +
		"error and never be applied by a later flush.")]
	public async Task When_RequestCancelledWhileQueued_Then_RejectedAndNeverApplied()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		using var cts = new CancellationTokenSource();
		var pending = sut.UpdateAsync(Request("a.xaml"), cts.Token);
		cts.Cancel();

		var response = await pending.WaitAsync(_flushTimeout);
		response.GlobalError.Should().Contain("cancelled");

		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);
		await Task.Delay(100);
		inner.Requests.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"Diagnostic events must be raised for queueing and flushing so the processor can relay " +
		"them as telemetry.")]
	public async Task When_QueuedAndFlushed_Then_GateEventsAreRaised()
	{
		var inner = new RecordingUpdater();
		var events = new ConcurrentQueue<WorkspaceGateEvent>();
		var sut = new WorkspaceGatedFileUpdater(inner, onEvent: events.Enqueue);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var pending = sut.UpdateAsync(Request("a.xaml"), CancellationToken.None);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);
		await pending.WaitAsync(_flushTimeout);

		events.Select(evt => evt.Kind).Should().ContainInOrder("queued", "flushed");
	}

	[TestMethod]
	[Description(
		"A throwing diagnostic callback (reporter/onEvent) during the flush must never orphan the " +
		"request's awaiter nor wedge the drain loop: the request still completes and a subsequent " +
		"request must still flow through (the gate must not stay permanently 'draining').")]
	public async Task When_OnEventThrowsDuringFlush_Then_RequestCompletesAndGateNotWedged()
	{
		var inner = new RecordingUpdater();
		var sut = new WorkspaceGatedFileUpdater(inner, onEvent: evt =>
		{
			if (evt.Kind == "flushed")
			{
				throw new InvalidOperationException("telemetry sink blew up");
			}
		});
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		var first = sut.UpdateAsync(Request("first.xaml"), CancellationToken.None);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Ready);

		// Despite the callback throwing, the awaiter is resolved (completed before the callback runs).
		(await first.WaitAsync(_flushTimeout)).GlobalError.Should().BeNull();

		// The drain loop must have reset its state: a later request still gets applied.
		var second = sut.UpdateAsync(Request("second.xaml"), CancellationToken.None);
		(await second.WaitAsync(_flushTimeout)).GlobalError.Should().BeNull();
		inner.Requests.Should().HaveCount(2);
	}

	[TestMethod]
	[Description(
		"Expired/cancelled entries must be compacted out of the queue rather than lingering: a " +
		"workspace stuck initializing must not accumulate dead entries. Observed via the queue " +
		"length reported on the next enqueue.")]
	public async Task When_EntryExpires_Then_ItIsRemovedFromTheQueue()
	{
		var inner = new RecordingUpdater();
		var events = new ConcurrentQueue<WorkspaceGateEvent>();
		var sut = new WorkspaceGatedFileUpdater(inner, queueTimeout: TimeSpan.FromMilliseconds(100), onEvent: events.Enqueue);
		sut.ReportWorkspaceState(HotReloadWorkspaceState.Initializing);

		// First request expires while the workspace stays 'Initializing'.
		var expired = sut.UpdateAsync(Request("expired.xaml"), CancellationToken.None);
		(await expired.WaitAsync(_flushTimeout)).GlobalError.Should().NotBeNull();

		// Let the timeout path's compaction (which runs after the awaiter is resolved) settle.
		await Task.Delay(50);

		// Second enqueue must see a queue of 1 (the expired entry was compacted out, not still present).
		_ = sut.UpdateAsync(Request("next.xaml"), CancellationToken.None);

		var queuedLengths = events.Where(evt => evt.Kind == "queued").Select(evt => evt.QueueLength).ToArray();
		queuedLengths.Should().Equal([1, 1], "the expired entry must not inflate the queue length");
	}

	private static TestUpdateRequest Request(string filePath)
		=> new([new FileEdit(filePath, OldText: null, NewText: "<new content />")]);

	private sealed record TestUpdateRequest(ImmutableArray<FileEdit> Edits) : IUpdateFileRequest
	{
		public string RequestId { get; } = Guid.NewGuid().ToString();
		public bool? ForceSaveOnDisk => null;
		public bool IsForceHotReloadDisabled => false;
		public TimeSpan? ForceHotReloadDelay => null;
		public int? ForceHotReloadAttempts => null;
	}

	private sealed class RecordingUpdater : IFileUpdater
	{
		private readonly ConcurrentQueue<IUpdateFileRequest> _requests = new();

		public IReadOnlyCollection<IUpdateFileRequest> Requests => _requests;

		public Task<IUpdateFileResponse> UpdateAsync(IUpdateFileRequest request, CancellationToken ct)
		{
			_requests.Enqueue(request);
			return Task.FromResult<IUpdateFileResponse>(new SuccessResponse(
				request.RequestId,
				null,
				[.. request.Edits.Select(edit => new FileEditResult(edit.FilePath, FileUpdateResult.Success, null))]));
		}

		private sealed record SuccessResponse(
			string RequestId,
			string? GlobalError,
			ImmutableArray<FileEditResult> Results,
			long? HotReloadCorrelationId = null) : IUpdateFileResponse;
	}
}
