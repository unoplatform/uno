# Wizard Telemetry

**Event Name Prefix:** `uno/wizard` (base), events may be prefixed with full path like `uno/wizard/live/`

Wizard telemetry tracks template project creation, CLI command generation, and user configuration choices through the Uno Platform project wizard interface.

## Overview

The Wizard service tracks user interactions with the Uno Platform project template wizard, capturing template parameter selections and project creation outcomes. Telemetry is implemented in the `WizardService.cs` class and uses the `ITelemetryClient` interface to send events.

**Event Name Prefixing:**

- Desktop/Native: Events use the base prefix from `WizardService.TelemetryData.Wizard_EventNamePrefix`
- WebAssembly: Events are prefixed with `{Wizard_EventNamePrefix}/live/`

## Wizard Events

All wizard events are tracked through the `TrackEventReplacements` method:

| Event Name | Properties | Measurements | When Triggered |
|------------|-----------|--------------|----------------|
| **Event_ProjectCreated** | `AllCreationOptions` (string) | `ReplacementsCount` (double), `Duration` (double) | User completes wizard and creates a new Uno Platform project |
| **Event_Cancelled** | `AllCreationOptions` (string) | `ReplacementsCount` (double), `Duration` (double) | User cancels the wizard before project creation |
| **Event_CommandGenerated** | `AllCreationOptions` (string) | `ReplacementsCount` (double), `Duration` (double) | User generates a CLI command instead of creating the project directly |
| **Event_CreationOption** | `CreationOption` (string) | `ReplacementsCount` (double), `Duration` (double) | Individual option tracking for each configuration parameter (multiple events per project) |

## Event Properties

### AllCreationOptions (string)

A semi-colon separated string containing all template parameter key-value pairs:

- Keys are sorted alphabetically
- Format: "key1=value1;key2=value2;key3=value3"
- Only includes **exportable symbols** defined in wizard metadata (filtered by `CanTrackReplacement`)
- Example: "preset=blank;platforms=Android;iOS;WebAssembly;markup=xaml;tests=none;theme=material"

### CreationOption (string)

A single key-value pair representing one configuration choice:

- Format: "key=value"
- Allows pivoting and analysis of individual configuration options
- One event is sent per configuration parameter
- Example: "preset=blank", "platforms=Android;iOS", "markup=xaml"

## Measurements

### ReplacementsCount (double)

- Total number of template parameter replacements
- Calculated as the count of items in `WizardInfo.Replacements`
- Example values: `5.0`, `8.0`, `12.0`, `15.0`

### Duration (double)

- Time in **milliseconds** from wizard start to completion/cancellation
- Calculated as: `DateTime.Now.Subtract(WizardStarted).TotalMilliseconds`
- Example values: `30000.0` (30 seconds), `45000.0` (45 seconds), `120000.0` (2 minutes)

## Property Value Examples

Example values for common wizard configuration properties:

### Template Presets

- **preset**: "blank", "default", "recommended", "mobile", "desktop"

### Platforms

- **platforms**: "Android;iOS;WebAssembly", "Windows;macOS;Linux", "Android;iOS;Windows"

### Markup Language

- **markup**: "xaml", "csharp"

### Testing Framework

- **tests**: "none", "xunit", "nunit", "mstest"

### Theme/Design System

- **theme**: "material", "fluent", "cupertino", "native"

### Project Configuration

- **authentication**: "none", "msal", "oidc"
- **extensions**: "navigation", "http", "localization"
- **logging**: "true", "false"
- **di**: "true", "false" (dependency injection)

## Telemetry Flow

```
User Opens Wizard
↓
User Configures Template Options
(preset, platforms, markup, tests, theme, etc.)
↓
User Action:
├─→ Cancel
│ → Event_Cancelled
│ → Event_CreationOption (per option)
├─→ Generate Command
│ → Event_CommandGenerated
│ → Event_CreationOption (per option)
└─→ Create Project
→ Event_ProjectCreated
→ Event_CreationOption (per option)
↓
Telemetry.FlushAsync()
↓
Telemetry.Dispose()
```
