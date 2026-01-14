# App MCP Telemetry

**Event Name Prefix:** `uno/app-mcp`

The Uno Platform App MCP (Model Context Protocol) provides agents with the ability to interact with running Uno applications. Telemetry tracks tool usage and interactions.

## MCP Tools Telemetry

The App MCP tracks usage of the following tools:

### Community License Tools

| Tool Name | Properties | Measurements | Description |
|-----------|-----------|--------------|-------------|
| `uno_app_get_runtime_info` | `Platform` (string), `OS` (string), `ProcessId` (int) | `Duration` (ms) | Get general information about the running app |
| `uno_app_get_screenshot` | `Width` (int), `Height` (int), `Format` (string) | `Duration` (ms), `ImageSize` (bytes) | Capture screenshot of the running app |
| `uno_app_pointer_click` | `X` (int), `Y` (int), `Button` (string) | `Duration` (ms) | Perform click at coordinates in the app |
| `uno_app_key_press` | `Key` (string), `Modifiers` (string) | `Duration` (ms) | Type individual keys with optional modifiers |
| `uno_app_type_text` | `TextLength` (int) | `Duration` (ms) | Type long strings of text in controls |
| `uno_app_visualtree_snapshot` | `ElementCount` (int), `Depth` (int) | `Duration` (ms), `SnapshotSize` (bytes) | Get textual representation of visual tree |
| `uno_app_element_peer_default_action` | `ElementType` (string), `ActionType` (string) | `Duration` (ms) | Execute default automation peer action on element |
| `uno_app_close` | `ExitCode` (int) | `Duration` (ms) | Close the running app |

### Pro License Tools

| Tool Name | Properties | Measurements | Description |
|-----------|-----------|--------------|-------------|
| `uno_app_element_peer_action` | `ElementType` (string), `ActionType` (string), `PeerAction` (string) | `Duration` (ms) | Invoke specific element automation peer action |
| `uno_app_get_element_datacontext` | `ElementType` (string), `DataContextType` (string) | `Duration` (ms), `DataSize` (bytes) | Get textual representation of DataContext |

## Session Tracking

App MCP sessions track the following:

| Property | Type | Description |
|----------|------|-------------|
| `SessionId` | GUID | Unique session identifier |
| `AgentType` | String | Type of agent (Claude, Copilot, Codex, etc.) |
| `SessionStartTime` | DateTime | When the MCP session started |
| `AppPlatform` | String | Platform of the running app (WebAssembly, iOS, Android, Windows, etc.) |
| `ToolsUsed` | Array | List of tools invoked during session |

## Error Tracking

Errors and exceptions are tracked with:

| Property | Type | Description |
|----------|------|-------------|
| `ErrorType` | String | Type of error (ConnectionError, ToolExecutionError, TimeoutError, etc.) |
| `ToolName` | String | Name of the tool that failed |
| `ErrorMessage` | String | Error message (sanitized, no PII) |
| `StackTraceHash` | String | SHA256 hash of stack trace for grouping |

## Agent Interaction Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `tool-invocation-count` | Counter | Number of times each tool is invoked |
| `tool-success-rate` | Percentage | Success rate per tool |
| `session-duration` | Duration | Length of MCP sessions |
| `tools-per-session` | Average | Average number of tools used per session |
| `screenshot-frequency` | Counter | How often screenshots are taken |
| `interaction-patterns` | String | Common sequences of tool usage |

## Privacy Notes

- **No PII**: No user input text, file paths, or application data is logged
- **Sanitized Errors**: Error messages are sanitized to remove any sensitive information
- **Hashed Identifiers**: Stack traces and error details are hashed for grouping without exposing implementation details
- **Agent Anonymization**: Agent sessions use anonymous session IDs

## Reference

For more detailed information, see the [Uno Platform App MCP Repository](https://github.com/unoplatform/uno.app-mcp) and [Using the Uno MCPs](xref:Uno.Features.Uno.MCPs).
