using System.Diagnostics;

namespace Uno.UI.RemoteControl.DevServer.Tests.Telemetry;

[TestClass]
public class ParentProcessObserverTelemetryTests : TelemetryTestBase
{
	[ClassInitialize]
	public static void ClassInitialize(TestContext context) => GlobalClassInitialize<ParentProcessObserverTelemetryTests>(context);

	[TestMethod]
	public async Task ChildServer_GracefulShutdown_When_ParentDies_TelemetryIsLogged()
	{
		var testFile = GetTestTelemetryFileName("parent_observer");
		var tempDir = Path.GetTempPath();
		var telemetryFile = Path.Combine(tempDir, testFile);

		await using var parentHelper = CreateTelemetryHelperWithExactPath(Path.Combine(tempDir, GetTestTelemetryFileName("parent")));
		await using var childHelper = CreateTelemetryHelperWithExactPath(telemetryFile);

		// Start parent server
		var parentStarted = await parentHelper.StartAsync(CT);
		parentStarted.Should().BeTrue();
		parentHelper.AssertRunning();

		// Start child server with --parentPid
		var parentPid = parentHelper.GetProcessId();
		var childStarted = await childHelper.StartAsync(CT, extraArgs: $"--ppid {parentPid}");
		childStarted.Should().BeTrue();
		childHelper.AssertRunning();

		// Kill parent
		await parentHelper.StopAsync(CT);

		// Wait for child to shutdown (max 15s)
		var sw = Stopwatch.StartNew();
		while (childHelper.IsRunning && sw.Elapsed < TimeSpan.FromSeconds(15))
		{
			await Task.Delay(250, CT);
		}
		childHelper.IsRunning.Should().BeFalse("child should shutdown after parent dies");
		childHelper.ExitCode.Should().NotBe(4, "the graceful shutdown completed, so the forced exit should not have run");

		// Read telemetry file
		File.Exists(telemetryFile).Should().BeTrue("telemetry file should exist");
		var content = await File.ReadAllTextAsync(telemetryFile, CT);
		var events = ParseTelemetryEvents(content);
		WriteEventsList(events);
		AssertHasEvent(events, "uno/dev-server/parent-process-lost");
		AssertNoEvent(events, "uno/dev-server/parent-process-lost-forced-exit");
	}

	[TestMethod]
	public async Task ChildServer_ForcedExit_When_ParentDies_And_GracefulShutdownHangs()
	{
		var fixtureDll = Path.Combine(
			Path.GetDirectoryName(typeof(ParentProcessObserverTelemetryTests).Assembly.Location)!,
			"Fixtures", "AddInWithHangingHostedService", "AddInWithHangingHostedService.dll");
		File.Exists(fixtureDll).Should()
			.BeTrue("hanging hosted-service fixture must be staged by _BuildAndCopyAddInTestFixtures");

		var tempDir = Path.GetTempPath();
		var telemetryFile = Path.Combine(tempDir, GetTestTelemetryFileName("parent_observer_forced_exit"));

		await using var parentHelper = CreateTelemetryHelperWithExactPath(Path.Combine(tempDir, GetTestTelemetryFileName("parent")));
		await using var childHelper = CreateTelemetryHelperWithExactPath(telemetryFile);

		var parentStarted = await parentHelper.StartAsync(CT);
		parentStarted.Should().BeTrue();
		parentHelper.AssertRunning();

		// The add-in's hosted service never stops, so the child's graceful shutdown hangs.
		var parentPid = parentHelper.GetProcessId();
		var childStarted = await childHelper.StartAsync(CT, extraArgs: $"--ppid {parentPid} --addins \"{fixtureDll}\"");
		childStarted.Should().BeTrue();
		childHelper.AssertRunning();

		await parentHelper.StopAsync(CT);

		// Detection (up to 7.5s) + forced-exit delay (5.75s), with margin
		var sw = Stopwatch.StartNew();
		while (childHelper.IsRunning && sw.Elapsed < TimeSpan.FromSeconds(20))
		{
			await Task.Delay(250, CT);
		}
		childHelper.IsRunning.Should().BeFalse("the forced exit should end the child when its graceful shutdown hangs");
		childHelper.ExitCode.Should().Be(4, "the parent process observer forces exit code 4");

		File.Exists(telemetryFile).Should().BeTrue("telemetry file should exist");
		var content = await File.ReadAllTextAsync(telemetryFile, CT);
		var events = ParseTelemetryEvents(content);
		WriteEventsList(events);
		AssertHasEvent(events, "uno/dev-server/parent-process-lost");
		AssertHasEvent(events, "uno/dev-server/parent-process-lost-forced-exit");
		AssertNoEvent(events, "uno/dev-server/shutdown");
	}
}
