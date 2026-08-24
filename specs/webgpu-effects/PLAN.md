# WebGPU effect-graph evaluator — scoped plan

## Problem
The WebGPU backend has no general effect-graph evaluator. It realizes only:
- **Acrylic** (blur+tint over backdrop) — the one shape `WebGpuBackend.CreateEffectFilter` pattern-matches.
- **Single-source per-pixel colour** effects reduced to one 4×5 matrix (`CompositionEffectBrush.WebGpu.TryGetWebGpuEffectRecipe`):
  Grayscale, Invert, Hue, Saturation, Sepia, Temperature/Tint, Opacity, Exposure, LinearTransfer, ColorMatrix,
  LuminanceToAlpha, ColorSource.

Everything else — standalone blur on content, all multi-source Blend/Composite/Arithmetic/CrossFade/AlphaMask,
lighting, procedural noise, transform/border — makes the recipe return false and the paint path **renders nothing**
(`CompositionEffectBrush.skia.cs` TryPaint just returns). That silent drop is the thing to eliminate. Skia realizes the
whole set via `SkiaEffectFuser` (`SKImageFilter` tree) — it is the correctness reference.

## Neutral vocabulary (already parsed — `src/Uno.UI.Composition.Drawing/EffectNode.cs`)
Leaves: `SourceInput` (content/backdrop), `ColorInput`, `TextureInput(ExtendX/Y)`.
Effects: `ColorMatrix`, `Blur(Sigma,ClampEdge)`, `Blend(Background,Foreground,Mode)`, `Modulate(Color)`,
`LuminanceToAlpha`, `Contrast`, `LinearTransfer`, `GammaTransfer`, `CrossFade(A,B,Weight)`, `AlphaMask(Source,Mask)`,
`ArithmeticComposite(Bg,Fg,m,s1,s2,off)`, `WhiteNoise(Freq,Offset)`, `Lighting(distant/point/spot ×diffuse/specular)`,
`Composite(Sources[],Mode)`, `Unsupported(name,Source?)`. `EffectGraphParser` already produces these from the D2D graph.

## Architecture
Replace the two ad-hoc paths with ONE recursive evaluator in the WebGPU backend:

```
ITexture/WebGpuRenderSurface Evaluate(EffectNode node, Rect bounds)
```
Bottom-up: leaves rasterize to an offscreen surface (`SourceInput` = the content/backdrop already captured;
`ColorInput` = clear; `TextureInput` = the bound texture + edge-extend sampler); each effect node evaluates its
child(ren) to surface(s), runs one GPU pass into a fresh pooled `WebGpuRenderSurface`, returns it. The root surface is
composited into the frame. This reuses existing blocks: `RenderInto`, `WebGpuRenderSurface(pool)`, `BlurPyramid`/
`BlurPyramidRegion`, `DrawImage`, `ImagePipe`, and the fullscreen-triangle composite pattern. `CreateEffectFilter` +
`TryGetWebGpuEffectRecipe` collapse into this; **acrylic stops being a special case** — it's `Blend(tint, Blur(backdrop))`.
The "renders nothing" fallback is deleted.

## Phases (by reachability × reuse; each independently shippable + testable)

**Phase 0 — evaluator skeleton + leaves.** `Evaluate` dispatch, offscreen-per-node, `SourceInput`/`ColorInput`/
`TextureInput` leaves, root composite. Port `ColorMatrix` (single per-pixel matrix pass — the recipe's 4×5 already
exists) so the current colour-effect coverage routes through the evaluator with identical output. Retire the recipe.

**Phase 1 — single-source colour (finish the set).** `LuminanceToAlpha`, `Contrast`, `LinearTransfer`,
`GammaTransfer`, `Modulate` (premultiplied product with a colour) as per-channel colour passes. Fold consecutive
colour passes into one matrix where possible (perf); transfer/gamma/contrast are per-channel functions → one
`colour-transfer` shader with uniforms. Mirrors `SkiaEffectFuser` cases 59–320.

**Phase 2 — blur.** `BlurEffectNode` → `BlurPyramid` on the child surface (already implemented; just wire it as a
node, honoring `ClampEdge`). Fixes standalone blur-on-content (today a no-op).

