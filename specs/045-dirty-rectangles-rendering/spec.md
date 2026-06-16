# Feature Specification: Dirty Rectangles Rendering

**Feature Branch**: `045-dirty-rectangles-rendering`  
**Created**: 2026-06-16  
**Status**: Draft  
**Input**: User description: "The goal is to implement support 'dirty rectangles' rendering, i.e. draw only parts that are different from the previous frame."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Redraw only changed regions (Priority: P1)

When an application's UI changes only in a small portion of the window between two frames (for example, a blinking text caret, a spinning progress ring, a single updating label, or a hovered button), the framework redraws only the region that actually changed instead of repainting the entire window surface.

**Why this priority**: This is the core value of the feature. The overwhelming majority of UI updates in real applications affect a small fraction of the screen. Repainting the whole surface every frame wastes CPU and GPU cycles and battery. Redrawing only the changed region directly reduces per-frame rendering cost, which is the entire reason for the feature.

**Independent Test**: Display a UI where a single small element animates or updates while the rest of the screen is static. Confirm (via rendering instrumentation/diagnostics) that the painted area per frame is bounded to the changed region rather than the full window, and that the visible result is pixel-identical to a full repaint.

**Acceptance Scenarios**:

1. **Given** a window with a large static background and one small element that changes, **When** the small element updates, **Then** only the area covering the changed element (plus any required margin) is repainted and the rest of the surface is preserved from the previous frame.
2. **Given** a frame in which nothing visible has changed, **When** the next frame is presented, **Then** no drawing work is performed for that frame (or the frame is skipped entirely) while the displayed content remains correct.
3. **Given** an element moves from one position to another, **When** the frame is rendered, **Then** both the region the element vacated and the region it now occupies are repainted so no stale pixels remain.

---

### User Story 2 - Visually correct output under all change patterns (Priority: P1)

The displayed result of dirty-rectangle rendering is always pixel-identical to what a full-frame repaint would have produced, across overlapping elements, transparency, transforms, clipping, scrolling, resizing, and rapid successive changes.

**Why this priority**: A rendering optimization that produces visual artifacts (stale pixels, tearing, ghosting, missing updates) is worse than no optimization. Correctness is non-negotiable and must hold for every change pattern, so it shares top priority with the core optimization itself.

**Independent Test**: Run a suite of UI scenarios (overlapping semi-transparent elements, animated transforms, scrolling lists, window resize, theme switch) with dirty-rectangle rendering enabled and compare the rendered output against a full-repaint baseline; outputs must match.

**Acceptance Scenarios**:

1. **Given** two overlapping elements where the lower one changes, **When** the frame is rendered, **Then** the overlap region is composited correctly and the upper element still appears on top with no stale pixels.
2. **Given** a semi-transparent element over a changing background, **When** the background region updates, **Then** the transparency is recomputed correctly within the affected region.
3. **Given** the window is resized, **Then** the newly exposed area is fully painted and previously visible content remains correct.
4. **Given** content scrolls, **Then** the newly revealed region is painted and no torn or duplicated content appears.

---

### User Story 3 - Diagnostics and opt-out control (Priority: P2)

Developers can observe which regions are being treated as dirty (for tuning and bug diagnosis) and can disable the optimization to fall back to full-frame rendering when needed.

**Why this priority**: Dirty-rectangle tracking is subtle and bugs manifest as visual glitches that are hard to attribute. A way to visualize the dirty regions and a reliable kill-switch back to full repaint are essential for diagnosing issues, validating correctness, and giving applications an escape hatch — but the feature delivers its value without them, so this is P2.

**Independent Test**: Enable the diagnostic overlay and confirm dirty regions are highlighted on screen; toggle the optimization off and confirm rendering reverts to full-frame behavior with identical visual output.

**Acceptance Scenarios**:

1. **Given** the diagnostic overlay is enabled, **When** regions are redrawn, **Then** the redrawn regions are visibly indicated (e.g., highlighted) so a developer can confirm what is being repainted.
2. **Given** the optimization is disabled via configuration, **When** the app renders, **Then** every frame is fully repainted and output is visually identical to the optimized path.

---

### Edge Cases

