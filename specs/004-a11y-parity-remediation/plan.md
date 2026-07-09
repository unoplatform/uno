# Accessibility / Automation Parity Remediation Plan

**Feature:** `004-a11y-parity-remediation`
**Branch:** `dev/doti/a11y-parity-remediation`
**Scope:** Skia backends — Win32 (UIA), WebAssembly (ARIA/AOM), macOS (NSAccessibility) — plus the shared `AutomationPeer` / `AriaMapper` / `SkiaAccessibilityBase` core.
**Status:** Implementation in progress on branch `dev/doti/a11y-parity-remediation-impl`. The
findings below remain the actionable backlog; the items marked ✅ under *Implementation status*
are done, runtime-tested, and validated against the live UIA tree.

### Implementation status (2026-07-09)

Phase 1 (shared foundations) — landed with runtime tests (Skia Desktop) + live UIA-tree
validation (managed UIA client + FlaUI) + rubber-duck review:

- ✅ **XP-01** — MenuBar/Table/Separator added to `AriaMapper.ControlTypeToRoleMap`; SplitButton/
  DropDownButton `aria-haspopup="menu"` (keyed on peer type so Expander is not misclassified);
  Win32 `UIA_TitleBarControlTypeId = 50037` mapped. Tests: `Given_AriaMapper`.
- ✅ **XP-04** — `RaiseNotificationEvent` wired on the Skia path to the accessibility listener
  (experimental `AutomationEvents.Notification` kept out of contract). Test: `Given_NotificationEvent`.
- ✅ **XP-03** — Disabled Invoke/Toggle/Select now throw `ElementNotEnabledException`
  (HRESULT `0x80040200`, set centrally); native activation helper `InvokeAutomationPeer` swallows
  it to stay crash-free. Test: `Given_DisabledActionException` (+ verified live: UIA client
  invoking a disabled button surfaces `UIA_E_ELEMENTNOTENABLED`).
- ✅ **SH-04** — `LandmarkTargetAutomationPeer` (internal) + `LandmarkType`/`LocalizedLandmarkType`
  promotion in `FrameworkElement.OnCreateAutomationPeer`; Win32 serves localized-only landmarks as
  Custom (UIAWrapper parity). Test: `Given_LandmarkPromotion`; sample: `AutomationProperties_Landmark`
  (verified live with FlaUI: Main/Navigation/Custom landmark types).
- ✅ **SH-02** — Shared `NotifyPropertyChangedEventCore` + macOS `NotifyAutomationEvent` resolve the
  peer's EventsSource (`ResolveProviderPeer(resolveEventsSource:true)`) before routing, matching
  Win32. macOS-only in effect → see `macos-verification.md`.
- ✅ **SH-03** — `RaiseStructureChangedEvent` routes (Skia) into the existing per-backend structure
  path (Win32 raises UIA StructureChanged; macOS posts children-changed), so custom peers overriding
  GetChildrenCore can signal changes. Test: `Given_StructureChangedEvent`. (Fine-grained
  ChildAdded/Removed = W32-05, follow-up.)

Phase 3 (Win32):
- ✅ **W32-08** — Win32 now raises `WindowOpened`/`WindowClosed` UIA events (TeachingTip producer).
- ✅ **W32-03** — Win32 serves `DescribedBy`/`ControllerFor`/`FlowsTo`/`FlowsFrom` relation provider
  arrays (were hard-coded null). Tests: `Given_AutomationRelations`; sample:
  `AutomationProperties_Relations` (verified live with FlaUI). Covers the relation half of W32-06.

Phase 5 (macOS — code-review + compile validated; runtime steps in `macos-verification.md`):
- ✅ **MAC-06** — `AutomationProperties.AutomationId` exposed as NSAccessibility
  `accessibilityIdentifier` (unblocks XCUITest/Appium) across P/Invoke + `UNOAccessibility.h/.m` + `ApplyAttributes`.
- ✅ **MAC-01** — `uno_accessibility_update_selected` keeps `_unoValue` (AXValue) in sync for
  RadioButton/Tab/TabItem on selection change (+ value-changed notification).

Phase 4 (WASM — validated on a published Skia-WASM head via Playwright DOM assertions, `Given_WasmAria` + `Given_WasmAriaRelations`):
- ✅ **WA-01** — `role=generic` (Custom) is ARIA name-prohibited; a named generic container is now
  promoted to `role=group` (permits a name), matching WinUI's named-container → UIA Group.
- ✅ **WA-04** — the factory no longer emits a flattened `aria-label` when a valid `aria-labelledby`
  IDREF is present (no double/competing naming, FR-019).
- ✅ **WA-02** — `aria-describedby`/`controls`/`flowto` gate each related element on
  `HasSemanticElement`, so a node-less (e.g. Collapsed) target no longer produces a dangling IDREF.
- **WA-05** — already handled: region/form gated on a name, main/nav/search kept unnamed (ARIA-valid).

**Deferred / not-actionable (with rationale):**
- **W32-06 ClickablePoint** — FlaUI/Appium already get a clickable point via UIA's bounding-rect
  fallback (`TryGetClickablePoint` returns true); no Uno peer overrides `GetClickablePointCore`, so an
  explicit ClickablePoint would be a no-op. Culture is empty (low value). The relation half landed with W32-03.
- **W32-02 TextEditTextChanged / LayoutInvalidated** — no Uno control raises these events, so backend
  handling would be dead code.
- **W32-05** (fine-grained Child​Added/Removed) and **XP-02** (live property-change consolidation) and
  the remaining **WA-\*** (WASM) / **MAC-\*** items remain open in the backlog below.

---

## 1. Context & goal

Uno's accessibility stack has matured significantly through a series of landed PRs. This plan
is the output of a **fresh, inspection-based parity audit** against the WinUI 3 reference
(open-source `microsoft-ui-xaml`, automation peers under
`src/dxaml/xcp/dxaml/lib/*AutomationPeer_Partial.cpp` and the UIA bridge under
`src/dxaml/xcp/win/…`). The goal is to confirm how close Uno now is to WinUI 3 and to
enumerate the remaining gaps with concrete, file-scoped fixes.

**Methodology.** Four independent audits were run in parallel, each diffing Uno against the
WinUI C++ reference:

| Audit | Surface |
|-------|---------|
| Win32 UIA | `src/Uno.UI.Runtime.Skia.Win32/Accessibility/*` (RawElementProvider, pattern wrappers, interop, events) |
| WASM ARIA | `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/*` + `ts/Runtime/{Accessibility,SemanticElements}.ts`; also verifies spec `003-wasm-a11y-remediation` completeness |
| macOS AX | `src/Uno.UI.Runtime.Skia.MacOS/Accessibility/*` + `UnoNativeMac/…/UNOAccessibility.{h,m}` |
| Shared core | `src/Uno.UI/UI/Xaml/Automation/**` peers, `src/Uno.UI/Accessibility/AriaMapper.cs`, `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`, `AccessibilityRouter.cs` |

Raw per-audit findings are archived in the session under `files/agent-*.md`.

A second independent audit pass (2026-07-09) added a dedicated map of the WinUI UIA bridge
itself (`UIAWindow.cpp`, `UIAWrapper.cpp`, `AutomationEventsHelper.cpp`, `framework.cpp`) and
cross-checked the four surfaces against it. It contributed items SH-03/04, W32-05–09,
WA-08/09, MAC-06–08, the producer-side notes in XP-02, and additional parity confirmations in
Section 10.

**Overall verdict.** Core structure, pattern-provider delegation, control-type coverage, and
the shared property-change plumbing are in good shape and largely match WinUI. The remaining
gaps cluster into **four cross-platform themes** (Section 3) plus a set of **platform-specific
issues** (Sections 4–7). None are architectural rewrites; most are additive routing/mapping
entries and a handful of correctness fixes.

**Severity legend.** `HIGH` = user-perceivable AT breakage or wrong data exposed to the
accessibility client. `MEDIUM` = missing live updates / relationships that degrade the AT
experience. `LOW` = polish, cache-staleness edge cases, or missing-but-rarely-hit peers.

---

## 2. Executive summary (severity-first)

