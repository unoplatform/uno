# Health & Diagnostics

`HealthService` exposes an always-available `uno_health` MCP tool and a `uno://health` resource so AI agents can inspect DevServer state even before the upstream Host is ready. The CLI also exposes `uno.devserver health`, with `--json` returning the same `HealthReport` payload. The bridge also exposes `uno_app_select_solution` so an agent can explicitly choose a Uno solution when auto-discovery is ambiguous or deferred.

## Active Server Ownership

`list`, `disco`, and `health` all need enough runtime detail to answer one practical question: **who started the DevServer that is currently running for this workspace?**

To support that, active server diagnostics should include:

- `ProcessId`
- `ParentProcessId`
- `IdeChannelId`
- a bounded **process ancestry chain** displayed ancestor-first (`IDE → ... → dotnet → Host`) with PID + process name (up to 8 levels)

The ancestry chain is diagnostic only. It helps callers distinguish:

- a host launched by the current IDE session
- a host launched by another IDE
- a host launched indirectly by an MCP server or shell

This is especially important when `uno.devserver start --ideChannel ...` reuses an existing Host and updates the active IDE channel in-place instead of killing the Host.

## Data Flow

```
DevServerMonitor.LastDiscoveryInfo
  --> DiscoveryIssueMapper.MapDiscoveryIssues()   (discovery issues)
  --> HealthService.BuildHealthReport()            (+ runtime issues)
  --> HealthReport { Status, Issues[] }
  --> DiscoverySummary { ..., ActiveServers[] }
```

## Key Files

| File | Role |
|------|------|
| `Mcp/HealthService.cs` | Produces `uno_health` tool and `uno://health` resource |
| `Mcp/HealthReport.cs` | Data model, `IssueCode` enum, `ValidationSeverity` enum |
| `Mcp/HealthReportFactory.cs` | Shared builder for MCP and CLI health reports |
| `Helpers/HealthReportFormatter.cs` | Plain-text / JSON rendering for CLI health output |
| `Mcp/DiscoveryIssueMapper.cs` | Static mapper: `DiscoveryInfo` --> `ValidationIssue[]` |

## Discovery-Time Issues (`DiscoveryIssueMapper`)

Static mapper that converts `DiscoveryInfo` fields into `ValidationIssue[]`. Issues are evaluated in order; some trigger an early exit (no further checks):

| IssueCode | Severity | Trigger | Early exit? |
|-----------|----------|---------|-------------|
| `WorkspaceNotResolved` | Fatal | Solutions exist, but none resolve to a valid Uno workspace | Yes |
| `WorkspaceAmbiguous` | Warning | Multiple Uno workspaces match equally well | Yes |
| `GlobalJsonNotFound` | Fatal | `GlobalJsonPath` is null | Yes |
| `UnoSdkNotInGlobalJson` | Fatal | `UnoSdkPackage` or `UnoSdkVersion` is null | Yes |
| `SdkNotInCache` | Fatal | `UnoSdkPath` is null | Yes |
| `PackagesJsonNotFound` | Warning | `PackagesJsonPath` is null | No |
| `DotNetNotFound` | Fatal | `DotNetVersion` is null | No |
| `DevServerPackageNotCached` | Fatal | Version known but path is null | No |
| `HostBinaryNotFound` | Fatal | Package path + TFM known but `HostPath` is null | No |
| `AddInPackageNotCached` | Warning | Settings version known but path is null | No |
| `AddInDiscoveryFallback` | Warning | `AddInDiscoveryFailed` is true | No |

## Runtime Issues (`HealthService`)

These are added directly by `HealthService.BuildHealthReport()`:

