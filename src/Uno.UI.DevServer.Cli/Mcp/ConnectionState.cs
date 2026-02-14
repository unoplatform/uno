using System.Text.Json.Serialization;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Represents the lifecycle state of the MCP stdio-to-HTTP bridge.
///
/// <para><b>State Transitions:</b></para>
/// <code>
///   Initializing
///       |
///       v
///   Discovering ----[SDK/host resolved]----> Launching
///       |                                       |
///       | [resolution failed]                   | [process started]
///       v                                       v
///   Degraded &lt;---[max retries]---         Connecting
///       ^                           |           |
///       |                           |           | [upstream connected]
///       |                           |           v
///       |                           +------ Connected
///       |                                       |
///       |                                       | [host process exited]
///       |                                       v
///       +---[max reconnections]---       Reconnecting
///                                            |
///                                            | [retry]
///                                            v
///                                        Discovering  (cycle)
///
///   Any state --[clean shutdown]--> Shutdown
/// </code>
///
/// <para>
/// This is an <b>observational</b> state machine: it does not drive behavior,
/// it reflects the current state as determined by DevServerMonitor events
/// and McpUpstreamClient connection status. The state is used by HealthService
/// for diagnostics and by McpStdioServer for contextual error messages.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ConnectionState
{
	Initializing,
	Discovering,
	Launching,
	Connecting,
	Connected,
	Reconnecting,
	Degraded,
	Shutdown,
}
