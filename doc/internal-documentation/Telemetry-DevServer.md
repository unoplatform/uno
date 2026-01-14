# Dev Server Telemetry

**Event Name Prefix:** `uno/dev-server`

Dev Server telemetry tracks server sessions and client connections, including app launch tracking, add-in discovery, processor loading, and connection lifecycle.

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

## DevServer Lifecycle Events

### Startup and Shutdown Events

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `startup` | `StartupHasSolution` (bool) | - | Global | DevServer started and is ready to receive connections |
| `shutdown` | `ShutdownType` (string) | `UptimeSeconds` (double) | Global | DevServer shutdown. This is the last event before `parent-process-lost` |
| `startup-failure` | `StartupErrorMessage` (string), `StartupErrorType` (string), `StartupStackTrace` (string) | `UptimeSeconds` (double) | Global | A catastrophic error occurred with the dev server (not only during startup) |
| `parent-process-lost` | - | - | Global | Parent process (process that started the dev server (i.e. IDE)) lost, graceful shutdown attempted. If unable to gracefully shut down, the process is killed and `parent-process-lost-forced-exit` event will be triggered |
| `parent-process-lost-forced-exit` | - | - | Global | Forced exit after graceful shutdown timeout |

### Add-In Discovery Events

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `addin-discovery-start` | - | - | Global | Add-in discovery started. An add-in is a service offered by the dev server global to the solution (can interact with the IDE) |
| `addin-discovery-complete` | `DiscoveryResult` (string), `DiscoveryAddInList` (string) | `DiscoveryAddInCount` (double), `DiscoveryDurationMs` (double) | Global | Add-in discovery completed |
| `addin-discovery-error` | `DiscoveryErrorMessage` (string), `DiscoveryErrorType` (string) | `DiscoveryDurationMs` (double) | Global | Add-in discovery failed |

### Add-In Loading Events

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `addin-loading-start` | `AssemblyList` (string) | - | Global | Add-in loading started |
| `addin-loading-complete` | `AssemblyList` (string), `Result` (string) | `DurationMs` (double), `FailedAssemblies` (double) | Global | Add-in loading completed |
| `addin-loading-error` | `AssemblyList` (string), `ErrorMessage` (string), `ErrorType` (string) | `DurationMs` (double), `FailedAssemblies` (double) | Global | Add-in loading failed |

### Processor Discovery Events

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `processor-discovery-start` | `AppInstanceId` (string), `DiscoveryIsFile` (bool) | - | Per-connection | Processor discovery started for connection, A processor is an agent that the client (connected application) asks the dev server to load.|
| `processor-discovery-complete` | `AppInstanceId` (string), `DiscoveryIsFile` (bool), `DiscoveryResult` (string), `DiscoveryFailedProcessors` (string) | `DiscoveryDurationMs` (double), `DiscoveryAssembliesProcessed` (double), `DiscoveryProcessorsLoadedCount` (double), `DiscoveryProcessorsFailedCount` (double) | Per-connection | Processor discovery completed |
| `processor-discovery-error` | `DiscoveryErrorMessage` (string), `DiscoveryErrorType` (string) | `DiscoveryDurationMs` (double), `DiscoveryAssembliesCount` (double), `DiscoveryProcessorsLoadedCount` (double), `DiscoveryProcessorsFailedCount` (double) | Per-connection | Processor discovery failed |

### Connection Events

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `client-connection-opened` | `ConnectionId` (string) | - | Per-connection | Client connection opened (metadata anonymized) and connected to the dev server |
| `client-connection-closed` | `ConnectionId` (string) | `ConnectionDurationSeconds` (double) | Per-connection | Client connection closed |

## App Launch Tracking

App launches are tracked via the `AppLaunchMessage` with the following properties:

| Event Name | Properties | Measurements | Scope | Description |
|------------|-----------|--------------|-------|-------------|
| `app-launch/launched` | `TargetPlatform` (string), `IsDebug` (string), `IDE` (string), `PluginVersion` (string) | - | Global | App launched (no MVID sent) |
| `app-launch/connected` | `TargetPlatform` (string), `IsDebug` (string), `IDE` (string), `PluginVersion` (string), `WasTimedOut` (string), `WasIdeInitiated` (string) | `LatencyMs` (double) | Global | App connected to DevServer |
| `app-launch/connection-timeout` | `TargetPlatform` (string), `IsDebug` (string), `IDE` (string), `PluginVersion` (string) | `TimeoutSeconds` (double) | Global | App connection timed out |

## Hot Reload Events

**Event Name Prefix:** `uno/dev-server/hot-reload`

Hot Reload processor events track the state transitions and operations of the Hot Reload system:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `notify-start` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload notification started |
| `notify-disabled` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload is unable to initialize and has been disabled |
| `notify-initializing` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload is initializing. |
| `notify-ready` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload is ready |
| `notify-processing-files` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload is processing files. This event will only be triggered when using Rider, VS Code or when Hot Design modifies a file on disk |
| `notify-completed` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload operation completed (i.e., the modifications have been successfully applied) |
| `notify-no-changes` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | No changes detected that have an impact on the application |
| `notify-failed` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload operation failed |
| `notify-rude-edit` | `Event` (string), `Source` (string), `PreviousState` (string) | `FileCount` (double), `DurationMs` (double, optional) | Rude edit detected (requires restart). Code is OK and could compile, but (Rosalyn) .NET compiler cannot create a diff to update the code and compile.|
| `notify-complete` | `Event` (string), `Source` (string), `PreviousState` (string), `NewState` (string), `HasCurrentOperation` (bool) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload state transition complete |
| `notify-error` | `Event` (string), `Source` (string), `PreviousState` (string), `NewState` (string), `HasCurrentOperation` (bool), `ErrorMessage` (string), `ErrorType` (string) | `FileCount` (double), `DurationMs` (double, optional) | Hot Reload error occurred; usually when the parent dev-server encountered a catastrophic error |

