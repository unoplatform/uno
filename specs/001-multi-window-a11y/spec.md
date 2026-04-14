# Feature Specification: Multi-Window Accessibility for Skia Desktop Hosts

**Feature Branch**: `001-multi-window-a11y`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "Multi-window accessibility support for Uno Platform Skia Win32 and macOS hosts. Each top-level Window must have its own accessibility tree and instance so screen readers can navigate active and inactive secondary windows correctly."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Screen reader can navigate every top-level window (Priority: P1)

A user of assistive technology (Narrator on Windows, VoiceOver on macOS, NVDA, JAWS) is running an Uno Platform desktop application that opens more than one top-level window — for example a main document window and a secondary preferences or tool window. The user must be able to reach and interact with every visible control in every open window, regardless of which window currently has keyboard focus.

**Why this priority**: This is the baseline functional expectation of any assistive-technology-compliant desktop application. Without it, users relying on screen readers cannot use multi-window applications at all: secondary windows are either invisible to the screen reader or expose the wrong content. This is also a likely accessibility-compliance blocker for enterprise customers.

**Independent Test**: Launch a sample application that opens two windows. With a screen reader active, navigate controls in the primary window, switch to the secondary window, navigate its controls, then switch back. All controls in both windows are announced correctly, with the right labels, roles, and states. Closing either window leaves the other fully navigable.

**Acceptance Scenarios**:

1. **Given** an application with two open top-level windows, **When** the user activates the secondary window and invokes screen-reader navigation, **Then** the screen reader reports the controls of the secondary window (not the primary window).
2. **Given** an application with two open top-level windows and the primary window focused, **When** the user directs the screen reader to navigate to the inactive secondary window (e.g., via VoiceOver item chooser, Narrator scan mode, or object navigation), **Then** the screen reader successfully reaches the secondary window's controls and reports them correctly.
3. **Given** two open windows with identical control labels, **When** the user navigates each window, **Then** the announced labels and states correspond to the controls of the respective window and do not leak state from the other window.
4. **Given** two open windows, **When** the user closes the secondary window, **Then** the primary window remains fully navigable by the screen reader with no loss of announcements or disconnected UI state.
5. **Given** two open windows, **When** the user closes the primary window first and leaves the secondary window open, **Then** the secondary window remains fully navigable by the screen reader.

---

### User Story 2 - Announcements route to the correct window (Priority: P1)

An application raises an accessibility announcement (for example, a "Changes saved" notification, a form-validation message, or a live-region update). The user, regardless of which window is active, must hear the announcement in the context of the right window, without hearing duplicate or dropped announcements caused by cross-window interference.

**Why this priority**: Accessibility announcements are the primary way screen-reader users are informed of transient state changes. If two windows both raise an identical message, or if one window's debouncing suppresses the other window's message, the user loses information. This bug is silent and effectively invisible to sighted testing.

