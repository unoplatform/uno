# Hot Reload Telemetry Events

This document summarizes all Hot Reload telemetry events emitted by the server (`ServerHotReloadProcessor.Notify`).

| Event Name                      | Main Properties                                         | Measurements (metrics)      |
|---------------------------------|--------------------------------------------------------|-----------------------------|
| HotReload.Notify.Start          | Event, Source, PreviousState                            | FileCount                   |
| HotReload.Notify.Disabled       | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount                   |
| HotReload.Notify.Initializing   | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount                   |
| HotReload.Notify.Ready          | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount                   |
| HotReload.Notify.ProcessingFiles| Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount                   |
| HotReload.Notify.Completed      | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount, DurationMs       |
| HotReload.Notify.NoChanges      | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount, DurationMs       |
| HotReload.Notify.Failed         | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount, DurationMs       |
| HotReload.Notify.RudeEdit       | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount, DurationMs       |
| HotReload.Notify.Complete       | Event, Source, PreviousState, NewState, HasCurrentOp    | FileCount, DurationMs       |
| HotReload.Notify.Error          | Event, Source, PreviousState, ErrorMessage, ErrorType   | FileCount, DurationMs       |

**Property details:**
- `FileCount`: Number of files affected by the operation
- `DurationMs`: Duration of the operation in milliseconds (if applicable)
- `HasCurrentOp`: Indicates if a Hot Reload operation is in progress
- `ErrorMessage`/`ErrorType`: Only present on `.Error` events

All events are tracked server-side in `Notify()`.
