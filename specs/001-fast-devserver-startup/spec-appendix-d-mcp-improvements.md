# MCP Protocol & Architecture Improvements

> **Parent**: [Main Spec](spec.md) — Section 8b
> **Related**: [Startup Workflow](spec-appendix-a-startup-workflow.md)

---

## 1. MCP Protocol Optimization

The current MCP proxy (`McpProxy.cs`) does not fully leverage the MCP protocol. These improvements are recommended alongside the fast startup work.

### Capability Declarations

The CLI MCP server currently declares no capabilities. It should declare:

```csharp
.WithServerInfo(new Implementation { Name = "uno-devserver", Version = "..." })
.WithCapabilities(new ServerCapabilities
{
    Tools = new ToolsCapability { ListChanged = true },
    Resources = new ResourcesCapability { Subscribe = true, ListChanged = true },
    Logging = new LoggingCapability(),
})
```

This tells clients exactly what the server supports, enabling better client behavior (e.g., resource subscriptions for health updates).

### Tool Annotations

MCP tool annotations provide metadata that helps AI models make better decisions:

```csharp
[McpServerTool(Annotations = new { readOnlyHint = true })]    // uno_app_get_screenshot
[McpServerTool(Annotations = new { destructiveHint = true })]  // uno_app_close
[McpServerTool(Annotations = new { openWorldHint = false })]   // all tools operate on connected app only
```

These annotations help the model avoid destructive operations without confirmation and understand which tools are safe to call speculatively.

### Structured Logging

MCP provides a `notifications/message` mechanism for server-to-client logging. Use it instead of (or in addition to) stderr:

- `level: "info"` — discovery progress, host launch status
- `level: "warning"` — degraded state, fallback triggered
- `level: "error"` — host crash, connection failure

Clients that support MCP logging (Claude Code, Claude Desktop) will display these in their UI.

### Known Bugs to Fix

| Bug | Location | Fix |
|-----|----------|-----|
| `ToolListChangedNotification` deserialized as `ResourceUpdatedNotificationParams` | `McpClientProxy.cs:74` | Use correct notification type |
| Hardcoded `ClientsWithoutListUpdateSupport` list | `McpProxy.cs:41` | Detect support via client capabilities, not name matching |
| No `ServerInfo` declared | `McpProxy.cs` | Add `.WithServerInfo()` |

---

## 2. Project Structure and DI Refactoring

The current `Mcp/` directory has architectural issues that should be addressed as part of the fast-startup work.

### Current Issues

| Issue | File | Lines | Violation |
|-------|------|:-----:|-----------|
| `McpProxy` is 700+ lines with tool forwarding, root handling, monitoring, caching, error handling | `McpProxy.cs` | ~714 | SRP |
| No interfaces for `DevServerMonitor`, `McpClientProxy` | `Mcp/*.cs` | — | DIP, testability |
| Service locator pattern in `DevServerMonitor` | `DevServerMonitor.cs:67` | 67 | DI anti-pattern |
| Hardcoded client names for capability detection | `McpProxy.cs:41` | 41 | OCP |
| One-shot `TaskCompletionSource` prevents reconnection | `McpClientProxy.cs:20` | 20 | Structural |

### Recommended Refactoring

**Split `McpProxy` into focused services**:

| New Class | Responsibility |
|-----------|---------------|
| `McpStdioServer` | STDIO MCP server lifecycle, handler registration |
| `McpUpstreamClient` | Upstream HTTP connection management, reconnection |
| `ToolListManager` | Tool caching, list_changed handling, timeout logic |
| `HealthService` | Health report aggregation, resource + tool exposure |
| `ProxyLifecycleManager` | State machine (Initializing → Connected → Degraded), orchestration |

**Extract interfaces** for testability:

```csharp
internal interface IHostLauncher
{
    Task<HostProcess> LaunchAsync(HostLaunchConfig config, CancellationToken ct);
}

internal interface IAddInResolver
{
    Task<AddInResolutionResult> ResolveAsync(string workingDirectory, CancellationToken ct);
}

internal interface IUpstreamMcpClient : IAsyncDisposable
{
    Task ConnectAsync(Uri endpoint, CancellationToken ct);
    Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken ct);
    Task<CallToolResult> CallToolAsync(string name, JsonElement? args, CancellationToken ct);
}
```

**DI registration** should use standard `IServiceCollection` patterns, not service locator. All services registered in `Program.cs` via extension methods.

This refactoring is a **prerequisite** for the state machine (Phase 1c) — the current monolithic structure cannot support reconnection cleanly.
