# Tasks: Mobile Accessibility and Automation

**Input**: Design documents from `/specs/005-mobile-a11y-automation/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`,
`contracts/mobile-adapter-contract.md`, `contracts/capability-matrix.md`, `quickstart.md`

**Tests**: Required. Every behavioral implementation task follows a failing native-observable
runtime test or contract test.

**Organization**: Tasks are grouped by user story. Android and iOS tasks are marked parallel
only when they touch different platform projects and share no incomplete file dependency.

## Phase 1: Setup

**Purpose**: Verify the isolated worktree and existing repository configuration before code changes.

- [X] T001 Verify C#/.NET and mobile build-output patterns are already covered in `.gitignore`
- [X] T002 Run the existing shared accessibility test baseline from `src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj`

---

## Phase 2: Foundational - Shared Peer Contract and Test Access

**Purpose**: Establish shared peer traversal/action semantics and native test hooks that block all stories.

**CRITICAL**: No user-story implementation starts until this phase is complete.

- [X] T003 [P] Add failing Control/Content/Raw, EventsSource, custom-peer, item-peer, and peer-order tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityTree.cs`
- [X] T004 [P] Add shared native-adapter test fixture abstractions in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/MobileAccessibilityTestHelper.cs`
- [X] T005 Implement live-peer tree inclusion, EventsSource resolution, and provider-action helpers in `src/Uno.UI/Accessibility/AccessibilityPeerHelper.cs`
- [X] T006 Add internal test-access contracts for node lookup, registry counts, and event observation in `src/Uno.UI/Accessibility/AccessibilityPeerHelper.cs`
- [X] T007 Run the shared automation tests from `src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj` and confirm T003 passes

**Checkpoint**: Shared peer semantics and test contracts are ready; platform work may proceed.

---

## Phase 3: User Story 1 - Discover and Understand Every Control (Priority: P1) - MVP

**Goal**: Expose correct native Android and iOS trees with stable identity, peer reading order,
names, roles, basic state, relationships, and bounds.

**Independent Test**: Render the standard automation fixture and inspect the real
`AccessibilityNodeInfoCompat` and `UIAccessibilityElement` trees; each node matches its resolved peer.

### Tests for User Story 1

- [X] T008 [P] [US1] Add failing Android native-node tests for tree order, name, role, state, bounds, Raw pruning, and stable IDs in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_SkiaAndroidAccessibilityNode.cs`
- [X] T009 [P] [US1] Add failing iOS native-element tests for container order, label, traits, state, frame, Raw pruning, and stable identity in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_SkiaIOSAccessibilityElement.cs`

### Implementation for User Story 1