| IssueCode | Severity | Trigger |
|-----------|----------|---------|
| `HostNotStarted` | Fatal | DevServer not yet started |
| `NoSolutionFound` | Warning | No `.sln` or `.slnx` file found in working directory tree |
| `HostCrashed` | Warning/Fatal | Connection state `Reconnecting` / `Degraded` |
| `HostUnreachable` | Warning | Started but not yet connected |
| `HostMcpEndpointNotAvailable` | Warning | Host responds to HTTP but `/mcp` returns 404/400 (pre-MCP host or MCP transport failed to register). Remediation suggests upgrading the DevServer package. |
| `UpstreamError` | Fatal | Upstream task faulted |

## Non-Mapped `IssueCode` Values

Three codes exist in the `IssueCode` enum but are **not currently mapped** in CLI code -- reserved for Host-side or future use:

- `DotNetVersionUnsupported`
- `AddInLoadFailed`
- `AddInBinaryNotFound`

## Notable HealthReport Fields

| Field | Type | Description |
|-------|------|-------------|
| `DiscoveredSolutions` | `string[]?` | `.sln`/`.slnx` paths relative to the active monitor working directory. This may be `null` when a caller omits the field, or an empty array when discovery completed with no candidates. |
| `ConnectionState` | `ConnectionState?` | Lifecycle state of the MCP bridge (see `Mcp/ConnectionState.cs` for state diagram) |
| `EffectiveWorkspaceDirectory` | `string?` | Resolved workspace directory used for discovery and cache identity |
| `SelectedSolutionPath` | `string?` | Solution selected for the current workspace |
| `SelectionSource` | `WorkspaceSelectionSource?` | Whether the current selection was automatic, roots-confirmed, or explicitly user-selected |
| `ResolutionKind` | `WorkspaceResolutionKind?` | How the workspace was selected (`CurrentDirectory`, `AutoDiscovered`, `Ambiguous`, `NoValidWorkspace`, `NoCandidates`) |
| `Discovery` | `DiscoverySummary?` | Full discovery info including `ActiveServers[]` with `IsInWorkspace`, `IdeChannelId`, and process ancestry |

## `start --ideChannel` reuse semantics

When a Host already exists for the selected solution:

- `uno.devserver start` without `--ideChannel` keeps the existing "already running" behavior
- `uno.devserver start --ideChannel <id>` reuses the Host and replaces the active IDE channel without restarting the process

Because the Host stays alive, `health` and `disco` must show the updated `IdeChannelId` and current process ancestry so launchers can verify that the running instance was reused rather than respawned.

### IDE channel lifecycle logging

The Host emits the following diagnostic logs during channel operations:

| Log level | Message | When |
|-----------|---------|------|
| Information | `IDE channel rebind requested: {ChannelId}` | HTTP POST hits the rebind endpoint |
| Information | `IDE channel pipe created: \\.\pipe\{ChannelId}` | Named pipe server stream created |
| Information | `IDE channel {ChannelId}: waiting for client connection...` | `WaitForConnectionAsync` begins |
| Information | `IDE channel {ChannelId}: client connected, JsonRpc attached.` | Client connected, RPC active |
| Warning | `IDE channel {ChannelId}: session was superseded` | Another rebind replaced this session |
| Warning | `IDE channel {ChannelId}: wait for connection was cancelled.` | Session CTS was cancelled |
| Debug | `IDE channel {ChannelId} is already active, skipping rebind.` | Idempotent rebind (same channelId) |

The CLI also forwards the Host subprocess stdout/stderr at Debug level on success, so IDE extensions can see reuse decisions (`"A DevServer is already running..."`) and rebind POST results.

## Health Status

```
Fatal issue present --> Unhealthy
Warning(s) only     --> Degraded
No issues           --> Healthy
```

## Adding New IssueCode Values

1. Add the new value to the `IssueCode` enum in `Mcp/HealthReport.cs`
2. Add mapping logic in `DiscoveryIssueMapper.cs` (if discovery-related) or `HealthService.BuildHealthReport()` (if runtime-related)
3. Add test in `Given_DiscoveryIssueMapper.cs` verifying the trigger condition and severity