**Phase 3 — multi-source blend/composite (the reachable blend-mode path).** Resurrect the general blend primitive
(the `CompositeBlend` shader from commit 6bdc79c, reverted in b6443e7) as a two-input `blend(bg,fg,mode)` pass →
covers `BlendEffectNode` + `CompositeEffectNode` (all 27 modes, no dst-copy dance needed here: both inputs are already
offscreen textures, so it's a plain two-sampler pass). Add `ArithmeticComposite` (m·s1·s2 + … linear pass),
`CrossFade` (lerp), `AlphaMask` (fg.rgb × mask.a). This is where the earlier blend-mode work truly belongs.

**Phase 4 — procedural + lighting (lowest priority, rare).** `WhiteNoise` (hash/value-noise shader),
`Lighting` (normal-from-alpha-gradient + distant/point/spot diffuse/specular shaders). Mirrors fuser cases 492–559.

**Unsupported (do in Phase 0).** `UnsupportedEffectNode` is emitted by the *parser* (`EffectGraphParser`) for D2D
effects it can't map — a shared, backend-neutral concept, not a WebGPU gap. Skia renders its `Source` unfiltered
(or nothing if source-less). WebGPU must match: `Evaluate(node.Source ?? blank)`. Trivial, so land it in Phase 0 —
that alone removes the blank-render for the "genuinely unmappable" class. Phases 1–4 then cover the nodes the parser
DOES produce and Skia DOES realize but WebGPU currently drops (Blur, Blend, Composite, Arithmetic, CrossFade,
AlphaMask, Lighting, WhiteNoise) — that, not "everything but colour", is the actual WebGPU-specific gap.

## Acceptance criterion: Skia≡WebGPU parity
The bar is **identical render output between Skia and WebGPU** for every graph the parser produces (the shared
"unsupported floor" — `SceneLightingEffect`, unmodeled D2D effects, odd ArithmeticComposite — stays dropped-to-source
on BOTH, so it's already at parity and is out of scope). Skia is the golden.

## Parity harness (build first)
The rendering backend is process-global, so parity is verified by running the SAME runtime test under each backend:
- New `Given_EffectBrush_Parity` runtime tests: build an `IGraphicsEffect` graph, put it on an element, screenshot
  (`UITestHelper`/`RawBitmap`), and `ImageAssert.HasColorAt` at points whose expected colour is computed from the
  effect definition (== what Skia produces). Both backends asserting the same expected values == parity with the spec.
- Run each test twice: Skia (default) and WebGPU (`UNO_WEBGPU=1`) on X11/lavapipe. A phase lands only when its tests
  pass on BOTH. One `[TestMethod]` per effect/mode; add fail-before/pass-after with each phase's commit.

## Source model (refines the phases)
Parser leaves are already textures (`TextureInput`, via `IDrawingFactory.RenderOffscreen`) except `SourceInput` (the
backdrop, deferred + captured at present time by the acrylic kind-6 path). So split by leaf kind:
- **Non-backdrop trees** (leaves = `TextureInput`/`ColorInput`) — the evaluator runs entirely on offscreen textures;
  no backdrop machinery. **Phases 0–4 target these first** (most effect brushes: effect-over-image/element).
- **Backdrop trees** (contain `SourceInput`) — keep the current acrylic path until a late phase folds backdrop capture
  into the evaluator (evaluate the sub-tree above the captured backdrop texture). Tracked, not Phase 0.

## Validation
- Per phase: render the same `IGraphicsEffect` graph on **Skia vs WebGPU** and diff pixels (Skia = golden). Harness:
  the `EffectBrushTests` sample + targeted runtime tests driving each effect; run under `UNO_WEBGPU=1` on X11/lavapipe.
- Regression: acrylic (`BasicAcrylicBrushTest`) + the existing colour-effect scenes must stay pixel-stable through the
  recipe→evaluator retire (Phase 0).
- Each phase is a separate commit with its fail-before/pass-after test.

## Reuse / risk
- Reuse: `RenderInto`, pooled surfaces, `BlurPyramid*`, `DrawImage`, `ImagePipe`, the reverted `CompositeBlend` shader.
- Perf: offscreen-per-node is the naive cost; mitigate with (a) colour-pass fusion (Phase 1), (b) region-limiting to the
  effect AABB (as the acrylic backdrop already does), (c) caching a static graph's surfaces (the arena/stamp pattern).
- Bounds/DPI: nodes evaluate in device pixels at the element AABB; match `EffectGraphParser` bounds semantics.
- Biggest risk: multi-input bounds/extend correctness (Blend/Composite with mismatched source rects) — cover with tests.

## Order to execute
Phase 0 → 3 first (0/1 remove the recipe + close colour; 2 is a quick win; 3 is the reachable blend-mode payoff).
Phase 4 last (rare, heaviest shaders). Each phase keeps Skia untouched (WebGPU-only), so no cross-backend risk.
