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

		// Read telemetry file
		File.Exists(telemetryFile).Should().BeTrue("telemetry file should exist");
		var content = await File.ReadAllTextAsync(telemetryFile, CT);
		var events = ParseTelemetryEvents(content);
		AssertHasEvent(events, "uno/dev-server/parent-process-lost");
	}
}