| ID | Severity | Platform(s) | One-line |
|----|----------|-------------|----------|
| XP-01 | HIGH | macOS + WASM + Win32 | Control-type→role table gaps (MenuBar, SplitButton, Table, Separator; TitleBar on Win32) |
| XP-02 | HIGH/MED | shared + WASM + macOS + Win32 | Live property-change routing incomplete (ItemStatus, PosInSet/SizeOfSet, Orientation, IsRequiredForForm, FullDescription, LiveSetting, ControllerFor) |
| XP-03 | MEDIUM | shared (surfaced by Win32) | Disabled-action exception parity — peers silently no-op / throw wrong type instead of `UIA_E_ELEMENTNOTENABLED` |
| XP-04 | HIGH | shared + Win32 | `RaiseNotificationEvent` is a no-op despite full platform plumbing existing — built-in controls' announcements are dropped |
| W32-01 | HIGH | Win32 | `TextRange.GetChildren` returns non-COM managed providers |
| W32-02 | HIGH | Win32 | `TextEditTextChanged` + `LayoutInvalidated` events dropped |
| W32-03 | MEDIUM | Win32 | `ControllerFor` / `DescribedBy` relation properties hard-coded null |
| W32-04 | LOW | Win32 | Listener advise-count gating not implemented (raises unconditionally) |
| WA-01 | HIGH | WASM | `aria-label` emitted on `role=generic` (ARIA name-prohibited) |
| WA-02 | HIGH | WASM | `aria-describedby` / `controls` / `flowto` IDREFs can dangle |
| WA-03 | HIGH | WASM | PasswordBox / `PlaceholderText` value live-sync missing (spec 003 FR-009) |
| WA-04 | MEDIUM | WASM | `ResolveLabel` still flattens `LabeledBy` into `aria-label` (FR-019) |
| WA-05 | MEDIUM | WASM | Landmarks (`main`/`navigation`/`search`) can be emitted unnamed (FR-014) |
| WA-06 | MEDIUM | WASM | Generic fallback under-exposes pattern state; virtualized item ARIA incomplete (FR-007/021/031) |
| WA-07 | LOW | WASM | Stale/duplicate JS interop `create*` declarations drift from TS |
| MAC-01 | HIGH | macOS | RadioButton `AXValue` goes stale after selection changes |
| MAC-02 | HIGH | macOS | Scroll-position re-emission unwired (`TrySubscribeScrollSource` never called) |
| MAC-03 | MEDIUM | macOS | Dynamic tree add/remove posts no VoiceOver notification |
| MAC-04 | MEDIUM | macOS | ScrollBar increment/decrement actions advertised only for Slider role |
| MAC-05 | LOW | macOS | Several state updates mutate fields without posting AX notifications |
| SH-01 | LOW | shared | Missing non-generated peers (Hub, HubSection, SeekSlider, PopupRoot, Ink*, etc.) |
| SH-02 | MEDIUM | shared | Base routing resolves owner via `TryGetPeerOwner`, not EventsSource-aware resolution |
| SH-03 | MEDIUM | shared | `RaiseStructureChangedEvent` public API never reaches the listener (custom peers can't signal) |
| SH-04 | MEDIUM | Win32 + macOS | Landmark elements not force-promoted into the tree (no `LandmarkTargetAutomationPeer`) |
| W32-05 | HIGH | Win32 | StructureChanged raised as coarse `ChildrenInvalidated` only; WinUI-faithful ChildAdded/Removed work unmerged |
| W32-06 | MEDIUM | Win32 | Property holes: ClickablePoint, FlowsTo/FlowsFrom, Culture, AnnotationTypes/Objects, IsPeripheral |
| W32-07 | MEDIUM | Win32 | TextRange granularity shallow (Line/Paragraph = document) + null instead of UIA reserved values |
| W32-08 | MEDIUM | Win32 | `WindowOpened`/`WindowClosed` dropped by the backend event switch |
| W32-09 | LOW | Win32 | Bridge polish: MSAA WM_GETOBJECT pass-through, hwnd map flush on destroy, focus-raise gating, FrameworkId |
| WA-08 | MEDIUM | WASM | IsOffscreen/clipping never reflected (`GetGlobalBoundsWithOptions` stub) |
| WA-09 | LOW | WASM | aria-current / colspan / rowspan / generic valuetext missing; unused C# debounce infra |
| MAC-06 | MEDIUM | macOS | AutomationId not exposed as `accessibilityIdentifier` (blocks XCUITest/Appium) |
| MAC-07 | MEDIUM | macOS | RoleOverride + computed attributes (haspopup/invalid/keyshortcuts/valuetext/controls/…) never reach AppKit |
| MAC-08 | LOW | macOS | No showMenu/scroll actions; disclosure level hardcoded; approximate line rects; PasswordBox/RichEditBox selection |

---

## 3. Cross-platform themes (highest leverage — fix once, benefit everywhere)

### XP-01 — Control-type → role table gaps  `HIGH`

`AriaMapper.ControlTypeToRoleMap` is the **single shared role source** consumed by both WASM
(directly) and macOS (`ResolveRole` → `AriaMapper.GetAriaRole`, `SkiaAccessibilityBase.cs:576-577`).
Win32 has its own separate control-type table. Several entries are missing.

**Real functional gaps (peers return the type, mapper drops it):**

| Control type | Expected role | Evidence |
|--------------|---------------|----------|
| `MenuBar` | `menubar` | `AutomationProperties.uno.cs` already maps MenuBar; absent from `AriaMapper.cs:22-58` |
| `SplitButton` / `DropDownButton` | `aria-haspopup="menu"` on its ExpandCollapse | `aria-haspopup` only set for ComboBox/Menu/MenuItem — `AriaMapper.cs:310-322` |
| `Table` | `table` | valid ARIA role referenced in `AutomationProperties.uno.cs`, missing `AriaMapper.cs:22-58,101-123` |
| `Separator` | `separator` | same as above |

**macOS dead-mapping gap:** native AppKit role mappings already exist for `Calendar`,
`MenuBar`, `SplitButton`, `AppBar`, `SemanticZoom`, `TitleBar`
(`UNOAccessibility.m:134,149,178,187,189-190`) but are **unreachable** because `AriaMapper`
never emits those strings — everything falls to `unknown`/`group` (`UNOAccessibility.m:122-130,194`).
Uno peers do return these types (`CalendarViewAutomationPeer.cs:54-59`,
`MenuBarAutomationPeer.cs:24-25`, `SplitButtonAutomationPeer.cs:64-67`,
`AppBarAutomationPeer.cs:105-106`, `SemanticZoomAutomationPeer.cs:30-31`,
`TitleBarAutomationPeer.cs:14-15`).

**Win32 gap:** `AutomationControlType.TitleBar = 37` exists in the enum
(`AutomationControlType.cs:193-197`) but the Win32 mapping omits it and falls through to
`Custom` (`Win32RawElementProvider.cs:1313-1357`). WinUI maps `ACTTitleBar`→`TitleBar_ControlType`
(`UIAWindow.cpp:2078-2082`).

**Fix.**
1. Add `MenuBar => menubar`, `Table => table`, `Separator => separator` to
   `AriaMapper.ControlTypeToRoleMap`.
2. Add SplitButton/DropDownButton `aria-haspopup` handling in the haspopup logic
   (`AriaMapper.cs:310-322`).
3. For macOS, introduce a **control-type→native-role path** so AppKit roles that have no valid
   ARIA equivalent (Calendar, AppBar, SemanticZoom, TitleBar) are still reachable — do not
   route those exclusively through ARIA. Entries with no valid ARIA role should be
   *intentionally* special-cased, not left to accidental generic fallback.
4. Add `UIA_TitleBarControlTypeId = 50037` to the Win32 interop constants and map
   `AutomationControlType.TitleBar`.

### XP-02 — Live property-change routing is incomplete  `HIGH`/`MEDIUM`

WinUI's `RaiseAutomaticPropertyChanges` (matched by Uno at `AutomationPeer.mux.cs:517-598`)
raises a broad set of property changes, but the **consumers** on each backend don't handle all
of them. Three consumers are involved and each is missing branches:

- Shared base switch: `SkiaAccessibilityBase.NotifyPropertyChangedEventCore` (`SkiaAccessibilityBase.cs:276-366`)
- WASM hand-maintained switch: `WebAssemblyAccessibility.cs:2017-2306` (does **not** delegate to base — spec 003 FR-010)
- Win32 property map: `Win32Accessibility.MapAutomationPropertyToUia` (`Win32Accessibility.cs:775-843`)

**Missing / partial branches:**

| Property | Gap |
|----------|-----|
| `ItemStatusProperty` | auto-raised by peer, but no update branch in shared base (`AutomationPeer.mux.cs:562-568`) |
| `PositionInSetProperty` / `SizeOfSetProperty` | WASM handles (`WebAssemblyAccessibility.cs:2259-2279`); base/macOS + Win32 map incomplete |
| `IsRequiredForFormProperty` | initial ARIA only; no live branch (only `IsDataValidForForm` handled — `WebAssemblyAccessibility.cs:2294-2305`) |
| `OrientationProperty` | no live branch on any backend |
| `LiveSettingProperty` | Win32 maps it; live `aria-live`/NS update on change missing (WASM/macOS) |
| `FullDescriptionProperty` | initial description uses it; live update only handles HelpText (`SkiaAccessibilityBase.cs:314-318`) |
| `ControllerFor` / `ControlledPeers` | WASM → `aria-controls` (`WebAssemblyAccessibility.cs:2239-2247`); base/macOS lack equivalent |
| `ValueValue` vs text | shared base routes **all** `ValuePatternIdentifiers.ValueProperty` to `UpdateTextValue` (`SkiaAccessibilityBase.cs:297-300`); WASM correctly separates ComboBox value from textbox text (`WebAssemblyAccessibility.cs:2150-2175`) |

**Fix.** The strategic fix is spec-003 **FR-010**: make WASM (and macOS) route through a
generalized shared map in `SkiaAccessibilityBase` instead of a per-platform hand-maintained
switch, then add the missing branches **once** in the base with abstract `Update*` hooks per
backend. Add the corresponding UIA property IDs to the Win32 map (most already exist in
`Win32UIAutomationInterop`).

**Producer-side parity note.** WinUI auto-diffs only IsEnabled/IsOffscreen/Name/ItemStatus
(`AutomationPeer.cpp:1777-1850`); other `AutomationProperties` changes reach UIA only when
app/control code calls `RaisePropertyChangedEvent` explicitly. The consumer branches above must
therefore handle app-raised events — Uno's DP callbacks for Name/HeadingLevel/IsDataValidForForm
already exceed WinUI here. Also missing from the Win32 map today: ScrollPercent (WASM handles
it, Win32 drops it), Selection, FullDescription, IsDialog, PositionInSet/SizeOfSet/Level,
LocalizedControlType. When extending the map from WinUI's `ConvertEnumToId` table, do **not**
copy its `APIsContentElement`↔`APIsControlElement` swap (`UIAWindow.cpp:2125-2128`) — an
upstream WinUI bug.

### XP-03 — Disabled-action exception parity  `MEDIUM`

WinUI action providers throw `UIA_E_ELEMENTNOTENABLED` (HRESULT `0x80040200`) when a pattern
method is invoked on a disabled element. Uno peers instead **silently no-op** or throw the
**wrong** exception type:

| Peer | Uno | WinUI |
|------|-----|-------|
| `ButtonAutomationPeer.cs:39-44` | silent no-op | `UIA_E_ELEMENTNOTENABLED` |
| `RepeatButtonAutomationPeer.cs:25-35` | `InvalidOperationException` | `UIA_E_ELEMENTNOTENABLED` |
| `ToggleButtonAutomationPeer.cs:47-52` (CheckBox) | silent no-op | `UIA_E_ELEMENTNOTENABLED` |
| `RadioButtonAutomationPeer.cs:42-50` | silent return | `UIA_E_ELEMENTNOTENABLED` |
| `ToggleSwitchAutomationPeer.cs:114-118` | silent no-op | `UIA_E_ELEMENTNOTENABLED` |

**Fix.** Standardize a disabled-guard that raises a well-known exception, and have the Win32
pattern wrappers translate it to HRESULT `0x80040200` so UIA clients see the correct error. On
WASM/macOS the guard prevents an incorrect activation (native controls are already disabled,
but the peer path should still not perform the action). Match WinUI's check ordering (enabled
check *before* the action).

