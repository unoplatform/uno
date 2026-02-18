using System.Diagnostics;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class MonitorDecisionsTests
{
	[TestMethod]
	[Description("When serverProcess is null (AmbientRegistry reuse), should poll health")]
	public void DeterminePostStartupAction_WhenNoOwnProcess_ShouldPollHealth()
	{
		var action = MonitorDecisions.DeterminePostStartupAction(serverProcess: null);

		action.Should().Be(MonitorDecisions.PostStartupAction.PollHealth,
			"reused DevServer via AmbientRegistry — should poll health to detect host death");
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
	[Description("When the process died during startup, should retry instead of hard-failing")]
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
			"process died during startup — should retry, not hard-fail");
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

	// -------------------------------------------------------------------
	// DisposeAndClearProcess
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("DisposeAndClearProcess disposes an exited process and returns null")]
	public void DisposeAndClearProcess_WhenExited_ReturnsNull()
	{
		var p = Process.Start(new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c exit 0",
			CreateNoWindow = true,
			UseShellExecute = false
		})!;
		p.WaitForExit();

		MonitorDecisions.DisposeAndClearProcess(p).Should().BeNull();
	}

	[TestMethod]
	[Description("DisposeAndClearProcess handles null gracefully")]
	public void DisposeAndClearProcess_WhenNull_ReturnsNull()
	{
		MonitorDecisions.DisposeAndClearProcess(null).Should().BeNull();
	}

	// -------------------------------------------------------------------
	// DetermineClientSupportsRoots
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Junie clients have Roots capability but not ListChanged — should still be detected")]
	public void DetermineClientSupportsRoots_WhenRootsPresent_ReturnsTrue()
	{
		MonitorDecisions.DetermineClientSupportsRoots(forceRootsFallback: false, rootsCapabilityPresent: true)
			.Should().BeTrue("Junie clients have Roots but not ListChanged");
	}

	[TestMethod]
	[Description("When no Roots capability, should return false")]
	public void DetermineClientSupportsRoots_WhenRootsAbsent_ReturnsFalse()
	{
		MonitorDecisions.DetermineClientSupportsRoots(forceRootsFallback: false, rootsCapabilityPresent: false)
			.Should().BeFalse();
	}

	[TestMethod]
	[Description("ForceRootsFallback overrides even when Roots capability is present")]
	public void DetermineClientSupportsRoots_WhenForceRootsFallback_AlwaysFalse()
	{
		MonitorDecisions.DetermineClientSupportsRoots(forceRootsFallback: true, rootsCapabilityPresent: true)
			.Should().BeFalse();
	}

	// -------------------------------------------------------------------
	// StartOnceGuard
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Concurrent calls to TryStart should succeed exactly once")]
	public void StartOnceGuard_ConcurrentCalls_OnlyOneSucceeds()
	{
		var guard = new MonitorDecisions.StartOnceGuard();
		var successCount = 0;

		Parallel.For(0, 20, _ =>
		{
			if (guard.TryStart())
			{
				Interlocked.Increment(ref successCount);
			}
		});

		successCount.Should().Be(1);
	}

	[TestMethod]
	[Description("Sequential calls to TryStart — only the first succeeds")]
	public void StartOnceGuard_SequentialCalls_OnlyFirstSucceeds()
	{
		var guard = new MonitorDecisions.StartOnceGuard();

		guard.TryStart().Should().BeTrue();
		guard.TryStart().Should().BeFalse();
		guard.IsStarted.Should().BeTrue();
	}
}
