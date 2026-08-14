# Effects neutralization — evaluate the effect graph with drawing primitives

**Status:** Design. Not yet implemented. Supersedes the backend
`IDrawingFactory.CreateEffectFilter` / `IEffectFilter` SPI.

## Problem

`IDrawingFactory.CreateEffectFilter(IGraphicsEffect effect, …)` hands the render backend an **opaque WinUI
effect graph**. To do anything with it the backend must cast to `IGraphicsEffectD2D1Interop` and interpret a
Direct2D reflection protocol:

```csharp
Guid GetEffectId();                                   // which effect — by GUID
uint GetSourceCount();  IGraphicsEffectSource GetSource(uint i);        // children, recurse
uint GetPropertyCount();
void GetNamedPropertyMapping(string name, out uint index, out …);       // param name -> index
object GetProperty(uint index);                       // param value, boxed
```

Consequences:

- Every backend re-implements a full D2D effect interpreter. `SkiaEffectFactory` ≈ **1,500 lines / 235 interop
  calls**; `WebGpuBackend` carries its own parallel (partial) walk.
- It is not a pluggable contract: a new backend can't reasonably consume `IGraphicsEffect` — it would need to
  know every effect's GUID, every property's name/index/boxed-type, and the recursive-walk protocol.

## Insight

An effect graph is a DAG of operations over pixel inputs, and **every operation decomposes into primitives the
drawing session already has**:

| Effect kind | Examples | Reduces to |
|---|---|---|
| Per-pixel colour | ColorMatrix, Grayscale, Invert, Hue, Saturation, Sepia, Tint, Contrast, Exposure, Temperature | a **colour filter** applied while drawing the input |
| Compositing | Composite, Blend, CrossFade, AlphaMask | draw A, draw B with a **blend mode** |
| Generators | ColorSource, WhiteNoise, Border | `DrawRect(color)` / draw a texture |
| Inputs | image/surface brush, backdrop | rasterize via `RenderOffscreen(Paint)` / snapshot the backdrop |
| Spatial | GaussianBlur, DirectionalBlur, drop-shadow | a **blur** primitive (drop-shadow already exists: `DrawShadow`) |

The effect **graph** is nothing but the *order* those draws happen in. So the neutral layer can **evaluate** the
graph by issuing drawing-session + `RenderOffscreen` calls, and the backend never needs an effect concept at all.
(Uno already has a "recipe" fallback when `CreateEffectFilter` returns null — this makes the recipe the *only*
path.)

## Architecture

1. **Neutral `EffectNode` IR** (in `Uno.UI.Composition.Drawing`) — a closed, strongly-typed tree.
2. **Neutral parser** (Uno-internal) — `IGraphicsEffect` → `EffectNode`. All D2D reflection lives here, once.
   The backend never sees D2D, GUIDs, or boxed properties.
3. **Neutral evaluator** (in `CompositionEffectBrush.TryPaint`) — walks the tree and issues
   drawing-session / `RenderOffscreen` calls, fusing where possible (below).
4. **Backend** — no effect SPI. Provides only drawing primitives (almost all already exist).

### `EffectNode` set (initial)

```csharp
abstract record EffectNode;

// leaves
record BackdropInput : EffectNode;                                  // the live scene behind the element
record ColorInput(Color Color) : EffectNode;                        // ColorSourceEffect
record BrushInput(IEffectSource Source) : EffectNode;               // image/surface/noise brush

// per-pixel colour (carried as a colour matrix so a run fuses by matrix-multiply)
record ColorMatrixNode(EffectNode Source, ColorMatrix Matrix) : EffectNode;
// non-matrix colour (rare): gamma / arbitrary transfer curves
record TransferNode(EffectNode Source, /* per-channel curve */ …) : EffectNode;

// spatial
record BlurNode(EffectNode Source, float Sigma, bool ClampEdge) : EffectNode;

// compositing / multi-input
record BlendNode(EffectNode Background, EffectNode Foreground, BlendMode Mode) : EffectNode;
record CompositeNode(IReadOnlyList<EffectNode> Sources, CompositeMode Mode) : EffectNode;
record OpacityNode(EffectNode Source, float Opacity) : EffectNode;  // = a colour matrix, may fold into ColorMatrixNode
```

Unsupported D2D effects (see Caveats) parse to a logged "unsupported" marker, not a crash.

### `IEffectSource`

The leaf payload for a **brush/image input only**:

```csharp
Vector2? Size { get; }                                             // intrinsic size, if any
bool Paint(IDrawingSession session, float opacity, Rect bounds);   // render this input's pixels
```

`IsBackdrop` is gone (backdrop is its own node). The **evaluator** rasterizes it — `RenderOffscreen(size, s =>
Source.Paint(s, …))` — not the backend. `Paint` stays a callback (not a texture) because rasterization is
backend-specific and lazy: the backend owns the offscreen size/format, and a device texture can only be minted by
a backend, so the neutral layer hands "paint yourself" and lets the evaluator materialize a texture on demand.

### Fusion (matching the native filter-DAG's pass count)

The evaluator returns, per subtree, a **`Layer`** that is one of:

