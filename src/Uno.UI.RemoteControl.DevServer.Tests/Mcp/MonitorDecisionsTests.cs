using System.Diagnostics;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class MonitorDecisionsTests
{
	[TestMethod]
	[Description("P1 regression: when serverProcess is null (AmbientRegistry reuse), should wait indefinitely, not monitor")]
	public void DeterminePostStartupAction_WhenNoOwnProcess_ShouldWaitIndefinitely()
	{
		var action = MonitorDecisions.DeterminePostStartupAction(serverProcess: null);

		action.Should().Be(MonitorDecisions.PostStartupAction.WaitIndefinitely,
			"P1: reused DevServer via AmbientRegistry — we don't own the process, should not monitor for crashes");
	}

	[TestMethod]
	[Description("When we own the server process, should monitor it for crashes")]
	public void DeterminePostStartupAction_WhenOwnProcess_ShouldMonitor()
	{
		using var process = Process.GetCurrentProcess();

		var action = MonitorDecisions.DeterminePostStartupAction(serverProcess: process);

		action.Should().Be(MonitorDecisions.PostStartupAction.MonitorOwnProcess);
	}

	[TestMethod]
	[Description("P1bis regression: when the process died during startup, should retry instead of hard-failing")]
	public void DetermineReadinessFailureAction_WhenProcessExited_ShouldRetry()
	{
		using var process = Process.Start(new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c exit 0",
			CreateNoWindow = true,
			UseShellExecute = false
		})!;
		process.WaitForExit();

		var action = MonitorDecisions.DetermineReadinessFailureAction(serverProcess: process);

		action.Should().Be(MonitorDecisions.ReadinessFailureAction.RetryStart,
			"P1bis: process died during startup — should retry, not hard-fail");
	}

	[TestMethod]
	[Description("When process is still running after readiness timeout, should hard-fail")]
	public void DetermineReadinessFailureAction_WhenProcessRunning_ShouldHardFail()
	{
		using var process = Process.GetCurrentProcess();

		var action = MonitorDecisions.DetermineReadinessFailureAction(serverProcess: process);

		action.Should().Be(MonitorDecisions.ReadinessFailureAction.HardFail);
	}

	[TestMethod]
	[Description("When serverProcess is null during readiness failure, should hard-fail")]
	public void DetermineReadinessFailureAction_WhenProcessNull_ShouldHardFail()
	{
		var action = MonitorDecisions.DetermineReadinessFailureAction(serverProcess: null);

		action.Should().Be(MonitorDecisions.ReadinessFailureAction.HardFail);
	}
}
