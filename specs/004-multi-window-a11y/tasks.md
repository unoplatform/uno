---

description: "Task list for feature 001-multi-window-a11y implementation"
---

# Tasks: Multi-Window Accessibility for Skia Desktop Hosts

**Input**: Design documents from `/specs/004-multi-window-a11y/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Runtime tests for `Given_MultiWindowAccessibility` are explicitly called out in spec.md (SC-007) and plan.md (Phase 1 gate). Tests are included for user stories covered by SC-007. Other existing accessibility runtime tests (SC-006) are exercised unmodified as validation — no new authoring required.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. The delivery is further split along the two-PR rollout documented in plan.md: **PR 1 ships the router + Win32 per-window behavior + automated test** (US1-Win32, US2-Win32, US4, US5). **PR 2 ships the macOS native context redesign + macOS per-window behavior + stress validation** (US1-macOS, US3). Tasks are labeled with the PR boundary where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

This feature is localized to the Skia desktop runtime assemblies:

- Shared base: `src/Uno.UI.Runtime.Skia/Accessibility/`
- Win32 host: `src/Uno.UI.Runtime.Skia.Win32/Accessibility/` and `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/`
- macOS host: `src/Uno.UI.Runtime.Skia.MacOS/Accessibility/`, `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/`, `src/Uno.UI.Runtime.Skia.MacOS/Native/`, `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/`
- Runtime tests: `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/`
- Shared helpers: `src/Uno.UI/Hosting/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Workspace preparation — no new project scaffolding is required because all affected projects exist.

- [ ] T001 Confirm `src/crosstargeting_override.props` targets `net10.0` (Skia) for local builds per AGENTS.md build setup.
- [ ] T002 Verify baseline build with `cd src && dotnet restore Uno.UI-Skia-only.slnf && dotnet build Uno.UI-Skia-only.slnf --no-restore` passes on the feature branch before any changes.
- [ ] T003 Verify baseline accessibility runtime test suite passes on the feature branch via `/runtime-tests Windows_UI_Xaml_Automation` (establishes SC-006 baseline for regression comparison).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the router, the owner interface, and refactor `SkiaAccessibilityBase` so per-window instance state is truly per-instance. These changes are prerequisites for every user story; they are not user-story-specific because they are shared plumbing.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. All per-window subclass refactors and routing flows depend on these artifacts.

- [X] T004 Add `internal static IEnumerable<KeyValuePair<XamlRoot, IXamlRootHost>> Enumerate()` (or equivalent snapshot enumeration) to `src/Uno.UI/Hosting/XamlRootMap.skia.cs` so the router can walk live hosts for fallback resolution and `ListenerExistsHelper` without a parallel registry.
- [X] T005 [P] Create `src/Uno.UI.Runtime.Skia/Accessibility/IAccessibilityOwner.cs` implementing the `internal` interface shape defined in `specs/004-multi-window-a11y/contracts/IAccessibilityOwner.cs` (single `SkiaAccessibilityBase? Accessibility { get; }` member).
- [X] T006 Create `src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs` implementing the static router per `specs/004-multi-window-a11y/contracts/AccessibilityRouter.cs`: `EnsureInitialized`, `SetActive`, `NotifyDisposed`, `Resolve(AutomationPeer)`, `Resolve(UIElement)`, `TryGetActive`, `FindAnyLiveOwner`, private `RouterAutomationPeerListener`, private `RouterAnnouncerShim`. Wire all four framework single-slot registrations (`AutomationPeer.AutomationPeerListener`, `AccessibilityAnnouncer.AccessibilityImpl`, `UIElementAccessibilityHelper.ExternalOnChildAdded/Removed`, `VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged`) in `EnsureInitialized`.
- [X] T007 Refactor `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs` per `specs/004-multi-window-a11y/contracts/SkiaAccessibilityBase.per-window.md`: remove `RegisterCallbacks()` and its writes to framework single-slot registrations, add `protected bool IsDisposed { get; private set; }`, add `public virtual void Dispose()` (sets `IsDisposed`, disposes debouncer timers, calls `DisposeCore`, untracks focused element — idempotent), add `protected abstract void DisposeCore()`, and expose router entry points `internal void RouteChildAdded`, `RouteChildRemoved`, `RouteVisualOffsetOrSizeChanged` that wrap the existing `*Core` methods.
- [X] T008 Audit per-instance fields on `SkiaAccessibilityBase` (`_trackedFocusedElement`, `_politeDebounceTimer`, `_assertiveDebounceTimer`, `_pendingPoliteContent`, `_pendingAssertiveContent`, `_lastAnnouncedPoliteContent`, `_lastAnnouncedAssertiveContent`, `_politeThrottleTimestamp`, `_assertiveThrottleTimestamp`) in `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`: remove any `static` modifier that would make them process-scoped and ensure their initialization runs in the instance constructor so two instances maintain independent state (enforces FR-009/FR-019 data-integrity rules).
- [X] T009 Guard pending dispatcher callbacks in `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs` — every `DispatcherQueue.TryEnqueue` continuation (announcement flushers, structure-change coalescing) checks `IsDisposed` first and short-circuits (addresses edge case "Window close during dispatch").
- [ ] T010 Build foundational layer with `cd src && dotnet build Uno.UI-Skia-only.slnf --no-restore` to confirm `IAccessibilityOwner`, `AccessibilityRouter`, and the refactored `SkiaAccessibilityBase` compile together before per-host wiring begins.

