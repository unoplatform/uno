# Feature Specification: WASM Accessibility — Advanced Features

**Feature Branch**: `002-wasm-a11y-advanced`
**Created**: 2026-02-11
**Status**: Draft
**Input**: Advanced accessibility features deferred from 001-wasm-accessibility: virtualized list support, live region events, advanced focus management, performance profiling, and WCAG compliance verification.
**Depends On**: `001-wasm-accessibility` (base semantic DOM tree, AriaMapper, SemanticElementFactory, focus/announcement infrastructure)

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Virtualized List Navigation (Priority: P1)

A screen reader user navigates a long scrollable list or grid (e.g., 10,000 items) in an Uno WASM application. As they arrow through items, only the currently-visible items have semantic DOM elements. When an item scrolls into view it appears in the accessibility tree; when it scrolls out it is removed. The user never encounters "phantom" items or loses their place.

**Why this priority**: Virtualized lists and grids (ListView, GridView via ListViewBase; ItemsRepeater) are among the most common controls in business applications. Without this, screen reader users cannot navigate any list longer than one viewport, making most data-driven apps inaccessible.

**Independent Test**: Load an ItemsRepeater with 1,000 items. Use a screen reader to arrow through items. Verify each announced item matches the visible content and that unrealized items are absent from the accessibility tree.

**Acceptance Scenarios**:

1. **Given** a ListView with 1,000 items and only 20 realized, **When** the accessibility tree is queried, **Then** only the ~20 realized items have semantic DOM elements.
2. **Given** a user scrolls down 50 items, **When** new items become realized, **Then** their semantic DOM elements are created with correct position-in-set and size-of-set ARIA attributes.
3. **Given** a focused item is about to scroll out of view, **When** the virtualizer attempts to recycle it, **Then** the item is pinned (not recycled) until focus moves elsewhere.
4. **Given** items are rapidly scrolled through, **When** many realize/unrealize events fire within a short window, **Then** DOM updates are batched to prevent layout thrashing (no more than one DOM mutation per animation frame).

---

### User Story 2 — Live Region Announcements via AutomationPeer Events (Priority: P1)

A screen reader user interacts with an application that shows status messages, validation errors, or progress updates. When content changes dynamically, the screen reader announces the update without the user needing to navigate to it. Developers trigger these announcements through the standard WinUI AutomationPeer.RaiseAutomationEvent API.

**Why this priority**: Live region announcements are a WCAG 2.1 Level A requirement (4.1.3 Status Messages). Without wiring RaiseAutomationEvent to aria-live regions, dynamic content changes are invisible to screen readers.

**Independent Test**: Create a TextBlock inside a live region. Programmatically change its text and call RaiseAutomationEvent(LiveRegionChanged). Verify the aria-live region in the DOM updates and the screen reader announces the new text.

**Acceptance Scenarios**:

1. **Given** an automation peer raises LiveRegionChanged, **When** the event reaches the WASM accessibility layer, **Then** the corresponding aria-live region is updated with the new content.
2. **Given** a peer with AutomationLiveSetting.Polite raises LiveRegionChanged, **When** the screen reader processes it, **Then** the announcement waits for the current speech to finish.
3. **Given** a peer with AutomationLiveSetting.Assertive raises LiveRegionChanged, **When** the screen reader processes it, **Then** the announcement interrupts current speech.
4. **Given** multiple LiveRegionChanged events fire within 100ms, **When** they reach the accessibility layer, **Then** only the final content is announced (debounced).

---

### User Story 3 — Focus Synchronization with FocusManager (Priority: P2)

When focus moves in the XAML visual tree (via keyboard, programmatic, or pointer interaction), the semantic DOM element corresponding to the focused control receives browser focus. Conversely, when a screen reader user focuses a semantic element, the XAML control receives focus. The two layers stay synchronized.

**Why this priority**: Without bidirectional focus sync, screen reader users experience a "split brain" where the visual focus indicator and the screen reader's active element disagree, causing confusion and unreliable navigation.

**Independent Test**: Tab through 5 buttons. Verify that document.activeElement in the browser matches the semantic element for the XAML-focused button at each step. Then use screen reader virtual cursor to move to a different button and verify the XAML focus follows.

