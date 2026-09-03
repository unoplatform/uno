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
| `GaussianBlur(backdrop)` | `DrawEffectBackdrop(blur, opacity)` (layer starts from the blurred backdrop) |
| `GaussianBlur(image)` | `SaveLayer(blur)` … draw source … `Restore` (content blurred on restore) |
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

## Backend primitives — no new verbs, just a neutral layer-filter parameter

`SaveLayer` and `DrawEffectBackdrop` already have the deferred-layer shape; the only problem is their parameter is
the opaque `IEffectFilter` (the backend-realized graph we delete). After the evaluator reduces colour →
colour-filters and composite → blend-modes (both already `SaveLayer` overloads), the *only* filter ever applied to
a layer is a **blur** (effects) or a **drop-shadow** (the non-analytic shadow path). Both are tiny neutral value
descriptors. So we change the parameter, not the verb:

- `SaveLayer(IEffectFilter)` → `SaveLayer(in LayerFilter)` — a neutral descriptor: a blur (σx, σy, clamp) with an
  optional offset + alpha-tint (a drop shadow *is* blur + offset + tint). Skia: `SaveLayer` with a blur/shadow
  `SKImageFilter` in the restore paint. This absorbs both the effect content-blur and the drop-shadow.
- `DrawEffectBackdrop(IEffectFilter, opacity)` → `DrawEffectBackdrop(in LayerFilter, opacity)` — the backdrop case
  (only ever a blur). Skia: `SKCanvasSaveLayerRec.Backdrop = CreateBlur`.

Already present and unchanged: `CreateColorMatrixColorFilter`, `CreateBlendModeColorFilter`,
`SaveLayer(colorFilter)`, `SaveLayer(blendMode)`, `DrawImage(…, colorFilter)`, `DrawRect`, `RenderOffscreen`,
`DrawShadow`. Optional: a colour-filter `Compose` for non-matrix transfer chains.

## Deleted

- Backend SPI: `IDrawingFactory.CreateEffectFilter`, `CreateDropShadowFilter`, `IEffectFilter`, and all backend
  consumption of `IGraphicsEffect` / `IGraphicsEffectD2D1Interop` — the drop-shadow params now flow straight into
  the neutral `SaveLayer(in LayerFilter)` descriptor, and the effect graph is evaluated, never realized.
- `SkiaEffectFactory` (~1,500 lines) and the WebGPU effect walk.
- The `IEffectFilter` overloads of `SaveLayer` / `DrawEffectBackdrop` → the neutral `LayerFilter` overloads.

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

## Resolution (adopted) — backend fuses the neutral tree

The pure-decomposition evaluator (neutral layer emits independent drawing ops) is **abandoned**: a headless
visual diff proved it can't reach parity, because **fusing non-separable blends over the backdrop is one combined
operation** that can't be decomposed into independent session ops. So the backend must do the combining.

Adopted shape — the `EffectNode` IR returns, but consumed by the **backend**, not evaluated by the neutral layer:

```
IGraphicsEffect --(Uno-internal parser, D2D reflection once)--> EffectNode tree --(backend fuses)--> IEffectFilter
```

- `CreateEffectFilter(EffectNode tree, …)` replaces `CreateEffectFilter(IGraphicsEffect …)`.
- The Uno-internal parser (`EffectGraphParser`) turns the D2D graph into the neutral tree — the backend never sees
  a GUID or a boxed property.
- The backend **combines** the tree into its native representation (Skia → one `SKImageFilter` DAG applied as the
  backdrop/layer filter, reusing today's `SKImageFilter`-building but reading params from tree nodes instead of D2D;
  WebGPU → its baked path). Fusion happens in the backend, so parity holds.