**Checkpoint**: Router + interface + base class are in place; both host wrappers still compile against the old singleton call sites (subclasses still call `RegisterCallbacks` removal must be handled per-host in Phase 3+).

---

## Phase 3: User Story 1 - Screen reader can navigate every top-level window (Priority: P1) 🎯 MVP

**Goal**: Every open top-level `Window` owns its own accessibility tree. Screen readers (Narrator / VoiceOver) can reach every control in every window regardless of which is active.

**Independent Test**: Launch a sample that opens two windows. With a screen reader active, navigate controls in the primary window, switch to the secondary, navigate its controls, then switch back. All controls in both windows are announced with the right labels, roles, and states. Closing either window leaves the other fully navigable.

### Win32 implementation (PR 1)

- [X] T011 [US1] Refactor `src/Uno.UI.Runtime.Skia.Win32/Accessibility/Win32Accessibility.cs`: remove `static _instance` / `static _currentHwnd`; change constructor to take `(HWND hwnd, UIElement rootElement, DispatcherQueue dispatcherQueue)`; make `_hwnd`, `_rootProvider`, `_providers` (`ConditionalWeakTable<UIElement, Win32RawElementProvider>`), `_pendingStructureChanges`, `_dispatcherQueue` per-instance fields.
- [X] T012 [US1] Add `protected override void DisposeCore()` to `src/Uno.UI.Runtime.Skia.Win32/Accessibility/Win32Accessibility.cs` that enumerates `_providers` via the .NET 9+ `ConditionalWeakTable` `IEnumerable<KeyValuePair<...>>` support, invokes `UiaDisconnectProvider(provider)` for each entry (using existing helper in `Win32AccessibilityInterop`/`Win32AccessibilityPatterns`), then clears the table. Do NOT call `UiaDisconnectAllProviders` (process-wide; disallowed by research Decision 5).
- [X] T013 [US1] Update `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs` to implement `IAccessibilityOwner`: add private `Win32Accessibility? _accessibility` field; construct it after the `HWND` is available and `Window.RootElement` is set; expose `SkiaAccessibilityBase? Accessibility => _accessibility`.
- [X] T014 [US1] In `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs`, ensure `AccessibilityRouter.EnsureInitialized()` is called once per process on wrapper construction (idempotent; router handles re-entry).
- [X] T015 [US1] Wire per-window disposal in `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs`: on `WM_DESTROY`, call `_accessibility?.Dispose()` *before* `XamlRootMap.Unregister(xamlRoot)` and *before* releasing the `HWND`; then call `AccessibilityRouter.NotifyDisposed(this)` so active-owner fallback runs.

### macOS implementation (PR 2 — deferred until Phase 5 lands native context)

