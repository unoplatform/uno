# Ambient Registry

Local file-based registry that tracks active DevServer instances, enabling process reuse and duplicate detection.

## Source File

`AmbientRegistry.cs`

## Storage

- **Directory:** `%LOCALAPPDATA%/Uno Platform/DevServers/`
- **File pattern:** `devserver-{PID}.json`

Each JSON file contains a `DevServerRegistration`:

```json
{
  "ProcessId": 12345,
  "ParentProcessId": 6789,
  "SolutionPath": "C:/src/MyApp/MyApp.sln",
  "StartTime": "2026-02-18T10:30:00Z",
  "Port": 53821,
  "MachineName": "DESKTOP-ABC",
  "UserName": "developer"
}
```

## API

| Method | Description |
|--------|-------------|
| `Register(solution, ppid, httpPort)` | Write registration for current process |
| `Unregister()` | Delete registration, cleanup stale entries |
| `GetActiveDevServers()` | Enumerate all live registrations (auto-cleans stale) |
| `GetActiveDevServerForPath(solution)` | Find DevServer for a solution path |
| `GetActiveDevServerForPort(port)` | Find DevServer by HTTP port |

Stale registrations (process no longer running) are automatically cleaned up during enumeration.

## Usage in `DevServerMonitor.StartProcess()`

Before launching a new Host process, the CLI checks the ambient registry for an existing DevServer matching the solution path. If found, it reuses that instance (returns the existing port) instead of spawning a duplicate.

```
CLI start --> DevServerMonitor.StartProcess()
                --> AmbientRegistry.GetActiveDevServerForPath(solution)
                    --> Found? Reuse (return existing port)
                    --> Not found? Launch new Host process
```

## Health Polling for Reused Instances

When the CLI reuses an existing DevServer (i.e., `_serverProcess` is null), `DevServerMonitor` polls the Host's `/mcp` endpoint periodically (`MonitorDecisions.HealthPollIntervalMs`, 10 seconds). If the Host stops responding for 3 consecutive polls, the monitor fires `ServerCrashed` and initiates recovery — identical to the owned-process crash path.

## Known Limitation

Reuse is limited to **MCP-alongside-IDE** scenarios. The Host's `IDEChannel` only supports a single IDE connection (`maxNumberOfServerInstances: 1`), so two full IDEs cannot share the same Host -- they would conflict on Hot Reload notifications and launch tracking.
