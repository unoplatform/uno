# Hot Reload Telemetry Events

This document describes all Hot Reload telemetry events emitted by the DevServer's `ServerHotReloadProcessor.Notify` method.

> [!TIP]
> For a complete overview of all Uno Platform telemetry, see the [Uno Platform Telemetry](../../doc/articles/uno-platform-telemetry.md) documentation.

## Event Prefix

All Hot Reload telemetry events use the prefix: `uno/dev-server/hot-reload`

## Opt-Out

To disable Hot Reload telemetry, set the environment variable:
```bash
UNO_PLATFORM_TELEMETRY_OPTOUT=1
```

Or use the MSBuild property:
```xml
<UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
```

## Events Overview

Hot Reload telemetry tracks the lifecycle and state transitions of hot reload operations, providing insights into:
- When hot reload operations start and complete
- File change processing
- Success and failure scenarios
- State transitions in the hot reload system

## Telemetry Events

| Event Name              | Main Properties (with hotreload/ prefix)             | Measurements (with hotreload/ prefix) | Description |
|-------------------------|-------------------------------------------------------|---------------------------------------|-------------|
| **notify-start** [[src]](HotReload/ServerHotReloadProcessor.cs#L158)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Marks the beginning of processing a hot reload notification |
| **notify-disabled** [[src]](HotReload/ServerHotReloadProcessor.cs#L168)         | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload has been disabled |
| **notify-initializing** [[src]](HotReload/ServerHotReloadProcessor.cs#L174)     | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload system is initializing |
| **notify-ready** [[src]](HotReload/ServerHotReloadProcessor.cs#L180)            | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload system is ready to process changes |
| **notify-processing-files** [[src]](HotReload/ServerHotReloadProcessor.cs#L186) | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload is processing file changes |
| **notify-completed** [[src]](HotReload/ServerHotReloadProcessor.cs#L191)        | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload successfully completed |
| **notify-no-changes** [[src]](HotReload/ServerHotReloadProcessor.cs#L196)       | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload detected no actual changes |
| **notify-failed** [[src]](HotReload/ServerHotReloadProcessor.cs#L201)           | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload operation failed |
| **notify-rude-edit** [[src]](HotReload/ServerHotReloadProcessor.cs#L206)        | Event, Source, PreviousState                          | FileCount, DurationMs (optional)      | Hot reload detected a rude edit (requires restart) |
| **notify-complete** [[src]](HotReload/ServerHotReloadProcessor.cs#L212)         | Event, Source, PreviousState, NewState, HasCurrentOperation  | FileCount, DurationMs (optional)      | Notification processing complete (summary event) |
| **notify-error** [[src]](HotReload/ServerHotReloadProcessor.cs#L221)            | Event, Source, PreviousState, NewState, HasCurrentOperation, ErrorMessage, ErrorType | FileCount, DurationMs (optional) | An error occurred during notification processing |

## Property Value Examples

The following are example values for properties in Hot Reload telemetry events:

### String Properties

- **Event**: `"ProcessingFiles"`, `"Completed"`, `"Failed"`, `"RudeEdit"`, `"NoChanges"` - Type of hot reload event
- **Source**: `"IDE"`, `"DevServer"` - Origin of the hot reload trigger
- **PreviousState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"` - State before the operation
- **NewState**: `"Ready"`, `"Disabled"`, `"Initializing"`, `"Processing"` - State after the operation (only in notify-complete and notify-error)
- **HasCurrentOperation**: `"True"`, `"False"` - Indicates if a hot reload operation is currently in progress (only in notify-complete and notify-error)
- **ErrorMessage**: `"Compilation failed"`, `"Syntax error"` - Error message when operation fails
- **ErrorType**: `"CompilationException"`, `"SyntaxException"` - Exception type when operation fails

### Measurement Values

- **FileCount**: Number of files affected by the hot reload operation (only present if there is a current operation)
- **DurationMs**: Duration of the hot reload operation in milliseconds (only present if the operation has completed)

## Property Details

- **Event**: The type of hot reload event that triggered the notification
- **Source**: The source of the event (IDE-initiated or DevServer-initiated)
- **PreviousState**: The hot reload system state before the operation
- **NewState**: The hot reload system state after the operation (only present in notify-complete and notify-error events)
- **HasCurrentOperation**: Indicates if a hot reload operation is in progress (only present in notify-complete and notify-error events)
- **FileCount**: Count of files considered in the hot reload operation (only present if there is a current operation)
- **DurationMs**: Time taken for the operation in milliseconds (only present if the operation has completed)
- **ErrorMessage**: Detailed error message (only present on notify-error events; **may contain sensitive information**)
- **ErrorType**: Type of exception that occurred (only present on notify-error events)

## Event Flow

A typical hot reload operation produces events in the following sequence:

1. **notify-start** - Operation begins
2. One of the state events:
   - **notify-initializing** - System is initializing
   - **notify-ready** - System is ready
   - **notify-disabled** - Hot reload is disabled
3. **notify-processing-files** - Processing file changes
4. One of the completion events:
   - **notify-completed** - Successfully applied changes
   - **notify-no-changes** - No actual changes detected
   - **notify-failed** - Operation failed
   - **notify-rude-edit** - Changes require app restart
5. **notify-complete** or **notify-error** - Final summary event

## Implementation Details

All hot reload telemetry events are tracked server-side in the `ServerHotReloadProcessor.Notify()` method. The telemetry captures:

- State transitions in the hot reload system
- Processing metrics (file count, duration)
- Success and failure indicators
- Error details when operations fail

## Privacy and Security Notes

- **ErrorMessage** properties are sent as raw values and **may contain sensitive information**. Review error handling carefully.
- File paths are not included in telemetry - only file counts are transmitted
- All timing and count measurements are aggregated statistics without identifying information

## See Also

- [Uno Platform Telemetry Overview](../../doc/articles/uno-platform-telemetry.md)
- [DevServer Telemetry](../Uno.UI.RemoteControl.Host/Telemetry.md)
- [Application Launch Monitor](../Uno.UI.RemoteControl.Server/AppLaunch/ApplicationLaunchMonitor.md)
- [Build Tools Telemetry](../../doc/articles/uno-toolchain-telemetry.md)
