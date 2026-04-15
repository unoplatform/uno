# Feature Specification: WebAssembly Skia Accessibility Enhancement

**Feature Branch**: `001-wasm-accessibility`
**Created**: 2026-02-11
**Status**: Draft
**Input**: Improve Accessibility support in Skia WebAssembly by mapping automation peer patterns to ARIA attributes and enabling user interaction through hidden HTML input elements.

## Clarifications

### Session 2026-02-11

- Q: What is the DOM update frequency strategy for rapid visual tree changes? → A: Debounce semantic DOM updates by 100ms after the last visual tree change
- Q: How should list virtualization control semantic element creation? → A: Use EffectiveViewport to create semantic elements only for visible items
- Q: What observability/debugging support should be provided? → A: Both trace-level logging for accessibility operations and a visual debug mode that renders semantic elements visibly

## Overview

Uno Platform applications running on Skia WebAssembly currently have limited accessibility support. While a basic semantic DOM tree exists alongside the canvas rendering, users relying on assistive technologies (screen readers, keyboard-only navigation) cannot fully interact with the application. This feature enhances accessibility by bridging the gap between Uno's automation peer system and web ARIA standards, enabling full keyboard and screen reader interaction.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Screen Reader Button Activation (Priority: P1)

A screen reader user navigates to a button in a Uno WebAssembly application and activates it using keyboard commands.

**Why this priority**: Buttons are the most fundamental interactive element. Without button activation, users cannot perform any actions in the application.

**Independent Test**: Can be fully tested by tabbing to any button and pressing Enter/Space. Delivers core click functionality for screen reader users.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a Button control, **When** a screen reader user tabs to the button, **Then** the screen reader announces the button's accessible name and role.
2. **Given** focus is on a button, **When** the user presses Enter or Space, **Then** the button's click handler executes.
3. **Given** a disabled button, **When** a screen reader user encounters it, **Then** the screen reader announces it as disabled and the button cannot be activated.

---

### User Story 2 - Slider Value Adjustment (Priority: P1)

A keyboard-only user adjusts a Slider control's value using arrow keys while hearing the current value announced.

**Why this priority**: Sliders require both value announcement and keyboard interaction - a critical pattern for range controls that currently has no support.

**Independent Test**: Can be fully tested by tabbing to a slider and using arrow keys. Delivers volume/brightness/progress control for keyboard users.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a Slider control, **When** a user tabs to the slider, **Then** the screen reader announces the current value, minimum, and maximum.
2. **Given** focus is on a slider, **When** the user presses arrow keys, **Then** the slider value changes and the new value is announced.
3. **Given** a slider with custom step values, **When** the user presses Page Up/Page Down, **Then** the value changes by the large change amount.

---

### User Story 3 - Checkbox Toggle (Priority: P1)

A screen reader user toggles a CheckBox control using the keyboard and hears the checked state change announced.

**Why this priority**: Form controls are essential for data entry. Checkboxes are among the most common form elements.

**Independent Test**: Can be fully tested by tabbing to a checkbox and pressing Space. Delivers form completion capability.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a CheckBox, **When** a screen reader user tabs to it, **Then** the screen reader announces the label and current checked state.
2. **Given** focus is on an unchecked checkbox, **When** the user presses Space, **Then** the checkbox becomes checked and the screen reader announces "checked".
3. **Given** a tri-state checkbox in indeterminate state, **When** toggled, **Then** the screen reader announces the appropriate state (checked, unchecked, or mixed).

---

### User Story 4 - Text Input (Priority: P2)

A screen reader user enters and edits text in a TextBox control with full keyboard support.

**Why this priority**: Text entry is essential for forms but more complex than basic controls due to selection, IME, and bidirectional sync requirements.

**Independent Test**: Can be fully tested by tabbing to a textbox and typing. Delivers form text entry capability.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a TextBox, **When** a screen reader user tabs to it, **Then** the screen reader announces the label and current content.
2. **Given** focus is on a textbox, **When** the user types characters, **Then** the text appears in the control and is readable by screen readers.
3. **Given** a password field, **When** the user types, **Then** the characters are masked and the screen reader does not announce the actual characters.

---

### User Story 5 - ComboBox Selection (Priority: P2)

A screen reader user opens a ComboBox dropdown and selects an item using keyboard navigation.

**Why this priority**: ComboBox involves expand/collapse pattern plus selection - a compound interaction pattern.

