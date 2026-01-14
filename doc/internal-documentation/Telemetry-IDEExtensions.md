# IDE Extensions Telemetry

IDE extensions track extension lifecycle, user interactions, and dev server operations.

## Visual Studio Code

**Event Name Prefix:** `uno/vscode`

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `extension-loaded` | `PluginVersion` (string), `IDE` (string) | - | Extension loaded successfully |
| `extension-unloaded` | `PluginVersion` (string), `IDE` (string) | - | Extension unloaded |
| `extension-failure` | `PluginVersion` (string), `IDE` (string), `Exception` (string), `Message` (string) | - | Extension failure occurred |
| `udei-opened` | `PluginVersion` (string), `IDE` (string) | - | Uno Design Experience Interface opened |
| `udei-action-clicked` | `PluginVersion` (string), `IDE` (string), `ActionName` (string) | - | Action clicked in UDEI |
| `dev-server-restart` | `PluginVersion` (string), `IDE` (string) | - | Dev Server restarted |

**Automatic Properties:**
- All events automatically include `PluginVersion` and `IDE` properties
- `IDE` property is automatically set to "vscode"

**App Launch Tracking:**
- Requires Uno.Sdk version 6.4.0 or higher
- Automatically tracks when apps are launched from VS Code

**Property Value Examples:**
- `PluginVersion`: "1.0.0", "1.2.5", "2.1.3"
- `IDE`: "vscode" (always)
- `Exception`: "Error", "TypeError", "NetworkError"
- `Message`: Sanitized error messages
- `ActionName`: "OpenDesigner", "RefreshPreview", "RestartDevServer", "OpenDocumentation"

**Reference:**
For more detailed information, see the [VS Code Extension Telemetry Documentation](https://github.com/unoplatform/uno.vscode/blob/main/documentation/Telemetry.md).

## Rider

**Event Name Prefix:** `uno/rider`

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `extension-loaded` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | Extension loaded successfully. Extension gets loaded when any project\solution is opened in IDE.  You can also see when it's triggered by "Telemetry event: extension-loaded" row in Uno Platform log |
| `extension-unloaded` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | Extension unloaded. Triggered when you're re-creating solution's, closing Rider, closing solution |
| `extension-failure` | `Exception` (string), `Message` (string) | - | Extension failure occurred. Triggered on exception\critical failure in any part of code. "Telemetry event: extension-failure." is written to the Uno Platform log when it occurs |
| `project-created` | `ProjectName` (string) | - | New Uno project created. Triggered when you create a solution with our Rider templates. "Telemetry event: project-created." is written to the Uno Platform log |
| `solution-build` | - | - | Solution built. Triggered every time solution is being built. "Telemetry event: solution-build" ." is written to the Uno Platform log |
| `debugger-launched` | - | - | Debugger launched successfully. Triggered when you launch the app with the debugger. "Telemetry event: debugger-launched" is written to the Uno Platform log |
| `no-debugger-launch` | - | - | Debugger launch skipped. Triggered when you launch the app without the debugger. "Telemetry event: no-debugger-launch" is written to the Uno Platform log |
| `udei-opened` | - | - | Uno Design Experience Interface opened |
| `udei-action-clicked` | `ActionName` (string) | - | Action clicked in UDEI |
| `dev-server-restart` | - | - | Dev Server restarted |

**Automatic Properties:**
- `IDE`: Always set to "rider"
- `IDEVersion`: Rider version
- `PluginVersion`: Uno Rider plugin version

**App Launch Tracking:**
- Automatically tracks when apps are launched from Rider
- Includes platform, debug mode, and IDE information

**Property Value Examples:**
- `PluginVersion`: "1.0.0", "1.3.2", "2.0.5"
- `IDE`: "rider" (always)
- `IDEVersion`: "2023.1", "2023.2.2", "2024.1"
- `Exception`: "NullPointerException", "IllegalStateException", "IOException"
- `Message`: Sanitized error messages
- `ProjectName`: "MyUnoApp", "CrossPlatformApp", "MobileProject"
- `ActionName`: "OpenDesigner", "RefreshPreview", "RestartDevServer", "ViewDocumentation"

**Reference:**
For more detailed information, see the [Rider Plugin Telemetry Documentation](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/Telemetry/Telemetry.md).

## Visual Studio

**Event Name Prefix:** `uno/visual-studio`

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `udei-opened` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | Uno Design Experience Interface opened |
| `udei-action-clicked` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string), `ActionName` (string) | - | Action clicked in UDEI |
| `udei-failure` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | UDEI failure occurred |
| `enumeration-fail` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | `Duration` (ms) | Project enumeration failed |
| `server-start-failure` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string), `ServerPackage` (string) | `Duration` (ms) | Dev Server start failed |
| `server-start-success` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string), `ServerPackage` (string), `ServerAPIPackage` (string) | `Duration` (ms) | Dev Server started successfully |
| `server-start-package-layout-failure` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string), `ServerPackage` (string) | `Duration` (ms) | Package layout failed during server start |
| `server-start-enumeration-exception` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string), `ExceptionType` (string) | `Duration` (ms) | Exception during enumeration at server start |

**Property Examples:**
- `PluginVersion`: "1.2.3"
- `IDE`: "visualstudio"
- `IDEVersion`: "17.8.4"
- `ActionName`: "OpenDesigner", "RefreshPreview", etc.
- `ServerPackage`: "Uno.WinUI.DevServer"
- `ServerAPIPackage`: "Uno.WinUI.DevServer.API"
- `ExceptionType`: Exception type name

**Reference:**
For more detailed information, see the [Visual Studio Extension Telemetry Documentation](https://github.com/unoplatform/uno.studio/blob/main/docs/Telemetry.md).