### XP-04 — `RaiseNotificationEvent` is unwired  `HIGH`

> **Severity note.** Raised from MEDIUM to HIGH: built-in Uno controls already call
> `RaiseNotificationEvent` (e.g. `InfoBar`, `TeachingTip`, `CalendarView`, `TabView`). With the
> Skia path commented out, those announcements are dropped entirely — direct, user-perceivable
> AT breakage, not just a missing live-update.

**Investigated and resolved.** `AutomationPeer.RaiseNotificationEvent(...)` is commented out on
the Skia path and only warns (`AutomationPeer.cs:573-583`, DOTI TODO). The **enum value**
`AutomationEvents.Notification = 30` is correctly kept commented (`AutomationEvents.cs:164`)
because in WinUI it is decorated `[VelocityFeature("Feature_ExperimentalApi")]` and is not in
the stable API contract.

However, the **method** `RaiseNotificationEvent(kind, processing, displayString, activityId)`
is **stable public API** (since UWP 16299) and the full announce pipeline already exists on all
three backends:

- `IAutomationPeerListener.NotifyNotificationEvent` (`IAutomationPeerListener.cs:17`)
- `AccessibilityRouter` route (`AccessibilityRouter.cs:184-185`)
- `SkiaAccessibilityBase.NotifyNotificationEvent` → `AnnouncePolite`/`AnnounceAssertive`
  (`SkiaAccessibilityBase.cs:401-419`)
- Win32 provider path (`Win32Accessibility.cs:648-670`) →
  `UiaRaiseNotificationEvent` (`Win32Accessibility.cs:113-140`)

Uno's `ListenerExistsHelper` does **not** filter by event id (it only reports whether
accessibility is enabled), so the stable method can be wired to `NotifyNotificationEvent(...)`
**without** exposing/using the experimental enum.

**Fix.**
1. Uncomment/implement the Skia path of `RaiseNotificationEvent` to call
   `AutomationPeerListener?.NotifyNotificationEvent(...)` **directly**, mirroring the working
   `RaiseAutomationEvent` pattern (`AutomationPeer.cs:548-558`). Let the platform handlers gate
   on `IsAccessibilityEnabled`. Keep `AutomationEvents.Notification` commented out — and do
   **not** substitute a placeholder/nearby `AutomationEvents` value into a
   `ListenerExistsHelper(...)` call. If a listener check is wanted, add a notification-specific
   internal check that does not depend on the (absent) `AutomationEvents.Notification` enum.
2. **Win32 refinement:** raise `UiaRaiseNotificationEvent` from the **specific peer's** resolved
   provider and preserve the caller's actual `kind` / `processing` / `activityId`. The current
   root-provider fallback with hard-coded kind/activityId is only a fallback, not parity.

---

## 4. Win32 (UIA) — platform-specific findings

### W32-01 — `TextRange.GetChildren` returns non-COM providers  `HIGH`
`UiaTextRangeProviderWrapper.GetChildren()` copies managed
`Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple` objects directly, unlike
every other wrapper which translates peers to `Win32RawElementProvider`. UIA clients can
receive unusable/non-fragment providers.
- Uno: `Win32AccessibilityPatterns.cs:740-753`
- WinUI: wraps each child AP via `CreateProviderForAP` then `QueryInterface(IUnknown)` —
  `UIAPatternProviderWrapper.cpp:4825-4856`
- **Fix:** resolve each child's `AutomationPeer` through `_accessibility.GetProviderForPeer(peer)`.

### W32-02 — `TextEditTextChanged` + `LayoutInvalidated` events dropped  `HIGH`
`NotifyAutomationEvent` handles text/selection/menu/structure/focus/live-region but has no
cases for `TextEditTextChanged` or `LayoutInvalidated`; interop constants stop before those IDs.
- Uno: `Win32Accessibility.cs:556-637`, `Win32UIAutomationInterop.cs:231-249`
- WinUI: registers/listens/maps both — `UIAWindow.cpp:1360-1370,1629-1645,1829-1881,1992-1997`
- **Fix:** add constants/PInvoke for `UiaRaiseTextEditTextChangedEvent`, map `LayoutInvalidated`,
  handle both events.

### W32-03 — `ControllerFor` / `DescribedBy` hard-coded null  `MEDIUM`
Peers implement `GetControlledPeers()` / `GetDescribedBy()`
(`AutomationPeer.partial.mux.cs:58,630`), but Win32 `GetPropertyValue` returns null for both,
losing the UIA relationships.
- Uno: `Win32RawElementProvider.cs:261-263`
- WinUI: `APControlledPeersProperty`→`ControllerFor`, `APDescribedByProperty`→`DescribedBy`
  (`UIAWindow.cpp:2253-2254,2295-2296,2382-2388`)
