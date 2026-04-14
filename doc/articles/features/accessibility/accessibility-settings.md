---
uid: Uno.Features.Accessibility.Settings
---

# AccessibilitySettings and options

## AccessibilitySettings

Some external libraries or UI toolkits depend on the `AccessibilitySettings` class to check for high contrast settings. On all Uno Platform targets, the platform APIs for detecting high contrast are not available, so the properties return predefined defaults unless you override them manually:

```csharp
var accessibilitySettings = new AccessibilitySettings();
accessibilitySettings.HighContrast;       // default: false
accessibilitySettings.HighContrastScheme; // default: "High Contrast Black"

// Override the defaults
WinRTFeatureConfiguration.Accessibility.HighContrast = true;
WinRTFeatureConfiguration.Accessibility.HighContrastScheme = "High Contrast White";

accessibilitySettings.HighContrast;       // true
accessibilitySettings.HighContrastScheme; // "High Contrast White"
```

When the value of `WinRTFeatureConfiguration.Accessibility.HighContrast` is changed, the `AccessibilitySettings.HighContrastChanged` event is raised.

## Text scaling

On iOS and Android (native rendering), the operating system provides accessibility text scaling. You can opt out of this per-app:

```csharp
// App's constructor (App.cs or App.xaml.cs)
Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor = true;
```

> [!CAUTION]
> Apple [recommends keeping text sizes dynamic](https://developer.apple.com/videos/play/wwdc2017/245). Only disable text scaling if your app has a specific layout constraint that requires it.

> [!NOTE]
> This setting applies to **iOS and Android native rendering** only. On Skia targets (WASM, Win32, macOS, Linux), text scaling is handled by the operating system or browser zoom level and is not controlled by this property.

## SimpleAccessibility mode (legacy)

> [!IMPORTANT]
> SimpleAccessibility mode is a legacy feature that applies only to **iOS and Android native rendering** targets. It is **not used on Skia targets** (WASM, Win32, macOS). If you are building a new app using Skia rendering (the default since Uno 5.x), you do not need this mode.

On iOS, the native accessibility model does not allow nested accessible elements to be individually focused — when a list item is selected, VoiceOver reads all inner accessible names concatenated, but does not let the user focus individual children. SimpleAccessibility mode brings this same behavior to Android for consistency.

To enable it (for native rendering targets only):

```csharp
// App's constructor (App.cs or App.xaml.cs)
#if __IOS__ || __ANDROID__
FeatureConfiguration.AutomationPeer.UseSimpleAccessibility = true;
#endif
```

On Skia targets, the accessibility tree is built from automation peers by the shared `SkiaAccessibilityBase` infrastructure, and the native platform's assistive technology handles navigation order — so SimpleAccessibility mode is not applicable.

## Disabling accessibility (iOS and Android native rendering only)

The accessibility system can be disabled programmatically. This is primarily used for automated UI testing scenarios where the accessibility layer may interfere with test execution:

```csharp
AutomationConfiguration.IsAccessibilityEnabled = false;
```

> [!WARNING]
> Do not disable accessibility in production applications. This setting is intended for test automation only.

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
