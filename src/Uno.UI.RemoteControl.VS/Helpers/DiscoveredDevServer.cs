namespace Uno.UI.RemoteControl.VS.Helpers;

/// <summary>
/// Information about an active DevServer host instance returned by the Uno DevServer
/// CLI's <c>disco</c> command. Used by <see cref="DevServerHostDiscovery"/> to detect
/// already-running hosts so <see cref="EntryPoint.EnsureServerAsync"/> can skip spawning
/// a duplicate.
/// </summary>
internal sealed class DiscoveredDevServer
{
	public int ProcessId { get; init; }

	public int Port { get; init; }

	public int ParentProcessId { get; init; }

	/// <summary>
	/// Absolute path to the solution this host is serving, when reported by the CLI.
	/// May be <see langword="null"/> for hosts launched without a solution context.
	/// </summary>
	public string? SolutionPath { get; init; }

	/// <summary>
	/// IDE channel pipe identifier the host is listening on, when surfaced by the disco
	/// payload. Currently absent from CLI versions in the field, which is why the
	/// EntryPoint adoption story is "skip-if-detected" rather than "connect-and-adopt"
	/// — the latter requires this value to wire <c>IdeChannelClient</c> to the existing
	/// host's pipe. Tracked as a Phase 2 follow-up.
	/// </summary>
	public string? IdeChannelId { get; init; }
}
