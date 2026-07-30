---
description: "Task list for WASM Accessibility Remediation"
---

# Tasks: WASM Accessibility Remediation

**Input**: Design documents from `/specs/003-wasm-a11y-remediation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/interop-contracts.md, quickstart.md

**Tests**: INCLUDED. The constitution (Principle III) mandates fails-before/passes-after
runtime tests, and the feature's whole point includes establishing the DOM-level coverage
that is currently absent (FR-016/017/030). Every story is test-first.

**Organization**: By user story (US1–US7 from spec.md), priority order. Each story is an
independently shippable increment (mirrors plan.md Phases A–G).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: US1–US7 (user-story phases only)
- All paths are repo-relative.

## ✅ Resolved decisions (previously open)

- **FR-007 composite tabindex model** → **roving active-item** (container `-1`/none; active item `0`). T015/T016 implement this.
- **FR-013 region liveness** → **creation-time gating only**; live scrollability-transition deferred. T043 implements creation-time; no live-transition task.
- **FR-015 body text** → **gated standalone `<p>`/`<span>` emission** (standalone TextBlocks not absorbed by a parent name; keep pruning for inner/label text). T045 implements this. *(Lowest-confidence — reconfirm with product owner if DOM-bloat surfaces.)*

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Build/test harness ready; baseline captured.

- [ ] T001 Verify Skia-WASM build + runtime-test execution per `specs/003-wasm-a11y-remediation/quickstart.md` (`crosstargeting_override.props` net10.0, `Uno.UI-Wasm-only.slnf`, `/runtime-tests` skill)
- [ ] T002 [P] Capture the current failing behavior (radio unchecked, heading is a tab stop, `AutomationId`→`aria-label`, `LabeledBy` flattened) as a short repro note in `specs/003-wasm-a11y-remediation/checklists/baseline.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared mechanisms that multiple stories build on. **⚠️ Complete before the dependent story tasks** (test helper blocks all test tasks; the live-sync mechanism blocks US3/US4/US7 live-sync).

- [ ] T003 Add a runtime-test helper that enables the AOM in-test and queries the semantic DOM (`document.getElementById('uno-semantics-{handle}')`) returning role/`aria-*`/`tabIndex`/`checked`, in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/AccessibilityTestHelper.cs` (extend the existing `EnableAccessibilityThroughDom` usage) — blocks every test task below
- [ ] T004 Establish the live-sync mechanism: chain the WASM `NotifyPropertyChangedEventCore` override to base (or introduce a property→attribute map) so per-property branches add cleanly, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` (FR-010) — blocks US3/US4/US7 live-sync tasks
- [X] T005 [P] Add an `isFocusable` parameter to the `Create*Element` JSImport signatures + a shared "apply full `GetAriaAttributes` set" entry point usable by both the factory and generic paths, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs` (plumbing only; enabler for US2 FR-005 and US7 FR-021) — touches shared signatures, do before US2/US7 impl

**Checkpoint**: Test helper + live-sync + factory plumbing ready — user stories can proceed.

---

## Phase 3: User Story 1 - RadioButton usable by screen-reader & keyboard (Priority: P1) 🎯 MVP

**Goal**: A `RadioButton` group renders the correct checked state, is selectable via the DOM, syncs on external change, and has exactly one tab stop per group (FR-001–004).

**Independent Test**: Build a group with one `IsChecked=true`; enable AOM; assert the `<input type=radio>` is `checked`, DOM activation selects the peer, code-side `IsChecked` flips the native `checked`, and one radio per group is `tabindex=0`.

### Tests (write first, must FAIL)

- [X] T006 [P] [US1] Tests for RadioButton in `Given_AccessibleCheckBox.cs` (3 active, `#if HAS_UNO`, Skia-Desktop-runnable): initial `checked` from `IsChecked` (checked/unchecked), and DOM-activation path via `ISelectionItemProvider.Select()` → `IsChecked` + `Checked` mapping. *(C#-layer/peer assertions — the DOM-level `<input>` assertions need the WASM AOM and are deferred to the T003 helper / hand-off.)*
- [X] T007 [US1] Populate initial radio `checked` from `IsSelected` (RadioButton control type) in `src/Uno.UI/Accessibility/AriaMapper.cs` (FR-001) — root-cause fix.
- [X] T008 [US1] **No code change needed** — `CreateCheckboxElement` already forwards `attributes.Checked == "true"` to `CreateRadioElement` (`SemanticElementFactory.cs:222`); the bug was upstream (T007). Satisfied by T007.
- [X] T009 [US1] Routed the radio DOM `change` to `callbacks.onSelection` → existing `OnSelection` JSExport → `ISelectionItemProvider.Select()` (not `onToggle`), in `SemanticElements.ts` `createRadioElement` (FR-002).
- [X] T010 [US1] External `IsSelected` change now updates the radio's native `checked` via `UpdateAriaChecked` (not `aria-selected`/`UpdateSelectionState`) in `WebAssemblyAccessibility.cs` `NotifyPropertyChangedEventCore` (FR-003).
- [X] T011 [US1] Radio roving at creation: `element.tabIndex = checked ? 0 : -1` in `SemanticElements.ts` `createRadioElement` (FR-004). *(First-when-none-checked is completed by US2/T016 focus-driven roving.)*
- [ ] T012 [US1] **HAND-OFF**: run T006 → green and validate on Skia Desktop (shared `AriaMapper` change — plan watch-item). Validation so far: TS typecheck clean; C# code-review only. Commands in the report below.  — **Behavior runtime-validated via A11y Inspector** (radio `aria-checked` + exactly one `tabindex=0` per group, others `-1`, observed live on WASM). Remaining: the formal `Given_AccessibleCheckBox` run on Skia Desktop (needs a build).

