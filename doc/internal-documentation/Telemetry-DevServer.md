# Dev Server Telemetry

**Event Name Prefix:** `uno/devserver`

Dev Server telemetry tracks server sessions and client connections, including app launch tracking.

## Telemetry Session Types

| Session Type | Description |
|--------------|-------------|
| `Root` | The DevServer process itself |
| `Connection` | A specific client connection to the DevServer |

## Session Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | GUID | Unique session identifier |
| `SessionType` | String | Either "Root" or "Connection" |
| `ConnectionId` | String | Connection identifier (for Connection sessions) |
| `SolutionPath` | String | Optional path to the solution being served |

## App Launch Tracking

App launches are tracked via the `AppLaunchMessage` with the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `Mvid` | GUID | Module Version ID (unique per app build) |
| `Platform` | String | Target platform (e.g., WebAssembly, iOS, Android) |
| `IsDebug` | Boolean | Whether the app is a debug build |
| `Ide` | String | IDE being used (e.g., VisualStudio, VSCode, Rider) |
| `Plugin` | String | Plugin/extension name and version |
| `Step` | Enum | Launch step: `Launched` or `Connected` |

## Property Value Examples

Example values for Dev Server telemetry properties:

- **Id**: "8b3c9f45-7a21-4e68-b9d2-1f5c6a8d3e4f" (GUID format)
- **SessionType**: "Root", "Connection"
- **ConnectionId**: "conn_12345", "client_abc789"
- **SolutionPath**: "/Users/developer/Projects/MyApp/MyApp.sln", "C:\\Projects\\MyApp\\MyApp.sln" (hashed in actual telemetry)
- **Mvid**: "a1b2c3d4-e5f6-7890-abcd-ef1234567890" (GUID format)
- **Platform**: "WebAssembly", "iOS", "Android", "Windows", "macOS", "Linux", "Skia.Gtk", "Skia.Wpf"
- **IsDebug**: `true`, `false`
- **Ide**: "VisualStudio", "VSCode", "Rider", "Unknown"
- **Plugin**: "Uno.VSCode.Extension v1.2.3", "Uno.Rider.Plugin v2.0.1", "Uno.VisualStudio.Extension v1.5.0"
- **Step**: "Launched", "Connected"

## Reference

For more detailed information, see the [Uno Platform Telemetry Source](https://github.com/unoplatform/uno).
