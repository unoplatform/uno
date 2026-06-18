# Phase 1 Data Model: Damage Region Rendering

Internal rendering state only — no persisted/storage entities, no public API types. All live on the Skia render path.

## Entity: DamageRegion (per-frame damage accumulator)

The accumulated screen-space area that must be repainted for the next frame.

| Field | Type | Notes |
|-------|------|-------|
| `Rects` | capped list of `SKRect` (root/scaled-pixel coords) | Reused/pooled; no per-frame alloc on hot path. |
| `IsFullFrame` | bool | Set when damage covers the whole surface or a threshold is exceeded → present clears+blits the full surface. |
| `IsEmpty` | bool (derived) | No rects and not full-frame → frame may be skipped entirely (FR-005, SC-004). |

**Behavior / rules**
- `AddRect(SKRect screenBounds)` — unions a changed region. If `Rects.Count` exceeds the rect-count threshold, collapse to a single bounding rect; if coverage exceeds the surface-fraction threshold, set `IsFullFrame`.
- `Reset()` — called after a frame is presented; clears rects, `IsFullFrame=false`.
- `ToRegion()` — materializes an `SKRegion`/clip used by the present step (pooled).
- Mutated under the existing per-`CompositionTarget` frame gate (thread-safe between UI and render thread).

**Relationships**: owned by `CompositionTarget`; populated by `Invalidation` events; consumed by the present step and by `RetainedLayer`.

## Entity: Invalidation (a single change contribution)

A request originating from a visual change that contributes area to the current `DamageRegion`.

| Field | Type | Notes |
|-------|------|-------|
| `OldBounds` | `SKRect` (screen coords) | The region the visual occupied before the change — repainted to erase vacated pixels (FR-003). |
| `NewBounds` | `SKRect` (screen coords) | The region the visual now occupies. |
| `Cause` | enum-ish | paint / matrix / opacity / visibility / z-order / clip / resize. Resize→ forces `IsFullFrame`. |

**Behavior / rules**
- Derived inside `Visual.skia.cs` invalidation methods (`InvalidatePaint`, `SetMatrixDirty`, `InvalidateParentChildrenPicture`) from the visual's total transform/clip bounds.
- For semi-transparent/overlapping content, the union of old+new bounds is contributed; correct compositing within the region is handled by replaying the full picture clipped to that region (no extra per-visual bookkeeping needed).
- Not retained as objects — conceptually it is the `(old, new)` pair fed into `DamageRegion.AddRect`; modeled here for clarity.

**Relationships**: produced by `Visual` invalidation; aggregated into `DamageRegion`.

## Entity: RetainedLayer (GPU previous-frame surface)

A single Uno-owned persistent offscreen surface that always holds the previous frame, used by GPU swapchain renderers so damage-region output is correct regardless of swapchain buffer rotation (Avalonia-style). Replaces the buffer-age window as the primary GPU mechanism.

| Field | Type | Notes |
|-------|------|-------|
| `Surface` | `SKSurface` / `GRBackendRenderTarget` | Sized to the scaled surface; recreated on resize/DPI change/corruption. |
| `Caps` | `{ RetainsPreviousFrameContents, IsSuitableForDirectRendering, IsCorrupted }` | Reported by the renderer per acquire. |

**Behavior / rules**
- GPU, `!RetainsPreviousFrameContents` (common): replay picture **damage-clipped onto `Surface`** (age-1, no cross-frame union), then blit `Surface` → back buffer.
- GPU fast path, `RetainsPreviousFrameContents && IsSuitableForDirectRendering` (e.g. EGL `EGL_BUFFER_PRESERVED`): render damage-clipped directly to back buffer, present damage sub-rect; layer unused.
- Persistent-surface renderers (X11/FrameBuffer software): retained backing bitmap ⇒ layer not required; use current `DamageRegion` directly.
- `IsCorrupted` (device/context lost, resize) ⇒ recreate `Surface` + full-frame redraw.

**Relationships**: lives on the platform renderer (or shared `Uno.UI.Runtime.Skia` base); consumes the current-frame `DamageRegion`; gates the present clip and blit.

> *Optional, deferred*: a buffer-age fast path (`EGL_EXT_buffer_age` + union of last `age` frames' damage, partial present, no layer) may be added per-backend later where it cheaply beats the blit. Not required for correctness or the initial GPU win.

## Configuration (FeatureConfiguration.Rendering)

| Setting | Type | Default | Maps to |
|---------|------|---------|---------|
| `EnableDamageRegion` | bool | OFF (→ default-ON per renderer once SC-002 holds) | master switch (FR-009) |
| `DamageRegionOverlay` | bool | OFF | diagnostic overlay of presented damage (FR-010) |

## State transitions (per frame)

```
[visual change] → Invalidation(old,new) → DamageRegion.AddRect (or IsFullFrame)
        │
        ▼
RequestNewFrame → (UI) Render(): record full SKPicture (unchanged)
        │
        ▼
(render thread) Draw():
   if DamageRegion disabled OR Caps.IsCorrupted → FULL frame (recreate layer if needed)
   else if DamageRegion.IsEmpty → skip present (SC-004)
   else:
     persistent-surface  → clip to DamageRegion → Clear+drawPicture → present damage sub-rect
     GPU (!retains)      → clip to DamageRegion → drawPicture onto RetainedLayer → blit layer → back buffer
     GPU (retains/fast)  → clip to DamageRegion → Clear+drawPicture on back buffer → present damage sub-rect
        │
        ▼
DamageRegion.Reset()  (RetainedLayer now holds this frame)
```
