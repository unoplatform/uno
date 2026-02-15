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
/// <remarks>
/// <para><b>Two separate retry counters exist in the system:</b></para>
/// <list type="bullet">
///   <item>
///     <term>DevServerMonitor retry counter</term>
///     <description>
///       Tracks consecutive failures during <em>initial startup</em> (SDK resolution,
///       host binary lookup, process launch, health-check). Fires <c>ServerFailed</c>
///       after 3 failed attempts. This counter is internal to DevServerMonitor.
///     </description>
///   </item>
///   <item>
///     <term>ProxyLifecycleManager reconnection counter</term>
///     <description>
///       Tracks consecutive <em>crash→restart</em> cycles after a successful initial
///       connection. Incremented on each <c>ServerCrashed</c> event, reset to 0 when
///       the upstream connection succeeds (toolListChanged). Transitions to
///       <see cref="Degraded"/> after exceeding <c>MaxReconnectionAttempts</c> (3).
///     </description>
///   </item>
/// </list>
///
/// <para><b>Event-to-state mapping:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Event</term>
///     <description>State transition</description>
///   </listheader>
///   <item>
///     <term><c>StartDevServerMonitor</c> succeeds</term>
///     <description><see cref="Initializing"/> → <see cref="Discovering"/></description>
///   </item>
///   <item>
///     <term><c>DevServerMonitor.ServerStarted</c></term>
///     <description>→ <see cref="Connecting"/></description>
///   </item>
///   <item>
///     <term><c>McpUpstreamClient</c> toolListChanged callback</term>
///     <description>→ <see cref="Connected"/> (reconnection counter reset to 0)</description>
///   </item>
///   <item>
///     <term><c>DevServerMonitor.ServerCrashed</c> (attempts ≤ max)</term>
///     <description>→ <see cref="Reconnecting"/> + <c>ResetConnectionAsync()</c></description>
///   </item>
///   <item>
///     <term><c>DevServerMonitor.ServerCrashed</c> (attempts > max)</term>
///     <description>→ <see cref="Degraded"/></description>
///   </item>
///   <item>
///     <term><c>DevServerMonitor.ServerFailed</c></term>
///     <description>→ <see cref="Degraded"/></description>
///   </item>
///   <item>
///     <term>Clean shutdown (host.StopAsync / DisposeAsync)</term>
///     <description>→ <see cref="Shutdown"/></description>
///   </item>
/// </list>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ConnectionState
{
	/// <summary>
	/// STDIO server started, serving cached tools. No DevServer monitor running yet.
	/// This is the initial state before any discovery or connection attempt.
	/// </summary>
	Initializing,

	/// <summary>
	/// Resolving the Uno SDK version, locating add-ins, and finding the host binary.
	/// Entered when <c>StartDevServerMonitor</c> succeeds or on a reconnection retry cycle.
	/// </summary>
	Discovering,

	/// <summary>
	/// Host process has been started and is initializing.
	/// Waiting for the health-check endpoint to report ready.
	/// </summary>
	Launching,

	/// <summary>
	/// Host is ready and the HTTP StreamableHttp connection to <c>/mcp</c> is in progress.
	/// Entered when <c>DevServerMonitor.ServerStarted</c> fires with the endpoint URL.
	/// </summary>
	Connecting,

	/// <summary>
	/// Upstream MCP client is active and proxying tools. This is the normal operating state.
	/// Entered when the <c>toolListChanged</c> callback fires after a successful connection.
	/// The reconnection counter is reset to 0 upon entering this state.
	/// </summary>
	Connected,

	/// <summary>
	/// Host process exited unexpectedly and is being automatically restarted.
	/// The upstream TCS has been reset via <c>ResetConnectionAsync()</c> to allow
	/// a fresh connection. The reconnection counter has been incremented.
	/// Tools called in this state receive a contextual "reconnecting" error message.
	/// </summary>
	Reconnecting,

	/// <summary>
	/// Unrecoverable error: either the initial startup failed after max retries
	/// (<c>ServerFailed</c>) or the host crashed more than <c>MaxReconnectionAttempts</c>
	/// times without a successful reconnection. Cached tools are still served if available.
	/// This state is not strictly terminal — external recovery (manual restart) is possible.
	/// </summary>
	Degraded,

	/// <summary>
	/// Clean shutdown in progress or completed. The proxy is no longer accepting requests.
	/// </summary>
	Shutdown,
}
