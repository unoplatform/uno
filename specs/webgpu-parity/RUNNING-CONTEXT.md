# WebGPU Backend — Running Context

**Purpose.** The single durable source of truth for the WebGPU drawing-backend parity effort. Conversation
compaction keeps dropping hard-won state; this doc is written to survive it, and is reconstructed partly from the
**full transcript** (`~/.claude/projects/-workspace-uno/*.jsonl`, ~469 human turns) because the memory files are a
lossy summary. **Update it as we go.** Complements the memory files (see "Pointers").

Last updated: 2026-08-10.

---

## 0b. FAITHFULNESS GAPS (user tested the reference vs neutral — reference is fast AND correct)

The user built the **reference (`ramez/webgpu-experiment`) win-x64 with the best combo defaulted on**
(`SamplesApp-Win-x64-WebGpu-RAMEZ-reference.zip`, via env defaults in its `Program.cs` Main) and found it **much
faster** than neutral AND **correct at 1.5× DPI** (neutral rendered the wrong size). So the port is NOT faithful and
the command stream is NOT identical. Two root causes:

1. **DPI bug — FIXED (`09f3091009`).** `WebGpuPresentSession.Scale/Save/Restore` were EMPTY STUBS. The composition
   records LOGICAL coords and applies `RasterizationScale` through the neutral present session (Save→Scale→Replay→
   Restore, `CompositionTarget.Rendering.skia.cs:206`), but the WebGPU session dropped it → logical size on a
   physical surface. Fixed: Save/Scale/Restore with a scale stack; `Replay` transforms the frame by the scale via
   `TransformFor` (scales geometry + clips; nested ReplayRefCmds keep their command-list ref so the cache still
   hits). Validated 73/73 (present.Scale(2) fills 2× region; Restore pops to 1×). **⚠ SYSTEMIC:** `Translate`/
   `Concat` on the present session are ALSO stubs — fine only because the composition never calls them at present-
   root, but audit for other stubbed seam methods.
