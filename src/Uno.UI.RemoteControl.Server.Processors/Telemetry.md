# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry events emitted by the server (`ServerHotReloadProcessor.Notify`, and `HotReloadOutcomeTelemetry` for the outcomes of the dev-server's own hot reloads).

Event name prefix: uno/dev-server/hot-reload

| Event Name              | Main Properties  (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) |
|-------------------------|-------------------------------------------------------|---------------------------------------|
| **notify-start** [[src]](HotReload/ServerHotReloadProcessor.cs#L227)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-disabled** [[src]](HotReload/ServerHotReloadProcessor.cs#L236)         | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-initializing** [[src]](HotReload/ServerHotReloadProcessor.cs#L241)     | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-ready** [[src]](HotReload/ServerHotReloadProcessor.cs#L246)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-processing-files** [[src]](HotReload/ServerHotReloadProcessor.cs#L252) | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-completed** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L257) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L72) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-no-changes** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L262) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L72) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-failed** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L267) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L72) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-rude-edit** [[IDE]](HotReload/ServerHotReloadProcessor.cs#L272) [[DevServer]](HotReload/HotReloadOutcomeTelemetry.cs#L72) | Event, Source, PreviousState (IDE); Event, Source, Result (DevServer) | FileCount, DurationMs (optional for IDE) |
| **notify-complete** [[src]](HotReload/ServerHotReloadProcessor.cs#L278)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs (optional)      |
| **notify-error** [[src]](HotReload/ServerHotReloadProcessor.cs#L287)            | Event, Source, PreviousState, NewState, HasCurrentOperation, ErrorMessage, ErrorType | FileCount, DurationMs (optional) |

## Hot Reload outcomes

The outcome events (`notify-completed`, `notify-no-changes`, `notify-failed`, `notify-rude-edit`) come from one of two sources. Their `Source` property tells them apart, so filter on it: counting these events by name adds up two populations.

- **`IDE`**: Visual Studio applied the hot reload and reported its outcome through the IDE channel. This is how an app built inside Visual Studio is hot reloaded. The event is framed by `notify-start` and `notify-complete`, like every `Notify()` event. Visual Studio reports a rude edit as `notify-start` with `Event=RudeEditDialogButton`, not as `notify-rude-edit`.
- **`DevServer`**: the dev-server applied the hot reload from its own workspace. The app picks that path when it was not built inside Visual Studio and runs without a debugger (Uno's VS Code debugger aside). So a VS Code, Rider or command-line run is measured, and an F5 debug session there is not. The dev-server sends one outcome event per hot-reload operation, without `notify-start` / `notify-complete`:
  - `notify-completed` means the update was produced and sent to the app, not that the app applied it.
  - `notify-failed` covers a blocked update (`Result=Failed`), a dev-server error (`Result=InternalError`), and an operation left without a result within 30 seconds (`Result=Timeout`).
  - `notify-no-changes` includes the passes where the dev-server sees again a change it has just applied.
  - An operation aborted by a newer one sends nothing: the newer one carries the outcome.
  - An operation whose files are all outside the solution (a log, `.DS_Store`) sends nothing: it is not a hot reload.
  - Nothing is sent for a pass that ends with its workspace (the dev-server shutting down, or hot reload disabled for the connection).

The dev-server never sends an outcome for an app built inside Visual Studio, nor for an operation an IDE notification starts or completes. A hot reload is therefore not counted twice.

## Property Value Examples

### String Properties
- **Event**: `"ProcessingFiles"`, `"Completed"`, ...
- **Source**: `"IDE"`, `"DevServer"`, ...
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **HasCurrentOperation**
- **Result**: `"Success"`, `"NoChanges"`, `"Failed"`, `"RudeEdit"`, `"InternalError"`, `"Timeout"`
- **ErrorMessage**: `"Compilation failed"`, `"Syntax error"`
- **ErrorType**: `"CompilationException"`, `"SyntaxException"`

## Property Details
- **Event**: The type of event that triggered the notification
- **Source**: The source of the event
- **PreviousState**: The state before the operation (absent from outcome events with `Source=DevServer`)
- **NewState**: The state after the operation (only present in notify-complete and notify-error)
- **HasCurrentOperation**: Indicates if a Hot Reload operation is in progress (only present in notify-complete and notify-error)
- **Result**: The result of the hot-reload operation (only present in outcome events with `Source=DevServer`)
- **FileCount**: Number of files affected by the operation (only present if there is a current operation). For outcome events with `Source=DevServer`, it is always present and counts only the files the solution knows.
- **DurationMs**: Duration of the operation in milliseconds, from its start to its outcome (only present if the operation has completed). For outcome events with `Source=DevServer`, it is always present and starts at the first file change, so it includes the file watcher's 250 ms wait for the batch to settle.
- **ErrorMessage**/**ErrorType**: Only present on notify-error events
