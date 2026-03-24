namespace Uno.UI.DevServer.Cli.Helpers;

#pragma warning disable IDE0055 // Shared linked source currently triggers non-converging formatter diagnostics.
/// <summary>
/// Represents one hop in a process ancestry chain used for diagnostics.
/// </summary>
public sealed class ProcessChainEntry
{
	public int ProcessId { get; init; }

	public string? ProcessName { get; init; }
}
#pragma warning restore IDE0055
