# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry tracked by the server (`ServerHotReloadProcessor.Notify`).

**Note:** Starting with Uno.DevTools.Telemetry v1.3.0, exceptions are tracked using the `TrackException` API instead of encoding exception details (ErrorMessage, ErrorType) as properties in `TrackEvent` calls. This provides better grouping, analytics, and diagnostics in Application Insights and other telemetry backends.

## Events

Event name prefix: uno/dev-server/hot-reload

| Event Name              | Main Properties  (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) |
|-------------------------|-------------------------------------------------------|---------------------------------------|
| **notify-start** [[src]](HotReload/ServerHotReloadProcessor.cs#L158)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-disabled** [[src]](HotReload/ServerHotReloadProcessor.cs#L168)         | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-initializing** [[src]](HotReload/ServerHotReloadProcessor.cs#L174)     | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-ready** [[src]](HotReload/ServerHotReloadProcessor.cs#L180)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-processing-files** [[src]](HotReload/ServerHotReloadProcessor.cs#L186) | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-completed** [[src]](HotReload/ServerHotReloadProcessor.cs#L191)        | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-no-changes** [[src]](HotReload/ServerHotReloadProcessor.cs#L196)       | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-failed** [[src]](HotReload/ServerHotReloadProcessor.cs#L201)           | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-rude-edit** [[src]](HotReload/ServerHotReloadProcessor.cs#L206)        | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      |
| **notify-complete** [[src]](HotReload/ServerHotReloadProcessor.cs#L212)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs (optional)      |

## Exceptions

Exceptions during Hot Reload operations are tracked using `TrackException` [[src]](HotReload/ServerHotReloadProcessor.cs#L227):

- **Context Properties**: Event, Source, PreviousState, NewState, HasCurrentOperation
- **Context Measurements**: FileCount, DurationMs (optional)
- **Exception Details**: Automatically captured by TrackException (type, message, stack trace)

These exceptions can be identified in Application Insights by their context properties (e.g., Event="ProcessingFiles") alongside the standard exception data.

## Property Value Examples

### String Properties
- **Event**: `"ProcessingFiles"`, `"Completed"`, ...
- **Source**: `"IDE"`, `"DevServer"`, ...
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"`
- **HasCurrentOperation**

## Property Details
- **Event**: The type of event that triggered the notification
- **Source**: The source of the event
- **PreviousState**: The state before the operation
- **NewState**: The state after the operation (only present in notify-complete and exceptions)
- **HasCurrentOperation**: Indicates if a Hot Reload operation is in progress (only present in notify-complete and exceptions)
- **FileCount**: Number of files affected by the operation (only present if there is a current operation)
- **DurationMs**: Duration of the operation in milliseconds (only present if the operation has completed)

All events and exceptions are tracked server-side in `Notify()`.