**Acceptance Scenarios**:

1. **Given** FocusManager.GotFocus fires for a Button, **When** the accessibility layer processes it, **Then** the corresponding semantic DOM element receives browser focus (element.focus()).
2. **Given** a screen reader user moves focus to a semantic element, **When** the element's focus event fires, **Then** the corresponding XAML control receives FocusState.Keyboard focus.
3. **Given** focus moves from control A to control B, **When** the transition completes, **Then** control A's semantic element has tabindex="-1" and control B's has tabindex="0" (roving tabindex pattern).
4. **Given** focus moves rapidly (e.g., holding Tab), **When** multiple GotFocus events fire, **Then** only the final focus target's semantic element receives browser focus (debounced to one per frame).

---

### User Story 4 — Modal Focus Trap (Priority: P2)

When a modal dialog (ContentDialog, Flyout with light-dismiss disabled) opens, screen reader focus is trapped within the dialog's semantic elements. The user cannot Tab or arrow-key out of the dialog boundary. When the dialog closes, focus returns to the element that opened it.

**Why this priority**: Focus trapping is a WCAG 2.4.3 requirement. Without it, screen reader users can navigate "behind" a modal dialog, interacting with controls that are visually obscured, leading to data loss or unexpected behavior.

**Independent Test**: Open a ContentDialog with two buttons. Verify that Tab cycles only between those two buttons. Verify that Shift+Tab from the first button moves to the last button (wrap-around). Close the dialog and verify focus returns to the opener.

**Acceptance Scenarios**:

1. **Given** a ContentDialog is shown, **When** the accessibility layer processes the popup, **Then** all semantic elements outside the dialog receive aria-hidden="true" and tabindex="-1".
2. **Given** focus is on the last focusable element in a modal, **When** the user presses Tab, **Then** focus wraps to the first focusable element in the modal.
3. **Given** a modal dialog closes, **When** the close animation completes, **Then** focus returns to the element that triggered the dialog and aria-hidden is removed from background elements.
4. **Given** nested modals (dialog within dialog), **When** the inner modal is active, **Then** focus is trapped within the inner modal only, and closing it returns focus to the outer modal.

---

### User Story 5 — Disabled Element Focus Recovery (Priority: P3)

When a focused element becomes disabled (IsEnabled=false) or is removed from the visual tree, focus must move to a valid target. The accessibility layer ensures the semantic DOM reflects this by moving browser focus to the next logical focusable element.

**Why this priority**: Without focus recovery, disabling a focused button leaves browser focus on a disabled element, causing screen readers to go silent or announce confusing state. This is a polish item that improves robustness.

**Independent Test**: Focus a button, then set IsEnabled=false. Verify that focus moves to the next sibling button and the screen reader announces the new focus target.

**Acceptance Scenarios**:

1. **Given** a focused button is disabled, **When** the IsEnabled property changes, **Then** focus moves to the next focusable sibling in tab order.
2. **Given** a focused element is removed from the visual tree, **When** the element is unloaded, **Then** focus moves to the parent container or next available focusable element.
3. **Given** no other focusable elements exist in the container, **When** the focused element is disabled, **Then** focus moves to the nearest ancestor that is focusable or to the document body.

---

### User Story 6 — Performance Validation (Priority: P3)

The accessibility layer adds negligible overhead to application rendering. Creating, updating, and removing semantic DOM elements does not cause visible jank or degrade frame rates, even for complex UIs with hundreds of accessible elements.

**Why this priority**: If accessibility causes performance problems, developers will disable it, defeating the purpose. Validation ensures the implementation is production-ready.

**Independent Test**: Load a complex UI with 500 accessible elements. Measure frame rate with and without the accessibility layer enabled. Verify overhead is within acceptable bounds.

**Acceptance Scenarios**:

1. **Given** a UI with 500 semantic elements, **When** measuring frame render time, **Then** the accessibility layer adds less than 2ms per frame on average.
2. **Given** a virtualized list scrolling at 60fps, **When** items are rapidly realized/unrealized, **Then** the frame rate does not drop below 30fps due to accessibility DOM updates.
3. **Given** 100 property change notifications fire simultaneously, **When** the debounce timer processes them, **Then** the total DOM update time is under 16ms (one frame budget).

