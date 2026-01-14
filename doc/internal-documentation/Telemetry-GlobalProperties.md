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
- **Stack Traces**: Only exception types are logged, not full stack traces

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

## Reference

For component-specific telemetry properties, see:

- [Hot Design Telemetry](Telemetry-HotDesign.md) - SessionDuration and usage event properties
- [AI Features](Telemetry-AIFeatures.md) - Design thread and operation phase properties
- [Dev Server](Telemetry-DevServer.md) - Session and connection properties
- [Licensing](Telemetry-Licensing.md) - License-specific properties
- [IDE Extensions](Telemetry-IDEExtensions.md) - IDE and plugin version properties
- [App MCP](Telemetry-AppMCP.md) - Agent and platform properties
