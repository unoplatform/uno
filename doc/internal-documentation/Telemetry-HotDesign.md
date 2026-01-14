# Hot Design Telemetry

**Event Name Prefix:** `uno/hot-design`

Hot Design telemetry tracks server-side sessions and client-initiated analytics events. The server acts as a telemetry aggregator, forwarding client events to Application Insights while adding session context.

## Server-Side Events

Server-side events are emitted by the Hot Design server component:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `session-started` | - | `SessionDuration` | Tracks when a Hot Design server session is started |
| `session-end` | - | `SessionDuration` | Tracks when a Hot Design server session ends |
| `usage` | `UsageEvent` (string) + event-specific properties | `SessionDuration` + event-specific measurements | Forwards all client-initiated analytics events with server session context |

## Client-Side Events

Client-side events are organized by category and forwarded to the server via the `usage` event:

### Session and Lifecycle

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `DebugSessionStarted` | - | - | Client debugging session started |
| `DebugSessionEnded` | - | - | Client debugging session ended |
| `Crash` | `ExceptionType` (string) | - | Application crash occurred |
| `EnterHotDesign` | - | - | User entered Hot Design mode |
| `LeaveHotDesign` | - | - | User left Hot Design mode |
| `PauseHotDesign` | - | - | Hot Design was paused |
| `ResumeHotDesign` | - | - | Hot Design was resumed |

### Licensing

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `HotReloadDisabled` | - | - | Hot Reload is disabled |
| `HotDesignLicenseUnavailable` | - | - | Hot Design license is not available |
| `HotDesignUnlicensedShowInfo` | - | - | Unlicensed Hot Design info shown to user |
| `HotDesignTrialPeriod` | - | - | User is in Hot Design trial period |
| `HotDesignTrialExpired` | - | - | Hot Design trial period expired |
| `HotDesignIsAvailable` | - | - | Hot Design is available and licensed |
| `GettingStartedUriLaunched` | - | - | Getting started URI was launched |
| `StudioAppLaunchedNotLicensed` | - | - | Studio app launched but not licensed |
| `StudioSignedOut` | - | - | User signed out of Studio |
| `UnknownHotDesignStatus` | - | - | Hot Design status is unknown |

### UI Controls

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `ZoomFlyoutOpened` | - | - | Zoom flyout was opened |
| `ZoomFlyoutClosed` | - | `Duration` (ms) | Zoom flyout was closed |
| `ChangeFormFactor` | `SelectedFormFactor` (string), `Width` (int), `Height` (int) | - | Form factor changed |
| `ApplicationSizeChanged` | `Width` (int), `Height` (int) | - | Application window size changed |
| `ZoomChanged` | `ZoomLevel` (double) | - | Zoom level changed |
| `ThemeChanged` | `IsDark` (bool) | - | Theme changed (light/dark) |
| `AutoFitChanged` | `IsEnabled` (bool) | - | Auto-fit setting changed |
| `ShowHideToolWindow` | `PropertyGridVisible` (bool), `ToolboxVisible` (bool), `ElementsVisible` (bool) | - | Tool window visibility changed |

### Property Grid

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `PropertyFlyoutOpened` | - | - | Property flyout was opened |
| `PropertyFlyoutClosed` | - | `Duration` (ms) | Property flyout was closed |
| `PropertyChanged` | `PropertyName` (string) | - | A property value was changed |
| `PropertyTypeChanged` | `PropertyName` (string) | - | A property type was changed |
| `PropertyClear` | `PropertyName` (string) | - | A property was cleared |
| `EditTemplate` | `PropertyName` (string) | - | Template editing initiated |
| `PropertySourceChanged` | `PropertyName` (string), `Source` (string) | - | Property source changed (binding, static, etc.) |
| `ResponsiveExtensionsChanged` | `IsEnabled` (bool), `Breakpoints` (string) | - | Responsive extensions settings changed |

### Undo/Redo

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `UndoStarted` | `UndoStackLength` (int) | - | Undo operation started |
| `UndoCompleted` | - | `Duration` (ms) | Undo operation completed |
| `RedoStarted` | `RedoStackLength` (int) | - | Redo operation started |
| `RedoCompleted` | - | `Duration` (ms) | Redo operation completed |

### Toolbox and Elements

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `ToolboxDragToCanvas` | `Name` (string) | - | Control dragged from toolbox to canvas |
| `ToolboxDragToElements` | `Name` (string) | - | Control dragged from toolbox to elements tree |
| `ToolboxDoubleClickToAdd` | `Name` (string) | - | Control added via double-click in toolbox |
| `ChangeProperties` | `Success` (bool), `ExceptionType` (string) | - | Properties changed |
| `MoveElement` | `Success` (bool), `ExceptionType` (string) | - | Element moved in visual tree |
| `ApplyTemplate` | `Success` (bool), `ExceptionType` (string) | - | Template applied to element |
| `DeleteElement` | `Success` (bool), `ExceptionType` (string) | - | Element deleted |
| `AddElement` | `Success` (bool), `ExceptionType` (string) | - | Element added to visual tree |
| `AddParent` | `Success` (bool), `ExceptionType` (string) | - | Parent container added |

### Chat and XAML Generation

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `ChatViewOpened` | - | - | Chat view was opened |
| `ChatViewStartNew` | - | - | New chat conversation started |
| `ChatContextOptionAdded` | `OptionType` (string) | - | Context option added to chat |
| `ChatContextOptionRemoved` | `OptionType` (string) | - | Context option removed from chat |
| `ChatGenerateStarted` | `PromptLength` (int), `ContextOptionsCount` (int) | - | XAML generation started |
| `ChatGenerateCancelled` | - | - | XAML generation cancelled |
| `ChatXamlPreviewSelected` | `PreviewVersion` (string) | - | XAML preview version selected |
| `ChatXamlPreviewApplied` | `PreviewVersion` (string) | - | XAML preview applied |
| `ChatImageAttached` | `Count` (int), `Method` (string) | - | Images attached to chat |
| `ChatImageRemoved` | - | - | Image removed from chat |
| `XamlGenerationProgress` | `Phase` (string) | - | XAML generation progress update |

### Search

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `ToolboxSearch` | `SearchTerm` (string) | - | User searched in toolbox |
| `PropertySearch` | `Query` (string) | - | User searched in property grid |
| `StoriesSearch` | `StoriesSearchTerm` (string) | - | User searched in stories |
| `ElementsSearch` | `ElementsSearchText` (string) | - | User searched in elements tree |
| `PropertyFilterSwitch` | `SelectedFilter` (string) | - | Property filter changed |

## Event Flow

1. **Client** generates usage events during user interaction
2. **Server** receives events and enriches them with session context (SessionDuration)
3. **Server** forwards enriched events to Application Insights as `usage` events
4. **Application Insights** stores and aggregates telemetry data

## Measurements

- **SessionDuration**: Duration (in milliseconds) since the Hot Design server session started, added to all client events
- **Duration/DurationMs**: Duration (in milliseconds) of a specific operation (e.g., flyout open/close, undo/redo)

## Reference

For more detailed information, see the [Hot Design Telemetry Documentation](https://github.com/unoplatform/uno.hotdesign/blob/main/internal-documentation/Telemetry.md).
