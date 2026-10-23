---
uid: Uno.Features.Accessibility.TestingWithScreenReaders
---

# Testing with screen readers

> [!TIP]
> For general screen reader setup, navigation shortcuts, and testing methodology, see [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing).

This guide covers Uno-specific steps for verifying accessibility in your application.

## Enabling the accessibility layer (WASM)

On WASM Skia targets, the accessibility layer activates when the user first presses the `Tab` key. A visually hidden **"Enable accessibility"** element becomes reachable via Tab or screen reader — activate it (click, `Enter`, or `Space`) before the full semantic tree becomes available.

To activate manually from browser DevTools:

```js
document.getElementById('uno-enable-accessibility').click();
```

For automated testing scenarios where you need the accessibility layer ready before any user interaction, you can force it on at startup:

```csharp
FeatureConfiguration.AutomationPeer.AutoEnableAccessibility = true;
```

> [!WARNING]
> `AutoEnableAccessibility` materializes and continuously maintains the full semantic DOM for the lifetime of the app, which has a significant runtime cost (every visual-tree change updates the semantic overlay). It is intended for testing and debugging — leave it disabled in production so the cost is only paid when an assistive technology actually requests accessibility. Set it before the host is built (typically in `App.xaml.cs` before `MainWindow` is created); it is read once during accessibility subsystem initialization.
> [!NOTE]
> On Windows (Win32) and macOS, the accessibility tree is always active — no manual activation is required.
> On Linux (X11), the tree is published while the desktop has accessibility enabled (AT-SPI `org.a11y.Status`), for example after turning on the screen reader in the desktop's accessibility settings.

## Browser and screen reader pairing (WASM)

When testing WASM apps with a screen reader, the browser choice affects results:

| Browser | Screen Reader | Notes |
|---------|---------------|-------|
| Chrome | NVDA (Windows) | Best overall support for ARIA in Chromium-based browsers. |
| Firefox | NVDA (Windows) | Good alternative; Firefox has its own accessibility engine. |
| Safari | VoiceOver (macOS) | Best VoiceOver experience. |
| Chrome | VoiceOver (macOS) | Works, but Safari is recommended. Enable Full Keyboard Access in System Settings → Keyboard. |

## Using the SamplesApp

The `AccessibilityScreenReaderPage` sample in the Uno SamplesApp includes test sections for common control types, and `Accessibility_Showcase` covers one screen-reader capability per section (labels, toggles, ranges, lists, expanders, form validation, live regions, dialogs, landmarks, hidden content):

1. Build and run the SamplesApp (`SamplesApp`, `net11.0-desktop`)
2. Navigate to the `Accessibility_ScreenReader` or `Accessibility_Showcase` sample
3. Enable your screen reader and Tab into the app
4. On WASM, activate the "Enable accessibility" button first

## Debugging the accessibility tree

### WASM — inspecting the semantic DOM

On WASM, look for the `#uno-semantics-root` container in the DOM. It contains hidden semantic overlay elements (buttons, inputs, headings, etc.) that the screen reader interacts with. Each element has `aria-label` and the appropriate `role` attribute.

Inspect using browser DevTools:

- **Chrome:** DevTools → Elements → Accessibility pane (right sidebar)
- **Firefox:** DevTools → Accessibility tab
- **Safari:** Develop → Show Web Inspector → Elements → Node → Accessibility

### Linux — inspecting the AT-SPI tree

On Linux (X11), each app window currently appears as its own application (named after the app) on the AT-SPI desktop. Browse its tree, roles, states and actions with [Accerciser](https://gitlab.gnome.org/GNOME/accerciser), or with any AT-SPI client such as the `Atspi` GObject bindings.

### Common issues

| Problem | Possible cause | Fix |
|---------|---------------|-----|
| Nothing is announced (WASM) | Accessibility layer not activated | Press `Tab`, then activate the "Enable accessibility" button |
| App missing from Orca or Accerciser (Linux) | Desktop accessibility is off | Turn on the screen reader in the desktop's accessibility settings (`org.a11y.Status` must report `IsEnabled` or `ScreenReaderEnabled`), and make sure at-spi2-core is installed. Debug logging for `Uno.UI.Runtime.X11` shows why the bridge did not start |
| Wrong label announced | `AutomationProperties.Name` not set or wrong `LabeledBy` target | Check `aria-label` in the semantic DOM |
| Headings not in Rotor | Missing `HeadingLevel` property | Verify `AutomationProperties.HeadingLevel` is set; on WASM check for `<h1>`–`<h6>` elements |
| Landmarks not listed | Missing `LandmarkType` property | Verify `AutomationProperties.LandmarkType` is set; on WASM check for `role="navigation"` etc. |
| Live region not announcing | `LiveSetting` not set or content not changing | On WASM, verify `aria-live` attribute exists on the semantic element |
| VoiceOver silent in Chrome | Known Chrome limitation | Test in Safari for best VoiceOver support; in Chrome, enable Full Keyboard Access in System Settings → Keyboard |

## See also

- [Accessibility overview](xref:Uno.Features.Accessibility)
- [AutomationProperties reference](xref:Uno.Features.Accessibility.AutomationProperties)
- [Custom automation peers](xref:Uno.Features.Accessibility.AutomationPeers)
- [Role override](xref:Uno.Features.Accessibility.RoleOverride)
- [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing)