- a **deferred draw** — `{ how to paint the source, accumulated ColorMatrix }` — *not yet materialized*; or
- a **materialized `IImageTexture`** — produced only when a boundary forced it.

Rules:

- **Per-pixel colour runs fuse.** `ColorMatrixNode` over a deferred layer multiplies its matrix into the layer's
  accumulated matrix (exact, neutral, **no backend call, no offscreen**). A whole chain Grayscale→Tint→Contrast
  collapses to one matrix → one `CreateColorMatrixColorFilter` → one `DrawImage(input, filter)`.
- **Materialize only at genuine boundaries:** `BlurNode` (samples neighbours → its input must be rasterized),
  `BlendNode`/`CompositeNode` (needs its inputs as pixels), and `BackdropInput` (snapshot the live target).
  `Materialize(layer)` = if already a texture return it; else `RenderOffscreen(size, s => <issue the deferred
  draw + its accumulated colour filter>)`.
- **Colour after a boundary fuses into the draw-back:** a `ColorMatrixNode` above a `BlurNode`/`BlendNode`
  becomes `DrawImage(materializedTex, colourFilter)` — one draw, no extra offscreen.
- **Non-matrix colour** (`TransferNode`) can't fold into a matrix; compose it via a backend colour-filter
  `Compose`, or force a boundary. Rare.

Net: materialization points equal the native `SKImageFilter` DAG for realistic graphs. Acrylic
(backdrop → blur → tint → composite noise) = **backdrop snapshot + one blur offscreen**, tint fused into the
draw-back, noise composited by blend — same passes as today.

### Backend primitives

Already present (unchanged): `CreateColorMatrixColorFilter`, `CreateBlendModeColorFilter`,
`SaveLayer(colorFilter)`, `SaveLayer(blendMode)`, `DrawImage(…, colorFilter)`, `DrawRect`, `RenderOffscreen`,
`DrawShadow`.

New (small, well-defined — replace the entire effect SPI):

- **Blur** — blur an offscreen's pixels by sigma with an edge mode (clamp vs transparent/decal). Skia:
  `SKImageFilter.CreateBlur` / mask-blur; WebGPU: a separable blur pass. Exact surface shape TBD (a `DrawImage`
  overload, a `SaveLayer(blur)`, or an `IImageTexture`→`IImageTexture` op).
- **Backdrop snapshot** — capture the current render-target backdrop region as an `IImageTexture` (reading the
  live framebuffer — genuinely backend-side). Generalizes today's `DrawEffectBackdrop`.
- **(Optional) Compose colour filters** — for non-matrix `TransferNode` chains. Skia: `SKColorFilter.CreateCompose`.

### Deleted

- From the backend SPI: `IDrawingFactory.CreateEffectFilter`, `CreateDropShadowFilter`, `IEffectFilter`, and all
  backend consumption of `IGraphicsEffect` / `IGraphicsEffectD2D1Interop`.
- `SkiaEffectFactory` (~1,500 lines) and the WebGPU effect walk.
- `IDrawingSession.SaveLayer(IEffectFilter)` and `DrawEffectBackdrop(IEffectFilter)` → replaced by the blur +
  backdrop-snapshot primitives.

### Caveats

- **D2D lighting effects** (DistantDiffuse/Specular, PointDiffuse/Specular, SpotDiffuse/Specular) are genuine
  convolution / normal-map ops, not reducible to blur + colour + blend. They are effectively unused in XAML;
  parse them to the unsupported marker (log once) and drop, or add a dedicated primitive later.
- Deep **spatial** graphs materialize once per spatial node — identical to the native DAG; acceptable.
- The retained-rendering layer already records/replays the evaluator's draw calls, so evaluation is **not**
  per-frame — it re-runs only when the effect/bounds change (as `CreateEffectFilter` does today).

## Staged implementation plan

Each stage builds clean and is validated before the next; the effect SPI is deleted only at the end.

1. **IR + parser.** Add `EffectNode` records + the neutral `IGraphicsEffect → EffectNode` parser (extract the
   "read GUID + properties" half of `SkiaEffectFactory`), with unsupported-node logging. Unit-testable alone.
2. **Evaluator (parity behind a flag).** Add the neutral evaluator (with fusion) driving the existing drawing
   session; route `CompositionEffectBrush.TryPaint` through it behind a toggle, keeping `CreateEffectFilter` in
   place. Validate parity vs the old path (acrylic, `EffectBrushTests`).
3. **Backend primitives.** Add Skia **blur** + **backdrop snapshot**; point the evaluator at them.
4. **Delete the effect SPI.** Remove `CreateEffectFilter`/`IEffectFilter`/`SkiaEffectFactory` + the drop-shadow
   filter; make the evaluator the only path.
5. **WebGPU.** Implement the same two primitives for WebGPU; delete its effect walk; validate.

## Validation

- `Given_AcrylicBrush` on Skia desktop (X11), GL **and** forced-software.
- `EffectBrushTests` sample screenshot parity vs pre-change (exercises the full D2D effect set).
- Per-effect runtime tests where they exist.
- No public specs reference private trackers (repo-local, shareable).