---

### User Story 7 — WCAG 2.1 AA Compliance Verification (Priority: P3)

The complete accessibility implementation (from spec 001 plus this spec) meets WCAG 2.1 Level AA requirements for all supported control types. This is verified through automated tooling (axe-core) and manual screen reader testing with NVDA and VoiceOver.

**Why this priority**: WCAG compliance is often a legal requirement (ADA, EN 301 549). Verification is the final gate before the feature can be shipped.

**Independent Test**: Run axe-core against a page containing every supported control type. Verify zero critical or serious violations. Conduct manual screen reader walkthroughs with NVDA (Windows) and VoiceOver (macOS).

**Acceptance Scenarios**:

1. **Given** a test page with all 11 supported control types, **When** axe-core scans the page, **Then** zero critical or serious WCAG 2.1 AA violations are reported.
2. **Given** a screen reader user (NVDA on Windows), **When** they navigate through all controls, **Then** each control's role, name, value, and state are correctly announced.
3. **Given** a screen reader user (VoiceOver on macOS), **When** they navigate through all controls, **Then** each control's role, name, value, and state are correctly announced.
4. **Given** keyboard-only navigation (no mouse, no screen reader), **When** a user completes all primary tasks, **Then** all interactive controls are reachable and operable via keyboard alone.

---

### Edge Cases

- What happens when a virtualized list has zero items? The listbox semantic element should have aria-label indicating empty state.
- What happens when all items in a modal dialog are disabled? Focus should remain on the dialog container itself (role="dialog").
- What happens when a live region fires hundreds of events per second (e.g., a progress bar)? The two-tier rate limiter applies: 100ms debounce catches the burst, then the sustained throttle caps output at one announcement per 500ms (polite) or 200ms (assertive).
- What happens when an element is simultaneously removed from the tree AND a focus event fires for it? The focus event should be silently dropped.
- What happens when two modal dialogs open at the exact same frame? The most recently opened dialog should trap focus; the earlier one should be treated as background.
- What happens when EffectiveViewport reports a zero-size rect? Semantic elements should not be created until a valid viewport is available.

## Requirements *(mandatory)*

### Functional Requirements

**Virtualized List Support:**
- **FR-001**: System MUST create semantic DOM elements only for realized (visible) items in virtualized lists and grids (ListView, GridView via ListViewBase; ItemsRepeater).
- **FR-002**: System MUST remove semantic DOM elements when items are unrealized (scrolled out of view).
- **FR-003**: System MUST update aria-posinset and aria-setsize attributes as items realize/unrealize.
- **FR-004**: System MUST pin focused items to prevent recycling while they hold focus.
- **FR-005**: System MUST batch DOM mutations during rapid scrolling using requestAnimationFrame (see FR-024 for the general batching requirement).
- **FR-006**: System MUST respond to item realization/unrealization lifecycle events (ElementPrepared/ElementClearing for ItemsRepeater; equivalent VirtualizingPanel events for ListViewBase) to track which items are currently visible. EffectiveViewportChanged drives these events internally but is not hooked directly.

**Live Region Events:**
- **FR-007**: System MUST wire AutomationPeer.RaiseAutomationEvent(LiveRegionChanged) to update aria-live DOM regions.
- **FR-008**: System MUST respect AutomationLiveSetting (Off, Polite, Assertive) when updating live regions.
- **FR-009**: System MUST implement two-tier rate limiting for live region events: (a) 100ms debounce for rapid bursts to announce only the final content, and (b) sustained throttle of 500ms for polite / 200ms for assertive to cap continuous streams (e.g., progress bars).
- **FR-010**: System MUST support text content changes in live regions by extracting announcement text via AutomationPeer.GetName(). In the semantic DOM overlay architecture, all live region announcements are text-based (pushed to aria-live div textContent); DOM structural mutations within live regions are not applicable.

