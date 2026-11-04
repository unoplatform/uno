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

	[TestMethod]
	[Description(
		"An update the dev-server applies from its own workspace sends exactly one outcome event, named like " +
		"the outcome the IDE reports, with Source=DevServer and the operation's duration.")]
	public async Task When_UpdateAppliedByDevServer_Then_OneCompletedEventIsSent()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));
		var telemetry = new RecordingTelemetry();
		using var outcomes = HotReloadOutcomeTelemetry.Attach(harness.Tracker, telemetry);

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
		using var outcomes = HotReloadOutcomeTelemetry.Attach(harness.Tracker, telemetry);

		var batch = ImmutableHashSet.Create("/work/App.csproj");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be("notify-failed");
		evt.Properties["Result"].Should().Be("Failed");
	}

	[TestMethod]
	[Description(
		"The processor disposes the subscription with its workspace (shutdown, workspace-cap eviction): a pass " +
		"completing after that is not an outcome the user saw, and sends nothing.")]
	public async Task When_Detached_Then_NoEventIsSent()
	{
		using var harness = new HotReloadManagerHarness(
			_ => Task.FromResult<(ImmutableArray<Update>, ImmutableArray<Diagnostic>)>(
				([HotReloadManagerHarness.CreateEmptyUpdate()], [])));
		var telemetry = new RecordingTelemetry();
		HotReloadOutcomeTelemetry.Attach(harness.Tracker, telemetry).Dispose();

		var batch = ImmutableHashSet.Create("/work/Model.cs");
		await harness.Manager.ProcessFileChanges(Task.FromResult(batch), CancellationToken.None).WaitAsync(_testTimeout);

		harness.Tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success);
		telemetry.Events.Should().BeEmpty();
	}

	[TestMethod]
	[Description(
		"An IDE notification (ServerHotReloadProcessor.Notify) completes an operation it started without files; " +
		"the IDE reports that outcome itself (Source=IDE), so the dev-server must not report it a second time.")]
	public async Task When_OperationStartedByIdeNotification_Then_NoEventIsSent()
	{
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());
		var telemetry = new RecordingTelemetry();
		using var outcomes = HotReloadOutcomeTelemetry.Attach(tracker, telemetry);

		await (await tracker.StartOrContinueHotReload()).Complete(HotReloadOperationResult.Success);

		tracker.Last!.Result.Should().Be(HotReloadOperationResult.Success);
		telemetry.Events.Should().BeEmpty();
	}

	[TestMethod]
	[DataRow(HotReloadOperationResult.Success, "notify-completed", "Completed")]
	[DataRow(HotReloadOperationResult.NoChanges, "notify-no-changes", "NoChanges")]
	[DataRow(HotReloadOperationResult.Failed, "notify-failed", "Failed")]
	[DataRow(HotReloadOperationResult.RudeEdit, "notify-rude-edit", "RudeEdit")]
	[DataRow(HotReloadOperationResult.InternalError, "notify-failed", "Failed")]
	public async Task When_OperationCompletes_Then_ItsOutcomeEventIsSent(HotReloadOperationResult result, string expectedName, string expectedEvent)
	{
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());
		var telemetry = new RecordingTelemetry();
		using var outcomes = HotReloadOutcomeTelemetry.Attach(tracker, telemetry);

		var operation = await tracker.StartHotReload(ImmutableHashSet.Create("/work/Page.xaml"));
		await operation.Complete(result, result is HotReloadOperationResult.InternalError ? new InvalidOperationException("internal") : null);

		var evt = telemetry.Events.Should().ContainSingle().Which;
		evt.Name.Should().Be(expectedName);
		evt.Properties["Event"].Should().Be(expectedEvent);
		evt.Properties["Result"].Should().Be(result.ToString());
	}

	[TestMethod]
	[Description("An aborted operation (superseded by a newer one, or no result in time) has no outcome to report.")]
	public async Task When_OperationAborted_Then_NoEventIsSent()
	{
		var tracker = new HotReloadTracker((_, _) => ValueTask.CompletedTask, reporter: new RecordingReporter());
		var telemetry = new RecordingTelemetry();
		using var outcomes = HotReloadOutcomeTelemetry.Attach(tracker, telemetry);

		var superseded = await tracker.StartHotReload(ImmutableHashSet.Create("/work/A.cs"));
		var newer = await tracker.StartHotReload(ImmutableHashSet.Create("/work/B.cs"));
		await newer.Complete(HotReloadOperationResult.Success);

		superseded.Result.Should().Be(HotReloadOperationResult.Aborted);
		telemetry.Events.Should().ContainSingle("only the newer operation reached an outcome")
			.Which.Name.Should().Be("notify-completed");
	}
}
