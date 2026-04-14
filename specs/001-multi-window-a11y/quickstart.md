# Quickstart: Multi-Window Accessibility Manual Validation

**Feature**: 001-multi-window-a11y
**Applies to**: Skia Desktop Win32 (PR 1) and Skia Desktop macOS (PR 2)

This checklist supplements the automated runtime test `Given_MultiWindowAccessibility`. Execute it with a real screen reader against the SamplesApp (or a minimal two-window repro sample) before shipping each PR.

---

## Prerequisites

- Skia Desktop SamplesApp built for the target platform.
- A secondary-window sample path available — either use `SamplesApp` and invoke `Window.Create` from a sample, or add a temporary debug sample that opens a secondary window on click.
- Screen reader:
  - **Windows**: Narrator (Win + Ctrl + Enter) with Scan Mode available (Caps Lock + Space).
  - **macOS**: VoiceOver (Cmd + F5) with the rotor (VO + U) and item chooser (VO + I) available.
- Recommended: enable `Uno.Foundation.Logging` at `Debug` to surface diagnostic traces for dropped events (FR-017, FR-018) during validation.

---

## Windows / Narrator checklist (PR 1 and PR 2)

### Tree coverage

- [ ] Launch SamplesApp. Enable Narrator.
- [ ] Open a secondary window from a sample.
- [ ] With focus on the primary window, press **Caps Lock + Right/Left** to traverse its controls. Narrator announces controls from the primary window.
- [ ] Activate the secondary window (click its title bar or Alt+Tab). Traverse with **Caps Lock + Right/Left**. Narrator announces controls from the secondary window (not primary).
- [ ] **Without activating**, use Narrator's Scan Mode across windows (Ctrl + Alt + arrows, or use the item chooser Ctrl + Alt + Q) and verify each window's controls are enumerable.
- [ ] Give both windows identical control labels. Verify each window's announcements match its own state (no leakage).

### Focus

- [ ] Move focus within the primary window. Announcements follow the keyboard focus.
- [ ] Alt+Tab to the secondary window. The previously focused secondary-window control is re-announced on activation.
- [ ] Disable the currently focused control via a sample command. Narrator reports focus recovery within the same window.

### Announcements

- [ ] Trigger a polite announcement from the primary window. Narrator speaks it.
- [ ] Trigger the *same text* polite announcement from the secondary window within 200 ms. Narrator speaks both (no cross-window dedup).
- [ ] Trigger an assertive announcement from the secondary window while the primary is active. Narrator speaks it associated with the secondary window.
- [ ] Trigger a framework-level source-less announcement (via an internal test hook) while the primary is active. Narrator speaks it associated with the primary.
- [ ] Alt+Tab away from the app entirely, then trigger a source-less announcement. Upon returning, no crash; last-active behavior is observed.

### Disposal

- [ ] Open secondary. Close secondary. Narrator continues to announce in the primary without stutter.
- [ ] Open secondary. Close primary first. Narrator continues to announce in the remaining secondary.
- [ ] Open and close the secondary window 100 times in a row (via a stress sample). No crash. Resident memory does not grow proportionally.
- [ ] While Narrator is mid-query on a secondary element, close the secondary. No hang, no crash; the query completes with a disconnected provider response.

---

## macOS / VoiceOver checklist (PR 2 only)

### Tree coverage

- [ ] Launch SamplesApp. Enable VoiceOver.
- [ ] Open a secondary window from a sample.
- [ ] With focus on the primary window, use **VO + arrow keys** to traverse its controls. VoiceOver announces primary controls.
- [ ] Activate the secondary window. Traverse with **VO + arrows**. VoiceOver announces secondary controls.
- [ ] With the primary window active, open the **Item Chooser (VO + I)**. Verify controls from both windows appear. Navigate to a secondary-window control; VoiceOver announces it correctly.
- [ ] Give both windows identical labels. Verify no cross-window leakage.

### Focus

- [ ] Move focus in the primary window. VoiceOver cursor tracks it.
- [ ] Cmd+` (backtick) between windows. Focus lands on the last-focused control in each.
- [ ] Disable a focused control via a sample command. VoiceOver reports focus recovery within the same window.

### Announcements

- [ ] Trigger a polite announcement from the primary. VoiceOver speaks.
- [ ] Within 200 ms, trigger the same polite announcement from the secondary. Both announcements are spoken.
- [ ] Assertive announcement from the secondary while the primary is main. Spoken in the secondary's context.
- [ ] Source-less announcement while primary is main. Spoken in primary's context.
- [ ] Switch away from the app entirely (Cmd+Tab). Trigger a source-less announcement via a scheduled callback. On return, no crash; last-active behavior observed.

### Disposal & stress (CRITICAL — this is the primary macOS regression path)

- [ ] Open secondary. Close secondary. VoiceOver continues on the primary.
- [ ] Open secondary. Close primary first. VoiceOver continues on the remaining secondary.
- [ ] **Rapid create/destroy stress**: from a sample command or debug hook, loop 100 iterations of create-then-immediately-close a secondary window with VoiceOver enabled. **Expected**: no segfault, no hang. This directly validates the original bug behind `MacOSWindowNative.cs:54`.
- [ ] VoiceOver queries mid-close: while VoiceOver is navigating a secondary's tree, close the secondary. Expect graceful announcement of the item disappearing; no crash.
- [ ] Memory: after the 100-iteration stress run, resident memory is within noise of the baseline (no linear growth).

---

## Diagnostic traces to watch for

Enable `LogLevel.Debug` or `LogLevel.Trace` on:

- `Uno.UI.Runtime.Skia.Accessibility.AccessibilityRouter` — dropped source-less announcements (FR-008), failed peer resolution (FR-018).
- `Uno.UI.Runtime.Skia.SkiaAccessibilityBase` — pre-initialization events (FR-017), disposed-instance callbacks.
- `Uno.UI.Runtime.Skia.Win32.Win32Accessibility` — provider cleanup, structure-change coalescing.

Unexpected traces during the checklist indicate design-contract violations and must be root-caused before shipping.

---

## Automated gate

Before running the manual checklist, confirm the automated gate passes:

```bash
# From src/
dotnet build Uno.UI-Skia-only.slnf --no-restore
# Then invoke the runtime-tests skill with the multi-window test class:
#   /runtime-tests Given_MultiWindowAccessibility
# Also run the full accessibility suite to confirm no single-window regression:
#   /runtime-tests Windows_UI_Xaml_Automation
```

All runtime tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/` must pass unmodified (SC-006) in addition to the new `Given_MultiWindowAccessibility` (SC-007).
