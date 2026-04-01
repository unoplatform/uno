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
> On WASM, the accessibility layer activates when the user first presses the `Tab` key. An "Enable accessibility" button appears that must be activated (clicked or via `Space`) before the full semantic tree is available. This is done to avoid performance overhead when accessibility is not needed.

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
| [AutomationProperties reference](automation-properties.md) | Supported `AutomationProperties` with per-platform mappings |
| [Custom automation peers](automation-peers.md) | Skia accessibility architecture and ARIA role mappings |
| [Role override](role-override.md) | Uno-specific `AutomationPropertiesExtensions.Role` attached property for explicit ARIA role control |
| [Testing with screen readers](testing-with-screen-readers.md) | WASM activation, SamplesApp testing, and debugging the accessibility tree |

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

## Text scaling (iOS and Android native rendering)

On iOS and Android with native rendering, the OS provides accessibility text scaling. To opt out:

```csharp
Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor = true;
```

> [!NOTE]
> On Skia targets, text scaling is handled by the OS or browser zoom level and is not controlled by this property.

## SimpleAccessibility mode (legacy)

> [!IMPORTANT]
> This applies only to **iOS and Android native rendering**. It is not used on Skia targets.

On iOS, VoiceOver reads all inner accessible names of a list item concatenated but does not let the user focus individual children. SimpleAccessibility mode brings this behavior to Android for consistency:

```csharp
#if __IOS__ || __ANDROID__
FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
#endif
```

## See also

- [Expose basic accessibility information (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information)
- [Custom automation peers (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
- [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing)
