# Global Telemetry Properties

This document describes the common properties that are automatically included with telemetry events across the Uno Platform ecosystem.

## Common Global Properties

These properties are included with most telemetry events across all Uno Platform components:

### System Information

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | DateTime | UTC timestamp when the event occurred |
| `OS` | String | Operating system name (Windows, macOS, Linux, Android, iOS) |
| `OSVersion` | String | Operating system version |
| `OSArchitecture` | String | System architecture (x64, x86, ARM, ARM64) |
| `KernelVersion` | String | Operating system kernel version |

### Environment Information

| Property | Type | Description |
|----------|------|-------------|
| `Culture` | String | Current system culture (e.g., en-US, fr-FR) |
| `IsCI` | Boolean | Whether running in a CI environment |
| `CIProvider` | String | CI provider name (Travis, Azure DevOps, AppVeyor, Jenkins, GitHub Actions, BitBucket, BuildKite, CodeBuild, Drone, MyGet, Space, TeamCity) |

### Uno Platform Version Information

| Property | Type | Description |
|----------|------|-------------|
| `UnoVersion` | String | Uno Platform package version |
| `UnoPlatformVersion` | String | Uno Platform SDK version |
| `TargetFramework` | String | Target framework (e.g., net10.0, net9.0-android, net10.0-ios) |
| `TargetFrameworks` | Array | All target frameworks in multi-targeted projects |

### IDE and Tooling Properties

| Property | Type | Description |
|----------|------|-------------|
| `IDE` | String | IDE being used (visualstudio, vscode, rider) |
| `IDEVersion` | String | Version of the IDE |
| `PluginVersion` | String | Version of the Uno Platform IDE extension/plugin |

### Application Information

| Property | Type | Description |
|----------|------|-------------|
| `SessionId` | GUID | Unique identifier for the current session |
| `UserId` | String | Anonymous, hashed user identifier (SHA256 of MAC address or stable token) |
| `MachineId` | String | Hashed (SHA256) MAC address for anonymous machine identification |

### Build and Project Information

| Property | Type | Description |
|----------|------|-------------|
| `ProjectType` | String | Type of project (app, library, test) |
| `IsDebug` | Boolean | Whether this is a debug build |
| `WorkingDirectory` | String | Hashed (SHA256) working directory path |

## Component-Specific Properties

Different Uno Platform components may include additional automatic properties:

### IDE Extensions

| Component | Automatic Properties |
|-----------|---------------------|
| **VS Code** | `IDE` (always "vscode"), `PluginVersion` |
| **Rider** | `IDE` (always "rider"), `IDEVersion`, `PluginVersion` |
| **Visual Studio** | `IDE` (always "visualstudio"), `IDEVersion`, `PluginVersion` |

### Hot Design

| Property | Description |
|----------|-------------|
| `SessionDuration` | Duration since Hot Design server session started (ms) |
| `UsageEvent` | Name of the client event being forwarded |

### Dev Server

| Property | Description |
|----------|-------------|
| `SessionType` | "Root" (DevServer process) or "Connection" (client connection) |
| `ConnectionId` | Unique connection identifier |
| `SolutionPath` | Path to solution being served (optional) |

### App MCP

| Property | Description |
|----------|-------------|
| `AgentType` | Type of agent (Claude, Copilot, Codex, etc.) |
| `AppPlatform` | Platform of running app (WebAssembly, iOS, Android, Windows, etc.) |

## Privacy and Anonymization

All global properties follow these privacy principles:

### What is Hashed/Anonymized

- **MAC Address**: Hashed with SHA256 to create stable, anonymous machine ID
- **Working Directory**: Hashed with SHA256 to avoid exposing file paths
- **User Tokens**: Hashed with SHA256 when used as user identifiers
- **Stack Traces**: For most telemetry, only exception types are logged, not full stack traces. However, certain diagnostic events (DevServer startup-failure, Licensing manager-failed) may include raw stack traces for troubleshooting purposes.

### What is NOT Collected

