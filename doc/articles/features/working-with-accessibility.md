---
uid: Uno.Features.Accessibility.Legacy
---

# Accessibility

> [!IMPORTANT]
> This page has been replaced by the new [Accessibility documentation](accessibility/index.md).
>
> The new documentation covers the current Skia-based rendering accessibility implementation across WASM, macOS, Win32, and more.

Please visit the updated pages:

- **[Accessibility overview](accessibility/index.md)** — Introduction, platform support, and getting started
- **[AutomationProperties reference](accessibility/automation-properties.md)** — All supported properties with XAML examples
- **[Custom automation peers](accessibility/automation-peers.md)** — How to create peers for custom controls
- **[Role override](accessibility/role-override.md)** — Uno-specific `AutomationPropertiesExtensions.Role`
- **[Testing with screen readers](accessibility/testing-with-screen-readers.md)** — WASM activation, SamplesApp testing, and debugging

## Legacy notes

- You can disable accessibility focus of native elements using `android:ImportantForAccessibility="No"` and `ios:IsAccessibilityElement="False"`.
- `ContentControl` based controls (`Button`, `CheckBox`, ...) automatically use the string representation of their `Content` property. In order for the `AutomationProperties.AutomationId` property to be selectable, add `AutomationProperties.AccessibilityView="Raw"` to the control as well.

## Enabling the screen reader

### Narrator (Windows)

1. Press **Windows key** and **Enter** at the same time.

### VoiceOver (iOS)

1. Launch the **Settings** app from your Home screen.
2. Tap on **General**.
3. Tap on **Accessibility**.
4. Tap on **VoiceOver** under the Vision category at the top.
5. Tap the **VoiceOver switch** to enable it.

### TalkBack (Android)

1. Launch the **Settings** app from your launch screen.
2. Tap on **Accessibility**.
3. Tap on **TalkBack**.
4. Tap the **switch** to enable it.
5. Tap the **OK** button to close the dialog.

### VoiceOver (macOS)

1. Launch the **System Preferences** from the macOS logo.
2. Tap on **Accessibility**.
3. Tap on **VoiceOver** under the Vision category at the top.
4. Tap the **Enable VoiceOver switch** to enable it.