**Property Notes:**
- All properties are prefixed with `hotreload/` in the actual telemetry
- All measurements are prefixed with `hotreload/` in the actual telemetry
- **Event**: Type of event that triggered the notification
- **Source**: Source of the event (IDE, DevServer, etc.)
- **PreviousState/NewState**: State before/after the operation (Ready, Disabled, Initializing, Processing)
- **HasCurrentOperation**: Indicates if a Hot Reload operation is in progress
- **FileCount**: Number of files affected (only present if there is a current operation)
- **DurationMs**: Duration of the operation in milliseconds (only present if operation has completed)

### Legacy App Launch Properties

| Property | Type | Description |
|----------|------|-------------|
| `Mvid` | GUID | Module Version ID (unique per app build) - Legacy, not sent in newer events |
| `Platform` | String | Target platform (e.g., WebAssembly, iOS, Android) |
| `IsDebug` | Boolean | Whether the app is a debug build |
| `Ide` | String | IDE being used (e.g., VisualStudio, VSCode, Rider) |
| `Plugin` | String | Plugin/extension name and version |
| `Step` | Enum | Launch step: `Launched` or `Connected` |

## Property Value Examples

Example values for Dev Server telemetry properties:

### Session Properties
- **Id**: "8b3c9f45-7a21-4e68-b9d2-1f5c6a8d3e4f" (GUID format)
- **SessionType**: "Root", "Connection"
- **ConnectionId**: "conn_12345", "client_abc789", "conn-abc123", "conn-xyz789"
- **SolutionPath**: "/Users/developer/Projects/MyApp/MyApp.sln", "C:\\Projects\\MyApp\\MyApp.sln" (hashed in actual telemetry)

### Startup/Shutdown Properties
- **StartupHasSolution**: `true`, `false`
- **ShutdownType**: "Graceful", "Crash"
- **StartupErrorMessage**: "Failed to bind to address http://[::]:52186: address already in use", "Unable to resolve service"
- **StartupErrorType**: "MissingMethodException", "IOException", "InvalidOperationException"
- **StartupStackTrace**: "at Program.Main(String[] args) in Program.cs:line 123" (raw stack traces may contain sensitive info)

### Discovery Properties
- **DiscoveryResult**: "Success", "PartialFailure", "NoTargetFrameworks", "NoAddInsFound"
- **DiscoveryAddInList**: "Uno.UI.App.Mcp.Server.dll;Uno.Settings.DevServer.dll" (filenames only)
- **DiscoveryIsFile**: `true`, `false`
- **DiscoveryErrorMessage**: "Directory not found", "Access denied"
- **DiscoveryErrorType**: "DirectoryNotFoundException", "UnauthorizedAccessException"
- **DiscoveryFailedProcessors**: "Processor1,Processor2" (comma-separated type names)

### Loading Properties
- **AssemblyList**: "Assembly1.dll;Assembly2.dll" (filenames only)
- **Result**: "Success", "PartialFailure", "Failed"
- **ErrorMessage**: "Assembly load failed", "Type not found"
- **ErrorType**: "FileLoadException", "TypeLoadException"

### App Instance Properties
- **AppInstanceId**: "abc123-def456", "instance-789"

### App Launch Properties
- **TargetPlatform**: "Desktop1.0", "Android35.0", "BrowserWasm1.0", "iOS18.5"
- **IsDebug**: "True", "False" (string format in newer events)
- **IDE**: "vswin", "rider-2025.2.0.1", "vscode-1.105.0", "Unknown", "None"
- **PluginVersion**: "1.0.0", "2.1.5", "Unknown", "None"
- **WasTimedOut**: "True", "False"
- **WasIdeInitiated**: "True", "False"

### Legacy Properties (older events)
- **Mvid**: "a1b2c3d4-e5f6-7890-abcd-ef1234567890" (GUID format, not sent in newer events)
- **Platform**: "WebAssembly", "iOS", "Android", "Windows", "macOS", "Linux", "Skia.Gtk", "Skia.Wpf"
- **IsDebug**: `true`, `false` (boolean format)
- **Ide**: "VisualStudio", "VSCode", "Rider", "Unknown"
- **Plugin**: "Uno.VSCode.Extension v1.2.3", "Uno.Rider.Plugin v2.0.1", "Uno.VisualStudio.Extension v1.5.0"
- **Step**: "Launched", "Connected"

### Hot Reload Properties
- **Event** (Hot Reload): "ProcessingFiles", "Completed", "NoChanges", "Failed", "RudeEdit"
- **Source** (Hot Reload): "IDE", "DevServer", "FileWatcher"
- **PreviousState** / **NewState**: "Ready", "Disabled", "Initializing", "Processing"
- **HasCurrentOperation**: `true`, `false`
- **ErrorMessage** (Hot Reload): "Compilation failed", "Syntax error in file", "Type not found"
- **ErrorType** (Hot Reload): "CompilationException", "SyntaxException", "TypeLoadException"

## Privacy Notes

- **ErrorMessage** and **StackTrace** properties are sent as raw values and may contain sensitive information; handle with care
- **ConnectionId** and metadata fields are anonymized
- **MVID** (Module Version ID) is not sent in newer app launch events to protect privacy
- **AddInList** and **AssemblyList** contain filenames only, not full paths

## Reference

For more detailed information, see the [Uno Platform Telemetry Source](https://github.com/unoplatform/uno).
