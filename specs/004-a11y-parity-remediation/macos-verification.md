# macOS accessibility verification guide

Some accessibility parity fixes on branch `dev/doti/a11y-parity-remediation-impl` change the **macOS
(NSAccessibility) backend**, which cannot be runtime-validated on a Windows CI machine (Win32
overrides the shared routing, so Windows runtime tests never exercise the macOS paths). This
document lists those changes with concrete, reproducible verification steps for an agent running on
**macOS**.

## Prerequisites (run once on macOS)

```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props   # if not present
# edit it to uncomment:  <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride> and <UnoFastDevBuild>true</UnoFastDevBuild>

# Build + run the Skia macOS SamplesApp head
dotnet build ../src/SamplesApp/SamplesApp.Skia.MacOS/SamplesApp.Skia.MacOS.csproj -c Release -f net10.0-maccatalyst -p:UnoFastDevBuild=true
# (or the net10.0 macOS head used in this repo — pick the SamplesApp.Skia.macOS project that exists)
```

Tooling to inspect the live NSAccessibility tree:
- **Accessibility Inspector** (ships with Xcode: `Xcode > Open Developer Tool > Accessibility Inspector`) — point it at the SamplesApp window and inspect roles, values, `AXValue`, actions, and notifications.
- **VoiceOver** (⌘+F5) — for end-user behavior (announcements, focus).
- Optional: **Appium + Mac2 driver** / XCUITest for scripted assertions on `accessibilityIdentifier`, roles, values.

Runtime tests can also be run on the macOS Skia head via the `/runtime-tests` skill with the macOS target; the automation-namespace tests (`Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.*`) should stay green.

---

## SH-02 — EventsSource-aware routing (property changes + automation events)

**What changed.** The shared `SkiaAccessibilityBase.NotifyPropertyChangedEventCore`
(`src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`) and the macOS
`MacOSAccessibility.NotifyAutomationEvent` override
(`src/Uno.UI.Runtime.Skia.MacOS/Accessibility/MacOSAccessibility.cs`) now resolve the peer's
**EventsSource** (`peer = peer.ResolveProviderPeer(resolveEventsSource: true)`) before routing to
the platform `Update*` / notification calls. This matches WinUI and the Win32 backend (which already
uses `FindExistingProviderForPeer(peer, resolveEventsSource: true)`). For `ListItem`/`TabItem`/
`TreeItem` peers this maps the raw container peer to the **data automation peer** the client sees;
for every other peer it is a no-op (returns `this`).

**Why it matters.** Without it, a property change (e.g. `IsSelected`, `Name`, `Value`) or automation
event (e.g. `SelectionItemPatternOnElementSelected`, `LiveRegionChanged`) raised on a ListView /
TabView / TreeView item was attributed to the raw container peer instead of the data peer, so
VoiceOver could announce the change against the wrong element or miss it.

**How to verify on macOS.**

1. Run the SamplesApp and open a **ListView** sample with selectable items (e.g. `ListView` category)
   and a **TabView** sample.
2. Open Accessibility Inspector, enable **notifications** logging, and turn on VoiceOver.
3. **Selection change:** select a different ListView item (click or arrow keys).
   - *Expected:* the `AXSelectedChildrenChanged` / value-changed notification and VoiceOver
     announcement are attributed to the **item element** (the row), and VoiceOver reads the newly
     selected row's label. Before the fix, the announcement could be attributed to the container or
     dropped.
4. **TabView / TreeView:** switch tabs / expand a tree node and confirm the selection/expand
     announcement targets the tab item / tree item element, not the parent.
5. **Regression check:** exercise non-item controls (Button, Slider, CheckBox, TextBox). Their
   property-change announcements must be **unchanged** (the fix is a no-op for them).
6. Confirm no new crashes/hangs — `ResolveProviderPeer` navigates to the parent to generate the
   EventsSource; verify rapid selection changes in a large virtualized ListView stay responsive.

**Pass criteria:** item selection/property/event announcements target the correct item element;
non-item controls behave exactly as before; no crashes or perf regressions in virtualized lists.

---

## MAC-06 — AutomationId exposed as `accessibilityIdentifier`

**What changed.** `AutomationProperties.AutomationId` now crosses the P/Invoke boundary and is set as
the NSAccessibility `accessibilityIdentifier` on the native element (parity with the WASM
`xamlautomationid`). Touches four layers:
- `src/Uno.UI.Runtime.Skia.MacOS/Native/NativeUno.cs` — new `uno_accessibility_update_identifier` P/Invoke.
- `UNOAccessibility.h` — new `unoIdentifier` property + C declaration.
- `UNOAccessibility.m` — new `accessibilityIdentifier` override (returns `_unoIdentifier`) and the
  `uno_accessibility_update_identifier` C function (mirrors `uno_accessibility_update_help`).
- `MacOSAccessibility.ApplyAttributes` — pushes `peer.GetAutomationId()` when non-empty.

Managed side compiles on Windows; the Obj-C `.h`/`.m` changes are mirrored verbatim from the
existing `help` attribute and must be built on macOS.

**How to verify on macOS.**
1. Build the native `libUnoNativeMac.dylib` (the UnoNativeMac Xcode/clang build) and the SamplesApp.
2. Open a sample with `AutomationProperties.AutomationId` set (e.g. `Automation/AutomationProperties_AutomationId`, or the new `Automation/AutomationProperties_Relations` whose elements set ids like `rel-controller`).
3. In Accessibility Inspector, select the element and confirm its **Identifier** field equals the AutomationId.
4. Or via XCUITest/Appium (Mac2 driver): `app.buttons["rel-controller"]` (or the relevant id) should resolve the element.

**Pass criteria:** every element with an AutomationId exposes it as `accessibilityIdentifier`; elements without one expose no identifier; XCUITest/Appium can locate elements by AutomationId.

---

## MAC-01 — RadioButton (and Tab/TabItem) `AXValue` kept in sync on selection

**What changed.** `UNOAccessibility.m` `uno_accessibility_update_selected` now, for elements whose
computed role is `NSAccessibilityRadioButtonRole` (radio, tab, tabitem all map to it), also updates
`_unoValue` to `@"1"`/`@"0"` and posts `NSAccessibilityValueChangedNotification`. Previously only
`_unoIsSelected` was updated, but `accessibilityValue` reads `_unoValue`, so VoiceOver reported a
stale checked state after selection changed. Native-only fix (build on macOS to validate).

**How to verify on macOS.**
1. Build native + SamplesApp; enable VoiceOver.
2. Open a RadioButton group sample and a TabView/Pivot sample.
3. Change the selected radio (arrow keys / click). *Expected:* VoiceOver announces the newly selected
   radio as "selected/checked" and the previously selected one as "not selected"; Accessibility
   Inspector shows the new radio's **Value = 1** and the old one **Value = 0**.
4. Switch tabs: the newly selected tab's value updates to 1 (old tab → 0).

**Pass criteria:** after any selection change, the radio/tab `AXValue` reflects the current selection
(no stale value); a value-changed notification fires so VoiceOver re-reads it.
