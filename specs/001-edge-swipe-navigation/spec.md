# Feature Specification: Edge Swipe Back Navigation

**Feature Branch**: `001-edge-swipe-navigation`
**Created**: 2026-02-24
**Status**: Draft
**Input**: User description: "Add edge gesture back navigation, similar to iOS native swipe-from-edge-to-go-back. Available as a drop-in opt-in feature across all targets."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enable Edge Swipe on a Frame (Priority: P1)

As an app developer, I want to enable edge swipe back navigation on a Frame by setting a single property, so that my users can swipe from the screen edge to go back without any additional code.

**Why this priority**: This is the core value proposition — a drop-in, single-property opt-in that brings gesture-based back navigation to all platforms. Without this, the feature has no entry point.

**Independent Test**: Can be fully tested by creating a Frame with the property enabled, navigating forward to a second page, then swiping from the left edge to return. Delivers the fundamental swipe-to-go-back experience.

**Acceptance Scenarios**:

1. **Given** a Frame with edge swipe navigation enabled and a user on the second page of a navigation stack, **When** the user touches near the left edge of the screen and swipes right past the completion threshold, **Then** the app navigates back to the previous page.
2. **Given** a Frame with edge swipe navigation enabled and a user on the second page, **When** the user touches near the left edge and swipes right but releases before the completion threshold, **Then** the current page snaps back to its original position and no navigation occurs.
3. **Given** a Frame with edge swipe navigation enabled and a user on the first page (no back history), **When** the user touches near the left edge and swipes right, **Then** nothing happens — the gesture is ignored.
4. **Given** a Frame without edge swipe navigation enabled, **When** the user touches near the left edge and swipes right, **Then** nothing happens — the gesture is not intercepted.

---

### User Story 2 - Interactive Visual Feedback During Swipe (Priority: P2)

As a user, I want to see the current page slide in real-time as I drag my finger from the edge, with visual cues (shadow/dimming) indicating the gesture is in progress, so that the interaction feels responsive and native.

**Why this priority**: Visual feedback is essential for the gesture to "feel native" — without it, there's no indication the gesture is happening until navigation completes. This transforms a functional feature into a polished experience.

**Independent Test**: Can be tested by initiating an edge swipe and observing that the current page follows the finger position in real-time, with a shadow overlay that fades proportionally to the swipe distance.

**Acceptance Scenarios**:

1. **Given** a user initiating an edge swipe on an enabled Frame, **When** the user drags their finger horizontally, **Then** the current page visually translates (slides) following the finger's horizontal position.
2. **Given** a user mid-swipe, **When** the page is partially slid over, **Then** a semi-transparent shadow/dimming overlay is visible, fading proportionally as the page slides further.
3. **Given** a user who releases the swipe past the completion threshold, **When** the release occurs, **Then** the page animates smoothly to fully off-screen before navigation completes (not an abrupt jump).
4. **Given** a user who releases the swipe before the completion threshold, **When** the release occurs, **Then** the page animates smoothly back to its original position.

---

### User Story 3 - Right-to-Left Language Support (Priority: P3)

As a developer building an app for RTL languages (Arabic, Hebrew, etc.), I want the edge swipe gesture to automatically adapt so users swipe from the right edge instead of the left, matching the RTL content flow direction.

**Why this priority**: RTL support is essential for internationalized apps but affects a smaller user base than the core LTR behavior. The gesture must respect content direction to be usable in RTL contexts.

**Independent Test**: Can be tested by setting the Frame's flow direction to right-to-left, navigating forward, then swiping from the right edge to verify back navigation triggers from the correct side.

**Acceptance Scenarios**:

1. **Given** a Frame with edge swipe enabled and FlowDirection set to RightToLeft, **When** the user swipes from the right edge toward the left, **Then** back navigation occurs.
2. **Given** a Frame with edge swipe enabled and FlowDirection set to RightToLeft, **When** the user swipes from the left edge, **Then** nothing happens — the left edge is not the leading edge in RTL.

---

### User Story 4 - Platform-Aware Behavior on iOS Native (Priority: P3)

As a developer using native iOS navigation (NativeFramePresenter), I want the edge swipe feature to automatically defer to the native iOS back swipe gesture rather than adding a duplicate handler, so there are no gesture conflicts.

**Why this priority**: iOS already provides a high-quality native back swipe when using NativeFramePresenter. Duplicate gesture handling would degrade the experience. This is a correctness concern for a specific (but important) platform configuration.

**Independent Test**: Can be tested by enabling edge swipe on a Frame using NativeFramePresenter on iOS and verifying that only the native gesture activates (no double-animation or conflicts).

**Acceptance Scenarios**:

1. **Given** a Frame using NativeFramePresenter on iOS with edge swipe enabled, **When** the user swipes from the left edge, **Then** only the native iOS back swipe gesture activates — the custom handler does not interfere.
2. **Given** a Frame NOT using NativeFramePresenter on iOS (e.g., Skia renderer), **When** edge swipe is enabled, **Then** the custom gesture handler activates normally.

---

### Edge Cases

