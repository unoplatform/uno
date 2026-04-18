---
uid: Uno.Features.Accessibility.AutomationProperties
---

# AutomationProperties reference

> [!TIP]
> This article covers Uno-specific platform mappings for `AutomationProperties`. For a full description of each property and usage guidance, see [Expose basic accessibility information (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information).

Uno implements the WinUI `AutomationProperties` and maps each to the platform's native accessibility API. The tables below show how each property is surfaced on each target.

## Name

The accessible name is resolved in this order:

1. Explicit `AutomationProperties.Name`
2. `AutomationProperties.LabeledBy` target's text
3. The control's plain text content (e.g., `Button.Content` when it is a string)
4. Inner text of child elements (Skia targets)

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `Name` property |
| macOS | Skia | `NSAccessibility.accessibilityLabel` |
| Web (WASM) | Skia / Native | `aria-label` attribute on the semantic DOM element |
| Android | Native | `AccessibilityNodeInfo.contentDescription` |
| iOS | Native | `UIAccessibility.accessibilityLabel` |

## AutomationId

> [!NOTE]
> To avoid performance overhead, `AutomationId` only has an effect when the `IsUiAutomationMappingEnabled` MSBuild property is set to `true`, or when `Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled` is set in code.

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `AutomationId` property |
| macOS | Skia | `NSAccessibility.accessibilityIdentifier` |
| Web (WASM) | Skia / Native | `xamlautomationid` attribute + `aria-label` on the HTML element |
| Android | Native | `View.contentDescription` |
| iOS | Native | `UIAccessibility.accessibilityIdentifier` |

## HelpText

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `HelpText` property |
| macOS | Skia | `NSAccessibility.accessibilityHelp` |
| Web (WASM) | Skia / Native | `aria-description` attribute |

## HeadingLevel

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `HeadingLevel` property |
| macOS | Skia | `NSAccessibility` heading trait |
| Web (WASM) | Skia | Rendered as `<h1>`–`<h6>` semantic HTML elements |

## LandmarkType

Supported values: `None` (default), `Custom`, `Form`, `Main`, `Navigation`, `Search`.

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `LandmarkType` property |
| macOS | Skia | `NSAccessibility` landmark |
| Web (WASM) | Skia / Native | ARIA landmark roles (`role="navigation"`, `role="main"`, `role="search"`, `role="form"`, `role="region"`) |

When using `LandmarkType="Custom"`, provide a human-readable description with `LocalizedLandmarkType`.

## LiveSetting

| Platform | Rendering | Mapping |
|----------|-----------|---------|
| Windows (Win32) | Skia | UIAutomation `LiveSetting` property |
| macOS | Skia | `NSAccessibility` notification |
| Web (WASM) | Skia / Native | `aria-live="polite"` or `aria-live="assertive"` attribute |

## AccessibilityView

Controls whether an element appears in the automation tree.

| Value | Meaning |
|-------|---------|
| `Content` | Visible in both Content and Control views (default) |
| `Control` | Visible in the Control view only |
| `Raw` | Hidden from all automation views |

## Uno-specific tips

- **Always localize** `AutomationProperties.Name`. In XAML use the resource naming convention:
  `MyButton.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name`
- **Avoid `Opacity="0"` and `IsHitTestVisible="False"`** to hide elements. Use `Visibility="Collapsed"` instead — screen readers can still focus invisible elements with non-collapsed visibility.
- **Set `AppBarButton.Label`** even when it is not visually displayed — it is used by the screen reader.

## See also

- [Accessibility overview](index.md)
- [Custom automation peers](automation-peers.md)
- [Testing with screen readers](testing-with-screen-readers.md)
- [Expose basic accessibility information (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information)