- `IEffectFilter` stays (the backend's fused result); `SaveLayer`/`DrawEffectBackdrop` keep applying it.
- `EffectGraphEvaluator` (pure decomposition) is deleted.

Net vs the original opaque-`IGraphicsEffect` SPI: the ~1,500-line per-backend **D2D interpreter** collapses to one
neutral parser + a per-backend tree→native fuser (no D2D); both backends get a clean typed tree.

### Node set (final)

`EffectNode` (public, in `Uno.UI.Composition.Drawing` so the backend SPI can take it):
- Leaves: `BackdropInput` (deferred live scene — the only non-texture input); `ColorInput(Color)`;
  `TextureInput(IImageTexture)` (a brush/image/noise input the neutral layer rasterized via `RenderOffscreen` at
  parse time — replaces `IEffectSource`/`BrushInput`).
- Ops: `ColorMatrixEffectNode(Source, float[] matrix)` (absorbs Opacity + every colour effect that is a 5×4
  matrix — Grayscale/Invert/Saturation/Sepia/Hue/Contrast/Exposure/Temperature/Tint — the parser computes the
  matrix); `BlurEffectNode(Source, σ, clampEdge)`; `BlendEffectNode(bg, fg, BlendMode)`;
  `CompositeEffectNode(sources[], BlendMode)`.
- Later, non-matrix ops as their own nodes (transfer curves, arithmetic-composite, cross-fade, border, noise,
  lighting).

`hasBackdrop` is **not** a parameter — the composition layer computes it from the tree
(`tree.Any(n => n is BackdropInput)`) to drive `RequiresRepaintOnEveryFrame`. SPI:
`IEffectFilter CreateEffectFilter(EffectNode tree, Rect bounds)`.

### Steps

1. Move `EffectNode` to `Uno.UI.Composition.Drawing` (public); add `TextureInput`; delete `EffectGraphEvaluator`.
2. Parser: `IGraphicsEffect` → tree, rasterizing non-backdrop brush sources to `TextureInput` via `RenderOffscreen`.
3. Add SPI `CreateEffectFilter(EffectNode, Rect)` **alongside** the old `(IGraphicsEffect,…)` (keep the old as the
   parity reference); Skia + WebGPU + command-list fallback implement it.
4. Skia fuser: tree → `SKImageFilter` (the existing `SkiaEffectFactory` building, reading node fields).
5. `CompositionEffectBrush`: parse → `CreateEffectFilter(tree, bounds)` → `DrawEffectBackdrop(filter)` behind the
   toggle; validate **pixel parity vs the old path** on `Given_AcrylicBrush` (GL + forced-software).
6. WebGPU fuser: tree → `WebGpuEffectFilter`; validate the same.
7. Broaden colour effects (matrices) for `EffectBrushTests` parity.
8. Make the tree path the default; delete the old `CreateEffectFilter(IGraphicsEffect)` + `SkiaEffectFactory`'s
   D2D reflection + `EffectGraphEvaluator`.

Parity is guaranteed by construction: the parser feeds the backend the same params the old code read from D2D, and
the fuser builds the same `SKImageFilter`s — validated screenshot-for-screenshot against the old path before the
old path is removed.

### Status (implemented)

Done and validated (Skia Desktop, headless X11/lavapipe):

- `EffectNode` is public in `Uno.UI.Composition.Drawing`; `EffectGraphEvaluator` deleted; `IEffectSource`/brush
  inputs are pre-rasterized to `TextureInput` via `RenderOffscreen`.
- SPI `IDrawingFactory.CreateEffectFilter(EffectNode, Rect)` implemented by Skia (`SkiaEffectFuser` → one
  `SKImageFilter` DAG, mirroring the legacy generators' backdrop-flag dance) and WebGPU (acrylic-shape extraction,
  identical to its legacy path).
- Parser neutralizes: GaussianBlur, ColorSource, Opacity, ColorMatrix, Blend, Composite, Grayscale, Invert,
  HueRotation, Exposure, Sepia, TemperatureAndTint, Saturation, Tint, LuminanceToAlpha, Contrast, LinearTransfer,
  GammaTransfer, Transform2D, CrossFade, AlphaMask, ArithmeticComposite, WhiteNoise.
- `CompositionEffectBrush` uses the tree path **by default**; a graph containing an `UnsupportedEffectNode` falls
  back to the legacy per-backend realization (no regression). `UNO_EFFECT_TREE=0` forces legacy (escape hatch).
- `hasBackdrop` computed from the tree (`ContainsBackdrop`), not reported by the backend.

Validation: `Given_AcrylicBrush` 5/5 on the default (tree) path — incl. `When_Drawn` 0.04 vs the WinUI reference and
the backdrop bounds-clamping test. A screenshot parity harness (temporary, not committed) confirmed the tree path is
**byte-identical** (0 differing pixels) to the legacy path across 17 colour/transfer/transform/composite effects
over a gradient source.

### Completed — full coverage, legacy interpreter deleted

- The six lighting effects are neutralized as one `LightingEffectNode` (kind discriminant + geometry the parser
  pre-computes via the neutral `EffectHelpers`).
- `BorderEffect` is modelled as an **edge-extend mode on `TextureInput`** (`EdgeExtend ExtendX/ExtendY`), not a
  dedicated node — Border isn't a transform, it's how a source is sampled outside its rectangle. The parser
  rasterizes the source at its intrinsic size; the fuser tiles/extends it (`CreateTile` / `CreateMatrixConvolution`).
- The legacy path is **gone**: `IDrawingFactory.CreateEffectFilter(IGraphicsEffect, resolver, out hasBackdrop)` and
  its Skia + WebGPU implementations, `SkiaEffectFactory` (the ~1500-line D2D interpreter), the dead
  `SkiaEffectHelpers` mode maps, the `UNO_EFFECT_TREE` escape hatch, and the `ContainsUnsupported()` fallback branch
  are all deleted. `EffectGraphParser` is the single place any Direct2D introspection happens.
- The SPI is now just `IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds)`; the drawing assembly no
  longer references `IGraphicsEffect`. An `UnsupportedEffectNode` (unbound source / unknown future effect) degrades
  to source-or-nothing.
- WebGPU realizes the acrylic shape from the tree; other effects return null → the existing recipe fallback.

Validated byte-identical (headless X11) to the pre-deletion legacy path across the full effect set — acrylic (runtime
`Given_AcrylicBrush` 5/5), 17 colour/transfer/transform/composite effects, the six lighting effects, and Border.

### Remaining (optional polish)

- `IEffectSource` is still `public` in `Uno.UI.Composition.Drawing` although it's now only a parser-internal adapter
  (the SPI no longer references it); it could move to `Uno.UI.Composition` as `internal`.

## Status (WIP)

- **Landed, green:** the primitive foundation (`LayerFilter`, neutral `SaveLayer(in LayerFilter)` /
  `DrawEffectBackdrop(in LayerFilter)` on all backends, the effect blend modes) and the neutral evaluator
  (`EffectGraphEvaluator`) — a direct `IGraphicsEffect` walk emitting drawing-session ops (backdrop blur, blend,
  composite, colour matrix, colour source, brush paint). Both are additive and **not yet wired** — `TryPaint`
  still uses `CreateEffectFilter`, so nothing is regressed.
- **Wired behind a toggle (`UNO_USE_EFFECT_EVALUATOR=1`, default off), blocked on one structural parity bug.**
  A headless visual diff (dump the `When_Backdrop` screenshot to PPM both ways) pinned it precisely: the old
  path fuses the *entire* acrylic graph (blur → luminosity blend → tint blend → composite noise) into **one**
  `SKImageFilter` and applies it as a single `SaveLayerRec.Backdrop`; the decomposed evaluator uses
  `DrawEffectBackdrop` (a `SaveLayerRec.Backdrop`) for the **blur alone** and layers the blends over the surface.
  For a backdrop blur feeding **non-separable** blends (`Color`/`Luminosity`), those aren't pixel-equivalent: the
  standalone backdrop blur bleeds neighbouring pixels (element renders grey-with-dark-edges 215/174 vs the
  correct uniform 247), and cropping the blur (`CreateBlur` crop rect **or** `SaveLayerRec.Bounds`, both tried —
  `LocalClipBounds` is correctly the element) does **not** stop it, because the backdrop the filter samples is the
  whole surface regardless of the output crop. The blends themselves are equivalent (`SKImageFilter.CreateBlendMode
  (mode, bg, fg)` ≡ draw bg + `SaveLayer(mode)` + draw fg), so the whole diff is this backdrop-input bleed.
  **Fix direction:** the backdrop input must be bounded to the element *before* blurring (draw the captured
  backdrop into a bounded picture, blur that — as the old `CreateEffectFilter` composed it), i.e. compose the
  backdrop-blur (and possibly the whole backdrop sub-graph) into one `SaveLayerRec.Backdrop` filter rather than a
  bare blur. That's a design step, not a one-liner.
- **Not started:** deleting `CreateEffectFilter`/`IEffectFilter`/`SkiaEffectFactory` (gated on evaluator parity),
  and the WebGPU non-separable-blend shaders (`Color`/`Luminosity`) the evaluator's decomposed acrylic needs.