- [X] T010 [P] [US1] Create `AndroidSkiaAccessibility` and initialize the router in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.Android/Hosting/AndroidHost.cs`
- [X] T011 [US1] Implement `IAccessibilityOwner`, activation, existing-helper configuration, and disposal in `src/Uno.UI.Runtime.Skia.Android/Hosting/AndroidSkiaXamlRootHost.cs`, `src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs`, and `src/Uno.UI.Runtime.Skia.Android/Rendering/IUnoSkiaRenderView.cs`
- [X] T012 [US1] Replace tab-stop enumeration and `Window.Current` root lookup with peer-tree traversal, accessible hit testing, weak virtual-ID maps, generation checks, and preassigned relation IDs in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T013 [US1] Populate Android basic node semantics, AutomationId identity, screen bounds, roles, labels, help, enabled/password/heading/dialog state, and basic relations in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T014 [P] [US1] Create the managed iOS virtual element and per-window adapter in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T015 [US1] Initialize the router and wire `RootViewController`/`NativeWindowWrapper` owner lifecycle in `src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/AppleUIKitHost.cs`, `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/RootViewController.cs`, and `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/AppleUIKitWindowWrapper.cs`
- [X] T016 [US1] Populate iOS labels, hints, traits, values, identifiers, container ordering, `AccessibilityFrameInContainerSpace`, basic label relationships, and stable handle maps in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T017 [P] [US1] Extend the representative control fixture in `src/SamplesApp/SamplesApp.Samples/Windows_UI.Xaml_Automation/AccessibilityScreenReaderPage.xaml`
- [ ] T018 [US1] Run US1 native-tree tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T018**: The complete Android automation namespace passes on an API 36
> emulator with TalkBack enabled: 328 passed, 0 failed, 133 platform skips. iOS native
> execution still requires a macOS/Xcode runner.

**Checkpoint**: TalkBack, VoiceOver, UIAutomator, and XCUITest can discover the standard fixture.

---

## Phase 4: User Story 2 - Operate Controls Through Assistive Technology (Priority: P1)

**Goal**: Advertise and execute all applicable native actions through live WinUI provider patterns.

**Independent Test**: Perform each native action against a representative peer and verify the
Uno control/provider state changes while disabled/read-only operations fail safely.

### Tests for User Story 2

- [X] T019 [P] [US2] Add failing cross-platform provider-action tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityActions.cs`
- [X] T020 [P] [US2] Add Android native action-ID and argument tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_SkiaAndroidAccessibilityNode.cs`
- [X] T021 [P] [US2] Add iOS activate/adjust/scroll/escape/custom-action tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_SkiaIOSAccessibilityElement.cs`

### Implementation for User Story 2

- [X] T022 [US2] Implement shared live-provider action execution for Invoke, Toggle, SelectionItem, ExpandCollapse, Value, RangeValue, Scroll, ScrollItem, VirtualizedItem, Window, MultipleView, Transform, and custom operations in `src/Uno.UI/Accessibility/AccessibilityPeerHelper.cs`
- [X] T023 [P] [US2] Map Android standard/custom actions and arguments to the shared action executor in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T024 [P] [US2] Map iOS activate, increment/decrement, scroll, escape, and localized custom actions to the shared action executor in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T025 [P] [US2] Add version-safe mixed toggle-state support in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AccessibilityNodeInfoCompatJni.cs`
- [X] T026 [US2] Enforce UI-thread dispatch and native failure translation for disabled, read-only, unavailable, and invalid-argument actions in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [ ] T027 [US2] Run US2 action tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T027**: Android provider-action coverage passes as part of the 326-test native
> namespace run. iOS native execution remains deferred for the T018 runner constraint.

**Checkpoint**: Primary mobile accessibility actions are operable and state-correct.

---

## Phase 5: User Story 3 - Keep Accessibility Focus Synchronized (Priority: P1)

**Goal**: Synchronize XAML and native accessibility focus without loops across dialogs,
popups, scrolling, virtualization, and removal.

**Independent Test**: Move focus in both directions through a modal and virtualized list and
verify a single valid native/XAML target throughout.

### Tests for User Story 3

- [X] T028 [P] [US3] Replace ignored focus coverage with failing mobile focus/modal tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibilityFocus.cs`
- [X] T029 [P] [US3] Add native focus-event and stale-focused-node tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityLifecycle.cs`

### Implementation for User Story 3

- [X] T030 [P] [US3] Implement Android native focus events, XAML handoff guards, accessible hit-test focus, modal filtering, and focus recovery in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T031 [P] [US3] Implement iOS focus notifications, XAML handoff guards, modal container filtering, and focus restoration in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T032 [US3] Align disabled-versus-removed focus behavior with WinUI in `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`
- [ ] T033 [US3] Run US3 focus tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T033**: Android focus, modal, input-focus, and stale-node tests pass in the
> native TalkBack-enabled run. iOS native execution remains deferred.

**Checkpoint**: Native accessibility focus remains valid and modal-safe.

---

## Phase 6: User Story 4 - Hear Live State and Structure Changes (Priority: P1)

**Goal**: Keep mounted native nodes synchronized with peer property, structure, text,
selection, window, and notification changes.

**Independent Test**: Mutate every mapped dynamic property and raise every supported event;
the existing native node reports final state and emits the correct native signal once.

### Tests for User Story 4

- [X] T034 [P] [US4] Add failing property, structure, notification, and automation-event tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityEvents.cs`
- [X] T035 [P] [US4] Re-enable and extend announcement tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibilityAnnouncements.cs`

### Implementation for User Story 4

- [X] T036 [P] [US4] Add Android generic property fallthrough invalidation, pending-ID handling, targeted node/bounds invalidation, and event translation in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs`
- [X] T037 [P] [US4] Add iOS generic property fallthrough invalidation and targeted layout/screen/announcement notifications in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T038 [P] [US4] Implement Android child add/remove/reorder, subtree events, and relation cleanup in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T039 [P] [US4] Implement iOS child add/remove/reorder, stable container updates, and relation cleanup in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T040 [US4] Audit and wire missing live property producers for AutomationId, ItemStatus, position/size, orientation, required/valid, FullDescription, LiveSetting, localized types, and relations in `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.cs`, `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.uno.cs`, and `src/Uno.UI/UI/Xaml/Automation/Peers/AutomationPeer.mux.cs`
- [X] T041 [US4] Complete shared event/property routing needed by both mobile adapters in `src/Uno.UI.Runtime.Skia/Accessibility/SkiaAccessibilityBase.cs`
- [X] T042 [US4] Coalesce redundant invalidations and preserve shared announcement throttling in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [ ] T043 [US4] Run US4 live-update tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T043**: Android property, structure, notification, and announcement tests pass
> in the native TalkBack-enabled run. iOS native execution remains deferred.

