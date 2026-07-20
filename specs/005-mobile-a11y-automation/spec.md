# Feature Specification: Mobile Accessibility and Automation

**Feature Branch**: `005-mobile-a11y-automation`  
**Created**: 2026-07-10  
**Status**: Draft  
**Input**: Implement full mobile accessibility and automation support for Android and iOS, matching the existing Win32, WebAssembly, and macOS backends while reproducing WinUI behavior as closely as each mobile accessibility API permits.
**Depends On**: `004-a11y-parity-remediation` shared automation-peer and backend parity work

## Context and Scope

Uno's Skia renderers on Android and iOS need first-class accessibility bridges equivalent
in completeness to the Win32 UIA, WebAssembly semantic-DOM, and macOS NSAccessibility
backends. The WinUI `AutomationPeer` tree remains the semantic source of truth. Each mobile
backend translates that shared contract into the platform accessibility model used by
TalkBack, VoiceOver, UIAutomator, XCUITest, and Appium.

The primary delivery targets are **Skia-on-Android** and **Skia-on-iOS**. Shared fixes may
also benefit the legacy native Android and UIKit renderers, which must keep compiling and
must not regress, but full new-feature parity on those maintenance-only renderers is not a
release gate. tvOS-specific behavior is out of scope except where it shares safe UIKit code.

"Full support" means that every WinUI semantic exposed by Uno's supported automation peers
is represented through a native equivalent when one exists. UIA-only concepts without a
mobile equivalent must use a documented, deterministic fallback rather than being silently
dropped or inventing a public Uno API.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover and understand every control (Priority: P1)

A TalkBack or VoiceOver user navigates a Uno application and hears the correct accessible
name, role, value, state, help text, position, and relationships for each meaningful element.
Decorative and purely structural elements do not pollute the accessibility tree.

**Why this priority**: If the native semantic tree is incomplete or wrong, every other
interaction and automation scenario becomes unreliable.

**Independent Test**: Render a representative page containing standard Uno controls and
inspect the native Android and iOS accessibility trees. Each element must expose the
expected semantics derived from its `AutomationPeer`.

**Acceptance Scenarios**:

1. **Given** a named enabled control with an automation peer, **When** assistive technology
   reaches it, **Then** its role, name, value, state, and available actions match the peer.
2. **Given** `LabeledBy`, `DescribedBy`, heading, landmark, item-position, or form metadata,
   **When** the native node is queried, **Then** every representable relationship and
   semantic is exposed without flattening away essential meaning.
3. **Given** a decorative, collapsed, off-screen, or non-semantic element, **When** the
   native tree is queried, **Then** it is omitted or hidden according to WinUI tree rules.
4. **Given** a custom peer or `EventsSource`, **When** its provider is resolved, **Then**
   the native node reflects the resolved peer rather than the visual owner by accident.

---

### User Story 2 - Operate controls through assistive technology (Priority: P1)

A mobile screen-reader user can invoke buttons, toggle choices, select items, expand or
collapse content, adjust ranges, scroll containers, edit text, and use custom peer actions.

**Why this priority**: A readable but non-operable tree still blocks completion of core app
tasks.

**Independent Test**: Invoke every supported native accessibility action against controls
that implement the corresponding WinUI provider pattern and verify the Uno control changes.

**Acceptance Scenarios**:

1. **Given** an enabled peer implementing Invoke, Toggle, SelectionItem, ExpandCollapse,
   RangeValue, Value, Scroll, ScrollItem, or Window semantics, **When** the corresponding
   native action is performed, **Then** the provider method runs on the UI thread.
2. **Given** a disabled or read-only control, **When** assistive technology requests a
   mutating action, **Then** the action is rejected consistently and the app does not crash.
3. **Given** a peer exposing multiple applicable actions, **When** its node is queried,
   **Then** all supported actions are advertised and route to the correct provider.
4. **Given** an action that changes state, **When** it completes, **Then** the native node
   and announcement update without requiring the user to leave and re-enter the control.

---

### User Story 3 - Keep accessibility focus synchronized (Priority: P1)

TalkBack or VoiceOver focus follows the logical XAML accessibility tree through navigation,
popups, dialogs, virtualization, scrolling, and programmatic focus changes without loops or
focus loss.

**Why this priority**: Mobile accessibility is unusable when native focus and XAML focus
diverge or when focused virtual nodes disappear unexpectedly.

