# Scoping: a SkiaSharp-free *build* of Uno (Skia targets) — WebGPU + managed engines

Status: scoping/analysis (no implementation yet). Companion to `RUNNING-CONTEXT.md` §42 (which proved the
SkiaSharp-free **run**). This doc scopes removing the SkiaSharp managed **assembly reference** from a build.

## 1. Goal & definition of done

Today (§42): SamplesApp *runs* with no native `libSkiaSharp.so` (WebGPU + managed geometry/fonts/decoder). But the
SkiaSharp **managed assembly** is still referenced and compiled in — the Skia backend types exist and self-register;
the managed path just wins at runtime when toggled.

**Done =** produce a build flavor of a Skia head (start with X11/Win32 desktop, then macOS/iOS/WASM/Android) +
SamplesApp where **`SkiaSharp.dll` is not referenced by any assembly in the closure**, it still renders, and CI can
assert the closure is Skia-free (e.g. `!File.Exists(SkiaSharp.dll)` next to the app, or an assembly-scan test).

This is intentionally a **build flavor**, not a replacement: the Skia backend stays the default; a
`-p:UnoDrawingBackend=WebGpu` (working name) build drops it.

## 2. Current dependency map (assembly level, measured 2026-08-12)

Neutral / already SkiaSharp-clean at the type level:
- `Uno.UI.Composition.Drawing` — the neutral seam (interfaces only). CLEAN.
- `Uno.UI.Composition` (assembly; built by the confusingly-named `Uno.UI.Composition.Skia.csproj`) — core
  composition. References HarfBuzzSharp (text metrics/shaping) but **no SkiaSharp** (the 2 grep hits are comments).
- `Uno.UI` — **0** direct SkiaSharp uses. Couples to the backend only via 3 hard references (see §3).
- Managed engines already exist in `Uno.UI.Composition`: `ManagedFontProvider`/`ManagedFont` (TTF/CFF/COLR outline
  parser, HarfBuzz shaping only), `ManagedGeometry`/`ManagedPathBuilder`, `ManagedImageDecoder` +
  `ManagedImageDecoderBackend` (§42). Toggled by `DrawingBackendOptions`.

The designated Skia assembly (keep, make optional):
- `Uno.UI.Composition.Skia` (assembly; built by `Uno.UI.Composition.SkiaBackend.csproj`) — 35 files, the whole Skia
  drawing impl (`SkiaDrawingFactory`, `SkiaFont`, `SkiaImageDecoder`, `SkiaGeometryInterop`, `SkiaRenderer`,
  `SkiaBackend.Register`, `SKCanvasVisualFactory`). This is where SkiaSharp *belongs*.

SkiaSharp "leak" surfaces to address (real work):
- `Uno.UWP` (Uno.Skia.csproj) — 4 files: `Color.skia.cs` (`SKColor`↔`Color` implicit operators),
  `SoftwareBitmap.skia.cs` (`ToSKColorType`), `BitmapEncoder.skia.cs` (+`.cs`) (WinRT encode via `SKImage`/`SKData`).
- Platform heads (each also project-references the SkiaBackend and has platform Skia files):
  - Win32 — 9: the `Rendering.{Software,OpenGl,Vulkan,IRenderer,cs}` surfaces feeding `SkiaRenderer`, plus
    Clipboard (image get/set), NativeElementHosting.
  - X11 — 3: `X11AirspaceRenderHelper`, `X11ClipboardExtension` (image paste), `X11HostBuilder` (backend registration).
  - MacOS — 1, AppleUIKit — 2, Android — 5, WASM — 4, Linux.FrameBuffer — 4 (software render surface, GL interop,
    snapshot/RenderTargetBitmap, clipboard).
- AddIns (already flavored `.Skia`/`.Reference`/`.Crossruntime`, inherently Skia — keep optional): `Uno.UI.Lottie`
  (Skottie), `Uno.UI.Svg` (Svg.Skia — but the managed SVG engine already exists per memory), `Uno.WinUI.Graphics2DSK`
  (SKCanvasElement — a PUBLIC API that exposes `SKCanvas` to user code; can never be Skia-free — stays in the Skia pkg).

## 3. The crux: `Uno.UI` → SkiaBackend hard coupling (3 references)

For a Skia-free build these three compile-time references to the `Uno.UI.Composition.Skia` (backend) assembly must
become pluggable:

1. **Project reference** — `Uno.UI/Uno.UI.Skia.csproj:42`:
   `<ProjectReference Include="..\Uno.UI.Composition.Skia\Uno.UI.Composition.SkiaBackend.csproj" />`.
2. **Backend registration** — `Uno.UI/UI/Xaml/Application.skia.cs:110`:
   `Uno.UI.Composition.Skia.SkiaBackend.Register();` (hard type reference + call).
