---
uid: Uno.Features.Accessibility
---

# Accessibility

> [!TIP]
> This article covers Uno-specific information for accessibility support. For a full description of the WinUI accessibility model and design guidelines, see [Accessibility overview](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-overview).

Uno Platform implements the WinUI [UI Automation](https://learn.microsoft.com/windows/desktop/WinAuto/uiauto-uiautomationoverview) framework to make your applications accessible to screen readers and other assistive technologies. The same `AutomationProperties` and `AutomationPeer` APIs you use on WinUI work across all Uno Platform targets — each platform maps them to its native accessibility layer.

| Platform               | Rendering | Assistive Technology           | Status |
|------------------------|-----------|--------------------------------|--------|
| Windows (Win32)        | Skia      | Narrator (via UIAutomation)    | ✔      |
| macOS                  | Skia      | VoiceOver                      | ✔      |
| Web (WASM)             | Skia      | Any screen reader (via ARIA)   | ✔      |
| Linux                  | Skia      | —                              | Planned |
| Android                | Skia      | TalkBack                       | WIP    |
| iOS                    | Skia      | VoiceOver                      | WIP    |

## How it works

On Skia-rendered targets, Uno maintains a **semantic accessibility tree** alongside the visual tree. When you set properties such as `AutomationProperties.Name` or `AutomationProperties.HeadingLevel`, the corresponding automation peer publishes that information to the platform's assistive technology:

- **Windows (Win32)** — Exposes a UIAutomation provider tree that Narrator and other UIAutomation clients can inspect.
- **macOS** — Creates native `NSAccessibility` elements so VoiceOver can navigate the application.
- **Web (WASM)** — Generates a hidden semantic DOM overlay with the appropriate [ARIA](https://www.w3.org/WAI/standards-guidelines/aria/) roles and attributes, making the app accessible to any browser-based screen reader.

> [!NOTE]
> On WASM, the accessibility layer activates when the user first presses the `Tab` key. A visually hidden **"Enable accessibility"** element becomes reachable via Tab or screen reader and must be activated (click, `Enter`, or `Space`) before the full semantic tree is available. This is done to avoid performance overhead when accessibility is not needed.

## Getting started

To make your Uno app accessible, follow the same patterns you would use on WinUI:

1. **Set accessible names** on interactive controls using `AutomationProperties.Name` or `AutomationProperties.LabeledBy`.
2. **Use headings** (`AutomationProperties.HeadingLevel`) so screen reader users can navigate the page structure.
3. **Define landmarks** (`AutomationProperties.LandmarkType`) to identify major regions of the UI.
4. **Announce dynamic content** with `AutomationProperties.LiveSetting` for live regions.
5. **Test with a screen reader** on each target platform.

```xml
<Page xmlns:auto="using:Microsoft.UI.Xaml.Automation">
    <StackPanel auto:AutomationProperties.LandmarkType="Main">
        <TextBlock Text="Settings"
                   auto:AutomationProperties.HeadingLevel="Level1" />

        <TextBox auto:AutomationProperties.Name="Display name"
                 auto:AutomationProperties.HelpText="Enter the name shown on your profile" />

        <Button Content="Save"
                auto:AutomationProperties.Name="Save settings" />
    </StackPanel>
</Page>
```

## Topics

| Topic | Description |
|-------|-------------|
| [AutomationProperties reference](xref:Uno.Features.Accessibility.AutomationProperties) | Supported `AutomationProperties` with per-platform mappings |
| [Custom automation peers](xref:Uno.Features.Accessibility.AutomationPeers) | Skia accessibility architecture and ARIA role mappings |
| [Role override](xref:Uno.Features.Accessibility.RoleOverride) | Uno-specific `AutomationPropertiesExtensions.Role` attached property for explicit ARIA role control |
| [Testing with screen readers](xref:Uno.Features.Accessibility.TestingWithScreenReaders) | WASM activation, SamplesApp testing, and debugging the accessibility tree |

## AccessibilitySettings

Some libraries depend on `AccessibilitySettings` to check for high contrast. On Uno targets, the properties return defaults unless overridden:

```csharp
var settings = new AccessibilitySettings();
settings.HighContrast;       // default: false
settings.HighContrastScheme; // default: "High Contrast Black"

// Override the defaults
WinRTFeatureConfiguration.Accessibility.HighContrast = true;
WinRTFeatureConfiguration.Accessibility.HighContrastScheme = "High Contrast White";
```

When `WinRTFeatureConfiguration.Accessibility.HighContrast` changes, the `AccessibilitySettings.HighContrastChanged` event is raised.

## SimpleAccessibility mode (legacy)

When SimpleAccessibility mode is enabled, an accessible element's name is the concatenation of the names of its visible children, and those children are excluded from accessibility focus. This mirrors how VoiceOver reads list items on iOS. To keep an element's children focusable, set `AutomationProperties.AccessibilityView` to `Raw` on that element.

```csharp
Uno.UI.FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
```

> [!NOTE]
> This mode doesn't match WinUI's accessibility model, and it prevents nesting interactive elements: only the outer element of a `Button` inside a `Button` is focusable.

## See also

- [Expose basic accessibility information (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information)
- [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
- [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing)