**Checkpoint**: RadioButton fully operable (MVP).

---

## Phase 4: User Story 2 - Non-interactive elements are not tab stops (Priority: P1)

**Goal**: `tabindex` is gated on real focusability across both creation paths; headings/landmarks/group-containers leave the tab order; composites have one roving tab stop; disabled div-composites are not tabbable (FR-005–008, FR-012). *Depends on T005.*

**Independent Test**: Render headings, a ListBox, a TabControl, a Menu, and an `IsTabStop=false` control; enable AOM; assert only genuinely interactive controls are tab stops.

### Tests (write first, must FAIL)

- [X] T013 [P] [US2] Failing tests in new `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleTabindex.cs`: heading not a tab stop; `IsTabStop=false`/disabled control not a tab stop; composite container is not a second tab stop; arrow-nav moves the single roving stop (uses T003)

### Implementation

- [X] T014 [US2] Honor `isFocusable` in each `create*Element` via `updateElementFocusability` and **remove the hardcoded heading `tabIndex=0`**, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` (FR-005/006; consumes T005)
- [X] T015 [US2] Apply one consistent **roving** composite model (container `-1`/active-item `0`) for listbox/tablist/tree/menu/grid AND the virtualized container fast-path in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` (FR-007)
- [X] T016 [US2] Drive `UpdateRovingTabindex` from focus movement (call from `OnXamlGotFocus`/`OnBrowserFocus`) + promote one item at creation, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/FocusSynchronizer.cs` (FR-012)
- [X] T017 [US2] Remove the tab stop from div/table composites when disabled, at creation and in `updateDisabledState`, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` (FR-008) — *(done where a disabled flag exists: button/toggle/switch gated `isFocusable && !disabled`; div-composites have no disabled param — deferred, see handoff)*
- [ ] T018 [US2] Run T013 → green — **HAND-OFF** (no build here): run on Skia WASM after build.  — **Behavior runtime-validated via A11y Inspector** (headings emit no `tabindex`; composite containers not tab stops, observed live on WASM). Remaining: the formal `Given_AccessibleTabindex` run (needs a build).

**Checkpoint**: Tab order contains only interactive controls.

---

## Phase 5: User Story 7 - ARIA attributes correct, complete, consistent (Priority: P1 → P2)

**Goal**: Fix wrong-target mappings, close factory/generic divergence, eliminate dangling IDREFs, and map the unmapped attributes (FR-018–030). *G1 = P1; G2 = P2. Depends on T004/T005.*

**Independent Test**: Assert emitted `role`/`aria-label`/`aria-labelledby`/relationship IDREFs/state attrs against the live DOM, including a generic-path control (Image/Group) and an `AutomationId`+`LabeledBy` case.

### Tests (write first, must FAIL)

- [X] T019 [P] [US7] Failing tests in new `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleAria.cs`: `AutomationId` not `aria-label` (→ `xamlautomationid`); `LabeledBy`→`aria-labelledby`; valid role tokens (`img`/`textbox`); generic-path control carries full attrs; no dangling IDREF; `aria-invalid`/`aria-orientation` present (FR-030) — *(this batch: AutomationId-not-name (Skia) + xamlautomationid DOM (WASM); LabeledBy/IDREF deferred)*

### Implementation — G1 (P1)

