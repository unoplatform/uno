---
uid: Uno.Features.Accessibility.AutomationProperties
---

# AutomationProperties reference

> [!TIP]
> This article covers Uno-specific information for `AutomationProperties`. For a full description and usage guidance, see [Expose basic accessibility information (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information).

`AutomationProperties` are attached properties you set on any XAML element to control how it is exposed to assistive technologies. Uno implements the full set of WinUI `AutomationProperties` — each is mapped to the equivalent native accessibility API on every supported platform.

## Name

The accessible name is the single most important accessibility property. It is the text a screen reader announces when the user focuses an element.

```xml
<Button AutomationProperties.Name="Save document" />
```

The name is resolved in this order:

1. Explicit `AutomationProperties.Name`
2. `AutomationProperties.LabeledBy` target's text
3. The control's plain text content (e.g., `Button.Content` when it is a string)
4. Inner text of child elements (Skia targets)

> [!IMPORTANT]
> Do not include the control type in the name (e.g., "Save button"). The screen reader already announces the control type separately. Including it produces redundant announcements like "Save button, button".

### Platform support

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `Name` property |
| macOS | `NSAccessibility.accessibilityLabel` |
| Web (WASM) | `aria-label` attribute on the semantic DOM element |
| Android | `AccessibilityNodeInfo.contentDescription` |
| iOS | `UIAccessibility.accessibilityLabel` |

## AutomationId

A developer-supplied identifier for the element, primarily used by UI testing frameworks such as [Uno.UITest](https://github.com/unoplatform/Uno.UITest).

```xml
<Button AutomationProperties.AutomationId="SaveButton"
        Content="Save" />
```

> [!NOTE]
> To avoid performance overhead, `AutomationId` only has an effect when the `IsUiAutomationMappingEnabled` MSBuild property is set to `true`, or when `Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled` is set in code.

### Platform mapping

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `AutomationId` property |
| macOS | `NSAccessibility.accessibilityIdentifier` |
| Web (WASM) | `xamlautomationid` attribute + `aria-label` on the HTML element |
| Android | `View.contentDescription` |
| iOS | `UIAccessibility.accessibilityIdentifier` |

## LabeledBy

Associates a visible label element with an input control. The screen reader uses the referenced element's text as the accessible name for the target control.

```xml
<TextBlock x:Name="EmailLabel" Text="Email address" />
<TextBox AutomationProperties.LabeledBy="{Binding ElementName=EmailLabel}" />
```

This is the preferred pattern for form fields because it keeps the visual label and accessible name in sync.

## HelpText

Provides supplemental context beyond the accessible name. Screen readers typically announce this as a secondary description.

```xml
<TextBox AutomationProperties.Name="Username"
         AutomationProperties.HelpText="Must be between 3 and 20 characters" />
```

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `HelpText` property |
| macOS | `NSAccessibility.accessibilityHelp` |
| Web (WASM) | `aria-description` attribute |

## AccessibilityView

Controls whether an element appears in the automation tree. Decorative or structural elements can be hidden so screen reader users don't have to navigate through them.

| Value | Meaning |
|-------|---------|
| `Content` | Element is visible in both Content and Control views (default for most controls) |
| `Control` | Element is visible in the Control view but not the Content view |
| `Raw` | Element is hidden from all automation views |

```xml
<!-- Hide a decorative border from screen readers -->
<Border AutomationProperties.AccessibilityView="Raw">
    <TextBlock Text="Important content" />
</Border>
```

## HeadingLevel

Marks an element as a heading, allowing screen reader users to navigate the page by heading structure — just like HTML heading levels.

```xml
<TextBlock Text="Account settings"
           AutomationProperties.HeadingLevel="Level1" />

<TextBlock Text="Profile"
           AutomationProperties.HeadingLevel="Level2" />

<TextBlock Text="Privacy"
           AutomationProperties.HeadingLevel="Level2" />
```

Supported values: `None` (default), `Level1` through `Level9`.

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `HeadingLevel` property |
| macOS | `NSAccessibility` heading trait |
| Web (WASM) | Rendered as `<h1>`–`<h6>` semantic HTML elements |

> [!TIP]
> On WASM, screen reader users can press `H` (NVDA/Narrator) or `VO+Cmd+H` (VoiceOver) to jump between headings. This is one of the most common navigation patterns for screen reader users.

## LandmarkType

Identifies major regions of the page so screen reader users can quickly jump between them.

```xml
<StackPanel AutomationProperties.LandmarkType="Navigation">
    <!-- Navigation links -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Main">
    <!-- Main content -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Search">
    <!-- Search UI -->
</StackPanel>
```

Supported values: `None` (default), `Custom`, `Form`, `Main`, `Navigation`, `Search`.

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `LandmarkType` property |
| macOS | `NSAccessibility` landmark |
| Web (WASM) | ARIA landmark roles (`role="navigation"`, `role="main"`, `role="search"`, `role="form"`, `role="region"`) |

### LocalizedLandmarkType

When using `LandmarkType="Custom"`, provide a human-readable description with `LocalizedLandmarkType`:

```xml
<StackPanel AutomationProperties.LandmarkType="Custom"
            AutomationProperties.LocalizedLandmarkType="Quick actions">
    <!-- Custom landmark content -->
</StackPanel>
```

## LiveSetting

Marks an element as a **live region** — when its content changes, the screen reader automatically announces the update without the user navigating to it.

```xml
<TextBlock x:Name="StatusText"
           Text="Ready"
           AutomationProperties.LiveSetting="Polite" />
```

| Value | Behavior |
|-------|----------|
| `Off` | Changes are not announced (default) |
| `Polite` | Changes are announced after the screen reader finishes its current speech |
| `Assertive` | Changes interrupt current speech immediately |

| Platform | Mapping |
|----------|---------|
| Windows (Win32) | UIAutomation `LiveSetting` property |
| macOS | `NSAccessibility` notification |
| Web (WASM) | `aria-live="polite"` or `aria-live="assertive"` attribute |

> [!NOTE]
> Use `Assertive` sparingly — interrupting speech can be disorienting. Reserve it for urgent notifications like error messages.

## IsRequiredForForm

Indicates that the element requires user input before form submission.

```xml
<TextBox AutomationProperties.Name="Email"
         AutomationProperties.IsRequiredForForm="True" />
```

Screen readers typically announce this as "required" when the user focuses the field.

## FullDescription

Provides a longer description for complex elements when `HelpText` is not sufficient.

```xml
<Image Source="chart.png"
       AutomationProperties.Name="Revenue chart"
       AutomationProperties.FullDescription="Bar chart showing monthly revenue from January to December 2025, with a peak in September at $2.4M" />
```

## DescribedBy, FlowsTo, FlowsFrom

Establishes relationships between elements in the accessibility tree.

```xml
<TextBox x:Name="PasswordField"
         AutomationProperties.Name="Password" />
<TextBlock x:Name="PasswordHint"
           Text="Must contain at least 8 characters" />

<!-- Associate the hint text with the password field -->
<!-- In code-behind: AutomationProperties.GetDescribedBy(PasswordField).Add(PasswordHint); -->
```

- **DescribedBy** — Points to elements that provide additional description
- **FlowsTo** — Defines a custom reading order (next element)
- **FlowsFrom** — Defines a custom reading order (previous element)

## PositionInSet and SizeOfSet

Reports the position and total count when an element is part of a set.

```xml
<Button AutomationProperties.Name="Page 2"
        AutomationProperties.PositionInSet="2"
        AutomationProperties.SizeOfSet="5" />
```

Screen readers announce this as "2 of 5", helping users understand their position within a group. For `ListView`/`ListViewItem`, these are set automatically by the framework.

## IsDialog

Marks an element as a dialog container. Screen readers use this to scope navigation within the dialog.

```xml
<Grid AutomationProperties.IsDialog="True">
    <TextBlock Text="Confirm delete?" />
    <Button Content="Yes" />
    <Button Content="No" />
</Grid>
```

## Additional properties

The following properties are also supported but less commonly used:

| Property | Description |
|----------|-------------|
| `AcceleratorKey` | Keyboard shortcut combination (e.g., "Ctrl+S") |
| `AccessKey` | Mnemonic / access key |
| `ItemStatus` | Status information (e.g., "New", "Busy") |
| `ItemType` | Type of item represented |
| `LocalizedControlType` | Localized control type string (overrides the default) |
| `Level` | Hierarchical level in an outline or set |
| `IsPeripheral` | Whether the element is peripheral to the main UI |
| `IsDataValidForForm` | Whether the element's value is valid for form submission |
| `Culture` | Locale identifier for the element |

## Tips

- **Always localize** `AutomationProperties.Name`. In XAML use the resource naming convention:
  `MyButton.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name`
- **Avoid `Opacity="0"` and `IsHitTestVisible="False"`** to hide elements. Use `Visibility="Collapsed"` instead — screen readers can still focus invisible elements with non-collapsed visibility.
- **Prefer `Inlines`** inside a single `TextBlock` over stacking multiple `TextBlock` elements in a panel. This lets the screen reader read all the text at once.
- **Use a converter to trim long text.** While a `TextBlock` may ellipsize visually, the screen reader reads the entire source text.
- **Set `AppBarButton.Label`** even when it is not visually displayed — it is used by the screen reader.

## See also

- [Accessibility overview](index.md)
- [Custom automation peers](automation-peers.md)
- [Controls accessibility reference](controls-reference.md)
- [Testing with screen readers](testing-with-screen-readers.md)
