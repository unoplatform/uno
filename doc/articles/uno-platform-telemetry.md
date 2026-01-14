---
uid: Uno.Development.Telemetry
---

# Uno Platform Telemetry

The Uno Platform includes telemetry features that collect anonymous usage information to help improve the platform. This document provides a comprehensive overview of all telemetry collected across different Uno Platform components.

**All collected data is anonymous and does not contain personal information such as usernames, email addresses, or sensitive project data.**

## Telemetry Categories

Uno Platform telemetry is organized into the following categories:

### Uno Platform Core (`unoplatform/uno`)

- [Build Tools Telemetry](#build-tools-telemetry) - XAML code generation during compilation
- [Dev Server Telemetry](#dev-server-telemetry) - Development server operations and lifecycle
- [Hot Reload Telemetry](#hot-reload-telemetry) - Hot reload operations and status
- [App Launch Monitoring](#app-launch-monitoring) - Application launch tracking and connection monitoring

### Other Uno Platform Repositories

Telemetry may also be collected by other Uno Platform repositories. If you use these components, please refer to their respective documentation:

- **Uno.Extensions** (`unoplatform/uno.extensions`) - Enhanced application features and extensions
- **Uno.Themes** (`unoplatform/uno.themes`) - Material, Fluent, and Cupertino design system implementations
- **Uno.Toolkit** (`unoplatform/uno.toolkit.ui`) - Additional UI controls and utilities
- **Uno Hot Design** (Internal) - Visual design tools and features
- **Uno AI/MCP Features** - AI-assisted development tools

> [!NOTE]
> This document currently covers telemetry from the main `uno` repository. For telemetry information from other unoplatform repositories, please check their respective documentation or repository README files.

## How to Opt Out

All Uno Platform telemetry features can be disabled using one of the following methods:

### Global Opt-Out (All Components)

Set the environment variable:
```bash
UNO_PLATFORM_TELEMETRY_OPTOUT=1
```

Or set it to `true`:
```bash
UNO_PLATFORM_TELEMETRY_OPTOUT=true
```

### MSBuild Property Opt-Out

Add the following property to your project file:
```xml
<PropertyGroup>
  <UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
</PropertyGroup>
```

## Build Tools Telemetry

The Uno Platform SDK includes telemetry in the XAML code generation process that runs during compilation.

**Important:** Only the build tools collect telemetry. The compiled application itself does **not** collect any telemetry.

### Event Prefix
`uno/generation`

### Events

| Event Name | Properties | Measurements |
|------------|-----------|--------------|
| **generate-xaml-done** | None | Duration (seconds) |
| **generate-xaml-failed** | ExceptionType | Duration (seconds) |

### Data Collected

- Timestamp of generation invocation
- Duration of the generation step
- Exception type if generation fails
- CI environment detection (Travis, Azure DevOps, AppVeyor, Jenkins, GitHub Actions, BitBucket, Buildkite, Codebuild, Drone, MyGet, Space, TeamCity)
- Operating system name, version, kernel version, and architecture
- Current culture
- Uno Platform NuGet package version
- Target frameworks
- Hashed (SHA256) current working directory
- Hashed (SHA256) MAC address for machine identification

For more details, see: [Build Tools Telemetry Documentation](xref:Uno.Development.ToolchainTelemetry)

## Dev Server Telemetry

The Uno DevServer emits telemetry events for server lifecycle, add-in discovery, processor discovery, and client connections.

### Event Prefix
`uno/dev-server`

### Server Lifecycle Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **startup** | StartupHasSolution | None | Global |
| **shutdown** | ShutdownType | UptimeSeconds | Global |
| **startup-failure** | StartupErrorMessage, StartupErrorType, StartupStackTrace | UptimeSeconds | Global |
| **parent-process-lost** | None | None | Global |
| **parent-process-lost-forced-exit** | None | None | Global |

### Add-In Discovery Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **addin-discovery-start** | None | None | Global |
| **addin-discovery-complete** | DiscoveryResult, DiscoveryAddInList | DiscoveryAddInCount, DiscoveryDurationMs | Global |
| **addin-discovery-error** | DiscoveryErrorMessage, DiscoveryErrorType | DiscoveryDurationMs | Global |

### Add-In Loading Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **addin-loading-start** | AssemblyList | None | Global |
| **addin-loading-complete** | AssemblyList, Result | DurationMs, FailedAssemblies | Global |
| **addin-loading-error** | AssemblyList, ErrorMessage, ErrorType | DurationMs, FailedAssemblies | Global |

### Processor Discovery Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **processor-discovery-start** | AppInstanceId, DiscoveryIsFile | None | Per-connection |
| **processor-discovery-complete** | AppInstanceId, DiscoveryIsFile, DiscoveryResult, DiscoveryFailedProcessors | DiscoveryDurationMs, DiscoveryAssembliesProcessed, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount | Per-connection |
| **processor-discovery-error** | DiscoveryErrorMessage, DiscoveryErrorType | DiscoveryDurationMs, DiscoveryAssembliesCount, DiscoveryProcessorsLoadedCount, DiscoveryProcessorsFailedCount | Per-connection |

### Connection Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **client-connection-opened** | ConnectionId | None | Per-connection |
| **client-connection-closed** | ConnectionId | ConnectionDurationSeconds | Per-connection |

### Property Examples

- **StartupHasSolution**: `"True"`, `"False"`
- **ShutdownType**: `"Graceful"`, `"Crash"`
- **DiscoveryResult**: `"Success"`, `"PartialFailure"`, `"NoTargetFrameworks"`, `"NoAddInsFound"`
- **DiscoveryAddInList**: `"Uno.UI.App.Mcp.Server.dll;Uno.Settings.DevServer.dll"`
- **ConnectionId**: Anonymized connection identifier

**Note:** ErrorMessage and StackTrace properties may contain sensitive information and should be handled with care.

For more details, see: [DevServer Telemetry Documentation](../../src/Uno.UI.RemoteControl.Host/Telemetry.md)

## Hot Reload Telemetry

Hot Reload telemetry tracks the state and progress of hot reload operations in the dev server.

### Event Prefix
`uno/dev-server/hot-reload`

### Events

| Event Name | Properties | Measurements |
|------------|-----------|--------------|
| **notify-start** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-disabled** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-initializing** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-ready** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-processing-files** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-completed** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-no-changes** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-failed** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-rude-edit** | Event, Source, PreviousState | FileCount, DurationMs (optional) |
| **notify-complete** | Event, Source, PreviousState, NewState, HasCurrentOperation | FileCount, DurationMs (optional) |
| **notify-error** | Event, Source, PreviousState, NewState, HasCurrentOperation, ErrorMessage, ErrorType | FileCount, DurationMs (optional) |

### Property Examples

- **Event**: `"ProcessingFiles"`, `"Completed"`, `"Failed"`, `"RudeEdit"`
- **Source**: `"IDE"`, `"DevServer"`
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **HasCurrentOperation**: `"True"`, `"False"`
- **FileCount**: Number of files affected by the operation
- **DurationMs**: Duration of the operation in milliseconds

For more details, see: [Hot Reload Telemetry Documentation](../../src/Uno.UI.RemoteControl.Server.Processors/Telemetry.md)

## App Launch Monitoring

App Launch Monitoring tracks when applications are launched and when they successfully connect back to the dev server.

### Event Prefix
`uno/dev-server/app-launch`

### Events

| Event Name | Properties | Measurements | Scope |
|------------|-----------|--------------|-------|
| **app-launch/launched** | TargetPlatform, IsDebug, IDE, PluginVersion | None | Global |
| **app-launch/connected** | TargetPlatform, IsDebug, IDE, PluginVersion, WasTimedOut, WasIdeInitiated | LatencyMs | Global |
| **app-launch/connection-timeout** | TargetPlatform, IsDebug, IDE, PluginVersion | TimeoutSeconds | Global |

### Property Examples

- **TargetPlatform**: `"Desktop1.0"`, `"Android35.0"`, `"BrowserWasm1.0"`, `"iOS18.5"`
- **IsDebug**: `"True"`, `"False"`
- **IDE**: `"vswin"`, `"rider-2025.2.0.1"`, `"vscode-1.105.0"`, `"Unknown"`, `"None"`
- **PluginVersion**: `"1.0.0"`, `"2.1.5"`, `"Unknown"`, `"None"`
- **WasTimedOut**: `"True"`, `"False"`
- **WasIdeInitiated**: `"True"`, `"False"`

### Measurements

- **LatencyMs**: Time between app launch and connection in milliseconds
- **TimeoutSeconds**: Configured timeout duration

**Note:** MVID (Module Version ID) is used internally for matching but is NOT sent in telemetry events to preserve privacy.

For more details, see: [Application Launch Monitor Documentation](../../src/Uno.UI.RemoteControl.Server/AppLaunch/ApplicationLaunchMonitor.md)

## Data Storage and Security

All telemetry data is:

- Sent securely to Microsoft Azure Application Insights
- Stored under restricted access
- Published under strict security controls from secure Azure Storage systems
- Anonymous and does not contain personal identifiable information

## Privacy Considerations

The Uno Platform team is committed to protecting your privacy:

- No personal data (usernames, email addresses) is collected
- No sensitive project-level data (project names, repository names, authors) is collected
- Directory and machine identifiers are hashed using SHA256
- MVID (Module Version ID) is never transmitted in telemetry

## Reporting Issues

If you suspect that telemetry is collecting sensitive data or that data is being handled insecurely or inappropriately, please file an issue in the [unoplatform/uno](https://github.com/unoplatform/uno/issues) repository for investigation.

## Telemetry in Other Uno Platform Repositories

The following Uno Platform repositories may also collect telemetry. Please check their documentation for specific details:

### Uno.Extensions

**Repository:** [unoplatform/uno.extensions](https://github.com/unoplatform/uno.extensions)

Uno.Extensions provides enhanced application features including navigation, configuration, dependency injection, and more. Check the repository for telemetry documentation if applicable.

### Uno.Themes

**Repository:** [unoplatform/uno.themes](https://github.com/unoplatform/uno.themes)

Uno.Themes provides Material, Fluent, and Cupertino design system implementations. Check the repository for telemetry documentation if applicable.

### Uno.Toolkit

**Repository:** [unoplatform/uno.toolkit.ui](https://github.com/unoplatform/uno.toolkit.ui)

Uno.Toolkit provides additional UI controls such as Card, TabBar, NavigationBar, and more. Check the repository for telemetry documentation if applicable.

### Uno Hot Design / Studio

Hot Design and Studio features may collect telemetry related to visual design tools and development experience enhancements. Please refer to internal documentation or the Studio documentation for details.

### AI and MCP Features

AI-assisted development features and Model Context Protocol (MCP) implementations may collect telemetry to improve AI-powered tools. Check the relevant feature documentation for details.

> [!IMPORTANT]
> If you use components from multiple Uno Platform repositories, each component's telemetry can be disabled using the same opt-out methods described in the [How to Opt Out](#how-to-opt-out) section. The `UNO_PLATFORM_TELEMETRY_OPTOUT` environment variable should disable telemetry across all Uno Platform components.

## Contributing Telemetry Documentation

If you maintain an Uno Platform repository that collects telemetry:

1. Create a `Telemetry.md` file in your repository documenting:
   - Event names and prefixes
   - Properties and measurements collected
   - Privacy considerations
   - Opt-out instructions
2. Link to this centralized documentation for consistency
3. Submit updates to this document to include your repository's telemetry summary

## See Also

- [Build Tools Telemetry](xref:Uno.Development.ToolchainTelemetry)
- [DevServer Telemetry](../../src/Uno.UI.RemoteControl.Host/Telemetry.md)
- [Hot Reload Telemetry](../../src/Uno.UI.RemoteControl.Server.Processors/Telemetry.md)
- [Application Launch Monitor](../../src/Uno.UI.RemoteControl.Server/AppLaunch/ApplicationLaunchMonitor.md)
- [.NET Core Telemetry](https://learn.microsoft.com/dotnet/core/tools/telemetry)
- [Uno.Extensions Repository](https://github.com/unoplatform/uno.extensions)
- [Uno.Themes Repository](https://github.com/unoplatform/uno.themes)
- [Uno.Toolkit Repository](https://github.com/unoplatform/uno.toolkit.ui)