**Independent Test**: Can be fully tested by tabbing to a combobox, pressing Enter to open, using arrows to navigate, and Enter to select.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a ComboBox, **When** a screen reader user tabs to it, **Then** the screen reader announces the current selection and that it's a combobox.
2. **Given** focus is on a closed combobox, **When** the user presses Enter or Alt+Down, **Then** the dropdown opens and the screen reader announces "expanded".
3. **Given** an open combobox, **When** the user presses arrow keys, **Then** items are announced as they receive focus.
4. **Given** an open combobox with an item focused, **When** the user presses Enter, **Then** the item is selected, the dropdown closes, and the selection is announced.

---

### User Story 6 - List Navigation (Priority: P2)

A screen reader user navigates through a ListView or ListBox, hearing item positions and selecting items.

**Why this priority**: Lists are fundamental for displaying collections. Selection and position announcement are critical for orientation.

**Independent Test**: Can be fully tested by tabbing to a list and using arrow keys to navigate items.

**Acceptance Scenarios**:

1. **Given** a Uno WebAssembly app with a ListView, **When** a screen reader user enters the list, **Then** the screen reader announces "list with N items".
2. **Given** focus is in a list, **When** the user presses arrow keys, **Then** items are announced with their position (e.g., "Item 3 of 10").
3. **Given** a multi-select list, **When** the user presses Space on an item, **Then** the item's selection state toggles and is announced.

---

### User Story 7 - Live Announcements (Priority: P3)

Dynamic content changes are announced to screen reader users without requiring focus changes.

**Why this priority**: Important for notifications, errors, and status updates but less critical than interactive controls.

**Independent Test**: Can be tested by triggering a notification and verifying screen reader announcement.

**Acceptance Scenarios**:

1. **Given** an application shows a toast notification, **When** the notification appears, **Then** the screen reader announces the message.
2. **Given** a form with validation, **When** validation fails, **Then** the error message is announced via assertive live region.
3. **Given** a progress indicator updates, **When** the progress changes significantly, **Then** the new status is announced politely.

---

### User Story 8 - Focus Management (Priority: P3)

Focus moves correctly between the visual canvas layer and the semantic accessibility layer, maintaining consistency.

**Why this priority**: Foundation for all other interactions but can be iteratively improved alongside other features.

**Independent Test**: Can be tested by tabbing through an application and verifying focus indicators match focused elements.

**Acceptance Scenarios**:

1. **Given** a user tabs through an application, **When** focus moves to a control, **Then** both the visual focus indicator and semantic focus are synchronized.
2. **Given** a dialog opens programmatically, **When** it appears, **Then** focus moves to the dialog's first focusable element.
3. **Given** focus is in a modal dialog, **When** the user presses Tab at the last element, **Then** focus wraps to the first element (focus trap).

---

### Edge Cases

- What happens when a control becomes disabled while it has focus? (Focus should move to next focusable element)
- How does the system handle controls that are scrolled out of view? (Semantic elements should be hidden from accessibility tree)
- What happens when the visual tree changes rapidly? (Debounce semantic DOM updates by 100ms after the last change to prevent screen reader spam)
- How are very long lists handled? (Use EffectiveViewport to create semantic elements only for visible items, leveraging built-in virtualization)
- What happens during IME composition in text fields? (Composition state should be preserved, final text committed)

## Requirements *(mandatory)*

### Functional Requirements

**Core Infrastructure**
- **FR-001**: System MUST map automation peer control types to appropriate ARIA roles (button, checkbox, slider, textbox, combobox, listbox, option, etc.)
- **FR-002**: System MUST extract accessible names from automation peers and apply them as ARIA labels
- **FR-003**: System MUST route DOM events from semantic elements to corresponding automation peer actions

**Button/Invoke Pattern**
- **FR-004**: System MUST create focusable button elements for controls implementing IInvokeProvider
- **FR-005**: System MUST invoke the automation peer's Invoke action when the button is activated (click, Enter, Space)

**Toggle Pattern**
- **FR-006**: System MUST sync aria-checked attribute with IToggleProvider state (true, false, mixed)
- **FR-007**: System MUST call IToggleProvider.Toggle() when checkbox/radio elements change
- **FR-008**: System MUST distinguish between checkbox and radio roles based on control type

**Range Value Pattern**
- **FR-009**: System MUST expose aria-valuenow, aria-valuemin, aria-valuemax for controls implementing IRangeValueProvider
- **FR-010**: System MUST call IRangeValueProvider.SetValue() when range input value changes
- **FR-011**: System MUST handle both horizontal and vertical slider orientations

