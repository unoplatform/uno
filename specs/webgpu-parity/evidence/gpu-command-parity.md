# WebGPU GPU-command parity — neutral vs ramez/webgpu-experiment

Method: an ordered GPU-command tracer (`UNO_WEBGPU_TRACE=1`) added to **both** backends emits one line per
render pass + pipeline draw. Both were driven with an **identical programmatic primitive scene** headless on
lavapipe (neutral via the smoke harness; ramez via `WebGpuTraceScenes` through the real `WebGpuDrawList` submit
path). Streams diffed line-by-line. `v=` is the draw's vertex count.

## Verdict per primitive

| Primitive | Draw sequence | Notes |
|---|---|---|
| solid rect | ✅ identical | `solid v=6` both |
| rect ×3 | ✅ identical | both coalesce 3→`solid v=18` |
| linear gradient | ✅ identical draw | `gradient v=6` — *but* neutral evaluates stops analytically (400 B uniform, no texture); ramez samples a 256×1 LUT texture (64 B uniform + tex). Same draw, different bound resources. Neutral caps at 16 stops. |
| radial gradient | ✅ identical draw | `gradient v=6` both |
| image | ✅ identical draw | `image v=6` — *but* neutral image uniform 112 B + premult (One/1-SrcA) blend; ramez 96 B + straight SrcOver. Both correct with their shader. |
| rect clip | ✅ identical | scissor-only, `solid v=6`, no mask draw either side |
| **path fill** | ⚠ different | neutral `path-stencil-**nz** v=3` + `cover v=6`; ramez `path-stencil-**eo** v=9` + `cover v=6`. **(a)** neutral honours the geometry fill-rule (non-zero here), ramez hardcodes even-odd. **(b)** fan triangulation differs (neutral minimal 1-tri = 3 v; ramez fans every edge incl. degenerate anchor edges = 9 v). Same filled result. |
| **rounded clip** | ⚠ different | neutral: **0 extra draws** — analytic rounded-rect SDF folded into the content draw's clip uniform. ramez: `clip-write(depth) v=6` + content + `clip-clear v=3` — writes a depth mask. Neutral leaner for one clip; ramez's depth approach is what lets it stack nested clips (neutral #18 gap). |
| path clip | ⚠ different (parallel) | both = stencil-the-fan → write depth mask → content. neutral `set1(fill) v=3`+`stencil-nz v=3`+`cover0 v=3`+`solid`; ramez `stencil-eo v=9`+`cover(depth) v=3`+`solid`+`clip-clear v=3`. Same mechanism, 4 draws each; differ in prep/cleanup order + winding + fan count. |
| save-layer opacity | ⚠ different | both render content to an offscreen then composite premult-over. neutral: 1 offscreen + `composite-srcover v=3` (fullscreen **tri**). ramez: an (empty) prefix pass + content offscreen + `layer-composite v=6` (fullscreen **quad**). |
| mask layer | ⚠ different | both: source→offscreen, mask→offscreen, composite masked. neutral folds mask via `composite-dstin v=3` then `composite-srcover v=3`; ramez uses a dedicated `mask-composite v=6` (source×mask.a). Same result, different decomposition. |
| **drop shadow** | ⚠ **materially different** | neutral: coverage = **silhouette** (`shadow-stencil`+`shadow-cover`) → **2** full-res separable blurs → composite as tinted **image**. ramez: coverage = the **rendered caster subtree** → **3**-pass blur pyramid → dedicated `shadow-composite` → **redraws the caster on top**. ramez shadows real content alpha; neutral shadows the path outline. |
| **backdrop / acrylic** | ⚠ **materially different** | neutral: **re-renders the command prefix** into an offscreen (O(n²)) → 2 full-res blurs → composite as image + solid overlays. ramez: blurs the **already-resolved target** (no prefix re-render) → 3-pass pyramid → single `backdrop-composite` shader (lum+noise+SDF) → tint. |

## Summary

- **Identical GPU command stream:** solid rect, rect coalescing, linear/radial gradient, image, rect clip (7/13).
- **Same draws, different bound resources (equivalent output):** gradient (analytic vs LUT), image (uniform size + blend).
- **Different but semantically equivalent:** path fill (winding + fan), rounded clip (analytic vs depth), path clip (order), save-layer / mask (composite decomposition).
- **Materially different (behavioural, not just encoding):**
  - **Shadow** — neutral = silhouette; ramez = rendered-content alpha + caster redraw. Different visual result for non-solid casters.
  - **Backdrop/acrylic** — neutral re-renders the prefix (O(n²)) and lacks noise; ramez samples the resolved target once + procedural noise. Perf + fidelity gap (audit #17).
  - **Path winding** — neutral honours fill-rule; ramez is even-odd-only.
  - **Rounded/nested clips** — neutral analytic single-rect; ramez depth-mask stack (audit #18).

None of the "different" rows are encoding bugs — they are the deliberate design divergences already tracked as
audit items #17 (acrylic), #18 (nested clips), #19 (gradient/glyph), plus the shadow-source and path-winding
choices. The core 2D primitives (rect/gradient/image/rect-clip) are byte-for-byte identical in what they submit.
