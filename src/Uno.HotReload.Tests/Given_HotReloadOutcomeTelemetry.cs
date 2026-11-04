using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Diffing;
using Uno.HotReload.Tests.TestUtils;
using Uno.HotReload.Tracking;
using Uno.UI.RemoteControl.Host.HotReload;

namespace Uno.HotReload.Tests;

/// <summary>
/// The dev-server's outcome telemetry (<see cref="HotReloadOutcomeTelemetry"/>, linked from
/// Uno.UI.RemoteControl.Server.Processors) for the hot reloads it applies from its own workspace.
/// </summary>
[TestClass]
public class Given_HotReloadOutcomeTelemetry
{
	private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(30);

	private static Task<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)> OneUpdate(int _)
		=> Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([HotReloadManagerHarness.CreateEmptyUpdate()], []));

	private static HotReloadTracker CreateTracker()
		=> new((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());

	[TestMethod]
	[Description(
		"An update the dev-server applies from its own workspace sends exactly one outcome event, named like " +
		"the outcome the IDE reports, with Source=DevServer and the operation's duration.")]
	public async Task When_UpdateAppliedByDevServer_Then_OneCompletedEventIsSent()
	{
		using var harness = new HotReloadManagerHarness(OneUpdate);
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(harness.Tracker, telemetry).Attach(CancellationToken.None);

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be("notify-completed");
		evt.Properties.Should().Equal(new Dictionary<string, string>
		{
			["Event"] = "Completed",
			["Source"] = "DevServer",
			["Result"] = "Success",
		});
		evt.Measurements["FileCount"].Should().Be(1);
		evt.Measurements["DurationMs"].Should().BeGreaterThanOrEqualTo(0);
	}

	[TestMethod]
	[Description("A pass the dev-server cannot apply (here, an updater error) sends one notify-failed.")]
	public async Task When_UpdateBlockedOnDevServer_Then_OneFailedEventIsSent()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(([], [])),
			onUpdate: solution => new SolutionUpdateResult(
				solution.WithProjectAssemblyName(solution.ProjectIds[0], "Rebound"),
				ChangeSet.IgnoreAll([]),
				[HotReloadManagerHarness.CreateErrorDiagnostic()]));
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(harness.Tracker, telemetry).Attach(CancellationToken.None);

		var batch = ImmutableHashSet.Create("/work/App.csproj");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be("notify-failed");
		evt.Properties["Result"].Should().Be("Failed");
	}

	[TestMethod]
	[Description(
		"A batch of files outside the solution (a log, .DS_Store) comes out as NoChanges with every file ignored: " +
		"it is not a hot reload, and sends nothing.")]
	public async Task When_EveryFileIsOutsideTheSolution_Then_NoEventIsSent()
	{
		using var harness = new HotReloadManagerHarness(
			OneUpdate,
			onUpdate: solution => new SolutionUpdateResult(solution, ChangeSet.IgnoreAll(["/work/.DS_Store"])));
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(harness.Tracker, telemetry).Attach(CancellationToken.None);

		var batch = ImmutableHashSet.Create("/work/.DS_Store");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.NoChanges);
		telemetry.Events.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"A pass still running when its workspace is torn down (shutdown, hot reload disabled) ends after the " +
		"workspace token is cancelled: it is not an outcome the user saw, and sends nothing.")]
	public async Task When_PassEndsAfterTheWorkspaceIsTornDown_Then_NoEventIsSent()
	{
		var emitEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseEmit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var harness = new HotReloadManagerHarness(async _ =>
		{
			emitEntered.SetResult();
			await releaseEmit.Task;
			return ([HotReloadManagerHarness.CreateEmptyUpdate()], []);
		});
		var telemetry = new RecordingTelemetry();
		using var workspace = new CancellationTokenSource();
		new HotReloadOutcomeTelemetry(harness.Tracker, telemetry).Attach(workspace.Token);

		var pass = harness.Manager.ProcessFileChanges(Task.FromResult(ImmutableHashSet.Create("/work/Model.cs")), CancellationToken.None);
		await emitEntered.Task.WaitAsync(_testTimeout);
		await workspace.CancelAsync();
		releaseEmit.SetResult();
		await pass.WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success);
		telemetry.Events.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"An IDE notification can continue the dev-server's own operation, files included, and complete it with the " +
		"IDE's result (ServerHotReloadProcessor.Notify). The IDE reports that outcome (Source=IDE): the dev-server " +
		"must not report it a second time.")]
	public async Task When_IdeNotificationTakesOverAnOperation_Then_NoEventIsSent()
	{
		var tracker = CreateTracker();
		var telemetry = new RecordingTelemetry();
		var outcomes = new HotReloadOutcomeTelemetry(tracker, telemetry);
		outcomes.Attach(CancellationToken.None);

		await tracker.StartHotReload(ImmutableHashSet.Create("/work/Model.cs"));
		var continued = await tracker.StartOrContinueHotReload();
		outcomes.SkipReportedByIde(continued);
		await continued.Complete(HotReloadOperationResult.Success);

		continued.ConsideredFilePaths.Should().ContainSingle("the IDE took over the dev-server's operation, files included");
		telemetry.Events.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"An operation with no result in time is aborted by its timeout: the user saved and nothing happened, " +
		"so it is sent as notify-failed with Result=Timeout.")]
	public async Task When_OperationTimesOut_Then_FailedWithTimeoutIsSent()
	{
		var tracker = CreateTracker();
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(tracker, telemetry).Attach(CancellationToken.None);

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Model.cs"));
		await operation.OnTimeoutAsync();

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be("notify-failed");
		evt.Properties["Event"].Should().Be("Failed");
		evt.Properties["Result"].Should().Be("Timeout");
	}

	[TestMethod]
	[DataRow(HotReloadOperationResult.Success, "notify-completed", "Completed")]
	[DataRow(HotReloadOperationResult.NoChanges, "notify-no-changes", "NoChanges")]
	[DataRow(HotReloadOperationResult.Failed, "notify-failed", "Failed")]
	[DataRow(HotReloadOperationResult.RudeEdit, "notify-rude-edit", "RudeEdit")]
	[DataRow(HotReloadOperationResult.InternalError, "notify-failed", "Failed")]
	public async Task When_OperationCompletes_Then_ItsOutcomeEventIsSent(HotReloadOperationResult result, string expectedName, string expectedEvent)
	{
		var tracker = CreateTracker();
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(tracker, telemetry).Attach(CancellationToken.None);

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Page.xaml"));
		await operation.Complete(result, result is HotReloadOperationResult.InternalError ? new InvalidOperationException("internal") : null);

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be(expectedName);
		evt.Properties["Event"].Should().Be(expectedEvent);
		evt.Properties["Result"].Should().Be(result.ToString());
	}

	[TestMethod]
	[Description("An operation aborted because a newer one completed has no outcome of its own: the newer one carries it.")]
	public async Task When_OperationIsSuperseded_Then_OnlyTheNewerOneIsSent()
	{
		var tracker = CreateTracker();
		var telemetry = new RecordingTelemetry();
		new HotReloadOutcomeTelemetry(tracker, telemetry).Attach(CancellationToken.None);

		var superseded = await tracker.StartHotReload(ImmutableHashSet.Create("/work/A.cs"));
		var newer = await tracker.StartHotReload(ImmutableHashSet.Create("/work/B.cs"));
		await newer.Complete(HotReloadOperationResult.Success);

		superseded.Result.Should().Be(HotReloadOperationResult.Aborted);
		telemetry.Events.Should().ContainSingle().Which.Name.Should().Be("notify-completed");
	}
}