**Value Pattern**
- **FR-012**: System MUST create text input elements for controls implementing IValueProvider
- **FR-013**: System MUST sync text content bidirectionally between semantic input and Uno control
- **FR-014**: System MUST handle password masking for PasswordBox controls
- **FR-015**: System MUST support IME composition for international text input

**ExpandCollapse Pattern**
- **FR-016**: System MUST expose aria-expanded attribute for controls implementing IExpandCollapseProvider
- **FR-017**: System MUST call Expand()/Collapse() methods when expand/collapse state changes

**Selection Pattern**
- **FR-018**: System MUST expose aria-selected on items implementing ISelectionItemProvider
- **FR-019**: System MUST expose aria-multiselectable on containers implementing ISelectionProvider
- **FR-020**: System MUST call Select()/AddToSelection()/RemoveFromSelection() on item activation

**Additional ARIA Attributes**
- **FR-021**: System MUST expose aria-disabled based on IsEnabled() state
- **FR-022**: System MUST expose aria-required for form controls marked as required
- **FR-023**: System MUST expose position-in-set and size-of-set for list items
- **FR-024**: System MUST support heading levels via aria-level
- **FR-025**: System MUST support landmark roles based on automation peer landmark type

**Focus Management**
- **FR-026**: System MUST synchronize focus between Uno elements and semantic elements
- **FR-027**: System MUST prevent focus loops between visual and semantic layers
- **FR-028**: System MUST support standard keyboard navigation (Tab, Shift+Tab, arrow keys)

**Live Regions**
- **FR-029**: System MUST support polite announcements via aria-live="polite"
- **FR-030**: System MUST support assertive announcements via aria-live="assertive"

**Performance**
- **FR-031**: System MUST debounce semantic DOM updates by 100ms after the last visual tree change
- **FR-032**: System MUST use EffectiveViewport to create semantic elements only for visible items in virtualized lists

**Observability**
- **FR-033**: System MUST provide trace-level logging for accessibility operations (element creation, ARIA updates, event routing) that can be enabled via Uno's logging configuration
- **FR-034**: System MUST provide a visual debug mode that renders semantic elements with visible outlines/borders for debugging accessibility tree structure

### Key Entities

- **Semantic Element**: A DOM element in the accessibility layer that represents a Uno UIElement. Contains ARIA attributes and event handlers.
- **Automation Peer**: The Uno Platform class that exposes accessibility information for a control (name, role, patterns, state).
- **Pattern Provider**: An interface (IInvokeProvider, IToggleProvider, etc.) that enables specific accessibility interactions.
- **ARIA Attributes**: Web accessibility attributes (role, aria-label, aria-checked, etc.) that communicate state to assistive technologies.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can tab through all focusable controls in a Uno WebAssembly application using keyboard only
- **SC-002**: Screen reader users can identify 100% of interactive controls by their accessible name and role
- **SC-003**: Button controls can be activated via keyboard (Enter/Space) by screen reader users
- **SC-004**: Slider controls can be adjusted via keyboard (arrow keys) with value announcements
- **SC-005**: Form controls (checkbox, textbox, combobox) can be operated entirely via keyboard
- **SC-006**: List controls announce item count and current position during navigation
- **SC-007**: Dynamic content changes are announced to screen readers within 500ms of occurrence
- **SC-008**: Application passes WCAG 2.1 Level AA automated testing for the supported control patterns
- **SC-009**: Focus indicator remains visible and synchronized with semantic focus at all times
- **SC-010**: 90% of common user tasks can be completed using keyboard-only navigation

## Assumptions

- The existing automation peer infrastructure in Uno Platform provides accurate accessibility information (names, roles, patterns)
- Screen readers (NVDA, VoiceOver, JAWS) correctly interpret standard ARIA attributes
- The semantic DOM overlay approach (transparent elements over canvas) does not significantly impact rendering performance
- Users have screen readers or keyboard navigation tools that support modern web standards
- The IAutomationPeerListener interface will continue to notify of property changes

## Out of Scope

- Native mobile accessibility (Android TalkBack, iOS VoiceOver) - this feature focuses on WebAssembly browser
- Automated accessibility testing tooling - manual testing with screen readers is the primary verification method
- Accessibility for custom/third-party controls without proper automation peers
- High contrast mode and visual accessibility features (separate from screen reader/keyboard support)
- RTL (right-to-left) text layout accessibility considerations