- **Fix:** convert peer relation lists to provider arrays. (Pairs with WA-02/XP-02 relationship work.)

### W32-04 — Listener advise-count gating not implemented  `LOW`
`OnAdviseEventAdded/Removed` are no-ops and `ListenerExistsHelper` returns true whenever
accessibility is on, so controls raise/compute events without a matching UIA subscription
(more work than WinUI, but not incorrect output).
- Uno: `Win32Accessibility.cs:709-712`, `SkiaAccessibilityBase.cs:435-436`
- WinUI: per-event advise counters consulted before raising — `UIAWindow.cpp:1348-1375,1380-1515`
- **Fix (optional/perf):** track advise counts and property IDs; consult before raising. Lower
  priority — behavior is correct, just less efficient.
- **Interaction with XP-04 (important):** notification routing must stay **independent** of any
  `AutomationEvents`-keyed gating, because Uno has no public `AutomationEvents.Notification`
  value. Two acceptable designs: (a) keep notification routing entirely out of advise-count
  gating (safe fallback — always deliver when accessibility is enabled), or (b) for true WinUI
  parity, track the **raw Win32 UIA `Notification_Event` advise counter** internally (WinUI's
  `m_cAdviseNotificationEvent`) and expose a notification-specific listener check. Note that (a)
  is a *safe fallback*, not WinUI-equivalent — WinUI does gate on its notification advise count.
- **Implementation prerequisite:** the `AdviseEventAdded/Removed` methods on the provider are
  currently **unreachable dead code** — `IRawElementProviderAdviseEvents` is never declared as a
  COM interface on `Win32RawElementProvider` (`Win32RawElementProvider.cs:1466-1474`), so UIA
  cannot QI it. Declaring and implementing the interface comes before any advise counting.

### W32-05 — StructureChanged shape: coarse `ChildrenInvalidated` only  `HIGH`
Master coalesces every tree mutation into `StructureChangeType.ChildrenInvalidated` on the
nearest ancestor (`Win32Accessibility.cs:415-431`). WinUI raises per-child `ChildAdded` **on the
added child** (null runtime id) and `ChildRemoved` **on the parent** carrying the removed child's
runtime id, switching to `ChildrenBulkAdded`/`ChildrenBulkRemoved` past
`AP_BULK_CHILDREN_LIMIT = 20`, and to `ChildrenInvalidated` only when added+removed together
exceed the limit (`AutomationEventsHelper.cpp:95-173`).
- A WinUI-faithful implementation already exists on branch `dev/doti/win32-uia-childadded-events`
  (commits `3f6a6171a1`, `99b4addb0a`) — fold it into this remediation.
- Two WinUI details to verify when merging: Added-then-Removed (and Removed-then-Added) pairs for
  the same `(child, parent)` key **cancel out** within a batch
  (`AutomationEventsHelper.cpp:283-297`), and `Reordered` is never fired (`:195-198`).

### W32-06 — Remaining `GetPropertyValue` holes  `MEDIUM`
Beyond W32-03, `Win32RawElementProvider.GetPropertyValue` (`Win32RawElementProvider.cs:203-294`)
omits or nulls properties WinUI serves (`UIAWrapper.cpp:485-795`):

| Property | WinUI behavior / Uno fix |
|----------|--------------------------|
| `ClickablePoint` | peer `GetClickablePoint()`, client→screen, `VT_EMPTY` when `(0,0)` (`UIAWrapper.cpp:551-574`). FlaUI/Appium `GetClickablePoint` relies on it — currently absent entirely. |
| `FlowsTo` / `FlowsFrom` | peer-array variants (`UIAWrapper.cpp:682-687`); FEAP implements both and WASM already consumes them — pure wiring. |
| `Culture` | served from peer (`:756`); also fix shared `GetCultureHelper` hardcoded `return 0` (`AutomationPeer.h.mux.cs:60-69`) to read the Culture DP. |
| `AnnotationTypes` / `AnnotationObjects` | one `GetAnnotations` call serves both ids (`:707-712`); wire FEAP's orphaned `GetAnnotationsCoreImpl` to `GetAnnotationsCore` and raise the **dual** property-changed WinUI emits on annotation change (`UIAWindow.cpp:1753-1783`). |
| `IsPeripheral` | served (`:736`); Uno stubs null. |

### W32-07 — TextRange granularity + reserved values  `MEDIUM`
`TextRangeAdapter` treats `TextUnit.Line`/`Paragraph` as the whole document, returns a single
whole-element rect from `GetBoundingRectangles`, and returns `null` from
`GetAttributeValue`/`FindAttribute` (`TextRangeAdapter.cs:56-153`). WinUI returns
`UiaGetReservedNotSupportedValue` for unsupported text attributes
(`UIAPatternProviderWrapper.h:1790-1791`) — null is a protocol deviation. Degrades
Narrator/NVDA per-line/word navigation and caret rects in multiline `TextBox`.
- **Fix:** translate null → UIA reserved not-supported value in the Win32 wrapper; longer-term,
  back Line/Word/Character units and per-range rects with the Skia text layout.

### W32-08 — `WindowOpened`/`WindowClosed` dropped  `MEDIUM`
`TeachingTipAutomationPeer` raises both (`TeachingTipAutomationPeer.cs:82-104`), but
`Win32Accessibility.NotifyAutomationEvent` has no case for them (`Win32Accessibility.cs:556-637`),
so they never reach `UiaRaiseAutomationEvent`. WinUI maps both event ids (`UIAWindow.cpp:1934+`).
Two-line fix; audit other dialog-like surfaces (ContentDialog) for the same raise while there.

### W32-09 — Bridge polish  `LOW`
- **WM_GETOBJECT** answers only `UiaRootObjectId` (`Win32WindowWrapper.cs:241-246`); WinUI passes
  wParam/lParam through `UiaReturnRawElementProvider` unconditionally
  (`JupiterControl.cpp:612-639`), letting the UIA↔MSAA bridge serve legacy `OBJID_CLIENT` clients.
- **No hwnd map flush on destroy:** WinUI calls `UiaReturnRawElementProvider(hwnd, 0, 0, nullptr)`
  in `FlushUiaBridgeEventTable` (`UIAWindow.cpp:2794-2800`); Uno disconnects providers
  individually but never clears the hwnd association.
- **Focus raise gating:** WinUI `SetFocusHelper` raises only when listening + keyboard-focusable +
  not already focused (`AutomationPeer.cpp:1461-1491`); Uno proceeds unconditionally
  (`AutomationPeer.mux.cs:442-444` TODO) — duplicate focus announcements possible.
