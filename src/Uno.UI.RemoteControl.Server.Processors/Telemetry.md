# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry events emitted by the server (`ServerHotReloadProcessor.Notify`).

Event name prefix: uno/dev-server/hot-reload

| Event Name              | Main Properties  (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) |
|-------------------------|-------------------------------------------------------|---------------------------------------|
| notify-start            | Event, Source, PreviousState                          | FileCount                             |
| notify-disabled         | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount                             |
| notify-initializing     | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount                             |
| notify-ready            | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount                             |
| notify-processing-files | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount                             |
| notify-completed        | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount, DurationMs                 |
| notify-no-changes       | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount, DurationMs                 |
| notify-failed           | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount, DurationMs                 |
| notify-rude-edit        | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount, DurationMs                 |
| notify-complete         | Event, Source, PreviousState, NewState, HasCurrentOp  | FileCount, DurationMs                 |
| notify-error            | Event, Source, PreviousState, ErrorMessage, ErrorType | FileCount, DurationMs                 |

## Property details
- `FileCount`: Number of files affected by the operation
- `DurationMs`: Duration of the operation in milliseconds (if applicable)
- `HasCurrentOp`: Indicates if a Hot Reload operation is in progress
- `ErrorMessage`/`ErrorType`: Only present on `.Error` events

All events are tracked server-side in `Notify()`.
