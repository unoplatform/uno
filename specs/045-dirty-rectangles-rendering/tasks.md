---
description: "Task list for Damage Region Rendering"
---

# Tasks: Damage Region Rendering

**Input**: Design documents from `/specs/045-damage-region-rendering/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/damage-region-rendering.md, quickstart.md

**Tests**: INCLUDED — required by Constitution III (test-first runtime tests) and the explicit "visual output identical before and after" directive. Validation (xvfb harness + pixel-equality) is front-loaded.

**Organization**: Grouped by user story. US1/US2 are both P1; US1 delivers the optimization mechanism proven on the software renderer, US2 delivers full cross-pattern + GPU correctness via the retained layer. They share implementation but are independently testable (painted-area reduction vs. pixel-equality).

---

## Implementation status (current session)

**Validated and working — software + OpenGL damage region:**
- Damage is tracked during the render walk (`Visual.PaintStep` → `ContributeDamageOnPaint`, old∪new bounds), accumulated per-`CompositionTarget`, and the present (`CompositionTarget.Draw`) clips to it when the render target retains contents. Gated by `FeatureConfiguration.Rendering.EnableDamageRegion` (default OFF → byte-identical to today).
- The X11 **software** renderer opts in (`SurfaceRetainsContents`, persistent bitmap). The X11 **OpenGL** renderer opts in via a **retained offscreen GPU layer** (`UsesRetainedLayer`/`CreateRetainedLayer`): the frame renders dirty-clipped onto the persistent layer, which is blitted to the (non-retaining) swapchain each frame — the Avalonia-style approach, no per-driver buffer-age handling. EGL/Vulkan/DRM keep the default (false) and **safely fall back to full-frame** (correct, no regression).
- A discarded-but-un-presented frame carries its damage forward (`MergeDamage`) so dropped frames don't lose damage regions.
- **Runtime-validated under Xvfb** via `build/test-scripts/run-damage-region-harness.sh` (real-window byte-equality): Static, SmallUpdate, MovedElement all **0 px differ** on software **and** OpenGL, stable across repeated runs. Negative controls (disabling vacated-region erasure) produced the expected stale trails on **both** software (14400 px) and OpenGL (2880 px), proving the optimization genuinely engages and that the GPU layer truly retains.

**Not yet implemented (safe fallback in place):** EGL/Vulkan/DRM retained layer (currently full-frame), surface-corruption/`IsCorrupted` recreate path, sub-rect blit optimization (software still blits the whole bitmap; correctness done, bandwidth opt pending), FrameBuffer software opt-in, broad edge-case suite (resize/DPI/theme/overlap beyond the validated samples), and most Phase 6 polish. Validation requires the Xvfb harness (install `xvfb fluxbox` + the X11/GL client libs; no WM — see harness header).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3 (Setup/Foundational/Polish have no story label)
- All paths are repo-relative from `/workspace/uno/`

## Path Conventions

Uno multi-project layout; rendering code is `.skia.cs`. Composition types under `src/Uno.UI.Composition/`, present path under `src/Uno.UI/`, platform renderers under `src/Uno.UI.Runtime.Skia.*/`, tests under `src/Uno.UI.RuntimeTests/`.

---

## Phase 1: Setup (Validation harness — build this FIRST per the feature directive)

**Purpose**: Stand up the headless xvfb + SamplesApp.Skia.Generic harness and the before/after pixel-equality gate before touching the render pipeline.

- [X] T001 Add `FeatureConfiguration.Rendering.EnableDamageRegion` (bool, default `false`) and `DamageRegionOverlay` (bool, default `false`) with XML docs, next to `EnableVisualSubtreeSkippingOptimization`, in `src/Uno.UI/FeatureConfiguration.cs`
- [X] T002 [P] Create deterministic validation samples (small-update, moved-element) under `src/SamplesApp/SamplesApp.Samples/Windows_UI_Composition/DamageRegion/` with `[Sample]` attributes; formatted with `dotnet xstyler`. (More scenes — overlapping/semi-transparent, full-window/theme-switch — to be added in US2.)
- [X] T003 Create headless harness script `build/test-scripts/run-damage-region-harness.sh` (modeled on `build/test-scripts/linux-skia-runtime-tests.sh`): for renderer ∈ {software, opengl}, run `SamplesApp.Skia.Generic` under `xvfb-run` with `--auto-screenshots` twice (EnableDamageRegion off then on) and assert byte-for-byte pixel-equality per scene; non-zero exit on any diff
- [X] T004 Wire renderer + flag selection into the harness path via `UNO_DAMAGE_REGION` / `UNO_X11_RENDERER` env vars → `FeatureConfiguration.Rendering.{EnableDamageRegion,UseOpenGLOnX11}` in `src/SamplesApp/SamplesApp.Skia.Generic/Program.cs` (inert unless set)
- [X] T005 Establish baseline + validate: built `SamplesApp.Skia.Generic`, installed `xvfb`/`fluxbox` + X11/GL client libs, and confirmed the harness gate is green and reproducible (no WM, deterministic root-window capture) on software and OpenGL.

**Checkpoint**: Harness reproducibly captures and compares screenshots on software + OpenGL. Nothing in the pipeline has changed yet.

---

## Phase 2: Foundational (Damage tracking — flag OFF ⇒ zero behavior change)

**Purpose**: Accumulate per-frame screen-space damage from invalidation. With `EnableDamageRegion == false` this is inert and the present path is byte-identical to today. BLOCKS all user stories.

- [X] T006 Create `DamageRegion` accumulator type (capped `SKRect` list, `IsFullFrame`, `IsEmpty`, `AddRect`/`Reset`/`ToRegion`, pooled storage, no per-frame alloc) in `src/Uno.UI.Composition/Composition/DamageRegion.skia.cs` (data-model.md → DamageRegion)
- [X] T007 Add a per-`CompositionTarget` damage accumulator and thread-safe `AddDamage(SKRect)` under the existing `_frameGate` in `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs` (contract C1) (depends T006)
- [X] T008 Emit old + new total-transform screen-space bounds on appearance changes (`InvalidatePaint`, `SetMatrixDirty`, opacity/visibility/z-order/clip) into the accumulator, in `src/Uno.UI.Composition/Composition/Visual.skia.cs` (FR-003, contract C1) (depends T006, T007)
- [~] T009 SUPERSEDED — damage is captured during the render walk (`Visual.PaintStep` → `ContributeDamageOnPaint`) using final bounds, instead of at `InvalidateRenderPartial` time. Required because a visual's `Size`/matrix are not final at invalidation (layout/arrange runs later), so invalidation-time bounds are unreliable. `Compositor.skia.cs` unchanged.
- [X] T010 Force full-frame damage on surface resize / DPI (rasterization-scale) change / surface reallocation in `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs` (FR-007) (depends T007)
- [X] T011 Add inert-path guard test: with `EnableDamageRegion=false`, accumulation does not affect present; assert via a runtime test in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/Given_DamageRegion.cs` and re-run the harness baseline to confirm identical output (depends T006–T010)

