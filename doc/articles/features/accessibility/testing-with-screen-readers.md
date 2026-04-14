---
uid: Uno.Features.Accessibility.TestingWithScreenReaders
---

# Testing with screen readers

This guide covers how to set up and use screen readers to verify accessibility in your Uno Platform application. It targets both developers testing during development and QA testers performing dedicated accessibility validation.

## Enabling the accessibility layer (WASM)

On WASM Skia targets, the accessibility layer activates when the user first presses the `Tab` key. An **"Enable accessibility"** button appears on the page — click it or press `Space` to activate the full semantic tree.

If the button does not appear or does not work as expected, you can activate it manually from browser DevTools:

```js
document.getElementById('uno-enable-accessibility').click();
```

After activation, tabbing through the app and using screen reader navigation should announce elements correctly.

> [!NOTE]
> On Windows (Win32) and macOS, the accessibility tree is always active — no manual activation is required.

## Screen reader setup

### macOS — VoiceOver (built-in)

VoiceOver is included with macOS. No installation needed.

- **Toggle on/off:** `Cmd+F5` (or triple-press Touch ID)
- **VoiceOver key (VO):** `Ctrl+Option` by default
- Best browser support: **Safari**

> [!TIP]
> If VoiceOver doesn't read elements in Chrome, enable **Full Keyboard Access** in System Settings → Keyboard.

### Windows — NVDA (free, recommended)