**Independent Test**: In an app with two open windows, trigger an announcement from window A. Shortly after, trigger the same announcement text from window B. Both announcements are heard (subject only to the screen reader's own duplicate-suppression rules), not artificially deduplicated by the framework. Additionally, trigger an announcement whose source element is in an inactive window while a different window is active; the announcement reaches the user in the correct window's context.

**Acceptance Scenarios**:

1. **Given** two open windows, **When** window A raises an announcement "Saved" and within 200 ms window B raises "Saved", **Then** both announcements are delivered to the platform screen reader and are associated with their originating window.
2. **Given** two open windows and an announcement that is raised with a known source element in window B while window A is active, **When** the announcement is dispatched, **Then** it is announced in the context of window B.
3. **Given** two open windows and an announcement with no associated source element (pure text), **When** the announcement is dispatched, **Then** it is routed to the currently active window.
4. **Given** no window is active (for example, the app lost focus entirely), **When** an announcement with no source element is dispatched, **Then** the system reaches a defined outcome (routed to the most-recently-active surviving window, or silently dropped with a diagnostic trace) without crashing.

---

### User Story 3 - Rapidly creating and closing windows does not crash (Priority: P1)

A developer or automation test opens and closes secondary windows in quick succession — for example, repeatedly launching transient dialogs, opening the same tool window many times, or running stress scenarios. The application must remain stable: no crashes, no stale references, no leaked accessibility state.

**Why this priority**: The current macOS implementation intentionally disables accessibility for secondary windows because it segfaults under rapid create/destroy. Shipping a multi-window accessibility feature that reintroduces crashes is worse than the status quo. This scenario is the primary root-cause driver for the native-side redesign.

**Independent Test**: Create and immediately close a secondary window 100 times in a tight loop (from user code or a sample) with a screen reader active. The process does not crash, does not leak growing memory, and the screen reader continues to function correctly against the remaining primary window.

**Acceptance Scenarios**:

1. **Given** a running application with a screen reader active, **When** the user opens and closes a secondary window 100 times in rapid succession, **Then** the application does not crash and the primary window remains fully accessible.
2. **Given** a running application, **When** a secondary window is closed while the screen reader is mid-query on one of its elements, **Then** the platform receives a well-formed "disconnected" response rather than a crash or a hang.
3. **Given** a running application, **When** a secondary window is closed, **Then** all accessibility resources owned by that window (native tree elements, event subscriptions, timers) are released and do not accumulate across repeated cycles.

---

### User Story 4 - Per-window focus and announcement state isolation (Priority: P2)

An application uses live regions, debounced status messages, or automated announcement streams in each of its open windows. The per-window state (what was last announced, what is currently focused) must not interfere across windows.

**Why this priority**: This is a correctness-of-experience issue distinct from P1. Even if announcements reach the right window, duplicate-suppression or debouncing state shared across windows causes subtle drops and misattributions that are difficult to diagnose. Fixing this is required for a fully correct experience but the immediate user-visible symptom can be less severe than missing tree coverage.

**Independent Test**: Two windows each announce the same live-region text at overlapping intervals. Both windows independently apply their own debouncing/throttling/duplicate-suppression rules. Focus moves between windows without one window's focus-tracking logic affecting the other.

**Acceptance Scenarios**:

1. **Given** two windows each raising polite live-region announcements, **When** the timing of announcements overlaps, **Then** each window's duplicate-suppression is applied only within that window.
2. **Given** focus moves from window A to window B, **When** a control is removed from window A's tree, **Then** window A's focus-recovery logic runs against window A's tree only (it does not affect focus in window B).

---

### User Story 5 - Maintains parity on primary-window-only applications (Priority: P1)

An application that opens only one top-level window (the common case today) must continue to work identically after the refactor. No regressions in tree correctness, announcement timing, focus behavior, or performance.

**Why this priority**: This is a compatibility baseline. The refactor must not regress the existing single-window accessibility experience already shipped in PR #23005. If single-window scenarios break, the multi-window gains are moot.

**Independent Test**: Run the existing accessibility runtime test suite (`Given_AccessibleButton`, `Given_AccessibleCheckBox`, `Given_AccessibleSlider`, `Given_AccessibilityAnnouncements`, `Given_AccessibilityFocus`, etc.) against the refactored implementation. All tests pass without modification.

**Acceptance Scenarios**:

1. **Given** the existing accessibility runtime test suite, **When** run against the refactored implementation, **Then** every test passes.
2. **Given** a single-window application with a screen reader active, **When** the user exercises typical control interactions (buttons, toggles, sliders, text inputs), **Then** behavior and announcements are indistinguishable from the pre-refactor experience.

---

### Edge Cases

- **Startup race**: An accessibility event is raised for a UI element before the window has fully initialized its accessibility subsystem. The system must drop the event silently (with a diagnostic trace) rather than crash.
- **Detached element**: A UI element without an owning window (not yet attached, or recently detached) triggers an accessibility callback. The system must resolve "no owning window" and drop the callback, not crash or route to an arbitrary window.
- **Window close during dispatch**: A coalesced structure-change event is dispatched after the owning window has begun tearing down. The pending callback must no-op rather than operate on disposed state.
- **Primary window closed first**: The first-created window is closed while a secondary window remains open. The secondary window's accessibility must continue to function; the active-window tracker must fall back gracefully.
- **All windows inactive**: The application loses focus entirely (user alt-tabs / switches apps). The active-window tracker retains the most-recently-active window so that when the user returns, announcements route correctly.
- **Rapid activation toggling**: The user alt-tabs rapidly between windows or apps. The active-window pointer updates correctly without race-related corruption.
- **Same-control announcement across windows**: Two windows raise identical announcement text within the same debounce window. Each window's duplicate-suppression acts independently.
- **Screen-reader query of an inactive window's root after its contents have changed**: An inactive window receives a tree query from the screen reader after the tree has mutated. The returned tree reflects the current state, not a stale snapshot.
- **Mixed key/main focus on macOS**: A secondary window is key but not main (e.g., a utility panel). The active-window tracker handles this based on the appropriate platform signal without oscillation.
- **Screen-reader client outlives the window**: An assistive technology client retains a reference to an element in a window that has since closed. The client receives a well-formed disconnected response on subsequent queries.

## Requirements *(mandatory)*

### Functional Requirements

#### Per-window accessibility tree

- **FR-001**: The system MUST expose a distinct accessibility tree for each top-level window created by the application, such that each tree is independently discoverable by the platform's accessibility technology.
- **FR-002**: Each per-window accessibility tree MUST remain queryable by assistive technology regardless of whether the window is currently active/focused.
- **FR-003**: The accessibility tree exposed for a given window MUST reflect only the content of that window; it MUST NOT include content belonging to any other window.
- **FR-004**: Closing one window MUST NOT affect the accessibility tree or responsiveness of any other still-open window.
- **FR-005**: The system MUST support applications that open a single primary window with behavior identical to the pre-refactor baseline.

#### Announcement routing

- **FR-006**: When an accessibility announcement is raised with a known source element, the system MUST announce it in the context of the window that owns that element, regardless of which window is currently active.
- **FR-007**: When an accessibility announcement is raised without a source element, the system MUST route it to the currently active window.
- **FR-008**: When no window is active at the time a source-less announcement is raised, the system MUST fall back to the most-recently-active surviving window, and if none exists, MUST drop the announcement with a diagnostic trace rather than crash.
- **FR-009**: Duplicate-suppression, debouncing, and throttling of announcements MUST be scoped per window; state from one window MUST NOT suppress or throttle announcements originating in another window.

#### Focus

- **FR-010**: Focus tracking for accessibility purposes MUST be scoped per window. Each window tracks its own focused element; cross-window focus coordination is not required.
- **FR-011**: Focus-recovery logic (when a focused element is disabled or unloaded) MUST run within the scope of the originating window only and MUST NOT redirect focus to another window.
- **FR-012**: When the active window changes, the accessibility subsystem MUST update its notion of the active window in response to the platform's activation signal.

#### Lifecycle and disposal

- **FR-013**: When a window is closed, the system MUST deterministically release all accessibility resources owned by that window before the underlying native window handle is destroyed.
- **FR-014**: When a window is closed, the system MUST signal the accessibility technology that the window's tree elements are no longer valid, such that subsequent client queries receive a well-formed disconnected response rather than dereferencing stale state.
- **FR-015**: Any per-window background timers, debouncers, dispatcher-queued callbacks, or event subscriptions established by the accessibility subsystem MUST be torn down when the window is closed, such that they do not fire against disposed state.
- **FR-016**: The system MUST tolerate rapid sequential creation and destruction of secondary windows without crashes, memory growth proportional to the number of cycles, or observable accessibility malfunction on surviving windows.

#### Stability and graceful degradation

- **FR-017**: Accessibility events raised during startup, before a window has completed accessibility initialization, MUST be silently ignored with a diagnostic trace.
- **FR-018**: Accessibility callbacks that reference a UI element with no discoverable owning window MUST be silently ignored with a diagnostic trace; they MUST NOT be routed to an arbitrary window.
- **FR-019**: The system MUST NOT rely on any process-global accessibility state shared across windows for correctness of per-window tree content, focus, or announcements.

#### Scope of platforms

- **FR-020**: The Windows (Win32) Skia desktop host MUST support multi-window accessibility as described in FR-001 through FR-019.
- **FR-021**: The macOS Skia desktop host MUST support multi-window accessibility as described in FR-001 through FR-019.
- **FR-022**: The WebAssembly host, mobile hosts (Android, iOS), and any other non-Skia-desktop hosts are out of scope for this feature and are not required to change.

#### Rollout and compatibility

- **FR-023**: The multi-window support MUST be delivered without a runtime opt-in toggle. The pre-refactor single-window behavior MUST be removed as part of the refactor; both behaviors MUST NOT coexist in shipped code.
- **FR-024**: The Windows (Win32) support MUST ship first. Between the Windows-only delivery and the macOS delivery, macOS MUST retain its current primary-window-only behavior (documented as a known limitation); it MUST NOT crash or regress for any single-window scenario during this interim.

### Key Entities

- **Window-scoped accessibility instance**: The per-window object that owns that window's accessibility tree, provider/element cache (where applicable), focus tracker, and announcement state. One instance exists per open top-level window; created when the window is created and disposed when the window is closed.
- **Accessibility router**: A process-wide coordinator that holds the single registration slots the framework exposes for accessibility listening (property changes, automation events, tree mutations, announcements) and dispatches each incoming signal to the correct window-scoped accessibility instance. Also tracks which window is currently active for source-less announcement routing.
- **Active window reference**: The router's record of which window-scoped accessibility instance is currently active. Updated on platform activation signals; retained across deactivation to support the fallback path for source-less announcements after the application loses focus.
- **Window-scoped native accessibility context** (macOS only): The per-window native-side container for tree elements, root element, and focused-element references, bound to the lifetime of the platform window object so that the native lifecycle matches the window lifecycle.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a multi-window Uno Platform Skia desktop application with an active screen reader, a user can navigate and interact with 100% of controls in every open top-level window, regardless of which window is currently active.
- **SC-002**: In a multi-window application, announcements raised with a known source element reach the platform screen reader associated with the correct (source) window in 100% of cases.
- **SC-003**: In a multi-window application, two windows raising identical announcement text within overlapping debounce windows each successfully deliver their announcement to the platform, with no cross-window suppression.
- **SC-004**: Opening and closing a secondary window 100 times in rapid succession with a screen reader active produces zero process crashes and no accessibility-state growth proportional to the cycle count.
- **SC-005**: After closing any single window, the remaining open windows continue to be fully navigable by the screen reader, with 100% of their controls reachable and correctly announced.
- **SC-006**: The existing single-window accessibility runtime test suite (all tests present prior to this feature) passes without modification on the refactored implementation.
- **SC-007**: A new automated test for the multi-window scenario verifies, on the Skia Desktop target, that: (a) two simultaneously open windows each have a non-null, enabled accessibility instance; (b) the element/provider sets for the two windows are disjoint; (c) resolving an automation peer from one window's tree produces that window's instance and no other; (d) disposing one window's instance removes it from routing without affecting the other.
- **SC-008**: For both Windows and macOS, a documented manual validation checklist (tree navigation in both windows, announcement routing to active and inactive windows, focus changes, close primary first, close secondary first, rapid create/destroy) is executed with a real screen reader and passes without observed defects before the corresponding platform's support is considered shipped.

## Assumptions

- The framework's existing single-slot accessibility registration points (automation peer listener, announcement implementation, tree-mutation callbacks, visual-change callbacks) remain single slots; the solution therefore introduces a routing layer rather than multi-instance registration at the framework level.
- A UI element attached to any window is reliably discoverable back to its owning window via the existing per-host visual-root-to-host mapping.
- Element identifiers used to correlate managed UI elements with their native accessibility counterparts (e.g., visual handles on macOS, provider references on Windows) are process-unique and do not collide across windows.
- Platform activation signals (the Windows activation message and the macOS "did become main" notification) are reliable triggers for active-window tracking. On macOS, "became main" is preferred over "became key" because it corresponds most closely to the user's notion of the "current" application window for announcements.
- The .NET 9+ `ConditionalWeakTable` enumeration capability is available and may be used for deterministic teardown of per-window provider caches.
- Screen-reader querying of inactive windows is a supported and expected scenario for all major assistive technologies (Narrator, NVDA, JAWS, VoiceOver) and automated testing tools; the system must therefore keep every window's tree live regardless of activation state.
- Announcements dispatched during a narrow window-close transition may be dropped without user-visible harm; the system optimizes for never crashing over never dropping in this edge case.
- The current guard in the macOS host that restricts accessibility initialization to the primary window is a known workaround for the native segfault and will be removed as part of the macOS delivery once the native context redesign is in place.

## Dependencies

- The feature builds on the initial accessibility implementation delivered in PR #23005. The shared base class, aria-role mapping, announcement debouncer, automation-peer listener contract, and per-platform initial implementations are prerequisites.
- The feature depends on the existing per-host mapping used by the Skia desktop hosts to resolve a visual-tree element back to its owning native window.
- The feature depends on the existing per-window wrapper objects on each Skia desktop host being the authoritative owners of the native window handle and its lifecycle hooks (activation, destruction), so that per-window accessibility instances can be anchored to them.

## Out of Scope

- WebAssembly host accessibility (handled by its own multi-document model).
- Mobile platform accessibility (Android, iOS), which uses native accessibility frameworks distinct from Uno's Skia desktop accessibility layer.
- Automated UIA-client-based assertions (driving Narrator / UIA from tests); manual validation with real screen readers covers the OS-integration behavior in this feature.
- Additional new accessibility patterns or peers beyond what PR #23005 already supports.
- Cross-window focus coordination or cross-window focus-recovery redirection.
- Changes to framework-level announcement APIs beyond what is required for correct per-window routing.