**Focus Synchronization:**
- **FR-011**: System MUST synchronize XAML focus (FocusManager.GotFocus) to browser focus on semantic elements.
- **FR-012**: System MUST synchronize browser focus on semantic elements back to XAML focus (FocusState.Keyboard).
- **FR-013**: System MUST implement roving tabindex (tabindex="0" on focused element, tabindex="-1" on others within a group).
- **FR-014**: System MUST debounce rapid focus changes to prevent excessive DOM focus calls.

**Modal Focus Trap:**
- **FR-015**: System MUST set aria-hidden="true" on all semantic elements outside an active modal.
- **FR-016**: System MUST trap Tab/Shift+Tab cycling within modal boundaries.
- **FR-017**: System MUST restore focus to the trigger element when a modal closes.
- **FR-018**: System MUST support nested modals with proper focus trap nesting.
- **FR-019**: System MUST remove aria-hidden from background elements when the last modal closes.

**Focus Recovery:**
- **FR-020**: System MUST move focus to the next focusable sibling when a focused element is disabled.
- **FR-021**: System MUST move focus to the nearest focusable ancestor when a focused element is removed from the tree.
- **FR-022**: System MUST move focus to the document body as a last resort when no focusable elements remain.

**Performance:**
- **FR-023**: System MUST add no more than 2ms per frame overhead for accessibility DOM operations on a page with 500 elements.
- **FR-024**: System MUST batch all DOM mutations within a single animation frame.
- **FR-025**: System MUST use on-demand activation via the existing "Enable accessibility" button (screen-reader-discoverable invisible button). The semantic DOM tree is not created until activated. After activation, listener counting MAY be used to optimize event processing.

**WCAG Compliance:**
- **FR-026**: System MUST pass axe-core automated testing with zero critical or serious WCAG 2.1 AA violations.
- **FR-027**: System MUST produce correct screen reader announcements for all 11 supported control types (Button, CheckBox, RadioButton, Slider, TextBox, TextBox (multiline), PasswordBox, ComboBox, ListBox, ListItem, HyperlinkButton).
- **FR-028**: System MUST support keyboard-only navigation for all interactive controls.
- **FR-029**: System MUST verify that visible focus indicators meeting WCAG 2.4.7 (Focus Visible) are present. Note: Focus indicators are rendered by the Skia rendering pipeline; this requirement is a verification gate, not an implementation task for this spec.
- **FR-030**: System MUST ensure all semantic elements have accessible names meeting WCAG 4.1.2 (Name, Role, Value).

### Key Entities

- **VirtualizedSemanticRegion**: Represents a viewport-aware section of the accessibility tree. Tracks which items are realized and manages their semantic element lifecycle. Key attributes: viewport bounds, realized item count, total item count, scroll position.
- **LiveRegionManager**: Coordinates live region announcements. Holds references to aria-live DOM elements and manages debounce timers. Key attributes: polite region element, assertive region element, pending announcement queue, debounce interval.
- **FocusSynchronizer**: Bidirectional bridge between XAML FocusManager and browser document.activeElement. Tracks the currently-focused semantic element and manages roving tabindex. Key attributes: current focused handle, previous focused handle, is-syncing flag (to prevent infinite loops).
- **ModalFocusScope**: Represents a focus trap boundary for a modal dialog. Maintains the set of focusable elements within the modal and the trigger element to restore focus to. Key attributes: modal element handle, focusable children, trigger element handle, parent scope (for nesting).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Screen reader users can navigate a virtualized list of 1,000+ items with correct announcements for every visible item, with zero phantom or missing items reported.
- **SC-002**: Dynamic content changes (status messages, validation errors, progress) are announced by screen readers within 500ms of the change occurring.
- **SC-003**: Focus indicator position in the browser always matches the XAML-focused control, with zero observed desynchronization during manual testing.
- **SC-004**: Modal dialogs trap focus with zero escape paths — Tab cycling stays within modal boundaries with no exceptions.
- **SC-005**: Disabling or removing a focused element never leaves the screen reader in a "silent" state — focus always recovers to a valid target within one frame.
- **SC-006**: Accessibility DOM overhead stays below 2ms per frame for pages with up to 500 semantic elements, as measured by browser performance profiling.
- **SC-007**: axe-core automated scan reports zero critical and zero serious WCAG 2.1 AA violations across all supported control types.
- **SC-008**: Two major screen readers (NVDA and VoiceOver) correctly announce role, name, value, and state for all 11 supported control types in manual testing.