Download from [nvaccess.org](https://www.nvaccess.org/download/).

- **Toggle on/off:** `Ctrl+Alt+N`
- **NVDA modifier key:** `Insert` by default

### Windows — Narrator (built-in)

- **Toggle on/off:** `Win+Ctrl+Enter`
- **Narrator modifier key:** `Caps Lock` by default

## Quick reference: navigation shortcuts

### Tab navigation (all screen readers)

`Tab` / `Shift+Tab` moves between **interactive controls only** — buttons, links, inputs, sliders, checkboxes. Headings and landmarks are not reachable via Tab.

### VoiceOver (macOS)

| Action | Shortcut |
|--------|----------|
| Move to next item | `VO+Right` |
| Move to previous item | `VO+Left` |
| Activate (click) | `VO+Space` |
| Open Rotor | `VO+U` |
| Next heading | `VO+Cmd+H` |
| Previous heading | `VO+Cmd+Shift+H` |
| Next landmark | *Use Rotor (`VO+U`) → Landmarks* |
| Read current item | `VO+F3` |

The **Rotor** (`VO+U`) is the most powerful navigation tool. It shows categorized lists of headings, landmarks, links, and form controls. Use left/right arrows to switch categories, up/down to pick an item, `Enter` to jump to it.

### NVDA (Windows)

| Action | Shortcut |
|--------|----------|
| Next heading | `H` |
| Previous heading | `Shift+H` |
| Heading by level (1–6) | `1`–`6` / `Shift+1`–`Shift+6` |
| Next landmark | `D` |
| Previous landmark | `Shift+D` |
| Next link | `K` |
| Next button | `B` |
| Next form field | `F` |
| Elements list (like Rotor) | `NVDA+F7` |
| Toggle browse/focus mode | `NVDA+Space` |

> [!NOTE]
> **Browse mode vs Focus mode:** NVDA starts in browse mode where single-letter shortcuts (`H`, `D`, `K`) navigate by element type. When you enter a text field or interactive widget, it switches to focus mode (keyboard input goes to the control). Press `NVDA+Space` to toggle manually.

### Narrator (Windows)

| Action | Shortcut |
|--------|----------|
| Next heading | `H` |
| Previous heading | `Shift+H` |
| Next landmark | `D` |
| Previous landmark | `Shift+D` |
| List all headings | `Narrator+F5` |
| List all landmarks | `Narrator+F7` |
| Scan mode toggle | `Narrator+Space` |

## What to test

Use the following checklist to verify accessibility. For each control type, check that the screen reader announces the expected information and that keyboard interactions work correctly.

### Buttons

- [ ] Tab to the button — hear label + "button"
- [ ] Press `Space` or `Enter` to activate
- [ ] Disabled buttons announce as "dimmed" or "unavailable"

### Checkboxes and radio buttons

- [ ] Tab to the control — hear label + role + checked/selected state
- [ ] Toggle with `Space` — state change is announced
- [ ] Radio buttons announce group membership and position ("1 of 3")

### Toggle buttons and switches

- [ ] Tab to the control — hear label + "toggle button"/"switch" + pressed/on state
- [ ] Toggle with `Space` — state change announced immediately

### Sliders

- [ ] Tab to slider — hear label + current value
- [ ] Arrow keys change the value — new value is announced
- [ ] VoiceOver: use `VO+Up/Down` to adjust

### Text fields

- [ ] Tab into the field — hear label + "text field"
- [ ] Typed text is read back character by character
- [ ] Password fields do NOT read back characters
- [ ] Multi-line fields (`AcceptsReturn="True"`) announce as "text area"
- [ ] Required fields announce "required"

### Headings

- [ ] Navigate with `H` (NVDA/Narrator) or `VO+Cmd+H` (VoiceOver)
- [ ] Hear text + "heading level X"
- [ ] Headings should NOT be reachable via `Tab`
- [ ] Use the Rotor (`VO+U`) or Elements List (`NVDA+F7`) to see all headings at once

### Landmarks

- [ ] Navigate with `D` (NVDA/Narrator) or Rotor → Landmarks (VoiceOver)
- [ ] Hear the landmark type ("navigation", "main", "search")
- [ ] Custom landmarks with `LocalizedLandmarkType` announce the custom description

### Live regions

- [ ] Navigate away from the live region (Tab to something else)
- [ ] Trigger a content change (e.g., click a button that updates a counter)
- [ ] **Polite** regions: new text announced after current speech finishes
- [ ] **Assertive** regions: new text interrupts current speech

### Combo boxes

- [ ] Tab to the combo box — hear selected value + "combo box" + "collapsed"
- [ ] Open with `Space` or `Enter` — hear "expanded"
- [ ] Arrow keys navigate items — selection is announced
- [ ] `Enter` to select, `Escape` to close

### Links

- [ ] Tab to the link — hear label + "link"
- [ ] Activate with `Enter`
- [ ] NVDA: press `K` to jump between links

### Lists

- [ ] Navigate into the list — items announce position ("1 of 5")
- [ ] Arrow keys move between items

## Using the SamplesApp

The `AccessibilityScreenReaderPage` sample in the Uno SamplesApp includes test sections for all of the above control types. To use it:

1. Build and run the SamplesApp (`SamplesApp.Skia.Generic`)
2. Navigate to the `Accessibility_ScreenReader` sample
3. Enable your screen reader and Tab into the app
4. On WASM, activate the "Enable accessibility" button first

## Debugging

### Inspecting the accessibility tree

Each browser's DevTools lets you inspect the accessibility tree — what the screen reader actually sees:

- **Chrome:** DevTools → Elements → Accessibility pane (right sidebar)
- **Firefox:** DevTools → Accessibility tab
- **Safari:** Develop → Show Web Inspector → Elements → Node → Accessibility

> [!TIP]
> Enable "Show Develop menu" in Safari preferences first.

On WASM, look for the `#uno-semantics-root` container in the DOM. It contains the hidden semantic overlay elements (buttons, inputs, headings, etc.) that the screen reader interacts with. Each element has `aria-label` and the appropriate `role` attribute set by the accessibility layer.

### Common issues

| Problem | Possible cause | How to fix |
|---------|---------------|------------|
| Nothing is announced | Accessibility layer not activated | Press `Tab`, then activate the "Enable accessibility" button. Or run `document.getElementById('uno-enable-accessibility').click()` in DevTools |
| Wrong label announced | `AutomationProperties.Name` not set, or wrong element referenced by `LabeledBy` | Check the `aria-label` attribute in the semantic DOM. Verify `AutomationProperties.Name` is set on the correct element |
| Headings not in Rotor/Elements list | Missing `HeadingLevel` property | Verify `AutomationProperties.HeadingLevel` is set. On WASM, check that the semantic element is rendered as `<h1>`–`<h6>` |
| Landmarks not listed | Missing `LandmarkType` property | Verify `AutomationProperties.LandmarkType` is set. On WASM, check for `role="navigation"` / `role="main"` etc. on the semantic element |
| Live region not announcing | Content not actually changing, or `aria-live` not set | Check that the element has `AutomationProperties.LiveSetting` set. On WASM, verify `aria-live` attribute exists. Some screen readers need a brief delay between the initial `aria-live` attribute and the first content change |
| VoiceOver silent in Chrome | Known Chrome limitation | Test in Safari for best VoiceOver support. In Chrome, enable "Full Keyboard Access" in System Settings → Keyboard |

## See also

- [Accessibility overview](index.md)
- [Controls accessibility reference](controls-reference.md)
- [Accessibility testing (Microsoft Learn)](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing)
