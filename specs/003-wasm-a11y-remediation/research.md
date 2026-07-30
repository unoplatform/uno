# Research: WASM Accessibility Remediation (Semantic Elements, Live-Sync, tabindex)

**Date**: 2026-06-06
**Branch**: `003-wasm-a11y-remediation`
**Depends on**: `001-wasm-accessibility` (AriaMapper, SemanticElementFactory, semantic DOM), `002-wasm-a11y-advanced` (roving tabindex, FocusSynchronizer, virtualization)

> **Evidence level: code review by inspection, adversarially verified.** Every claim
> below was traced in the live source and independently re-verified by a second
> reviewer instructed to refute it. **None of this is runtime-validated** — the
> existing runtime tests for these mappings are almost entirely `[Ignore]`d (see
> §4), so the headline conclusion is *"correct by inspection, untested at runtime."*

This document consolidates two audits:
1. **Mapping verification** — does each XAML control → HTML/ARIA mapping actually
   produce the right element, attributes, live state-sync, and DOM→Uno event routing?
2. **tabindex audit** — which semantic elements become keyboard tab stops, and is
   that correct per the WAI-ARIA Authoring Practices (APG)?

---

## 1. Scope context: two rendering targets, do not conflate

The accessibility code spans two **independent** code paths:

- **Skia AOM** (`Uno.UI.Runtime.Skia.WebAssembly.Browser`): a parallel semantic DOM
  overlay (`#uno-semantics-root`, `pointer-events:none`) over the Skia canvas. This is
  the **in-scope, actively-developed** target. All `Create*Element` / `AriaMapper`
  logic lives here.
- **Native WASM DOM** (`Uno.UI` `__WASM__`): the legacy target where each `UIElement`
  is a real DOM element. **Maintenance-only.** Relevant only because it is the source
  of the most visible (but lowest-severity) tabindex symptom (§3.4).

## 2. Mapping verdicts (verification audit)

| Control → expected | Verdict | Core problem |
|---|---|---|
| **Button → `<button>`** | 🟢 correct | Full chain works (create, `aria-label`, disabled, click→`IInvokeProvider.Invoke`). Mouse click is dead via `pointer-events:none` — relies on keyboard/AT (correct for a native `<button>`). |
| **TextBox/PasswordBox → `<input>`/`<textarea>`** | 🟡 partial | TextBox solid. **PasswordBox programmatic value never live-syncs** (set-once). `aria-multiline`, placeholder, initial-disabled set-once. |
| **CheckBox → `<input type=checkbox>`** | 🔴 broken (RadioButton half) | CheckBox correct. **RadioButton broken on three independent axes** (§2.1). |
| **TextBlock(Heading) → `<h1>`–`<h6>`** | 🟡 partial | Correct for levels 1–6 at creation. **Levels 7–9 silently clamped**; **no live `HeadingLevel` sync**; `textContent` stale on name change; **also a spurious tab stop** (§3.1). |
| **TextBlock(Body) → `<p>`/`<span>`** | 🔴 gap | **Pruned entirely** — produces *no* DOM node (not even a `<div>`). Also affects `RichTextBlock`/`RichTextBlockOverflow`. |
| **ScrollViewer → `role=region`** | 🟡 partial | `role=region` emitted **unconditionally** (every ScrollViewer → rotor noise) and **unlabeled or labeled with a descendant-text dump** (axe/WCAG "region must have a name" violation). |

Corrected headline: **not "4 of 6 done" — at best ~2.5 of 6 are solid; one is broken;
the rest partial/gap; and the whole subsystem is runtime-unverified.**

### 2.1 RadioButton — three fatal defects

`RadioButton` dispatches to `<input type="radio">` correctly, but:

1. **Initial `checked` is always `false`.** `AriaMapper.GetAriaAttributes` populates
   `Checked` only from the *Toggle* pattern ([AriaMapper.cs:239](../../src/Uno.UI/Accessibility/AriaMapper.cs)),
   but `RadioButtonAutomationPeer.GetPatternCore` exposes **only `SelectionItem`**
   ([RadioButtonAutomationPeer.cs:21-33](../../src/Uno.UI/UI/Xaml/Automation/Peers/RadioButtonAutomationPeer.cs)).
   So `isChecked = attributes.Checked == "true"` is always false
   ([SemanticElementFactory.cs:222](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs)).
   A `RadioButton{IsChecked=true}` renders **unchecked**.