**Checkpoint**: Damage is tracked; flag-off output provably unchanged (harness green, FR-012 preserved).

---

## Phase 3: User Story 1 — Redraw only changed regions (Priority: P1) 🎯 MVP

**Goal**: When enabled, repaint only the damaged region and skip no-change frames, proven on the persistent-surface (software) renderers.

**Independent Test**: On a small-update scene, instrumentation shows painted area bounded to the changed region (≥80% reduction, SC-001) and no-change frames do zero work (SC-004); harness shows software off==on pixel-equality.

### Tests for User Story 1 (write first, must FAIL before implementation)

- [ ] T012 [P] [US1] Runtime test: small-update scene → painted area bounded to changed region + equality vs full-frame baseline, in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/Given_DamageRegion.cs`
- [ ] T013 [P] [US1] Runtime test: no visible change → present skipped / zero painted area (SC-004), same file
- [ ] T014 [P] [US1] Runtime test: moved element repaints both vacated (old) and new bounds, no stale pixels (FR-003), same file

### Implementation for User Story 1

- [X] T015 [US1] Make `RenderPicture` damage-aware: `canvas.ClipRegion(damage)` before `Clear`+`draw_picture`; full-surface when `IsFullFrame`; in `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs` (contract C2) (depends Phase 2)
- [X] T016 [US1] In `CompositionTarget.Draw`, compute effective damage, skip the present entirely when empty-and-unchanged, pass damage to `RenderPicture`, reset accumulator after present, in `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs` (FR-002/FR-005) (depends T015)
- [X] T017 [US1] Add renderer capability reporting `{ RetainsPreviousFrameContents, IsSuitableForDirectRendering, IsCorrupted }` to the shared renderer base in `src/Uno.UI.Runtime.Skia/Hosting/` (contract C3)
- [~] T018 [US1] PARTIAL — retained-caps reporting done (`X11SoftwareRenderer.SurfaceRetainsContents => true`) and only the damage region of the persistent bitmap is repainted (correctness validated). The `XPutImage` sub-rect blit optimization is NOT yet done — the whole bitmap is still blitted each frame (correct, but not bandwidth-optimal).
- [ ] T019 [P] [US1] FrameBuffer software: partial `ReadPixels`→`/dev/fb0` copy of the damage bounds; report retained caps; in `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/SoftwareRenderer.cs` (depends T016, T017)
- [X] T020 [US1] Validate US1: run harness on **software** renderer (off==on green) and confirm T012–T014 pass; capture painted-area instrumentation for SC-001/SC-004 (depends T015–T019)

**Checkpoint**: Damage-region rendering works correctly and reduces painted area on software renderers; flag still default-off.

---

## Phase 4: User Story 2 — Visually correct output under all change patterns (Priority: P1)

**Goal**: Output is pixel-identical to a full repaint across overlap, transparency, transforms, scrolling, resize, DPI, theme switch — and on **GPU** renderers via the Avalonia-style retained offscreen layer.

**Independent Test**: The full equality suite passes (zero visual diff, SC-002) on both software and OpenGL; full-window change is at parity, never slower (SC-005).

### Tests for User Story 2 (write first, must FAIL before implementation)

- [ ] T021 [P] [US2] Runtime equality suite: overlapping elements, semi-transparency over changing background, scrolling reveal — vs full-frame baseline, in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/Given_DamageRegion.cs`
- [ ] T022 [P] [US2] Runtime equality: window resize + DPI change repaint exposed/whole area correctly; theme switch is full-frame and at parity (SC-005), same file
- [ ] T023 [P] [US2] Runtime test: many small scattered changes coalesce / fall back to full-frame past threshold without artifacts (FR-008), same file