- [X] T016 [US1] Refactor `src/Uno.UI.Runtime.Skia.MacOS/Accessibility/MacOSAccessibility.cs`: remove `static _instance`; change constructor to take `(nint windowHandle, UIElement rootElement, DispatcherQueue dispatcherQueue)`; make `_windowHandle`, `_accessibilityTreeInitialized`, `_isCreatingAOM`, `_activeModalHandle`, `_modalTriggerHandle` per-instance fields; call `uno_accessibility_init_context(_windowHandle)` from the constructor (native API added in T026).
- [X] T017 [US1] Update `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs` (or `MacOSWindowWrapper.cs` — whichever is the accessibility owner per data-model.md) to implement `IAccessibilityOwner`: construct `MacOSAccessibility` per window after the native window is ready and the root element is set; expose `SkiaAccessibilityBase? Accessibility`.
- [X] T018 [US1] Remove the primary-window guard at `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowNative.cs:54` (`if (MacSkiaHost.Current.InitialWindow == this)`) per plan.md PR 2 scope and research Decision 6 — secondary windows initialize accessibility once per-window context exists.

### Automated test (PR 1, gated on Skia Desktop Windows until PR 2)

- [X] T019 [US1] Author `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs`: create a secondary `Window`, assert both primary and secondary hosts resolve to non-null `IAccessibilityOwner.Accessibility` with `IsAccessibilityEnabled == true`, assert `Resolve(elementFromWindowA)` returns window A's instance and `Resolve(elementFromWindowB)` returns window B's instance (covers SC-007 a, c). Apply `[PlatformCondition(ConditionalTest.IsSkiaDesktopWindows)]` for PR 1; remove the condition in PR 2 (tracked in T029).
- [ ] T020 [US1] Run `/runtime-tests Given_MultiWindowAccessibility` against Skia Desktop (Windows) and confirm the two-window tree-resolution assertions pass.

**Checkpoint**: In PR 1, Win32 screen-reader navigation of both windows works and is covered by `Given_MultiWindowAccessibility`. macOS retains primary-only behavior (documented FR-024 interim) until PR 2 (Phase 5) lands.

---

## Phase 4: User Story 2 - Announcements route to the correct window (Priority: P1)

**Goal**: Announcements with a source element route to that element's window. Source-less announcements route to the currently active window (sticky across deactivation) or drop gracefully with a trace if none (FR-008).

**Independent Test**: In a two-window app, trigger an announcement from window A and then the same text from window B within 200 ms — both reach the platform screen reader without cross-window dedup. Trigger an announcement whose source is in window B while window A is active — it is announced in window B's context.

- [X] T021 [US2] Wire `WM_ACTIVATE` handling in `src/Uno.UI.Runtime.Skia.Win32/UI/Xaml/Window/Win32WindowWrapper.cs`: on `WA_ACTIVE` and `WA_CLICKACTIVE`, call `AccessibilityRouter.SetActive(this)`; do **not** clear on `WA_INACTIVE` (sticky per research Decision 3).
- [X] T022 [US2] Wire `NSWindowDidBecomeMainNotification` in `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs` (or the wrapper owning the notification subscription): on `DidBecomeMain`, call `AccessibilityRouter.SetActive(this)`; do not react to `DidResignMain` (sticky).
- [X] T023 [US2] Verify `RouterAnnouncerShim.AnnouncePolite` / `AnnounceAssertive` implementations in `src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs` drop source-less announcements with a diagnostic trace when `TryGetActive()` returns null (FR-008); add the trace call via `this.Log().Debug(...)` style used elsewhere in the Skia runtime. Confirm the peer-element path (NotifyNotificationEvent) uses `Resolve(peer)` not `TryGetActive`, so source-bearing announcements always route to the owning window regardless of activation state (FR-006).
- [X] T024 [US2] Extend `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs` with an announcement-routing case: raise a polite announcement via an automation peer in window B while window A is active, assert the routed instance equals window B's `SkiaAccessibilityBase`. Also assert that the two windows' debouncer timers (`_politeDebounceTimer`) are distinct object references (guards FR-009).
- [ ] T025 [US2] Run `/runtime-tests Given_MultiWindowAccessibility` against Skia Desktop (Windows) and confirm the announcement-routing assertions pass.