- [X] T020 [US7] Source `aria-label` only from `ResolveLabel`; surface `AutomationId` as a DOM id (`xamlautomationid`/`data-*`), in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` + `.../ts/Runtime/Accessibility.ts` (FR-018) *(Extended to the factory `Create*Element` path via a post-create `SetXamlAutomationId` setter — runtime-confirmed via A11y Inspector: generic-path RadioGroup already emitted `xamlautomationid`; factory-path controls (checkbox/button/radio/textbox) now do too.)*
- [X] T021 [US7] Populate `AriaAttributes.LabelledBy` in `src/Uno.UI/Accessibility/AriaMapper.cs` and emit `aria-labelledby` (resolve labeller's semantic id) on both paths in `WebAssemblyAccessibility.cs` + `Accessibility.ts` (FR-019) *(Implemented as `AriaMapper.ResolveLabelledByElement` (labeller → UIElement) + WASM-side `SemanticElementFactory.ResolveLabelledByIdRef` (UIElement → `uno-semantics-{handle}`, gated on `HasSemanticElement` so no dangling IDREF). Wired on factory `CreateElement`, generic `AddSemanticElement`, and the dynamic `LabeledByProperty` sync (clears on non-semantic/removed labeller). `aria-label` left independent. TS `updateAriaLabelledBy` setter + both `[JSImport]`s pre-existed; this populates them (they were dead). Tests: `When_LabeledBy_Semantic_Element_Then_AriaLabelledBy_References_Labeller`, `When_LabeledBy_NonSemantic_Element_Then_No_Dangling_AriaLabelledBy`. Code-review + brace-balance + TS-typecheck only — runtime-pending `/runtime-tests` WASM.)*
- [X] T022 [US7] Normalize `FindHtmlRole` UIA tokens → valid ARIA (`image`→`img`, `edit`→`textbox`, drop `pane`/`window`/`custom`/…) and reconcile `ToggleSwitch`→`switch`, in `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.uno.cs` (FR-020; shared C# — validate the native path too)
- [ ] T023 [US7] Apply the full `GetAriaAttributes` set on the generic `AddSemanticElement` path (describedby/controls/flowto/required/description/posinset/setsize/selected/valuenow/modal+`role=dialog`) in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` (FR-021; consumes T005)
- [ ] T024 [US7] IDREF integrity: emit relationship ids only when `HasSemanticElement(handle)`, clear on remove/deselect, with defensive `getElementById` guards, in `SemanticElementFactory.cs` + `WebAssemblyAccessibility.cs` + `Accessibility.ts` (FR-022)
- [ ] T055 [US7] Virtualized-item ARIA parity: thread the full attribute set (roles, name, posinset/setsize, state) through the virtualized fast-path (`addVirtualizedItem`/`registerVirtualizedContainer`) so ListView/ItemsRepeater items don't bypass the factory's attribute application, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` + `Accessibility/WebAssemblyAccessibility.cs` (G1; research §5/§8.2) *(added during /speckit-analyze remediation)*. **PARTIAL — focus-resolution seam fixed (Inspector-found):** the T057↔T015/T016 seam — XAML focus on a virtualized item (`NavigationViewItem`) did not move DOM/AT focus to the item (`ResolveToSemanticHandle` only matched `_semanticParentMap`, which the virtualized path never writes, so it walked up to the container ancestor — Inspector "has XAML focus but DOM/AT focus did not follow"). Fixed *without* entangling virtualized items in `_semanticParentMap` (which would double-remove via `OnChildRemoved`): added `VirtualizedSemanticRegion.ContainsRealizedHandle` and a `IsRealizedVirtualizedItem` check in `ResolveToSemanticHandle` + `HasSemanticElement` (consults the region's `_realizedHandles`, which already honors the focus-pin guard). Test: `When_NavigationViewItem_Focused_Then_Its_Own_Semantic_Node_Receives_DOM_Focus`. **Runtime validation pending (`/runtime-tests` WASM + Inspector).** **STILL OPEN (the rest of T055):** thread `isFocusable`/roving `tabindex` + posinset/setsize/state through `addVirtualizedItem` (the items currently hardcode `tabindex=-1` with no `isFocusable` param).
- [ ] T025 [US7] Run the G1 subset of T019 → green (incl. T055 virtualized parity) — **HAND-OFF** (no build here): run G1 subset after build.

### Implementation — G2 (P2)

- [X] T026 [P] [US7] Map `aria-invalid` from `IsDataValidForForm` (inverted) + live-sync, in `src/Uno.UI/Accessibility/AriaMapper.cs` + `WebAssemblyAccessibility.cs` + `Accessibility.ts` (FR-023; live-sync consumes T004) **[IMPLEMENTED — live-sync wired via new `OnIsDataValidForFormChanged` callback raising `IsDataValidForFormProperty`; code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T027 [P] [US7] Map `aria-orientation` for Slider/ScrollBar (replace `orient`/CSS) in `SemanticElementFactory.cs` + `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` (FR-024) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T028 [US7] Source `aria-roledescription` from `LocalizedControlType` AND `LocalizedLandmarkType` on **all** landmark types, gated on an accessible name, in `src/Uno.UI/Accessibility/AriaMapper.cs` + `WebAssemblyAccessibility.cs` (FR-025; pairs with US5 T041) **[IMPLEMENTED — code-review + TS-typecheck only, runtime-pending /runtime-tests]** *(Source is the **authored** `AutomationProperties.LocalizedControlType`/`LocalizedLandmarkType` attached properties (null when unset), NOT the peer's `GetLocalized*Type()` which defaults to the role name — emitting `aria-roledescription="button"` on every named control is an ARIA anti-pattern. So roledescription rides only on an explicitly-authored localized type + an accessible name. Proof tests: `When_Named_Control_Without_Authored_LocalizedType_Then_No_RoleDescription` / `..._With_Authored_..._Then_RoleDescription_Emitted`.)*
- [X] T029 [P] [US7] Map `aria-level` from `AutomationProperties.Level` (distinct from `HeadingLevel`) in `src/Uno.UI/Accessibility/AriaMapper.cs` (FR-026) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [ ] T030 [US7] Add live-sync branches + wire the missing changed-callbacks/raises for `LandmarkType`/`LocalizedLandmarkType`/`FullDescription`/`IsRequiredForForm`/`IsDialog`/`LiveSetting`/`AcceleratorKey`/`AccessKey`, in `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.cs` + `WebAssemblyAccessibility.cs`; preserve `FullDescription`>`HelpText` precedence (FR-027; consumes T004)
- [X] T031 [US7] Value-semantics: `aria-haspopup` from the C# value; `AccessKey`→HTML `accesskey`; stop injecting posinset "N of M" into `aria-label`; **stop forcing `aria-atomic=true`** (omit unless a region's WinUI semantics require it), in `src/Uno.UI/Accessibility/AriaMapper.cs` + `SemanticElements.ts`/`Accessibility.ts` (FR-028) **[IMPLEMENTED — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T032 [P] [US7] Completeness gaps that have a concrete source: `aria-busy`(`ItemStatus`), `lang`(`Culture`), in `src/Uno.UI/Accessibility/AriaMapper.cs` + appliers (FR-029). `aria-owns`/`aria-current`/`aria-details` are **out of scope** (no source) per FR-029. **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [ ] T033 [US7] Run all of T019 → green

### Implementation — tree-walk completeness (P2; runtime-found, research §9)

- [ ] T057 [US7] Register virtualized containers + **backfill already-realized items at AOM-build time**: make `TryRegisterVirtualizedContainer` idempotent and invoke it from `BuildSemanticsTreeRecursive` (not only `OnChildAdded`/`!_isCreatingAOM`), and replay currently-realized repeater children through the `VirtualizedSemanticRegion.OnItemRealized` path, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` (~:308,:713-720,:1176) (FR-031; complements T055). Fixes `NavigationView` destinations missing. **ROOT CAUSE RUNTIME-CONFIRMED (Inspector + adversarial trace):** the *entire* left-nav is absent from the AT tree (`NavigationViewItem "Buttons & Links"/"Selection"/… → (no node)`), even though `IsSemanticElement` returns **true** for each (peer-present branch `:1055-1059`; `NavigationViewItemAutomationPeer` → `ListItem`/`TabItem`, both role-mapped, `SemanticElementFactory.CreateListItemElement` exists). The drop is the emission walk: `BuildSemanticsTreeRecursive` returns at the hosting `ItemsRepeater` (`:1233-1236` `if (child is ListViewBase or ItemsRepeater) return;`) and the only `TryRegisterVirtualizedContainer` call site is `OnChildAdded:308`, which early-returns during `CreateAOM` (`_isCreatingAOM` guard `:291`/`:713`). So a `NavigationView` present at *Enable-Accessibility* time gets its repeater pruned **and** no region registered. **Additional finding:** even if registered at build time, `ElementPrepared` (`:419-434`) only fires for items realized *after* subscription — so the backfill (replay currently-realized children through `OnItemRealized`) is **mandatory**, not optional. Realized-children enumeration pattern: `ItemsRepeater.GetVirtualizationInfo(e).IsRealized` (see `ItemsRepeater.Templates.cs:163-167`). **Watch-point:** double emission — guard via idempotent registration + existing `_semanticParentMap` (`:324`) + region realized-handle tracking. **Regression risk to FR-015/FR-033: none** (does not touch `IsSemanticElement`/Raw/TextBlock-absorption). Pair with T055 (attribute parity on backfilled items); gate with T059. **IMPLEMENTED:** `TryRegisterVirtualizedContainer` made idempotent (early-out when a region for the container handle already exists) and now invoked from `BuildSemanticsTreeRecursive` before the repeater early-return; both branches backfill already-realized items (ItemsRepeater via `repeater.Children` + `GetVirtualizationInfo().IsRealized`; ListViewBase via `MaterializedContainers` + `IndexFromContainer`); per-item emit extracted into a shared `EmitRealizedItem` helper reused by the live `ElementPrepared`/`ContainerContentChanging` handlers. Test added: `Given_AccessibleAria.When_NavigationView_Items_Realized_Before_Enable_Then_Each_Emits_Semantic_Node` (WASM; enable-after-load flow). **Runtime validation pending (`/runtime-tests` WASM + Inspector rebuild).** Code-review only here (no build in this environment). **FOLLOW-UP FIX (Inspector-found regression):** the backfill/handlers initially emitted *every* realized repeater child as `role="option"` with no semantic gate, so decorative `AccessibilityView=Raw` `NavigationViewItemSeparator`/`NavigationViewItemHeader` were exposed as clutter (the earlier "regression risk: none" assessment missed Raw decorative children). Fixed by gating the shared `EmitRealizedItem` chokepoint on `IsSemanticElement(itemElement)` (Raw short-circuit skips both). Test: `When_NavigationView_Has_Raw_Separator_Then_It_Is_Not_Emitted`. **SECOND FOLLOW-UP (Inspector-found, container-level):** the *container* itself had no Raw gate either — a decorative `AccessibilityView=Raw` `ItemsRepeater` (e.g. `RadioButtons`' `InnerRepeater`, template `RadioButtons.xaml:13,34`) was registered + emitted as a `role=listbox` clutter node. Fixed by gating `TryRegisterVirtualizedContainer` on `IsSemanticElement(element)` **and** making both walkers (`OnChildAdded`, `BuildSemanticsTreeRecursive`) *recurse into* a Raw container (return-early only for semantic containers) so its non-decorative items still emit via the normal walk (transparent-Raw handling, WinUI parity). Test: `When_Repeater_Is_AccessibilityView_Raw_Then_Not_Emitted_As_Listbox`. **Runtime validation pending — confirm RadioButtons items still emit correctly (`/runtime-tests` WASM + Inspector).**
- [ ] T058 [US7] Prune `Visibility=Collapsed`/hidden subtrees in the semantic-tree walk so hidden controls (e.g. the inactive `TopNavOverflowButton`) are not exposed to AT, in `WebAssemblyAccessibility.cs` `IsSemanticElement`/walk (FR-032). **Inspector-confirmed (rebuild):** INFO "Declared AccessibilityView=Content but the emitted node is hidden (not rendered)" on the NavigationView `Back`/`Close`/`Close Navigation` template parts (`NavigationView.cs:717/752`, *not* a dev overlay). **Implementation note:** membership (`IsSemanticElement`) is currently **visibility-blind** (no `Visibility`/`Visual.IsVisible`/`IsOffscreen` check); the **generic** path emit-then-hides via the DOM `hidden` attr (`Accessibility.ts:421-423` from `IsVisible` at `WebAssemblyAccessibility.cs:1373`), but the **type-specific factory path never threads visibility at all** (`CreateElement`/`Create*Element` take no `isVisible`), so typed Back/Close buttons are emitted **fully exposed**. Fix belongs in the walk/`IsSemanticElement` (skip non-rendered), not in adding more emit-then-hide flags. **IMPLEMENTED:** predicate `IsPrunedAsHidden(e) => e.Visibility == Visibility.Collapsed` (≡ `!Visual.IsVisible`, the only Collapsed signal — *not* opacity/zero-size/`IsOffscreen`, the last being a broken stub) applied as a **subtree prune** (early-`return` before emission/registration/recursion) in **both** walkers — `OnChildAdded` and `BuildSemanticsTreeRecursive` — since an `IsSemanticElement`-only skip would still leak descendants. Placed *before* `TryRegisterVirtualizedContainer`, so a Collapsed repeater is skipped and recovers idempotently when shown. **No regression:** FR-015 (pre-check ahead of the TextBlock/Raw branches) and T057 (realized-offscreen items keep `IsVisible==true`, so they are NOT pruned). Emit-then-hide DOM path retained for post-creation Visible→Collapsed flips. Test: `Given_AccessibleAria.When_Element_Collapsed_Then_No_Semantic_Node_While_Visible_Sibling_Emits` (WASM; typed factory-path Button). **Scope:** Collapsed only — elements hidden purely via Opacity/clip stay exposed by design. **Runtime validation pending (`/runtime-tests` WASM + Inspector).** Code-review only here (no build). **FOLLOW-UP FIX (Inspector-found regression):** the build-time prune left a `Collapsed→Visible` transition with no path to re-create the node (no `ShowSemanticElement` exists; `OnSizeOrOffsetChanged` only updates positioning on an existing node), permanently hiding runtime-toggled controls (NavigationView back/close/pane-toggle, flyout content, conditional UI). Fixed with a lazy re-emit: pruned handles are tracked in `_prunedHandles` (added at both prune sites, removed in `OnChildRemoved`); the `OnSizeOrOffsetChanged` visible branch does `if (_prunedHandles.Remove(handle) …) BuildSemanticsTreeRecursive(FindSemanticParent(parent), shownElement)` (O(1) guard; idempotent; re-prunes still-collapsed descendants). Test: `When_Collapsed_Element_Becomes_Visible_Then_Semantic_Node_Is_Emitted`. **Hot-path change — runtime/perf validation by maintainer required (`/runtime-tests` WASM + Inspector).**
- [ ] T059 [US7] Verify via the A11y Inspector (runtime) on a pinned-open Left `NavigationView`: all destinations reachable; no `rendered? hidden → in AT tree? exposed` phantom; AutomationId-not-reflected + ImplicitTextBlock findings clear after FR-018/FR-015 land (SC-011).

**Checkpoint**: ARIA output is correct, complete, and path-consistent; all NavigationView destinations reachable; no hidden controls exposed.

---

## Phase 6: User Story 4 - Headings expose the correct level (Priority: P2)

**Goal**: Levels 1–6 map exactly; 7–9 clamp the `<hN>` tag to `<h6>` but `aria-level` carries the true level; `aria-level` live-syncs (FR-011). *Depends on T004.*

**Independent Test**: Set `HeadingLevel=Level7`; assert `<h6>` with `aria-level=7`; change the level at runtime and assert `aria-level` updates.

### Tests (write first, must FAIL)

- [X] T034 [P] [US4] Failing tests in new `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleHeading.cs`: levels 1–6 tag+`aria-level`; 7–9 → `<h6>`+`aria-level` 7–9; runtime `HeadingLevel` change updates `aria-level` (uses T003) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**

### Implementation

- [X] T035 [US4] Level 7–9 passthrough: `aria-level` carries the true level (1–9), tag clamped to `<h6>`, in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts` `createHeadingElement` + `src/Uno.UI/Accessibility/AriaMapper.cs` (FR-011) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T036 [US4] Heading live-sync: `HeadingLevel` change updates `aria-level` (wire the raise + branch; tag re-creation best-effort), in `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.cs` + `WebAssemblyAccessibility.cs` (FR-011; consumes T004; relates to T030) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [ ] T037 [US4] Run T034 → green

**Checkpoint**: Heading levels correct and live. (Heading-not-a-tab-stop is delivered by US2/T014.)

---

## Phase 7: User Story 3 - Dynamic state changes reach AT (Priority: P2)

**Goal**: PasswordBox value and TextBox placeholder reflect runtime changes (FR-009). *Depends on T004.* (`aria-required` live-sync is owned by US7/T030/FR-027; heading `aria-level` by US4/T036/FR-011 — not here.)

**Independent Test**: Change each property in code after creation; assert the DOM attribute updates.

### Tests (write first, must FAIL)

- [ ] T038 [P] [US3] Failing tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleTextBox.cs`: programmatic `PasswordBox.Password` live-syncs (masked); runtime `PlaceholderText` change (uses T003). (`aria-required` live-sync is tested under US7/T030, not here.)

### Implementation

- [ ] T039 [US3] Raise a value automation event for `PasswordBox` (masked) so the existing `Value` sync path fires — fix the `peer is TextBoxAutomationPeer` gate, in `src/Uno.UI/UI/Xaml/Controls/PasswordBox/PasswordBox.cs` + `PasswordBoxAutomationPeer.cs` (and `TextBox.cs:361` guard) (FR-009; consult WinUI per Constitution VII; shared-code watch-item)
- [ ] T040 [US3] Add the `PlaceholderText` live-sync branch in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` (FR-009; consumes T004). NOTE: `aria-required`/`IsRequiredForForm` live-sync is owned by **T030** (FR-027), not duplicated here — coordinate on the same file.
- [ ] T041 [US3] Run T038 → green

**Checkpoint**: Runtime state changes reach AT.

---

## Phase 8: User Story 5 - Scrollable regions meaningful; static text reachable (Priority: P3)

**Goal**: `role=region` only for scrollable + named ScrollViewers; meaningful `aria-label`; standalone body text emitted as gated `<p>`/`<span>` (FR-013–015). *Decisions resolved: region creation-time gating; body-text gated standalone emission.*

**Independent Test**: Non-scrollable ScrollViewer → no region; scrollable+named → labeled region; unlabeled landmark not emitted; standalone body text exposed per chosen design.

### Tests (write first, must FAIL)

- [X] T042 [P] [US5] Failing tests in new `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleScrollViewer.cs`: non-scrollable → no `region`; scrollable+named → labeled `role=region`; no unlabeled region; no `aria-roledescription` without a name (uses T003) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**

### Implementation

- [X] T043 [US5] Gate `role=region` on actual scrollability (`IScrollProvider`) AND a real accessible name (`ResolveLabel`, not raw `GetName()`), in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` + `src/Uno.UI/Accessibility/AriaMapper.cs` (FR-013) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T044 [US5] Enforce "every landmark/region MUST have a name; never emit `aria-roledescription` without one" in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` (FR-014; pairs with US7 T028) **[IMPLEMENTED via parallel agent — code-review + TS-typecheck only, runtime-pending /runtime-tests]**
- [X] T045 [US5] Emit a non-interactive `<p>` (block) / `<span>` (inline) text element for a **standalone** body `TextBlock` not absorbed by a parent name; keep pruning for inner/label text; no `tabindex`/interactive role on it — in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs` `IsSemanticElement` + `SemanticElementFactory.cs`/`SemanticElements.ts` (FR-015; decision resolved)  — **DESIGN VETTED (do not implement blind):** the simple name-equality "absorbed-by-parent" test is **unsound** — for UIElement-content controls (`<Button><TextBlock/></Button>`) `ResolveLabel`=='' so the text would emit loose + the button stays unnamed; and string-content double-announce depends on an **unmeasurable** template depth bound. Needs **identity-based** absorption (is this the element `ResolveLabel` picks?) looking *through* the ContentPresenter, suppression of ALL content-bearing post-create attrs for `Text`, and **build/Inspector iteration** to tune. Depends on FR-033. **IMPLEMENTED:** `IsStandaloneBodyText`/`IsAbsorbedByAncestorName` (identity: `ReferenceEquals(cc.Content, element)` + string-content match + first-named-peer-ancestor equality, depth≤6), `SemanticElementType.Text` → `CreateTextElement` (early-return suppresses all post-create ARIA), TS `createTextElement` (`<p>`/`<span>`, textContent only). TS typecheck clean; C#↔TS seam 10/10. **AWAITING INSPECTOR VALIDATION** (rebuild): confirm UIElement-content buttons now named, no double-announce on string-content controls, no loose body-text duplication. **INSPECTOR-VALIDATED (rebuild):** HIGH "no accessible representation" false-positives cleared (33→28 findings); standalone Raw-boundary `"|"` correctly shows "no semantic node". **REGRESSION FOUND & FIXED:** the blanket `AutomationControlType.Text → SemanticElementType.Text` mapping routed *every* kept TextBlock — including ones kept for an explicit `LiveSetting`/`AutomationId`/`Landmark`/`Name` — through the bare `CreateTextElement` early-return, dropping `aria-live`/`xamlautomationid`/landmark-role/`aria-label` (Inspector: "LiveSetting not reflected" WARNING + "AutomationId 'ButtonsStatus' not reflected" INFO on the "Ready" status text). Fix = mapper-gate `GetTextSemanticType(owner)` in `AriaMapper.cs`: classify `Text` **only** when the owner has none of {explicit Name, Landmark≠None, LiveSetting≠Off, AutomationId} — else `Generic` (restores the pre-FR-015 generic-fallback path that emits all four). Single-file change; plain body text still → bare `<p>`. Regression tests added in `Given_AccessibleAria.cs` (mapper-level `When_LiveRegion_TextBlock_Then_SemanticType_Is_Generic` / `When_Plain_TextBlock_Then_SemanticType_Is_Text`; WASM DOM `When_LiveRegion_TextBlock_On_Wasm_Then_AriaLive_And_XamlAutomationId_Are_Emitted`). **Runtime-test execution pending (`/runtime-tests`).**
- [X] T060 [US5] **(new, surfaced by FR-015 vet)** Fix UIElement-content control naming: `ResolveLabel`'s immediate-children walk sees the template's ContentPresenter, not the content `TextBlock`, so `<Button><TextBlock/></Button>` (and HyperlinkButton/ListViewItem with element content) are **unnamed** (4.1.2). Make `ResolveLabel` look through the ContentPresenter to the content. *(FR-033; prerequisite for FR-015's absorption to be correct; build/Inspector validation required.)* **IMPLEMENTED:** added `TextBlock textBlockContent => textBlockContent.Text` to `ResolveLabel`'s `ContentControl.Content` switch (before the `UIElement => null` catch). **AWAITING INSPECTOR VALIDATION.**
- [ ] T046 [US5] Run T042 → green

**Checkpoint**: Landmarks/regions are meaningful; body-text behavior decided & tested.

---

## Phase 9: User Story 6 - Mappings covered by runtime tests (Priority: P2, cross-cutting)

**Goal**: Re-enable/replace the `[Ignore]`d a11y suite with active DOM-level assertions and add the axe gate (FR-016/017, SC-006). Finalized after the other stories land.

### Implementation

- [X] T047 [US6] Re-enable or replace the `[Ignore]`d methods with active DOM-level asserts across `Given_AccessibleButton.cs`, `Given_AccessibleCheckBox.cs`, `Given_AccessibleComboBox.cs`, `Given_AccessibleSlider.cs`, `Given_AccessibleListView.cs`, `Given_AccessibleTextBox.cs` in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/` (FR-016/017) *(Removed 51 `[Ignore]` markers and added 15 new `[PlatformCondition(SkiaWasm)]` semantic-DOM tests across the six files, reusing the `Given_AccessibleAria` harness. Code-review + brace-balance only — runtime-pending `/runtime-tests` WASM (T046/T049 will confirm timing/propagation, esp. Button `tabindex=0` and Slider `aria-valuenow` live-sync).)*
- [ ] T048 [P] [US6] Add an axe-core (or equivalent) scan gate over a page with all in-scope controls — zero "region must have a name" / focusability violations (SC-006), in `src/Uno.UI.RuntimeTests/...` 
- [ ] T049 [US6] Confirm the full a11y suite runs (not `[Ignore]`d) on Skia WASM in CI

**Checkpoint**: Coverage active; SC-003/SC-006 satisfied.

---

## Phase 10: Polish & Cross-Cutting Concerns

- [ ] T050 [P] Native-WASM-DOM regression smoke: confirm the shared `FindHtmlRole` normalization (T022) and `PasswordBox` raise (T039) improve, not break, the native path
- [ ] T051 [P] (Optional, out-of-scope) Remove the native blanket `tabindex="-1"` over-application + dead `hasOwnProperty` branch in `src/Uno.UI/ts/WindowManager.ts`
- [ ] T052 Manual screen-reader pass (NVDA Windows, VoiceOver macOS) per `quickstart.md`
- [ ] T053 [P] Update `research.md`/`spec.md` evidence labels from "code-review" to "runtime-validated" where tests now prove it
- [ ] T054 Run `quickstart.md` validation end-to-end
- [ ] T056 [P] Add a release-note / changelog entry for the observable a11y output changes (AutomationId out of `aria-label`, normalized `role` tokens, headings/containers leaving the tab order, dropped `aria-atomic`) — bug-fixes, not API breaks, but downstream-visible (Constitution VI; spec Assumptions) *(added during /speckit-analyze remediation)*

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → no deps.
- **Foundational (P2)** → after Setup. T003 blocks all test tasks; T004 blocks US3/US4/US7 live-sync; T005 blocks US2/US7 path work.
- **US1 (P3 phase)** → after Foundational. Independent (only T003 for its test).
- **US2 (P4)** → after T005.
- **US7 (P5)** → after T004 (live-sync subtasks) + T005 (generic parity).
- **US4 (P6)**, **US3 (P7)** → after T004.
- **US5 (P8)** → after Foundational; T044 pairs with US7 T028; T045 unblocked (FR-015 resolved → gated standalone emission).
- **US6 (P9)** → after the stories whose `[Ignore]`d tests it re-enables (run last among functional work).
- **Polish (P10)** → after all desired stories.

### User-story independence

- **US1, US2, US7-G1** are all P1 and largely independent — once Foundational is done they can proceed in parallel (different primary files, with T005 landed first).
- **US4, US3** (P2) are independent of each other (different controls), both consume T004.
- **US5** (P3) is independent; T044/T028 should be coordinated (same naming rule).

### Within each story

Tests first (must fail) → C#/TS implementation → run green. Validate shared-`Uno.UI` changes on Skia Desktop too (T007, T022, T039).

### Parallel opportunities

- Setup: T002 ∥ T001.
- Foundational: T005 ∥ {T003, T004} (different files); T003/T004 both edit `WebAssemblyAccessibility.cs` so sequence them.
- After Foundational: US1 ∥ US2 ∥ US7-G1 (staffed).
- Within US7-G2: T026 ∥ T027 ∥ T029 ∥ T032 (different attributes/files); T028/T030/T031 touch `AriaMapper.cs`/`WebAssemblyAccessibility.cs` — sequence.

---

## Parallel Example: User Story 1

```bash
# Test first (must fail):
Task: "T006 RadioButton DOM tests in Given_AccessibleCheckBox.cs"
# Then implementation (T007/T008 touch different files → parallelizable):
Task: "T007 initial radio checked in AriaMapper.cs"
Task: "T008 pass checked into CreateRadioElement in SemanticElementFactory.cs"
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (T003 at minimum) → 3. Phase 3 US1 → 4. **STOP & validate** RadioButton independently (Skia WASM + Desktop) → 5. ship.

### Incremental delivery (recommended order, mirrors plan Phases A–G)

US1 (RadioButton) → US2 (tabindex) → US7-G1 (ARIA wrong-target) → US4 (headings) → US3 (live-sync) → US7-G2 (ARIA completeness) → US5 (region + body text) → US6 (re-enable suite + axe). Each is an independently testable increment.

### Parallel team strategy

After Foundational: Dev A → US1, Dev B → US2, Dev C → US7-G1. Then redistribute P2/P3. Coordinate the three shared files (`AriaMapper.cs`, `WebAssemblyAccessibility.cs`, `SemanticElements.ts`) to avoid conflicts.

---

## Notes

- `[P]` = different files, no incomplete-task dependency.
- Tests are mandatory here (Constitution III) — verify red before green.
- Shared `Uno.UI` changes (AriaMapper initial radio state, FindHtmlRole normalization, PasswordBox raise) affect all Skia hosts and — for role normalization — the native path; validate beyond WASM.
- The three previously-open decisions (FR-007/FR-013/FR-015) are now **resolved** (see the top of this file); no task is blocked. FR-015's gated-standalone-emission decision is the lowest-confidence — reconfirm with the product owner if DOM-bloat surfaces.
- Total tasks: 56 (T001–T056; T055/T056 added during /speckit-analyze remediation — IDs are non-contiguous in-phase by design to preserve existing references).
- Commit per logical group with Conventional Commit messages.
