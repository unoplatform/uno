# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry events emitted by the server (`ServerHotReloadProcessor.Notify`, and `HotReloadOutcomeTelemetry` for the outcomes of the dev-server's own hot reloads).

Event name prefix: uno/dev-server/hot-reload

| Event Name              | Main Properties  (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) |
|-------------------------|-------------------------------------------------------|---------------------------------------|
| **notify-start** [[src]](HotReload/ServerHotReloadProcessor.cs#L231)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-disabled** [[src]](HotReload/ServerHotReloadProcessor.cs#L240)         | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-initializing** [[src]](HotReload/ServerHotReloadProcessor.cs#L245)     | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-ready** [[src]](HotReload/ServerHotReloadProcessor.cs#L250)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-processing-files** [[src]](HotReload/ServerHotReloadProcessor.cs#L256) | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-completed** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L261) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L52) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-no-changes** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L266) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L52) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-failed** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L271) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L52) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-rude-edit** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L276) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L52) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-complete** [[src]](HotReload/ServerHotReloadProcessor.cs#L282)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs (optional)      |
| **notify-error** [[src]](HotReload/ServerHotReloadProcessor.cs#L291)            | Event, Source, PreviousState, NewState, HasCurrentOperation, ErrorMessage, ErrorType | FileCount, DurationMs (optional) |

## Hot Reload outcomes

The outcome events (`notify-completed`, `notify-no-changes`, `notify-failed`, `notify-rude-edit`) come from one of two sources. Their `Source` property tells them apart, so filter on it: counting these events by name adds up two populations.

- **`IDE`**: Visual Studio applied the hot reload and reported its outcome through the IDE channel. This is how an app built inside Visual Studio is hot reloaded. The event is framed by `notify-start` and `notify-complete`, like every `Notify()` event. Visual Studio reports a rude edit as `notify-start` with `Event=RudeEditDialogButton`, not as `notify-rude-edit`.
- **`DevServer`**: the dev-server applied the hot reload from its own workspace, as it does for an app not built inside Visual Studio (for example from VS Code, Rider or the command line). It sends one outcome event per hot-reload operation, without `notify-start` / `notify-complete`:
  - `notify-completed` means the update was produced and sent to the app, not that the app applied it.
  - `notify-failed` covers both a blocked update (`Result=Failed`) and a dev-server error (`Result=InternalError`).
  - `notify-no-changes` includes the passes where the dev-server sees again a change it has just applied.
  - An operation aborted by a newer one, or left without a result within 30 seconds, sends nothing.
  - Nothing is sent for a pass that ends with its workspace (the dev-server shutting down, or hot reload disabled for the connection).

The dev-server never sends an outcome for an app built inside Visual Studio, nor for an operation an IDE notification completes, so a hot reload is not counted twice.

## Property Value Examples

### String Properties
- **Event**: `"ProcessingFiles"`, `"Completed"`, ...
- **Source**: `"IDE"`, `"DevServer"`, ...
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **HasCurrentOperation**
- **Result**: `"Success"`, `"NoChanges"`, `"Failed"`, `"RudeEdit"`, `"InternalError"`
- **ErrorMessage**: `"Compilation failed"`, `"Syntax error"`
- **ErrorType**: `"CompilationException"`, `"SyntaxException"`

## Property Details
- **Event**: The type of event that triggered the notification
- **Source**: The source of the event
- **PreviousState**: The state before the operation
- **NewState**: The state after the operation (only present in notify-complete and notify-error)
- **HasCurrentOperation**: Indicates if a Hot Reload operation is in progress (only present in notify-complete and notify-error)
- **Result**: The result of the hot-reload operation (only present in outcome events with `Source=DevServer`)
- **FileCount**: Number of files affected by the operation (only present if there is a current operation)
- **DurationMs**: Duration of the operation in milliseconds, from its start to its outcome (only present if the operation has completed)
- **ErrorMessage**/**ErrorType**: Only present on notify-error events