3. **Default renderer** — `Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:29`:
   `internal static IRenderer Renderer { get; set; } = new SkiaRenderer();` (hard type reference to the backend).

Everything else in `Uno.UI` is neutral (`SkiaRenderHelper` is misnamed but uses only the neutral seam;
`CompositionTarget` records frames through `IRenderer`/`IDrawingSession`). So the framework is ONE indirection away
from backend-agnostic: replace these 3 compile-time references with runtime resolution.

## 4. Phased plan (each phase independently shippable & testable)

**Phase A — decouple `Uno.UI` from the backend assembly (the enabler).**
- Introduce a backend-registration seam the framework calls without a type reference: e.g. a
  `[ModuleInitializer]`/`ApiExtensibility` contract, or an `IDrawingBackend { Register(); IRenderer CreateRenderer(); }`
  resolved from `GraphicsRegistry`/`DrawingFactory` (both already exist as neutral registries). The SkiaBackend and
  WebGpuBackend each register themselves; `Application.skia.cs` calls the resolved backend, not `SkiaBackend`.
- Default `CompositionTarget.Renderer` becomes "the registered backend's renderer" (lazy) instead of `new SkiaRenderer()`.
- Make `Uno.UI.Skia.csproj`'s SkiaBackend ProjectReference conditional on a build property (`UnoDrawingBackend`).
- Risk: MEDIUM (touches startup + default-renderer for ALL Skia targets). Mitigation: default property = current
  behavior (Skia referenced); the neutral resolution must fall back to Skia identically when present.
- Validation: existing Skia runtime-tests must pass unchanged with the default flavor.

**Phase B — the `UnoDrawingBackend=WebGpu` head build (desktop first: X11 + Win32).**
- Under the property: drop the SkiaBackend project ref; the head's `.skia.cs` Skia-render files
  (`Win32WindowWrapper.Rendering.{Software,OpenGl,Vulkan}`, `X11AirspaceRenderHelper`) are `<Compile Remove>`d or
  `#if`-guarded; the head registers WebGpu + managed engines by default (no env toggle).
- Provide managed replacements / guards for the head's remaining Skia utilities:
  - Clipboard image get/set → managed encode/decode (Phase D) or NotImplemented-guard.
  - Snapshot/RenderTargetBitmap → already `WebGpuBackend.ReadPixelsRgba` off-browser.
- Risk: MEDIUM. Validation: the §42 test but with libSkiaSharp *never built into the output* + an assembly-scan
  assert (no `SkiaSharp.dll` in the publish dir).

**Phase C — `Uno.UWP` imaging (4 files).**
- `Color.skia.cs` `SKColor` operators → move into the SkiaBackend (or guard); they're convenience interop, not core.
- `SoftwareBitmap`/`BitmapEncoder` → managed PNG/JPEG **encoder** (decode already managed). Encoder is new work
  (managed PNG encode is modest; JPEG encode is larger — consider guarding JPEG encode initially).
- Risk: LOW–MEDIUM (encoder scope). Most apps decode, few encode.

