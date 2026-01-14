# Uno Platform Telemetry Documentation

This document provides a comprehensive overview of telemetry across the Uno Platform ecosystem, including Hot Design, AI Features, Dev Server, Licensing, and IDE Extensions.

## Table of Contents

- [Overview](#overview)
- [Hot Design Telemetry](#hot-design-telemetry)
  - [Server-Side Events](#server-side-events)
  - [Client-Side Events](#client-side-events)
    - [Session and Lifecycle](#session-and-lifecycle)
    - [Licensing](#licensing)
    - [UI Controls](#ui-controls)
    - [Property Grid](#property-grid)
    - [Undo/Redo](#undoredo)
    - [Toolbox and Elements](#toolbox-and-elements)
    - [Chat and XAML Generation](#chat-and-xaml-generation)
    - [Search](#search)
  - [Event Flow](#event-flow)
  - [Measurements](#measurements)
- [AI Features](#ai-features)
  - [Application Insights](#application-insights)
  - [Request Context Fields](#request-context-fields)
  - [Azure Tables Storage](#azure-tables-storage)
  - [Privacy Notes](#privacy-notes)
- [Dev Server](#dev-server)
  - [Telemetry Session Types](#telemetry-session-types)
  - [Session Properties](#session-properties)
  - [App Launch Tracking](#app-launch-tracking)
- [Licensing](#licensing-1)
  - [License Manager Events (Client)](#license-manager-events-client)
  - [Navigation Events](#navigation-events)
  - [License API Events (Server)](#license-api-events-server)
  - [DevServer Licensing Events](#devserver-licensing-events)
- [IDE Extensions](#ide-extensions)
  - [Visual Studio Code](#visual-studio-code)
  - [Rider](#rider)
  - [Visual Studio](#visual-studio)
- [Privacy & Compliance](#privacy--compliance)
  - [GDPR Compliance](#gdpr-compliance)
  - [Data Collection Policy](#data-collection-policy)
  - [Disabling Telemetry](#disabling-telemetry)
  - [Data Retention](#data-retention)
  - [Instrumentation Keys](#instrumentation-keys)
  - [Contact](#contact)
- [Additional Resources](#additional-resources)

---

## Overview

Telemetry is collected across the Uno Platform ecosystem to understand usage patterns, improve features, and diagnose issues. All telemetry data is collected with privacy in mind, with no personally identifiable information (PII) being transmitted.

All telemetry events use a prefixed naming convention based on the feature area:
- `uno/hot-design` - Hot Design telemetry
- `uno/ai` - AI Features telemetry
- `uno/devserver` - Dev Server telemetry
- `uno/licensing` - Licensing telemetry
- `uno/vscode` - VS Code Extension telemetry
- `uno/rider` - Rider Plugin telemetry
- `uno/visual-studio` - Visual Studio Extension telemetry

---

## Hot Design Telemetry

**Event Name Prefix:** `uno/hot-design`

Hot Design telemetry tracks server-side sessions and client-initiated analytics events. The server acts as a telemetry aggregator, forwarding client events to Application Insights while adding session context.

### Server-Side Events

Server-side events are emitted by the Hot Design server component:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `session-started` | - | `SessionDuration` | Tracks when a Hot Design server session is started |
| `session-end` | - | `SessionDuration` | Tracks when a Hot Design server session ends |
| `usage` | `UsageEvent` (string) + event-specific properties | `SessionDuration` + event-specific measurements | Forwards all client-initiated analytics events with server session context |

### Client-Side Events

Client-side events are organized by category and forwarded to the server via the `usage` event:

#### Session and Lifecycle

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `DebugSessionStarted` | - | - | Client debugging session started |
| `DebugSessionEnded` | - | - | Client debugging session ended |
| `Crash` | `ExceptionType` (string) | - | Application crash occurred |
| `EnterHotDesign` | - | - | User entered Hot Design mode |
| `LeaveHotDesign` | - | - | User left Hot Design mode |
| `PauseHotDesign` | - | - | Hot Design was paused |
| `ResumeHotDesign` | - | - | Hot Design was resumed |

#### Licensing

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

#### UI Controls

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

#### Property Grid

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

#### Undo/Redo

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `UndoStarted` | `UndoStackLength` (int) | - | Undo operation started |
| `UndoCompleted` | - | `Duration` (ms) | Undo operation completed |
| `RedoStarted` | `RedoStackLength` (int) | - | Redo operation started |
| `RedoCompleted` | - | `Duration` (ms) | Redo operation completed |

#### Toolbox and Elements

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

#### Chat and XAML Generation

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

#### Search

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `ToolboxSearch` | `SearchTerm` (string) | - | User searched in toolbox |
| `PropertySearch` | `Query` (string) | - | User searched in property grid |
| `StoriesSearch` | `StoriesSearchTerm` (string) | - | User searched in stories |
| `ElementsSearch` | `ElementsSearchText` (string) | - | User searched in elements tree |
| `PropertyFilterSwitch` | `SelectedFilter` (string) | - | Property filter changed |

### Event Flow

1. **Client** generates usage events during user interaction
2. **Server** receives events and enriches them with session context (SessionDuration)
3. **Server** forwards enriched events to Application Insights as `usage` events
4. **Application Insights** stores and aggregates telemetry data

### Measurements

- **SessionDuration**: Duration (in milliseconds) since the Hot Design server session started, added to all client events
- **Duration/DurationMs**: Duration (in milliseconds) of a specific operation (e.g., flyout open/close, undo/redo)

### Reference

For more detailed information, see the [Hot Design Telemetry Documentation](https://github.com/unoplatform/uno.hotdesign/blob/main/internal-documentation/Telemetry.md).

---

## AI Features

**Event Name Prefix:** `uno/ai`

AI Features use Application Insights with custom telemetry initializers to track design threads and operations.

### Application Insights

AI Features telemetry uses Azure Application Insights with custom telemetry initializers that add request context fields to all telemetry items.

### Request Context Fields

The following fields are added to all telemetry requests:

| Field Name | Type | Description |
|------------|------|-------------|
| `design_thread_id` | GUID | Unique identifier for the design thread |
| `parent_design_thread_id` | GUID | Parent design thread identifier (for nested operations) |
| `operation_phase` | Enum | Phase of the operation: `UxDesign`, `XamlGeneration`, `UiImprovement`, `FitnessEvaluation` |
| `loop_iteration` | String | Iteration number for loops |

### Azure Tables Storage

The `CallDetailsEntry` entity in Azure Tables includes:

| Field | Description |
|-------|-------------|
| `DesignThreadId` | Unique design thread identifier |
| `ParentDesignThreadId` | Parent design thread identifier |
| `OperationPhase` | Current operation phase |
| `LoopIteration` | Loop iteration number |

### Privacy Notes

- **No PII**: No personally identifiable information is collected
- **Token Handling**: Raw bearer tokens are never logged; stable SHA256 hashes are used for opaque tokens
- **User ID**: Extracted from JWT claims when available

### Reference

For more detailed information, see the [Uno AI Features Repository](https://github.com/unoplatform/uno.ai-private).

---

## Dev Server

**Event Name Prefix:** `uno/devserver`

Dev Server telemetry tracks server sessions and client connections, including app launch tracking.

### Telemetry Session Types

| Session Type | Description |
|--------------|-------------|
| `Root` | The DevServer process itself |
| `Connection` | A specific client connection to the DevServer |

### Session Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | GUID | Unique session identifier |
| `SessionType` | String | Either "Root" or "Connection" |
| `ConnectionId` | String | Connection identifier (for Connection sessions) |
| `SolutionPath` | String | Optional path to the solution being served |

### App Launch Tracking

App launches are tracked via the `AppLaunchMessage` with the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `Mvid` | GUID | Module Version ID (unique per app build) |
| `Platform` | String | Target platform (e.g., WebAssembly, iOS, Android) |
| `IsDebug` | Boolean | Whether the app is a debug build |
| `Ide` | String | IDE being used (e.g., VisualStudio, VSCode, Rider) |
| `Plugin` | String | Plugin/extension name and version |
| `Step` | Enum | Launch step: `Launched` or `Connected` |

### Reference

For more detailed information, see the [Uno Platform Telemetry Source](https://github.com/unoplatform/uno).

---

## Licensing

**Event Name Prefix:** `uno/licensing`

Licensing telemetry tracks license management operations, user navigation, and API interactions.

### License Manager Events (Client)

Client-side license manager events:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `logout-requested` | - | - | User requested logout |
| `logout-success` | - | - | Logout completed successfully |
| `logout-failure` | `Error` (string) | - | Logout failed |
| `login-requested` | - | - | User requested login |
| `login-success` | - | - | Login completed successfully |
| `login-failure` | `error` (string), `errorDescription` (string) | - | Login failed |
| `login-canceled` | - | - | User canceled login |
| `login-timeout` | `Error` (string), `ErrorDescription` (string) | - | Login timed out |
| `license-status` | `LicenseName` (string), `LicenseStatus` (string), `TrialDaysRemaining` (int), `LicenseExpiryDate` (date) | - | License status checked |
| `manager-started` | `Feature` (string) | - | License manager started for a feature |
| `manager-failed` | `ErrorType` (string), `ErrorStackTrace` (string) | - | License manager failed |

### Navigation Events

User navigation events:

| Event Name | Description |
|------------|-------------|
| `nav-to-my-account` | User navigated to My Account |
| `nav-to-help-overview` | User navigated to Help Overview |
| `nav-to-help-getting-started` | User navigated to Getting Started help |
| `nav-to-help-counter-tutorial` | User navigated to Counter Tutorial |
| `nav-to-help-report-issue` | User navigated to Report Issue |
| `nav-to-help-suggest-feature` | User navigated to Suggest Feature |
| `nav-to-help-ask-question` | User navigated to Ask Question |
| `nav-to-discord-server` | User navigated to Discord server |
| `nav-to-youtube-channel` | User navigated to YouTube channel |
| `nav-to-end-user-agreement` | User navigated to End User Agreement |
| `nav-to-privacy-policy` | User navigated to Privacy Policy |
| `nav-to-purchase-now` | User navigated to Purchase |
| `nav-to-trial-extension` | User navigated to Trial Extension |

### License API Events (Server)

Server-side licensing API events:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `get-user` | - | - | User info requested |
| `get-user-success` | - | `DurationMs` | User info retrieved successfully |
| `get-user-failure` | `Error` (string) | `DurationMs` | User info retrieval failed |
| `get-licenses` | - | - | Licenses requested |
| `get-licenses-success` | - | `DurationMs` | Licenses retrieved successfully |
| `get-licenses-failure` | `Error` (string) | `DurationMs` | License retrieval failed |
| `get-offers` | - | - | Offers requested |
| `get-offers-success` | `Details` (string) | `DurationMs` | Offers retrieved successfully |
| `get-offers-failure` | `Error` (string) | `DurationMs` | Offer retrieval failed |
| `get-features` | - | - | Features requested |
| `get-features-success` | - | `DurationMs` | Features retrieved successfully |
| `get-features-failure` | - | `DurationMs` | Feature retrieval failed |
| `invalid-api-key` | - | - | API key validation failed |
| `invalid-token` | - | - | Token validation failed |
| `no-license-id` | - | - | No license ID provided |
| `id-not-found` | - | - | License ID not found |
| `license-feature-not-granted` | `Feature` (string) | - | Feature not granted by license |
| `license-feature-info-returned` | `Feature` (string) | - | Feature info returned |

### DevServer Licensing Events

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `license-manager-start` | `Feature` (string) | - | License manager started in DevServer |

### Reference

For more detailed information, see the [Uno Licensing Repository](https://github.com/unoplatform/uno.licensing).

---

## IDE Extensions

IDE extensions track extension lifecycle, user interactions, and dev server operations.

### Visual Studio Code

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

**Reference:**
For more detailed information, see the [VS Code Extension Telemetry Documentation](https://github.com/unoplatform/uno.vscode/blob/main/documentation/Telemetry.md).

### Rider

**Event Name Prefix:** `uno/rider`

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `extension-loaded` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | Extension loaded successfully |
| `extension-unloaded` | `PluginVersion` (string), `IDE` (string), `IDEVersion` (string) | - | Extension unloaded |
| `extension-failure` | `Exception` (string), `Message` (string) | - | Extension failure occurred |
| `project-created` | `ProjectName` (string) | - | New Uno project created |
| `solution-build` | - | - | Solution built |
| `debugger-launched` | - | - | Debugger launched successfully |
| `no-debugger-launch` | - | - | Debugger launch skipped |
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

**Reference:**
For more detailed information, see the [Rider Plugin Telemetry Documentation](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/Telemetry/Telemetry.md).

### Visual Studio

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

---

## Privacy & Compliance

### GDPR Compliance

Uno Platform telemetry is designed to be GDPR compliant:

- **Transparency**: All telemetry collection is documented and disclosed
- **User Control**: Users can opt out at any time using environment variables or IDE settings
- **Data Minimization**: Only essential, non-identifying data is collected
- **No PII**: No personally identifiable information is collected
- **Security**: Data is transmitted securely to Microsoft Azure Application Insights

### Data Collection Policy

#### What IS Collected

- Event names and timestamps
- Version information (IDE, plugin, SDK)
- Anonymous user IDs (hashed tokens, hashed MAC addresses)
- Performance measurements (durations, counts)
- Feature usage patterns (which features are used, how often)
- Error types (exception types only, no stack traces)
- Operating system and architecture information
- Target frameworks and platforms
- Hashed directory paths (SHA256)

#### What is NOT Collected

- **Personal Information**: No usernames, email addresses, or account details
- **User Content**: No file paths, source code, or project-specific information
- **Detailed Errors**: No stack traces or detailed error messages that could contain sensitive data
- **Raw Tokens**: Bearer tokens are never logged; only SHA256 hashes for opaque tokens

### Disabling Telemetry

Users can disable telemetry in multiple ways:

#### Visual Studio
1. Tools → Options → Uno Platform → Telemetry
2. Uncheck "Enable telemetry"

#### VS Code
1. Open Settings (Ctrl+,)
2. Search for "Uno Platform Telemetry"
3. Uncheck "Enable Telemetry"

#### Rider
1. File → Settings → Tools → Uno Platform
2. Uncheck "Send usage statistics"

#### Environment Variable
Set the environment variable:
```bash
export UNO_PLATFORM_TELEMETRY_OPTOUT=true
```

Or in Windows:
```powershell
$env:UNO_PLATFORM_TELEMETRY_OPTOUT = "true"
```

This will disable telemetry for all Uno Platform tools.

### Data Retention

- **Application Insights**: Default retention is 90 days
- **Azure Tables**: Configurable retention based on storage policies

Data is automatically purged after the retention period.

### Instrumentation Keys

| Environment | Instrumentation Key |
|-------------|---------------------|
| **Production** | `9a44058e-1913-4721-a979-9582ab8bedce` |
| **Development** | `81286976-e3a4-49fb-b03b-30315092dbc4` |

### Contact

For privacy concerns or questions about telemetry:

- **Email**: privacy@platform.uno
- **GitHub Issues**: [unoplatform/uno/issues](https://github.com/unoplatform/uno/issues)

---

## Additional Resources

### Documentation Links

- **Hot Design Telemetry**: [uno.hotdesign/internal-documentation/Telemetry.md](https://github.com/unoplatform/uno.hotdesign/blob/main/internal-documentation/Telemetry.md)
- **VS Code Extension Telemetry**: [uno.vscode/documentation/Telemetry.md](https://github.com/unoplatform/uno.vscode/blob/main/documentation/Telemetry.md)
- **Rider Plugin Telemetry**: [uno.rider/Telemetry/Telemetry.md](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/Telemetry/Telemetry.md)
- **Visual Studio Extension Telemetry**: [uno.studio/docs/Telemetry.md](https://github.com/unoplatform/uno.studio/blob/main/docs/Telemetry.md)
- **Uno Licensing API**: [unoplatform/uno.licensing](https://github.com/unoplatform/uno.licensing)
- **Uno AI Features**: [unoplatform/uno.ai-private](https://github.com/unoplatform/uno.ai-private)

### Repository Search Links

- [Hot Design Telemetry (unoplatform/uno.hotdesign)](https://github.com/unoplatform/uno.hotdesign/search?q=telemetry)
- [VS Code Extension (unoplatform/uno.vscode)](https://github.com/unoplatform/uno.vscode/search?q=telemetry)
- [Rider Plugin (unoplatform/uno.rider)](https://github.com/unoplatform/uno.rider/search?q=telemetry)
- [Visual Studio Extension (unoplatform/uno.studio)](https://github.com/unoplatform/uno.studio/search?q=telemetry)
- [Licensing (unoplatform/uno.licensing)](https://github.com/unoplatform/uno.licensing/search?q=telemetry)
- [Dev Server (unoplatform/uno)](https://github.com/unoplatform/uno/search?q=devserver+telemetry)

---

**Note**: Due to the private nature of some repositories, search results may be limited based on your access permissions. For full access to telemetry documentation, ensure you have the necessary permissions to the relevant repositories.
