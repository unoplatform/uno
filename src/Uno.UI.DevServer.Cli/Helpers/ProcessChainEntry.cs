namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Represents one hop in a process ancestry chain used for diagnostics.
/// </summary>
public sealed class ProcessChainEntry
{
	public int ProcessId { get; init; }

	public string? ProcessName { get; init; }
}
