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

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `Name` property |
| macOS | `NSAccessibility.accessibilityLabel` |
| Web (WASM) | `aria-label` attribute on the semantic DOM element |
| Android | `AccessibilityNodeInfo.contentDescription` |
| iOS | `UIAccessibility.accessibilityLabel` |

## AutomationId

> [!NOTE]
> To avoid performance overhead, `AutomationId` only has an effect when the `IsUiAutomationMappingEnabled` MSBuild property is set to `true`, or when `Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled` is set in code.

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `AutomationId` property |
| macOS | `NSAccessibility.accessibilityIdentifier` |
| Web (WASM) | `xamlautomationid` attribute + `aria-label` on the HTML element |
| Android | `View.contentDescription` |
| iOS | `UIAccessibility.accessibilityIdentifier` |

## HelpText

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `HelpText` property |
| macOS | `NSAccessibility.accessibilityHelp` |
| Web (WASM) | `aria-description` attribute |

## HeadingLevel

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `HeadingLevel` property |
| macOS | `NSAccessibility` heading trait |
| Web (WASM) | Rendered as `<h1>`–`<h6>` semantic HTML elements |

## LandmarkType

Supported values: `None` (default), `Custom`, `Form`, `Main`, `Navigation`, `Search`.

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `LandmarkType` property |
| macOS | `NSAccessibility` landmark |
| Web (WASM) | ARIA landmark roles (`role="navigation"`, `role="main"`, `role="search"`, `role="form"`, `role="region"`) |

When using `LandmarkType="Custom"`, provide a human-readable description with `LocalizedLandmarkType`.

## LiveSetting

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `LiveSetting` property |
| macOS | `NSAccessibility` notification |
| Web (WASM) | `aria-live="polite"` or `aria-live="assertive"` attribute |

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