**Checkpoint**: Announcement routing is correct in PR 1 for Win32. macOS announcement routing works through the same router once PR 2 lands (no additional announcement-specific work needed on macOS).

---

## Phase 5: User Story 3 - Rapidly creating and closing windows does not crash (Priority: P1)

**Goal**: 100× create-then-close of a secondary window with a screen reader active does not crash, does not leak accessibility state, and does not disturb the primary window. This is the primary macOS native redesign (PR 2).

**Independent Test**: Create and immediately close a secondary window 100 times in a tight loop (from user code or a sample) with a screen reader active. Process does not crash, resident memory does not grow linearly, primary window remains fully accessible.

### macOS native per-window context (PR 2)

- [X] T026 [US3] Replace `UNOAccessibility.h` at `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOAccessibility.h` per `specs/004-multi-window-a11y/contracts/UNOAccessibility.context.h`: declare `@interface UNOAccessibilityContext` (properties: `window` weak, `elements` NSMutableDictionary, `rootElement`, `focusedElement`); declare `uno_a11y_context_for_window`; update all `uno_accessibility_*` entry points to take `NSWindow*` for window-scoped operations (`init_context`, `destroy_context`, `create_element`, `remove_element`, `post_layout_changed`, `post_announcement`, `set_focus`).
- [X] T027 [US3] Rewrite `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOAccessibility.m`: remove `g_elements` / `g_rootElement` / `g_focusedElement` / `g_window` globals; implement `UNOAccessibilityContext`; implement `uno_accessibility_init_context(NSWindow*)` by creating context and calling `objc_setAssociatedObject(window, key, context, OBJC_ASSOCIATION_RETAIN)`; implement `uno_accessibility_destroy_context(NSWindow*)` by clearing dict, nilling root/focused, then `objc_setAssociatedObject(window, key, nil, OBJC_ASSOCIATION_RETAIN)`; rewrite each `uno_accessibility_*` function to resolve context via `uno_a11y_context_for_window(window)` (or from the element's back-pointer for per-element setters) and tolerate a nil context (drop with trace per FR-017/FR-018).
- [X] T028 [US3] Update `src/Uno.UI.Runtime.Skia.MacOS/Native/NativeUno.cs` P/Invoke declarations to match the new signatures (`IntPtr window` parameter for window-scoped entry points; add `uno_accessibility_init_context` / `uno_accessibility_destroy_context` imports).
- [X] T029 [US3] Complete `src/Uno.UI.Runtime.Skia.MacOS/Accessibility/MacOSAccessibility.cs` disposal path: override `DisposeCore` to call `uno_accessibility_destroy_context(_windowHandle)` *before* `MacOSWindowNative.Handle` is cleared to zero (order matters — research Decision 6).
- [X] T030 [US3] Remove the `[PlatformCondition(IsSkiaDesktopWindows)]` guard on `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs` so it runs on full Skia Desktop coverage (PR 2 gate per plan.md).

### Disposal correctness and stress validation

- [X] T031 [US3] Add a disposal-assertion case to `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs` covering SC-007 b and d: confirm the primary and secondary provider/element sets are disjoint, then dispose window B and assert window A's `Accessibility` remains enabled and responsive while the router's `Resolve` for any former window-B element returns null.
- [ ] T032 [US3] Run `/runtime-tests Given_MultiWindowAccessibility` against Skia Desktop (Windows) and Skia Desktop (Mac) and confirm all SC-007 assertions pass on both platforms.
- [ ] T033 [US3] Execute the macOS stress scenario in `specs/004-multi-window-a11y/quickstart.md` "Disposal & stress" section: 100-iteration rapid create/destroy of a secondary window with VoiceOver active; record no segfault, memory within noise of baseline, primary window remains accessible (SC-004). Capture evidence in the PR 2 description.

**Checkpoint**: macOS multi-window support is live; rapid create/destroy is stable. Both Win32 and macOS now fully deliver US1–US3.

---

## Phase 6: User Story 4 - Per-window focus and announcement state isolation (Priority: P2)

**Goal**: Two windows' focus trackers, debouncers, and duplicate-suppression do not interfere. Mostly a property of the Phase 2 refactor — this phase validates the property and closes remaining shared-state risks.

**Independent Test**: Two windows each announce the same live-region text at overlapping intervals. Both windows independently apply their debouncing/throttling/duplicate-suppression. Focus moves between windows without one window's focus-recovery logic affecting the other.

- [X] T034 [P] [US4] Audit `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs` for any remaining `static` announcement/focus state (in addition to the T008 audit) — specifically `ResetAnnouncementTracking`, `_lastAnnouncedPoliteContent`, `_lastAnnouncedAssertiveContent`. Confirm each resolves to the per-instance field and not a base-class `static`.
- [X] T035 [P] [US4] Audit `src/Uno.UI.Runtime.Skia.Win32/Accessibility/Win32Accessibility.cs` for any remaining `static` UIA state (expected: none after T011–T012); any surviving static dictionary, cache, or field is a data-integrity violation.
- [X] T036 [P] [US4] Audit `src/Uno.UI.Runtime.Skia.MacOS/Accessibility/MacOSAccessibility.cs` for any remaining `static` per-host state (expected: none after T016 and T029); capture same invariant as Win32 audit.
- [X] T037 [US4] Extend `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs` with a polite-announcement duplicate-suppression test: trigger the same polite text in both windows within the debounce window; assert both instances' `_lastAnnouncedPoliteContent` record the announcement (or use the public debouncer surface) — neither window's dedup suppressed the other (FR-009).
- [X] T038 [US4] Extend `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs` with a focus-recovery isolation test: set focus in window A, remove the focused element from window A's tree, assert window A's `RecoverFocus` runs against window A's root; assert window B's `_trackedFocusedElement` is untouched (FR-010/FR-011).

**Checkpoint**: Per-window state isolation is verified by automated regression protection.

---

## Phase 7: User Story 5 - Maintains parity on primary-window-only applications (Priority: P1)

**Goal**: Single-window applications behave identically to the pre-refactor baseline. No regression in tree correctness, announcement timing, focus behavior, or performance.

**Independent Test**: Run the existing accessibility runtime test suite (`Given_AccessibleButton`, `Given_AccessibleCheckBox`, `Given_AccessibleSlider`, `Given_AccessibleTextBox`, `Given_AccessibilityAnnouncements`, `Given_AccessibilityFocus`, `Given_AccessibleComboBox`, `Given_AccessibleListView`, `Given_AutomationPeer`) against the refactored implementation. All tests pass unmodified.

- [X] T039 [US5] Run full accessibility test suite against the refactored implementation with `/runtime-tests Windows_UI_Xaml_Automation` on Skia Desktop (Windows); confirm all existing tests listed in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/` pass without modification (SC-006).
- [ ] T040 [US5] Run the same full accessibility test suite against Skia Desktop (Mac) after PR 2 work lands; confirm unmodified pass rate (SC-006 across platforms).
- [ ] T041 [US5] Profile per-callback router overhead on a single-window scenario (e.g., using the Win32 SamplesApp's accessibility hot path) — confirm router dispatch adds at most one dictionary lookup per callback as stated in plan.md performance constraints; record a qualitative measurement in the PR description.

**Checkpoint**: No regression on single-window scenarios on either platform.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, manual validation, and rollout hygiene spanning both PRs.

- [X] T042 [P] Verify diagnostic traces land in the categories listed in `specs/004-multi-window-a11y/quickstart.md` "Diagnostic traces to watch for" — `Uno.UI.Runtime.Skia.Accessibility.AccessibilityRouter`, `Uno.UI.Runtime.Skia.SkiaAccessibilityBase`, `Uno.UI.Runtime.Skia.Win32.Win32Accessibility` — by enabling `LogLevel.Debug` during a test run and confirming each drop path (source-less announcement with no active owner, callback for peer with no XamlRoot, pre-init event on a new instance) produces a trace.
- [ ] T043 [P] Execute the Windows / Narrator manual validation checklist in `specs/004-multi-window-a11y/quickstart.md` and record completion in the PR 1 description (SC-008).
- [ ] T044 Execute the macOS / VoiceOver manual validation checklist in `specs/004-multi-window-a11y/quickstart.md` (including the CRITICAL rapid create/destroy stress block) and record completion in the PR 2 description (SC-008, SC-004).
- [X] T045 [P] Author the PR 1 description documenting the macOS primary-window-only interim limitation required by FR-024 (link the upcoming PR 2 scope inline without referencing private trackers, per AGENTS.md "Public Documentation and Spec References").
- [X] T046 [P] Author the PR 2 description documenting removal of the interim limitation and the full macOS multi-window accessibility support; reference the automated `Given_MultiWindowAccessibility` coverage expansion and the manual VoiceOver stress-test evidence.
- [ ] T047 Final cross-platform gate: `cd src && dotnet build Uno.UI-Skia-only.slnf --no-restore`, `/runtime-tests Given_MultiWindowAccessibility`, `/runtime-tests Windows_UI_Xaml_Automation` — all green before merging each PR.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; establishes baseline.
- **Foundational (Phase 2)**: Depends on Setup completion — **BLOCKS all user stories**. Introduces the router, owner interface, and per-instance base class refactor.
- **User Story 1 (Phase 3)**: Depends on Phase 2. PR 1 delivers Win32 (T011–T015); PR 2 delivers macOS (T016–T018). Automated test (T019–T020) lands with PR 1 and is promoted (T030) in PR 2.
- **User Story 2 (Phase 4)**: Depends on Phase 2 and on US1 Win32 wrapper work (T013–T015) for `WM_ACTIVATE` wiring. macOS activation wiring (T022) depends on US1 macOS wrapper work (T017).
- **User Story 3 (Phase 5)**: Depends on Phase 2 and on US1 macOS wrapper work (T016–T017). This is the PR 2 core body.
- **User Story 4 (Phase 6)**: Depends on Phase 2 being complete; audits (T034–T036) can run after each host refactor is done. Test extensions (T037–T038) depend on US1 + US2 being wired so both windows are fully functional.
- **User Story 5 (Phase 7)**: Depends on Phase 2 completion; Win32 validation (T039) is a PR 1 gate; macOS validation (T040) is a PR 2 gate.
- **Polish (Phase 8)**: Depends on all preceding phases per-PR. Manual Narrator validation (T043) and PR 1 description (T045) gate PR 1; manual VoiceOver validation (T044) and PR 2 description (T046) gate PR 2.

### User Story Dependencies

- **US1 (P1)**: Foundational-only. Win32 half independently shippable (PR 1). macOS half depends on the native context work in US3 (T026–T029) because `MacOSAccessibility` constructor calls `uno_accessibility_init_context`.
- **US2 (P1)**: Depends on US1 wrapper-level work for `SetActive` hookpoints. Independent of US3.
- **US3 (P1)**: Depends on US1 macOS wrapper plumbing (T017) and on the native redesign (T026–T028). Delivers PR 2.
- **US4 (P2)**: Independent audit/test tasks after Phase 2; no strong ordering relative to US1/US2/US3 beyond needing their wiring for the multi-window test scenarios.
- **US5 (P1)**: Pure validation against the refactor; runs after each PR's host-specific work completes.

### PR Boundary

- **PR 1 (Win32-only)**: T001–T015, T019–T025, T034–T035, T037–T039, T042–T043, T045, T047 (Win32 gate).
- **PR 2 (macOS)**: T016–T018, T026–T033, T036, T040–T041, T044, T046, T047 (full gate).

### Within Each User Story

- Router/owner/base refactor (Phase 2) before any host-specific wiring.
- Host refactor (Win32Accessibility / MacOSAccessibility) before wrapper refactor (wrapper holds the instance).
- Wrapper refactor before activation / disposal wiring (wrapper owns those hooks).
- Automated test authoring (`Given_MultiWindowAccessibility`) grows incrementally per story — author once (T019), extend per story (T024, T031, T037, T038), unlock on Mac (T030).

### Parallel Opportunities

- T005 and T006 can run in parallel with T004 once the `IXamlRootHost` enumeration signature is agreed (T005 only uses types already in place; T006 consumes T004's enumeration — treat T004→T006 as serial, T005 as fully parallel).
- T011 (`Win32Accessibility` refactor) and T016 (`MacOSAccessibility` refactor) are on different files and parallelizable once Phase 2 lands — but T016 additionally waits on T026–T028 for the native context surface.
- T034, T035, T036 are three independent static-field audits on three different files — run in parallel.
- T042, T043, T045 are independent polish tasks — run in parallel (T044 and T046 are the macOS equivalents and parallelize the same way).

---

## Parallel Example: Phase 2 Foundational

```bash
# After T004 lands XamlRootMap.Enumerate, these run in parallel:
Task: "Create IAccessibilityOwner.cs in src/Uno.UI.Runtime.Skia/Accessibility/IAccessibilityOwner.cs"   # T005
Task: "Create AccessibilityRouter.cs in src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs"   # T006

# Then:
Task: "Refactor SkiaAccessibilityBase per-instance state"   # T007–T009 (same file, serial)
```

## Parallel Example: Phase 6 State-Isolation Audits

```bash
# All three touch different files:
Task: "Audit SkiaAccessibilityBase for static state"   # T034
Task: "Audit Win32Accessibility for static state"      # T035
Task: "Audit MacOSAccessibility for static state"      # T036
```

---

## Implementation Strategy

### MVP = PR 1 (Win32 multi-window)

1. Complete Phase 1: Setup (baseline validation).
2. Complete Phase 2: Foundational — router, owner interface, base-class refactor.
3. Complete Phase 3 Win32 slice (T011–T015), Phase 4 Win32 slice (T021, T023–T025), Phase 6 Win32 audit (T035, T037–T038), Phase 7 Win32 validation (T039), Phase 8 PR 1 polish (T042–T043, T045).
4. Merge PR 1. macOS continues with the documented primary-window-only interim (FR-024).

### PR 2 = macOS multi-window

1. Pick up at Phase 3 macOS slice (T016–T018), deliver the full native-context redesign (Phase 5 / T026–T033), finish Phase 6 macOS audit (T036), Phase 7 macOS validation (T040–T041), Phase 8 PR 2 polish (T044, T046).
2. Promote the automated test off `PlatformCondition` (T030) so Skia Desktop Mac is covered too.
3. Merge PR 2. The feature is complete across both hosts.

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready (no user-visible effect yet; single-window behavior preserved because routing fans out to the single existing instance).
2. + Phase 3 Win32 slice → Win32 multi-window tree works.
3. + Phase 4 Win32 slice → Win32 announcement routing correct.
4. + Phase 6 audits + Phase 7 validation + Phase 8 polish (PR 1) → PR 1 shippable.
5. + Phase 3 macOS slice + Phase 5 native context + Phase 6 + Phase 7 + Phase 8 (PR 2) → macOS complete.

### Parallel Team Strategy

With multiple developers:

1. Together: finish Phase 1 + Phase 2 (single-PR-owner review of the router change is advised).
2. Then split PR 1 Win32 work vs. starting PR 2 macOS native-context redesign in parallel (different files, different platform tooling).
3. Integration points: automated test (`Given_MultiWindowAccessibility.cs`) is shared — coordinate authoring / extensions to avoid merge conflicts; each story adds a distinct `[TestMethod]`.

---

## Notes

- [P] tasks = different files, no dependencies.
- [Story] label maps task to specific user story; setup/foundational/polish phases carry no story label.
- Each user story's automated assertions live in `Given_MultiWindowAccessibility.cs` — new `[TestMethod]` per story to keep independence.
- Verify existing tests still pass before extending them.
- Commit at each checkpoint.
- **Do not skip hooks / signing** (AGENTS.md + CLAUDE.md operational rules).
- **Do not reference private trackers** in the PR descriptions (AGENTS.md Public Documentation rule).
- Always run the Skia Desktop build + runtime test gate before merging either PR (T047).
