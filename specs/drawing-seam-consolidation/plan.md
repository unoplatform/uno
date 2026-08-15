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

## Validation

Compile: X11/Win32/macOS/Linux.FrameBuffer (via `SamplesApp.Skia.Generic`), Android (`net10.0-android`), WASM
(`net10.0`), WebGpu head. Runtime (Xvfb :99 / lavapipe): X11 `OpenGL`, `OpenGLES`, `Software` render a
non-blank frame; WebGpu head compiles (browser/native runtime handed off). iOS/AppleUIKit = Metal, compile
handed off.
```