- **FrameworkId:** Uno returns `"Uno"`, WinUI `"XAML"` (`UIAWrapper.cpp:672-676`). NVDA/JAWS key
  framework-specific quirk handling off this string — keep `"Uno"` only as a conscious decision.

---

## 5. WebAssembly (ARIA/AOM) — platform-specific findings

Spec `003-wasm-a11y-remediation` is **largely implemented** (RadioButton, heading levels 7–9,
AutomationId separation as `xamlautomationid`, focusability plumbing, collapsed-subtree pruning,
static TextBlock non-interactive path, invalid/roledescription mapping). Remaining items below
are the spec FRs still marked Partial/Missing plus new findings.

### WA-01 — `aria-label` on `role=generic`  `HIGH`
`Custom → generic` is an ARIA **name-prohibited** role, yet author names are applied
generically and factory-wide.
- `AriaMapper.cs:57`, `SemanticElementFactory.cs:89-95`, `Accessibility.ts:510-512`
- **Fix:** suppress author names on name-prohibited roles, **or** promote a *named* generic
  container to `role=group` (which permits a name). (Ties to FR-020.)

### WA-02 — `aria-describedby` / `controls` / `flowto` can dangle  `HIGH`
Only `LabeledBy` checks target semantic existence; the peer relation collections do not, so
IDREFs can point at non-existent nodes.
- `SemanticElementFactory.cs:1269-1295`
- **Fix:** gate each related owner with `HasSemanticElement`; clear empty/stale attributes.
  (Cross-refs W32-03 relationship work and FR-022.)

### WA-03 — PasswordBox / `PlaceholderText` live-sync missing  `HIGH`  (FR-009)
PasswordBox never raises a Value automation event (`PasswordBox.cs:110-116`); TextBox raise is
gated to `TextBoxAutomationPeer` (`TextBox.cs:360-373`). Placeholder is set only at element
creation (`SemanticElementFactory.cs:423-427`) with no `PlaceholderText` branch in
`NotifyPropertyChangedEventCore`.
- **Fix:** raise Value for PasswordBox; add a `PlaceholderText` live branch (folds into XP-02).

### WA-04 — `ResolveLabel` flattens `LabeledBy` into `aria-label`  `MEDIUM`  (FR-019)
`aria-labelledby` is emitted when the labeller has a node (`SemanticElementFactory.cs:160-170,1248-1263`),
but `ResolveLabel` still also flattens `LabeledBy` into a literal `aria-label`
(`AriaMapper.cs:408-443`), producing double/wrong naming.
- **Fix:** when a valid `aria-labelledby` IDREF exists, do not also emit a flattened `aria-label`.

### WA-05 — Landmarks can be emitted unnamed  `MEDIUM`  (FR-014)
Roledescription is correctly gated on name, but `main`/`navigation`/`search` landmark roles may
still be emitted without an accessible name (`WebAssemblyAccessibility.cs:1791-1802`).
- **Fix:** gate landmark role emission (or supply a localized landmark name) consistently.

### WA-06 — Generic fallback + virtualized items under-expose state  `MEDIUM`  (FR-007/021/031)
The generic path applies only a subset of ARIA attributes — missing
required/invalid/description/selected/value/modal parity vs the factory path
(`WebAssemblyAccessibility.cs:1839-1939`). Virtualized items still hard-code `tabIndex=-1` with
no active item at creation (`SemanticElements.ts:1502-1547`).
- **Fix:** centralize a single full `AriaAttributes` application shared by generic + factory
  paths; complete virtualized-item ARIA (posinset/setsize/roving) at creation.

### WA-07 — Stale/duplicate JS interop declarations  `LOW`
`WebAssemblyAccessibility.NativeMethods` still declares old `create*` signatures without
`isFocusable`, drifting from TS/factory (`WebAssemblyAccessibility.cs:2553-2575` vs
`SemanticElementFactory.cs:1300-1328`, `SemanticElements.ts:419-430`).
- **Fix:** remove the unused stale imports (or update signatures).

### WA-08 — IsOffscreen/clipping never reflected  `MEDIUM`
`OnSizeOrOffsetChanged` deliberately avoids `peer.IsOffscreen()` because
`UIElement.GetGlobalBoundsWithOptions` is a stub returning an empty rect
(`WebAssemblyAccessibility.cs:718-723`); overlay visibility relies on `Visual.IsVisible` alone,
so ancestor-clipped content can stay exposed to AT.
- **Fix:** implement the global-bounds helper (shared win — it also feeds `IsOffscreenImpl` on
  all Skia backends, `AutomationPeer.mux.cs:295-349`), then honor offscreen state in the overlay.

### WA-09 — Minor ARIA + dead code  `LOW`
- `aria-current` and `aria-colspan`/`aria-rowspan` never emitted; `aria-valuetext` only for
  Slider, not generic RangeValue providers.
- RichTextBlock standalone text unsupported (documented FR-015 limitation,
  `WebAssemblyAccessibility.cs:1481-1482`).
- Unused C# debounce infra `QueueUpdate`/`FlushPendingUpdates`
  (`WebAssemblyAccessibility.cs:196-306`) is not driven by the live property path — wire it up
  or remove it.
- `OnScroll` carries a TODO to route via automation peers instead of per-scroller type checks
  (`WebAssemblyAccessibility.cs:958-959`).

---

## 6. macOS (NSAccessibility) — platform-specific findings

### MAC-01 — RadioButton `AXValue` goes stale  `HIGH`
Radio exposes SelectionItem (not Toggle). Initial state is converted to a native value
(`AriaMapper.cs:330-341`, `MacOSAccessibility.cs:589-591`), but selection change routing calls
only `UpdateSelected` (`SkiaAccessibilityBase.cs:325-328`, `MacOSAccessibility.cs:756-757`),
which updates only `unoIsSelected` (`UNOAccessibility.m:1234-1239`). But `accessibilityValue`
for a radio reads `unoValue` (`UNOAccessibility.m:264-275`), so VoiceOver reports the stale
checked state.
- **Fix:** on selection change for a radio, also update `AXValue` (0/1).

