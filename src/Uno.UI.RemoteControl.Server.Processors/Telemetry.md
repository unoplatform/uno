# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry events emitted by the server (`ServerHotReloadProcessor.Notify`).

Event name prefix: uno/dev-server/hot-reload

| Event Name              | Main Properties  (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) |
|-------------------------|-------------------------------------------------------|---------------------------------------|
| **notify-start** [[src]](HotReload/ServerHotReloadProcessor.cs#L158)            | Event, Source, PreviousState                          | FileCount                             |
| **notify-disabled** [[src]](HotReload/ServerHotReloadProcessor.cs#L168)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount                             |
| **notify-initializing** [[src]](HotReload/ServerHotReloadProcessor.cs#L174)     | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount                             |
| **notify-ready** [[src]](HotReload/ServerHotReloadProcessor.cs#L180)            | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount                             |
| **notify-processing-files** [[src]](HotReload/ServerHotReloadProcessor.cs#L186) | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount                             |
| **notify-completed** [[src]](HotReload/ServerHotReloadProcessor.cs#L191)        | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs                 |
| **notify-no-changes** [[src]](HotReload/ServerHotReloadProcessor.cs#L196)       | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs                 |
| **notify-failed** [[src]](HotReload/ServerHotReloadProcessor.cs#L201)           | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs                 |
| **notify-rude-edit** [[src]](HotReload/ServerHotReloadProcessor.cs#L206)        | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs                 |
| **notify-complete** [[src]](HotReload/ServerHotReloadProcessor.cs#L212)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs                 |
| **notify-error** [[src]](HotReload/ServerHotReloadProcessor.cs#L221)            | Event, Source, PreviousState, ErrorMessage, ErrorType | FileCount, DurationMs                 |

## Property Value Examples

### String Properties
- **Event**: `"ProcessingFiles"`, `"Completed"`, ...
- **Source**: `"IDE"`, `"DevServer"`, ...
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **HasCurrentOperation**
- **ErrorMessage**: `"Compilation failed"`, `"Syntax error"`
- **ErrorType**: `"CompilationException"`, `"SyntaxException"`

## Property Details
- **Event**: The type of event that triggered the notification
- **Source**: The source of the event
- **PreviousState**: The state before the operation
- **NewState**: The state after the operation (when applicable)
- **HasCurrentOperation**: Indicates if a Hot Reload operation is in progress
- **FileCount**: Number of files affected by the operation
- **DurationMs**: Duration of the operation in milliseconds (if applicable)
- **ErrorMessage**/**ErrorType**: Only present on error events

All events are tracked server-side in `Notify()`.