**Independent Test**: Move focus in both directions between XAML and the native
accessibility service, including through a modal dialog and a virtualized list.

**Acceptance Scenarios**:

1. **Given** XAML focus moves to an accessible element, **When** the move settles, **Then**
   the corresponding native accessibility element can receive accessibility focus.
2. **Given** assistive technology focuses or activates a native accessibility element,
   **When** XAML focus is appropriate for that control, **Then** XAML focus moves once
   without a feedback loop.
3. **Given** a modal popup or dialog, **When** it opens, **Then** background content is
   excluded from traversal and focus enters the modal; closing restores a valid focus target.
4. **Given** a focused virtualized item, **When** layout or scrolling would recycle it,
   **Then** focus is preserved or moved deterministically before its native node disappears.

---

### User Story 4 - Hear live state and structure changes (Priority: P1)

Dynamic changes to names, values, selection, validation, expansion, live regions, windows,
and the accessible tree reach TalkBack and VoiceOver promptly through native notifications.

**Why this priority**: Mobile apps are dynamic; creation-time-only semantics become stale
immediately and can cause users to act on incorrect information.

**Independent Test**: Mutate each mapped automation property and raise each supported
automation event after mounting a control, then observe the native property/event stream.

**Acceptance Scenarios**:

1. **Given** a mapped automation property changes, **When** the peer raises its property
   change, **Then** the existing native node reports the new value and emits the appropriate
   platform notification.
2. **Given** children are added, removed, reordered, realized, or unrealized, **When** the
   structure changes, **Then** the native tree updates incrementally with no stale nodes.
3. **Given** a live-region, notification, window, menu, layout, text-edit, or focus event,
   **When** the peer raises it, **Then** the closest native equivalent is delivered once.
4. **Given** a burst of redundant updates, **When** they occur in one UI cycle, **Then**
   duplicate native notifications are coalesced without losing the final state.

---

### User Story 5 - Navigate rich text, collections, and ranges (Priority: P2)

Users can navigate and understand text controls, lists, grids, trees, tabs, menus, ranges,
and scrollable regions with the semantics expected from native mobile applications.

**Why this priority**: These composite controls dominate production applications and need
more than a generic label and tap action.

**Independent Test**: Exercise representative text, collection, range, and scroll controls
with TalkBack and VoiceOver and inspect their native node metadata and actions.

**Acceptance Scenarios**:

1. **Given** editable, read-only, multiline, password, or rich text, **When** queried or
   edited through assistive technology, **Then** value, selection, editability, and secure
   text behavior are correct for the supported platform capabilities.
2. **Given** a list, grid, tree, tab set, menu, or radio group, **When** navigated, **Then**
   container type, item role, selection, hierarchy, and position/count metadata are exposed.
3. **Given** a slider, progress bar, scroll bar, or other range control, **When** queried or
   adjusted, **Then** minimum, maximum, current value, text value, orientation, and supported
   increment actions are correct.
4. **Given** a scrollable or virtualized surface, **When** traversed or scrolled, **Then**
   realized child nodes, bounds, visibility, and scroll actions stay synchronized.

---

### User Story 6 - Automate mobile Uno applications reliably (Priority: P2)

An application author can locate and operate Uno controls through UIAutomator, XCUITest, or
Appium using stable identifiers and native actions without relying on rendered text or
screen coordinates.

**Why this priority**: Accessibility and UI test automation share the native semantic tree;
missing identifiers or unstable virtual nodes block robust end-to-end testing.

**Independent Test**: Build a mobile sample with unique `AutomationId` values and run
Android and iOS automation scripts that locate, inspect, activate, and verify controls.

**Acceptance Scenarios**:

1. **Given** `AutomationProperties.AutomationId`, **When** the application is inspected by
   a native automation framework, **Then** the corresponding stable native identifier is
   exposed and is not substituted for the user-facing accessible name.
2. **Given** two controls with distinct identifiers, **When** the tree changes or scrolls,
   **Then** each realized native node remains uniquely locatable and routes to the same peer.
3. **Given** a standard control action, **When** invoked through Appium, UIAutomator, or
   XCUITest, **Then** it follows the same provider path as assistive-technology activation.
4. **Given** password or otherwise sensitive content, **When** inspected through automation,
   **Then** private values remain protected according to the platform's secure-text rules.

---

### User Story 7 - Preserve performance and lifecycle correctness (Priority: P2)

