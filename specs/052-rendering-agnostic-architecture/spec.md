# Engineering Analysis: Rendering-Agnostic Architecture for Uno Platform

**Feature**: Future-proof Uno Platform's architecture, solution layout, and NuGet packaging so that the 2D rasterizer (**Skia** today) can be complemented or replaced by alternative backends (**Skia Graphite / WebGPU**, an **Impeller-style** rasterizer, a **native WebGPU/Vello renderer**) without churning consumer code or the public API surface — and decide, in that light, the fate of the **"reference" build** and the **runtime assembly-swapping** mechanism.
**Spec number**: 052
**Target release window**: Uno Platform 7.0 groundwork (architecture + packaging direction); backends land later per `003-skia-opengl-backends` phase-gates
**Feature branch**: `dev/mazi/rendereragnostic`
**Status**: Draft — analysis & recommendation
**Audience**: Uno Platform runtime/graphics maintainers
**Related**: `043-drop-native-rendering` (native UI backends removed in 7.0 → Skia is the single UI renderer), `003-skia-opengl-backends` (future GPU-backend intake: Vulkan/Metal/WebGPU/Graphite)

> This document is an **analysis + recommendation**, not an implementation plan. It (1) establishes the conceptual model, (2) measures the current Skia coupling with hard numbers, (3) studies how **Avalonia** and **Flutter** solve the same problem, and (4) proposes an architecture + packaging direction with an explicit, sequenced decision on the reference build.
>
> Evidence base: first-hand inspection of the cross-targeting/reference/packaging machinery **plus** a 9-agent research workflow (5 grounding this codebase, 4 studying Avalonia/Flutter/WebGPU/packaging), each finding adversarially re-verified. All counts below survived that verification (minor count corrections applied inline). Codebase counts are a snapshot of `dev/mazi/rendereragnostic`, on which the native-removal of spec 043 is **already merged** — so this tree is effectively the post-native-removal end-state.

---

## 1. Executive summary

