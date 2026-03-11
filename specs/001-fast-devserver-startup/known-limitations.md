# Known Limitations

Known limitations of the current DevServer architecture, documented during implementation. Each limitation includes context, impact, and the conditions under which it should be addressed.

---

## L1. AmbientRegistry Reuse: Single IDEChannel Connection

**Phase**: 1b (Controller Bypass)
**Date**: 2026-02-14
**File**: `src/Uno.UI.DevServer.Cli/Mcp/DevServerMonitor.cs`

### Context

Phase 1b introduces reuse of existing DevServer instances via AmbientRegistry. When the MCP CLI detects a DevServer already running for the same solution, it connects to the existing instance instead of launching a new one.

### Limitation

The Host only supports **a single IDEChannel connection** (named pipe) at a time (`IdeChannelServer.cs`, `maxNumberOfServerInstances: 1`). This means:

| Scenario | Works? | Reason |
|----------|:---:|--------|
| IDE + MCP CLI on same solution | Yes | MCP uses HTTP (`/mcp`), not the named pipe |
| IDE-A + IDE-B on same solution | No | Both need the IDEChannel pipe |

### Liveness Monitoring

When the CLI reuses an existing Host via AmbientRegistry (`_serverProcess` is null), `DevServerMonitor` uses HTTP health polling instead of process exit monitoring. The `/mcp` endpoint is polled every 10 seconds; after 3 consecutive failures the monitor fires `ServerCrashed` and initiates recovery.

### Why this is safe for MCP

The MCP proxy connects exclusively via the HTTP `/mcp` endpoint. It does not use the IDEChannel named pipe. The communication paths are independent:

- **IDEChannel** (named pipe): Hot Reload notifications, XAML updates, launch tracking
- **MCP endpoint** (HTTP/SSE): AI tools, inspection, metadata

### What would break if two IDEs shared the same Host

1. **IDEChannel**: The second IDE cannot connect to the pipe (single slot)
2. **Launch monitor**: FIFO queue per MVID — one IDE consumes the other's event
3. **Hot Reload**: Notifications sent only to the IDE connected to the pipe
4. **Assembly contexts**: Static `_loadContexts` shared across connections, risk of version conflicts

### When to address

If any of these scenarios becomes real:
- Two IDEs need to work simultaneously on the same project
- The MCP CLI needs access to Hot Reload notifications
- A multi-tenant DevServer scenario is required

### Potential resolution

- Make `IdeChannelServer` multi-connection (`maxNumberOfServerInstances > 1`)
- Add per-IDE routing in `ApplicationLaunchMonitor` and processors
- Isolate `AssemblyLoadContext` per IDE connection