### MAC-02 — Scroll-position re-emission unwired  `HIGH`
The base requires the platform `OnChildAdded` path to call `TrySubscribeScrollSource`
(`SkiaAccessibilityBase.cs:137-143`) so scroll changes re-emit descendant positions
(`SkiaAccessibilityBase.cs:191-212`). macOS add/build paths never call it
(`MacOSAccessibility.cs:310-337,431-450`); the native frame update only mutates the cached frame
(`UNOAccessibility.m:1119-1123`).
- **Fix:** subscribe/unsubscribe scroll sources for added/removed elements (including
  Raw-pruned containers) and post a layout-changed notification after scroll.

### MAC-03 — Dynamic tree add/remove posts no VoiceOver notification  `MEDIUM`
Add/remove mutates the native tree (`MacOSAccessibility.cs:331,362`;
`UNOAccessibility.m:1011-1063,1065-1109`) but posts nothing. A structure hook exists
(`MacOSAccessibility.cs:832-833`) but isn't used for direct mutations.
- **Fix:** coalesce and post children-changed / layout-changed after add/remove.

### MAC-04 — ScrollBar range actions advertised only for Slider role  `MEDIUM`
ScrollBar is RangeBase with RangeValue and native has increment/decrement handlers
(`UNOAccessibility.m:562-575`), but action names add increment/decrement only for
`NSAccessibilitySliderRole` (`UNOAccessibility.m:589-592`), not `NSAccessibilityScrollBarRole`
(`UNOAccessibility.m:153`).
- **Fix:** expose increment/decrement for scrollbar / incrementor range roles too.

### MAC-05 — Several state updates don't post notifications  `LOW`
help/description/roleDescription/heading/password/range-bounds/required/readOnly/focusable
mutate fields without posting (`UNOAccessibility.m:1136-1140,1159-1178,1188-1231,1258-1269`), so
VoiceOver may serve cached values.
- **Fix:** post value/title/layout notifications where AppKit may cache.

### MAC-06 — AutomationId not exposed as `accessibilityIdentifier`  `MEDIUM`
No `accessibilityIdentifier` is set on `UNOAccessibilityElement`;
`AutomationProperties.AutomationId` never crosses the P/Invoke boundary. Blocks XCUITest/Appium
element lookup (the WASM overlay exposes `xamlautomationid` for exactly this purpose).
- **Fix:** add a native `unoIdentifier` property + `accessibilityIdentifier` override, push it
  from `ApplyAttributes` and on AutomationId change.

### MAC-07 — Computed attributes never reach AppKit  `MEDIUM`
`AutomationProperties.RoleOverride` affects only focusability (`SkiaAccessibilityBase.cs:489-506`),
not native role resolution. `AriaMapper.GetAriaAttributes` computes HasPopup, Invalid,
KeyShortcuts, AccessKey, MultiSelectable, ValueText, Controls, DescribedBy, LabelledBy
(`AriaMapper.cs:656-735`) — none have a native property or P/Invoke on macOS.
- **Fix:** wire RoleOverride into `ResolveRole`; extend `UNOAccessibilityElement` +
  `ApplyAttributes` for the attributes with NSAccessibility equivalents (invalid, valuetext →
  value description, haspopup → subrole/actions, multiselectable). Folds into the XP-02 base-map
  consolidation.

### MAC-08 — Interaction/text polish  `LOW`
- No `showMenu` action (peer `ShowContextMenu` support exists) and no NSAccessibility scroll
  actions (`UNOAccessibility.m:578-601`).
- `accessibilityDisclosureLevel` hardcoded to 0 (`UNOAccessibility.m:520-522`).
- Multiline text line rects estimated by even height division (`UNOAccessibility.m:785-794`) —
  approximate caret/range geometry.
- Text selection pushed only for `TextBox`, not PasswordBox/RichEditBox
  (`MacOSAccessibility.cs:794-802`).

---

## 7. Shared core (peers) — findings

### SH-01 — Missing non-generated peers  `LOW`
Not present as hand-authored peers in Uno: Hub, HubSection, SeekSlider, LandmarkTarget,
PopupRoot, Ink* peers, FaceplateContentPresenter, FullWindowMediaRoot, ListViewBaseItemData
(WinUI has them under `src/dxaml/xcp/dxaml/lib/*AutomationPeer_Partial.cpp`). Most are niche;
prioritize by control availability in Uno.
- **Fix:** add peers where the corresponding Uno control exists and is Skia-relevant.

### SH-02 — Base routing not EventsSource-aware  `MEDIUM`
The shared Skia path resolves owners via `TryGetPeerOwner`, not
`ResolveProviderPeer(resolveEventsSource:true)` (`SkiaAccessibilityBase.cs:276-366`,
`AccessibilityRouter.cs:96-109`, `AutomationPeer.ProviderTargets.cs:10-18`). ListItem/TabItem/
TreeItem event targeting can therefore diverge from WinUI on non-Win32 backends.
- **Fix:** resolve EventsSource before property/event provider routing on the shared path.

### SH-03 — `RaiseStructureChangedEvent` never reaches the listener  `MEDIUM`
The public API validates args, then stops ("TODO Uno", `AutomationPeer.partial.mux.cs:649-665`);
backend visual-tree hooks cover framework-driven mutations only. WinUI's managed
`RaiseStructureChangedEventImpl` raises for real (`AutomationPeer_Partial.cpp:791-857`) —
custom peers overriding `GetChildrenCore` (the documented WinUI pattern for composite and
virtualized controls) have no way to signal structure changes on Uno.
- **Fix:** route through `IAutomationPeerListener` into the same per-backend structure paths
  used by tree mutations. Pairs with W32-05; WASM can keep its no-op (DOM mutation is the
  structure signal there).

### SH-04 — Landmark elements not force-promoted into the tree  `MEDIUM`
WinUI forces any element setting `LandmarkType`/`LocalizedLandmarkType` into the UIA tree by
assigning `LandmarkTargetAutomationPeer` (`framework.cpp:374-391`) — the same mechanism as
Name/LabeledBy → `NamedContainerAutomationPeer`, which Uno already has
(`FrameworkElement.cs:954-976`). Uno lacks the landmark half and the peer type, so
`<Grid AutomationProperties.LandmarkType="Navigation">` is invisible on Win32/macOS (the WASM
overlay independently keeps landmark elements in `IsSemanticElement`).
- **Fix:** add `LandmarkTargetAutomationPeer` and extend the promotion check. Timing caveat:
  WinUI promotes at DP-set time, Uno checks at peer-creation — verify an element whose peer was
  already requested (and null-cached) before the DP was set still promotes afterwards.

