# DevServer Telemetry Events Inventory

This table lists all telemetry events emitted by the Uno DevServer, with their properties and measurements, for GDPR/privacy review. The last column indicates if the event is global (server-wide) or per-connection.

**Note:** Starting with Uno.DevTools.Telemetry v1.3.0, exceptions are tracked using the `TrackException` API instead of encoding exception details (ErrorMessage, ErrorType, StackTrace) as properties in `TrackEvent` calls. This provides better grouping, analytics, and diagnostics in Application Insights and other telemetry backends.

Event name prefix: uno/dev-server

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **startup** [[src]](Program.cs#L187)                         | StartupHasSolution                                                        |                                                                                                                   |                                                                                     | Global         |
| **shutdown** [[src]](Program.cs#L211)                        | ShutdownType                                         | UptimeSeconds                                                                                                     |                                                                                     | Global         |
| **startup-failure** [[src]](Program.cs#L233)                 | *(exception tracked via TrackException)*                  | UptimeSeconds                                                                                                     | Exception details captured via TrackException API                           | Global         |
| **parent-process-lost** [[src]](ParentProcessObserver.cs#L38)             |                                                                           |                                                                                                                   | Emitted when parent process is lost, graceful shutdown is attempted. No properties. | Global         |
| **parent-process-lost-forced-exit** [[src]](ParentProcessObserver.cs#L46) |                                                                           |                                                                                                                   | Emitted if forced exit after graceful shutdown timeout. No properties.              | Global         |
| **addin-discovery-start** [[src]](Extensibility/AddIns.cs#L24)                 |                                                                           |                                                                                                                   |                                                                                     | Global         |
| **addin-discovery-complete** [[src]](Extensibility/AddIns.cs#L147)              | DiscoveryResult, DiscoveryAddInList                                       | DiscoveryAddInCount, DiscoveryDurationMs                                                                          | AddInList: filenames only                                                           | Global         |
| **addin-discovery-error** [[src]](Extensibility/AddIns.cs#L126)                 | *(exception tracked via TrackException)*                                 | DiscoveryDurationMs                                                                                               | Exception details captured via TrackException API                                      | Global         |
| **addin-loading-start** [[src]](Helpers/AssemblyHelper.cs#L23)                   | AssemblyList                                                              |                                                                                                                   | AssemblyList: filenames only                                                        | Global         |
| **addin-loading-complete** [[src]](Helpers/AssemblyHelper.cs#L65)                | AssemblyList, Result                                                      | DurationMs, FailedAssemblies                                                                                      |                                                                                     | Global         |
| **addin-loading-error** [[src]](Helpers/AssemblyHelper.cs#L83)                   | *(exception tracked via TrackException)*                                     | DurationMs, FailedAssemblies                                                                                      | Exception details captured via TrackException API                                      | Global         |
| **processor-discovery-start** [[src]](RemoteControlServer.cs#L407)                 | AppInstanceId, DiscoveryIsFile                                            |                                                                                                                   |                                                                                     | Per-connection |
| **processor-discovery-complete** [[src]](RemoteControlServer.cs#L579)              | AppInstanceId, DiscoveryIsFile, DiscoveryResult, DiscoveryFailedProcessors | DiscoveryDurationMs, DiscoveryAssembliesProcessed, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount | FailedProcessors: comma-separated type names                                        | Per-connection |
| **processor-discovery-error** [[src]](RemoteControlServer.cs#L603)                 | *(exception tracked via TrackException)*                                 | DiscoveryDurationMs, DiscoveryAssembliesCount, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount     | Exception details captured via TrackException API                                      | Per-connection |
| **client-connection-opened** [[src]](RemoteControlExtensions.cs#L92)               | ConnectionId                                                              |                                                                                                                   | Metadata fields are anonymized                                                      | Per-connection |
| **client-connection-closed** [[src]](RemoteControlExtensions.cs#L139)               | ConnectionId                                                              | ConnectionDurationSeconds                                                                                         |                                                                                     | Per-connection |
| **app-launch/launched** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L48)                    | TargetPlatform, IsDebug, IDE, PluginVersion                                              |                                                                                                                   | No identifiers (MVID not sent)                                                      | Global         |
| **app-launch/connected** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L59)                   | TargetPlatform, IsDebug, IDE, PluginVersion, WasTimedOut, WasIdeInitiated                                              | LatencyMs                                                                                                         | No identifiers (MVID not sent)                                                      | Global         |
| **app-launch/connection-timeout** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L73)          | TargetPlatform, IsDebug, IDE, PluginVersion                                              | TimeoutSeconds                                                                                                    | No identifiers (MVID not sent)                                                      | Global         |

## Property Value Examples

### String Properties
- **StartupHasSolution**
- **ShutdownType**: `"Graceful"`, `"Crash"`
- **DiscoveryResult**: `"Success"`, `"PartialFailure"`, `"NoTargetFrameworks"`, `"NoAddInsFound"`
- **DiscoveryAddInList**: `"Uno.UI.App.Mcp.Server.dll;Uno.Settings.DevServer.dll"`
- **DiscoveryIsFile**
- **AppInstanceId**: `"abc123-def456"`, `"instance-789"`
- **DiscoveryIsFile**
- **DiscoveryFailedProcessors**
- **ConnectionId**: `"conn-abc123"`, `"conn-xyz789"`
- **TargetPlatform**: `"Desktop1.0"`, `"Android35.0"`, `"BrowerWasm1.0"`, `"iOS18.5"`...
- **IsDebug**: `"True"`, `"False"`
- **IDE**: `"vswin"`, `"rider-2025.2.0.1"`, `"vscode-1.105.0"`, `"Unknown"`, `"None"`
- **PluginVersion**: `"1.0.0"`, `"2.1.5"`, `"Unknown"`, `"None"`
- **WasTimedOut**: `"True"`, `"False"`
- **WasIdeInitiated**: `"True"`, `"False"`

## Notes
- Exception details (message, type, stack trace) are now automatically captured by the TrackException API.
- TrackException provides better grouping and analytics in Application Insights compared to encoding exceptions as event properties.
- Exceptions are handled with care to avoid exposing sensitive information beyond what's needed for diagnostics.