Enabling mobile accessibility does not rebuild the whole tree per frame, leak visual
elements, retain recycled items, or perform expensive work when no accessibility service is
active.

**Why this priority**: Mobile devices are resource-constrained, and an always-expensive or
leaking bridge would not be shippable even if functionally correct.

**Independent Test**: Profile a page with 500 accessible elements and a virtualized list
while enabling, navigating, mutating, scrolling, disabling, and re-enabling accessibility.

**Acceptance Scenarios**:

1. **Given** no accessibility or automation client is active, **When** the app renders,
   **Then** backend-specific semantic work remains dormant except for lightweight listener
   detection and required platform registration.
2. **Given** a property or subtree change, **When** accessibility is active, **Then** only
   affected native nodes are invalidated or rebuilt.
3. **Given** elements are unloaded or recycled, **When** a collection occurs, **Then** the
   bridge holds no stale strong reference to the element, peer, or native node.
4. **Given** accessibility is toggled or an app window changes, **When** the backend
   reattaches, **Then** the tree is rebuilt once and does not duplicate nodes or listeners.

---

### User Story 8 - Prove parity on both mobile platforms (Priority: P2)

Contributors can run deterministic automated tests plus a documented TalkBack and VoiceOver
validation matrix that covers the shared contract and platform translations.

**Why this priority**: Without platform-level tests, backend behavior can compile while the
actual native accessibility tree remains broken.

**Independent Test**: Run the shared contract suite and the Android/iOS native tree and
action suites against the same control matrix; complete the manual AT smoke matrix.

**Acceptance Scenarios**:

1. **Given** a shared peer scenario, **When** run against Android and iOS adapters, **Then**
   both expose equivalent semantics or an explicitly documented platform-specific fallback.
2. **Given** a functional fix, **When** its test is run before and after implementation,
   **Then** it demonstrably fails before and passes after on the affected platform.
3. **Given** the release validation matrix, **When** completed, **Then** every P1 control,
   action, event, focus, and automation-identity scenario passes on both TalkBack and VoiceOver.

### Edge Cases

- An element has a peer but no representable native role; it uses a deterministic generic
  role while retaining name, state, actions, and children.
- `EventsSource` chains, custom peers, and peer-only children must not create duplicate or
  unreachable native nodes.
- A relationship target is collapsed, virtualized away, in another window, or has no native
  node; the relationship is omitted or updated rather than left dangling.
- Bounds are empty, clipped, transformed, scrolled, or cross a window boundary; hit testing
  and reported native bounds must remain valid.
- A focused element becomes disabled; its native state/actions update immediately without
  forcing XAML focus away, matching WinUI. If it is collapsed, removed, or recycled, native
  accessibility focus must recover to a deterministic valid target.
- Multiple windows, popups, nested modals, and transient flyouts must expose only their
  active accessible subtrees and restore focus correctly.
- Rapid progress, scroll, text, or selection updates must not flood native event queues.
- Accessibility is enabled after the visual tree already exists, disabled while focused, or
  re-enabled after an activity/view-controller recreation.
- Controls with no accessible name must not borrow `AutomationId` as spoken text.
- Secure text must never be exposed through labels, values, logs, or automation snapshots.
- Platform services may query nodes from non-UI threads; all peer access and mutations must
  obey Uno and native thread-affinity rules without deadlocks.

## Requirements *(mandatory)*

### Functional Requirements

**Shared semantic contract**

- **FR-001**: The system MUST use the resolved WinUI `AutomationPeer` graph, including
  `EventsSource` and peer-provided children, as the semantic source of truth.
- **FR-002**: The system MUST expose every supported peer's name, control type, localized
  control type, help text, value, enabled/read-only state, focusability, off-screen state,
  heading, landmark, position, size, level, and orientation when the target API can represent it.
- **FR-003**: The system MUST provide a documented deterministic fallback for each shared
  semantic or event that Android or iOS cannot represent directly.
- **FR-004**: The system MUST NOT add a public Uno API solely for a platform bridge when the
  existing WinUI automation contract can express the behavior.
- **FR-005**: The system MUST reuse shared internal peer-resolution, tree-inclusion, and
  provider-action helpers across Android and iOS while materializing native node properties
  from the live resolved peer on demand rather than maintaining an eagerly copied semantic snapshot.

**Tree and identity**