**Checkpoint**: Native state remains live without refocusing or full-tree rebuilds.

---

## Phase 7: User Story 5 - Navigate Rich Text, Collections, and Ranges (Priority: P2)

**Goal**: Expose complete text, collection, grid/table/tree, range, orientation, scroll,
hierarchy, relationship, annotation, and advanced provider semantics.

**Independent Test**: Exercise representative rich text, collection, range, and scroll
controls and inspect their native metadata/actions on both platforms.

### Tests for User Story 5

- [X] T044 [P] [US5] Add failing range, text, collection, hierarchy, and relation tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityRichControls.cs`
- [X] T045 [P] [US5] Extend existing ListView, Slider, TextBox, DataGrid, and ScrollViewer accessibility tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleListView.cs`, `Given_AccessibleSlider.cs`, `Given_AccessibleTextBox.cs`, `Given_AccessibleDataGrid.cs`, and `Given_AccessibleScrollViewer.cs`

### Implementation for User Story 5

- [X] T046 [P] [US5] Implement Android range/progress/orientation and set-progress semantics in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T047 [P] [US5] Implement Android list/grid/table/tree/tab/menu/radio collection and hierarchy metadata in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T048 [US5] Implement Android editable/read-only/multiline/password/text-selection and text-change semantics in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T049 [US5] Implement Android LabeledBy, DescribedBy, ControlledPeers, FlowsTo/From, annotations, culture, validation, level/position/size, and localized role fallbacks in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T050 [P] [US5] Implement iOS adjustable/range/progress/orientation and scroll semantics in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T051 [P] [US5] Implement iOS list/grid/table/tree/tab/menu/radio container, hierarchy, and selection metadata in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T052 [US5] Implement iOS editable/read-only/multiline/password/rich-text and supported selection semantics in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T053 [US5] Implement iOS relationships, annotations, culture, validation, level/position/size, landmarks, and localized custom content/rotors in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T054 [US5] Implement MultipleView/ChangeView, Transform/Transform2, Drag/DropTarget, Styles, Spreadsheet, Text2/TextChild/TextRange/TextEdit, and documented fallback behavior in `src/Uno.UI/Accessibility/AccessibilityPeerHelper.cs`, `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`, and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T055 [US5] Implement virtualized-item realization, pinning, metadata, and stale-node cleanup in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [ ] T056 [US5] Run US5 rich-control tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T056**: Android rich-control native metadata and action tests pass, including
> GridItem, range, text, relations, and localized role data. iOS native execution remains deferred.

