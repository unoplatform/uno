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

## Reference

For more detailed information, see the [Uno Platform Telemetry Source](https://github.com/unoplatform/uno).