- **FR-006**: The system MUST create stable native accessibility nodes for meaningful
  realized peers and omit decorative, collapsed, pruned, or inaccessible elements.
- **FR-007**: The system MUST maintain parent/child ordering consistent with the resolved
  peer tree across add, remove, move, virtualization, popup, and window changes.
- **FR-008**: The system MUST expose `AutomationId` through the native automation identifier
  channel without using it as the spoken accessible name.
- **FR-009**: The system MUST maintain stable per-window node identity for the lifetime of a
  realized peer and MUST prevent stale node IDs from routing to a recycled peer.
- **FR-010**: The system MUST report transformed screen bounds, clipping, visibility,
  hit-testability, and accessibility focus eligibility accurately.
- **FR-011**: The system MUST support peer relationships and annotations using native
  equivalents where available and capability-documented fallbacks otherwise.

**Patterns and actions**

- **FR-012**: The system MUST translate Invoke, Toggle, Selection, SelectionItem,
  ExpandCollapse, RangeValue, Value, Scroll, ScrollItem, Window, and applicable text
  provider behavior into native actions.
- **FR-013**: The system MUST advertise only actions valid for the peer's current state and
  MUST update the advertised action set when that state changes.
- **FR-014**: The system MUST execute provider actions on the Uno UI thread and preserve the
  WinUI disabled/read-only error semantics without crashing the native accessibility service.
- **FR-015**: The system MUST route native activation and UI test automation through the
  same provider/action path.
- **FR-016**: The system MUST support custom actions required to represent additional peer
  patterns when no standard native action exists.

**Focus, navigation, and modals**

- **FR-017**: The system MUST synchronize XAML focus and native accessibility focus without
  re-entrant loops or duplicate focus notifications.
- **FR-018**: The system MUST provide deterministic traversal order based on the peer tree,
  tab order, and platform conventions.
- **FR-019**: The system MUST constrain accessibility traversal to the active modal scope
  and restore a valid focus target when that scope closes.
- **FR-020**: The system MUST preserve WinUI's XAML-focus behavior when the focused element
  is merely disabled, while updating its native enabled/action state; it MUST recover native
  accessibility focus when the element is hidden, removed, recycled, or moved to an inactive window.
- **FR-021**: The system MUST support accessibility hit testing and requests to bring an
  off-screen or virtualized item into view.

**Properties, events, and live updates**

- **FR-022**: The system MUST update native node properties when the corresponding
  automation property changes, without requiring full-tree reconstruction.
- **FR-023**: The system MUST translate focus, property, structure, live-region,
  notification, window, menu, layout, text-edit, and selection events to the closest native
  event or announcement.
- **FR-024**: The system MUST coalesce redundant events within one UI dispatch cycle while
  preserving event ordering and final state.
- **FR-025**: The system MUST rebuild or invalidate the smallest affected native subtree
  after structural changes.
- **FR-026**: The system MUST handle accessibility service activation, deactivation,
  application lifecycle changes, and multi-window attachment without duplicate listeners or nodes.

**Rich controls**

- **FR-027**: The system MUST expose editable, read-only, multiline, password, and rich text
  semantics, including supported selection and editing actions, without exposing secure content.
- **FR-028**: The system MUST expose list, grid, tree, tab, menu, radio-group, and other
  composite semantics including selection, hierarchy, position, count, and virtualization metadata.
- **FR-029**: The system MUST expose range and progress semantics including minimum,
  maximum, current value, text value, orientation, and supported adjustment actions.
- **FR-030**: The system MUST expose scrollability, current scroll state when representable,
  directional scroll actions, and scroll-to-item behavior.

**Platform coverage**

- **FR-031**: Android MUST expose the semantic tree and actions through native Android
  accessibility APIs consumable by TalkBack, UIAutomator, and Appium.
- **FR-032**: iOS MUST expose the semantic tree and actions through UIKit accessibility APIs
  consumable by VoiceOver, XCUITest, and Appium.
- **FR-033**: Shared changes MUST preserve compilation and existing behavior on legacy native
  Android/UIKit renderers; full parity on those maintenance-only renderers is not a release gate.
- **FR-034**: UIA-only behavior with no mobile equivalent MUST be recorded in a maintained
  capability matrix with the chosen Android and iOS fallback.

**Testing, diagnostics, and performance**

- **FR-035**: The implementation MUST include shared contract tests plus native Android and
  iOS tree, property, event, focus, identity, and action tests.