**Checkpoint**: Rich/composite controls expose the capability-matrix semantics.

---

## Phase 8: User Story 6 - Automate Mobile Uno Applications Reliably (Priority: P2)

**Goal**: Make stable non-spoken AutomationId identity and representative actions available
to UIAutomator, XCUITest, Uno.UITest, and Appium-compatible clients.

**Independent Test**: Locate every fixture control by AutomationId, inspect it, perform
representative actions, and confirm secure values stay hidden.

### Tests for User Story 6

- [X] T057 [P] [US6] Add failing native AutomationId/name-separation and secure-value tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityAutomation.cs`
- [X] T058 [P] [US6] Add failing Android/iOS automation smoke tests in `src/SamplesApp/SamplesApp.UITests/Windows_UI_Xaml_Automation/MobileAccessibility_Tests.cs`

### Implementation for User Story 6

- [X] T059 [P] [US6] Finalize Android normalized resource/unique ID exposure and live identifier updates in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T060 [P] [US6] Finalize iOS `AccessibilityIdentifier` exposure and live identifier updates in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`
- [X] T061 [US6] Extend AutomationId, name, relationship, action, and secure-text fixtures in `src/SamplesApp/SamplesApp.Samples/Windows_UI.Xaml_Automation/AutomationProperties_AutomationId.xaml` and `src/SamplesApp/SamplesApp.Samples/Windows_UI.Xaml_Automation/AccessibilityScreenReaderPage.xaml`
- [ ] T062 [US6] Run native and SamplesApp automation tests through `src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj`

> **PARTIAL T062**: The direct Android UIAutomator SamplesApp suite passes 10 of 10 tests
> with TalkBack enabled. iOS XCUITest/Uno.UITest execution remains deferred.

**Checkpoint**: Mobile UI automation can locate and operate controls without abusing spoken names.

---

## Phase 9: User Story 7 - Preserve Performance and Lifecycle Correctness (Priority: P2)

**Goal**: Eliminate per-frame semantic work, stale IDs, strong-reference leaks, unsafe callbacks,
and full-tree rebuilds.

**Independent Test**: Profile a 500-node fixture and 1,000-item virtualized list through
repeated add/remove/window/focus cycles and verify timing and registry baselines.

### Tests for User Story 7

- [X] T063 [P] [US7] Add failing registry leak, stale-generation, window-disposal, and thread-affinity tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityLifecycle.cs`
- [X] T064 [P] [US7] Add 500-node update and 1,000-item virtualization performance tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityPerformance.cs`

### Implementation for User Story 7

- [X] T065 [P] [US7] Remove Android per-render root invalidation, retain explicit post-layout surface/orientation invalidation, and complete weak-map compaction in `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKCanvasView.cs`, `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs`, and `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`
- [X] T066 [P] [US7] Complete iOS weak ownership, stable element reuse, container diffing, and disposal cleanup in `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T067 [US7] Add bounded invalidation queues, UI-thread marshaling, and disposed-callback guards in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T068 [US7] Validate per-XamlRoot isolation and active-owner routing in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MultiWindowAccessibility.cs`
- [ ] T069 [US7] Run US7 lifecycle/performance tests through `build/test-scripts/android-run-skia-runtime-tests.sh` and `build/test-scripts/ios-uitest-run.sh`

> **PARTIAL T069**: Android lifecycle, registry, stale-node, 500-node, and 1,000-item
> virtualization tests pass in the native run. iOS native execution remains deferred.

**Checkpoint**: Mobile accessibility is incremental, leak-free, and lifecycle-safe.

---