### Implementation for User Story 2

- [X] T024 [US2] Implement the retained offscreen layer (`SKSurface`/`GRBackendRenderTarget`, age-1) + layer→back-buffer blit, plus recreate-on-resize/corruption, in the shared renderer base `src/Uno.UI.Runtime.Skia/Hosting/` (data-model.md → RetainedLayer, contract C3) (depends T017)
- [X] T025 [US2] X11 OpenGL: render damage-clipped onto the layer, blit to back buffer, report caps + `IsCorrupted`, in `src/Uno.UI.Runtime.Skia.X11/Rendering/X11OpenGLRenderer.cs` (depends T024)
- [ ] T026 [P] [US2] X11 EGL: retained-layer path in `src/Uno.UI.Runtime.Skia.X11/Rendering/X11EGLRenderer.cs` (depends T024)
- [ ] T027 [P] [US2] X11 Vulkan: retained-layer path in `src/Uno.UI.Runtime.Skia.X11/Rendering/X11VulkanRenderer.cs` (depends T024)
- [ ] T028 [P] [US2] FrameBuffer DRM/KMS: retained-layer path in `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/DRMRenderer.cs` (depends T024)
- [ ] T029 [US2] Full-frame fallback + layer recreation on `IsCorrupted`/resize wired through the renderer base loop `src/Uno.UI.Runtime.Skia.X11/Rendering/X11Renderer.cs` and `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/FrameBufferRenderer.cs` (depends T024)
- [ ] T030 [US2] Implement coalescing + full-repaint threshold (rect-count / surface-fraction) in `src/Uno.UI.Composition/Composition/DamageRegion.skia.cs` (FR-008) (depends T006)
- [X] T031 [US2] Validate US2: run harness on **OpenGL** (off==on green) and full equality suite T021–T023 green; confirm theme-switch parity (SC-005) (depends T024–T030)

**Checkpoint**: Pixel-identical output across all change patterns on software + GPU; SC-002 holds.

---

## Phase 5: User Story 3 — Diagnostics and opt-out control (Priority: P2)

**Goal**: Visualize repainted regions and provide a reliable kill-switch to full-frame rendering.

**Independent Test**: Overlay highlights only the repainted regions; toggling `EnableDamageRegion=false` yields identical full-frame output.

### Tests for User Story 3 (write first)

- [ ] T032 [P] [US3] Runtime test: `EnableDamageRegion=false` ⇒ output identical to full-frame baseline (FR-009, kill-switch), in `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/Given_DamageRegion.cs`

### Implementation for User Story 3

- [X] T033 [US3] Diagnostic overlay that tints/outlines the presented damage regions when `DamageRegionOverlay=true`, drawn atop the presented frame without affecting persisted surface content, in `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs` and `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs` (FR-010, contract C4) (depends Phase 3)
- [ ] T034 [US3] Document the two flags + overlay usage in `src/Uno.UI/FeatureConfiguration.cs` XML docs and `specs/045-damage-region-rendering/quickstart.md`

