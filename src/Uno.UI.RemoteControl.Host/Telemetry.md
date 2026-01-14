# DevServer Telemetry Events

This document provides a comprehensive inventory of all telemetry events emitted by the Uno DevServer, with their properties and measurements, for GDPR/privacy review.

> [!TIP]
> For a complete overview of all Uno Platform telemetry, see the [Uno Platform Telemetry](../../doc/articles/uno-platform-telemetry.md) documentation.

## Event Prefix

All DevServer telemetry events use the prefix: `uno/dev-server`

## Opt-Out

To disable DevServer telemetry, set the environment variable:
```bash
UNO_PLATFORM_TELEMETRY_OPTOUT=1
```

Or use the MSBuild property:
```xml
<UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
```

## DevServer Lifecycle Events

Events related to the DevServer startup, shutdown, and parent process monitoring.

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **startup** [[src]](Program.cs#L187)                         | StartupHasSolution                                                        |                                                                                                                   | Indicates whether a solution file was provided at startup                           | Global         |
| **shutdown** [[src]](Program.cs#L211)                        | ShutdownType                                         | UptimeSeconds                                                                                                     | Tracks server uptime and shutdown reason (Graceful or Crash)                        | Global         |
| **startup-failure** [[src]](Program.cs#L233)                 | StartupErrorMessage, StartupErrorType, StartupStackTrace                  | UptimeSeconds                                                                                                     | **Warning:** ErrorMessage/StackTrace may contain sensitive information              | Global         |
| **parent-process-lost** [[src]](ParentProcessObserver.cs#L38)             |                                                                           |                                                                                                                   | Emitted when parent process is lost; graceful shutdown is attempted                 | Global         |
| **parent-process-lost-forced-exit** [[src]](ParentProcessObserver.cs#L46) |                                                                           |                                                                                                                   | Emitted if forced exit after graceful shutdown timeout                              | Global         |

## Add-In Discovery and Loading Events

Events related to discovering and loading DevServer add-ins and extensions.

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **addin-discovery-start** [[src]](Extensibility/AddIns.cs#L24)                 |                                                                           |                                                                                                                   | Start of add-in discovery process                                                   | Global         |
| **addin-discovery-complete** [[src]](Extensibility/AddIns.cs#L147)              | DiscoveryResult, DiscoveryAddInList                                       | DiscoveryAddInCount, DiscoveryDurationMs                                                                          | AddInList contains filenames only, no full paths                                    | Global         |
| **addin-discovery-error** [[src]](Extensibility/AddIns.cs#L126)                 | DiscoveryErrorMessage, DiscoveryErrorType                                 | DiscoveryDurationMs                                                                                               | **Warning:** ErrorMessage may contain sensitive information                         | Global         |
| **addin-loading-start** [[src]](Helpers/AssemblyHelper.cs#L23)                   | AssemblyList                                                              |                                                                                                                   | AssemblyList contains filenames only, no full paths                                 | Global         |
| **addin-loading-complete** [[src]](Helpers/AssemblyHelper.cs#L65)                | AssemblyList, Result                                                      | DurationMs, FailedAssemblies                                                                                      | Tracks successful and failed add-in loading                                         | Global         |
| **addin-loading-error** [[src]](Helpers/AssemblyHelper.cs#L83)                   | AssemblyList, ErrorMessage, ErrorType                                     | DurationMs, FailedAssemblies                                                                                      | **Warning:** ErrorMessage may contain sensitive information                         | Global         |

## Processor Discovery Events

Events related to discovering and loading message processors for connected applications.

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **processor-discovery-start** [[src]](RemoteControlServer.cs#L407)                 | AppInstanceId, DiscoveryIsFile                                            |                                                                                                                   | Per-connection processor discovery initiated                                        | Per-connection |
| **processor-discovery-complete** [[src]](RemoteControlServer.cs#L579)              | AppInstanceId, DiscoveryIsFile, DiscoveryResult, DiscoveryFailedProcessors | DiscoveryDurationMs, DiscoveryAssembliesProcessed, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount | FailedProcessors contains comma-separated type names                                | Per-connection |
| **processor-discovery-error** [[src]](RemoteControlServer.cs#L603)                 | DiscoveryErrorMessage, DiscoveryErrorType                                 | DiscoveryDurationMs, DiscoveryAssembliesCount, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount     | **Warning:** ErrorMessage may contain sensitive information                         | Per-connection |

## Client Connection Events

Events related to client application connections to the DevServer.

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **client-connection-opened** [[src]](RemoteControlExtensions.cs#L92)               | ConnectionId                                                              |                                                                                                                   | Connection metadata is anonymized                                                   | Per-connection |
| **client-connection-closed** [[src]](RemoteControlExtensions.cs#L139)               | ConnectionId                                                              | ConnectionDurationSeconds                                                                                         | Tracks duration of client connection                                                | Per-connection |

## App Launch Events

Events related to application launch tracking and monitoring. These events track when an IDE or DevServer launches an app and when the app successfully connects.

| Event Name                          | Properties (string, no prefix)                                            | Measurements (double, with prefixes)                                                                              | Sensitive / Notes                                                                   | Scope          |
|-------------------------------------|---------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|----------------|
| **app-launch/launched** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L48)                    | TargetPlatform, IsDebug, IDE, PluginVersion                                              |                                                                                                                   | MVID not sent for privacy; tracks app launch registration                          | Global         |
| **app-launch/connected** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L59)                   | TargetPlatform, IsDebug, IDE, PluginVersion, WasTimedOut, WasIdeInitiated                                              | LatencyMs                                                                                                         | MVID not sent for privacy; tracks successful app connection                         | Global         |
| **app-launch/connection-timeout** [[src]](../Uno.UI.RemoteControl.Server/Helpers/ServiceCollectionExtensions.cs#L73)          | TargetPlatform, IsDebug, IDE, PluginVersion                                              | TimeoutSeconds                                                                                                    | MVID not sent for privacy; tracks app launch timeout                                | Global         |

## Property Value Examples

The following are example values for string properties in telemetry events:

### String Properties

- **StartupHasSolution**: `"True"`, `"False"` - Whether a solution file was provided at startup
- **ShutdownType**: `"Graceful"`, `"Crash"` - How the server shutdown occurred
- **StartupErrorMessage**: `"Failed to bind to address http://[::]:52186: address already in use"`, `"Unable to resolve service for type 'Microsoft.Extensions.Configuration.IConfiguration'"` - Error message during startup failure
- **StartupErrorType**: `"MissingMethodException"`, `"IOException"`, `"InvalidOperationException"` - Exception type during startup failure
- **StartupStackTrace**: `"at Program.Main(String[] args) in Program.cs:line 123"` - Stack trace during startup failure
- **DiscoveryResult**: `"Success"`, `"PartialFailure"`, `"NoTargetFrameworks"`, `"NoAddInsFound"` - Result of add-in or processor discovery
- **DiscoveryAddInList**: `"Uno.UI.App.Mcp.Server.dll;Uno.Settings.DevServer.dll"` - Semicolon-separated list of discovered add-in filenames
- **DiscoveryIsFile**: `"True"`, `"False"` - Whether discovery was from a file or directory
- **DiscoveryErrorMessage**: `"Directory not found"`, `"Access denied"` - Error message during discovery
- **DiscoveryErrorType**: `"DirectoryNotFoundException"`, `"UnauthorizedAccessException"` - Exception type during discovery
- **ErrorMessage**: `"Assembly load failed"`, `"Type not found"` - Generic error message
- **ErrorType**: `"FileLoadException"`, `"TypeLoadException"` - Generic exception type
- **AppInstanceId**: `"abc123-def456"`, `"instance-789"` - Anonymized application instance identifier
- **DiscoveryFailedProcessors**: Comma-separated list of processor type names that failed to load
- **ConnectionId**: `"conn-abc123"`, `"conn-xyz789"` - Anonymized connection identifier
- **TargetPlatform**: `"Desktop1.0"`, `"Android35.0"`, `"BrowserWasm1.0"`, `"iOS18.5"` - Target platform identifier
- **IsDebug**: `"True"`, `"False"` - Whether the app is a debug build
- **IDE**: `"vswin"`, `"rider-2025.2.0.1"`, `"vscode-1.105.0"`, `"Unknown"`, `"None"` - IDE that launched the app
- **PluginVersion**: `"1.0.0"`, `"2.1.5"`, `"Unknown"`, `"None"` - Version of the Uno Platform IDE plugin
- **WasTimedOut**: `"True"`, `"False"` - Whether the connection attempt timed out
- **WasIdeInitiated**: `"True"`, `"False"` - Whether the launch was initiated by the IDE

## Measurement Value Types

All measurements are double-precision floating-point values:

- **UptimeSeconds**: Server uptime in seconds
- **DiscoveryAddInCount**: Number of add-ins discovered
- **DiscoveryDurationMs**: Duration of discovery operation in milliseconds
- **DurationMs**: Duration of operation in milliseconds
- **FailedAssemblies**: Count of assemblies that failed to load
- **DiscoveryAssembliesProcessed**: Number of assemblies processed during discovery
- **DiscoveryProcessorsLoadedCount**: Number of processors successfully loaded
- **DiscoveryProcessorsFailedCount**: Number of processors that failed to load
- **ConnectionDurationSeconds**: Duration of connection in seconds
- **LatencyMs**: Latency between app launch and connection in milliseconds
- **TimeoutSeconds**: Configured timeout duration in seconds

## Privacy and Security Notes

- **ErrorMessage** and **StackTrace** properties are sent as raw values and **may contain sensitive information**. Handle with care.
- All identifiers (ConnectionId, AppInstanceId) are anonymized or generated values, not real user or system identifiers.
- File lists contain only filenames, not full paths, to avoid exposing directory structures.
- **MVID (Module Version ID)** is used internally for app launch matching but is **never transmitted in telemetry events** to preserve privacy.

## See Also

- [Uno Platform Telemetry Overview](../../doc/articles/uno-platform-telemetry.md)
- [Hot Reload Telemetry](../Uno.UI.RemoteControl.Server.Processors/Telemetry.md)
- [Application Launch Monitor](../Uno.UI.RemoteControl.Server/AppLaunch/ApplicationLaunchMonitor.md)
- [Build Tools Telemetry](../../doc/articles/uno-toolchain-telemetry.md)

