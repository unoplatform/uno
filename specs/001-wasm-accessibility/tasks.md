# Tasks: WebAssembly Skia Accessibility Enhancement

**Input**: Design documents from `/specs/001-wasm-accessibility/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/accessibility-api.ts

**Tests**: Runtime tests included as they are standard for Uno Platform UI features (see AGENTS.md runtime tests guidance).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Core accessibility**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/`
- **TypeScript**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/`
- **Runtime tests**: `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project structure and foundational classes that all patterns depend on

- [X] T001 Create AriaMapper.cs skeleton with control type to ARIA role mapping in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AriaMapper.cs
- [X] T002 [P] Create SemanticElementFactory.cs skeleton with SemanticElementType enum in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs
- [X] T003 [P] Create AccessibilityDebugger.cs skeleton for debug overlay mode in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AccessibilityDebugger.cs
- [X] T004 [P] Create SemanticElements.ts skeleton with element factory interfaces in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/SemanticElements.ts
- [X] T005 Add JSImport method declarations for new element types in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/NativeMethods.cs
- [X] T006 Add JSExport callback declarations for pattern events in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Critical**: No user story work can begin until this phase is complete

- [X] T007 Implement AriaMapper.GetAriaAttributes() to extract ARIA attributes from automation peer in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AriaMapper.cs
- [X] T008 Implement AriaMapper.GetSemanticElementType() to determine HTML element type from control type in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AriaMapper.cs
- [X] T009 [P] Implement AriaMapper.GetPatternCapabilities() to detect supported patterns in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AriaMapper.cs
- [X] T010 Implement SemanticElementFactory.CreateElement() dispatch to type-specific factories in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/SemanticElementFactory.cs
- [X] T011 [P] Implement debounce timer infrastructure (100ms) for DOM updates in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs
- [X] T012 [P] Implement AccessibilityDebugger.EnableDebugMode() with outline toggle in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/AccessibilityDebugger.cs
- [X] T013 Implement TypeScript setup() to initialize semantic root with live regions in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts
- [X] T014 [P] Implement TypeScript enableDebugMode() to toggle visible outlines in src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/Accessibility.ts

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Screen Reader Button Activation (Priority: P1)

**Goal**: Enable screen reader users to activate buttons via keyboard (Enter/Space)

**Independent Test**: Tab to any button and press Enter/Space - the button's click handler should execute

### Runtime Tests for User Story 1

- [X] T015 [P] [US1] Create Given_AccessibleButton.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleButton.cs
- [X] T016 [P] [US1] Add test When_Button_Is_Focusable_Then_Has_Tabindex in Given_AccessibleButton.cs
- [X] T017 [P] [US1] Add test When_Button_Is_Invoked_Then_Click_Handler_Fires in Given_AccessibleButton.cs
- [X] T018 [P] [US1] Add test When_Button_Is_Disabled_Then_AriaDisabled_Is_True in Given_AccessibleButton.cs

### Implementation for User Story 1

- [X] T019 [US1] Implement createButtonElement() in SemanticElements.ts to create native button element
- [X] T020 [US1] Add click/keydown handlers to button element for Enter/Space in SemanticElements.ts
- [X] T021 [US1] Implement OnInvoke JSExport callback to call IInvokeProvider.Invoke() in WebAssemblyAccessibility.cs
- [X] T022 [US1] Wire up button element creation in AddSemanticElement when control type is Button in WebAssemblyAccessibility.cs
- [X] T023 [US1] Implement aria-disabled sync for IsEnabled state changes in WebAssemblyAccessibility.cs
- [X] T024 [US1] Add updateDisabledState() JSImport/TypeScript method in Accessibility.ts

**Checkpoint**: Screen reader users can tab to and activate buttons

---

## Phase 4: User Story 2 - Slider Value Adjustment (Priority: P1)

**Goal**: Enable keyboard-only slider adjustment with value announcements

**Independent Test**: Tab to slider, use arrow keys - value changes and new value is announced

### Runtime Tests for User Story 2

- [X] T025 [P] [US2] Create Given_AccessibleSlider.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleSlider.cs
- [X] T026 [P] [US2] Add test When_Slider_Focused_Then_Value_MinMax_Exposed in Given_AccessibleSlider.cs
- [X] T027 [P] [US2] Add test When_ArrowKey_Pressed_Then_Value_Changes in Given_AccessibleSlider.cs
- [X] T028 [P] [US2] Add test When_Value_Changes_Then_AriaValueNow_Updates in Given_AccessibleSlider.cs

### Implementation for User Story 2