2. **Perf gap — draw count (IN PROGRESS).** RenderDoc: 51% per-draw/state overhead across ~438 draws. Within-
   recording coalescing can't merge ACROSS recordings because arena baked each visual's transform into its per-op
   ClipU bind group → distinct bind group per visual → unmergeable. Full-faithful fix = ramez's frame-level geometry
   assembly (CPU vertex lists per kind, coalesced into range draws over shared per-frame buffers; SLAB = later
   partial-upload optimisation; transform TABLE = the per-vertex-index form for expensive/local geometry).
   - **DONE (`83a6f5ca85`) — shared solid buffer + cross-visual solid coalescing.** Every device-space solid run
     (immediate + **solid-only cached recordings**, e.g. Border backgrounds/fills) appends into ONE per-pass shared
     vertex buffer; adjacent solid ops sharing a clip bind group coalesce into one draw ACROSS visuals. Solid-only
     recordings re-emit each frame (ramez arena baseline) instead of caching per-visual buffers. Validated 75/75 +
     fail-first cross-visual scene (2 separate visuals → 1 solid draw). Also added a **per-draw-KIND profiler
     counter** (`draws=N[S.. P.. I.. G.. C.. Clip.. coal-..]`) so a real-scene run reveals which draws dominate.
   - **DONE (`be17641196`) — frame-solid path for MIXED recordings.** Generalized to any recording containing rects
     (Button = bg + border + glyphs): its solids re-emit into the shared buffer each frame; non-solids stay cached
     (device space, rebuilt on transform/clip change), consumed in draw order as the recording is re-walked. Pure
     non-solid recordings still take arena (moving-visual reuse). 78/78 + a mixed rect/path/rect interleave scene.
     ⚠ Only coalesces solids that end up ADJACENT in the op stream (interleaved solid/path/solid does NOT cross-merge
     — correct, matches ramez). ⚠ Trades a per-frame solid re-upload for static mixed/solid recordings (was fully
     cached) → SLAB (#20) removes that later. Watch `bufNew`/`upload` on the profile.
   - **NEXT (gate on the per-kind profile):** (a) if S dominates with a high `coal-` count → the shared-buffer work
     is the win, tune SLAB (#20) to kill the re-upload. (b) if P (glyphs/paths) dominates → **transform TABLE**
     (local verts + per-vertex u32 index, one shared storage binding) so glyph fans coalesce across visuals (task
     #28). The counter line `draws=N[S.. P.. I.. G.. C.. Clip.. coal-..]` on the user's real scene decides.
   - ⚠ Can't perf-validate here (lavapipe = software GPU); validation is draw-count via tracer + correctness via
     smoke. FPS proof needs the user's real HW — deliver a zip and read the `[webgpu-profile]` line back.
   - **ANALYTIC RRECT DONE (`e982c25bb7`, ported from ramez's per-vertex RoundedWgsl).** Rounded-rect FILLS now
     draw as one local-space SDF quad (transform-independent, no tessellation) instead of a path stencil+cover.
     Wired via a `RoundedRectFillHint` on the retained CompositionSpriteShape, set by BorderVisual backgrounds.
     **Re-diff across the 40 samples: neutral 9589 → 7830 draws = 1.59× → 1.30× vs ramez (−18%)**; path-cover
     3264 → 1505, rrect 0 → 1759. The shader ALREADY supports the border RING (InnerHalf); wiring the border stroke
     is the next easy win. ⚠ This changed the shipping Skia border fill to SKRoundRect (visually equal, but gate on
     screenshot CI). ⚠ Headless validated draw-count + smoke only; border pixel-placement needs a screenshot run.
   - **PRESENT-SESSION DE-STUBBED (`9048ca2eb8`) + dead-stub removed.** WebGpuPresentSession's immediate draw/clip/
     layer/matrix verbs were empty `{}` stubs → the built-in diagnostics FPS overlay (drawn on the present session
     after Replay via DrawFps) silently vanished on WebGPU. Now forwards all verbs to an internal recorder and
     composites over the frame at Dispose (LoadOp.Load pass; dedicated target now STORES MSAA so the load is valid at
     any sample count). Removed the dead throwing WebGpuGraphicsContext. **No behavior stubs remain in the WebGPU
     render path** (only genuine no-ops: recorder Dispose, EndContour). FPS overlay adds ~26 draws/frame — disable
     `DebugSettings.EnableFrameRateCounter` for parity captures (the harness does).
   - **BORDER RING DONE (`9368209575`).** DrawRoundedRectBorder seam verb: WebGPU = one analytic annulus SDF quad
     (rrect InnerHalf), Skia = outer round rect with inner Difference-clipped. BorderVisual border strokes wired.
     **6561 → 6401 (1.09× → 1.06× vs ramez)**; path-cover 1505 → 1369.
   - **RRECT COALESCING DONE (`ada7eb78c6`).** rrects now route through a per-pass shared buffer (immediate +
     re-appended cached via the frame-solid path) and coalesce adjacent same-clip ops. **Re-diff: 7830 → 7178 =
     1.30× → 1.19× vs ramez**; rrect draws 1759 → 1107. (2 rrect visuals → one `DRAW rrect v=12`.) Progress overall:
     9589 → 7178 (1.59× → 1.19×).
   - **QUALITATIVE per-sample diff (GridViewGrouped etc.) — biggest remaining divergence = CLIPS.** Neutral does a
     full per-clipped-visual path-clip depth protocol (`clipdepth-set1 v=3` + `clip-stencil-eo v=18` +
     `clipdepth-cover0 v=3` + draw + `clipdepth-set0 v=3` = 4 draws) ~10×/sample; ramez has ~none. Cause: rounded
     clips reaching the backend as **ClipPath** (even-odd rounded-rect fans) instead of the analytic **ClipRoundRect**
     (0 draws). BorderVisual's child clip is already analytic (`ApplyPostPaintingClipping` line 188); the culprit is
     the base `Visual.ApplyPostPaintingClipping` / `CompositionGeometricClip` (Visual.skia.cs:814 `session.ClipPath`).
     **NEXT (biggest lever): route rounded-rect clips through ClipRoundRect analytically** — needs identifying which
     visuals set the per-item rounded ClipPath and giving them the round-rect fast path. Then border ring (#30,
     shader-ready) and glyph coalescing (#28).
   - **REMAINING gap to ramez (1.30×→1.19×), from the re-diff kind counts:** (a) **border RINGS** still tessellate
     (path-cover 1505 incl. rings+glyphs) — ramez draws them as one rrect-ring (AddBorder); shader is ready, wire
     the stroke next. (b) **clip draws** neutral ~2420 vs ramez ~2036 (neutral's path-clip depth protocol is 3
     draws/transition). (c) **glyphs** ~parity now (transform table #28 for cross-visual glyph coalescing).
   - **FULL-UI DIFF ACROSS 40 REAL SAMPLES (2026-08-10) — the decider.** Local-only harness in both apps
     (`UNO_WEBGPU_SAMPLE_TRACE=N`) navigates each sample and dumps its real frame stream; see
     `evidence/fullui-samples-diff.md`. Result: **neutral = 1.59× ramez draws (9589 vs 6033; per-page avg 1.87×, max
     3.17×)** — NOT at parity on real UIs. Root causes, quantified: **(1 dominant) neutral has NO analytic
     rounded-rect FILL pipeline** — ramez draws every rect (square/rounded) as ONE `rrect` quad (1264 draws), neutral
     tessellates each rounded fill into `path-stencil-nz`+`path-cover` (2 draws) → ~2048 excess path draws. This is
     the #1 lever (WinUI rounds nearly everything). **(2) glyph fans not coalesced across visuals** → the transform
     TABLE (#28). **REPRIORITISED:** analytic **rrect fill** first (biggest, ramez-proven), then #28. This turn's
     solid coalescing is correct + matches ramez byte-for-byte, but `solid` is only 312/9589 draws — small lever.
   - **STREAM PARITY CHECKED (2026-08-10) against `evidence/ramez-trace-combo.txt`:** ramez-combo coalesces 3 rects
     → ONE `DRAW solid v=18`; neutral's coalesce-cached scene now emits the identical `DRAW solid v=18`, and the
     cross-visual scene (2 separate visuals) emits ONE `DRAW solid v=12` — ramez's v=6*N single-coalesced-solid rule,
     independent of how neutral decomposes work into recordings. Pinned by vertex-count asserts in the smoke
     (`30afa65a0c`). **Still to stream-diff:** mixed recordings' full sequence, and glyph/path cross-visual (needs
     the transform TABLE #28) — compare those when that increment lands.

## 0. RESOLVED CONFIG (authoritative — 2026-08-10)

**User's decision (verbatim):** "we only want the features that are from the best env-var combination. the output
command stream should be near identical." → The comparison baseline is **ramez WITH the best-perf combo ON**, and
**neutral must implement the same feature set** so the two GPU command streams come out **near-identical**. This
SUPERSEDES the earlier "arena/atlas OFF / converge-gaps-keep-wins" reading below.

**CORRECTED best-perf combo (user re-confirmed 2026-08-10, authoritative):**
`UNO_WEBGPU=1 UNO_WEBGPU_ARENA=1 UNO_WEBGPU_SLAB=1 UNO_WEBGPU_CACHE=1 UNO_WEBGPU_DIRTY=1 UNO_WEBGPU_COALESCE=1 UNO_WEBGPU_MSAA=1`.
Differs from the earlier-recorded "#420" in TWO ways: **MSAA=1** (not 2) and **NO GLYPHATLAS** (it was wrongly
included). Consequences:
- Neutral needs: **MSAA=1** (no-resolve 1× path — task `#15`, currently unimplemented; this now *agrees* with the old
  "drop MSAA" directive #421 rather than conflicting), **path/glyph coalescing** (COALESCE), **arena** (local verts +
  transform table, `#22`), **slab** (`#20`), **per-visual paint cache** (CACHE), **dirty-range upload** (DIRTY).
- **Glyph atlas is OUT** — matches #317 "all optimisations except glyph atlas" and keeps text as Skia-free paths
  (seam rules #56/#57 preserved). No atlas to port. 
- The stored `evidence/ramez-trace.txt` was captured with these flags **OFF** — **stale baseline**. Re-capture ramez
  with the corrected combo ON before diffing. `evidence/neutral-trace-current.txt` is the current neutral stream.
- Flag-INDEPENDENT gaps already validly identified (ramez's inherent approach, not env-gated): shadow
  (silhouette→caster-subtree, 2→3-pass blur pyramid, →single `shadow-composite` shader) and backdrop
  (re-render-prefix O(n²) → sample the already-rendered scene offscreen, 2→3-pass blur pyramid, →single
  `backdrop-composite` shader with luminosity + procedural noise). Ramez noise:
  `n = (fract(sin(dot(floor(pos.xy), vec2(12.9898,78.233))) * 43758.5453) - 0.5) * 2.0 * noiseOpacity`.
  Ramez blur pyramid: box-downsample ÷4 per pass until small (`step<0` ⇒ downsample), then a 9-tap gaussian H then V
  (`step = 1.5/dim`); first pass extracts just the region behind the acrylic from the full framebuffer.

<details><summary>Superseded earlier reconciliation (kept for history)</summary>

Two of your directives were in tension:

- **"Same GPU command stream, with the envvars set as explained before."** The env combo you pointed to (transcript
  #420, "this combination yielded the best performance") is: `UNO_WEBGPU_ARENA=1 SLAB=1 CACHE=1 DIRTY=1 COALESCE=1
  MSAA=2 GLYPHATLAS=1 NOTEXT=0` (+ logging vars). With those on, ramez uses **local-space verts + a transform-table
  (arena), a Skia glyph atlas, path/glyph coalescing, and 2× MSAA** — a materially different command stream from
  neutral.
- **"Converge gaps, keep wins"** (the bar you just chose): keep neutral's leaner/better approaches (analytic clip,
  analytic gradient, correct winding) — and neutral deliberately has **no arena/slab and no glyph atlas**.

So "byte-identical to ramez-with-#420-flags" would require porting arena + glyph atlas (contradicting the bar and
the earlier agreement to prune them). **RESOLVED (2026-08-10, on resume):** compare against ramez with the
**stream-shaping flags matched to what neutral implements** (arena/slab/glyphatlas OFF, since neutral doesn't do
them; coalesce already matches; MSAA per the drop-MSAA directive below), and treat arena/atlas as separate perf
features tracked (`#20`/`#22`) but NOT required for "same stream." This follows directly from the chosen bar
("converge gaps, keep wins") — arena/atlas are wins-to-track, not stream-parity requirements. Parity comparison uses
ramez with arena/slab/glyphatlas OFF. Remaining push targets the genuine gaps: `#18` nested clips, `#17` backdrop,
drop shadow, `#421` drop-MSAA.

</details>

Related standing directives from the transcript that aren't yet done:
- **#421: "drop MSAA, implement the missing optimizations and turn them on by default, delete/turn off the rest."**
  Neutral still defaults to 2×/4× MSAA — the drop-MSAA (DPI-aware 1× / no-resolve, task `#15`) is NOT done.
- **#317: "do all the optimisations (except the glyph atlas)… full faithful port."** vs **#419: "slab/arena not
  improving or even making worse… prune."** Net working decision (my analysis, not explicitly ratified): port the
  optimizations that measurably help on-by-default; prune arena/slab; defer the glyph atlas (fights Skia-free text).

---

## 1. Goal & branches

Make Uno's **neutral drawing-backend abstraction** (`feature/drawing-backend-abstraction`) reproduce the original
**`ramez/webgpu-experiment`** WebGPU renderer faithfully, performantly, cross-platform. Validation philosophy
(#302): **two independent implementations of everything — one Skia-based, one non-Skia** — so the abstraction is
proven, not just shaped.

- **Neutral checkout:** `/workspace/uno` (branch `feature/drawing-backend-abstraction`).
- **Ramez checkout:** `/workspace/uno-webgpu-orig` (detached `ramez/webgpu-experiment`, commit `17618c34df`).
- **Binding layers differ:** neutral migrated to the **modern webgpu.h ABI** (wgpu-native v29.0.1.1 via
  `Uno.WebGpu.Native`, Silk.NET discarded — #385-387); **ramez uses Silk.NET.WebGPU 2.23**. Same wgpu family,
  different managed bindings + wgpu-native versions — keep in mind when diffing raw behaviour.

---

## 2. Key locations

**Neutral backend:** `src/Uno.UI.Composition.WebGpu/WebGpuBackend.cs` (~2900 lines: `WebGpuDevice`, texture/buffer
pools, `WebGpuRenderSurface`, `WebGpuProfiler`, **`WebGpuTrace`** (ordered tracer), `WebGpuCommandRecorder`,
`WebGpuPresentSession` = `RunFrame`/`RenderInto`/`ApplyDepthClip`/`RenderShadow`/`BlurPass`, gradient packing,
pipelines/WGSL). `WebGpuSwapChainContext.cs` (native present: render→owned offscreen resolve→blit into acquired
swapchain image; a direct resolve into a surface texture does NOT composite). `wgpu-native.targets` (per-RID native
fetch). `Uno.UI.Composition.WebGpu.Smoke/Program.cs` (headless lavapipe pixel+trace harness). X11 head:
`Skia.X11/Hosting/X11XamlRootHost.cs:432` + `Skia.X11/Rendering/X11SoftwareGraphicsContext.cs:131` (Xlib surface).
Win32 head: `Skia.Win32/Rendering/Win32WindowWrapper.Rendering.cs`.

**Ramez backend:** `Skia.X11/Rendering/WebGpu/WebGpuDrawList.cs` (~2100; `DrawScene`/`RenderSegmented`),
`Rendering.cs` (`RenderToRgba`/`WebGpuTargets`/pipelines), `WebGpuContext.cs`, `WebGpuVertexSlab.cs`,
`Composition/IWebGpuDrawList.cs`, `Visual.WebGpu.skia.cs`, `WebGpuBrushPainter.skia.cs`. Present:
`X11WebGpuRenderer.cs` (offscreen readback + `XPutImage`; no swapchain on lavapipe). **My parity harness
(throwaway, in the ramez tree):** `WebGpu/WebGpuCmdTrace.cs` + `WebGpu/WebGpuTraceScenes.cs` (invoked from ramez
`SamplesApp.Skia.Generic/Program.cs` under `UNO_WEBGPU_TRACE_SCENES=1`, returns before host; Win32 csproj excludes
`WebGpuTraceScenes.cs` from its linked glob to avoid an ambiguous type at the shared call site).

**Evidence (durable, in this repo):** `specs/webgpu-parity/evidence/`: `gpu-command-parity.md` (per-primitive
table), `neutral-trace.txt`, `ramez-trace.txt` (the two captured ordered command streams), `transcript-humans.txt`
(all 469 human turns — the authoritative instruction history). The 2.2 MB `transcript-assistant.txt` (my analysis)
stays in the session scratchpad; re-derive from the full transcript if needed (see §12).

**Prior validated binary:** `/workspace/SamplesApp-Win32-WebGpu-win-x64.tar.gz` (win-x64, in-pass depth clip).

---

## 3. Environment variables

**Production config goes through host builders, NOT env vars** (#178). The `UNO_WEBGPU_*` vars are experimental/dev
toggles + logging.

**Neutral:** `UNO_WEBGPU=1|neutral|swapchain` (enable head) · `UNO_WEBGPU_PROFILE=1` (per-frame profiler +
`[webgpu] backend init`) · **`UNO_WEBGPU_TRACE=1`** (ordered command trace `PASS/DRAW/PASS end` — the parity
instrument) · `UNO_WEBGPU_MSAA=1|2|4` · `UNO_WEBGPU_PIPELINE=1` (non-blocking poll) · `UNO_RENDER_PERF=1`.

**Ramez:** `UNO_WEBGPU=1` + feature flags. **The "best-perf combo" you referenced (#420):**
`ARENA=1 SLAB=1 CACHE=1 DIRTY=1 COALESCE=1 MSAA=2 GLYPHATLAS=1 NOTEXT=0` (+ `RENDER_PERF`/`GPUTIME` = logging).
Flag meanings: `ARENA` local verts + transform table; `SLAB` per-visual stable vertex slices; `CACHE` per-visual
paint cache; `DIRTY` dirty-range vertex upload; `COALESCE` path/glyph fan coalescing (generic adjacent-same-kind
coalescing is unconditional); `GLYPHATLAS` Skia-rasterized glyph atlas; `NOTEXT` skip text; `GPUTIME` per-pass GPU
timing; `CLIPOPT` (default on) incremental clip masks. **My parity harness adds** `UNO_WEBGPU_TRACE=1` +
`UNO_WEBGPU_TRACE_SCENES=1`.

**lavapipe headless (both):** `DISPLAY=:99` (`Xvfb :99 -screen 0 1600x1200x24 &`),
`VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/lvp_icd.json`, `WGPU_BACKEND=vulkan`, `LD_LIBRARY_PATH=.`.

---

## 4. Build & run recipes

Native libs needed next to the dll: `libwgpu_native.so` (v29) + `libSkiaSharp.so` (**4.148**, not 3.x) +
`libHarfBuzzSharp.so` (linux-x64). `-p:NuGetAudit=false` is REQUIRED for SamplesApp builds (a new advisory on
`System.Security.Cryptography.Xml` trips NU1903-as-error). WASM SkiaSharp emdawnwebgpu path uses 3.1.56 (#385).

- **Neutral smoke:** build `Uno.UI.Composition.WebGpu.Smoke` (net10.0, Debug); run under lavapipe env +
  `UNO_WEBGPU_TRACE=1`. NOTE: rebuilding the smoke does not always rebuild the referenced backend project — build
  `Uno.UI.Composition.WebGpu` explicitly after backend edits.
- **Neutral SamplesApp X11 head:** `dotnet build SamplesApp/SamplesApp.Skia.Generic/…csproj -c Release
  -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true -p:NuGetAudit=false`; run headless with `DISPLAY=:99`
  + lavapipe env + `UNO_WEBGPU=1`.
- **Ramez SamplesApp + trace driver:** same build (in `/workspace/uno-webgpu-orig/src`); run with
  `UNO_WEBGPU=1 UNO_WEBGPU_TRACE=1 UNO_WEBGPU_TRACE_SCENES=1`.
- **WASM SamplesApp:** `dotnet run` (not publish — much faster) for iteration.

---

## 5. Seam design rules (from the full transcript — these are load-bearing constraints)

- **No record/replay verbs in the abstraction** (#41, #87): too SKPictureRecorder-coded. Retained drawing is
  modeled cleanly (backends may retain per-Visual data via `IRenderData`), not via explicit record/replay verbs.
- **Public SPI, NO InternalsVisibleTo** (#47, #114, #120): the Skia impl and any third-party impl get the *same*
  public surface. No IVT shortcuts.
- **Skia is fine for non-rendering** (font/image *decoding*) but **rendering must be neutral** (#56). The neutral
  "glyph draw" is just **path drawing** (glyphs happen to be paths) (#57); emoji/color glyphs come through as
  **images** (#96, #256-257: one method returns a positioned glyph OR a positioned IImage).
- **Each backend knows its own types** (#129-130): Skia is NOT the factory; the WebGPU backend creates its own
  `IShader`/etc. concrete types. `IDrawingFactory` is the per-backend factory.
- **We create the context+window (paired), not the backend** (#144-146, #233-241): `CreateWindow` returns window+
  context together; the context doesn't own the window. Backend is handed a **render-target view** — surface-vs-
  texture is deliberately abstracted away (drawing always targets a view) (#146). Draw to a texture then blit to the
  window surface (dirty-rectangles) is an internal choice, invisible to the seam.
- **No default backend** (#139): must be set explicitly (so Skia can eventually be dropped for size). Registration
  is via host builders (#135, #178).
- **Requirements fail loudly on software** (#218-219); the graphics *kind* is coupled to its requirements (#220).
- **Explicit verb parameters, not a PaintParams grab-bag** (#73-85): each draw verb takes what it needs; explicit
  overloads per scenario. Shadow is one effect/verb (SDF/maskfilter detail is per-backend, #82-83).
- **Color matrices are color filters; effects graph is known upfront** (#248-249): no builder, no SkSL/D2D interop
  leakage; the neutral effect set mirrors WinUI's public surface (not Win2D/D2D internals, #188-191).
- **IFontProvider** (renamed from IFontManager, #299): registered via builder, one interface across all platforms,
  external users can supply their own (#330-331). Metrics/shaping/tables live in `IFont` (#176, #255-256).
- **SkiaSharp-exposing API → GLCanvasElement** on its own context/framebuffer (extra copy, acceptable) (#367).
  **HarfBuzz stays** (everyone depends on it). Zero-native-deps is NOT the goal (#173).
- **Single command encoder per frame** (#416): faithful port of ramez's optimizations but one encoder, not many.
- **Composition is Skia-less**: SkiaSharp package refs removed from Composition; `BorderVisual` and most composition
  made cross-runtime, not skia-only (#187, #369-370).

---

## 6. Retained recordings, GeometryCache, interleaving (architecture)

- **Retained recordings + GeometryCache:** `Visual._content`/`_childrenContent` are `IRenderData` recordings.
  `Replay(recording)` emits a `ReplayRefCmd` keyed by the recording's immutable command-list reference;
  `WebGpuDevice.GeometryCache` builds/caches that recording's GPU geometry once. **Deficiency:** it stores
  device-space (transform-baked) verts → a transform change rebuilds+re-uploads. Correct design (= ramez arena,
  `#22`): store **local-space** verts on `WebGpuRenderData` (the backend-concrete `IRenderData` — the right home,
  not the neutral `Visual`) + transform via a **uniform/transform-table** → moving a visual = one uniform write.
- **Interleaved build+draw:** `RenderInto` builds ops up-front but renders nested offscreens (layer/backdrop/shadow)
  eagerly during the build loop. Not forced by the seam; a two-phase design (build-all→upload-once→draw-all,
  deferring offscreens) is seam-compatible and is what a persistent slab (`#20`) needs.
- **In-pass depth-mask path clips (shipped):** depth = clip mask (content DepthCompare=GreaterEqual, z=0, cleared
  0 = no clip); stencil = winding. Replaced per-clip offscreen coverage textures (the old Cov146/242 perf disaster).

---

## 7. GPU-command parity — methodology & status

**Method:** ordered tracer (`UNO_WEBGPU_TRACE`) in both backends (`PASS`/`DRAW`/`PASS end`), driven by an identical
programmatic primitive scene headless on lavapipe (neutral = smoke; ramez = `WebGpuTraceScenes`), diffed. Trace
catches STRUCTURAL divergence (passes/pipelines/draws); DATA divergence (uniform/vertex contents → wrong pixels) is
caught by the smoke's Skia cross-checks. **Comparison so far ran ramez with arena/slab/glyphatlas OFF** (what
neutral matches — see §0 reconciliation).

| Primitive | Verdict | Note |
|---|---|---|
| solid rect, rect×3 coalesce | ✅ identical | `solid v=6` / coalesced `v=18` |
| linear + radial gradient | ✅ identical draw | neutral analytic (≤64 stops) vs ramez 256-LUT — KEEP analytic |
| image | ✅ identical draw | uniforms 112B/premult vs 96B/srcover — equivalent |
| rect clip | ✅ identical | scissor-only |
| path fill | ⚠ KEEP (neutral correct) | neutral `stencil-nz` honours fill-rule + 3-v fan; ramez `stencil-eo`-only + 9-v fan |
| rounded clip | ✅ analytic + nests (`#18` done) | neutral 0 draws (SDF), now a 4-deep rounded-rect array ANDed in `clipCov` — nests correctly, still 0 extra draws |
| path clip | ≈ parallel | both stencil→depth-mask, 4 draws, differ in prep/cleanup order |
| save-layer / mask | ≈ equivalent | offscreen+composite; tri(v=3) vs quad(v=6); mask decomposition differs |
| **drop shadow** | ❌ GAP → converge | neutral silhouette + 2 full-res blurs + tinted-image; ramez rendered-subtree + 3-pass pyramid + shadow shader + caster redraw |
| **backdrop / acrylic** | ⚠ partial (noise done) | luminosity+grain now one composite (`411e616045`); remaining deltas = 3-pass blur pyramid + O(n²) prefix |

**Fresh diff vs ramez WITH the corrected combo** (`evidence/ramez-trace-combo.txt`, 2026-08-10, MSAA=1 both sides):
the combo makes ramez's PRIMITIVE scenes a single `PASS main` (same as neutral) — rect/gradient/image/clip all match
draw-for-draw. Remaining structural deltas are concentrated in effects and are dominated by ONE recurring thing:
- **3-pass blur pyramid** (ramez `offscreen-ss:blur ×3`) vs neutral **2-pass** full-res gaussian — appears in BOTH
  shadow and backdrop. THIS is the highest-value remaining structural converge (one change fixes both).
- Shadow: ramez also renders the caster subtree as source + redraws it (silhouette-vs-subtree, seam-level).
- Rounded clip: neutral analytic = 1 draw; ramez depth-mask = 3 draws (`clip-write`/`solid`/`clip-clear`). Neutral is
  leaner — a deliberate near-miss, NOT converging backward.
- ARENA/SLAB/CACHE/DIRTY don't change the traced PASS/DRAW structure on these scenes (buffer-management) — they'd
  show only as vertex/uniform upload deltas, invisible to the PASS/DRAW tracer.

---

## 8. Decisions log

- **Convergence bar = "converge gaps, keep wins"** (2026-08-10): fix nested clips / backdrop / shadow; keep
  analytic clip/gradient + correct winding as documented exceptions. (See §0 tension with the #420 combo.)
- **Path winding: LEAVE** — neutral honours geometry `FillRule` (nz→Inc/DecWrap); ramez even-odd-only. Neutral correct.
- **Optimizations:** faithful port EXCEPT glyph atlas (#317); prune arena/slab (env-gated, uncertain, #419);
  **drop MSAA + missing-opts-on-by-default** (#421, NOT yet done). Arena transform-table survives as `#22`.
- **Env config for the comparison:** unresolved (§0) — currently arena/slab/glyphatlas OFF on ramez.

---

## 8b. This-session convergence (2026-08-10, committed on `feature/drawing-backend-abstraction`)

Corrected the comparison config to the authoritative best combo (§0), then converged in committed increments:
- `fbac454d2b` fix(webgpu): build guard (wgpu-native.targets OutDir).
- `064b860cfc` feat: nested rounded clips (`#18`) + wide gradients + cmd tracer.
- `411e616045` feat: acrylic luminosity+grain baked into one composite (noise gap closed; `#17` partial).
- `24ae56b9d5` feat: **MSAA=1 no-resolve** path (`#15`, part of the best combo) — validated 56/56, every pass
  `msaa=1`, default 4× unaffected. Swapchain-1× is code-review only (WSI can't run headless — needs real-HW check).
- `e6bf954646` feat: **3-pass blur pyramid** shadow+backdrop (`#17` partial) — matches ramez `offscreen-ss:blur ×3`.
- `b35b18e11e` test: rebuild-vs-reuse trace + moving-visual scene (arena calibration harness).
- `ffc96daa5d` feat: **ARENA v1** (`#22`) — transform-safe (solid/image, no-clip) recordings re-stamp instead of
  rebuild on move; moved frame traces `geometry-reuse`; 58/58. Perf calibrated via the stream (user's principle).
- `0db9f3b5a8` feat: ARENA phase-2 — clipped solid/image (`finv` in `clipCov` + moved scissor); 60/60.
- `0e816a1074` feat: ARENA phase-3 — gradients (`finvMap` in the gradient fragment); 62/62.
- `3d0f068f17` docs: paths-arena attempted + reverted (wgpu exclusive auto-layout blocker; needs shared explicit
  pipeline layout or a per-op stencil bg). Arena final coverage = **solid + image + gradient, clipped or not**.

**13 commits total this session.** Full arena for the common moving-visual cases, stream-verified; paths-arena +
SLAB/DIRTY + path-COALESCE remain (each needs a specific refactor or harness — see §8d).

**FULL ARCHITECTURE PORT (2026-08-10 pt 2, after the user's "port it fully / match the command stream"):**
- `8c5f92d09e` perf: `clipCov` no-clip fast path + static-index unroll (fixes a REAL regression found on the user's
  Intel GPU — RenderDoc showed 51% of GPU time in per-draw/state/barrier overhead across ~438 draws; the frame is
  DRAW-COUNT bound, not fragment bound).
- `1e2cb4fa3e` refactor: **explicit shared `ClipU` pipeline layout** (ramez-faithful) — one bind group binds to
  solid/cover/stencil. This is the foundation the earlier paths-arena attempt lacked (wgpu auto-layouts are
  pipeline-exclusive). Re-added the stencil `ClipU`/xform binding. 62/62, no behaviour change.
- `2eaf77ad9c` feat: **paths-arena** — arena now covers **solid+image+gradient+PATH**, clipped or not; a moved path
  reuses its stencil fan + cover. 64/64.
- `083f43dfa6` perf: **coalesce rect runs in cached recordings** (`BuildCoalesced`) — cached Borders/backgrounds
  collapsed from N draws to 1 (the immediate path already coalesced; cached didn't). Direct draw-count cut for the
  Intel bottleneck. 67/67 + a trace check proving 3 rects → 1 `DRAW solid`.

- `4df8826d78` perf: **path/glyph fan COALESCE** — a run of consecutive non-zero same-colour+clip paths (a text
  run) merges into ONE stencil + ONE cover; N glyphs collapse from 2N draws to 2. Even-odd excluded (overlap→hole).
  70/70 + a trace check proving 2 paths → 1 stencil + 1 cover.

**COALESCE now cuts BOTH draw-count sources the RenderDoc showed** (rect runs + path/glyph fans) in cached
recordings. **Still to port (matching ramez fully):** transform TABLE (ramez binds one storage buffer + per-vertex
index; neutral uses per-op ClipU xform — same DRAW stream, fewer bind switches — perf-only, doesn't change the
trace), SLAB (persistent per-visual vertex slice), DIRTY (dirty-range upload). Confirm MSAA=1 on real HW. The
draw-count reductions (rect + path coalescing) are the direct fix for the measured Intel 51%-per-draw bottleneck —
re-capture the RenderDoc/profiler on the next Windows build to confirm the draw count dropped.

**PASS/DRAW command stream is now NEAR-IDENTICAL** for every traced primitive + effect scene vs the corrected-combo
ramez capture (`e6bf954646` blur pyramid closed the last recurring structural delta). Converged this session:
MSAA=1, acrylic single-composite + grain, shadow+backdrop 3-pass blur pyramid, nested clips, wide gradients.

**Remaining deltas (all either neutral-better or NOT visible in the PASS/DRAW tracer):**
- Rounded clip: neutral analytic = 1 draw vs ramez depth-mask = 3 draws. **Neutral leaner — deliberate near-miss, do NOT converge backward.**
- Shadow source: ramez renders the caster subtree + redraws it (silhouette-vs-subtree). Seam-level; neutral's
  silhouette is equivalent for a shape's drop shadow. Low priority.
- Backdrop O(n²): only bites with MULTIPLE stacked backdrops (re-renders the prefix per backdrop). Single-backdrop
  stream already matches. The fix is the multi-pass frame split (sample the rendered scene) — big rearchitecture,
  perf-not-stream for the common single-backdrop case.
- **COALESCE / CACHE / DIRTY / ARENA / SLAB** are buffer-management (vertex/uniform *upload* + transform re-stamp).
  They do NOT change the traced PASS/DRAW/pipeline structure — they're a **perf** axis, not a command-stream axis.
  To measure them needs a perf harness or richer scenes (text/coalesce), not the PASS/DRAW tracer.

**So: the "near-identical command stream" goal is essentially MET for the traced scenes.** Further #420 features are
perf optimizations that won't move the PASS/DRAW diff. Next-step options: (a) perf-profile arena/slab on a heavy
scene; (b) richer scenes (text) to expose COALESCE deltas; (c) the structural near-misses above.

## 8d. ARENA v1 DONE (committed `ffc96daa5d`, 2026-08-10)

Arena implemented for **transform-safe recordings** (solid/image draws, no clip). `ClipU` gained an NDC→NDC affine
`xform`/`xoff`; every color vertex shader routes through `xformPos` (identity for immediate draws). `ArenaXform(t) =
A·T·A⁻¹` (A = device→NDC map). Arena-safe recordings build once at identity and, on move, re-stamp the per-op clip
bind groups with the new xform and REUSE the vertex buffers. **Acceptance test PASSES:** moving-visual moved frame
now traces `geometry-reuse(cache-hit)` (was `geometry-rebuild(transform-changed)`), moved rect lands correctly,
58/58 smoke. Matches ramez's build-once stream → perf calibrated by the stream, per the user's principle.

**Arena phase-2 DONE (committed `0db9f3b5a8`):** extended to rect/rounded-**clipped** solid/image visuals (the
scrolling-card case). Added `finv` (inverse device affine) to `ClipU`; `clipCov` maps the moved fragment back to the
recording's own space so a clip baked at identity stays correct after the move; the device-space **scissor** Aabb is
transformed by the replay transform (a local scissor was masking the moved rect away — caught fail-first). Validated:
rounded-clipped child replayed translated reuses geometry + lands correctly (center red, corner masked), 60/60.

**Arena phase-3 DONE (committed `0e816a1074`):** gradients too — the gradient fragment now routes its device
position through the shared `finvMap`, so a moved gradient's geometry (baked at identity) follows the re-stamp.
`GradientCmd` is arena-safe; 62/62 (moving linear gradient stays aligned to the moved geometry).

**Arena coverage now: solid + image + gradient, clipped or not, re-stamp on move (build-once).** Remaining:
- **Paths** — ATTEMPTED + reverted (`3d0f068f17` documents it). Adding a ClipU/xform binding to `PosOnlyWgsl` (the
  stencil pass) hit wgpu's **exclusive auto-layout**: `Validation Error … Exclusive pipelines don't match` — a bind
  group from the cover pipeline's auto-layout can't bind to the stencil pipeline. Paths-arena needs EITHER a second
  stencil-layout bind group per op (expand the 7-field op tuple → 8, across 11 `ops.Add` sites) OR a **shared
  explicit ClipU pipeline layout** across solid/cover/stencil (the cleaner fix). Deferred — solid/image/gradient
  arena is unaffected (those kinds don't use the stencil pass).

  **ROOT CAUSE of the divergence from ramez (confirmed by reading ramez source):** it's a pipeline-layout choice.
  Ramez creates **explicit pipeline layouts** (`DeviceCreatePipelineLayout`, Rendering.cs:132) and its arena is a
  **global transform-table storage buffer bound once at group 0, SHARED across the stencil + cover pipelines**
  (WebGpuDrawList.cs:1414/1516), with each **vertex carrying an index** into the table. Neutral uses **`layout:auto`**
  on every pipeline (auto-derived BGLs are pipeline-EXCLUSIVE) and folds the xform into the **per-op ClipU uniform**.
  Per-op-uniform works for solid/image/gradient (one pipeline per draw) but a path is TWO pipelines (stencil+cover)
  that must SHARE the xform bind group — auto-layout forbids that. The ramez-faithful fix: explicit shared ClipU (or
  transform-table) layout for at least the stencil/cover pair.
- **SLAB (`#20`)** persistent per-visual vertex slice + **DIRTY** dirty-range upload — refinements on top of arena;
  they change buffer *allocation*, NOT the PASS/DRAW/UPLOAD trace, so they can't be stream-validated headless (only
  the profiler's `bufNew`/`upBytes` shows them). **COALESCE (`#21`)** path/glyph fan merge needs a multi-path scene.

**⚠ BUILD LESSON:** the smoke's incremental build can go STALE (a Program.cs edit didn't recompile; a scene silently
vanished while reporting 0 errors / ALL PASS). Always force `-t:Rebuild` (or `touch Program.cs`) when validating a
smoke edit, and sanity-check the PASS count changed. (Banked alongside the "rebuild the backend explicitly" lesson.)

**⚠ PERF REGRESSION (found on real HW, fixed `8c5f92d09e`):** a 60fps sample dropped to ~34fps (poll/GPU-bound,
`offscr=0 bufNew=0 bg hit=310/miss=0` = fully-cached static scene, so NOT arena re-stamp — pure per-fragment shader
cost). Two this-session shader changes on the SHARED path hit EVERY fragment even when the feature was inactive:
(1) `clipCov` ran `finvMap` (arena inverse xform) UNCONDITIONALLY — unclipped fragments used to hit a near-free
`return 1.0`; (2) `#18` replaced the static `clip.rect` with a `clip.rects[i]` DYNAMIC uniform-array index in a loop
(a GPU perf cliff). Fix: `n==0` fast path (no finvMap) + unroll to STATIC indices (`roundCov` helper). **LESSON: any
addition to a shared vertex/fragment shader costs GPU time on every draw even when the feature is off — always gate
it behind a fast path / static access, and validate perf on real HW (lavapipe `poll` is software-GPU, not
representative).** Still-elevated CPU `render`≈8.6ms may partly be `MakeClipBg` now clearing+packing 56 floats
(was 16) per draw for the bind-group key — secondary, revisit if the GPU fix doesn't fully restore 60fps.

## 8c. ARENA design (implemented in 8d; kept for reference)

**Goal:** make the `GeometryCache` transform-independent so a moved visual re-stamps a transform uniform instead of
rebuilding geometry. **Acceptance test (ready):** the smoke `moving-visual frame2 (moved)` trace line must change
from `UPLOAD geometry-rebuild(transform-changed)` to `UPLOAD geometry-reuse(cache-hit)` — with 56/56 still passing
and the rect landing at the moved position.

**Vertex-space facts:** `_m` (recorder matrix, Map) bakes commands to device at record; `Ndc()` bakes device→NDC at
build; all vertex shaders are pass-throughs (`o.p = vec4(pos,0,1)`, pos already NDC). The cache key includes
`Transform`, so a move → cache miss → full rebuild.

**Design (lowest-churn):** fold a 2D-affine `xform` into the existing **ClipU** uniform (already bound to every
color pipeline, already re-stamped per op via `MakeClipBg`). Steps:
1. Extend `ClipU` (176→~240B): add `xform` (mat, NDC→NDC affine). Vertex shaders do `o.p = applyXform(clip.xform, pos)`.
2. Build cached ops at the recording's OWN space (`TransformFor(..., identity)`) → `pos = Ndc(child-local)`.
3. Per-entry `xform = A·T·A⁻¹` embedded affine, where `T = rr.Transform`, `A` = the device→NDC map (surface size),
   so `finalNDC = A·T·A⁻¹·pos + (b − A·T·A⁻¹·b)`. On move: recompute `xform` + re-stamp the clip device coords
   (both live in ClipU) → one uniform write, verts reused. Drop `Transform` from the rebuild condition.
4. Immediate (non-cached) draws: `xform = identity` (pos already final NDC) — no behaviour change.
5. **Also re-stampable on move (device-space uniforms):** gradient geo (`Grad`), image quad, shadow/backdrop
   placement. For the solid-rect acceptance test only ClipU matters; gradient/image re-stamp are follow-ups.
**Risk:** every draw's vertex position now routes through `clip.xform` — a bug mis-positions everything. Validate the
full 56/56 (not just the moving scene) + eyeball the trace. Do it as its own careful pass, not rushed.

SLAB/DIRTY build ON arena (persistent per-visual vertex slice + upload only the changed uniform range). CACHE is
already the `GeometryCache`. COALESCE (path/glyph) is orthogonal and needs a multi-path scene to show in the trace.

## 9. Work log

**Acrylic composite + grain converged (2026-08-10, committed `411e616045`):** the image shader gained an acrylic
branch (`op.w`) that samples the blurred backdrop, applies `mix(blurred,lum)`, and adds ramez's procedural grain in
ONE draw (was blurred-image + separate luminosity overlay); rounded corners still via `clipCov`. `WebGpuEffectFilter`
gained `Noise` (0.02 default for parsed acrylic). Validated 56/56, incl. a flat-backdrop noise scene fail-before
(Noise=0→variance 0)/pass-after (Noise=0.12→60). Remaining backdrop gaps: 2→3-pass blur pyramid, region-bounded
blur, and the O(n²) prefix re-render (needs the multi-pass frame split — see §10).

**Done & validated (lavapipe pixel + trace):** ordered tracer in both backends + full per-primitive comparison;
**build fix** (`wgpu-native.targets` Copy guarded `And '$(OutDir)' != ''` — MSB3023 in outer multi-TFM pass; neutral
Linux SamplesApp builds again); **gradient >16-stop clamp fixed** (`MaxGradientStops` 16→64, offsets parameterized,
WGSL arrays 64/16, replay-origin index derived; fail-first 20-stop smoke check; ALL PASS);
**`#18` nested rounded clips** (rounded-rect array ANDed in `clipCov`; fail-first `nested-rounded` scene; 55/55).

**Prior (memory `webgpu-oom-and-parity-audit.md`):** VRAM OOM fixed (MSAA/depth pool reuse ~6GB→~700MB); pool
eviction + device teardown; swapchain double-free on resize; native swapchain blit; in-pass depth-mask clips
(Cov146/242→0); opaque-acrylic short-circuit; SaveLayer(IColorFilter) matrix on solid fills; radial-gradient exact
under rotation; frame profiler. Benchmark vs master (#340-363, PR #23526): ~parity at 700 buttons; the *webgpu*
path is "unusably slow" vs the ramez branch (#451) — the core perf problem driving the audits.

**Lessons banked:** gradient tests MUST set `WebGpuShader.LocalMatrix = Matrix3x2.Identity` (default is all-zeros,
not identity → `t=0` → looks like "all stops broken"; cost a long false-alarm chase). Rebuilding the smoke doesn't
reliably rebuild the backend project. Present-timer measures vsync-wait, not CPU (a 29-vs-21fps "regression" was a
vsync fluke).

---

## 10. Remaining work (priority under "converge gaps, keep wins")

1. ✅ **DONE — Nested rounded clips (`#18`)** (2026-08-10). `ClipRoundRect` now pushes onto a `RoundClip[]` (cap 4,
   copy-on-write) that `clipCov` ANDs per-fragment; `ClipU` WGSL is `rects[4]/radii[4]/ex/ctrl/size` (176B, `ctrl.x`
   = count — note `meta` is a WGSL reserved word). Path-exclude split out of the rounded channel (`PathExclude`, was
   conflated in `HasExclude`). Fail-first smoke `nested-rounded` (outer r=20 / inner r=4, corner (12,12)) red→black;
   55/55 lavapipe pixel checks pass, 0 extra draws. **Still limited (rare):** ≥2 *arbitrary path* clips — the depth
   channel is single-slot, so nested paths keep only the AABB for the outer ones (documented; ramez stacks masks in
   depth via CLIPOPT — a bigger rework if it ever matters).
2. **Backdrop/acrylic (`#17`)** — stop O(n²) prefix re-render; sample the resolved scene-so-far (needs a
   resolve-then-sample step — single-MSAA-pass can't `LoadOp.Load`); add downsample pyramid + procedural noise.
3. **Drop shadow** — content-vs-silhouette; matching fully is a seam-level change (bracket a subtree); at least
   match the blur structure.
4. **Drop-MSAA default (`#15`, directive #421)** — DPI-aware 1× / no-resolve variant.
5. **Analytic rounded-rect/border** (`rrect`/border pipelines; neutral tessellates). Bigger; lower urgency.
6. Tail: `#14` bounds-size offscreens, `#19` gradient local-space/effect-routing/glyph fixes, `#20` persistent slab
   + dirty upload, `#21` general coalescing + scissor dedup, `#22` arena transform-restamp (see §6 deficiency).

---

## 11. Working style & constraints

- **Autonomy mandate (repeated, emphatic):** never stop / no gaps / no multi-part / commit as you go / use best
  judgement / "I'm stepping away" (#263, #303, #313, #315, #318, #384, #389). BUT the debugging-discipline rule
  still holds: accurate validation labels (Code review vs Compile vs Runtime), root-cause before guards.
- **When explaining abstractions: one at a time**, dependency-ordered (#60-61, #213-214).
- **Local-only, NEVER commit:** Msal exclusion in `ItemExclusions.props` (#445); `crosstargeting_override.props`
  (gitignored); SamplesApp FPS-counter edit in `App.xaml.cs` + the `Diag/` sample; the ramez-tree parity harness.
- Commits: Conventional Commits, end with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## 12. Pointers (memory files under …/memory/)

`webgpu-oom-and-parity-audit.md` (VRAM fix + parity audit + this session), `webgpu-backend.md` (89 KB deep
narrative), `todo-webgpu-arena.md` (`#22`), `backend-vision-roadmap.md`, `drawing-backend-doc.md` (keep
`doc/uno-drawing-backend-abstraction.md` current), `composition-skia-decoupling.md`, `managed-*.md`,
`x11-render-backend-verification.md`, `runtime-tests-headless-display.md`. Index: `MEMORY.md`.
Full transcript: `~/.claude/projects/-workspace-uno/22bf45a7-7591-4492-abc1-14cbeb854d70.jsonl` (extract human turns
+ assistant text, skip tool_result noise — see the python in this session's history).

---

## 13. Session 2026-08-10 (resident slab + button-cache root cause)

Commits this session (branch `feature/drawing-backend-abstraction`):
- `0b45a6ea8d` fix(webgpu): resident cross-recording solid/rrect slabs — device-global `SolidSlab`/`RrectSlab`
  (`WebGpuSlab` over `WebGpuVertexSlab`), keyed by recording; static recording's slice stays resident (no
  re-tessellate/upload) and coalesces across recordings. `BeginFrame`/`EndFrame(RetainOnly live)` in `Replay`.
- multi-instance fix (same commit lineage): a recording replayed >1×/frame at different transforms can't share one
  resident slice (2nd `Put` would overwrite) → each repeat emission gets a fresh **transient** slice via a
  per-pass `frameEmitted` set; first emission keeps the stable slice. Fixed the smoke "retained frame1" regression
  (was: both replays drew at the 2nd offset). Smoke **86/86**.
- `d4e8efcb79` fix(webgpu): make rounded-rect recordings GPU-cacheable — **root cause of the button perf gap**:
  `IsCacheable` admitted rect/path/image/gradient but NOT `RoundedRectCmd`, so every Border/Button recording was
  non-cacheable → re-tessellated inline every frame. Frame-solid/arena/`TransformFor`/`HasReappendable`/
  `BuildSimpleOp` already handle rrect; this was the one gate left when analytic rrect landed.

**Headless evidence (lavapipe, `Performance_1000ButtonsContinuousRendering`, 100 buttons, via local `UNO_WEBGPU_HOLD`
harness in App.xaml.cs):** steady state `FS: build=1 hit=25 nonSolidRebuilt=0` per 19-frame window ⇒ the 100 buttons
collapse to ~1 recording, **built once, re-emitted from the slab every frame** (glyphs+gradients resident, 0 rebuild).
`bg(hit=422 miss=0) bufNew=0`, `render≈8ms`. **CAVEAT on the fps number:** ~19fps here is lavapipe **software-present**
bound (`present≈43ms`, `acquire≈38ms`) — NOT CPU/GPU; `render≈8ms` is the real work. fps on this box is not
representative of real HW. `alloc≈3.5MB/f` persists even fully resident and is ~identical with/without the rrect fix ⇒
almost certainly composition-layer walk (shared with ramez), not backend geometry — **verify** before optimizing.

**Interpretation:** the "per recording, not cross-recording" caveat the user was angry about is closed — geometry is
now resident in device-global slabs across recordings, multi-instance is correct, glyphs resident on hit. The pre-
pipelining 32fps was already fixed by `UNO_WEBGPU_PIPELINE` default-on (user confirmed). Remaining real-HW perf gap vs
ramez (if any) is unproven — needs either a user real-HW re-test or a **ramez-tree A/B on this same lavapipe box**
(compare `render`/`upload`/`alloc`, ignore `present`). Transform-table for cheap MOVES (scroll re-stamp, `#22`/`#28`)
is the remaining residency gap (static is resident; a moved visual still rebuilds its slice — fStale on transform
change). Local-only: `UNO_WEBGPU_HOLD` + `RunWebGpuHold()` harness in App.xaml.cs (never commit).

---

## 14. Session 2026-08-11 — ramez A/B (the "why slower" answer) + #31 root cause

Built ramez's SamplesApp.Skia.Generic (added `UNO_WEBGPU_HOLD` harness mirror, local-only) and ran the SAME
`Performance_1000ButtonsContinuousRendering` scene on the SAME lavapipe box. Metric = CPU (`present`/`readback` is
lavapipe noise, differs by tree).

| phase (per frame, 100 buttons) | neutral | ramez |
|---|---|---|
| CPU build/op-walk | **~7 ms** | ~2 ms |
| CPU pass-encode | 0.65 ms | 0.74 ms  ← **already at parity** |
| present/readback | ~38 ms (swapchain) | ~36 ms (readback) — both lavapipe-bound |

**Finding: the pass-encode is already ramez-parity. The whole gap is the ~7ms op-build.** Split via `UNO_RENDER_PERF`
counters (all local diagnostics, since reverted): `build≈7 encode≈0.65 clip≈2.5(n=412 MakeClipBg/frame)
direct(rr=101 grad=102) replayRef=138(fs=26 arena=112)`.

Two causes, both now understood:
1. **Arena per-op clip re-stamp** — the arena path called `MakeClipBg` per op every frame. FIXED
   (`ad5ee6418b`, stamp memoization): a static arena visual now reuses its stamped ops (0 MakeClipBg). Didn't move
   THIS scene (only 9 arena visuals) but is a real win for text-heavy/scroll scenes.
2. **#31 — the dominant one (still open).** `CACHEREJECT` diag: `GradientCmd+PathFanClip=105, RectCommand+PathFanClip=32`.
   The 100 button backgrounds are a gradient (or solid) on a rounded-rect **Shape** with a **Stretch** → 
   `Shape.skia.cs:51` sets `_geometryTransform` to a **scale** → `CompositionSpriteShape.skia.cs:111` gates the
   analytic rrHint on `_geometryTransform.IsIdentity`, so a scaled shape's non-solid fill falls to
   `ClipPath(fillGeometry)` (a tessellated PATH-FAN clip, line 133). `IsCacheable` rejects any recording with a
   `PathFan` clip (per-frame pooled coverage), so **every button background is non-cacheable → inlined at top level →
   re-tessellated + re-encoded every frame** (the 101 direct rrect + 102 direct grad, ~412 MakeClipBg/frame). THIS is
   the ~5ms. (Border *backgrounds* via `RectangleClip`/`BorderVisual.ClipRoundRect` are already analytic — the leak is
   Shape-with-Stretch fills.)

**#31 fix (next):** in `CompositionSpriteShape.Paint`, when `_geometryTransform` is a pure scale/translation (not
rotation/skew), apply it to the hint rect+radii and use `ClipRoundRect`/`DrawRoundedRect` instead of the path.
SUBTLE: a non-uniform scale turns circular corners **elliptical** → the clip case (`ClipRoundRect`, RoundRectangle
has Vector2 per-corner radii) can represent it, but the solid `DrawRoundedRect` takes scalar Vector4 radii (circular
only) — and the WebGPU `ClipData.Rounds`/analytic-clip must be confirmed to handle elliptical radii before relying on
it. It's a SHARED-composition-layer change (affects all Skia targets), so validate with Skia runtime tests + smoke,
not just WebGPU. Expected payoff: button backgrounds become cacheable → resident → neutral build drops toward ramez's
~2ms, closing the real-HW gap.

Commits this session: `0b45a6ea8d` resident slab, `d4e8efcb79` rrect cacheable, `ad5ee6418b` arena stamp memo.

---

## 15. Session 2026-08-11 (cont.) — #31 investigation: the fork

Goal: make the 100 button gradient backgrounds cacheable (they clip via a path fan → `IsCacheable` rejects →
inlined → re-tessellated every frame = ~5ms of the ~7ms build). Chosen approach (user pick): analytic elliptical
rounded clips.

DONE + COMMITTED: `3aefa152ab` — WebGPU analytic clip now supports **elliptical** corners (per-axis RadiiX/RadiiY
through RoundClip + ClipU `radiiY` array (288B) + a gradient-normalised implicit-ellipse SDF in `roundCov` that
degenerates EXACTLY to the circular formula). Smoke 89/89 incl. a new elliptical (rx=24,ry=8) scene cross-checked vs
Skia. This is the enabling infra + an independent correctness win (ClipRoundRect previously forced circular).

THEN — attempted the composition wiring (set RoundedRectFillHint on rounded Shapes; route non-solid fills to analytic
ClipRoundRect folding a scale+translation geometry transform, elliptical). REVERTED — did NOT help, because a
decisive diagnostic (`UNO_DIAG_SHAPECLIP`) showed the button gradient backgrounds are:
`comment= geom=CompositionPathGeometry fill=CompositionLinearGradientBrush bounds=0,0,35,31 hintSet=False` — 90× at
35×31. **They are NOT `Rectangle`s** (confirmed: `RECTARRANGE` never logs a 35×31 gradient rect) and NOT BorderVisual
backgrounds (those set the hint + are already analytic). They are **`Path` shapes with rounded-rect `Data`** (empty
comment = Release; Shape sets `#path` only in DEBUG). `CompositionSpriteShape.Paint` non-solid branch clips the fill
to `fillGeometry` via `ClipPath` → path fan → non-cacheable.

**THE FORK (needs a decision):** the analytic-hint approach can't generically fire for a `Path` — its `Data` is an
arbitrary geometry, so we can't cheaply know it's a rounded rect. Options for the actual residency fix:
  (a) **Make path-fan-clipped recordings cacheable** — persist the clip's fan geometry WITH the recording (owned)
      instead of the per-frame pooled coverage that `IsCacheable` rejects. General (fixes ALL path-clipped content,
      incl. these Paths) and doesn't need analytic clips; keeps the depth-mask clip (slower per-draw than analytic
      but RESIDENT, which is the win). Likely the highest-leverage, most general fix.
  (b) **Introspect Path `Data`** for a rounded-rect signature and set the analytic hint — narrow, brittle.
  (c) Find WHY the button template uses a `Path` (not a Border) for its rounded gradient background and change the
      style/rendering to a Border (already analytic) — may be an Uno control-template detail.
The committed elliptical clip (a) is orthogonal and stays regardless.

Local diagnostics used (`UNO_DIAG_SHAPECLIP`, `UNO_RENDER_PERF` build/encode/clip/direct/CACHEREJECT counters, the
`UNO_WEBGPU_HOLD` harness) were all reverted/removed from the backend; only `UNO_WEBGPU_HOLD` remains in App.xaml.cs
(local-only).

---

## 16. Session 2026-08-11 (cont.) — #31 approaches all REGRESS; path-fan is fine

Explore-agent finding: the 100 button "gradient backgrounds" are actually the **elevation border RING**
(`ControlElevationBorderBrush`, a LinearGradientBrush) drawn by `BorderVisual._borderShape` (a rounded-rect ring
`CompositionPathGeometry`). `RoundedRectBorderHint` is ALREADY set on it, but `CompositionSpriteShape.Paint`'s
non-solid branch only consulted the FILL hint (`rrHint`), never `brHint` → a gradient ring fell back to
`ClipPath(ring)` = path fan. (The default button *fill* is a SolidColorBrush, already analytic.)

Tried all three greenlit approaches; measured each on lavapipe (RENDERPERF `frameMs` = CPU op-build+encode,
`1000ButtonsContinuous`=100 buttons, steady-state avgMs):
- **Baseline (current committed):** cmds=344, **avgMs 9.7** ← best.
- **(1) allow PathFan in IsCacheable** (make path-clipped recordings cacheable): cmds 344→243 but **avgMs ~14** —
  REGRESSION. The depth-mask path clip is redrawn every frame regardless, so caching removes no per-frame cost and
  churns clip transitions.
- **(2) analytic annulus for gradient border ring** (consume `brHint`: outer ClipRoundRect ∩ inner Difference):
  cmds 344→243 (path fans gone, cacheable) but **avgMs ~14.5** — REGRESSION. The 2-round per-fragment clipCov over
  the full outer bounds + the now-cacheable recordings rebuilding cost more than the path-fan on this scene.
Both reverted. Only kept: the corrected IsCacheable comment (`fd92428c3d`) and the elliptical clip infra
(`3aefa152ab`, orthogonal correctness win).

**Conclusion / honest state:** the #31 premise ("route rounded clips to analytic → faster") does NOT hold for the
button scene on lavapipe — the path-fan depth-mask is actually cheaper, and making the recordings cacheable doesn't
help because the clip work is per-frame regardless. The residual neutral-vs-ramez CPU gap (~9.7ms vs ~2ms build) is
therefore NOT the clip strategy; it's the **per-frame op-list rebuild architecture** (138 ReplayRefs + 206 direct
primitives re-walked + MakeClipBg/BuildSimpleOp + tuple-adds every frame) vs ramez's more persistent draw-list.
Closing it fully = a persistent draw-list rework (big), OR confirm on REAL hardware whether the gap still matters
after the 5 committed residency/cache fixes + pipelining (lavapipe CPU numbers may not reflect real HW). Caveat that
recurs: lavapipe `frameMs` includes some GPU-sync cost; treat ±3ms as noise and prefer real-HW confirmation.

NOTE for next session: `RoundedRectBorderHint` is set but unused for non-solid (gradient) border rings — a latent
inconsistency. If a persistent draw-list ever lands, revisit the analytic annulus there (it's correct, just not a
win under per-frame-rebuild + software fragment shading).

---

## 17. Session 2026-08-11 (cont.) — persistent-draw-list refactor + GPU command diff vs ramez

Commit `ff66691163`: moved the per-recording compiled GPU draw-list from the device-global `GeometryCache` dict
(keyed by command list, swept every frame) ONTO `WebGpuRenderData` (the `IRenderData` = "backend-defined retained
state", per its own doc). `ReplayRefCmd` carries the render data; replay reads/writes `Data.Compiled` directly.
Dispose (UI thread) → Interlocked-exchange the field, enqueue to a ConcurrentQueue drained on the render thread at
BeginFrameResources (mirrors `_pendingTextures`). Slab slices reclaim via RetainOnly. Smoke 89/89; CPU **neutral**
(avgMs ~10, same as before) — the dict/eviction weren't the bottleneck. This is the persistence FOUNDATION (state on
the handle), not yet the cheap-replay win.

**GPU command-stream diff (button scene, neutral vs ramez, lavapipe, same combo):**
| draw kind | neutral | ramez |
|---|---|---|
| gradient (border rings) | 102 | 102 |
| glyph path-stencil | 112 (nz) | 112 (eo) |
| glyph path-cover | 112 | 112 |
| rrect | 125 | 114 |
| border-ring CLIP draws | ~420 (clipdepth-set1/set0/cover0 + clip-stencil-eo) | ~508 (clip-clear 203 + clip-path-stencil-eo 102 + clip-path-cover 102 + clip-write(rounded) 101) |

**VERDICT: GPU streams are structurally the same** — same gradient (102) + glyph (112/112) counts, comparable rrect
(125 vs 114) and clip magnitude. **Crucially ramez ALSO path-clips the border-ring gradients** (clip-path-stencil/
cover) — it does NOT use an analytic annulus, and emits MORE clip draws than neutral. This CONFIRMS the earlier
empirical result: analytic clips were never ramez's lever (that's why approach-2 regressed). The neutral↔ramez gap is
therefore **NOT GPU and NOT the clip strategy — it is pure CPU emission**: neutral re-tessellates + rebuilds the
non-cacheable path-clipped border rings every frame; ramez keeps their fans RESIDENT (slab) and re-emits cheaply via
the persistent draw-list. Closing it = make the path-clipped border-ring recordings resident + cheap to re-emit
(NOT analytic). NOTE approach-1 (allow PathFan caching) regressed earlier — needs the real profiler to see whether
those cached recordings were rebuilding (stale) or the fan re-tessellation moved rather than disappeared.

**Minor GPU divergences to check:** glyph fill winding is **nonzero (neutral) vs even-odd (ramez)** — same for
non-self-intersecting glyphs, differs for overlapping contours (worth verifying against WinUI). rrect 125 vs 114.

CPU parity: NOT yet achieved (neutral RenderInto ~10ms vs ramez build+encode ~2.7ms). The user's next step (real
profiler on both branches) will pinpoint the exact emission hotspot. win32 WebGPU logic comparison: agent running.

---

## 18. Session 2026-08-11 (cont.) — win32 WebGPU comparison + CPU profiling

**Win32 WebGPU logic vs ramez (agent audit).** BIG finding: **ramez's X11 host has NO wgpu surface/swapchain at
all** — it renders offscreen, reads pixels back to CPU (RGBA→BGRA), and `XPutImage`s to the window
(`X11WebGpuRenderer.cs`, rationale: lavapipe WSI present is unreliable). So the neutral **Win32 swapchain path**
(`WebGpuSwapChainContext.cs`: `WGPUSurfaceSourceWindowsHWND` → `wgpuSurfaceConfigure`(Fifo) → per-frame
`wgpuSurfaceGetCurrentTexture` at present time → fullscreen-triangle GPU blit of the offscreen resolve target →
`wgpuSurfacePresent`) is **genuinely new, with no ramez counterpart to match** — its comment about "mirroring the
original branch's proven Win32 swapchain" is unsubstantiated (ramez avoids the swapchain). Reviewed standalone it's
internally consistent; the historical **resize double-free is correctly guarded** by `_ownsColor=false` on the
external-color surface (`WebGpuBackend.cs`), so Dispose frees the borrowed swapchain view exactly once. No bug found
in surface/configure/present/resize.
- **Area 5 (adapter/device) — the one real divergence, now FIXED (`aff27a299f`):** neutral's device ctor spun on
  RequestAdapter/RequestDevice but never checked the result (null → deep fault later); now throws a diagnostic like
  ramez. STILL divergent (deliberate scope): neutral requests an EMPTY device descriptor — no `TimestampQuery`, no
  `TextureAdapterSpecificFormatFeatures` — so GPU timestamps + >4× MSAA are unavailable (neutral caps at spec-
  guaranteed 1/2/4× via PickSampleCount). `Win32Helper.GetHInstance()` passes a PROCESS handle as HINSTANCE — low
  risk (consistent with the window-class registration; wgpu keys on HWND), noted only.

**CPU profiling (dotnet-trace, neutral button-hold, lavapipe).** Real sampled profile: the 16 threads are all
~equally busy and **no single managed frame dominates self-time** — the render CPU is a *distributed per-op
emission walk*, not one hot method. This matches the phase-timer split (build≈7ms) and confirms the fix is a
persistent draw-list that eliminates the whole per-frame re-emit, not a point optimization.

**Net of this session on CPU parity:** GPU stream = parity (§17). Win32 = audited + hardened. CPU = still ~5× ramez
on op-build, root-caused to per-frame re-emission of the non-cacheable path-clipped border rings + the general
op-list rebuild. The `IRenderData`-owned compiled draw-list (`ff66691163`) is the foundation; the remaining win is
making replay cheap (resident fans + a transform-table re-stamp so a static/moved visual re-emits pre-built ops with
one index write). That's the next piece — to be pinpointed by the user's own real-profiler comparison of both
branches on Windows.

Commits this session (9): 111cffff8a, 0b45a6ea8d, d4e8efcb79, ad5ee6418b, 3aefa152ab, fd92428c3d, ff66691163,
aff27a299f (+ the earlier resident-geometry one). Fresh win-x64 zip at /workspace/uno/SamplesApp-Win-x64-WebGpu.zip.

---

## 19. Session 2026-08-11 (cont.) — real CPU profiles (dotnet-trace) neutral vs ramez; [4] corrected

**[4] CORRECTION: ramez DOES have a Win32 WebGPU swapchain host** — `src/Uno.UI.Runtime.Skia.Win32/Rendering/
Win32WindowWrapper.Rendering.WebGpuSwapchain.cs` (+ `.WebGpu.cs`). My first win32 agent over-focused on X11 and wrongly
concluded "no counterpart". Re-running the comparison against the correct files (agent in progress). So the neutral
Win32 swapchain path SHOULD mirror ramez's and must be diffed properly.

**Real CPU profile (dotnet-trace, both branches, button-hold, lavapipe). Inclusive time, arb units:**
- neutral `WebGpuSwapChainContext.Present()` = **5596** ← dominates (offscreen→swapchain BLIT + surface acquire +
  wgpuSurfacePresent; on lavapipe present BLOCKS on CPU so this is inflated — but the extra blit pass is real).
- neutral op-build `RunFrame`=1548 → `RenderInto`=1393 → `BuildCoalesced`=961, `MakeClipBg`=371, `BuildSimpleOp`=250,
  `Vbuf`=245, `ApplyDepthClip`=92.
- ramez render-thread dominated by `wgpuQueueSubmit`=2288 (its present = submit + readback path here).

**Two levers, now separated:**
1. **Present path (Win32).** neutral renders to an offscreen resolve target then BLITS (fullscreen triangle) to the
   swapchain every frame + `wgpuSurfacePresent`. If ramez renders straight into the swapchain view (no extra blit),
   that per-frame full-screen pass is a real Windows cost. ← the win32 agent is checking exactly this; likely a
   primary Windows-fps factor since Present dominates the profile.
2. **Op-build CPU** (measurable in isolation via RENDERPERF `frameMs`, which times ONLY RenderInto, not present:
   steady ~10ms neutral vs ramez build+encode ~2.7ms). Steady-state dominated by the **102 non-cacheable border-ring
   gradients** re-emitted every frame: BuildSimpleOp + Vbuf + MakeClipBg + **ApplyDepthClip re-uploads the clip FAN
   via a pooled buffer EVERY frame** (`MakeBuffer(_scratch)` in ApplyDepthClip). Ramez path-clips them too (GPU diff
   §17) but keeps the fan **RESIDENT in the slab** — no per-frame re-tessellation/upload. **THE op-build parity fix:
   make the path-clip fan resident (upload once, reuse) + cache the path-clipped recording** — NOT analytic clips
   (approach-1 alone regressed because ApplyDepthClip still re-uploaded the fan every frame; the residency of the fan
   is the missing half).

Measurement bind (recurring): lavapipe present BLOCKS on CPU so total-frame + Present() are GPU-bound and mislead;
only RENDERPERF `frameMs` (RenderInto-isolated) is a trustworthy CPU-op-build signal here. Real Windows numbers need
the user's machine.

---

## 20. Session 2026-08-11 (cont.) — Win32 swapchain diff (CORRECTED) + reference-default alignment

Re-ran the Win32 comparison against ramez's ACTUAL Win32 swapchain (`Win32WindowWrapper.Rendering.WebGpuSwapchain.cs`).
Findings:
- **Both hosts do offscreen→swapchain BLIT** (fullscreen triangle) + `wgpuSurfacePresent`. My "ramez renders straight
  into the swapchain, avoids the blit" hypothesis was WRONG — the blit is NOT the gap.
- **MSAA default DIVERGED:** neutral fixed 2x/4x; ramez DPI-aware (`scale>=2?1:scale>1?2:4`). FIXED (`c57764edcb`):
  neutral `PickSampleCount` is now DPI-aware via a static `WebGpuDevice.RasterizationScale` the Win32 host sets before
  device creation. NOTE the zip previously FORCED `MSAA=1` in Program.cs (local) — so the tested zip already ran 1x
  (cheapest) ⇒ MSAA was NOT the tested-zip slowness. Removed that force so the DPI-aware default is used (user:
  "use the webgpu branch's default").
- **Present mode:** both default Fifo. User confirmed ramez was NOT benchmarked with a non-Fifo mode ⇒ present mode is
  NOT the gap. Added the `UNO_WEBGPU_PRESENT` hatch anyway (mirrors ramez, defaults Fifo) — changes nothing by default.
- **hinstance DIVERGES (unfixed):** neutral passes `Process.GetCurrentProcess().Handle` (process handle); ramez passes
  `GetModuleHandle(null)` (module HINSTANCE — correct, matters on Vulkan win32 surface). Left unfixed: an untested
  Win32 P/Invoke change risks the windows-only build I can't compile here. Follow-up.
- **Pipeline default:** both non-blocking (neutral `UNO_WEBGPU_PIPELINE` default ON = wait=0). The contradictory
  comment in WebGpuBackend.cs (~96-105, "off by default" vs "Default ON") should be corrected — code is Default ON.

**So neither MSAA, present mode, nor the blit explains the tested-zip Windows slowness** (zip ran MSAA=1, Fifo,
pipeline-on — same as ramez's effective config). Remaining suspect = the **op-build CPU / persistent-draw-list
re-emit** (§19 lever B): the non-cacheable path-clipped border rings re-tessellating their clip fan every frame
(ApplyDepthClip's per-frame MakeBuffer) vs ramez's resident fan. That's the next fix, and it's measurable via
RENDERPERF frameMs. Rebuilt the win-x64 zip with these default alignments.

---

## 21. Session 2026-08-11 (cont.) — CPU PARITY ACHIEVED (the ClipDataEquals bug)

The op-build gap was ONE bug. Instrumented builds-per-window on the 1000-button hold: the non-arena cached path was
rebuilding **~100 recordings EVERY frame** (`ca=2000` per 20-frame window), yet `miss=0` and `transformChanged=0` —
i.e. STABLE recordings (same render data, same transform) rebuilding for no reason. Cause: `ClipDataEquals` compared
`Rounds`/`PathFan` by **ReferenceEquals**, but those clip arrays are re-allocated every frame (copy-on-write
`ClipData.Push` / `ClipCompose`). So a logically-identical clip read as "changed" every frame → full `BuildCoalesced`
re-tessellate + re-upload per clipped cached recording.

FIX (`2e25b6f55b`): value-compare clip data (Aabb + Rounds element-wise + PathFan SequenceEqual only when both have
one; far cheaper than the rebuild it prevents). Result on the button scene (RENDERPERF frameMs = RenderInto CPU,
isolated from present, MSAA=1):
- **before: ~9.7-10ms, ca=2000/window (100 rebuilds/frame)**
- **after: ~3.0-3.3ms, ca=0** — **AT PARITY with the reference branch's ~2.7ms build+encode.**

This is the CPU op-build parity the whole investigation was chasing. It was NOT the clip strategy (analytic vs path),
NOT MSAA/present, NOT the persistent-draw-list caching location — it was a reference-vs-value equality bug that
defeated the existing cache for every clipped recording. The earlier approach-1/2/A+B regressions were all downstream
of this: they added machinery on top of recordings that were rebuilding every frame anyway.

Combined session result: GPU stream at parity (§17), CPU op-build at parity (this), Win32 defaults aligned (§20).
Remaining: verify on real Windows HW (user); the lavapipe total-frame is still present-bound but that's env, not the
branch. Commits: …, c57764edcb (DPI-aware MSAA), hinstance, 2e25b6f55b (ClipDataEquals — the parity fix).

---

## 22. Session 2026-08-11 — SYSTEMATIC 3-subsystem audit (device / per-frame / pipeline) + fixes

User (rightly) demanded a systematic audit instead of reactive one-offs. Ran 3 parallel Explore audits comparing
neutral (`Uno.UI.Composition.WebGpu/`) vs ramez (`Skia.X11/Rendering/WebGpu/`). Full inventory:

**Device/adapter/context:** ONE real fix — neutral registered NO uncaptured-error callback, so wgpu's default handler
PANICKED the process on any validation error (the recurring "panic" incl. 2x MSAA). FIXED `210206cbc7` (log+continue,
like ramez). Everything else MATCH: instance, power pref, RequiredLimits (both null/defaults), format feature,
DPI-aware MSAA formula, color↔depth sample matching, depth format. Intentional/OK: no TimestampQuery (neutral has no
GPU-timer subsystem), MSAA=8 caps at 4x, no device label, BGRA swapchain vs offscreen-RGBA+swizzle (neutral better).

**Pipeline/pass/blend/stencil/MSAA:** essentially clean — MATCH or neutral-MORE-CORRECT: image blend premultiplied
(neutral correct for Uno pixels; ramez straight-alpha would halo), path-fill winding honors GeometryFillRule (neutral
correct; ramez hardcodes even-odd), depth/stencil StoreOp=Discard (neutral perf win). One verify-item: neutral bakes
MSAA sample count into pipelines at device init; ramez re-picks per frame + rebuilds on DPI change → neutral won't
re-adapt MSAA on a RUNTIME DPI change unless the host recreates the device (rare edge; not fixed).

**Per-frame upload/transform:** static cached frame is CLOSE TO PARITY (geometry resident both sides, bind-group
cache is a direct port, gradients cached, scissor+coalescing match). Real divergences: (1) per-op MakeClipBg for
IMMEDIATE/inline content (small for the cached button scene); (2) re-tessellation on MOVE for clipped/frame-solid
recordings — neutral's arena-restamp only covers UNCLIPPED recordings; ramez uses a per-vertex transform-index +
storage-buffer transform table (the #10/#28 transform table — matters for SCROLLING, not static buttons); (3) no
skip-if-identical upload (mitigated: resident geometry doesn't re-Put on hit); (4) whole-tree TransformFor per frame
at non-100% DPI.

**DPI TransformFor (#4) TESTED + DISPROVEN as the dominant cost:** forced scale=1.5 on the 100-button hold →
render 1.1→1.3ms (+0.2ms), alloc 118→202KB. NOT the ~10ms the user saw. So the big transform-table rewrite is NOT
warranted for the static scene (it'd trim ~1MB/f GC alloc at 500 buttons, secondary).

**Real-HW gap = `submit` (8.5ms in the user's profile), which is DRIVER-side (lavapipe shows submit≈0.6ms so it's
invisible here).** The user's profile PREDATES the resident-fan fix (B): it showed `upload=1.7MB/frame`, which flushes
AT submit on real GPUs. Current build shows `upload=0KB` (B works). So **B should directly cut the user's submit** —
needs a profile from the B-zip to confirm. Commits this session incl. B `4a1609049f`, ClipDataEquals `2e25b6f55b`,
path-cache `b335367dda`, MSAA-feature `a7cf726e51`, uncaptured-error `210206cbc7`, DPI-MSAA `c57764edcb`, hinstance.

**Transform table (#28) is the last real perf divergence but only bites MOVING/scroll content** — deferred as a
scoped follow-up (big: per-vertex index + storage buffer + WGSL for all pipelines + local-vert build), NOT needed for
the static-button parity the user is measuring.

## 23. Session 2026-08-11 (cont.) — resize-stretch fix + PATH-FILL TRANSFORM TABLE (#28) IMPLEMENTED

**Reported bug:** on window resize (user @1.5x DPI) path icons on the SamplesApp startup screen STRETCH instead of
re-rendering. Root cause: path-fill (kind 1) fan/cover verts were CPU-NDC-baked at build (pos/size); a surface-size
change left the baked NDC stale → the arena reuse (or cache hit) replayed old-size NDC into the resized surface.

**Immediate fix (commit `c1bb0390f7`):** stamp BuiltW/BuiltH on the geometry cache; rebuild when the surface size
differs. This matches ramez's OWN solid/rr resize behavior (ramez also NDC-bakes solid/rr and relies on re-record on
layout change). Correct but re-tessellates on resize.

**Proper fix — ramez's transform table for PATH FILLS (commits `25797442a6` feat + `b7847539bb` perf):**
Key finding from reading ramez `ReplayCachedArena`: ramez's transform table is **path/cover-ONLY** (glyphs & arbitrary
paths). Solid/rr stay NDC-baked + translation-offset (`AppendOffset`) — same as neutral. So "do ramez's version" =
implement the table for kind-1 path fills only; solid/rrect/clip-fans/shadows stay on the NDC pipes.

Design (neutral's resident model, stable-slot variant of ramez's per-frame dedup):
- Path fan/cover verts stored in **recorded-device** space (immediate/non-arena/frame-solid) or **local/identity**
  space (arena) + a **per-vertex u32 slot index** (raw bits in a float slot). Strides: fan 2→3, cover 6→7.
- A per-pass **read-only storage buffer** `_xforms` holds 8 floats/slot = a local→NDC affine `a=(2R11/W,2R21/W,
  2R41/W−1,−2R12/H) b=(−2R22/H,1−2R42/H,0,0)` folding an extra transform R (Identity for device verts; the replay
  transform for arena local verts) + the current device→NDC projection. WGSL `Xf{a,b}`, `NDC=(p.x*a.x+p.y*a.y+a.z,
  p.x*a.w+p.y*b.x+b.y)` — matches ramez `StencilArenaWgsl`/`CoverArenaWgsl` exactly.
- New pipelines `StencilTable{EO,NZ}`/`CoverTablePipe`, EXPLICIT BGLs: group 0 = XformBgl (storage), cover group 1 =
  the shared **ClipBgl** (so existing ClipU bind groups — incl. the arena re-stamp's `finv` for `clipCov` — bind
  unchanged; the table cover's vertex ignores `clip.xform`, using xf[slot] instead).
- Device-level stable **slot allocator + free-list**; a recording's slot recycles when its compiled state drains
  (`_pendingCompiled`). Immediate draws use **transient** slots freed at pass end. `_xforms` is per-RenderInto,
  saved/restored around the nested-layer render, uploaded once before the pass.
- Wired at every path-fill site: immediate (transient slot, R=I), arena (stable slot, R=rr.Transform, identity
  verts), non-arena cached (stable slot, R=I, device verts), frame-solid non-solid extraction (stable slot, R=I).
  Entry rewritten EVERY frame the recording draws → resize/move/DPI = one 8-float table write, no re-bake/re-tess.
- **Pure-path arena entries skip the resize rebuild entirely** (device verts + table = size-independent); mixed
  entries (NDC-baked solids) still rebuild. This is the "cheap resize" ramez behavior.

**Validation (smoke, lavapipe, 94/94, 0 px disagreement vs Skia on the cross-checked scenes):** exercises EVERY table
path — text/stroke/combine/clip-path (immediate), `arena-path: moved` + `arena-path resize` (arena, move & resize),
`mixed-frame-solid: path` (frame-solid), `coalesce-path`/`coalesce-cached`/`retained frame1/2` (cached), `resize`,
`dpi-scale`. Runtime-validated headless. **Real-app (SamplesApp) validation PENDING** via the win-x64 zip
(`SamplesApp-Win-x64-WebGpu.zip`) — confirm the startup path icons no longer stretch on resize + scroll of text/icons
is cheap. NDC-bake retired for path fills; the `xform` NDC→NDC special case retired for path fills (solids still use it
via the arena re-stamp).

**Not done (scoped follow-ups):** cross-visual glyph coalescing into one draw via the shared table (task #28's batching
half — the slot machinery now supports it, but the frame-solid path still emits one draw per glyph run); frame-solid /
non-arena still rebuild on resize because their SOLIDS are NDC-baked (only pure-path arena skips it).

## 24. Session 2026-08-11 (cont.) — Win32 native-publish crash fix + backlog reconciliation

**CRASH FIXED — DllNotFoundException('webgpu') on the Win32 head (commit `aa7f11a624`).** `ProvisionWgpuNative`
copied `wgpu_native.dll` as a LOOSE file into `$(OutDir)` after Build; `dotnet publish -o <dir>` only copies TRACKED
publish items, so the native never reached the published app → `wgpuCreateInstance` DllNotFound. Fix: register it as a
`ResolvedFileToPublish` item (`AfterTargets=ComputeFilesToPublish`), split asset-selection/fetch into their own
targets. Load-bearing subtlety: a target-level `Condition="'$(_WgpuAsset)' != ''"` is evaluated BEFORE its
DependsOnTargets run (so it sees the property empty and skips) — the guards must be on the TASKS. Verified: win-x64
self-contained publish now contains `wgpu_native.dll` (PE32+, 8MB); zip rebuilt.

**Backlog reconciliation — walked every "deferred"/loose-end item against the code. Result: 5 were already done
(stale tasks), 1 is an architectural tradeoff, 2 are rare/benign, the rest are the real remaining work:**

ALREADY DONE (verified in code, tasks marked complete):
- **#31 rounded-rect clips → analytic:** `CompositionClip.ApplyClip` routes rect→ClipRect, rounded→ClipRoundRect;
  `ApplyPrePaintingClipping` uses it, so every Visual.Clip/Border-CornerRadius rounded clip already hits analytic
  `clipCov`. No rounded post-clip overrides exist; remaining ClipPath callers are genuinely path-shaped. Stale.
- **#20 dirty-range slabs:** `WebGpuSlab.Put` uploads only the changed slice's bytes (full only on grow); uniforms
  are bg-cached. Done.
- **#19 gradient local-space + effect routing + glyph:** gradients carry LocalMatrix (gradient-local eval, exact
  under transform); effects + glyphs validated in smoke (0 px vs Skia). Done.
- **per-op MakeClipBg for immediate/inline:** immediate (owned==null) hits the cross-frame bg cache (TryGetCachedBg);
  "every clip is cacheable" since path clips carry no per-frame coverage tex. Done.
- **Color glyphs / emoji:** wired end-to-end (UnicodeText/ParsedText → HasColorGlyphs → AppendColorGlyphImages →
  DrawImage; ManagedFont COLR/CPAL + SkiaFont). The old "DrawImage no-op" memory note was stale. Done.

ARCHITECTURAL TRADEOFF (not a bug — do NOT "fix" by reverting the win):
- **Solid scroll-offset on frame-solid move (was "D3"):** neutral uses a RESIDENT SHARED slab (multiple recordings'
  solids coalesced into one buffer + one draw — the cross-visual static-frame win the user measured). ramez instead
  RE-EMITS its draw-list every frame and `AppendOffset`s a moved recording's solids. A moved recording can't be
  offset independently inside neutral's shared coalesced slab, so a scroll re-Puts that recording's slab slice. But
  the rebuild only RE-TRANSFORMS pre-flattened geometry (TransformFor on points + a dirty-range slab upload) — it does
  NOT re-flatten glyph/shape outlines (fans are cached in FanDevice). So the "re-tessellation on move" is really a
  point-transform + small dirty upload, and matching ramez's offset would mean reverting to the per-frame re-emit
  architecture that LOSES the static-frame win. Consciously kept; documented, not changed.

RARE / BENIGN (documented, low value):
- **Nested ARBITRARY-path clips:** ClipRoundRect stacks (Rounds ANDed), but ClipPath sets a single PathFan
  (innermost wins). Two nested arbitrary-path clips are very rare in real UI; a multi-fan depth-mask intersection is
  fiddly for little value. Left as a documented limitation.
- **rrect draw count 125 vs 114 (§17):** ~11 more analytic rrect quads than ramez; analytic quads are cheap and the
  smoke rrect scenes agree with Skia. Benign perf delta; not chased.

REAL REMAINING WORK:
- **Cross-visual glyph coalescing** — merge same-colour+same-clip glyph runs ACROSS recordings into one stencil+cover
  draw (the transform table's per-vertex slot now makes this correct). The batching analog of the solid slab; the one
  substantive tractable perf item left. Headless-validatable.
- **Acrylic/backdrop O(n²) prefix re-render (#17)** and **bounds-sized offscreens (#14)** — both need the
  resolve-then-sample rearchitecture (single-MSAA-pass can't LoadOp.Load) and REAL-GPU visual validation.
- **Runtime-DPI MSAA re-adapt** — needs Win32-host device recreation on a DPI change; not reproducible headless.
- **Glyph-winding vs WinUI** — neutral honours FillRule (nz), ramez hardcodes eo; author a WinUI-parity runtime test
  to confirm neutral is the correct one.