## Phase 10: User Story 8 - Prove Parity on Both Mobile Platforms (Priority: P2)

**Goal**: Demonstrate complete capability-matrix coverage with automated native observables
and documented TalkBack/VoiceOver results.

**Independent Test**: Run the shared, Android, iOS, and SamplesApp suites against the same
fixture and complete the manual AT matrix with no unresolved P1 blocker.

### Tests for User Story 8

- [X] T070 [P] [US8] Add data-driven coverage tests for all 39 core properties, 34 patterns, pattern-state groups, relations, 30 events, and notifications in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityCapabilityMatrix.cs`
- [X] T071 [P] [US8] Add capability fallback and unsupported-semantic diagnostics tests in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_MobileAccessibilityCapabilityMatrix.cs`

### Implementation for User Story 8

- [X] T072 [US8] Close every remaining direct/derived/custom mapping or explicit fallback in `src/Uno.UI.Runtime.Skia.Android/Accessibility/UnoExploreByTouchHelper.cs`, `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs`, `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/UnoUIAccessibilityElement.cs`, and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [X] T073 [US8] Add redaction-safe trace logging for node lifecycle, peer resolution, actions, focus, invalidation, events, and stale-node rejection in `src/Uno.UI.Runtime.Skia.Android/Accessibility/AndroidSkiaAccessibility.cs` and `src/Uno.UI.Runtime.Skia.AppleUIKit/Accessibility/AppleUIKitAccessibility.cs`
- [ ] T074 [US8] Execute and record the TalkBack, VoiceOver, UIAutomator, XCUITest, and Appium-compatible matrix in `specs/005-mobile-a11y-automation/quickstart.md`
- [ ] T075 [US8] Run all mobile accessibility runtime and SamplesApp tests using `build/test-scripts/android-run-skia-runtime-tests.sh`, `build/test-scripts/ios-uitest-run.sh`, and `src/SamplesApp/SamplesApp.UITests/SamplesApp.UITests.csproj`

> **PARTIAL T074-T075**: Android native runtime and direct UIAutomator execution are complete
> with TalkBack enabled. VoiceOver, XCUITest/Appium-compatible iOS execution, and the manual
> cross-platform assistive-technology matrix still require macOS/Xcode and representative devices.

**Checkpoint**: The complete mobile parity claim is backed by automated and manual evidence.

---

## Phase 11: Polish and Cross-Cutting Validation

**Purpose**: Final cleanup, documentation alignment, builds, review, and task completion.

- [X] T076 [P] Update implementation notes and any intentional platform deviations in `specs/005-mobile-a11y-automation/plan.md` and `contracts/capability-matrix.md`
- [X] T077 [P] Format any modified SamplesApp XAML with `src/SamplesApp/Settings.XamlStyler`
- [X] T078 Build Android packages with `src/Uno.UI-netcoremobile-only.slnf` using `net10.0-android`
- [X] T079 Build iOS packages with `src/Uno.UI-netcoremobile-only.slnf` using `net10.0-ios`
- [X] T080 Run the full affected shared accessibility suite from `src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj`

> **EVIDENCE T078-T079**: The Android solution filter builds with
> `-p:UnoTargetFrameworkOverride=net10.0-android -p:NetPrevious=net10.0`. The iOS-compatible
> solution subset builds with `net10.0-ios` after excluding the Android-only
> `Uno.UI.GooglePlay.netcoremobile` and `Uno.UI.BindingHelper.Android.netcoremobile` projects.
> AppleUIKit also builds independently for `net9.0-ios18.0` / `iossimulator-x64`.
- [X] T081 Run architecture, contract, performance, operability, quality, security, and skeptic reviews over the complete feature diff
- [ ] T082 Confirm every task is marked complete in `specs/005-mobile-a11y-automation/tasks.md` and every success criterion in `specs/005-mobile-a11y-automation/spec.md` has evidence

