using System.Diagnostics;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Pure decision logic extracted from <see cref="DevServerMonitor"/> for testability.
/// </summary>
internal static class MonitorDecisions
{
	internal enum PostStartupAction
	{
		/// <summary>We own the process — monitor it for crashes and retry if it exits.</summary>
		MonitorOwnProcess,

		/// <summary>We reused an existing DevServer via AmbientRegistry — wait indefinitely.</summary>
		WaitIndefinitely
	}

	internal enum ReadinessFailureAction
	{
		/// <summary>The process died during startup — retry.</summary>
		RetryStart,

		/// <summary>The readiness probe timed out but the process is still running (or null) — hard fail.</summary>
		HardFail
	}

	/// <summary>
	/// Decides the post-startup action based on whether we own the server process.
	/// </summary>
	internal static PostStartupAction DeterminePostStartupAction(Process? serverProcess)
		=> serverProcess is not null
			? PostStartupAction.MonitorOwnProcess
			: PostStartupAction.WaitIndefinitely;

	/// <summary>
	/// Decides the readiness failure action based on whether the process has exited.
	/// </summary>
	internal static ReadinessFailureAction DetermineReadinessFailureAction(Process? serverProcess)
		=> serverProcess is { HasExited: true }
			? ReadinessFailureAction.RetryStart
			: ReadinessFailureAction.HardFail;
}