- **FR-036**: The implementation MUST include Appium or native-framework smoke tests that
  locate controls by `AutomationId` and operate representative provider patterns.
- **FR-037**: The implementation MUST provide trace-level diagnostics for node lifecycle,
  peer resolution, action routing, focus routing, and native event emission without logging
  secure text.
- **FR-038**: The implementation MUST avoid constructing or refreshing a full native
  semantic tree on every render frame.
- **FR-039**: The implementation MUST release peer, visual, and native-node references when
  elements are unloaded, recycled, or windows are destroyed.
- **FR-040**: Every behavior change MUST have a fails-before/passes-after automated test on
  the lowest layer that proves the native observable behavior.

### Key Entities

- **Mobile Semantic Node**: Stable per-window registry entry for a resolved automation peer,
  including native identity, weak owner lookup, parent/children, bounds invalidation, and lifecycle state.
- **Peer Projection**: Ephemeral, pull-based view of the resolved peer's current properties,
  patterns, relationships, and actions, materialized when Android or iOS queries a native node.
- **Mobile Accessibility Tree**: Per-window graph of semantic nodes, including active modal
  scope, root nodes, focus state, virtualization state, and native node-ID lookup.
- **Mobile Accessibility Adapter**: Android- or iOS-specific translator that exposes semantic
  nodes through the native accessibility API and routes native requests back to provider actions.
- **Automation Action**: Platform-neutral command derived from a WinUI provider pattern and
  advertised as one or more native standard or custom actions.
- **Capability Mapping**: Recorded mapping from each WinUI property, pattern, relation, and
  event to its Android equivalent, iOS equivalent, or documented fallback.
- **Accessibility Focus State**: Re-entrancy-safe association among XAML focus, native
  accessibility focus, the active window/modal, and the currently realized semantic node.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The mobile capability matrix covers 100% of WinUI automation properties,
  provider patterns, relations, and events used by Uno's non-generated peers, with an Android
  mapping, iOS mapping, or explicit unsupported/fallback rationale for every entry.
- **SC-002**: On both Android and iOS, every in-scope standard control in the parity fixture
  exposes the expected role, name, value/state, bounds, identifier, children, and actions in
  automated native-tree assertions.
- **SC-003**: All P1 actions can be completed using TalkBack and VoiceOver without touch
  exploration falling back to raw screen coordinates.
- **SC-004**: Programmatic state and structure changes become observable through the native
  accessibility API within one UI dispatch cycle, except intentionally rate-limited announcements.
- **SC-005**: Accessibility focus remains valid through 100 consecutive focus, popup,
  scroll, virtualization, disable, and removal transitions with no duplicate focus loop,
  dead node, or unrecoverable focus state.
- **SC-006**: UIAutomator and XCUITest/Appium smoke suites locate 100% of fixture controls by
  `AutomationId` and successfully invoke representative Invoke, Toggle, Selection,
  ExpandCollapse, RangeValue, Value, Scroll, and ScrollItem actions.
- **SC-007**: A virtualized collection of at least 1,000 items exposes only realized or
  accessibility-required nodes, reports correct item metadata, and retains no recycled peer
  after forced garbage collection in lifecycle tests.
- **SC-008**: With accessibility inactive, the bridge adds no full-tree construction and no
  continuous per-frame work; with accessibility active on a 500-node fixture, incremental
  semantic updates complete within a 16 ms frame budget at the 95th percentile on the
  Android API 34 emulator and iOS 17.5 simulator used by the existing Skia mobile CI
  stages; representative physical-device profiling is a manual release gate.
- **SC-009**: Automated shared and platform suites pass with zero ignored P1 accessibility
  tests, and the documented TalkBack/VoiceOver matrix has no unresolved blocker for the
  standard control fixture.

## Assumptions

- The current accessibility parity branch is the baseline and its shared peer routing,
  disabled-action semantics, structure events, relationships, and notification work remain available.
- WinUI's open-source automation implementation is the behavioral reference; mobile-native
  conventions take precedence only where UIA concepts have no faithful platform equivalent.
- Android and iOS native accessibility services can query nodes outside the normal render
  path, so snapshots and node identity must be safe across asynchronous native callbacks.
- The implementation reuses existing Uno controls and peer/provider interfaces rather than
  introducing a parallel public accessibility object model.
- Manual TalkBack and VoiceOver validation remains necessary for announcement quality even
  after native tree and event behavior is automated.