---

## 8. Phased remediation roadmap

Ordered for maximum parity-per-change and to land shared fixes before platform consumers.

**Phase 1 — Shared foundations (unblocks the rest).**
- XP-01 mapper entries (MenuBar/Table/Separator/SplitButton haspopup).
- XP-04 `RaiseNotificationEvent` stable-method wiring (keep experimental enum out).
- XP-03 disabled-action guard + exception type.
- SH-02 EventsSource-aware routing.
- SH-03 `RaiseStructureChangedEvent` listener wiring.
- SH-04 landmark promotion (`LandmarkTargetAutomationPeer`).

**Phase 2 — Live property-change consolidation (XP-02).**
- Migrate WASM (then macOS) to route through the generalized base map (spec 003 FR-010).
- Add missing branches once in the base: ItemStatus, PosInSet/SizeOfSet, Orientation,
  IsRequiredForForm, FullDescription, LiveSetting, ControllerFor, Value-vs-text split.
- Extend the Win32 property map with the corresponding UIA IDs.

**Phase 3 — Win32 correctness.**
- W32-05 merge the `dev/doti/win32-uia-childadded-events` StructureChanged work first (verify
  WinUI cancellation semantics).
- W32-01 TextRange child providers, W32-02 TextEdit/LayoutInvalidated events,
  W32-03 ControllerFor/DescribedBy, W32-06 remaining property holes (ClickablePoint first —
  unblocks FlaUI/Appium), W32-07 TextRange reserved values, W32-08 WindowOpened/Closed,
  XP-01 TitleBar control type, XP-04 per-peer notification refinement.
  (W32-04 advise-count gating + W32-09 bridge polish optional/last.)

**Phase 4 — WASM correctness.**
- WA-01 name-prohibited generic, WA-02 dangling IDREFs, WA-03 PasswordBox/placeholder,
  WA-04 LabeledBy flattening, WA-05 unnamed landmarks, WA-06 generic/virtualized parity,
  WA-08 global-bounds/offscreen, WA-07 stale interop cleanup, WA-09 minor ARIA + dead code.

**Phase 5 — macOS correctness.**
- MAC-01 radio AXValue, MAC-02 scroll re-emission, MAC-03 tree-change notifications,
  MAC-04 scrollbar actions, MAC-06 accessibilityIdentifier, MAC-07 RoleOverride + computed
  attributes, MAC-05 state notifications, MAC-08 interaction/text polish, plus the macOS
  control-type→native-role path from XP-01.

**Phase 6 — Peer surface + polish.**
- SH-01 missing peers (as needed), remaining LOW items.

---

## 9. Validation & test strategy

- **Runtime tests** live in `Uno.UI.RuntimeTests`; WASM already has DOM-assertion tests
  (`Given_AccessibleTabindex.cs`, `Given_AccessibleTextBox.cs`, `Given_AccessibleAria.cs`).
  Each fix above should ship with a fails-before/passes-after test. Several spec-003 FRs are
  marked "no runtime evidence" — those tests exist but need to be **run** and gated in CI.
- **Per-platform assertion targets:**
  - WASM: assert emitted ARIA attributes / roles / IDREF validity in the DOM.
  - Win32: assert UIA provider/property/event via a UIA client harness (control type, relation
    props, raised events).
  - macOS: assert NSAccessibility role/value/actions/notifications.
- **WinUI parity checks:** use the `/winui-runtime-tests` skill to validate the same scenarios
  against native WinUI where a Windows reference is available.
- **Regression guard:** native (non-Skia) targets are maintenance-only — keep them compiling and
  unchanged in behavior. All new routing lives on the Skia/shared path.
- Follow repo conventions: Conventional Commits, one focused commit per coherent chunk, each
  building clean; every PR references an issue and fills `.github/PULL_REQUEST_TEMPLATE.md`.

---

## 10. Appendix — Parity confirmed (no action needed)

- Pattern-provider delegation: Invoke, Toggle, Value, RangeValue, ExpandCollapse, Selection,
  Scroll, Grid/Table, Transform, Window wrappers delegate real peer providers (Win32).
- `RaiseAutomaticPropertyChanges` matches WinUI's tracked auto-properties and old/new null
  semantics (`AutomationPeer.mux.cs:517-598`).
- RadioButton suppresses inherited Toggle, exposes only SelectionItem; `Select()` activation
  matches `RadioButtonAutomationPeer_Partial.cpp`.
- SelectorItem `IsSelectionPatternApplicable` and RangeBase bounds checks match WinUI.
- Bounding-rect logical→screen conversion with DPI + clipping; provider cleanup via
  `UiaDisconnectProvider`; focus lookup resolves peer event sources (Win32).
- Heading levels 7–9 handled WCAG-valid (`<h6>` clamp + true `aria-level`); AutomationId
  surfaced as `xamlautomationid` (not the accessible name); static TextBlock path
  non-interactive; Slider value/min/max/orientation correct (WASM).
- Per-window native context/lifecycle/teardown; frame conversion; focus + announcements on
  standard notifications; Raw pruning in initial + dynamic traversal (macOS).
- Landmark initial mapping + roledescription-from-authored-localized-type; virtualized item
  container resolution with parent fallback (shared).
- `RaiseAutomationEvent` ListenerExists gating mirrors `CAutomationPeer::RaiseAutomationEvent`;
  EventsSource generation restricted to ListItem/TabItem/TreeItem with the same re-entrancy
  guard as WinUI (`AutomationPeer.mux.cs:21-90`).
- Structure bulk threshold 20 equals WinUI `AP_BULK_CHILDREN_LIMIT`; `GetEmbeddedFragmentRoots`
  returning null matches WinUI (also null); `RaiseAutomaticPropertyChanges` diffs exactly WinUI's
  four tracked properties.
- No `LegacyIAccessible` pattern = parity — WinUI providers don't expose it either (UIA core
  bridges MSAA); Win32 wires 33 pattern wrappers, matching WinUI's full wrapper set.
- Name/LabeledBy → `NamedContainerAutomationPeer` promotion present
  (`FrameworkElement.cs:954-976`), matching `framework.cpp:400-418`.

---

*Raw per-audit reports archived in session `files/agent-win32-uia.md`, `agent-wasm-aria.md`,
`agent-macos-ax.md`, `agent-shared-core.md`.*
