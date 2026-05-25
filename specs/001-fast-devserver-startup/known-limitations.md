# Known Limitations

Known limitations of the current DevServer architecture, documented during implementation. Each limitation includes context, impact, and the conditions under which it should be addressed.

---

## L1. AmbientRegistry Reuse: Single Active IDEChannel

**Phase**: 1b (Controller Bypass)
**Date**: 2026-02-14
**File**: `src/Uno.UI.DevServer.Cli/Mcp/DevServerMonitor.cs`

### Context

Phase 1b introduces reuse of existing DevServer instances via AmbientRegistry. When the MCP CLI detects a DevServer already running for the same solution, it connects to the existing instance instead of launching a new one.

### Limitation

The Host only supports **a single active IDEChannel connection** (named pipe) at a time (`IdeChannelServer.cs`, `maxNumberOfServerInstances: 1`). This means:

| Scenario | Works? | Reason |
|----------|:---:|--------|
| IDE + MCP CLI on same solution | Yes | MCP uses HTTP (`/mcp`), not the named pipe |
| IDE-A + IDE-B on same solution | Partially | The Host can be reused, but only one active IDE channel can own the pipe at a time |

### Current target behavior

`uno.devserver start --ideChannel <id>` should **reuse** an already-running Host for the same solution and replace the active IDE channel in-place. This avoids killing a Host owned by another launcher while still letting a new IDE session attach.

This is intentionally narrower than full multi-tenant support:

- The Host remains **single-channel**
- Rebinding the active channel is supported
- Simultaneous pipe connections from multiple IDEs are still unsupported

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

- Keep the current single-channel design, but allow **channel replacement** on an already-running Host
- Later, if needed, make `IdeChannelServer` multi-connection (`maxNumberOfServerInstances > 1`)
- Add per-IDE routing in `ApplicationLaunchMonitor` and processors
- Isolate `AssemblyLoadContext` per IDE connection

---

## L2. AmbientRegistry Fallback: Same-Port Reuse Not Handled

**Phase**: 1b (Controller Bypass)
**Date**: 2026-04-02
**File**: `src/Uno.UI.DevServer.Cli/Mcp/MonitorDecisions.cs`

### Context

When the controller detects an existing DevServer for the same solution, it exits with code 0. The monitor's readiness probe returns `ProcessExited` and attempts an AmbientRegistry fallback to adopt the existing server.

### Limitation

`ShouldAttemptAmbientFallback` requires `existingPort != currentPort`. When the controller reuses a server on the **same** port that was originally requested, the fallback finds nothing on a different port and treats it as a failure. The monitor then retries the full startup cycle instead of simply continuing with HTTP polling on the current port.

### Impact

In practice this is rare: the CLI allocates a random free port via `EnsureTcpPort()`, so collision with an IDE-launched server's port is unlikely. When it does happen, the monitor retries (up to `maxRetries`), adding unnecessary delay.

### When to address

When same-port reuse becomes observable in production (e.g., IDE and CLI configured with a fixed `--httpPort`).

### Potential resolution

- Inspect the controller's exit code: if exit code 0, continue with HTTP polling on the current port even without a different-port AmbientRegistry match
- Or: allow `ShouldAttemptAmbientFallback` to match same-port entries when the spawned process exited cleanly (code 0)
