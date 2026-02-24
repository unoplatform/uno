# Health & Diagnostics

`HealthService` exposes an always-available `uno_health` MCP tool and a `uno://health` resource so AI agents can inspect DevServer state even before the upstream Host is ready.

## Data Flow

```
DevServerMonitor.LastDiscoveryInfo
  --> DiscoveryIssueMapper.MapDiscoveryIssues()   (discovery issues)
  --> HealthService.BuildHealthReport()            (+ runtime issues)
  --> HealthReport { Status, Issues[] }
  --> DiscoverySummary { ..., ActiveServer? }
```

## Key Files

| File | Role |
|------|------|
| `Mcp/HealthService.cs` | Produces `uno_health` tool and `uno://health` resource |
| `Mcp/HealthReport.cs` | Data model, `IssueCode` enum, `ValidationSeverity` enum |
| `Mcp/DiscoveryIssueMapper.cs` | Static mapper: `DiscoveryInfo` --> `ValidationIssue[]` |

## Discovery-Time Issues (`DiscoveryIssueMapper`)

Static mapper that converts `DiscoveryInfo` fields into `ValidationIssue[]`. Issues are evaluated in order; some trigger an early exit (no further checks):

| IssueCode | Severity | Trigger | Early exit? |
|-----------|----------|---------|-------------|
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
| `HostCrashed` | Warning/Fatal | Connection state `Reconnecting` / `Degraded` |
| `HostUnreachable` | Warning | Started but not yet connected |
| `UpstreamError` | Fatal | Upstream task faulted |

## Non-Mapped `IssueCode` Values

Three codes exist in the `IssueCode` enum but are **not currently mapped** in CLI code -- reserved for Host-side or future use:

- `DotNetVersionUnsupported`
- `AddInLoadFailed`
- `AddInBinaryNotFound`

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