2. **DOM click is a no-op.** `change → onToggle → OnToggle` queries
   `PatternInterface.Toggle`, `null` for radio → `Toggle()` never runs and `Select()`
   is never called either (listener uses `onToggle`, not `onSelection`)
   ([WebAssemblyAccessibility.cs:782-797](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs),
   [SemanticElements.ts:430-434](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)).
   **A screen-reader user cannot select a radio.**
3. **External state change writes the wrong attribute.** `RadioButton` is excluded from
   the `ToggleState` raise ([ToggleButton.mux.cs:37](../../src/Uno.UI/UI/Xaml/Controls/Primitives/ToggleButton.mux.cs))
   and instead raises `IsSelectedProperty` → `UpdateSelectionState` → sets
   **`aria-selected`** (invalid on `role=radio`) instead of native `checked`
   ([WebAssemblyAccessibility.cs:1550-1558](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).

### 2.2 PasswordBox live-value desync

`PasswordBox.OnPasswordChanged` raises no `ValueProperty` automation event
([PasswordBox.cs:110-123](../../src/Uno.UI/UI/Xaml/Controls/PasswordBox/PasswordBox.cs)),
and `TextBox.OnTextChanged`'s raise is gated on `peer is TextBoxAutomationPeer`
([TextBox.cs:361](../../src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.cs)) — false, because
`PasswordBoxAutomationPeer : FrameworkElementAutomationPeer`. Programmatic password
changes never reach the DOM input. DOM→Uno still works via `OnTextInput → SetValue`.

### 2.3 Body TextBlock pruned

`IsSemanticElement` prunes plain `TextBlock`/`RichTextBlock`/`RichTextBlockOverflow`
unless `Name`/`HeadingLevel`/`Landmark`/`LiveSetting` is set
([WebAssemblyAccessibility.cs:1032-1052](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).
The suspected `<div>`-with-null-role path is **dead code** for standalone text — text
reaches AT only when a parent control absorbs it via `AriaMapper.ResolveLabel`. This is
partly *by design* (DOM-bloat / nested-focusable mitigation, documented at
WebAssemblyAccessibility.cs:1024-1031) — so the remediation decision is a **design
choice**, see §5 Open Decisions.

## 3. tabindex audit

### Root cause: the factory bypasses the focusability gate

Two element-creation paths exist; only one consults real focusability:

- **Generic path** correctly gates: `IsAccessibilityFocusable(child, child.IsFocusable)`
  ([SkiaAccessibilityBase.cs:473-500](../../src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs),
  where `IsFocusable = IsVisible && IsEnabled && (IsTabStop || …)`) →
  `updateElementFocusability` → `if (isFocusable) tabIndex=0; else removeAttribute`
  ([Accessibility.ts:217-229](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts)).
- **Type-specific factory path** — tried **first** when a peer exists (the common case,
  [WebAssemblyAccessibility.cs:1219-1247](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).
  **No `Create*Element` JSImport accepts `isFocusable`**; each `create*Element` assigns
  `tabIndex` as a **hardcoded literal**, decoupled from `IsTabStop`/`IsEnabled`.

So for nearly every real control, the tab-stop decision is a TypeScript constant.

### 3.1 High-severity tab-order defects (Skia AOM)

- **Heading `<h1>`–`<h6>` is a tab stop** — `element.tabIndex = 0` hardcoded
  ([SemanticElements.ts:461](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)).
  Headings are non-interactive; rotor/H-key navigation does not require a tab stop.
  WCAG 2.4.3 regression. **Permanent**: TextBlock isn't a `Control`, so the dynamic
  focusability fix (`UpdateIsFocusable`) can never correct it.
- **RadioButton — every radio born `tabIndex=0`** ([SemanticElements.ts:414](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)).
  APG: exactly one radio per group is tabbable. (Compounds §2.1.)
- **Roving tab stop goes stale on arrow nav** — `UpdateRovingTabindex` runs only on
  selection/toggle, never on focus movement; `FocusSynchronizer` never calls it
  ([FocusSynchronizer.cs:115-224](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/FocusSynchronizer.cs),
  driver sites [WebAssemblyAccessibility.cs:1482-1485,1562-1565](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).

### 3.2 Medium-severity (Skia AOM)

- **Composite containers** `listbox`/`tablist`/`tree`/`menu`/`grid` hardcoded
  `tabIndex=0` ([SemanticElements.ts:640,892,965,1226,1113](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)).
  tablist/tree/menu containers should not be tab stops (the active item is); listbox/
  combobox can present **two** stops (container `0` + roving item `0`). Unconditional —
  fires on empty/decorative/disabled instances. Inconsistent with the virtualized
  container which sets **no** tabindex ([SemanticElements.ts:1371-1383](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts)).
- **Disabled div-based composites stay tabbable** — `updateDisabledState` sets only
  `aria-disabled`, never `tabIndex`; div/table roles have no native `disabled`; and the
  fix only fires on a focusability *change*, not at creation.

### 3.3 What is correct (do not regress)

Interactive controls — `button`, `checkbox`, `slider`, `textbox`/`textarea`/`password`,
`combobox`, `link` — `tabIndex=0` is right. Composite *items* — `option`, `tab`
(`selected?0:-1`), `treeitem`, `gridcell`, `columnheader`, `menuitem`, virtualized items
— `tabIndex=-1` is right. Grid `row` (no tabindex), generic path, root (`role=application`),
`semanticsRoot`, `applyCommonStyles` (sets no tabindex), and the enable-accessibility
button (`tabIndex=0`) are all correct.

### 3.4 Native WASM DOM (maintenance-only, low priority)

Every `UIElement` is born with a `tabindex` attribute — `tabindex="-1"` for
non-focusable ([WindowManager.ts:295-299](../../src/Uno.UI/ts/WindowManager.ts), C# passes
`isFocusable:false` at [UIElement.wasm.cs:68-74](../../src/Uno.UI/UI/Xaml/UIElement.wasm.cs)).
This is **almost certainly the symptom most visible in DevTools.** `-1` is *not* a tab
stop, so it is DOM over-application/noise, not a focus-order bug. The follow-up
`SetIsFocusable` path *is* correctly gated ([WindowManager.ts:1605-1608](../../src/Uno.UI/ts/WindowManager.ts)).
The `if (element.hasOwnProperty("tabindex"))` guard at line 295 is **dead code** (wrong
casing). Optional cleanup only; do not change behavior on this target.

## 4. Test coverage reality

DOM-level / interaction assertions are essentially absent:

- `Given_AccessibleButton` 9/9 `[Ignore]`d · `Given_AccessibleCheckBox` 11/11 `[Ignore]`d
  · `Given_AccessibleComboBox` 7/7 · `Given_AccessibleSlider` 10/10 ·
  `Given_AccessibleListView` 5/5 (`[Ignore]`d).
- `Given_AccessibleTextBox` — 11 methods, **only 2 active** (both caret/typing-order;
  neither asserts element type).
- **Zero TypeScript tests** for `SemanticElements.ts` / `Accessibility.ts`.
- The active tests assert the C# `AriaMapper` in isolation — none assert the produced
  HTML element, ARIA values, or DOM→Uno routing.
- The whole AOM is **inert until the sr-only "Enable accessibility" button is activated**
  (`AutoEnableAccessibility` defaults false; no SR auto-detection,
  [FeatureConfiguration.cs:80](../../src/Uno.UI/FeatureConfiguration.cs)). Only the 2
  active TextBox tests exercise it (via `EnableAccessibilityThroughDom`).

## 5. Cross-cutting structural findings

- **Attribute-update-on-change is a hand-maintained switch.** `NotifyPropertyChangedEventCore`
  ([WebAssemblyAccessibility.cs:1459-1705](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs))
  doesn't call base; any property without an explicit branch is **creation-only**. Root
  cause of heading-level, placeholder, password, scrollability, `aria-required`,
  `aria-keyshortcuts`, `aria-multiselectable` staleness. The base `Update*` overrides are
  dead on WASM.
- **Factory vs. generic-path attribute divergence.** Anything falling to
  `CreateGenericElement` (Image, Group, Pane/ScrollViewer) applies a strict subset of
  attributes (no `aria-disabled`, `aria-description`, `aria-required`, `aria-posinset/setsize`,
  `aria-labelledby`…) and labels via `GetName()` instead of `ResolveLabel`.
- **Debounce is dead code.** The 100ms debounce queue ([WebAssemblyAccessibility.cs:138-249](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs))
  is built but never used; updates fire synchronously (mismatch vs. 001 FR-031).
- **Virtualization bypasses the factory** — ListView/ItemsRepeater items hardcode roles
  and skip factory attribute application.
- **Redundant dispatch** — `GetSemanticElementType` computed at WebAssemblyAccessibility.cs:1221
  for logging then recomputed in the factory (twice per element on a hot path).

## 6. Open decisions (resolve in spec/plan)

| Decision | Options | Recommendation |
|---|---|---|
| **Body text exposure** | (a) leave pruned (text via parent label, by design); (b) emit `<p>`/`<span>` for standalone text-boundary nav | **(a) keep pruning** for controls' inner text, **(b) additionally** emit a text element only for *standalone* TextBlocks not absorbed by a parent — gated to avoid DOM bloat. Needs product call. |
| **ScrollViewer region** | (a) drop `role=region` unless scrollable + named; (b) always region | **(a)**: gate `role=region` on actual scrollability **and** a real name; otherwise no landmark. |
| **Composite tabindex model** | (a) container-`0` + `aria-activedescendant`; (b) roving item-`0` + container-`-1` | **(b) roving** for tablist/tree/menu/grid (matches existing item roving + DOM `.focus()`); listbox per WinUI behavior. Apply **one** model consistently. |
| **Focusability gate in factory** | thread `isFocusable` through every `Create*Element` vs. post-create `UpdateIsFocusable` correction | **Thread it through** — single rule on all paths; mirrors the generic path. |

## 7. Summary of decisions

| Topic | Decision | Rationale |
|---|---|---|
| Overall approach | Remediate existing 001/002 code, not greenfield | Code exists; defects are localized |
| RadioButton | Treat as a correctness bug, fix first | Broken on 3 axes; highest user impact |
| tabindex | Gate on real focusability across **all** paths; headings/non-interactive get none | Root cause is the factory's hardcoded literals |
| Live-sync | Add missing `NotifyPropertyChangedEventCore` branches; consider a generalized property→attribute map | Hand-maintained switch is the systemic gap |
| Tests | Add DOM-level runtime tests (enable AOM, assert element/attributes/routing) + re-enable the `[Ignore]`d suite | Current coverage is near-zero and inactive |
| Native WASM DOM | Out of scope except optional `tabindex`-noise cleanup | Maintenance-only target |
| ARIA attribute correctness | Fix wrong-target mappings, close factory/generic divergence, map the unmapped, add IDREF integrity + tests | See §8 |

---

## 8. ARIA attribute mapping audit

> Third audit (adversarially verified; two overclaims corrected — noted at the end). Covers
> every `AutomationProperties`/peer property → ARIA attribute. **No runtime test asserts any
> emitted ARIA output today.** Answer to "are they all mapped correctly?": **No.**

### 8.1 Wrong-target — mapped to the wrong attribute (P1)

- **`AutomationId` → `aria-label`** (generic path). `automationId = AutomationProperties.GetAutomationId(child)` is read **first**, falling back to the real name only when empty, then set as `aria-label`
  ([WebAssemblyAccessibility.cs:1279-1295](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs),
  [Accessibility.ts:432-434](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts)).
  A test id (`"SubmitButton_42"`) becomes the announced name. AutomationId must be a DOM
  id/`data-*` (`xamlautomationid`), never the name. Factory path correctly ignores it → the
  two paths diverge.
- **`LabeledBy` never emitted as `aria-labelledby` — dead mapping.** `AriaAttributes.LabelledBy`
  ([AriaMapper.cs:566](../../src/Uno.UI/Accessibility/AriaMapper.cs)) is declared and *read* by
  two appliers but **never assigned** (grep: 0 assignments). The relationship is flattened
  into a static `aria-label` via `ResolveLabel`; both appliers are unreachable dead code.
- **`FindHtmlRole` emits invalid non-ARIA role tokens** set verbatim as `role`: `image`
  (→`img`), `edit` (→`textbox`), plus `pane`/`window`/`custom`/`datagrid`/`spinner`/
  `statusbar`/`titlebar`/`semanticzoom`/`splitbutton`/`tabitem`/`headeritem`/`calendar`/
  `thumb` ([WebAssemblyAccessibility.cs:1251-1254](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs),
  [AutomationProperties.uno.cs:56-212](../../src/Uno.UI/UI/Xaml/Automation/AutomationProperties.uno.cs)).
  Hits **both** the Skia generic path and the native WASM-DOM path; `ToggleSwitch→checkbox`
  there even disagrees with `AriaMapper`'s `switch`.
- **`AccessKey` conflated into `aria-keyshortcuts`** (it's a mnemonic; HTML `accesskey` is
  closer); space-joined with `AcceleratorKey` ([AriaMapper.cs:223-232](../../src/Uno.UI/Accessibility/AriaMapper.cs)).

### 8.2 Systemic factory-vs-generic divergence (P1)

The generic `AddSemanticElement` path never calls `GetAriaAttributes`, so any element that
falls to it (Image, Group, ProgressBar, named containers, unsupported control types) loses
at creation: `aria-describedby`, `aria-controls`, `aria-flowto`, `aria-required`,
`aria-description`, `aria-posinset/setsize`, `aria-selected`, `aria-valuenow`,
`aria-modal`/`role=dialog`. Factory path applies them
([SemanticElementFactory.cs:86-176](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs)).

### 8.3 Dangling IDREFs (P1)

`aria-controls`/`flowto`/`describedby`/`activedescendant` emit `uno-semantics-{handle}` for
related peers with **no check the target exists in the AOM**
([SemanticElementFactory.cs:970-997](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs)).
Structural/pruned peers (Grid/Border/TextBlock) have non-zero handles but no semantic node →
the reference dangles. No clearing on collection-clear/deselect either.

### 8.4 Wholly unmapped

`aria-invalid` (`IsDataValidForForm` never read) · `aria-orientation` (Slider uses a
non-standard `orient`/CSS hack; ScrollBar nothing) · `aria-roledescription` from
**`LocalizedControlType`** (only Custom landmark wired) · `aria-level` from
**`AutomationProperties.Level`** (only `HeadingLevel` feeds it) · `aria-busy` (`ItemStatus`)
· `lang` (`Culture`) · `aria-owns` · `aria-current` · `aria-details`. (`FlowsFrom`,
`IsPeripheral` correctly unmapped — no ARIA equivalent.)

### 8.5 Creation-only (no live-sync) & value-semantics

- Creation-only / dead live-sync: `aria-level`(HeadingLevel), `aria-required`,
  `aria-keyshortcuts`, `aria-multiselectable`, `aria-haspopup` (hardcoded in TS; C# value
  dead), `aria-valuetext`, `FullDescription`. Root cause as in §5 — the WASM
  `NotifyPropertyChangedEventCore` override never chains to base, so the `HeadingLevel`/
  `LandmarkType`/`LabeledBy`/`ControlledPeers`/`FlowsTo`/`DescribedBy` branches are dead.
- Value-semantics: `posinset` injected as `"N of M"` *text into `aria-label`* for
  non-supporting roles; `aria-atomic` hardcoded `true`; heading levels 7–9 clamped (overlaps
  the heading defect in §2/§3).

### 8.6 Corrected overclaims (kept honest)

- Heading live-sync: the *base* `SkiaAccessibilityBase` **does** have a `HeadingLevel` branch
  ([:319-323](../../src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs)); it's dead
  on WASM only because the override doesn't call base. ("No WASM live-sync" still holds.)
- `aria-modal`: a *real opened* `ContentDialog` **does** get `role=dialog`+`aria-modal` at
  runtime via `FocusTrap.ts`; only the creation-time application and `IsDialog` live-sync are
  missing.

### 8.7 What 003 already covered vs. net-new

Already in plan (creation-time): `aria-label` resolution, `aria-required` (creation),
posinset/setsize gating, `aria-keyshortcuts`, live/landmark roles, slider value + pattern
state. **Net-new (FR-018…030):** the `AutomationId`/`LabeledBy`/role-token fixes, factory↔
generic parity, dangling-IDREF integrity, `aria-invalid`/`aria-orientation`/
`LocalizedControlType`/standalone-`Level`, the missing live-sync branches, value-semantics
corrections, and ARIA-output tests.

### 8.8 Custom / landmark mapping (focused trace, adversarially verified — verdict: partial)

The role mapping itself is **correct**, but it is surrounded by gaps:

- **Role: correct.** `GetLandmarkRole` ([AriaMapper.cs:421-432](../../src/Uno.UI/Accessibility/AriaMapper.cs))
  maps `Main/Navigation/Search/Form/Custom → main/navigation/search/form/region`;
  `Custom→region` is the correct WAI-ARIA mapping. No value→null gap (`None` excluded by
  callers). Landmark controls take the **generic path** (Custom/Pane/Group → `Generic`), and
  the generic path *does* apply both the landmark role (overwriting any computed role,
  [WebAssemblyAccessibility.cs:1269-1277](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs))
  and (Custom-only) `aria-roledescription` ([:1336-1343](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).
  The factory-path appliers ([SemanticElementFactory.cs:91-101](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs))
  are dead for landmarks (gated on `created==true`, which is false for Generic).
- **`LocalizedLandmarkType` honored for Custom only** ([AriaMapper.cs:195](../../src/Uno.UI/Accessibility/AriaMapper.cs),
  [WebAssemblyAccessibility.cs:1336](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)).
  On `Main`/`Navigation`/`Search`/`Form` it is silently dropped — parity gap vs WinUI/UIA
  (the Win32 backend returns it unconditionally, [Win32RawElementProvider.cs:245](../../src/Uno.UI.Runtime.Skia.Win32/Accessibility/Win32RawElementProvider.cs)). → **FR-025**.
- **Live-sync dead.** `LandmarkTypeProperty`/`LocalizedLandmarkTypeProperty` register no
  changed-callback ([AutomationProperties.cs:218-223,258-263](../../src/Uno.UI/UI/Xaml/Automation/AutomationProperties.cs))
  and are never raised, so the live-sync branches ([WebAssemblyAccessibility.cs:1519-1528](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs),
  [SkiaAccessibilityBase.cs:330-335](../../src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs))
  are unreachable across all Skia heads. (`HelpText` is dead for the same reason.) → **FR-027**.
- **Unlabeled region / roledescription-without-name.** A Custom landmark (or any
  Pane/ScrollViewer) with no name → `role="region"` with no `aria-label` → axe "region must
  have a name". Worse: with `LocalizedLandmarkType` but no name it emits `role=region` +
  `aria-roledescription` + no `aria-label` — an *actively non-conforming* use of
  `aria-roledescription` (ARIA: roledescription requires an accessible name). No name
  fallback, no diagnostic. → **FR-014**.
- **Name asymmetry**: generic path uses `GetAutomationId()??GetName()` only, bypassing
  `ResolveLabel`'s Content/child-TextBlock fallbacks → content-only landmark containers can
  be unlabeled where a typed control wouldn't. → **FR-018/021**.
- **Zero tests** (only default-value peer assertions in `Given_AutomationPeer.cs:355-368`).
- *Corrected (non-load-bearing):* the invalid `FindHtmlRole` tokens `"pane"`/`"custom"` do
  **not** reach the Skia generic path (valid `region`/`generic` from `AriaMapper` wins
  first); they're only reachable on the native-WASM DOM path. Auto-raised properties also
  include `ItemStatus`/`IsOffscreen` (doesn't change the dead-branch conclusion).

## 9. Tree-walk completeness — NavigationView destinations unreachable to AT (runtime-confirmed)

> **Evidence level: runtime-observed (A11y Inspector over CDP) + code review.** Found while
> testing the implementation: a `NavigationView` (`PaneDisplayMode="Left"`, pane pinned open)
> exposes, in the semantic DOM, only the *selected* item plus a stray "More" button — the
> other destinations are absent. A separate `findings-navigationview-overflow.md` first
> reported this with a **suspected** width-measurement cause; that cause is **refuted** below.

### 9.1 Refuted hypothesis (width / overflow split)

The doc's suspected "under-sized `availableSize.Width` triggers the top-nav overflow split in
Left mode" is **refuted in code and at runtime**. `IsTopNavigationView()` is exactly
`PaneDisplayMode == Top` ([NavigationView.cs:4285](../../src/Uno.UI/UI/Xaml/Controls/NavigationView/NavigationView.cs));
in `Left` it is false, and *every* entry into the overflow machinery
(`MeasureOverride`→`HandleTopNavigationMeasureOverride`→`ShrinkTopNavigationSize`/
`MoveItemsOutOfPrimaryList`, :1505-1523/:3852-3917/:4088-4119) is gated on it — so the split
**cannot run** in Left mode regardless of width. Runtime corroboration: with the inspector's
"Relevant only" filter **off**, the destinations are present at **L1 (XAML)** but emit
**no L2 semantic node** (flagged `⚠ no semantic representation`) — they are not parked in an open
overflow flyout, confirming this is an emission gap, not a width/overflow effect.

### 9.2 Root cause A (primary, in 003 scope) — virtualized items not registered at AOM build

Left-nav items are hosted in an **`ItemsRepeater`** (`MenuItemsHost`). Both semantic-tree walks
skip `ItemsRepeater` ([WebAssemblyAccessibility.cs:342](../../src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs)
and :1176-1179); repeater items are meant to be surfaced via a `VirtualizedSemanticRegion`
subscribed to `ItemsRepeater.ElementPrepared` in `TryRegisterVirtualizedContainer` (:408-434).
But that is invoked **only from `OnChildAdded`** (:308), gated by `!_isCreatingAOM` (:291). The
initial `CreateAOM`→`BuildSemanticsTreeRecursive` runs with `_isCreatingAOM=true` (:713-720) and
**never registers the region**, so any item already realized when accessibility is enabled emits
no node and receives no later `ElementPrepared` (it was prepared before any subscription
existed). → the destinations emit no L2 semantic node and are AT-invisible (the repeater *host*
emits a `group` div, but its realized children do not). The shell **landmarks DO emit** correctly
(`region`/`navigation`/`main` per the inspector) — so the gap is specifically the repeater-hosted
items and their inner `ImplicitTextBlock` labels (the latter also FR-015). **Generalizes** beyond
NavigationView to *any* `ItemsRepeater`/`ListViewBase` already populated at AOM-enable time — the
same family as the virtualized-parity gap (§5/§8.2, FR-021/T055).

### 9.3 Root cause B (secondary, in 003 scope) — Collapsed/hidden subtree exposed to AT

Runtime-confirmed by the inspector: the "More" node is `x:Name="TopNavOverflowButton"`, whose
host `TopNavGrid` is `Collapsed` in non-Top mode (`TopPaneVisibility=Collapsed`,
NavigationView.cs:4716-4730) — yet it is `rendered? hidden` **but** `in AT tree? exposed`
(`ignored=False`). So the a11y walker / `IsSemanticElement` does **not** prune
`Visibility=Collapsed` (or otherwise-hidden) subtrees, exposing a phantom control to AT.

### 9.4 Runtime corroboration of existing FRs (from the same inspector run)

- **FR-018** confirmed: inspector finding `AutomationId not reflected … expected
  xamlautomationid="NavLandmark", observed (absent)` — AutomationId is not surfaced as a DOM id.
- **FR-015** confirmed: findings `ImplicitTextBlock "Home"/"Reports"/"Go" has no accessible
  representation` — standalone/inner text not exposed.

### 9.5 Folded requirements

- **FR-031** (root cause A): register virtualized containers + backfill already-realized items at
  AOM-build time, not only via the live `OnChildAdded` path.
- **FR-032** (root cause B): the semantic-tree walker MUST NOT expose `Visibility=Collapsed`/
  hidden subtrees to AT.
- The width-measurement fix the source doc proposed is **dropped** (targets an unreachable path).