Uno 7.0 removes the three native UI renderers (Android Views, iOS/UIKit, WASM DOM) and makes **Skia the single UI rendering engine on every target** (`043-drop-native-rendering`). That is a huge simplification — but it concentrates the entire framework onto *one specific 2D rasterizer*. The strategic question is: **how do we keep the door open to swap or complement Skia** (with Skia's own next-gen Graphite/WebGPU backend, or a fundamentally different rasterizer like Impeller or a native WebGPU/Vello renderer) **without another platform-scale upheaval — and without sacrificing** Uno's signature developer advantage: *authoring class libraries as plain `net10.0`, with no `net10.0-android/ios/browser` target frameworks.*

Everything turns on **one distinction** kept sharp throughout:

- **Level 1 — the GPU surface *under* Skia.** Skia's rasterizer (Ganesh today, Graphite tomorrow) draws onto a GPU surface via OpenGL/WGL/GLX, EGL, Vulkan, Metal, WebGL, or software. **Uno already does this** — its `Uno.UI.Runtime.Skia.*` hosts each select among these surface APIs, and a full custom Vulkan renderer already feeds Skia's `GRContext`. **Adopting Skia Graphite (Dawn/WebGPU, Vulkan, Metal) is a Level-1 change** — still Skia's 2D API — and is the cheapest, lowest-risk path to "WebGPU rendering."
- **Level 2 — swapping the 2D rasterizer *itself*** (Skia ↔ Impeller ↔ native-WebGPU/Vello). This requires an abstraction **above** Skia, at the `Uno.UI.Composition` scene-graph level, so the visual tree emits *backend-neutral* draw operations any rasterizer can consume. **This is the true "rendering-agnostic" architecture** — and today it does not exist: the scene graph is Skia-hardcoded.

**What the numbers say (§3.1).** The coupling is **highly concentrated, not diffuse** — which is the best possible news:
- `Uno.UI.Composition` **is** the Skia scene graph: **54 `.skia.cs` files (~8.3K LOC)**, 46 referencing SkiaSharp, **all suffixed** (zero leakage into shared files); **31 of 54** do real `SKCanvas`/`SKPath`/`SKPaint` drawing.
- `Uno.UI` is mostly Skia-clean: Skia lives in **131 `.skia.cs` files (~25K LOC)**; only **4** non-suffixed files reference a SkiaSharp *type* and all are `#if __SKIA__`-gated (a 5th, `SKCanvasVisualBase`, mentions Skia only in a doc-comment and is the deliberate *decoupling exemplar*).
- The rest of the general libraries — **`Uno.Foundation`, `Uno.UI.Dispatching`, `Uno.UI.Toolkit`, `Uno.UI.FluentTheme(.v1/.v2)`, `Uno.UI.XamlHost`** — are **100% Skia-free**. `Uno.UWP` touches SkiaSharp in just **4 files**, all image-codec/bitmap data (not rendering).
- Consumer-visible breakage from a rasterizer swap is bounded to **exactly 4 public API surfaces** (§3.5).

So the Level-2 job is a *deep but concentrated* refactor — **not** a framework-wide sweep — but it is **more than one library**: it centers on `Uno.UI.Composition`, and it must also re-home the **render-loop entry and host-handoff surface that live in `Uno.UI`** (`SkiaRenderHelper` drives `SKPictureRecorder`/raw `sk_*` P/Invoke/`SKPath.Op` clip math; `CompositionTarget.OnNativePlatformFrameRequested(SKCanvas, Func<Size,SKCanvas>)` *returns an `SKPath`* consumed by all 9 hosts; `RenderTargetBitmap` builds `SKImage`), plus the text and effects sub-systems. Honest scope = "Composition + the `Uno.UI` render-loop/host-handoff surface + text + effects."

**Two industry reference points frame the target:**
- **Avalonia** proves the *interface* model: controls draw against an abstract `DrawingContext`/`IDrawingContextImpl`, with `IPlatformRenderInterface` as the backend factory; **Skia is one runtime-registered backend** (`.UseSkia()`). It's provably real — `Avalonia.Direct2D1` was a second rasterizer until Avalonia 12. Its core + controls + even `Avalonia.Skia` ship as **plain `net8.0;net10.0`** (no platform TFM); platform heads are separate packages. Backend selection is **runtime DI**, *not* a compile-time assembly swap.
- **Flutter** proves the *Level-2 swap end-to-end*: the engine records a backend-neutral **`DisplayList`** via a **`DlCanvas`** interface and dispatches ops through **`DlOpReceiver`** to *either* Skia (`DlSkCanvasDispatcher`) or **Impeller** — the framework never calls Skia directly, which is exactly why Impeller replaced Skia with **zero framework changes**.

**The reference-build reframe — and the central tension.** First-hand inspection shows the **"reference" build is already an (accidental) backend-neutral compile surface**: it is compiled *without* `__SKIA__`, so it **excludes every `.skia.cs` body** (all `SkiaSharp`/`SKCanvas` code) and substitutes `.reference.cs` stubs (bodies literally `throw new NotSupportedException("Reference assembly")`). A plain `net10.0` consumer library compiles against it and **never sees SkiaSharp**. That is precisely the property a rendering-agnostic framework wants from its public contract.

This creates the tension at the heart of the packaging decision:
> The Uno.UI reference build is simultaneously **(a)** packaging overhead we'd like to cut now that UI is Skia-only, **and (b)** the only thing keeping the public/compile surface **backend-neutral**. Dropping it — letting the *Skia* build become the compile surface — is the easy packaging win, but it **pulls `SkiaSharp`/`HarfBuzzSharp` into every consumer's compile closure (reference graph) and makes the Skia build's SK-typed public members the surface consumers compile against**, cementing Skia into the public contract and working *against* rendering-agnosticism.
>
> *(Mechanism precision: the `__SKIA__` compile symbol is set only in Uno's internal `Uno.CrossTargetting.targets`; it is **not** propagated to consumers via `buildTransitive`, so a consumer's own compilation never gets `__SKIA__`. The leak is the transitive `SkiaSharp` PackageReference + the SK-typed public API of the Skia assembly — not a consumer-side define.)*

**Resolution (the thesis of this document):** the two goals **converge** if sequenced correctly. Make `Uno.UI`/`Uno.UI.Composition` *genuinely* Skia-free (the Level-2 refactor: move the `.skia.cs` draw bodies + the 4 public leaks behind an `IDrawingBackend`/scene-recorder seam, ship Skia as a separate `Uno.UI.Composition.Skia` backend resolved at host startup — Avalonia's model). Then the reference twin for the UI layer **disappears because the coupling did** — a backend-neutral UI assembly is its own compile+runtime surface. The reference build + two-layer swap **stays only where it is still load-bearing**: the per-platform WinRT libraries (`Uno.UWP`/`Uno.Foundation`/`Uno.UI.Dispatching`), which is a platform-capability concern, not a rendering one.

**Two strategic postures (be explicit about which one is being funded).** The analysis surfaces a genuine fork, and the honest default is the conservative one:
- **Posture 1 — Level-1-only (the conservative default).** Ship **Graphite** for the modern-GPU/WebGPU story, **keep the reference build exactly as-is indefinitely** as cheap neutrality insurance, and **do not build the Level-2 seam** until a real, benchmarked second rasterizer exists to justify it. This captures the entire realistic near-term win with none of the multi-release parity risk. Today there is *no adoptable second rasterizer* for Uno (Impeller reachable via the experimental `NImpeller` binding but NuGet-less/browser-less/GPU-only; the Vello .NET binding `VelloSharp` archived Jan 2026 pending a restart; in-browser native WebGPU blocked) — so on today's evidence, Posture 1 is the rational default. Avalonia is a mixed data point: it *removed* its only shipped second backend (Direct2D) in v12, yet it is now *building a new one* (Impeller via `NImpeller`) behind the same `[Unstable]` abstraction — evidence the seam has enduring value even if any single backend comes and goes.
- **Posture 2 — invest in the Level-2 seam now.** Justified only if leadership wants rendering-agnosticism as a *strategic optionality* independent of a specific second rasterizer, and accepts a multi-release, behavioral-parity-heavy refactor. The recommendations below describe *how* to do Posture 2 well **if** it is chosen; they are **not** an argument that it must be chosen over Posture 1.

The seam work (Posture 2) should be gated on a concrete trigger — a funded, benchmarked second rasterizer, or a measured jank/frame-time win Graphite cannot deliver — not started speculatively.

**Headline recommendations:**
1. **Adopt the two-level vocabulary** in all roadmap discussions; never conflate "Uno on WebGPU" (almost always **Graphite = Level 1, cheap**) with "swap the rasterizer" (**Level 2, expensive**).
2. **Near-term "WebGPU" = Skia Graphite (Level 1)** via a `SkiaSharp 3.119 → 4.15x` bump — work **Uno itself is already driving** into SkiaSharp (issue #3962, "Graphite … contributed by Uno Platform"). No architecture change; Metal/macOS-iOS first, then Vulkan, then Dawn/WebGPU. Aligns with spec 003 Phase 4.
3. **Introduce the Level-2 seam** (`IDrawingBackend` / `ISceneRecorder`, Uno's *retained, per-Visual, cacheable* `DlCanvas`/`DisplayList` analog) in `Uno.UI.Composition`; ship Skia as an **additive backend assembly** — call it e.g. `Uno.Graphics.Skia`, **not** today's `Uno.UI.Composition.Skia` (which is a build *variant* producing `AssemblyName=Uno.UI.Composition`, the opposite of an additive backend) — registered **process-wide before first paint** (a startup backend registry, analogous to but distinct from the owner-keyed `ApiExtensibility`). Treat as a multi-release effort; the payoff must be a measurable win (jank/perf), per Flutter's Impeller rationale.
4. **Reference build:** *keep it unchanged for the UI layer until the Level-2 refactor lands* (dropping it early cements Skia into the contract). Once the UI layer is backend-neutral, **drop the UI reference variant** (it becomes redundant) and **keep reference + two-layer swap only for the WinRT libs**. `RuntimeAssetsSelectorTask` is simplified, not deleted.
5. **Packaging for backends:** a backend is **not** a new TFM and **not** a 5th project variant. Prefer the **separate-backend-package + startup-DI** model (Avalonia-style, runtime-selectable, keeps the neutral core clean); the existing `uno-runtime/<tfm>/<backend>` folder + `UnoRuntimeIdentifier` machinery is the fallback if compile-time backend fixing is acceptable.
6. **Treat Impeller and Vello as *gated watch items*, not bets** (this corrects an earlier "rule out Impeller" line). Flutter now ships a **standalone Impeller Toolkit C ABI**, and **`AvaloniaUI/NImpeller`** is an active .NET binding to it (Avalonia+Flutter collaboration) — so Impeller is *reachable* from .NET, and Avalonia building it behind their rendering abstraction **validates Uno's Level-2-seam thesis**. But neither Level-2 rasterizer is adoptable near-term: NImpeller is experimental (no NuGet/license, GPU-only, **no WASM**), and the Vello .NET binding (`VelloSharp`) was **archived in Jan 2026** pending a restart. Watch both behind the same seam; bet on Graphite.

---

## 2. The two-level model (the spine of this analysis)

```
 ┌─────────────────────────────────────────────────────────────────────┐
 │  App XAML / C#                                                        │
 ├─────────────────────────────────────────────────────────────────────┤
 │  Uno.UI            — visual tree, layout, input                       │
 │                      (mostly backend-neutral: 131 .skia.cs, only 5    │
 │                       #if __SKIA__ leaks in shared files — §3.1)      │
 ├─────────────────────────────────────────────────────────────────────┤
 │  Uno.UI.Composition — scene graph (Visual / Compositor)              │
 │        ▲                                                              │
 │        │  ═══════  LEVEL 2 SEAM  ═══════  (DOES NOT EXIST YET)        │
 │        │   Today the scene graph is Skia-hardcoded: PaintingSession   │
 │        │   wraps a raw SKCanvas; Visual.Paint(in PaintingSession);    │
 │        │   IRenderer.BackgroundColor is SKColor; 46 .skia.cs files.   │
 │        │   Avalonia analog: IDrawingContextImpl/IPlatformRenderIface  │
 │        │   Flutter analog:  DlCanvas → DisplayList → DlOpReceiver     │
 │        ▼                                                              │
 │  Rasterizer backend:  [ Skia (Ganesh) ]  → future: Graphite,          │
 │                       Impeller-style, native WebGPU / Vello          │
 ├─────────────────────────────────────────────────────────────────────┤
 │        ▲   ═══════  LEVEL 1 SEAM  ═══════  (EXISTS & MATURE today)    │
 │        │   Hosts hand Skia an SKSurface/SKCanvas; single handoff:     │
 │        │   CompositionTarget.OnNativePlatformFrameRequested(SKCanvas) │
 │        ▼                                                              │
 │  GPU surface API:  OpenGL/WGL/GLX · EGL · Vulkan · Metal · WebGL ·    │
 │                    Software  (chosen per Uno.UI.Runtime.Skia.* host)  │
 │                    Skia Graphite slots in HERE (Dawn/Metal/Vulkan).   │
 └─────────────────────────────────────────────────────────────────────┘
```

- **Level 1 already exists and is mature** in Uno: Win32 (GL default → Vulkan opt-in → software), X11 (GLX/EGL/Vulkan/software), macOS/iOS (Metal), WASM (WebGL2→WebGL1→software), Android (GLES→Vulkan opt-in→software), FrameBuffer (DRM/software). Vulkan here is confirmed a **Skia Ganesh backend** (`GRContext.CreateVulkan`), not a native renderer. **Skia Graphite is a Level-1 modernization of exactly this path.**
- **Level 2 does not exist** in Uno: the `Visual` tree → draw-calls transition is Skia-hardcoded. This is the seam this analysis targets.
- **Keeping the levels distinct is essential.** "Run Uno on WebGPU" almost always means *Level 1 via Graphite* (cheap, in flight), not *Level 2 native WebGPU renderer* (expensive, speculative). Conflating them inflates cost estimates and misdirects the roadmap.

---

## 3. Current state (verified)

### 3.1 How much Skia is in the general libraries (the quantification ask)

**Verdict: the coupling is deep but sharply concentrated in `Uno.UI.Composition`, and the `.skia.cs` suffix convention has already isolated almost all of it behind a compile boundary.**

| Library | Skia coupling | Hard numbers |
|---|---|---|
| **`Uno.UI.Composition`** | **Rasterizer-deep** — *is* the Skia scene graph | 54 `.skia.cs` files, ~8,354 LOC (`wc -l`); 46 files reference SkiaSharp, **all `.skia.cs`, 0 shared-file leakage**; **31 of 54** do actual `SKCanvas`/`SKPath`/`SKPaint` drawing |
| **`Uno.UI`** | **Mostly clean** — Skia in suffixed files + a few `#if __SKIA__` shared aliases | 131 `.skia.cs` files, ~25,407 LOC; **4 non-suffixed files reference a SkiaSharp type, all `#if __SKIA__`-gated** (`StreamGeometry`, `PathStreamGeometryContext`, `SystemFocusVisual`, `ImageSourceHelpers`). `SKCanvasVisualBase` mentions Skia only in a doc-comment (canvas passed as `object`) — the decoupling exemplar, not a leak. |
| **`Uno.UWP`** | **Data-only** — image codec / bitmap / `Color↔SKColor` | 4 files (via `Uno.Skia.csproj`); not scene rendering |
| **`Uno.Foundation`, `Uno.UI.Dispatching`, `Uno.UI.Toolkit`, `Uno.UI.FluentTheme(.v1/.v2)`, `Uno.UI.XamlHost`** | **None** | 0 SkiaSharp / 0 HarfBuzz |
| Across all of `src` | — | **237 `.skia.cs` files** total; SkiaSharp pinned at **3.119.0** (`Directory.Build.targets:77`) |

**Only two shipping general libraries carry a direct `SkiaSharp` PackageReference:** `Uno.UI.Composition.Skia` (the rasterizer) and `Uno.UWP`'s `Uno.Skia.csproj` (4 codec files). `Uno.UI.Skia.csproj` references **HarfBuzzSharp** directly (text shaping) and gets SkiaSharp only transitively from Composition.

**The Level-2 seam is narrow but invasive.** The abstract seams a rasterizer swap must replace are *themselves typed in SkiaSharp*:
- `Visual.PaintingSession.skia.cs` — an internal `readonly ref struct` whose `public readonly SKCanvas Canvas;` field is threaded through the whole render loop.
- `Visual.Paint(in PaintingSession)` — internal virtual overridden across ~30 Composition types.
- `IRenderer.BackgroundColor` → `SKColor`; `ISkiaSurface.Paint(SKCanvas, float opacity)`; `CompositionBrush.Paint(SKCanvas, …)`; `Compositor.RenderRootVisual(SKCanvas, …)` (the top-level host entry).
- `Visual.skia.cs` (~1,036 LOC) drives `SKPictureRecorder`, `SKPath` pools, `SKMatrix`, and **raw `UnoSkiaApi.sk_*` P/Invoke** (14 DllImports); `CompositionSpriteShape.skia.cs` (~1,095 LOC) reimplements WinUI stroke/dash/cap/miter on `SKPathEffect`/`SKPathMeasure`/`SKPathVerb`.

**The coupling is genuine 2D-rasterizer logic, not incidental data-holding.** Geometry building (path verb-walking), stroke/dash/cap/miter compensation for where Skia diverges from WinUI, analytic-shadow silhouette `SKPath.Op` math, and picture-collapsing optimizations all encode Skia-specific behavior. **Two sub-systems the LOC count understates:**
- **Text/glyph:** 9 `Uno.UI` `.skia.cs` files use `SKTypeface`/`SKFont`/`SKTextBlob`, and HarfBuzz shaping currently **sources its font tables through Skia's `SKTypeface`** (`FontDetails.skia.cs` fuses `SKFont`/`SKFontMetrics` with a HarfBuzz `Font`). "Just keep HarfBuzz" is insufficient — the typeface/table/metrics provider must be re-supplied for a non-Skia backend.
- **Effects/shaders:** `CompositionEffectBrush` + `EffectHelpers.skia.cs` + `D2D1Helpers.skia.cs` encode a D2D→SkSL shader-graph translation — a separate hard problem for any non-Skia rasterizer.

**The one pattern that already demonstrates the intended decoupling** is `SKCanvasVisualBase`/`SKCanvasVisualBaseFactory`: the Skia canvas is passed as `object` and wired via `ApiExtensibility` — the shared base deliberately avoids a hard SkiaSharp reference. It's the exception today; it's the template for tomorrow.

### 3.2 The reference build + runtime assembly-swap — how the pure-`net10.0`-library promise works

*Verified: `src/Uno.CrossTargetting.targets`, `src/Uno.UI/Uno.UI.Reference.csproj`, `src/SourceGenerators/Uno.UI.Tasks/RuntimeAssetsSelector/RuntimeAssetsSelectorTask.cs`, `build/nuget/uno.winui.runtime-replace.targets`, `build/nuget/Uno.WinUI.nuspec` / `Uno.WinRT.nuspec`.*

Uno decouples the **compile-time API surface** from the **runtime implementation**, going beyond what standard NuGet asset selection can do (standard NuGet has *no* way to vary the compile assembly by RID — "same API surface for all RIDs"). A Uno package therefore ships:
- **`lib/net9.0` + `lib/net10.0`** — the **"Reference" assemblies**: a compile-only "bait" surface whose method bodies are `throw new NotSupportedException("Reference assembly")` stubs (`ProduceReferenceAssembly=false`, so it is a *stub-implementation* assembly, **not** a metadata-only `[ReferenceAssembly]` one — it wouldn't itself throw `BadImageFormatException`).
- **`uno-runtime/<tfm>/<rid>`** (rid = `skia` | `webassembly`) — the real per-runtime implementation.
- (For the WinRT layer) **`lib/net*-android|ios|tvos|maccatalyst`** — per-platform WinRT implementations.

At app build the **`ReplaceUnoRuntime`** target (in `build/nuget/uno.winui.runtime-replace.targets`) runs **`RuntimeAssetsSelectorTask_v0`**, which physically swaps the reference copy-local binaries for the ones under `uno-runtime/<tfm>/<rid>`:
- **Single-layer** (`UnoRuntimeIdentifier=skia|webassembly`): keep the reference for the *compiler*; swap runtime binaries to `uno-runtime/<tfm>/skia`.
- **Two-layer** (Skia-on-mobile/WASM: `UnoUIRuntimeIdentifier=skia` + `UnoWinRTRuntimeIdentifier=android|ios|maccatalyst|tvos|webassembly`): UI assemblies come from `uno-runtime/.../skia`; the **3 WinRT assemblies** (`Uno.dll`, `Uno.Foundation.dll`, `Uno.UI.Dispatching.dll` — `IsWinRTAssembly`) come from `lib/net*-<platform>` (or `uno-runtime/.../webassembly`).

**Symbols** (from `Uno.CrossTargetting.targets`): `IsCrossruntime` = `Skia | WebAssembly | Reference` → all get `__CROSSRUNTIME__;UNO_REFERENCE_API`; `__SKIA__` = Skia-only; `__NETSTD_REFERENCE__` = Reference-only (26 `.reference.cs` files in `Uno.UI`; `__NETSTD_REFERENCE__` in 38 files / 55 occurrences). The reference build is compiled **without** `__SKIA__`, so it excludes every `.skia.cs` and is SkiaSharp-free — **the (accidental) backend-neutral compile surface**.

**Why a library can be plain `net10.0`:** it compiles against the SkiaSharp-free reference surface; at app publish the framework swaps in the correct runtime implementation by RID. The reference surface is the *bait*; the RID-specific runtime is the *switch* — a mechanized "bait-and-switch." (Nuance: for third-party libs the real enabler is the `uno-runtime` packaging convention + the task's `Mono.Cecil` check that only retargets DLLs actually referencing `Uno.UI`.)

> **Consequence:** the reference build is not dead weight from the native era. Post-native-removal it is *the* thing keeping the public contract free of a specific rasterizer. §6 decides its fate — and shows why that decision must be **sequenced behind** the §5 refactor.

### 3.3 The Skia hosts and the Level-1 GPU-surface layer

*9 Skia host projects (`Uno.UI.Runtime.Skia.{Android, AppleUIKit, Linux.FrameBuffer, MacOS, Tizen, WebAssembly.Browser, Win32, X11, …}`).* Each abstracts only the **GPU surface** where Skia draws (**Level 1**): it acquires an `SKSurface`/`SKCanvas` backed by one of **6 surface families** (GL/WGL/GLX, GLES/EGL, Vulkan, Metal, WebGL, Software) and hands the `SKCanvas` to the **single cross-platform seam** `CompositionTarget.OnNativePlatformFrameRequested(SKCanvas?, Func<Size,SKCanvas>)` (12 call sites). Confirmed facts:
- **Vulkan is a Skia Ganesh backend** (`VulkanContext.skia.cs` → `GRContext.CreateVulkan`), gated by `FeatureConfiguration.Rendering.UseVulkanOnWin32/UseVulkanOnX11` — not a native renderer bypassing Skia.
- **There is no shared host-renderer interface** — each host has a bespoke renderer class. The one named `Uno.UI/Rendering/IRenderer.skia.cs` is trivial (`SKColor BackgroundColor`) and unrelated.
- **`GLCanvasElement`** is the app-level raw-GL escape hatch (`INativeOpenGLWrapper`, implemented by **4** hosts: Win32/X11/MacOS/WinUI): draw GL into an FBO → `glReadPixels` → `WriteableBitmap` → composite as an `ImageBrush`. It is **OpenGL-only via `Silk.NET.OpenGL`** and has a CPU-readback perf ceiling — but it is the working template for spec 003 REQ-004 (WebGPU/3D controls as backend-specific surfaces composited into the Skia scene).
- **Every host has a software fallback** (DIB/XImage/SKBitmap/DRM) — the universal last resort that any Level-2 rasterizer must also preserve for headless/CI.

**Implication:** Level 1 is done and mature; **Skia Graphite adoption is contained to the SkiaSharp/`GRContext` construction inside the hosts** and does not touch the Composition tree. It does **not** by itself advance Level 2.

### 3.4 Project / TFM / solution topology (post-native-removal, as on this branch)

- **UI-rendering libraries collapsed to 2 heads each:** `Uno.UI` and `Uno.UI.Composition` now ship only `.Reference.csproj` + `.Skia.csproj` (their `.Wasm`/`.netcoremobile` UI heads were deleted).
- **Platform-capability libraries keep 4 heads:** `Uno.UWP`, `Uno.Foundation`, `Uno.UI.Dispatching` each keep `{Reference, Skia, Wasm, netcoremobile}` because Skia-on-Android/iOS/WASM consume their per-platform WinRT/dispatcher implementations. (`Uno.UI.Dispatching` is easy to misfile next to `Uno.UI.*` but belongs to the WinRT/`Uno.WinRT` package group.)
- **TFM sets** (root `Directory.Build.props`): `NetSkiaPreviousAndCurrent` = `NetWasmPreviousAndCurrent` = `NetReferencePreviousAndCurrent` = **`net9.0;net10.0`**; `NetMobilePreviousAndCurrent` = **8 TFMs** (`net9.0-android;net9.0-ios18.0;net9.0-maccatalyst18.0;net9.0-tvos18.0;net10.0-android;net10.0-ios26.0;net10.0-maccatalyst26.0;net10.0-tvos26.0`).
- **6 solution filters** remain (`Skia-only`, `Reference-Only`, `Windows-only`, `RemoteControl-Only`, `UnitTests-only`, `Tools`).
- **`Uno.WinUI.nuspec` ships one backend folder today:** `uno-runtime/net9.0/skia` + `uno-runtime/net10.0/skia`, plus Reference `lib/net9.0` + `lib/net10.0` (8 UI DLLs × 2 TFMs, hand-listed). The WinRT layer (`Uno.WinRT.nuspec`) carries the 8 per-platform mobile `lib/net*-<plat>` folders.

**Where a future backend dimension slots (crucial):** a Level-2 backend is **not a new TFM** and **not a 5th project variant**. The topology already models it as **a new value of `UnoRuntimeIdentifier`**, materializing as a sibling folder `uno-runtime/<tfm>/<backend>` (e.g. `uno-runtime/net10.0/webgpu` next to `.../skia`). Options the topology allows:
- **(a)** new `uno-runtime/<tfm>/<backend>` folder + new `UnoRuntimeIdentifier` value — cheapest, reuses `RuntimeAssetsSelectorTask`, single `Uno.WinUI` package, backend fixed per app build;
- **(b)** a build-time selector property (analogous to the old `UnoFeatures skiarenderer`) that sets it;
- **(c)** a separate package (`Uno.WinUI.WebGpu`) resolved like a host.

### 3.5 Skia leakage in the public surface (bounds the breaking-change surface)

A rasterizer swap is source-breaking for **consumers** through **4 public API leaks**:
1. `SkiaExtensions` — public static class, **14** SK conversion methods (`Microsoft.UI.Composition`);
2. `Color ↔ SKColor` implicit operators — public struct (`Windows.UI`);
3. `VulkanImageInfo.PixelSize` — public record struct exposing `SKSizeI`;
4. `SKCanvasElement.RenderOverride(SKCanvas)` — AddIn API, and the one leak that is **genuine subclassable extensibility** (consumers override it *today*), so a swap breaks real user code independent of internals.

**Subtlety:** three of the four live in `.skia.cs` files, so they are excluded from the *reference* compile surface and bite only **Skia-build-facing** compilation (app heads, or anyone compiling against the runtime assembly). That is precisely why **Option B** (making the Skia build the compile surface, §6) is what would *newly* expose them to plain library authors — another reason Option B is regressive before the seam exists.

**A 5th, host-facing contract** a Level-2 swap breaks is **not** a consumer leak but is real: `CompositionTarget.OnNativePlatformFrameRequested(SKCanvas, Func<Size,SKCanvas>)` — it takes an `SKCanvas` and *returns an `SKPath`* (native-element clip) and is the contract all **9 bespoke Skia hosts** (and any third-party embedding host) implement against. It is the seam most coupled to the hosts and the most likely to churn downstream host authors.

**Caveat:** even after all of the above, apps still *transitively* acquire SkiaSharp because `Uno.Sdk` implicitly injects `SkiaSharp.Views` on Skia/WASM heads; and the **Svg/Lottie AddIns** are bound to the Skia family (`SkiaSharp.Skottie`, `SkiaSharp.Views.Uno.WinUI`) with no non-Skia equivalent. So even the fully-realized Option C end-state does **not** deliver a literal "no SkiaSharp in the app" guarantee for Skia heads — it makes SkiaSharp a *backend implementation detail*, not an *absent* dependency.

---

## 4. How Avalonia and Flutter solve it (case studies)

### 4.1 Avalonia — the interface-abstraction model (verified vs. master @ `dab62c09d2`, v12.1.x)

- **Level 2 (rasterizer) is a real, proven abstraction.** Controls draw against the public abstract `DrawingContext` (`Avalonia.Base/Media/DrawingContext.cs`), whose `DrawGeometryCore`/`PushClipCore`/`DrawGlyphRun` take `IGeometryImpl`/`IBitmapImpl` handles — **never a Skia type**. `IPlatformRenderInterface` (factory for `IGeometryImpl`/`IBitmapImpl`/`IGlyphRunImpl`/`IStreamGeometryImpl`) + `IDrawingContextImpl` (the abstract drawing surface) live in `Avalonia.Base/Platform/`. `Avalonia.Skia` is registered via `.UseSkia()` → `AvaloniaLocator.Bind<IPlatformRenderInterface>().ToConstant(...)`. **Proof it's not theoretical:** `Avalonia.Direct2D1` (`.UseDirect2D1()`) was a second rasterizer for years, removed only in Avalonia 12.
- **Level 1 (GPU surface) is a separate abstraction:** `IPlatformGraphics`/`IPlatformGraphicsContext` (OpenGL/Vulkan/Metal) adapted into Skia's `GRContext` via `ISkiaGpu`/`ISkiaGpuRenderTarget` (`Avalonia.Skia/Gpu/{OpenGl,Vulkan,Metal}/`).
- **The compositor sits above the rasterizer:** UI-thread `Compositor` + render-thread `ServerCompositor` (`IRenderLoopTask`); visual changes serialize to a `BatchStreamWriter`; the server replays a recorded draw list (`CompositionDrawListVisual`/`CompositionRenderData`) against `IPlatformRenderInterface`. **The same "record a serializable draw list, replay against an interface" mechanism enables *both* the render-thread split *and* the rasterizer swap.**
- **Packaging:** core + controls + even `Avalonia.Skia` target **plain `net8.0;net10.0`** (netstandard2.0 dropped in v12); platform heads (`Avalonia.Android`=`net10.0-android36.0`, `.iOS`, `.Browser`) are **separate packages**. Library authors reference the neutral core and are **never forced onto `-android/-ios` TFMs**. Backend selection is **runtime DI**, the *opposite* of Uno's compile-time assembly swap.
- **Honest caveat:** `IPlatformRenderInterface`/`IDrawingContextImpl` are marked `[Unstable]`/PrivateApi — an internal extensibility seam, not a public stable backend SPI. If Uno copies the pattern, the same "internal, may churn" tension applies.

**Lessons for Uno:** (1) name the two seams explicitly; (2) the draw-list-recording mechanism buys swappability *and* a cleaner render-thread story at once; (3) a neutral core is what lets control libraries stay platform-TFM-free; (4) single-backend-in-practice ≠ dead abstraction — Direct2D's existence is what made the seam trustworthy.

### 4.2 Flutter — the Level-2-swap-done-right model

- **The framework never talks to Skia (on the native C++-engine path).** Dart widgets → RenderObjects → a **Layer tree**, flushed via `SceneBuilder` into an opaque `Scene`; the objects crossing the `dart:ui` seam (`Scene`, `EngineLayer`, `Picture`, `Canvas`) are opaque handles into the C++ engine. (Flutter *web* has no C++ engine and binds Skia directly via CanvasKit/skwasm — the swap lesson applies to the native path, which is the relevant analog for Uno.)
- **The swap seam is `DisplayList`.** Inside the engine, painting records into a backend-neutral **`DisplayList`** via the **`DlCanvas`** interface, which dispatches ops through **`DlOpReceiver`** (~50+ pure-virtual methods) to *either* **Skia** (`DlSkCanvasDispatcher`) or **Impeller** (`DlDispatcher`). This indirection (introduced 2021–2023 to move the engine off direct `SkCanvas`/`SkPaint`) is exactly why **Impeller replaced Skia with zero framework changes** — "Using a different rendering package will not affect the way in which the Flutter Engine is used."
- **Impeller** = a from-scratch rasterizer on Metal/Vulkan/GLES with **ahead-of-time-compiled shaders**, built to kill runtime shader-compilation jank. Status (Flutter 3.38, Nov 2025): **only engine on iOS**; **default on Android API 29+** (Vulkan, GLES fallback; Skia ≤28); **web still Skia** (CanvasKit/skwasm); desktop still Skia (Impeller-Vulkan in design, #183495).
- **Level 1 is separate:** the embedder's `FlutterRendererConfig` provisions a Metal/Vulkan/OpenGL/software surface, independent of the rasterizer choice.
- **Honest caveats:** `DisplayList` is a **flat 2D draw-op list, not a retained scene graph**; Impeller does **not** consume it byte-for-byte (its dispatcher re-materializes ops into Impeller concepts, with known semantic gaps). `dart:ui` was a hand-built FFI boundary designed backend-neutral *from the start*; `Uno.UI.Composition` mirrors WinUI Composition and is Skia-coupled *now* — so Uno's refactor is genuinely harder than "copy Flutter."

**Lessons for Uno:** Uno's `Visual` tree ↔ Flutter's Layer tree; Uno's cached per-`Visual` `SKPicture` ↔ Flutter's per-`EngineLayer`/`DisplayList`. Uno's `Visual.Paint(in PaintingSession)`-binds-`SKCanvas` **is** Flutter's *pre-2021* coupling. The clean-swap prerequisite is to insert a backend-neutral recorded-draw-op interface (`ISceneRecorder`/`IDlCanvas`) between `Visual.Paint` and SkiaSharp — and Uno's existing `PaintDirty`/`SKPicture`-invalidation machinery would transfer almost unchanged if the recorded unit becomes a neutral display list instead of an `SKPicture`.

### 4.3 Backend feasibility for .NET/Uno (Graphite / WebGPU / Impeller / Vello)

- **Skia Graphite = the cheapest credible "WebGPU," and it's Level 1.** Graphite is Skia's ground-up replacement for Ganesh, driving **Dawn (WebGPU over Metal/Vulkan/D3D12)** plus direct Metal/Vulkan. Default-on in Chrome on Apple Silicon since **July 2025** (~15% MotionMark gain). Adopting it keeps `SkCanvas`/`SkPaint` and Uno's entire Composition scene graph **untouched**.
- **Uno is *already* the partner driving Graphite into SkiaSharp.** `SkiaSharp 4.150.0-preview.2` (late June 2026) headlines "Graphite … contributed by Uno Platform"; `mono/SkiaSharp#3962` (labeled `partner/unoplatform`, driven by an Uno Platform engineer) tracks the C-API with backend priority **Metal → Vulkan → Dawn** and Ganesh/Graphite coexistence. Uno pins **3.119.0** today; stable is 4.148.0; Graphite lands in the 4.15x preview line. **A `3.119 → 4.15x` bump is the dependency** — matching spec 003 Phase 4. First realistically shippable Graphite target is **macOS/iOS (Metal)**, not WASM/WebGPU.
- **WebGPU in .NET = `Silk.NET.WebGPU` (Dawn/wgpu-native), v2.23.0** — mature for native GPU work, **but not in-browser** (the .NET WASM runtime can't run it yet). That's a hard blocker for native-WebGPU on Skia-on-WASM, which stays on Skia (WebGL2 now → Dawn/WebGPU-via-Graphite later).
- **A native WebGPU 2D renderer (Level 2) = a large, speculative effort — and its best .NET on-ramp just regressed.** The reference implementation is **Vello** (Rust/wgpu, GPU-compute), still **alpha**. The community .NET binding **`VelloSharp`** was the *best-fitting* Level-2 option for Uno — it carried a **browser-wasm RID, a `VelloSharp.Uno` adapter, a SkiaSharp compat shim, and a CPU software fallback** — but it was **archived/deprecated (2026-01-31)**, "waiting for the final Vello API… then restarted in a new repo." So today it is a frozen, single-maintainer, `0.5.0-alpha.3` codebase pending a restart.
- **Impeller is *no longer a dead end* — but it is a watch item, not an adoptable backend** (this corrects an earlier absolute "ruled out; no .NET binding" claim). The facts:
  - **Flutter ships a *standalone* "Impeller Toolkit"** — an exported, single-header **C ABI** (`impeller.h`) with prebuilt `impeller_sdk.zip` binaries on Google's CDN for **linux/windows/darwin/android** (all common arches; **no iOS prebuilt, no WASM**). The ABI explicitly has **no stability guarantees** yet. So "engine-internal / symbols unexported" was **wrong**.
  - **A .NET binding exists: `AvaloniaUI/NImpeller`** — an active P/Invoke binding (created 2025-10-14, worked on through mid-2026, 262★) generated from `impeller.h`, exposing **Metal *and* Vulkan** backends. It is driven by Tim Miller (drasticactions) with **Avalonia's founder Nikita Tsukanov** contributing, out of an **Avalonia–Flutter collaboration** (Impeller's creator reached out). So "no .NET binding" was **wrong**.
  - **Why it's still not adoptable for Uno now:** it is **early plumbing** — no NuGet, no release/tag, **no LICENSE** (MIT only *planned*), SDL2-GLES-tested-on-Linux-only per its own README, pulling native libs from **Flutter's nightly feed** (moving target). It is a **raw binding with no rendering-backend layer** (zero `IPlatformRenderInterface`/`IDrawingContextImpl` — Avalonia's own Impeller backend lives on a private, experimental branch, to be open-sourced "when ready"). Standalone Impeller is **GPU-only with no software fallback** and — decisively for Uno — has **no WASM/browser story at all**.
- **REQ-004 (3D/GPU controls):** plan a **per-backend surface seam** (GL/Vulkan/Metal/WebGPU each an `INativeContext`-style surface) under a shared `Silk.NET` umbrella, **not** one unified 3D context. `GLCanvasElement` is the OpenGL-bound starting point; `VulkanCanvasElement`/`WebGPUCanvasElement` are tractable increments.

**Ranked backend paths for Uno** (all Level-2 options are pre-production; the recommendation does **not** flip): ① **Graphite-over-Dawn/Vulkan via SkiaSharp** — low-effort/high-payoff, already in flight, Uno co-maintains SkiaSharp (**Level 1**). ② *(Level-2, gated watch items, not bets)* **Impeller via `NImpeller`** — strongest upstream backing (Avalonia + Flutter collaboration, actively developed, real standalone C ABI), but experimental, NuGet-less, browser-less, Avalonia/Google-owned, no software fallback. ③ **Vello via `VelloSharp`** — technically the best Uno fit (browser-wasm + `VelloSharp.Uno` + CPU fallback) but the binding is **archived pending a restart**. ④ **From-scratch native WebGPU renderer** — multi-year, no artifact.

> **Signal worth weighing:** the fact that **Avalonia's founder is binding standalone Impeller in .NET, in collaboration with Flutter's Impeller creator, to sit behind Avalonia's `IDrawingContextImpl` abstraction**, is real-world **validation of the Level-2-seam thesis** (§5) — it demonstrates both that a rendering-abstraction seam pays off (a second rasterizer can be bolted behind it) *and* that standalone Impeller is a credible .NET rasterizer target. It **strengthens the case for building Uno's seam as future-proofing (Posture 2)** even while the near-term backend bet (Graphite, Level 1) is unchanged. The WASM gap is what keeps every Level-2 option off the near-term critical path — Skia already runs in Uno's browser target.

---

## 5. Proposed target architecture

**5.1 Establish a backend-neutral contract (the non-negotiable core).** No public **or** internal type in `Uno.UI`/`Uno.UI.Composition` may reference a rasterizer type. Concretely: retype the render seam from `SKCanvas` to an abstract `IDrawingSession`/`ISceneRecorder` (Uno's `DlCanvas`/`DisplayList` analog, informed by Avalonia's `IDrawingContextImpl` and Flutter's `DlOpReceiver`), with a compact op vocabulary (`DrawRect`/`DrawPath`/`DrawImage`/`DrawGlyphRun`/`PushClip`/`PushOpacity`/`PushTransform`/`DrawDisplayList`). Remove the 4 public Skia leaks (§3.5) behind neutral equivalents.

**5.2 Ship Skia as a backend (`Uno.UI.Composition.Skia`).** The `.skia.cs` draw bodies move behind the seam into a **separate backend assembly** that carries the `SkiaSharp`/`HarfBuzzSharp` PackageReferences, selected at **host startup** via `ApiExtensibility`/DI — reusing the exact idiom the Skia hosts already use to inject platform services, and the `SKCanvasVisualBase` "canvas-as-`object`" pattern that already exists. This is Avalonia's `Avalonia.Base` + separate `Avalonia.Skia` split, adapted to Uno.

**5.3 Two routes, and why we blend them.**
- **Route A — compile-time backend variants** (`UnoRuntimeIdentifier=WebGPU` + `.webgpu.cs` partials, swapped by `RuntimeAssetsSelectorTask` exactly like `skia`/`wasm`). Nearly free structurally (the machinery exists), but the backend is fixed per app build.
- **Route B — runtime backend abstraction** (`IDrawingBackend` loaded at startup, Avalonia/Flutter model). Runtime-switchable, cleaner, keeps the neutral core truly Skia-free — but requires the §5.1 de-Skia-ing refactor.
- **Recommendation: Route B for the seam** (it is what makes the contract neutral and the reference-build simplification safe — §6), **optionally reusing Route A's packaging machinery** if a backend must be shipped as a swapped `uno-runtime/<tfm>/<backend>` folder rather than a startup-selected assembly.

**5.4 What must move behind the seam (the work-list):**
- the ~31 drawing `Visual`/`Brush` partials; geometry (`SKPath` verb-walking, stroke/dash/cap/miter);
- **the per-`Visual` `SKPicture` cache + `PaintDirty`/`ChildrenSKPictureInvalid` invalidation + the picture-collapsing optimization** — Uno's current performance crown jewels, and intimately `SKPicture`-shaped (`sk_refcnt_safe_unref`, `sk_canvas_draw_picture`). **This is the decisive design choice, not a mechanical port:** the neutral recorded unit must be a **retained, per-`Visual`, cacheable display list** that maps 1:1 onto this machinery — *not* a flat per-frame op list (Flutter's `DisplayList` is flat; Uno's retained cache is why the analogy is only partial). Resolve **Open Q1 in favour of a retained IR** before Phase 2 begins;
- the render-loop entry + host handoff in `Uno.UI`: `SkiaRenderHelper`, and the `OnNativePlatformFrameRequested` **`SKCanvas`-in/`SKPath`-out** host contract (§3.5);
- **text — decomposed into two cuts, not "ported":** (a) a neutral **font/table/metrics provider** (today `SKTypeface`-backed; `FontDetails.skia.cs` currently *fuses* `SKFont`+`SKFontMetrics`+a HarfBuzz `Font` into one memoized record) and (b) a neutral **`DrawGlyphRun`** op (today `SKTextBlob`-backed), with HarfBuzz shaping neutrally between them;
- **effects** (`CompositionEffectBrush` D2D→SkSL shader-graph); `RenderTargetBitmap`/`CompositionVisualSurface`/`AlphaMaskSurface` offscreen/readback.

Behavioral-parity risk is the real cost driver (Skia's exact dash/miter/AA/shadow/effect output is baked into visual-regression baselines) — treat pixel-diff regression, **and preserving the SKPicture-cache performance**, as the primary hazards, not LOC.

---

## 6. Decision — the reference build & assembly-swap

The options, with the pure-`net10.0`-library promise and rendering-agnostic fit made explicit:

| Option | Reference build | Pure-`net10.0` library promise | Rendering-agnostic fit | Notes |
|---|---|---|---|---|
| **A. Keep as-is** | kept for all libs | preserved | neutral surface preserved (accidental) | status quo; most build complexity (extra variant + task everywhere) |
| **B. Drop reference for UI libs; *Skia build becomes the compile surface*** | dropped for `Uno.UI`/Composition/Toolkit/FluentTheme | preserved *at runtime* | **REGRESSIVE** — pulls `SkiaSharp`/`HarfBuzz` into the consumer compile closure and exposes the Skia build's SK-typed public members as the compile surface; cements Skia into the public contract | the naive "simplify now" move; **conflicts with the goal if done before §5** |
| **C. Drop reference for UI libs *because the UI layer is backend-neutral* (§5 done)** | dropped for UI libs; the neutral UI assembly is its own compile+runtime surface | preserved *and strengthened* | **INTENTIONAL** — Skia lives only in the separate `Uno.UI.Composition.Skia` backend, never in the compile closure | the recommended end-state; **requires §5 first** |
| **D. Drop reference entirely** | none | **broken** for two-layer WinRT | regressive | off the table — WinRT libs keep per-platform impls |

**`Uno.UWP`/`Uno.Foundation`/`Uno.UI.Dispatching` keep per-platform WinRT impls**, so the **two-layer swap + a neutral compile surface remain necessary there regardless of rendering** (`RuntimeAssetsSelectorTask.IsWinRTAssembly` is hard-limited to those 3 assemblies). So `RuntimeAssetsSelectorTask` **cannot be deleted** in 7.0 — only its `Uno.UI` branch simplifies.

**Recommendation & sequencing:**
1. **7.0 (now): Option A for the UI layer — keep the reference build unchanged.** Dropping it early (Option B) is the trap: it makes Skia the compile surface and *cements* the coupling this whole effort exists to remove. The packaging saving is real but small; the architectural cost is large and hard to reverse.
2. **After the §5 Level-2 refactor: move to Option C.** A backend-neutral `Uno.UI`/`Composition` is its own compile+runtime surface → drop the UI reference variant (removes ~one variant per UI project and a large slice of the hand-written nuspec), keep reference + two-layer swap only for the WinRT libs, simplify the task's UI branch. Trimming/AOT is unaffected (it always operated on the runtime impl; the reference build's ~60 suppressed `IL2xxx/IL3xxx` warnings must be re-verified clean on the neutral assembly).
3. **If §5 slips past 7.0**, stay on Option A. Do **not** take Option B as an interim "cleanup" — it is a one-way door away from rendering-agnosticism.
4. **Under Posture 1 (Level-1-only), Option A is the *permanent* answer, not an interim.** Keeping the UI reference build costs little — an extra csproj variant and some hand-written nuspec entries — and the packaging saving from dropping it is **real but small**. So the reference-build decision is genuinely *low-stakes* unless and until Posture 2 is funded: the reference build is cheap neutrality insurance worth keeping by default. The value of §6 is mostly in **not** making the *wrong* move (Option B), rather than in urgency to make the right one.

> In one line: **don't drop the UI reference build to simplify packaging; drop it as a *consequence* of making the UI layer backend-neutral.** Same destination, opposite causality — and only the second order is safe. Absent Posture 2, keeping it forever is perfectly fine.

---

## 7. NuGet & solution restructuring

- **A backend is a runtime/packaging dimension, not a TFM or a public-package split.** Keep control libraries plain `net9.0;net10.0`. Two viable shapes:
  - **Preferred — separate backend package + startup DI (Avalonia model):** `Uno.UI.Composition.Skia` (and later `Uno.UI.Composition.WebGpu`) as a backend assembly the host wires up. Runtime-selectable; keeps the neutral core's compile closure Skia-free; no new `uno-runtime` folder needed for the UI layer.
  - **Fallback — `uno-runtime/<tfm>/<backend>` folder + `UnoRuntimeIdentifier` value:** reuse `RuntimeAssetsSelectorTask` to swap the whole UI implementation assembly per backend (like `skia`/`webassembly` today). Backend fixed per app build; requires updating both the task's selection logic **and** the hand-maintained nuspec `<file>` entries.
- **Nuspecs are static, hand-maintained files** (`build/nuget/Uno.WinUI.nuspec` etc.) — any variant/backend change is a manual, error-prone edit; a mismatch silently produces a package that restores but fails at runtime. Automating nuspec generation from the project graph is a worthwhile hardening independent of this analysis.
- **Solution/topology cleanups** surfaced en route: an orphaned duplicate bare `Uno.Foundation.csproj` (Skia identity, in no `.slnf`) is dead build weight; `NetWasmPreviousAndCurrent` still exists (kept for the WASM *WinRT/dispatcher* layer, not a DOM UI renderer) and its naming invites confusion.
- **Don't over-collapse `__CROSSRUNTIME__`.** It currently unions `{Skia, WebAssembly, Reference}`; for UI libs that's effectively `{Skia, Reference}`. A future non-Skia *managed* backend would naturally also be `IsCrossruntime` and inherit `*.crossruntime.cs` — but would need its own `IsCrossruntime` carve-out and a `.<backend>.cs` suffix rule in `Uno.CrossTargetting.targets` if it takes Route A.

---

## 8. Recommendation & phased roadmap

Sequenced against spec 003 (backend phase-gates) and spec 043 (7.0 native removal):

| Phase | Work | Level | When | Gate |
|---|---|---|---|---|
| **0** | Adopt the two-level vocabulary; audit & document the 4 public Skia leaks; keep reference build **as-is** for UI (Option A) | — | 7.0 | This document |
| **1** | **SkiaSharp `3.119 → 4.15x` bump**; enable **Graphite** (Metal → Vulkan → Dawn), Uno's own SkiaSharp contribution (#3962) | **1** | Post-7.0, per spec 003 Phase 4 | Graphite-vs-Ganesh benchmark (frame time, jank, stability); macOS/iOS Metal first |
| **2** | Introduce the **retained `IDrawingBackend`/`ISceneRecorder` seam**; move `Visual.Paint`/`PaintingSession` + brushes + geometry + the render-loop/host handoff behind it; extract Skia into an **additive backend assembly** (`Uno.Graphics.Skia`, not the existing `Uno.UI.Composition.Skia` variant) | **2** | Multi-release | **The neutral recorded unit maps 1:1 onto the existing `SKPicture` cache + collapsing optimization with no measured perf regression**; no *visible* regression within the established pixel-diff tolerance; the 4 public leaks removed/neutralized |
| **3** | **Decompose** text into a neutral font/table/metrics provider + a neutral `DrawGlyphRun` (HarfBuzz shaping between), and **effects** (shader-graph) behind the seam | **2** | Multi-release | Text/effects parity within tolerance on the Skia backend |
| **4** | **Drop the UI reference build (Option C)**; simplify `RuntimeAssetsSelectorTask`'s UI branch; keep reference + two-layer only for WinRT libs | — | After Phase 2–3 | Neutral `Uno.UI` compiles Skia-free; consumer builds unaffected; trimming/AOT re-verified |
| **5 (optional)** | Prototype a **second backend behind the seam** — Impeller (`NImpeller`) or Vello (`VelloSharp`, once its restart lands) — *only if* a measurable win justifies it and the ecosystem matures (NuGet/license/**WASM**) | **2** | Speculative | A jank/perf win over Graphite that pays for the risk; a non-frozen, browser-capable .NET binding exists |

**Non-negotiables & sequencing traps:** Impeller/Vello stay *watch items*, not near-term bets (track `NImpeller`, Avalonia's Impeller backend open-sourcing, and the `VelloSharp` restart as leading indicators); software fallback is preserved through every change (standalone Impeller has none — another reason it can't be a sole backend); in-browser WASM stays on Skia (WebGL2 → Graphite/Dawn) until the .NET WASM runtime can host native WebGPU. **Baseline ordering:** the `3.119 → 4.15x` + Graphite bump (Phase 1) shifts visual output, so **capture and settle the visual-regression baselines on the post-Graphite Skia** *before* Phase-2 seam extraction — otherwise the neutral-backend diff targets stale `3.119` output that will already have moved. **AOT/trimming:** the Route-B startup backend registry must be **AOT/NativeAOT-friendly** (source-generated/static registration, not reflection-based plugin discovery) — a reflective backend resolver would regress the trimming/AOT story the reference build already protects.

---

## 9. Risks

- **Behavioral parity, not LOC, is the cost.** Skia's dash/miter/AA/shadow/effect output is baked into visual baselines; a Level-2 backend swap risks pixel-diff regressions far beyond the line count. Re-baseline and diff aggressively.
- **Text is the stickiest coupling.** HarfBuzz sourcing font tables through `SKTypeface` means "keep HarfBuzz" is insufficient — the typeface/table/metrics provider must be re-supplied for any non-Skia backend.
- **Graphite is preview-grade** in SkiaSharp (mid-2026) and Uno is still writing it; the `3.119 → 4.15x` jump is itself a non-trivial migration (v4 promoted obsolete members to errors). Dawn adds native build/toolchain surface across the package matrix.
- **Option B is a one-way door.** Dropping the UI reference build before the neutral seam exists cements Skia into the public contract.
- **AddIns (Svg/Lottie) and implicit `SkiaSharp.Views` injection** mean "no SkiaSharp in the app" is not automatic even after the 4 public leaks are fixed.
- **The seam is `[Unstable]`-shaped** (as Avalonia's is): a backend SPI is inherently an internal, may-churn surface — set expectations accordingly.

## 10. Open questions

1. **Op vocabulary & IR — leaning resolved (§5.4):** the seam should be a **retained, per-`Visual`, cacheable display list**, not an immediate-mode `IDrawingSession`, because Uno's `SKPicture` cache + `PaintDirty` invalidation model assumes a cached recorded unit. (Flutter's flat `DisplayList` is *not* the right analog here.) Confirm the exact op vocabulary against the ~31 drawing partials before Phase 2.
2. **Backend lifetime:** is the backend a process-wide singleton resolved once before first paint, and how do offscreen sessions (`RenderTargetBitmap`/`VisualSurface`/`AlphaMaskSurface`) obtain the *same* backend consistently?
2. **Text engine:** abstract shaping in the same `IDrawingBackend` seam, or a distinct "text engine" port (HarfBuzz is a separate package)?
3. **Consumer contract:** are any of the 4 public Skia leaks (esp. `SKCanvasElement.RenderOverride`, `RenderTargetBitmap`) intended public extensibility that a swap makes a breaking change independent of internals?
4. **Packaging model:** separate backend package + startup DI (Avalonia) vs. `uno-runtime/<tfm>/<backend>` swap — pick per whether runtime backend-switching is a requirement.
5. **macOS Metal-native path:** macOS drives Metal via native Obj-C; does a Level-2/WebGPU path there require reworking the native side or can it stay Metal-native with a swapped upper layer?
6. **WinRT reference unification:** could `Uno.UWP`/`Uno.Foundation` also unify their reference surface, or must it stay because no per-platform impl is a valid universal compile surface? (Likely stays.)

---

## Appendix A — Verified metrics (snapshot: `dev/mazi/rendereragnostic`)

| Metric | Value |
|---|---|
| `.skia.cs` files: `Uno.UI` / `Uno.UI.Composition` / all `src` | 131 / 54 / 237 |
| `.skia.cs` LOC (`wc -l`): `Uno.UI` / `Composition` | ~25,407 / ~8,354 |
| Composition `.skia.cs` partials doing real drawing | 31 of 54 |
| SkiaSharp-*type*-referencing files, `Uno.UI` (total / `.skia.cs` / shared `#if __SKIA__`) | 41 / 37 / 4 (+`SKCanvasVisualBase`, comment-only) |
| SkiaSharp-referencing files, `Composition` (total / shared leakage) | 46 / 0 |
| General libs with a direct `SkiaSharp` PackageReference | 2 (`Uno.UI.Composition.Skia`; `Uno.UWP/Uno.Skia`) |
| General libs 100% Skia-free | `Uno.Foundation`, `Uno.UI.Dispatching`, `Uno.UI.Toolkit`, `Uno.UI.FluentTheme(.v1/.v2)`, `Uno.UI.XamlHost` |
| Public consumer-visible Skia API leaks | 4 |
| `.reference.cs` stub files in `Uno.UI`; `__NETSTD_REFERENCE__` files/occurrences | 26; 38 / 55 |
| Skia host projects / GPU-surface families / `INativeOpenGLWrapper` impls | 9 / 6 / 4 |
| UI-lib heads (post-native) / platform-capability-lib heads | 2 (Reference, Skia) / 4 (+Wasm, netcoremobile) |
| UI/Skia/Reference TFMs / mobile TFMs | `net9.0;net10.0` / 8 |
| SkiaSharp pinned (repo) / stable / Graphite-preview line | 3.119.0 / 4.148.0 / 4.15x |

## Appendix B — Primary sources

- **Uno codebase:** `Uno.CrossTargetting.targets`, `Uno.UI.Reference.csproj`/`Uno.UI.Skia.csproj`, `RuntimeAssetsSelector/RuntimeAssetsSelectorTask.cs`, `build/nuget/uno.winui.runtime-replace.targets`, `build/nuget/Uno.WinUI.nuspec`/`Uno.WinRT.nuspec`, `Uno.UI.Composition/Composition/**.skia.cs` (`Visual.skia.cs`, `Visual.PaintingSession.skia.cs`, `Compositor.skia.cs`, `CompositionSpriteShape.skia.cs`, `Uno/ISkiaSurface.skia.cs`), `Uno.UI/Rendering/IRenderer.skia.cs`, `Uno.UI/Vulkan/VulkanContext.skia.cs`, `Uno.UI/Graphics/INativeOpenGLWrapper.cs`, `GLCanvasElement`.
- **Avalonia (master @ `dab62c09d2`):** `Avalonia.Base/Platform/IPlatformRenderInterface.cs`, `IDrawingContextImpl.cs`, `IPlatformGpu.cs`; `Avalonia.Base/Media/DrawingContext.cs`; `Avalonia.Base/Rendering/Composition/**`; `Avalonia.Skia/Gpu/{OpenGl,Vulkan,Metal}/`; Avalonia 12 breaking-changes (Direct2D removal; netstandard2.0 drop).
- **Flutter:** `dart:ui` `SceneBuilder`/`EngineLayer`; engine `display_list` (`DlCanvas`/`DisplayList`/`DlOpReceiver`, PRs #39762/#29470); Impeller README + FAQ; Flutter 3.27–3.38 Impeller default-status notes; embedder `FlutterRendererConfig`.
- **Backends:** skia.org Graphite docs + Chromium Graphite blog (2025-07); `mono/SkiaSharp` #3962 + 4.150.0-preview release notes; `Silk.NET.WebGPU`; linebender **Vello** + `VelloSharp` (archived 2026-01-31); **Flutter Impeller Toolkit** standalone C ABI (`flutter/impeller/toolkit/interop`, `impeller_sdk.zip`) + **`AvaloniaUI/NImpeller`** .NET binding + Avalonia's Impeller-collaboration blog.
- **Packaging:** Microsoft Learn (reference assemblies; NuGet `ref/`/`lib/`/`runtimes/{rid}` asset selection — "no per-RID compile substitution"); MAUI multi-targeting; WinUI/WinAppSDK Windows-only class libraries.

*Grounded in first-hand inspection + a 9-agent adversarially-verified research workflow (`wf_034ab896`, 18 agents, ~2M tokens). Counts are a branch snapshot and should be re-checked at implementation time.*
