# Effects neutralization — evaluate the effect graph with drawing primitives

**Status:** Design. The backend `IDrawingFactory.CreateEffectFilter` / `IEffectFilter` SPI is replaced by a
**neutral evaluator** that walks the WinUI effect graph and issues drawing-session calls. **No intermediate
`EffectNode` IR** — the walk and the drawing are one pass.

## Problem

`CreateEffectFilter(IGraphicsEffect effect, …)` hands the render backend an **opaque WinUI effect graph**. To do
anything the backend casts to `IGraphicsEffectD2D1Interop` and interprets a Direct2D reflection protocol
(`GetEffectId` GUIDs, `GetSource` recursion, `GetNamedPropertyMapping`/`GetProperty` boxed values). Every backend
re-implements a full D2D interpreter — `SkiaEffectFactory` ≈ **1,500 lines**; WebGPU a parallel partial walk. It
is not a pluggable contract.

## Insight

An effect graph is a DAG of operations over pixel inputs, and every operation decomposes into primitives the
drawing session already has (colour filter, blend mode, `RenderOffscreen`, blur). So the **neutral layer walks the
graph directly and evaluates it into drawing-session calls**; the backend never sees an effect. No `EffectNode`
tree — the evaluator recurses over `IGraphicsEffect` and emits draws as it goes.

## The one subtlety: backdrop effects defer to present

`CompositionEffectBrush.TryPaint` runs during the **frame record** (the visual-tree walk), where the drawing
session is a *recorder*. The **backdrop** (the live scene behind the element) does not exist yet — it only exists
at **present**, when the recorded frame is composed onto the surface. Today this works because
`DrawEffectBackdrop(filter)` is a **recorded op**: the filter is baked at record time but applied to the backdrop
at present.

Consequence for the evaluator: it must emit **recorded, deferred drawing-session ops**, *not* immediate
`RenderOffscreen` textures (which would capture an empty backdrop at record time). Our retained-rendering layer
(the command-list / `SKPicture`) already records and replays these ops against the real backdrop at present. So
the evaluator is expressed entirely as **nested `SaveLayer` + draw** ops:

| Graph node | Evaluator emits (all recorded / deferred) |
|---|---|
| `ColorSourceEffect(color)` | `DrawRect(bounds, color)` |
| `GaussianBlur(backdrop)` | `SaveLayerBlurredBackdrop(sigma, clamp)` … `Restore` (layer starts from the blurred backdrop) |
| `GaussianBlur(image)` | `SaveLayerBlurred(sigma, clamp)` … draw source … `Restore` (content blurred on restore) |
| per-pixel colour (matrix) | `SaveLayer(colorFilter)` … draw source … `Restore`, or `DrawImage(src, colorFilter)` |
| `Composite`/`Blend` | draw A; `SaveLayer(blendMode)` … draw B … `Restore` |
| source-parameter → image brush | `source.Paint(session, opacity, bounds)` |

Everything is recorded; nothing reads the backdrop at record time. Image inputs *may* still use `RenderOffscreen`
when a texture is genuinely wanted, but the deferred `SaveLayer` form is the default and is what makes backdrop
effects work.

## Fusion

Per-pixel colour ops **compose**: a run collapses to one colour filter applied in one draw — almost every colour
effect is a colour matrix (incl. the tint = `sample*color` diagonal), so a run multiplies into a single matrix
(neutral, exact) → one `CreateColorMatrixColorFilter`. Extra `SaveLayer`s materialize only at genuine boundaries
(a blur layer, a blend/composite). So pass count matches the native `SKImageFilter` DAG; acrylic stays
"one blurred-backdrop layer + tint + noise composite". Non-matrix colour (Gamma/Linear transfer curves) is the
rare exception — compose via a backend colour-filter `Compose` or accept a boundary.

## Backend primitives

Already present: `CreateColorMatrixColorFilter`, `CreateBlendModeColorFilter`, `SaveLayer(colorFilter)`,
`SaveLayer(blendMode)`, `DrawImage(…, colorFilter)`, `DrawRect`, `RenderOffscreen`, `DrawShadow`.

New (small, deferred/recordable — replace the whole effect SPI):

- **`SaveLayerBlurred(sigma, clamp)`** — the layer's *content* is blurred on `Restore`. Skia: `SaveLayer` with a
  blur `SKImageFilter` in the restore paint.
- **`SaveLayerBlurredBackdrop(sigma, clamp)`** — the layer starts from the *blurred backdrop* (the acrylic case).
  Skia: `SKCanvasSaveLayerRec.Backdrop = CreateBlur`. Generalizes today's `DrawEffectBackdrop`.
- **(optional) Compose colour filters** — for non-matrix transfer chains. Skia: `SKColorFilter.CreateCompose`.

## Deleted

- Backend SPI: `IDrawingFactory.CreateEffectFilter`, `CreateDropShadowFilter`, `IEffectFilter`, and all backend
  consumption of `IGraphicsEffect` / `IGraphicsEffectD2D1Interop`.
- `SkiaEffectFactory` (~1,500 lines) and the WebGPU effect walk.
- `IDrawingSession.SaveLayer(IEffectFilter)` / `DrawEffectBackdrop(IEffectFilter)` → the blur-layer primitives.

## Caveats

- D2D **lighting** effects (Distant/Point/Spot Diffuse/Specular) are genuine convolution/normal-map ops, not
  reducible to blur+colour+blend; effectively unused in XAML — log once and render the source unmodified.
- The retained-rendering layer already caches the evaluator's recorded ops, so it re-runs only on effect/bounds
  change, not per frame.

## Plan

1. Add the two blur-layer primitives (`SaveLayerBlurred`, `SaveLayerBlurredBackdrop`) to `IDrawingSession` + the
   Skia session; keep the effect SPI for now.
2. Write the neutral evaluator (direct `IGraphicsEffect` walk → recorded `SaveLayer`/draw ops, with colour-matrix
   fusion) and route `CompositionEffectBrush.TryPaint` through it. Validate parity: `Given_AcrylicBrush` (GL +
   forced-software) and `EffectBrushTests` screenshots vs pre-change.
3. Delete `CreateEffectFilter`/`IEffectFilter`/`SkiaEffectFactory`/`CreateDropShadowFilter` + the
   `SaveLayer(IEffectFilter)`/`DrawEffectBackdrop(IEffectFilter)` overloads; evaluator is the only path.
4. WebGPU: implement the two primitives, delete its effect walk, validate.

## Validation

`Given_AcrylicBrush` (Skia desktop, GL and forced-software), `EffectBrushTests` screenshot parity, and per-effect
runtime tests where they exist.
