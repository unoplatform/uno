# Tasks: WASM Accessibility — Advanced Features

**Input**: Design documents from `/specs/002-wasm-a11y-advanced/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/
**Branch**: `002-wasm-a11y-advanced` | **Depends on**: `001-wasm-accessibility`

**Tests**: Included per Constitution Principle III (Test-First Quality Gates) and plan.md test file specifications.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify base infrastructure and understand extension points

- [X] T001 Build the existing 001-wasm-accessibility code and verify existing runtime tests pass: `dotnet build src/Uno.UI-Wasm-only.slnf --no-restore`
- [X] T002 [P] Review existing extension points in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs, src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts, and src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts — document how IUnoAccessibility interface and JSImport/JSExport interop are wired

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core Uno.UI changes and coordinator infrastructure that MUST be complete before ANY user story

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add `OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)` method to the IUnoAccessibility interface (or equivalent accessibility extensibility interface established in 001-wasm-accessibility) and implement an empty handler in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs
- [X] T004 Modify `AutomationPeer.RaiseAutomationEvent` in src/Uno.UI/UI/Xaml/Automation/Peers/AutomationPeer.cs to dispatch to `IUnoAccessibility.OnAutomationEvent(this, eventId)` — consult WinUI C++ sources (D:\Work\microsoft-ui-xaml2\src\dxaml\xcp\core\core\elements\AutomationPeer.cpp:1528-1534) for alignment with the ListenerExists → RaiseAutomationEvent pattern per research R1
- [X] T005 Extend WebAssemblyAccessibility.cs coordinator in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs with subsystem initialization: add fields for VirtualizedSemanticRegion registry, LiveRegionManager, FocusSynchronizer, and ModalFocusScope — initialize them during accessibility activation (after "Enable accessibility" button click)

**Checkpoint**: Foundation ready — user story implementation can begin

---

## Phase 3: User Story 1 — Virtualized List Navigation (Priority: P1) MVP

**Goal**: Screen reader users can navigate virtualized lists/grids (ItemsRepeater, ListView, GridView) with only realized items in the accessibility tree. Correct aria-posinset/aria-setsize, focus pinning, and batched DOM mutations.

**Independent Test**: Load an ItemsRepeater with 1,000 items. Arrow through items with a screen reader. Verify each announced item matches visible content and unrealized items are absent from the accessibility tree.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T006 [US1] Write runtime tests in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_VirtualizedListAccessibility.cs — test scenarios: (1) only realized items have semantic elements, (2) scrolling creates/removes semantic elements with correct posinset/setsize, (3) focused item is pinned and not recycled, (4) rapid scrolling batches DOM updates, (5) empty list shows empty-state label, (6) ListView and GridView work same as ItemsRepeater

### Implementation for User Story 1

- [X] T007 [P] [US1] Create VirtualizedSemanticRegion.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/ — implement entity per data-model.md (ContainerHandle, TotalItemCount, RealizedHandles dictionary, ViewportBounds, IsFocusPinned, PinnedIndex) with JSImport NativeMethods for: addVirtualizedItem, removeVirtualizedItem, updateVirtualizedItemCount, registerVirtualizedContainer, unregisterVirtualizedContainer (per contracts/virtualization.ts)
- [X] T008 [P] [US1] Implement virtualized item DOM lifecycle in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts — add functions per contracts/virtualization.ts: registerVirtualizedContainer (creates listbox/grid element), addVirtualizedItem (creates option/row with aria-posinset, aria-setsize), removeVirtualizedItem (removes element), updateVirtualizedItemCount (updates aria-setsize on all realized items), unregisterVirtualizedContainer (removes all)
- [X] T009 [US1] Extend AriaMapper.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AriaMapper.cs with aria-posinset and aria-setsize attribute mapping for virtualized list/grid items — map from data index (0-based) to aria-posinset (1-based) and TotalItemCount to aria-setsize
- [X] T010 [US1] Hook ItemsRepeater.ElementPrepared/ElementClearing events in VirtualizedSemanticRegion.cs — on ElementPrepared: get AutomationPeer, call AriaMapper.GetAriaAttributes, call NativeMethods.AddVirtualizedItem; on ElementClearing: call NativeMethods.RemoveVirtualizedItem and remove from RealizedHandles
- [X] T011 [US1] Hook ListViewBase virtualization lifecycle (ListView/GridView) in VirtualizedSemanticRegion.cs — subscribe to equivalent realize/unrealize events on ListViewBase's VirtualizingPanel, use same semantic element creation pattern as ItemsRepeater (per research R3: ListViewBaseAutomationPeer is currently a stub — may need similar filtering to RepeaterAutomationPeer)
- [X] T012 [US1] Implement requestAnimationFrame batching for realize/unrealize DOM mutations in SemanticElements.ts — queue add/remove operations and flush once per animation frame to prevent layout thrashing during rapid scrolling (FR-005, FR-024). NOTE: This establishes the batching pattern that other subsystems (live regions, focus sync, modal trap) should follow for their own DOM mutations per FR-024.
- [X] T013 [US1] Integrate focus pinning in VirtualizedSemanticRegion.cs — when a realized item holds focus (via FocusSynchronizer or FocusManager), set IsFocusPinned=true and PinnedIndex to prevent the item's semantic element from being removed even if ElementClearing fires (coordinate with ViewManager's existing auto-pin behavior per research R3)
- [X] T014 [US1] Handle edge cases in VirtualizedSemanticRegion.cs — (1) zero items: set container aria-label to indicate empty state, (2) EffectiveViewport reports zero-size rect: skip semantic element creation until valid viewport arrives, (3) TotalItemCount changes: call updateVirtualizedItemCount to update aria-setsize on all realized items
- [X] T015 [US1] Register VirtualizedSemanticRegion subsystem with WebAssemblyAccessibility.cs coordinator — detect ItemsRepeater and ListViewBase instances entering the visual tree, create VirtualizedSemanticRegion for each, clean up on removal

**Checkpoint**: Virtualized list accessibility works independently — screen reader can navigate realized items in ItemsRepeater, ListView, and GridView

---

## Phase 4: User Story 2 — Live Region Announcements (Priority: P1)

**Goal**: AutomationPeer.RaiseAutomationEvent(LiveRegionChanged) triggers aria-live region updates in the DOM. Two-tier rate limiting prevents announcement flooding.

**Independent Test**: Create a TextBlock in a live region, programmatically change its text and call RaiseAutomationEvent(LiveRegionChanged). Verify the aria-live region updates and the screen reader announces the new text.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T016 [US2] Write runtime tests in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_LiveRegionEvents.cs — test scenarios: (1) LiveRegionChanged event updates aria-live DOM, (2) Polite setting uses aria-live="polite", (3) Assertive setting uses aria-live="assertive", (4) rapid events within 100ms debounce to final content only, (5) sustained throttle caps polite at 500ms and assertive at 200ms, (6) Off setting is ignored

### Implementation for User Story 2

- [X] T017 [P] [US2] Create LiveRegionManager.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/ — implement entity per data-model.md (PoliteDebounceTimer, AssertiveDebounceTimer, PoliteThrottleTimestamp, AssertiveThrottleTimestamp, PendingPoliteContent, PendingAssertiveContent) with two-tier rate limiting state machine (Idle → Debouncing → Announcing → Throttled) and JSImport NativeMethods for: updateLiveRegionContent, clearPendingAnnouncements (per contracts/live-regions.ts)
- [X] T018 [P] [US2] Create LiveRegion.ts in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/ — implement per contracts/live-regions.ts: create aria-live="polite" and aria-live="assertive" div elements on init, implement updateLiveRegionContent (updates textContent based on liveSetting), announcePolite/announceAssertive with two-tier rate limiting (100ms debounce + 500ms/200ms throttle), clearPendingAnnouncements
- [X] T019 [US2] Wire WebAssemblyAccessibility.OnAutomationEvent to LiveRegionManager — in the OnAutomationEvent handler (added in T003), check for AutomationEvents.LiveRegionChanged, extract peer.GetLiveSetting() (Off=0, Polite=1, Assertive=2) and peer.GetName() for content text, pass to LiveRegionManager
- [X] T020 [US2] Implement GetLiveSetting routing in LiveRegionManager.cs — route Off (0) to no-op, Polite (1) to polite region with 500ms sustained throttle, Assertive (2) to assertive region with 200ms sustained throttle, apply 100ms debounce for both
- [X] T021 [US2] Handle edge cases in LiveRegionManager.cs — (1) rapid progress bar: sustained throttle caps output, (2) Off setting: silently drop, (3) page unload/accessibility disable: call clearPendingAnnouncements, (4) content text extraction: use AutomationPeer.GetName() for all announcement content
- [X] T022 [US2] Register LiveRegionManager with WebAssemblyAccessibility.cs coordinator — initialize LiveRegion.ts polite/assertive divs during accessibility activation, wire OnAutomationEvent dispatch

**Checkpoint**: Live region announcements work independently — RaiseAutomationEvent(LiveRegionChanged) produces screen reader announcements with correct urgency

---

## Phase 5: User Story 3 — Focus Synchronization (Priority: P2)

**Goal**: Bidirectional XAML ↔ browser focus sync. FocusManager.GotFocus moves DOM focus to the semantic element; screen reader focus on a semantic element moves XAML focus. Roving tabindex and debounce prevent storms.

**Independent Test**: Tab through 5 buttons. Verify document.activeElement matches the semantic element for the XAML-focused button. Use screen reader to move to a different button and verify XAML focus follows.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T023 [US3] Write runtime tests in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_FocusSynchronization.cs — test scenarios: (1) FocusManager.GotFocus sets DOM focus on semantic element, (2) semantic element focus event sets XAML FocusState.Keyboard, (3) roving tabindex: focused gets tabindex="0", others tabindex="-1", (4) rapid Tab holds debounce to final target only, (5) IsSyncing guard prevents infinite loops

### Implementation for User Story 3

- [X] T024 [P] [US3] Create FocusSynchronizer.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/ — implement entity per data-model.md (CurrentFocusedHandle, PreviousFocusedHandle, IsSyncing guard, CorrelationId, PendingFocusHandle, RafId) with JSImport NativeMethods for: focusSemanticElement, blurSemanticElement, updateRovingTabindex (per contracts/focus-sync.ts)
- [X] T025 [P] [US3] Implement roving tabindex and focus sync functions in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts — extend existing focusSemanticElement to set tabindex="0" on target and tabindex="-1" on previous, add blurSemanticElement, updateRovingTabindex (per contracts/focus-sync.ts), add requestAnimationFrame debounce for rapid focus calls
- [X] T026 [US3] Wire FocusManager.GotFocus → FocusSynchronizer → DOM focus (XAML→browser direction) in FocusSynchronizer.cs — subscribe to FocusManager.GotFocus static event, check IsSyncing guard, set IsSyncing=true, call NativeMethods.FocusSemanticElement(handle), update CurrentFocusedHandle/PreviousFocusedHandle, set IsSyncing=false (extends existing FocusNative path per research R2)
- [X] T027 [US3] Wire semantic element focus → FocusSynchronizer → Control.Focus() (browser→XAML direction) in FocusSynchronizer.cs — extend existing OnFocus JSExport callback: check IsSyncing guard, set IsSyncing=true, find UIElement from handle, call control.Focus(FocusState.Keyboard), set IsSyncing=false (per data flow in quickstart.md)
- [X] T028 [US3] Implement requestAnimationFrame debounce in FocusSynchronizer.cs — when multiple GotFocus events fire within one frame (e.g., holding Tab), store PendingFocusHandle and schedule via requestAnimationFrame, only the final focus target receives DOM focus (FR-014)
- [X] T029 [US3] Register FocusSynchronizer with WebAssemblyAccessibility.cs coordinator — initialize during accessibility activation, subscribe to FocusManager.GotFocus/LostFocus, wire OnFocus JSExport callback

**Checkpoint**: Focus sync works independently — Tab cycling keeps XAML and DOM focus synchronized with roving tabindex

---

## Phase 6: User Story 4 — Modal Focus Trap (Priority: P2)

**Goal**: ContentDialog traps screen reader focus within its boundaries. aria-hidden hides background. Tab/Shift+Tab wrap within modal. Focus restores on close. Nested modals supported.

**Independent Test**: Open a ContentDialog with two buttons. Verify Tab cycles only between those buttons. Shift+Tab from first wraps to last. Close dialog and verify focus returns to opener.

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T030 [US4] Write runtime tests in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_ModalFocusTrap.cs — test scenarios: (1) opening ContentDialog sets aria-hidden on background, (2) Tab wraps from last to first within modal, (3) Shift+Tab wraps from first to last, (4) closing modal removes aria-hidden and restores focus to trigger, (5) nested modals: inner traps focus, closing inner returns to outer, (6) all items in modal disabled: focus remains on dialog container (role="dialog"), (7) two ContentDialogs opened in same frame: most recent traps focus

### Implementation for User Story 4

- [X] T031 [P] [US4] Create ModalFocusScope.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/ — implement entity per data-model.md (ModalHandle, TriggerHandle, FocusableChildren, ParentScope, HiddenElements) with state machine (Inactive → Active → Restoring → Inactive) and JSImport NativeMethods for: activateFocusTrap, deactivateFocusTrap, updateFocusTrapChildren, handleTrapTab (per contracts/modal-focus.ts)
- [X] T032 [P] [US4] Create FocusTrap.ts in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/ — implement per contracts/modal-focus.ts: activateFocusTrap (set aria-hidden on background, save original tabindex, add keydown listener), deactivateFocusTrap (restore aria-hidden, restore tabindex, restore focus to trigger), updateFocusTrapChildren, handleTrapTab (wrap last→first / first→last), isFocusTrapActive, getActiveTrapHandle
- [X] T033 [US4] Hook ContentDialog.Opened/Closed events in ModalFocusScope.cs — on Opened: enumerate focusable children within the dialog's Popup, save trigger element handle (FocusManager.GetFocusedElement before opening), call NativeMethods.ActivateFocusTrap; on Closed: call NativeMethods.DeactivateFocusTrap (per research R4: ContentDialog uses Popup internally)
- [X] T034 [US4] Implement aria-hidden="true" on all semantic elements outside the modal in FocusTrap.ts — walk all elements in the semantic container, skip elements that are descendants of the modal, set aria-hidden="true" and tabindex="-1" on background elements, store originals for restoration (FR-015, FR-019)
- [X] T035 [US4] Implement Tab/Shift+Tab wrapping within modal boundaries in FocusTrap.ts — on keydown with Tab: if focused element is last in focusableHandles array, move focus to first (and vice versa for Shift+Tab), preventDefault to prevent escape (FR-016)
- [X] T036 [US4] Support nested modals in ModalFocusScope.cs — maintain ParentScope linked list: when inner modal opens, save outer scope as ParentScope; when inner closes, reactivate outer scope's trap; only innermost scope is active (FR-018). Handle edge case: if two ContentDialogs open in the same frame, treat the most recently queued dialog as the active trap (per spec edge case).
- [X] T037 [US4] Implement focus restore to trigger element on modal close in ModalFocusScope.cs — call NativeMethods.FocusSemanticElement(TriggerHandle) after deactivating trap, handle case where trigger was removed during modal (fall back to body) (FR-017)
- [X] T038 [US4] Register ModalFocusScope with WebAssemblyAccessibility.cs coordinator — detect ContentDialog instances, subscribe to Opened/Closed events, manage active ModalFocusScope lifecycle

**Checkpoint**: Modal focus trap works independently — ContentDialog traps focus with Tab wrapping, aria-hidden on background, nested modal support, and focus restore

---

## Phase 7: User Story 5 — Disabled Element Focus Recovery (Priority: P3)

**Goal**: When a focused element is disabled or removed, focus moves to the next valid target. No "silent" screen reader state.

**Independent Test**: Focus a button, set IsEnabled=false. Verify focus moves to the next sibling and screen reader announces the new target.

**Dependency**: Requires US3 (FocusSynchronizer) — extends FocusSynchronizer.cs with recovery logic

### Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T039 [US5] Write runtime tests in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_FocusRecovery.cs — test scenarios: (1) disabling focused button moves focus to next sibling, (2) removing focused element from tree moves focus to parent, (3) no other focusable elements: focus moves to body, (4) simultaneous removal + focus event is silently dropped

### Implementation for User Story 5

- [X] T040 [US5] Extend FocusSynchronizer.cs to detect focus loss when IsEnabled changes to false — subscribe to UIElement.IsEnabledChanged or equivalent property change notification, when the currently-focused element becomes disabled, trigger focus recovery (FR-020)
- [X] T041 [US5] Extend FocusSynchronizer.cs to detect focus loss when element is removed from visual tree — subscribe to UIElement.Unloaded event on the currently-focused element, when it fires, trigger focus recovery (FR-021)
- [X] T042 [US5] Implement focus recovery algorithm in FocusSynchronizer.cs — find next valid focus target: (1) next focusable sibling in tab order, (2) if none, nearest focusable ancestor, (3) if none, document body; call Control.Focus() on the recovery target and update DOM focus via FocusSynchronizer (FR-020, FR-021, FR-022)
- [X] T043 [US5] Handle edge case: simultaneous element removal + focus event — if OnFocus callback fires for a handle whose element has been removed from the visual tree, silently drop the event instead of crashing or leaving focus in limbo (per spec edge case)

**Checkpoint**: Focus recovery works independently — disabled/removed elements never leave screen reader silent

---

## Phase 8: User Story 6 — Performance Validation (Priority: P3)

**Goal**: Accessibility DOM overhead stays within budget: <2ms/frame for 500 elements, >30fps during virtualized scrolling, <16ms for 100 simultaneous property changes.

**Independent Test**: Load complex UI with 500 accessible elements. Measure frame rate with/without accessibility. Verify overhead is within bounds.

**Dependency**: Requires US1-US4 implemented (needs working features to measure)

### Tests for User Story 6

- [ ] T044 [US6] Write performance benchmarks in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibilityPerformance.cs — benchmark scenarios: (1) create/update/remove 500 semantic elements and measure per-frame overhead, (2) scroll a 1,000-item virtualized list at maximum speed and measure FPS, (3) fire 100 property changes simultaneously and measure total DOM update time

### Implementation for User Story 6

- [ ] T045 [US6] Measure and validate <2ms frame overhead with 500 semantic elements — profile using browser Performance API (performance.now()), ensure average accessibility DOM overhead per frame is below 2ms threshold (FR-023)
- [ ] T046 [US6] Measure and validate >30fps during virtualized list rapid scrolling — profile scrolling a 1,000-item list with accessibility enabled, ensure requestAnimationFrame batching keeps frame rate above 30fps (FR-024)
- [X] T047 [US6] Add performance metrics display to AccessibilityDebugger.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AccessibilityDebugger.cs — extend debug overlay to show: semantic element count, average frame overhead (ms), live region announcement rate, focus sync latency

**Checkpoint**: Performance validated — accessibility overhead within budget

---

## Phase 9: User Story 7 — WCAG 2.1 AA Compliance Verification (Priority: P3)

**Goal**: Complete accessibility implementation passes axe-core with zero critical/serious violations. NVDA and VoiceOver correctly announce all 11 supported control types.

**Independent Test**: Run axe-core against a page with all 11 control types. Verify zero critical/serious violations. Conduct manual screen reader walkthroughs.

**Dependency**: Requires US1-US5 implemented (needs complete feature set to verify)

### Implementation for User Story 7

- [ ] T048 [US7] Create comprehensive test page with all 11 supported control types (Button, CheckBox, RadioButton, Slider, TextBox, TextBox (multiline), PasswordBox, ComboBox, ListBox, ListItem, HyperlinkButton) — deploy as a WASM sample app page for axe-core scanning and manual testing
- [X] T056 [US7] Fix accessible name (aria-label) application gaps in SemanticElements.ts — codebase analysis shows aria-label is only applied for Button, CheckBox, RadioButton; extend to Slider, TextBox, TextBox (multiline), PasswordBox, ComboBox, ListBox, ListItem, and implement createLinkElement() for HyperlinkButton (FR-030, WCAG 4.1.2). Must complete before axe-core scan (T049)
- [ ] T049 [US7] Run axe-core automated scan (`npx axe-core-cli http://localhost:5000 --tags wcag2aa`) and fix all critical/serious WCAG 2.1 AA violations — iterate until zero violations (FR-026)
- [ ] T050 [US7] Conduct manual NVDA (Windows) screen reader testing — navigate all 11 control types, verify correct role/name/value/state announcements, document results (FR-027, SC-008)
- [ ] T051 [US7] Conduct manual VoiceOver (macOS Safari) screen reader testing — navigate all 11 control types, verify correct role/name/value/state announcements, document results (FR-027, SC-008)
- [ ] T052 [US7] Verify keyboard-only navigation for all interactive controls — Tab/Shift+Tab reaches all controls, Enter/Space activates, arrow keys navigate within groups, visible focus indicators present (FR-028, FR-029)