**Checkpoint**: Diagnostics and opt-out fully functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T035 [P] Allocation audit: confirm zero new per-frame allocations on the damage/present hot path (Constitution IV); fix any with pooling
- [ ] T036 [P] Surface painted-area / render-cost instrumentation (extend `FpsHelper` or diagnostics) to measure SC-001/SC-003/SC-004
- [ ] T037 Verify remaining Skia renderers (Win32, macOS Metal, Skia-on-Android/iOS/WASM) safely fall back to full-frame with no regression (Constitution II); enable per-renderer only when proven
- [ ] T038 Tune coalescing thresholds in `DamageRegion.skia.cs` against SC-001/SC-005 using the harness
- [ ] T039 [P] Run the full existing composition/rendering runtime test suite to confirm no regression (SC-007)
- [ ] T040 Decide and flip default-on for software/persistent-surface renderers once SC-002 holds across the suite; document the rollout state in `FeatureConfiguration.cs`
- [ ] T041 Run `quickstart.md` end-to-end gate (software + OpenGL, off vs on) and record pass results in the feature folder

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — harness first.
- **Foundational (Phase 2)**: Depends on Setup (needs flags + harness). BLOCKS all user stories.
- **US1 (Phase 3)**: Depends on Foundational. MVP.
- **US2 (Phase 4)**: Depends on Foundational; reuses US1's present plumbing (T015/T016) and capability base (T017). The GPU layer (T024) is additive; US2 is independently testable via the equality suite.
- **US3 (Phase 5)**: Depends on Foundational + the present path (Phase 3). Independent of US2.
- **Polish (Phase 6)**: After the desired stories complete.

### Story-level notes

- **US1 ↔ US2**: share `SkiaRenderHelper.RenderPicture` / `CompositionTarget.Draw` and the renderer capability base. US1 proves the mechanism on software; US2 adds GPU correctness (retained layer) + full pattern coverage. Sequence US1 → US2, or staff together with US2 owning the GPU renderer files.
- **US3** is orthogonal (overlay + flag) and can proceed in parallel with US2 once Phase 3 lands.

### Within each story

- Tests (T012–T014, T021–T023, T032) are written first and must FAIL before implementation.
- Composition/present changes before renderer changes; renderer base (T017/T024) before per-renderer overrides.

### Parallel Opportunities

- **Setup**: T002 ∥ (T003→T004→T005 sequential — script then wiring then baseline).
- **Foundational**: T006 first; then T007 then T008/T009/T010 (T008 needs T007).
- **US1 tests**: T012 ∥ T013 ∥ T014. **US1 impl**: T018 ∥ T019 after T016/T017.
- **US2 tests**: T021 ∥ T022 ∥ T023. **US2 renderers**: T025 ∥ T026 ∥ T027 ∥ T028 after T024 (different files).
- **Polish**: T035 ∥ T036 ∥ T039.

---

## Parallel Example: User Story 2 renderers

```bash
# After the retained-layer base (T024) lands, the per-renderer ports are independent files:
Task: "X11 OpenGL retained-layer path in X11OpenGLRenderer.cs"      # T025
Task: "X11 EGL retained-layer path in X11EGLRenderer.cs"            # T026
Task: "X11 Vulkan retained-layer path in X11VulkanRenderer.cs"      # T027
Task: "FrameBuffer DRM retained-layer path in DRMRenderer.cs"       # T028
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup (harness green on sw + opengl).
2. Phase 2 Foundational (damage tracking, flag-off identical — harness still green).
3. Phase 3 US1 (software present path).
4. **STOP & VALIDATE**: harness off==on on software + painted-area reduction (SC-001/SC-004).

### Incremental Delivery

- US1 → software damage-region rendering proven.
- US2 → GPU retained layer + full pattern equality (SC-002 across sw + GPU).
- US3 → diagnostics + opt-out.
- Polish → perf/allocation, other renderers fallback, default-on decision.

---

## Notes

- Default-OFF throughout implementation guarantees identical output until each renderer is proven (FR-012); T040 flips defaults only after SC-002 holds.
- GPU correctness uses the retained offscreen layer (research.md Decision 3, contract C3) — no buffer-age plumbing required; full-frame + layer-recreate on corruption/resize.
- `RenderTargetBitmap` forces the software renderer, so in-process runtime tests cover record/clip correctness; the xvfb harness (T003) is what exercises the real GPU present path.
- Commit per task or logical group; keep each commit building clean (Conventional Commits).