- What happens when the user starts a swipe and then moves their finger vertically instead of horizontally? The gesture should be abandoned, allowing vertical scrolling to proceed.
- What happens when the swipe starts inside a horizontally scrollable area (e.g., ScrollViewer with horizontal scrolling)? The edge zone is narrow enough (default 20 device-independent pixels) to avoid most conflicts, but if a conflict occurs, the edge gesture should yield to child scroll controls.
- What happens when the swipe starts inside a SwipeControl (list item swipe actions)? The edge gesture should yield to the SwipeControl's own swipe handling.
- What happens when the user rapidly swipes with high velocity but covers only a short distance? If velocity exceeds the velocity threshold, navigation should complete regardless of distance.
- What happens if the Frame's content changes during a swipe animation (e.g., programmatic navigation)? The gesture should cancel, clean up any transforms, and allow the programmatic navigation to proceed.
- What happens if the feature is toggled off (IsEnabled set to false) while a swipe is in progress? The gesture should cancel gracefully with a snap-back animation.
- What happens on a device with no touch input (keyboard/mouse-only desktop)? By default, the gesture only responds to touch input. Mouse-based edge swipe is available as an opt-in configuration for developers who want it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST be opt-in via a single declarative property on a Frame element (no code-behind required for basic usage).
- **FR-002**: The feature MUST detect touch input originating within a configurable edge zone (default: 20 device-independent pixels from the leading edge).
- **FR-003**: The feature MUST only activate when the Frame has back navigation history available.
- **FR-004**: The feature MUST provide real-time visual feedback by translating the current page content in the direction of the swipe.
- **FR-005**: The feature MUST display a shadow/dimming overlay during the swipe that fades proportionally to the swipe progress.
- **FR-006**: The feature MUST complete back navigation when the swipe distance exceeds a configurable fraction of the Frame width (default: 40%) OR when swipe velocity exceeds a configurable threshold (default: 800 pixels per second).
- **FR-007**: The feature MUST animate the page back to its original position (snap-back) when the swipe does not meet completion thresholds.
- **FR-008**: The feature MUST respect the Frame's content flow direction — the leading edge is the left side for LTR and the right side for RTL.
- **FR-009**: The feature MUST NOT interfere with existing native back gestures on platforms that already provide them (e.g., iOS NativeFramePresenter).
- **FR-010**: The feature MUST NOT conflict with child gesture handlers (ScrollViewer horizontal scrolling, SwipeControl swipe actions) — it should yield when appropriate.
- **FR-011**: The feature MUST work on all supported Uno Platform targets: WebAssembly, Skia Desktop (Windows, macOS, Linux), Android, and iOS.
- **FR-012**: The feature MUST only respond to touch and pen input by default. Mouse input support MUST be available as an opt-in configuration.
- **FR-013**: The feature MUST provide configurable thresholds (edge zone width, completion distance fraction, velocity threshold, mouse input toggle) for developer customization.
- **FR-014**: Completion and snap-back animations MUST use easing curves that feel natural and responsive.

### Key Entities

- **Edge Swipe Gesture**: A touch interaction that begins within the edge zone and translates horizontally. Has properties: start position, cumulative translation, velocity, state (idle, detecting, swiping, completing, cancelling).
- **Edge Zone**: A narrow strip along the leading edge of the Frame where swipe gestures are detected. Width is configurable. Position depends on flow direction (LTR vs RTL).
- **Completion Threshold**: The combination of distance and velocity criteria that determine whether a swipe results in navigation or snap-back. Both are independently configurable.

## Assumptions

- The first version (v1) will reveal the Frame's background behind the sliding page. A "previous page peek" effect (showing the previous page behind the current one with parallax) is deferred to a future enhancement.
- The completion animation duration is approximately 250ms with an ease-out curve, matching common mobile platform conventions.
- The shadow overlay uses a semi-transparent dark color at approximately 50% opacity at rest, fading to 0% as the page fully slides away.
- On platforms where native back gestures already exist at the OS level (Android system back gesture), the custom edge swipe coexists as a separate in-app gesture — they operate at different levels (OS vs UI element) and do not conflict.
- The feature integrates with the standard Frame.GoBack() navigation method and does not introduce alternative navigation paths.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can enable edge swipe back navigation on any Frame with a single property declaration — no additional code, setup, or configuration required.
- **SC-002**: The gesture interaction provides real-time visual feedback with no perceptible lag between finger movement and page translation.
- **SC-003**: The swipe gesture correctly navigates back on all five target platforms (WebAssembly, Skia Desktop, Android, iOS with Skia renderer, iOS with NativeFramePresenter via native delegation).
- **SC-004**: Users accustomed to iOS-style back swipe find the gesture intuitive and natural on first use, without instructions.
- **SC-005**: The edge swipe gesture does not interfere with horizontal scrolling, list item swiping, or other touch-based interactions outside the edge zone.
- **SC-006**: RTL apps correctly detect swipes from the right edge without any additional developer configuration beyond setting FlowDirection.
- **SC-007**: The feature adds no measurable performance overhead when enabled but not actively in use (idle state).