## Clarifications

### Session 2026-02-11

- Q: Should modifying core Uno.UI AutomationPeer.RaiseAutomationEvent dispatch be in-scope or an external prerequisite? → A: In-scope. The implementation plan must first consult the WinUI C++ sources for alignment before modifying the shared Uno.UI dispatch chain.
- Q: Should the semantic DOM be always-on or activated on-demand? → A: On-demand. The existing "Enable accessibility" button (invisible div with role="button" in Accessibility.ts) is clicked by screen readers to initialize the semantic tree. FR-025 listener counting applies after activation — the semantic tree is not created until the button is clicked.
- Q: How should live region event rates be managed — single debounce or two-tier? → A: Two-tier. 100ms debounce for rapid bursts (e.g., validation errors), plus sustained throttle of 500ms for polite / 200ms for assertive for continuous streams (e.g., progress bars).
- Q: Should GridView be included in virtualized list support? → A: Yes. GridView (inherits from ListViewBase, not ItemsRepeater) should also be supported alongside ListView and ItemsRepeater.

## Assumptions

- The base accessibility infrastructure from spec 001-wasm-accessibility is complete and functional (semantic DOM tree, AriaMapper, SemanticElementFactory, debounce timer, JSImport/JSExport interop).
- EffectiveViewportChanged events are reliably fired by the Uno framework for virtualized containers (ItemsRepeater, ListView).
- Core Uno.UI changes to AutomationPeer.RaiseAutomationEvent dispatch chain are in-scope. The implementation plan must consult WinUI C++ sources (D:\Work\microsoft-ui-xaml2) to ensure alignment with the native dispatch pattern before modifying shared code.
- FocusManager.GotFocus and LostFocus events are available and fire reliably on the WASM Skia target.
- Performance measurements will be conducted on a representative mid-range device (e.g., 2020-era laptop, Chrome browser).
- NVDA (Windows) and VoiceOver (macOS Safari) are the target screen readers for manual testing.
- Visible focus indicators (WCAG 2.4.7) are provided by the Skia rendering pipeline and are assumed to be functional. This spec verifies their presence but does not implement them.

## Scope Boundaries

**In Scope:**
- Virtualized list accessibility for ItemsRepeater, ListView, and GridView (ListView/GridView via ListViewBase, ItemsRepeater separately)
- AutomationPeer.RaiseAutomationEvent wiring for LiveRegionChanged
- Bidirectional focus synchronization (XAML ↔ browser)
- Modal focus trapping for ContentDialog and modal Flyouts
- Focus recovery when elements are disabled or removed
- Performance profiling and optimization
- axe-core automated testing
- Manual screen reader testing with NVDA and VoiceOver

**Out of Scope:**
- TreeView virtualization (different navigation paradigm, separate spec)
- Drag-and-drop accessibility (complex interaction pattern, separate spec)
- High-contrast mode / forced-colors support (theming concern, separate spec)
- Touch screen reader gestures (TalkBack, VoiceOver iOS — mobile platform concern)
- Custom automation peer implementations by app developers (SDK documentation concern)
- Internationalization of ARIA labels (app-level responsibility)
- DataGrid accessibility (complex composite control, separate spec)

## Dependencies

- **001-wasm-accessibility**: Base semantic DOM infrastructure must be complete
- **Uno.UI core**: RaiseAutomationEvent dispatch changes are in-scope — this spec includes modifying the shared Uno.UI AutomationPeer dispatch chain, aligned with WinUI C++ sources
- **FocusManager**: Relies on FocusManager.GotFocus/LostFocus events being wired on the Skia WASM target
- **EffectiveViewport**: Relies on FrameworkElement.EffectiveViewportChanged being implemented for WebAssembly Skia
- **ContentDialog**: Modal focus trapping depends on ContentDialog's open/close lifecycle events being accessible
