# Drawing-seam consolidation

> **STATUS: DONE.** Step A `7f2030a9e2`, Step B `9092b38541` (GL-loader prereq `89c6c0bbfb`). Built: desktop
> (X11/Win32/macOS/Linux.FrameBuffer), Android (`net10.0-android`), WASM (`net10.0`), WebGpu. Runtime-validated
> on X11/lavapipe (OpenGL/OpenGLES/Software render identically, no crash). WebGpu + Win32/macOS/Android/iOS
> runtime handed off. Follow-up: wire Vulkan (`IVulkanRenderTarget` + arm); refresh
> `doc/uno-drawing-backend-abstraction.md`.


Consolidates the pluggable-backend seam in `Uno.UI.Composition.Drawing` after a design review. Goal: a
render backend is a **single, device-bound object** whose surface is **fully typed — no casts, no type
switches, nothing pretending to be neutral that isn't**. Every unavoidable runtime cast is confined to Uno
(host targets, negotiation), never the backend, keyed by the closed `GraphicsContextKind` enum.

The GL-loader change (`IGLRenderTarget.GetProcAddress` required, commit `89c6c0bbfb`) is already landed and
is not part of this.

## Design decisions (from the review)

1. **`IRenderData` is backend-bound, opaque, and replays itself.** `IRenderData.Replay(IDrawingSession into)`
   — the native impl downcasts `into` to its own backend session (`SKPicture`→`SKCanvas`) and only works with
   same-backend sessions; the command-list fallback is the *one* impl whose `Replay` is legitimately neutral
   (re-issues verbs on any session). The neutrality is a property of the fallback, not of `IRenderData`.

2. **One recorder factory.** `IRenderer.BeginFrame` and `IRetainedRenderingSession.CreateRecording` are the
   same call (`SkiaDrawingSession.StartRecording()` today). Collapse to a single `CreateRecording()` on the
   backend; the root frame is just the first call.

3. **Merge `IDrawingFactory` + `IRenderer` into one device-bound backend** (keep the name `IDrawingFactory`
   for now — least churn; optional rename to `IDrawingBackend` later). It already manufactures sessions
   (`RenderOffscreen`), so `CreateRecording()` / `BeginPresent()` belong there too. The `Graphics(factory,
   renderer)` pair and the separate renderer static collapse; `IGraphicsProvider.CreateGraphics(context)`
   returns one object, installed as `DrawingFactory.Current`.

4. **`IRenderTarget` is pure data** — `Width`/`Height`/`ColorFormat` + its API handle. No behaviour, no
   back-reference to a backend. Neutral family: `ISoftwareRenderTarget`, `IGLRenderTarget`,
   `IVulkanRenderTarget`, `IMetalRenderTarget`, `IWebGpuRenderTarget` — each an API-specific interface in the
   Drawing seam (like GL/Metal already are). **Delete `SkiaRenderTarget`** (an `SKCanvas` wearing
   `IRenderTarget`; dead — nothing constructs it). **Replace the concrete `WebGpuRenderSurface` downcast** with
   a neutral `IWebGpuRenderTarget`.