- [X] T029 [US2] Implement createSliderElement() in SemanticElements.ts to create input[type=range]
- [X] T030 [US2] Add input event handler to slider for value changes in SemanticElements.ts
- [X] T031 [US2] Implement OnRangeValueChange JSExport to call IRangeValueProvider.SetValue() in WebAssemblyAccessibility.cs
- [X] T032 [US2] Implement updateSliderValue() JSImport/TypeScript for bidirectional sync in Accessibility.ts
- [X] T033 [US2] Wire up slider element creation for AutomationControlType.Slider in WebAssemblyAccessibility.cs
- [X] T034 [US2] Handle RangeBaseAutomationPeer property changes (Value, Minimum, Maximum) in WebAssemblyAccessibility.cs

**Checkpoint**: Keyboard users can adjust sliders with value announcements

---

## Phase 5: User Story 3 - Checkbox Toggle (Priority: P1)

**Goal**: Enable screen reader users to toggle checkboxes with state announcements

**Independent Test**: Tab to checkbox and press Space - checkbox toggles and state is announced

### Runtime Tests for User Story 3

- [X] T035 [P] [US3] Create Given_AccessibleCheckBox.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleCheckBox.cs
- [X] T036 [P] [US3] Add test When_Checkbox_Focused_Then_Checked_State_Exposed in Given_AccessibleCheckBox.cs
- [X] T037 [P] [US3] Add test When_Space_Pressed_Then_Checkbox_Toggles in Given_AccessibleCheckBox.cs
- [X] T038 [P] [US3] Add test When_TriState_Then_AriaChecked_IsMixed in Given_AccessibleCheckBox.cs

### Implementation for User Story 3

- [X] T039 [US3] Implement createCheckboxElement() in SemanticElements.ts to create input[type=checkbox]
- [X] T040 [US3] Add change event handler to checkbox for toggle events in SemanticElements.ts
- [X] T041 [US3] Implement OnToggle JSExport to call IToggleProvider.Toggle() in WebAssemblyAccessibility.cs
- [X] T042 [US3] Wire up checkbox element creation for AutomationControlType.CheckBox in WebAssemblyAccessibility.cs
- [X] T043 [US3] Handle TogglePatternIdentifiers.ToggleStateProperty changes (true/false/mixed) in WebAssemblyAccessibility.cs
- [X] T044 [P] [US3] Implement createRadioElement() for RadioButton (similar pattern) in SemanticElements.ts

**Checkpoint**: Screen reader users can toggle checkboxes and radio buttons

---

## Phase 6: User Story 4 - Text Input (Priority: P2)

**Goal**: Enable screen reader users to enter and edit text in TextBox controls

**Independent Test**: Tab to textbox and type - text appears and is readable by screen readers

### Runtime Tests for User Story 4

- [X] T045 [P] [US4] Create Given_AccessibleTextBox.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleTextBox.cs
- [X] T046 [P] [US4] Add test When_TextBox_Focused_Then_Value_Exposed in Given_AccessibleTextBox.cs
- [X] T047 [P] [US4] Add test When_Text_Entered_Then_Value_Syncs in Given_AccessibleTextBox.cs
- [X] T048 [P] [US4] Add test When_PasswordBox_Then_Input_Type_Is_Password in Given_AccessibleTextBox.cs

### Implementation for User Story 4

- [X] T049 [US4] Implement createTextBoxElement() in SemanticElements.ts for input[type=text] or textarea
- [X] T050 [US4] Add input event handler for text changes with selection support in SemanticElements.ts
- [X] T051 [US4] Implement OnTextInput JSExport to call IValueProvider.SetValue() in WebAssemblyAccessibility.cs
- [X] T052 [US4] Implement updateTextBoxValue() JSImport/TypeScript for bidirectional sync in Accessibility.ts
- [X] T053 [US4] Wire up textbox element creation for AutomationControlType.Edit in WebAssemblyAccessibility.cs
- [X] T054 [US4] Handle password masking for PasswordBox (input[type=password]) in SemanticElements.ts
- [X] T055 [US4] Handle IME composition events for international text input in SemanticElements.ts

**Checkpoint**: Screen reader users can enter and edit text in forms

---

## Phase 7: User Story 5 - ComboBox Selection (Priority: P2)

**Goal**: Enable screen reader users to open ComboBox dropdowns and select items

**Independent Test**: Tab to combobox, press Enter to open, use arrows to navigate, Enter to select

### Runtime Tests for User Story 5

