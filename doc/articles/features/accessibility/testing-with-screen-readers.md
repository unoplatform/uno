---
uid: Uno.Features.Accessibility.TestingWithScreenReaders
---

# Testing with screen readers

> [!TIP]
> For general screen reader setup, navigation shortcuts, and testing methodology, see [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing).

This guide covers Uno-specific steps for verifying accessibility in your application.

## Enabling the accessibility layer (WASM)

On WASM Skia targets, the accessibility layer activates when the user first presses the `Tab` key. An **"Enable accessibility"** button appears — activate it (click or press `Space`) before the full semantic tree becomes available.

To activate manually from browser DevTools:

```js
document.getElementById('uno-enable-accessibility').click();
```

To skip the activation button entirely and enable the accessibility layer on startup:

```csharp
FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true;
```

> [!NOTE]
> On Windows (Win32) and macOS, the accessibility tree is always active — no manual activation is required.

## Using the SamplesApp

The `AccessibilityScreenReaderPage` sample in the Uno SamplesApp includes test sections for common control types:

1. Build and run the SamplesApp (`SamplesApp.Skia.Generic`)
2. Navigate to the `Accessibility_ScreenReader` sample
3. Enable your screen reader and Tab into the app
4. On WASM, activate the "Enable accessibility" button first

## Debugging the accessibility tree

### WASM — inspecting the semantic DOM

On WASM, look for the `#uno-semantics-root` container in the DOM. It contains hidden semantic overlay elements (buttons, inputs, headings, etc.) that the screen reader interacts with. Each element has `aria-label` and the appropriate `role` attribute.

Inspect using browser DevTools:

- **Chrome:** DevTools → Elements → Accessibility pane (right sidebar)
- **Firefox:** DevTools → Accessibility tab
- **Safari:** Develop → Show Web Inspector → Elements → Node → Accessibility

### Common issues

| Problem | Possible cause | Fix |
|---------|---------------|-----|
| Nothing is announced (WASM) | Accessibility layer not activated | Press `Tab`, then activate the "Enable accessibility" button |
| Wrong label announced | `AutomationProperties.Name` not set or wrong `LabeledBy` target | Check `aria-label` in the semantic DOM |
| Headings not in Rotor | Missing `HeadingLevel` property | Verify `AutomationProperties.HeadingLevel` is set; on WASM check for `<h1>`–`<h6>` elements |
| Landmarks not listed | Missing `LandmarkType` property | Verify `AutomationProperties.LandmarkType` is set; on WASM check for `role="navigation"` etc. |
| Live region not announcing | `LiveSetting` not set or content not changing | On WASM, verify `aria-live` attribute exists on the semantic element |
| VoiceOver silent in Chrome | Known Chrome limitation | Test in Safari for best VoiceOver support; in Chrome, enable Full Keyboard Access in System Settings → Keyboard |

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
- [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing)