5. **Typed present, cast-free backend.** `IDrawingFactory<TTarget> : IDrawingFactory` adds
   `IPresentSession BeginPresent(TTarget target)`. A backend implements one generic instantiation per kind it
   serves (Skia: GL/Vulkan/Metal/Software; WebGpu: WebGpu). The neutral→typed erasure is a **bind-time switch
   over the closed `GraphicsContextKind`** in `GraphicsRegistry` — it captures the typed backend once (and is
   the capability gate: a backend that can't serve a kind is declined there). Composition holds the bound
   presenter and calls it per frame: one unconditional Uno-side downcast of the fresh target + the typed
   `BeginPresent`. No type switch, no `_ => throw`, no cast on the backend.

## Deleted / collapsed

`IRetainedRenderingSession`, `RetainedRenderingSession.For`, `CommandListRetainedSession`, `IRenderer`,
`IRenderer.BeginFrame`, the `Graphics` record, `SkiaRenderTarget`, the `WebGpuRenderSurface` public downcast.
`SkiaDrawingFactory` + `SkiaRenderer` merge into one `SkiaDrawingFactory`; WebGpu equivalent merges.

## Final shape

```csharp
public interface IDrawingFactory {                 // device-bound backend (was IDrawingFactory + IRenderer)
    // resources: RenderOffscreen, SnapshotAsync, CreateImageTexture, gradient shaders, colour/effect filters …
    ICommandRecorder CreateRecording();            // was BeginFrame / CreateRecording
}
public interface IDrawingFactory<TTarget> : IDrawingFactory where TTarget : IRenderTarget {
    IPresentSession BeginPresent(TTarget target);  // typed — backend never casts the target
}
public interface ICommandRecorder : IDrawingSession { IRenderData Finish(); }
public interface IRenderData : IDisposable { void Replay(IDrawingSession into); }   // backend-bound
public interface IRenderTarget : IDisposable { int Width { get; } int Height { get; } GraphicsColorFormat ColorFormat { get; } }
// family: ISoftware / IGL / IVulkan / IMetal / IWebGpu RenderTarget  (SkiaRenderTarget deleted)
// GraphicsRegistry: bind-time kind→typed-BeginPresent switch (also capability gate); composition holds the bound presenter.
```

## Execution (each step: build all Linux heads + X11 runtime GL/GLES/Software + compile WebGpu head, then commit)

- **Step A — merge + retained model.** Merge `IRenderer` into `IDrawingFactory` (`CreateRecording` + keep a
  non-generic `BeginPresent(IRenderTarget)` transitional); `Replay` onto `IRenderData`; delete
  `IRetainedRenderingSession`/`For`/`IRenderer`/`Graphics`/`BeginFrame`; command-list fallback becomes a
  neutral `CommandListRenderData`. Reroute `Visual.skia.cs` (`DrawingFactory.Current.CreateRecording()`,
  `data.Replay(session)`) and `CompositionTarget.Rendering.skia.cs`. Merge Skia/WebGpu concretes.
- **Step B — target seam.** Neutral family (`+IVulkan/IWebGpu`, delete `SkiaRenderTarget`, neutralize
  `WebGpuRenderSurface`), make `IRenderTarget` pure data, `IDrawingFactory<TTarget>.BeginPresent`, bind-time
  kind switch + gate in `GraphicsRegistry`, drop the `BeginPresent` type-switch in the backends, composition
  holds the bound presenter.

## Step C — device details on the context; typed `IGraphicsProvider<TContext>`

Fixes an inversion: device details currently ride on the **render target** (`IGLRenderTarget.GetProcAddress`/
`Flavor`, `IMetalRenderTarget.Device`/`Queue`), which is why `SkiaGraphicsProvider.CreateGraphics` ignored the
context. The context *is* the device (its own doc says so); device details belong there, the provider reads
them, and the render target becomes a pure surface — which also makes `IGraphicsProvider<TContext>` worthwhile.

**Neutral device-context interfaces (Drawing)** — neutral because their payloads are `enum`/`Func`/`nint`:
- `IGLDeviceContext : IGraphicsContext { GLFlavor Flavor; Func<string,nint> GetProcAddress; }`
- `IMetalDeviceContext : IGraphicsContext { nint Device; nint Queue; }`
- WebGPU's device is a managed backend type, so `IWebGpuDeviceContext` stays in WebGpu.Init (non-neutral);
  Software needs no device (base `IGraphicsContext`).

**Render targets → pure surface:** strip `Flavor`+`GetProcAddress` from `IGLRenderTarget` (keep `FramebufferId`/
`SampleCount`/`StencilBits`/size — the framebuffer is genuinely per-frame surface); strip `Device`+`Queue` from
`IMetalRenderTarget` (keep `Texture`/size).

**Host contexts implement the device-context** (alongside `ISwapChain`): every GL context (X11 GLX/EGL, Win32,
DRM, Android, WASM) exposes `Flavor`+`GetProcAddress`; every Metal context (macOS, Apple) exposes `Device`+
`Queue` — moved verbatim from their render targets. Loader/flavor stay static/context-stable; this is the
consistent home for the loader (still present on every GL context, honoring "loader everywhere").

**Provider generic:** `IGraphicsProvider` base keeps `PreferredContexts` only; `IGraphicsProvider<in TContext> :
IGraphicsProvider { IDrawingFactory CreateGraphics(TContext); }`. Skia implements `<IGLDeviceContext>`,
`<IMetalDeviceContext>`, `<IGraphicsContext>` (software) — each reads the device off the typed context, no cast.
WebGPU implements `<IGraphicsContext>` and self-casts to its own `IWebGpuDeviceContext` (backend-specific device
— the accepted self-cast, once at startup).

**Skia backend:** `SkiaDrawingFactory` stores the device-context (`IGLDeviceContext?`/`IMetalDeviceContext?`);
`PresentForGL` reads loader/flavor from it + the framebuffer from the render target; `PresentForMetal` reads
device/queue from it + the texture from the render target. `GRContext-GL` still builds lazily (GL context
current only at present) but now from the context's loader.

**Negotiation narrowing (GraphicsRegistry), keyed on the closed kind** (also the capability gate):
```
kind switch {
  OpenGL or OpenGLES => (provider as IGraphicsProvider<IGLDeviceContext>)?.CreateGraphics((IGLDeviceContext)context),
  Metal              => (provider as IGraphicsProvider<IMetalDeviceContext>)?.CreateGraphics((IMetalDeviceContext)context),
  _                  => (provider as IGraphicsProvider<IGraphicsContext>)?.CreateGraphics(context),   // Software, WebGpu(self-cast)
}   // null → decline, negotiation falls through
```
Correct pairing is guaranteed by `PreferredContexts` (negotiation only asks a provider for kinds it declared).

## Step D — make WebGpu init internal

Directive: a third-party backend needs only the WebGpu faces `IWebGpuRenderTarget` + `IWebGpuDeviceContext`
(public); the *init/creation/swapchain* machinery is internal (like the GL/Metal host contexts).
- Internal: `ISwapChain` (host↔framework, not backend SPI), `WebGpuContext` (Create* host helpers),
  `WebGpuSwapChainContext`, `WebGpuBrowserGraphicsContext` (already), `WebGpuRenderSurface`.
- Public (backend seam): `IWebGpuRenderTarget`, `IWebGpuDeviceContext`, `WebGpuDevice` (the device the face
  returns — kept public so `IWebGpuDeviceContext` can be).
- IVT: `Drawing → …WebGpu.Init` (for `WebGpuSwapChainContext : ISwapChain`); `…WebGpu.Init → the 6 hosts`
  that call `WebGpuContext.Create*` (X11/Win32/MacOS/WASM.Browser/Android/AppleUIKit).

## Step E — restore Vulkan onto the neutral seam (audit P0) — CONFIRMED Path B

Vulkan was dropped (`2b4d9e85b1` "Skia-free host — drop Vulkan-Skia", + `1d70992074`/`b0048c4024`/`89eaeeecee`);
the `Uno.UI.Composition.Skia/Vulkan/VulkanContext` subsystem is orphaned-but-intact. The old path was
deliberately **Skia-coupled** (`VulkanContext` owned the `GRContext`-Vulkan + `SKSurface`, handed back
`SkiaRenderTarget(canvas)` — the type we deleted). Path B makes Vulkan a **neutral kind like GL/Metal**: the host
context carries the device, the Skia backend builds the `GRContext`.

**E1 — neutral seam types (`Uno.UI.Composition.Drawing`).**
- `IVulkanDeviceContext : IGraphicsContext` — the `GRVkBackendContext` inputs, all neutral:
  `nint Instance, PhysicalDevice, Device, Queue`; `uint GraphicsQueueFamilyIndex`; `uint MaxApiVersion`;
  `string[] InstanceExtensions, DeviceExtensions`; `Func<string,nint,nint,nint> GetProcAddress` (name, instance,
  device → addr — the `GRVkGetProcedureAddressDelegate` shape).
- `IVulkanRenderTarget : IRenderTarget` — the `GRVkImageInfo` inputs for the render image, all neutral:
  `ulong Image, Memory, MemorySize`; `uint Format, ImageTiling, ImageLayout, ImageUsageFlags, SampleCount,
  LevelCount, CurrentQueueFamily`; `bool Protected`. (`Width`/`Height`/`ColorFormat` from `IRenderTarget`.)

**E2 — `GraphicsRegistry`.** `CreateGraphics` narrowing: `Vulkan => IGraphicsProvider<IVulkanDeviceContext>`;
`CanPresent`: `Vulkan => backend is IDrawingFactory<IVulkanRenderTarget>`.

**E3 — Skia backend.** `SkiaDrawingFactory : IDrawingFactory<IVulkanRenderTarget>` with `_vulkanDevice`;
`SkiaGraphicsProvider : IGraphicsProvider<IVulkanDeviceContext>` → `new SkiaDrawingFactory(vulkanDevice: ctx)`.
`PresentForVulkan(IVulkanRenderTarget vk)`: build+cache `GRContext.CreateVulkan(GRVkBackendContext{ … from
_vulkanDevice, Extensions = GRVkExtensions.Create(getProc, instance, physDevice, InstanceExtensions,
DeviceExtensions) })`; each frame `_vulkanContext.ResetContext()` (external Vulkan blit mutates state), then
`GRBackendRenderTarget(vk.Width, vk.Height, GRVkImageInfo{ … from vk })` + `SKSurface.Create(...TopLeft, Bgra8888,
sRGB)`, recreated when the image handle/size changes. Return a present session that flushes the GRContext on
dispose (the host's `Present` then blits+presents).

**E4 — `VulkanContext` (strip Skia).** Remove `_grContext`, `_vkExtensions`, `_cachedRenderTarget`,
`_cachedSkSurface`, `GrContext`, `CachedSkSurface`, `EnsureCachedSkiaSurface`, `DisposeCachedSkiaSurface`,
`RenderFrame(Action<SKSurface>)`, and the `GRContext.CreateVulkan` in `Initialize`. Keep instance/device/
swapchain/render-image creation, `Resize`/`ResizeRenderImage`, the device `Lock()`, `BlitAndPresent()` (pure
Vulkan). Expose: the device handles (already), `EnabledExtensions` lists, the `GetProcAddress` wrapper, and the
render image's `GRVkImageInfo` fields (`RenderImageInfo`), plus `EnsureRenderImage()`.

**E5 — X11 host.** `X11VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext` owns the `VulkanContext`;
`AcquireRenderTarget` takes the device `Lock` + ensures the render image + returns an `IVulkanRenderTarget` over
it (resize → `ResizeRenderImage`); `Present` → `VulkanContext.BlitAndPresent()` + release the lock. Recreate
`X11/Vulkan/X11VulkanSurfaceFactory.cs` (`VK_KHR_xlib_surface`) from `2b4d9e85b1^`. `X11XamlRootHost
.CreateWindowAndContext`: `case Vulkan` (create the plain window + context), gated by `UseVulkanOnX11` (declined
when false → falls through). **Validate on lavapipe** (`UNO_X11_RENDERER=Vulkan`).

**E6 — Win32 + Android** mirror E5. **DONE, compile-verified** (Win32 head + `net10.0-android` head build clean):
- **Win32**: recreated `Vulkan/Win32VulkanSurfaceFactory` (`VK_KHR_win32_surface`), added
  `Win32VulkanGraphicsContext : ISwapChain, IVulkanDeviceContext` (HWND-based; size via `GetClientRect`), a gated
  `TryCreateVulkan()` arm in the `CreateWindowAndContext` switch (declines when `!UseVulkanOnWin32`, swallows
  creation failure → falls through). The builder's `Win32RenderingBackend.Vulkan` case only sets
  `UseVulkanOnWin32=true` — Vulkan is first in the preference order (see below), so GL stays as the fall-back,
  exactly as on feature/breakingchanges.

**Vulkan-first defaults (match feature/breakingchanges).** The Skia provider's default `PreferredContexts` is
`[Vulkan, OpenGL, OpenGLES, Metal, Software]` — Vulkan first. On breakingchanges each GPU host tried Vulkan first
and fell back to GL (X11 `if (UseVulkanOnX11)` before GL; Win32 `UseVulkanOnWin32 ? Vulkan ?? GL ?? Software`;
Android Vulkan view before the canvas view), all default-on. The neutral seam reproduces that with one shared
order: a host serves Vulkan when its `UseVulkanOnX/UseOpenGLOnX` knobs allow and otherwise declines (→ falls
through to GL); hosts that never serve Vulkan (macOS→Metal, LinuxFB/WASM→GLES) decline it harmlessly. No new
env vars — the same `Use*` knobs, same defaults. **Runtime-validated on X11/lavapipe:** the default (no
`UNO_X11_RENDERER`) now negotiates `Vulkan context via SkiaDrawingFactory` and renders (luma 0.75, non-blank);
forcing `UNO_X11_RENDERER=OpenGL` still negotiates OpenGL (fall-back intact).
- **Android**: recreated `Platform/Vulkan/AndroidVulkanSurfaceFactory` + `AndroidVulkanNativeInterop`
  (`VK_KHR_android_surface`), added `AndroidVulkanGraphicsContext` (ANativeWindow-based) and a neutral
  `UnoSKVulkanView : SurfaceView` (own render thread, serves the Vulkan kind via `ContextFactory` — mirrors
  `UnoSKWebGpuView`). `CreateRenderView` now honors the builder's **documented default** (`UseVulkan`): a Vulkan
  SurfaceView when `UseVulkanOnSkiaAndroid`, with a try/catch fall-back to the canvas view. NOTE: this restores
  Vulkan-as-default on Android (the Skia-free refactor had silently dropped it to GLES-default) — **runtime
  behavior change, compile-only here; on-device validation required.**

Note: `VulkanContext` lived in the `Uno.UI.Composition.Skia` **backend** assembly, which hosts must not
reference — so E4 also **relocated** the whole `Vulkan/` subsystem to the shared host runtime
`Uno.UI.Runtime.Skia` (its namespace was already `Uno.UI.Runtime.Skia.Vulkan`; files renamed `.skia.cs`→`.cs`
since that assembly isn't cross-targeted; `SkiaSharp` package added there for `SKSizeI`). E1–E5 DONE +
**runtime-validated on X11/lavapipe** (`UNO_X11_RENDERER=Vulkan` renders, luma parity with GL/GLES/Software, no
crash). E6 (Win32/Android Vulkan contexts) remains — mechanical mirrors of E5.

## Step F — audit follow-ups (non-Vulkan)

- `UseOpenGLOnSkiaAndroid` set-but-never-read: Android `ApplicationActivity.CreateRenderView` only branched
  WebGpu-vs-GLES; setting it false was silently ignored. **PARTIAL:** the flag is now **non-silent** — when false,
  `CreateRenderView` logs a clear warning and uses the GLES canvas view. **DEFERRED:** actually forcing software
  needs a real Android software swapchain (a `SurfaceView` + `SurfaceHolder.LockCanvas` path with a per-pixel
  BGRA→RGBA present, since the neutral `ISoftwareRenderTarget` is BGRA8888 but Android `Bitmap` is RGBA8888). That
  is a new renderer with a channel-order correctness hazard that can't be validated without an Android device — not
  shipped blind on a maintenance-only target. Tracked for on-device follow-up.

## Validation

Compile: X11/Win32/macOS/Linux.FrameBuffer (via `SamplesApp.Skia.Generic`), Android (`net10.0-android`), WASM
(`net10.0`), WebGpu head. Runtime (Xvfb :99 / lavapipe): X11 `OpenGL`, `OpenGLES`, `Software` render a
non-blank frame; WebGpu head compiles (browser/native runtime handed off). iOS/AppleUIKit = Metal, compile
handed off.
```