- [X] T056 [P] [US5] Create Given_AccessibleComboBox.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleComboBox.cs
- [X] T057 [P] [US5] Add test When_ComboBox_Closed_Then_AriaExpanded_IsFalse in Given_AccessibleComboBox.cs
- [X] T058 [P] [US5] Add test When_Enter_Pressed_Then_ComboBox_Opens in Given_AccessibleComboBox.cs
- [X] T059 [P] [US5] Add test When_Item_Selected_Then_Selection_Announced in Given_AccessibleComboBox.cs

### Implementation for User Story 5

- [X] T060 [US5] Implement createComboBoxElement() in SemanticElements.ts with role=combobox
- [X] T061 [US5] Add keydown handlers for Enter/Alt+Down to expand in SemanticElements.ts
- [X] T062 [US5] Implement OnExpandCollapse JSExport to call IExpandCollapseProvider.Expand/Collapse() in WebAssemblyAccessibility.cs
- [X] T063 [US5] Implement updateExpandCollapseState() JSImport/TypeScript for aria-expanded sync in Accessibility.ts
- [X] T064 [US5] Wire up combobox element creation for AutomationControlType.ComboBox in WebAssemblyAccessibility.cs
- [X] T065 [US5] Handle ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty changes in WebAssemblyAccessibility.cs

**Checkpoint**: Screen reader users can operate comboboxes

---

## Phase 8: User Story 6 - List Navigation (Priority: P2)

**Goal**: Enable screen reader users to navigate through ListView/ListBox with position announcements

**Independent Test**: Tab to list, use arrow keys to navigate items - position is announced

### Runtime Tests for User Story 6

- [X] T066 [P] [US6] Create Given_AccessibleListView.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibleListView.cs
- [X] T067 [P] [US6] Add test When_List_Focused_Then_ItemCount_Announced in Given_AccessibleListView.cs
- [X] T068 [P] [US6] Add test When_Arrow_Pressed_Then_Position_Announced in Given_AccessibleListView.cs
- [X] T069 [P] [US6] Add test When_Space_Pressed_Then_Item_Selected in Given_AccessibleListView.cs

### Implementation for User Story 6

- [X] T070 [US6] Implement createListBoxElement() in SemanticElements.ts with role=listbox
- [X] T071 [US6] Implement createListItemElement() in SemanticElements.ts with role=option
- [X] T072 [US6] Add aria-posinset and aria-setsize attributes to list items in SemanticElements.ts
- [X] T073 [US6] Implement OnSelection JSExport to call ISelectionItemProvider.Select() in WebAssemblyAccessibility.cs
- [X] T074 [US6] Implement updateSelectionState() JSImport/TypeScript for aria-selected sync in Accessibility.ts
- [X] T075 [US6] Wire up list element creation for AutomationControlType.List/ListItem in WebAssemblyAccessibility.cs
- [X] T076 [US6] Handle SelectionPatternIdentifiers property changes in WebAssemblyAccessibility.cs
- [ ] T077 [US6] Integrate EffectiveViewport for virtualized list semantic element lifecycle in WebAssemblyAccessibility.cs

**Checkpoint**: Screen reader users can navigate and select items in lists

---

## Phase 9: User Story 7 - Live Announcements (Priority: P3)

**Goal**: Enable dynamic content changes to be announced without focus changes

**Independent Test**: Trigger a notification - screen reader announces the message

### Runtime Tests for User Story 7

- [X] T078 [P] [US7] Create Given_AccessibilityAnnouncements.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibilityAnnouncements.cs
- [X] T079 [P] [US7] Add test When_Polite_Announcement_Then_LiveRegion_Updates in Given_AccessibilityAnnouncements.cs
- [X] T080 [P] [US7] Add test When_Assertive_Announcement_Then_LiveRegion_Updates_Immediately in Given_AccessibilityAnnouncements.cs

### Implementation for User Story 7

- [X] T081 [US7] Implement announcePolite() JSImport/TypeScript for aria-live="polite" in Accessibility.ts
- [X] T082 [US7] Implement announceAssertive() JSImport/TypeScript for aria-live="assertive" in Accessibility.ts
- [ ] T083 [US7] Wire up AutomationPeer.RaiseAutomationEvent for LiveRegionChanged in WebAssemblyAccessibility.cs
- [X] T084 [US7] Expose public API for custom announcements in WebAssemblyAccessibility.cs

**Checkpoint**: Dynamic content changes are announced to screen readers

---

## Phase 10: User Story 8 - Focus Management (Priority: P3)

**Goal**: Synchronize focus between visual canvas layer and semantic accessibility layer

**Independent Test**: Tab through application - focus indicators match focused elements