> **EVIDENCE T080-T081**: The final `Windows_UI_Xaml_Automation` Skia Desktop runtime
> run completed with 209 passed, 0 failed, and 252 mobile/platform skips. The capability
> matrix contributed 25 passed and 4 mobile skips. Generic Skia, Android `net10.0-android`,
> AppleUIKit `net9.0-ios18.0`/`iossimulator-x64`, Uno.UI.UnitTests, and
> SamplesApp.UITests builds succeeded. Android API 36 completed 328 native automation tests
> with 0 failures and 133 platform skips while TalkBack was active; the direct UIAutomator
> suite completed 10 of 10 tests. The final multi-lens review findings were remediated,
> including modal announcement revalidation, ownerless EventsSource routing, non-light-dismiss
> popup detection, cached native-node rejection, active-owner lifecycle, and tri-state identity.
>
> **BLOCKED T082**: Native iOS/VoiceOver/XCUITest and the manual cross-platform matrix
> remain unavailable locally. Final review also retains follow-up work for peer-generation
> identity across recycled containers, non-item ownerless peer projection, and native text
> selection/granularity actions. Completion cannot be claimed until those items and the
> required iOS evidence are resolved or explicitly accepted as deferred.

---

## Dependencies and Execution Order

### Phase dependencies

- **Phase 1 Setup**: no dependencies.
- **Phase 2 Foundational**: depends on Phase 1 and blocks all user stories.
- **US1-US4 (P1)**: execute in priority order because each builds on the same native adapter files:
  US1 tree -> US2 actions -> US3 focus -> US4 live updates.
- **US5-US8 (P2)**: depend on US1-US4. Android and iOS tasks within each story may run in parallel.
- **Polish**: depends on all selected stories.

### User story dependencies

- **US1**: Foundational only.
- **US2**: US1 native nodes and identity.
- **US3**: US1 identity/tree plus US2 action routing.
- **US4**: US1 tree/identity and US3 focus/lifecycle.
- **US5**: US2 action executor and US4 live invalidation.
- **US6**: US1 identity and US2 representative actions.
- **US7**: US1 registries and US4 invalidation/event lifecycle.
- **US8**: US1-US7 complete.

### Parallel opportunities

- T003 and T004.
- T008 and T009.
- Android T010-T013 and iOS T014-T016 after T005-T006.
- T020 and T021; T023-T025 after T022.
- T030 and T031.
- Android T036/T038 and iOS T037/T039.
- Android T046-T049 and iOS T050-T053 after shared tests.
- T057 and T058; T059 and T060.
- T063 and T064; T065 and T066.
- T070 and T071.
- T076 and T077.

## Parallel examples

### US1

```text
Android: T008 -> T010 -> T011 -> T012 -> T013
iOS:     T009 -> T014 -> T015 -> T016
```

### US4

```text
Android: T036 + T038
iOS:     T037 + T039
Shared producer work: T040 -> T041 -> T042
```

### US5

```text
Android: T046 -> T047 -> T048 -> T049
iOS:     T050 -> T051 -> T052 -> T053
Shared advanced patterns: T054
```

## Implementation strategy

### MVP first

1. Complete Setup and Foundational.
2. Complete US1.
3. Stop and validate the native Android and iOS trees independently.
4. Continue P1 actions, focus, and live events only after tree identity is stable.

### Incremental delivery

1. US1: discoverable native trees.
2. US2: operable controls.
3. US3: synchronized focus/modals.
4. US4: live state/events.
5. US5: rich controls.
6. US6: automation identity.
7. US7: performance/lifecycle hardening.
8. US8: parity proof.

## Notes

- Tests must fail before their paired implementation.
- `[P]` tasks use different files and have no incomplete shared-file dependency.
- Mark each completed task `[X]` immediately.
- Do not edit generated files.
- Keep native Android/UIKit renderers compiling but do not expand their behavior.
- Do not add a new Appium project or a cached mobile semantic snapshot.
