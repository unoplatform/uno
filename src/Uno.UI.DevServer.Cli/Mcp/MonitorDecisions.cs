using System.Diagnostics;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Pure decision logic extracted from <see cref="DevServerMonitor"/> for testability.
/// </summary>
/// <seealso href="../health-diagnostics.md"/>
/// <seealso href="../../../specs/001-fast-devserver-startup/known-limitations.md"/>
internal static class MonitorDecisions
{
	internal enum PostStartupAction
	{
		/// <summary>We own the process — monitor it for crashes and retry if it exits.</summary>
		MonitorOwnProcess,

		/// <summary>We reused an existing DevServer via AmbientRegistry — poll health periodically.</summary>
		PollHealth
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
			: PostStartupAction.PollHealth;

	/// <summary>
	/// Interval (ms) between HTTP health poll attempts when attached to an existing DevServer.
	/// </summary>
	internal const int HealthPollIntervalMs = 10_000;

	/// <summary>
	/// Decides the readiness failure action based on whether the process has exited.
	/// </summary>
	internal static ReadinessFailureAction DetermineReadinessFailureAction(Process? serverProcess)
		=> serverProcess is { HasExited: true }
			? ReadinessFailureAction.RetryStart
			: ReadinessFailureAction.HardFail;

	/// <summary>
	/// Disposes a process handle and returns null, preventing native handle leaks
	/// when clearing <c>_serverProcess</c> references.
	/// </summary>
	internal static Process? DisposeAndClearProcess(Process? process)
	{
		try { process?.Dispose(); } catch { }
		return null;
	}

	/// <summary>
	/// Determines whether the MCP client supports roots. Checks for the presence of
	/// the Roots capability object (not just ListChanged), so clients like Junie that
	/// declare Roots support without ListChanged are correctly detected.
	/// </summary>
	internal static bool DetermineClientSupportsRoots(bool forceRootsFallback, bool rootsCapabilityPresent)
		=> !forceRootsFallback && rootsCapabilityPresent;

	/// <summary>
	/// Thread-safe guard ensuring that a start operation occurs exactly once,
	/// even under concurrent calls from <c>EnsureDevServerStartedFromSolutionDirectory</c>
	/// and <c>ProcessRoots</c>.
	/// </summary>
	internal sealed class StartOnceGuard
	{
		private int _started;

		public bool TryStart() => Interlocked.CompareExchange(ref _started, 1, 0) == 0;

		public bool IsStarted => Volatile.Read(ref _started) == 1;
	}
}