### Runtime Tests for User Story 8

- [X] T085 [P] [US8] Create Given_AccessibilityFocus.cs test fixture in src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AccessibilityFocus.cs
- [X] T086 [P] [US8] Add test When_Tab_Pressed_Then_Focus_Moves_Sequentially in Given_AccessibilityFocus.cs
- [X] T087 [P] [US8] Add test When_Dialog_Opens_Then_Focus_Moves_To_Dialog in Given_AccessibilityFocus.cs
- [X] T088 [P] [US8] Add test When_Element_Disabled_With_Focus_Then_Focus_Moves_Away in Given_AccessibilityFocus.cs

### Implementation for User Story 8

- [X] T089 [US8] Implement OnFocus JSExport to sync focus from semantic to Uno element in WebAssemblyAccessibility.cs
- [X] T090 [US8] Implement OnBlur JSExport to handle focus leaving semantic element in WebAssemblyAccessibility.cs
- [X] T091 [US8] Implement focusSemanticElement() JSImport/TypeScript to programmatically focus in Accessibility.ts
- [ ] T092 [US8] Wire up FocusManager.GotFocus to sync focus to semantic layer in WebAssemblyAccessibility.cs
- [ ] T093 [US8] Handle focus trap for modal dialogs (Tab wrapping) in Accessibility.ts
- [ ] T094 [US8] Handle focus recovery when focused element becomes disabled in WebAssemblyAccessibility.cs

**Checkpoint**: Focus is consistently synchronized between visual and semantic layers

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T095 [P] Add logging infrastructure for accessibility operations (FR-033) in WebAssemblyAccessibility.cs
- [X] T096 [P] Implement debug mode visible overlay rendering (FR-034) in AccessibilityDebugger.cs
- [ ] T097 [P] Add performance profiling for semantic DOM update cycle in WebAssemblyAccessibility.cs
- [ ] T098 Code review and cleanup across all accessibility files
- [ ] T099 Verify WCAG 2.1 Level AA compliance for all supported patterns
- [ ] T100 Run quickstart.md validation with NVDA and VoiceOver

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-10)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 11)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (Button)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (Slider)**: Can start after Foundational - No dependencies on other stories
- **User Story 3 (Checkbox)**: Can start after Foundational - No dependencies on other stories
- **User Story 4 (TextBox)**: Can start after Foundational - No dependencies on other stories
- **User Story 5 (ComboBox)**: Can start after Foundational - Uses expand/collapse pattern
- **User Story 6 (ListView)**: Can start after Foundational - May reuse selection pattern from US3
- **User Story 7 (Live)**: Can start after Foundational - Independent pattern
- **User Story 8 (Focus)**: Can start after Foundational - May improve other stories but independent

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- TypeScript element factory before C# wiring
- JSExport callbacks before property change handlers
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks (T001-T006) after T001 can run in parallel
- Foundational tasks T009, T011, T012, T014 can run in parallel
- Once Foundational phase completes, all P1 user stories (US1, US2, US3) can start in parallel
- All tests within a user story can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: P1 User Stories

```bash
# After Foundational phase completes, launch all P1 stories in parallel:

# User Story 1 (Button):
Task: "Create Given_AccessibleButton.cs test fixture"
Task: "Implement createButtonElement() in SemanticElements.ts"

# User Story 2 (Slider):
Task: "Create Given_AccessibleSlider.cs test fixture"
Task: "Implement createSliderElement() in SemanticElements.ts"

# User Story 3 (Checkbox):
Task: "Create Given_AccessibleCheckBox.cs test fixture"
Task: "Implement createCheckboxElement() in SemanticElements.ts"
```

---

## Implementation Strategy

### MVP First (P1 User Stories Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3-5: User Stories 1, 2, 3 (Button, Slider, Checkbox)
4. **STOP and VALIDATE**: Test with NVDA/VoiceOver
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 (Button) → Test independently → Basic click support
3. Add User Story 2 (Slider) → Test independently → Range value support
4. Add User Story 3 (Checkbox) → Test independently → Toggle support
5. Add User Story 4-6 (TextBox, ComboBox, ListView) → P2 patterns
6. Add User Story 7-8 (Live, Focus) → P3 polish
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Button) + User Story 4 (TextBox)
   - Developer B: User Story 2 (Slider) + User Story 5 (ComboBox)
   - Developer C: User Story 3 (Checkbox) + User Story 6 (ListView)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- TypeScript compiles to embedded JS resource - rebuild after TS changes
- Use SamplesApp for manual screen reader testing