**Checkpoint**: WCAG 2.1 AA compliance verified — zero axe-core violations, screen readers announce correctly

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final integration, documentation, and quality pass

- [X] T053 Review all new public APIs and document architecture decisions — ensure JSImport/JSExport declarations follow established patterns, C# classes have XML doc comments, TypeScript functions have JSDoc
- [X] T054 Extend AccessibilityDebugger.cs in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AccessibilityDebugger.cs with debug overlays for: virtualized region state (realized count vs total), live region announcement log, focus sync state (current handle, IsSyncing), modal trap state (active/nested) — depends on T047 (both modify AccessibilityDebugger.cs; execute sequentially to avoid conflicts)
- [ ] T055 Run full integration validation — build (`dotnet build src/Uno.UI-Wasm-only.slnf --no-restore`), run all runtime tests headlessly, run axe-core scan, verify no regressions in existing 001 accessibility tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 Virtualized Lists (Phase 3)**: Depends on Foundational (Phase 2) — no dependency on other stories
- **US2 Live Regions (Phase 4)**: Depends on Foundational (Phase 2) — specifically T003/T004 for AutomationPeer dispatch
- **US3 Focus Sync (Phase 5)**: Depends on Foundational (Phase 2) — no dependency on other stories
- **US4 Modal Focus Trap (Phase 6)**: Depends on Foundational (Phase 2) — no dependency on other stories
- **US5 Focus Recovery (Phase 7)**: Depends on US3 (Phase 5) — extends FocusSynchronizer.cs
- **US6 Performance (Phase 8)**: Depends on US1-US4 — needs working features to measure
- **US7 WCAG Compliance (Phase 9)**: Depends on US1-US5 — needs complete feature set to verify
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Independence