- **Personal Information**: No usernames, email addresses, or account names
- **File Paths**: Only hashed representations
- **Source Code**: No code content or project-specific information
- **Credentials**: No passwords, API keys, or tokens
- **Project Details**: No project names, repository names, or author information

## Application Insights Context

Telemetry is sent to Azure Application Insights with the following standard context properties:

### Cloud Context

| Property | Description |
|----------|-------------|
| `cloud.roleInstance` | Instance identifier |
| `cloud.role` | Component name (uno/hot-design, uno/devserver, etc.) |

### Device Context

| Property | Description |
|----------|-------------|
| `device.type` | Device type (Desktop, Mobile, Server) |
| `device.osVersion` | Operating system version |
| `device.locale` | System locale |

### Session Context

| Property | Description |
|----------|-------------|
| `session.id` | Application Insights session ID |
| `session.isFirst` | Whether this is the first session |

## Opt-Out

Users can disable telemetry collection globally by setting the environment variable:

```bash
# On macOS/Linux
export UNO_PLATFORM_TELEMETRY_OPTOUT=true

# On Windows (PowerShell)
$env:UNO_PLATFORM_TELEMETRY_OPTOUT = "true"

# On Windows (Command Prompt)
set UNO_PLATFORM_TELEMETRY_OPTOUT=true
```

Or via MSBuild property:

```xml
<PropertyGroup>
  <UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
</PropertyGroup>
```

See [Privacy & Compliance](Telemetry-Privacy.md) for more opt-out options specific to different tools.

## Property Value Examples

Example values for common global properties:

### System Information Examples

- **Timestamp**: "2024-01-14T18:45:36.272Z" (ISO 8601 format)
- **OS**: "Windows", "macOS", "Linux", "Android", "iOS"
- **OSVersion**: "Windows 11 22H2", "macOS 14.2", "Ubuntu 22.04", "Android 13", "iOS 17.2"
- **OSArchitecture**: "x64", "x86", "ARM", "ARM64"
- **KernelVersion**: "10.0.22621.2861", "23.2.0", "5.15.0-91-generic"

### Environment Information Examples

- **Culture**: "en-US", "fr-FR", "de-DE", "ja-JP", "es-ES"
- **IsCI**: `true`, `false`
- **CIProvider**: "GitHub Actions", "Azure DevOps", "Jenkins", "Travis", "AppVeyor"

### Version Information Examples

- **UnoVersion**: "5.1.0", "5.2.0-dev.123", "6.0.0-preview.1"
- **UnoPlatformVersion**: "5.1.0", "5.2.0", "6.0.0"
- **TargetFramework**: "net10.0", "net9.0-android", "net10.0-ios", "net9.0-windows10.0.19041"
- **TargetFrameworks**: ["net10.0", "net10.0-android", "net10.0-ios", "net10.0-windows10.0.19041"]

### IDE and Tooling Examples

- **IDE**: "visualstudio", "vscode", "rider"
- **IDEVersion**: "17.8.4", "1.85.1", "2023.3.2"
- **PluginVersion**: "1.2.3", "2.0.1", "3.1.0-beta.5"

### Application Information Examples

- **SessionId**: "3fa85f64-5717-4562-b3fc-2c963f66afa6" (GUID format)
- **UserId**: "a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8" (SHA256 hash, 64 hex characters)
- **MachineId**: "1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2" (SHA256 hash, 64 hex characters)

### Build Information Examples

- **ProjectType**: "app", "library", "test"
- **IsDebug**: `true`, `false`
- **WorkingDirectory**: "f4e3d2c1b0a9..." (SHA256 hash of actual path)

## Reference

For component-specific telemetry properties, see:

- [Hot Design Telemetry](Telemetry-HotDesign.md) - SessionDuration and usage event properties
- [AI Features](Telemetry-AIFeatures.md) - Design thread and operation phase properties
- [Dev Server](Telemetry-DevServer.md) - Session and connection properties
- [Licensing](Telemetry-Licensing.md) - License-specific properties
- [IDE Extensions](Telemetry-IDEExtensions.md) - IDE and plugin version properties
- [App MCP](Telemetry-AppMCP.md) - Agent and platform properties