- **Nothing changed**: A frame where no visible content changed performs no draw work, yet the displayed surface stays correct.
- **Everything changed**: A change affecting the whole window (theme switch, full-screen animation, root background change) degrades gracefully to a full repaint without being slower than today.
- **Many small scattered changes**: Numerous small dirty regions across the frame are handled without the bookkeeping cost exceeding the cost of a single full repaint (the system may coalesce regions or fall back to full repaint past a threshold).
- **Moving / transformed / animated elements**: Both old and new bounds are invalidated so the vacated area is restored.
- **Off-screen and partially clipped changes**: Changes outside the visible surface or clipped by a parent do not trigger repaints of areas that are not visible.
- **Window resize / DPI (scale) change**: Surface reallocation re-establishes a correct full frame.
- **Overlapping and semi-transparent content**: Changes under transparent or overlapping elements correctly repaint the full affected stack within the region.
- **Occlusion**: A change to an element fully hidden behind an opaque element does not cause visible repaint work where it would not be seen.
- **Rapid successive invalidations within one frame**: Multiple invalidations before a frame is presented are accumulated into the correct combined region.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST track, per frame, the region(s) of the rendered surface whose visual output differs from the previously presented frame.
- **FR-002**: The system MUST repaint only the tracked changed region(s) and preserve unchanged areas from the previous frame, while producing output pixel-identical to a full repaint.
- **FR-003**: When a visual element changes position, size, visibility, opacity, transform, or any property affecting its appearance, the system MUST invalidate both the element's previous and new occupied regions.
- **FR-004**: The system MUST accumulate all invalidations that occur before a frame is presented into a single combined dirty region for that frame.
- **FR-005**: When no visible change has occurred since the last presented frame, the system MUST avoid performing redraw work for that frame.
- **FR-006**: The system MUST correctly composite overlapping, clipped, and semi-transparent content within a dirty region so that no stale pixels or incorrect blending remain.
- **FR-007**: The system MUST fully repaint newly exposed surface area produced by window resize, scale/DPI change, or surface reallocation.
- **FR-008**: The system MUST degrade to a full-frame repaint when changes cover most/all of the surface or when the number/complexity of dirty regions would make partial repaint no cheaper than a full repaint.
- **FR-009**: The system MUST provide a configuration switch to disable dirty-rectangle rendering and fall back to full-frame rendering, producing visually identical output.
- **FR-010**: The system MUST provide a diagnostic mode that visualizes the regions being repainted, for tuning and debugging.
- **FR-011**: The optimization MUST be applied without requiring application code changes — existing applications benefit automatically when the optimization is enabled.
- **FR-012**: The system MUST preserve existing public rendering API contracts and WinUI behavioral parity; the optimization is an internal rendering change only and MUST NOT alter observable application behavior other than performance.
- **FR-013**: The system MUST keep native (non-Skia) rendering targets working as they do today; this optimization targets the Skia rendering pipeline and MUST NOT regress native-rendering output. *(See Assumptions for platform scope.)*

### Key Entities *(include if feature involves data)*

- **Dirty Region**: The accumulated area of the rendered surface that must be repainted for the next frame, expressed as one or more rectangles (or an equivalent region representation). Has a relationship to the elements that contributed to it.
- **Previous Frame Surface**: The retained pixel content of the last presented frame, used as the basis onto which only dirty regions are repainted.
- **Invalidation**: A request, originating from a visual change to an element, that marks one or more areas of the surface as needing repaint for the next frame. Carries the affected bounds (old and/or new).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a representative UI where a small element (≤5% of window area) updates while the rest is static, per-frame painted area is reduced by at least 80% compared to full-frame repaint.
- **SC-002**: Rendered output with the optimization enabled is pixel-identical to full-frame rendering across the full correctness test suite (zero visual differences).
- **SC-003**: For small-change scenarios, measured per-frame CPU/GPU rendering cost is reduced relative to the current full-repaint behavior (no regression; meaningful improvement on partial-update workloads).
- **SC-004**: Frames in which nothing visible changed perform no redraw work (verifiable via instrumentation showing zero painted area).
- **SC-005**: A full-window change (e.g., theme switch) renders no slower than the current full-repaint behavior (worst case is at parity, not a regression).
- **SC-006**: The optimization can be toggled off at runtime/startup and the application continues to render correctly with identical visual output.
- **SC-007**: No regression in existing runtime rendering tests on the targeted Skia platforms.

## Assumptions

- **Platform scope**: This optimization targets the **Skia rendering pipeline** (Desktop Win32/macOS/Linux and Skia-on-mobile/WASM), consistent with the project's Skia-first development scope. Native rendering targets (native Android Views, native iOS/UIKit, WASM DOM) are maintenance-only and must remain working but are out of scope for this optimization.
- **Default state**: Whether the optimization ships enabled-by-default or behind an opt-in flag is a rollout decision; the feature provides a switch either way (FR-009). A reasonable default is to enable it once correctness is validated, with the switch available as an escape hatch.
- **Region representation**: The system may coalesce multiple small dirty regions into a smaller set (or a bounding region) and may fall back to full repaint past a complexity threshold; exact thresholds are tuning details to be determined during implementation.
- **Correctness baseline**: "Correct" is defined as pixel-identical to the existing full-frame repaint output for the same scene.
- **Behavioral parity**: No public API or observable application behavior changes other than rendering performance; WinUI parity is preserved.
- **Validation approach**: Correctness and performance are validated via runtime rendering tests on at least the Skia desktop target, comparing against full-repaint baselines and measuring painted area / cost.