```
Phase 2 (Foundational)
  ├── US1 (Virtualized Lists)  ─── can run in parallel ───┐
  ├── US2 (Live Regions)       ─── can run in parallel ───┤
  ├── US3 (Focus Sync)         ─── can run in parallel ───┤
  └── US4 (Modal Focus Trap)   ─── can run in parallel ───┤
                                                           │
  US5 (Focus Recovery)  ← depends on US3                   │
  US6 (Performance)     ← depends on US1-US4               │
  US7 (WCAG Compliance) ← depends on US1-US5               │
                                                           │
  Phase 10 (Polish)     ← depends on all ─────────────────┘
```

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. C# entity + TypeScript module can be created in parallel [P]
3. JSImport NativeMethods are part of the C# entity class
4. Wiring (events, coordinator) depends on entity + TS module
5. Edge cases after core implementation
6. Coordinator registration last

### Parallel Opportunities

**After Foundational completes, these can run simultaneously:**
- US1 (C# + TS files are unique, no overlap)
- US2 (C# + TS files are unique, no overlap)
- US3 (C# + TS extends Accessibility.ts — coordinate with US4)
- US4 (C# + TS files are unique — FocusTrap.ts is new)

**Within each story, [P]-marked tasks can run in parallel:**
- T007 + T008 (VirtualizedSemanticRegion.cs + SemanticElements.ts)
- T017 + T018 (LiveRegionManager.cs + LiveRegion.ts)
- T024 + T025 (FocusSynchronizer.cs + Accessibility.ts roving tabindex)
- T031 + T032 (ModalFocusScope.cs + FocusTrap.ts)

---

## Parallel Example: User Story 1

```bash
# Write tests first:
Task: "T006 — Runtime tests in Given_VirtualizedListAccessibility.cs"

# Then launch C# and TS in parallel:
Task: "T007 — VirtualizedSemanticRegion.cs with NativeMethods"
Task: "T008 — SemanticElements.ts virtualized lifecycle functions"

# Then sequential wiring:
Task: "T009 — AriaMapper extensions"
Task: "T010 — Hook ItemsRepeater events"
Task: "T011 — Hook ListViewBase events"
Task: "T012 — requestAnimationFrame batching"
Task: "T013 — Focus pinning"
Task: "T014 — Edge cases"
Task: "T015 — Coordinator registration"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 — Virtualized Lists
4. **STOP and VALIDATE**: Test virtualized list navigation with screen reader
5. This alone makes data-driven WASM apps accessible

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 (Virtualized Lists) + US2 (Live Regions) → P1 stories complete (MVP!)
3. US3 (Focus Sync) + US4 (Modal Focus Trap) → P2 stories complete
4. US5 (Focus Recovery) → P3 polish
5. US6 (Performance) + US7 (WCAG) → Validation and compliance
6. Each story adds value without breaking previous stories

### Parallel Strategy

With multiple developers after Foundational completes:
- Developer A: US1 (Virtualized Lists) then US5 (Focus Recovery)
- Developer B: US2 (Live Regions) then US6 (Performance)
- Developer C: US3 (Focus Sync) then US4 (Modal Focus Trap) then US7 (WCAG)

---

## Notes

- [P] tasks = different files, no dependencies — safe to parallelize
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable (except US5/US6/US7 which have noted dependencies)
- Constitution Principle III: Write tests first, verify they fail, then implement
- Commit after each task or logical group using conventional commits (`feat(a11y):`)
- All new C# files go in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/`
- All new TS files go in `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/`
- Core Uno.UI change (T004) is the highest-risk task — consult WinUI C++ sources before modifying
- WebAssemblyAccessibility.cs coordinator registration tasks (T015, T022, T029, T038) all touch the same file — execute sequentially