**Phase D — AddIns & public raw-Skia API.**
- `Uno.WinUI.Graphics2DSK` (SKCanvasElement) stays Skia-only by definition → ensure a WebGpu build simply doesn't
  reference it (it's an optional package already). Document that raw-`SKCanvas` drawing is unavailable in a Skia-free build.
- Lottie/Svg → the managed SVG engine already exists (memory: "Managed SVG engine"); Lottie(Skottie) stays optional.
- Risk: LOW (already flavored packages).

**Phase E — CI proof & cross-platform.**
- Add a build-closure assert (test or MSBuild check) that `SkiaSharp` is absent from the `UnoDrawingBackend=WebGpu`
  publish closure. Extend the flavor to macOS/iOS/WASM/Android heads (each has 1–5 Skia files to guard).

## 5. Open decisions (need the user)

1. **Delivery shape**: a build property (`UnoDrawingBackend=WebGpu|Skia|Both`, default `Skia`) that conditionally
   includes references/files — vs. separate assemblies/packages. Recommendation: **build property** first (least
   churn, one codebase), package split later if shipped.
2. **Scope of "done"**: is proving it on **one desktop head** (X11 or Win32) the milestone, or all Skia heads?
   Recommendation: **X11 desktop first** (headless-testable here), then fan out.
3. **Encoder gap**: implement a managed PNG encoder now, or guard `BitmapEncoder`/clipboard-image as NotImplemented in
   the Skia-free flavor initially? Recommendation: **guard first**, implement managed PNG encode as a fast follow.
4. **Public API**: accept that `SKCanvasElement` (raw-Skia drawing) is unavailable in a Skia-free build? (It must be.)

## 6. Recommended first slice

Phase A + Phase B for **X11 only**, gated behind `UnoDrawingBackend=WebGpu`, validated headless here with the §42
recipe **plus** an assembly-scan assert that no `SkiaSharp.dll` is in the output. That converts §42's "runs without the
native lib" into "builds without the managed assembly" for one head — the real proof — with the framework decoupling
(Phase A) being the reusable enabler for every other head.

Estimated effort for the first slice: Phase A ~1–2 focused sessions (startup/renderer indirection + keep Skia default
green), Phase B/X11 ~1 session (property + file guards + managed-engine defaults + CI assert). Clipboard-image and
BitmapEncoder guarded (not implemented) in this slice.

---

## 7. Progress — Phase A DONE (framework decoupled + app-level flags), runtime-validated

Decision taken (user): **explicit app registration** — Uno.UI is backend-agnostic and packaged once; the app/head
registers a backend under app-level build flags. Implemented + validated headless on Linux/X11 (commit follows §6).

Done:
- **Uno.UI decoupled** from the Skia backend assembly:
  - `Application.StartPartial` no longer calls `SkiaBackend.Register()` (the app entry does).
  - `CompositionTarget.Renderer` default now resolves via the neutral `DrawingRegistration.DefaultRenderer`
    (set by `SkiaBackend.Register`) instead of `new SkiaRenderer()`. Uno.UI references no backend type.
  - Dropped the `SkiaBackend` ProjectReference from `Uno.UI.Skia.csproj`.
- **New neutral pieces**: `DrawingRegistration.DefaultRenderer` (Drawing seam); `ManagedBackend.Register()`
  (managed factory + decoder + font provider) for the SkiaSharp-free app path; `DrawingFactory.RegisterDefault`
  (register-if-absent).
- **Module-init fix (the key runtime enabler)**: `SkiaDrawingFactory`'s `[ModuleInitializer]` now uses
  `RegisterDefault`, so even though the Skia assembly is still in the closure (hosts reference it), its
  self-registration no longer clobbers an explicit `ManagedBackend.Register()`. This is what lets the app-level flag
  actually pick managed at runtime without gating the hosts yet.
- **App-level build flags** on `SamplesApp.Skia.Generic`: `UnoDrawingBackendSkia` / `UnoDrawingBackendWebGpu`
  (both default `true` = current behavior). They conditionally add the backend ProjectReferences and define
  `UNO_DRAWING_SKIA` / `UNO_DRAWING_WEBGPU`, which gate the `#if` registration in `Program.cs`.
- **Hosts + RuntimeTests** now reference the Skia backend directly (X11/Win32/MacOS/LinuxFB + RuntimeTests), since
  Uno.UI no longer provides it transitively.

Validated (headless Linux/X11, lavapipe):
- DEFAULT (`UnoDrawingBackendSkia=true`): builds green, renders (154 draws/frame), no "no backend" errors.
- SKIA-OFF (`-p:UnoDrawingBackendSkia=false`): builds green; runs on **ManagedBackend + WebGPU**; with every
  `libSkiaSharp.*` deleted it renders (surface + managed-decoded `ImageTexture.upload`) with **zero Skia touch**,
  no abort. i.e. the app-level flag now yields a **runtime** Skia-free app.

## 8. Remaining for a Skia-free CLOSURE ON DISK (Phase B) — beyond runtime

The `-p:UnoDrawingBackendSkia=false` app is Skia-free at RUNTIME, but `SkiaSharp.dll` / `Uno.UI.Composition.Skia.dll`
are **still in the output** because the Skia runtime hosts (X11/Win32/MacOS/LinuxFB) reference the backend
unconditionally (they use `SkiaGeometryInterop` for native-clip SVG and register `SkiaGraphicsProvider` as a WebGPU
fallback). To drop them from disk (true "builds without SkiaSharp"):
1. Gate the hosts' Skia usage behind the same flags (host-level `#if`/conditional refs): the `SkiaGraphicsProvider`
   fallback and the `.Rendering.{Software,OpenGl,Vulkan}` Skia surfaces.
2. Provide a **managed geometry → SVG-path** converter so the native-clip path (`SkiaGeometryInterop.Lease(...)
   .ToSvgPathData()`) has a Skia-free equivalent, OR route clip via a neutral seam.
3. `Uno.UWP` imaging (`Color`/`SoftwareBitmap`/`BitmapEncoder` — 4 files): managed PNG encode or guard.
4. Add a CI assembly-scan assert (no `SkiaSharp.dll` in the `UnoDrawingBackendSkia=false` publish closure).
Contradiction to note: "flags only on SamplesApp" can't achieve an on-disk Skia-free closure while the shared hosts
drag the backend — Phase B necessarily extends the flags to the hosts (or gates their Skia code paths).
