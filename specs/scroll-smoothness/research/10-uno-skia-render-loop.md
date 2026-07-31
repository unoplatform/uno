# Uno Skia rendering / composition pipeline audit — frame scheduling, vsync, FPS ceiling

Scope: what schedules a frame, where the work runs, what limits FPS, and where scroll smoothness is
structurally capped. All claims cite `src/`-relative paths + line numbers in
`D:/Work/uno-worktrees/scrollsmooth`. Anything not verifiable in source is marked **UNVERIFIED**.

---

## 0. Executive summary (the shape of the pipeline)

Uno Skia uses a **two-stage, two-thread** pipeline that deliberately mirrors WPF's milcore split:

1. **Record** (`CompositionTarget.Render()`): walks the composition tree on the **UI thread** and
   produces one `SKPicture` per frame plus a damage `SKPath`.
   `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:110-201`
2. **Present** (`CompositionTarget.Draw()`): replays the last recorded `SKPicture` onto the platform
   surface and swaps. Runs on the **platform render thread** (Win32/X11/iOS/Android-Vulkan/FrameBuffer)
   or on the UI thread (macOS, WASM).
   `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:224-333`

The hand-off is a single-slot double buffer guarded by `_frameGate`:
`_lastRenderedFrame` is written by Record and *borrowed* (nulled) by Present, then returned.
`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:72, 135-158, 235-249, 415-437`

The ASCII sequence diagram at the top of
`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:17-68` is the authoritative
in-repo description of this protocol.

**Everything composition-related — animation ticking, expression evaluation, matrix computation,
paint recording — happens on the UI thread.** There is no compositor thread. See §3.

---

## 1. Frame scheduling per platform

### 1.0 The common core

`IXamlRootHost.InvalidateRender()` is the single platform hook (`src/Uno.UI/Hosting/IXamlRootHost.cs:11`).

```
Visual property change
  → CompositionObject.OnPropertyChanged            (CompositionObject.cs:502-506)
  → Visual.OnPropertyChangedCore                    (Visual.cs:192-218)
  → Compositor.InvalidateRender(visual)             (Compositor.cs:251)
  → Compositor.InvalidateRenderPartial              (Compositor.skia.cs:258-263)
      visual.SetMatrixDirty(); visual.InvalidatePaint(); visual.CompositionTarget?.RequestNewFrame();
  → CompositionTarget.RequestNewFrame               (CompositionTarget.RenderScheduling.skia.cs:86-118)
      (coalesces; if a request is not already pending) host.InvalidateRender()
  → [platform] wakes its render source
  → [platform] calls CompositionTarget.OnNativePlatformFrameRequested(canvas, resizeFunc)
        (CompositionTarget.RenderScheduling.skia.cs:166-176)
        - enqueues EnqueueRenderCallback onto NativeDispatcher's dedicated *render slot*
        - immediately calls Draw() (present the previously recorded picture)
  → NativeDispatcher pumps the render slot on the UI thread → Render() records the next SKPicture
        (CompositionTarget.RenderScheduling.skia.cs:120-157)
```

Critical consequence: **the platform "frame requested" callback presents the picture recorded during
the *previous* frame and only then schedules the recording of the next one.** There is a structural
one-frame (≥1 vsync) latency from "state changed" to "pixels on screen", plus whatever the dispatcher
adds. `Render()` also re-invalidates the host at line 172-175 so a recorded frame always gets a
present opportunity.

`Render()` never checks whether layout is clean; only the opportunistic early path does
(`SkiaRenderHelper.CanRecordPicture`, `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:33-34`, used at
`CompositionTarget.RenderScheduling.skia.cs:185`).

### 1.1 Win32 (`Uno.UI.Runtime.Skia.Win32`) — dedicated render thread, vsync-locked

- **Render thread**: `RenderThread` class, `IsBackground = true`, name `"Uno Render Thread"`.
  `src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32WindowWrapper.RenderThread.cs:14-109`
  Loop: `_frameSignal.WaitOne()` → `StartPaint` → `DrawFrame()` → `CopyPixels()` (swap/blit, "may
  block for VSync") → `EndPaint`. (lines 52-89)
- **What schedules a frame**: `IXamlRootHost.InvalidateRender() => _renderThread?.SignalNewFrame()`
  — an `AutoResetEvent.Set()`, explicitly *not* `InvalidateRect`/`WM_PAINT`, because a synthesized
  `WM_PAINT` is the lowest-priority Win32 message and can be starved indefinitely by the dispatcher's
  own posted messages. `src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32WindowWrapper.Rendering.cs:19-24`
  OS-driven repaints still arrive via `WM_PAINT` → `SignalNewFrame()`
  (`UI/Xaml/Window/Win32WindowWrapper.cs:292-296`).
- **Vsync**: three backends, selected Vulkan → OpenGL → Software
  (`UI/Xaml/Window/Win32WindowWrapper.cs:108-115`):
  - **OpenGL/WGL**: `wglSwapIntervalEXT(1)` when `SetFrameRateAsScreenRefreshRate` is true, else `0`.
    `Rendering/Win32WindowWrapper.Rendering.OpenGl.cs:118-121, 156-168`. With interval 1,
    `SwapBuffers` blocks the render thread at the refresh (line 235). Interval `0` → paced by
    `Win32RenderPacer` timer (line 240).
  - **Vulkan**: swapchain prefers `VK_PRESENT_MODE_MAILBOX_KHR`, falls back to `FIFO`, then
    `IMMEDIATE`; `imageCount = minImageCount + 1` (triple-buffered when the driver allows).
    `src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:70-72, 103-109, 115`.
    MAILBOX doesn't block, so the loop is paced by `Win32RenderPacer` (`DwmFlush`).
    `Rendering/Win32WindowWrapper.Rendering.Vulkan.cs:112-134`
  - **Software**: `BitBlt` returns instantly, so pacing is `Win32RenderPacer`.
    `Rendering/Win32WindowWrapper.Rendering.Software.cs:64-103`
- **`Win32RenderPacer`** (`Rendering/Win32RenderPacer.cs`): blocks on `PInvoke.DwmFlush()` (line 61)
  to align to the DWM compositor's vsync. After `DwmFlushFailureThreshold = 3` consecutive failures
  it *permanently* degrades to a `System.Timers.Timer`-based `FramePacer` for the window's lifetime
  (lines 26, 63-82) — "Frame timing will not be vsync-aligned for the rest of this window's lifetime."
- **UI thread blocking on the render thread**: `SynchronousRenderAndDraw` calls
  `_renderThread.WaitForNextPresent(TimeSpan.FromMilliseconds(100))` — a bounded UI-thread stall,
  used on `WM_NCPAINT` / first show / size change.
  `UI/Xaml/Window/Win32WindowWrapper.cs:413-432`, callers at lines 261 and 677.
- **UI thread pump**: `Win32EventLoop.RunOnce()` deliberately does
  `PeekMessage(..., PM_REMOVE | PM_QS_INPUT)` *first*, then falls back to `GetMessage` — i.e. **input
  messages outrank the dispatcher's own posted message**, explicitly "for wheel scrolling where we
  don't want to wait for the queue to be empty before continuing to scroll".
  `src/Uno.UI.Runtime.Skia.Win32/Native/Win32EventLoop.cs:113-132`

### 1.2 X11 (`Uno.UI.Runtime.Skia.X11`) — dedicated render thread, **timer-paced, not vsync-locked**

- **Render thread**: `"X11RenderThread"`, `ThreadPriority.AboveNormal`.
  `src/Uno.UI.Runtime.Skia.X11/Rendering/X11XamlRootHost.Rendering.cs:22-43`
- **Scheduling**: `InvalidateRender() => _framePacer.RequestFrame()` (line 53-59). `FramePacer` is a
  `System.Timers.Timer` with `Interval = Math.Clamp(remainingMs, 1, TargetIntervalMs)`.
  `src/Uno.UI.Runtime.Skia/Hosting/FramePacer.cs:17-83` — so **every invalidation is delayed by at
  least 1 ms and at most one frame interval**, and the wake-up comes from a thread-pool timer, not
  from the display.
- **Refresh rate**: `X11DisplayInformationExtension` reports XRandR fps into
  `UpdateRenderTimerFps` → `_framePacer.UpdateTargetFps(fps)` when
  `SetFrameRateAsScreenRefreshRate` is true.
  `src/Uno.UI.Runtime.Skia.X11/Graphics/Display/X11DisplayInformationExtension.cs:213-221`;
  `Rendering/X11XamlRootHost.Rendering.cs:45-51`
- **No swap-interval control**: `glXSwapBuffers` (`Rendering/X11OpenGLRenderer.cs:71`) and
  `eglSwapBuffers` (`Rendering/X11EGLRenderer.cs:118`) are called with whatever the driver default
  swap interval is — grep for `SwapInterval` finds **only** the Win32 GL renderer. So X11 frame
  timing = timer cadence AND (implicitly) whatever the driver does inside SwapBuffers.
- **Separate X11 event threads** (2 of them) feed the dispatcher:
  `src/Uno.UI.Runtime.Skia.X11/Hosting/X11XamlRootHost.x11events.cs:36-45`; `Expose` →
  `InvalidateRender()` (line 211-212).
- Vulkan on X11 uses the same MAILBOX/FIFO swapchain code as Win32
  (`Rendering/X11VulkanRenderer.cs:77-86` → `VulkanContext.BlitAndPresent`), paced only by the host's
  `FramePacer`.

### 1.3 macOS (`Uno.UI.Runtime.Skia.MacOS`) — **rendering on the UI (AppKit main) thread**

- **Scheduling**: `IXamlRootHost.InvalidateRender() => NativeUno.uno_window_invalidate(...)`
  `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs:288`
  → `((UNOWindow*)window).renderingView.needsDisplay = true;`
  `UnoNativeMac/UnoNativeMac/UNOWindow.m:390-396`
- **View**: `UNOMetalFlippedView : MTKView` with `enableSetNeedsDisplay = YES`
  (`UNOWindow.m:322-327`); the delegate's `drawInMTKView:` acquires `currentDrawable`, calls into
  managed `MetalDraw`, then `[commandBuffer presentDrawable:]; [commandBuffer commit];`
  `UnoNativeMac/UnoNativeMac/UNOMetalViewDelegate.m:35-79`
- **Thread**: `drawInMTKView:` is an AppKit display pass ⇒ main thread. `MacOSWindowHost.MetalDraw`
  (`MacOSWindowHost.cs:86-129`) calls `OnNativePlatformFrameRequested` on the **UI thread**, so
  *record and present are serialized on the same thread on macOS*. Software path
  (`SoftDraw`, lines 131-180) likewise.
- **Vsync**: MTKView draws are serviced by CoreAnimation/CVDisplayLink at the display refresh.
  The exact coalescing semantics of `enableSetNeedsDisplay=YES` + `paused` are **UNVERIFIED** from
  this repo (only the `NSLog` at `UNOMetalViewDelegate.m:28` observes them).
- macOS is also the reason `_latestFrames` retains the last picture per target — "a window minimized
  on hosts that stop servicing invalidations, like macOS".
  `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:46-49`

### 1.4 Android-Skia (`Uno.UI.Runtime.Skia.Android`)

Two backends, selected by `FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid` (default `true`)
and `UseOpenGLOnSkiaAndroid` (default `true`), `src/Uno.UI/FeatureConfiguration.cs:659, 665`.

- **GLSurfaceView path** (`UnoSKCanvasView : GLSurfaceView`):
  `RenderMode = Rendermode.WhenDirty` (`Rendering/UnoSKCanvasView.cs:53`);
  `InvalidateRender()` → `RequestRender()` (lines 61-66). `IRenderer.OnDrawFrame` (line 160-229) runs
  on GLSurfaceView's **own GLThread**, and its `eglSwapBuffers` is throttled by SurfaceFlinger's
  BufferQueue ⇒ effectively vsync-locked. (BufferQueue throttling is Android platform behavior, not
  visible in this repo — **UNVERIFIED here**.)
- **Vulkan path** (`UnoSKVulkanView : SurfaceView`): its own `"UnoVulkanRenderThread"`
  (`Rendering/UnoSKVulkanView.cs:83-88`) with a `_renderEvent.Wait(100ms)` loop
  (lines 141-152). **There is no frame pacer and no vsync wait in this loop** — it renders one frame
  per `InvalidateRender()`, and MAILBOX present does not block. Because `Render()` re-invalidates
  every frame while animating (`Compositor.skia.cs:252-255`,
  `CompositionTarget.Rendering.skia.cs:167-175`), this path can free-run above the display refresh.
- **Dispatcher**: `.Android.cs` partials are compiled for any Android TFM regardless of
  `UnoRuntimeIdentifier` (`src/Uno.CrossTargetting.targets:40-41`), so Skia-on-Android uses
  `Handler(Looper.MainLooper).Post` for the dispatcher
  (`src/Uno.UI.Dispatching/Native/NativeDispatcher.Android.cs:33, 40-43`).
  `Choreographer` is used **only** by `RunAnimation` (line 50-59), which the Skia frame pipeline does
  **not** use. `MaxRenderSpan` there is `(1/60) * (2/3) s ≈ 11.1 ms` (line 29).

### 1.5 iOS/tvOS-Skia (`Uno.UI.Runtime.Skia.AppleUIKit`) — CADisplayLink on a dedicated thread

- `UnoSKMetalView : MTKView`, `src/Uno.UI.Runtime.Skia.AppleUIKit/Rendering/UnoSKMetalView.cs`
  - `Paused = true`, `EnableSetNeedsDisplay = false` (lines 72-75) — UIKit's internal display link
    is disabled; Uno drives its own.
  - `_link = CADisplayLink.Create(() => this.Draw())` (line 37), added to the run loop of a
    dedicated `"UnoSKMetalViewRenderThread"` with `NSQualityOfService.UserInteractive`
    (lines 89-126). `NSRunLoop.Current.Run()` blocks forever (line 119).
  - `PreferredFramesPerSecond = UIScreen.MainScreen.MaximumFramesPerSecond` (lines 77-78);
    on iOS 15+ `CAFrameRateRange { Minimum = 30, Preferred = max, Maximum = max }` (lines 100-105).
  - **Render-on-demand**: `QueueRender() => _link.Paused = false` (line 130-133), and `Draw` sets
    `_link.Paused = true` again at line 153 — one display-link callback per invalidation.
  - Present: `commandBuffer.PresentDrawable(drawable); commandBuffer.Commit();` (lines 186-193).
- `RootViewController.InvalidateRender() => _skCanvasView?.QueueRender()`
  `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/RootViewController.cs:272-275`
- Note: `AppleUIKitPointerInputSource` uses two *additional* `CADisplayLink`s for scroll flushing and
  momentum: `_activeScrollDisplayLink` (line 328) and `_momentumDisplayLink` (line 430).
  `src/Uno.UI.Runtime.Skia.AppleUIKit/Devices/Input/AppleUIKitPointerInputSource.cs:56-57, 328, 430`

### 1.6 WebAssembly-Skia (`Uno.UI.Runtime.Skia.WebAssembly.Browser`) — rAF, single-threaded

- `BrowserRenderer.InvalidateRender()` coalesces via `_pendingInvalidate` then calls JS
  `invalidate` (`Rendering/BrowserRenderer.cs:47-57`), which is
  `window.requestAnimationFrame(() => instance.requestRender())`
  (`ts/Runtime/BrowserRenderer.ts:47-51`).
- `RenderFrame()` (`Rendering/BrowserRenderer.cs:65-116`) runs on the browser main thread and calls
  `OnNativePlatformFrameRequested` there ⇒ **record and present are on the same thread**; the
  recording is deferred to the dispatcher (see §1.0), which on WASM is
  `window.setImmediate(...)` (`src/Uno.UWP/ts/Windows/Dispatching/NativeDispatcher.ts:18-31`).
- Vsync = rAF = display refresh, enforced by the browser.
- Threading is off unless `UNO_BOOTSTRAP_MONO_RUNTIME_FEATURES` contains `threads`
  (`src/Uno.UI.Dispatching/Native/NativeDispatcher.wasm.cs:39-44`).

### 1.7 Linux FrameBuffer (`Uno.UI.Runtime.Skia.Linux.FrameBuffer`)

- **DRM/GBM path** (`Rendering/DRMRenderer.cs`): true page-flip driven.
  - `InvalidateRender()` (lines 286-324): if a flip is already in flight, just records
    `_invalidateRenderCalledWhileWaitingForPageFlip` and returns (coalescing); otherwise
    `eglSwapBuffers` → `gbm_surface_lock_front_buffer` → `drmModePageFlip(..., Event, ...)`.
  - A dedicated `"DRM pageflip loop"` thread `poll()`s the DRM fd (lines 261, 350-375); the
    `OnPageFlip` callback (lines 377-393) calls `Render()` **on that thread** and re-arms.
  - So on DRM the *present cadence is exactly the CRTC vblank*. `CalculateRefreshRate` (lines 264-284)
    derives fps from the mode.
- **Software framebuffer path** (`Rendering/SoftwareRenderer.cs`): dedicated
  `"FrameBuffer software rendering thread"` (lines 30-53), `InvalidateRender() =>
  _renderInvalidationEvent.Set()` (line 56), and `PresentToOutput` blocks on
  `ioctl(FBIO_WAITFORVSYNC)` (`SoftwareRenderer.cs:66` → `Hosting/FrameBufferDevice.cs:260-270`)
  before `ReadPixels` into the framebuffer.
- The host's own UI thread is an `EventLoop` (`Hosting/FramebufferHost.cs:170-183`).

### 1.8 WPF host

**Not present in this repository.** `find` over the repo returns only
`doc/articles/wpf-migration.md` and `doc/articles/wpf-winui-equivalents.md`. The WPF Skia host ships
from a different repo; its scheduling is **UNVERIFIED** here.

### 1.9 Headless / Tizen

- Headless: `HeadlessWindowWrapper` owns a `HeadlessRenderer` with its own render thread
  (`src/Uno.UI.Runtime.Skia.Headless/UI/HeadlessWindowWrapper.cs:45-51, 84, 88-92`).
- Tizen: no `InvalidateRender` implementation found by grep in
  `src/Uno.UI.Runtime.Skia.Tizen/` — **UNVERIFIED / likely not wired to the current pipeline**.

### 1.10 Platform summary table

| Platform | Frame source | Present thread | Record thread | Vsync-locked? |
|---|---|---|---|---|
| Win32 GL | `AutoResetEvent` + `wglSwapInterval(1)` blocking `SwapBuffers` | "Uno Render Thread" | UI | Yes (swap interval) |
| Win32 Vulkan | `AutoResetEvent` + `DwmFlush()` | "Uno Render Thread" | UI | Yes (DwmFlush), degrades to timer after 3 failures |
| Win32 Software | `AutoResetEvent` + `DwmFlush()` after `BitBlt` | "Uno Render Thread" | UI | Yes (DwmFlush), degradable |
| X11 (GL/EGL/Vulkan) | `System.Timers.Timer` (`FramePacer`) | "X11RenderThread" | UI | **No** — timer-paced; driver may add its own |
| macOS Metal/Software | `needsDisplay = YES` → AppKit display pass | **UI thread** | UI | Yes (CoreAnimation), semantics UNVERIFIED |
| Android GLSurfaceView | `RequestRender()`, `Rendermode.WhenDirty` | GLSurfaceView GLThread | UI | Yes via SurfaceFlinger (UNVERIFIED in-repo) |
| Android Vulkan | `ManualResetEventSlim` | "UnoVulkanRenderThread" | UI | **No** — no pacer, MAILBOX non-blocking |
| iOS/tvOS Metal | `CADisplayLink` (own thread run loop) | "UnoSKMetalViewRenderThread" | UI | Yes |
| WASM | `requestAnimationFrame` | **UI (browser main)** | UI | Yes |
| FrameBuffer DRM | `drmModePageFlip` + poll on flip events | "DRM pageflip loop" | UI | Yes (vblank) |
| FrameBuffer software | `AutoResetEvent` + `FBIO_WAITFORVSYNC` | FB render thread | UI | Yes |
| WPF | — | — | — | Not in this repo |

---

## 2. How animations are ticked

### 2.1 Composition animations (`KeyFrameAnimation`, `ExpressionAnimation`)

**Ticked synchronously at the start of every recorded frame, on the UI thread**, inside
`Compositor.RenderRootVisual`:

```csharp
// src/Uno.UI.Composition/Composition/Compositor.skia.cs:206-222
foreach (var animation in _runningAnimations.Keys.ToArray())
{
    try { animation.RaiseAnimationFrame(); }
    catch (Exception e) { … animation.Stop(); }
}
```

- `RaiseAnimationFrame()` fires `CompositionAnimation.AnimationFrame`
  (`src/Uno.UI.Composition/Composition/CompositionAnimation.cs:26, 95`).
- The subscriber is `CompositionObject.ReEvaluateAnimation`, registered in `StartAnimation`
  (`src/Uno.UI.Composition/Composition/CompositionObject.cs:97`, handler at 125-181). It calls
  `animation.Evaluate()` and writes the result back through `SetAnimatableProperty` — which goes
  through `SetProperty` → `OnPropertyChanged` → `InvalidateRender` again.
- Registration/bookkeeping: `Compositor.RegisterAnimation` /
  `UnregisterAnimation` maintain `_runningAnimations` and `_runningTargets`
  (`Compositor.skia.cs:20-21, 44-128`). Only `IsTrackedByCompositor` animations participate;
  `KeyFrameAnimation` overrides it to `true`
  (`src/Uno.UI.Composition/Composition/KeyFrameAnimation.cs:21`), base `CompositionAnimation` is
  `false` (`CompositionAnimation.cs:28`).
- **Cadence**: exactly the record cadence — i.e. "once per presented frame" only insofar as record
  and present stay in lockstep. `RenderRootVisual` re-requests a frame while anything is running:
  ```csharp
  // Compositor.skia.cs:252-255
  if (_runningAnimations.Count > 0 || transitionsCount > 0)
      rootVisual.CompositionTarget?.RequestNewFrame();
  ```
- **Time base**: `KeyFrameEvaluator.Evaluate()` samples `_compositor.TimestampInTicks`
  (`src/Uno.UI.Composition/Composition/KeyFrameAnimations/KeyFrameEvaluator.cs:57-66`), i.e.
  `Stopwatch.GetTimestamp()` scaled (`Compositor.cs:33-38`). **This is wall-clock at record time,
  not a vsync/presentation timestamp.** Any jitter in when the UI thread gets to `Render()` is
  directly baked into animated values → visible micro-stutter even when the present cadence is
  perfectly regular. There is no frame-time prediction anywhere in the pipeline.
- **Per-frame allocation**: `_runningAnimations.Keys.ToArray()` allocates an array on every frame
  with any running animation (`Compositor.skia.cs:206`).
- `ExpressionAnimation.Expression` setter raises `RaiseAnimationFrame()` directly
  (`src/Uno.UI.Composition/Composition/ExpressionAnimation.cs:29`).
- Background brush transitions (`ColorBrushTransitionState`) are ticked in the same place and force
  `InvalidatePaint()` on their visual every frame (`Compositor.skia.cs:238-250`).

### 2.2 `CompositionTarget.Rendering` (XAML-level animations, ScrollView, inertia)

- Raise path: `Render()` → `OnFramePictureRecorded` → if `_isRenderingActive` and not already
  scheduled, `NativeDispatcher.Main.Enqueue(RaiseRendering, NativeDispatcherPriority.High)`.
  `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:198, 439-453`
- `RaiseRendering` (lines 458-487) is a **separate dispatcher item**, so subscribers observe frame N
  and their property writes land in frame N+1. That is a **second frame of latency** on top of §1.0
  for everything driven by `CompositionTarget.Rendering`.
- Adding a handler flips the pipeline into **continuous mode**: `_isRenderingActive = true` and every
  live target gets `RequestNewFrame()`; `Render()` then re-requests a frame unconditionally
  (`CompositionTarget.Rendering.skia.cs:84-108`, and line 167-170).
- `RenderingEventArgs.RenderingTime` = `Stopwatch.GetElapsedTime(_start)` measured *at raise time*,
  not at present time (lines 25, 475).
- Per-raise allocations: `new FramePicture[...]`, `new List<(Window, object)>`, `new
  RenderingEventArgs(...)` (lines 462-475).

**Consumers relevant to scrolling:**
- `ScrollPresenter` (`src/Uno.UI/UI/Xaml/Controls/ScrollPresenter/ScrollPresenter.cs:6773-6774`)
- `ScrollView` (`src/Uno.UI/UI/Xaml/Controls/ScrollView/ScrollView.cs:1150-1152`)
- `GestureRecognizer` inertia via `CompositionInertiaProcessorTimer`
  (`src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:333-364`), which is
  the default (`WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation`
  / `...ForUiElement`, both default `true`,
  `src/Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.GestureRecognizer.cs:65-77`;
  selection at `GestureRecognizer.Manipulation.InertiaProcessor.cs:193-199`). Note the comment at
  line 342-343: it deliberately ignores `RenderingEventArgs.RenderingTime` and uses its own
  `Stopwatch`.
- XAML `Storyboard` animators: `DispatcherAnimator<T>` subscribes/unsubscribes
  `CompositionTarget.Rendering` (`src/Uno.UI/UI/Xaml/Media/Animation/Animators/DispatcherAnimator.skia.cs:19-20`),
  with a `DefaultFrameRate = 60` constant (line 12).

### 2.3 InteractionTracker inertia — **thread-pool timer at a fixed 17 ms**

This is the path used by `ScrollPresenter`/`ScrollView`/`ItemsView` for fling/wheel inertia.

```csharp
// src/Uno.UI.Composition/Composition/InteractionTracker/InteractionTrackerPointerWheelInertiaHandler.cs:15,54-55
private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60
…
_stopwatch = Stopwatch.StartNew();
_timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
```

Identical construction in
`InteractionTrackerActiveInputInertiaHandler.cs:23, 47-48`.

- `System.Threading.Timer` ⇒ **thread-pool thread**, ~17 ms nominal (58.8 Hz), with OS timer
  granularity jitter, and *no relationship whatsoever to the display refresh*. On a 60 Hz display
  this beats against vsync; on 120/144 Hz it caps inertia updates at ~59 Hz regardless of what the
  renderer can do.
- Positions are computed from `_stopwatch.ElapsedMilliseconds` (integer milliseconds:
  `InteractionTrackerPointerWheelInertiaHandler.cs:66, 77`;
  `InteractionTrackerActiveInputInertiaHandler.cs:58, 70-73`) — a 1 ms quantization on top of the
  timer jitter.
- `InteractionTracker.SetPosition` then marshals to the UI thread as a **Normal-priority** dispatcher
  item: `src/Uno.UI.Composition/Composition/InteractionTracker/InteractionTracker.cs:62-74`.
  So each inertia sample must queue behind whatever else is in the Normal queue before it can move
  the visual, and it interacts with the render-slot throttling in §7.2.

### 2.4 Answer to "who raises `AnimationFrame` and at what cadence"

`Compositor.RenderRootVisual` raises it, synchronously, once per **recorded** frame, on the UI
thread, inside the picture recording. It is tied to the *record* cadence, not to the presented frame,
and it is evaluated against wall-clock (`Stopwatch`) rather than a frame/presentation timestamp.

---

## 3. Off-UI-thread composition/animation evaluation

**There is none. Plainly: all composition-object mutation, animation evaluation, expression
evaluation, matrix computation and paint recording happen on the UI thread.**

Evidence:
- `Render()` starts with `NativeDispatcher.CheckThreadAccess();`
  (`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:114`), as do
  `ICompositionTarget.AddDamage` (lines 205, 211), `TryExecuteOnNextRenderAsync` (line 344),
  `EnqueueRenderCallback` (`CompositionTarget.RenderScheduling.skia.cs:123`),
  `OnRenderFrameOpportunity` (line 183), and the `Rendering` event add/remove
  (`CompositionTarget.Rendering.skia.cs:88, 101`).
- Recording uses **process-global static** non-reentrant state:
  `SkiaRenderHelper._recorder` (`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:21`),
  `Visual._recorder`, `Visual._pathPool`, `Visual._spareRenderPath`, `Visual._spareShadowPath`,
  `Visual._spareShadowContributions` (`src/Uno.UI.Composition/Composition/Visual.skia.cs:26-47`).
  Nothing about the walk is parallelizable or relocatable as written.
- The only thing the render thread does is `Draw()` — replay the finished `SKPicture` and swap.
- `Compositor.RequestCommitAsync()` is a no-op: "Uno currently does not buffer composition commits"
  (`src/Uno.UI.Composition/Composition/Compositor.cs:213-216`).

The one exception is the **InteractionTracker inertia timers** (§2.3), which compute positions on a
thread-pool thread but immediately marshal the result to the UI thread.

Consequence for scroll: **there is no DWM/DirectComposition-style independent animation.** If the UI
thread is busy, scrolling stops moving — not just visually stale, but the offsets themselves stop
advancing.

---

## 4. What triggers a repaint; dirty tracking; how much of the tree is re-walked

### 4.1 Damage regions — implemented, and used at present time

- `CompositionTarget._pendingDamage` is an `SKPath` accumulated on the UI thread during recording
  (`CompositionTarget.Rendering.skia.cs:64`, `ICompositionTarget.AddDamage` at 203-213).
- `Render()` unions the previous frame's carried damage, clamps to the frame rect, snapshots it
  (with an `SKPath` recycling pool to avoid a native alloc per frame) and stores it alongside the
  picture (`CompositionTarget.Rendering.skia.cs:126-158`).
- `Draw()` applies it: `canvas.ClipPath(damage, antialias: false)` when the canvas wasn't just
  recreated (`CompositionTarget.Rendering.skia.cs:288-295`), then replays the whole picture through
  that clip (line 298-302), then `damage.Reset()` (line 312).
- Per-visual damage contribution: `Visual.ContributeDamageOnPaint`
  (`src/Uno.UI.Composition/Composition/Visual.Damage.skia.cs:27-69`) — early-outs when
  `!contentChanged && !moved && !shadowSilhouetteChanged`; otherwise unions the visual's content path
  (`_ownContentPath`, outset for AA by a 4 px stroke, lines 175-189) plus its *previous*
  `_lastRenderBounds` when it moved.
- Debug overlay: `FeatureConfiguration.Rendering.DamageRegionOverlay`
  (`src/Uno.UI/FeatureConfiguration.cs:743`), drawn at
  `CompositionTarget.Rendering.skia.cs:215-222, 304-307`. Note: enabling the overlay *disables* the
  damage clip (line 292).
- GPU backends keep a **retained layer** so the damage clip is meaningful across frames:
  `RetainedLayer` (`src/Uno.UI/Helpers/RetainedLayer.skia.cs`), used by Win32 GL
  (`Win32WindowWrapper.Rendering.OpenGl.cs:217-222, 229-233`), X11 GL (`X11Renderer.cs:29, 55-56,
  78-81`), macOS Metal (`MacOSWindowHost.cs:36, 107-115`), Android GL
  (`UnoSKCanvasView.cs:158, 203, 217`), iOS Metal (`UnoSKMetalView.cs:24, 177-180`).
  `Present` blits the **whole** layer to the swapchain each frame
  (`RetainedLayer.skia.cs:34-41`) — so the *pixel replay* is damage-limited but the *layer→swapchain
  blit* is full-surface every frame.

### 4.2 The picture cache — and why scrolling defeats it

Two levels of caching per `Visual`:

- `_picture` — the visual's own painted content, re-recorded only when `VisualFlags.PaintDirty`
  (`src/Uno.UI.Composition/Composition/Visual.skia.cs:53, 487-500`).
- `_childrenPicture` — a collapsed picture of the entire child subtree
  (`Visual.skia.cs:54, 533-590`).

`_childrenPicture` is only ever *populated* by the "picture collapsing optimization", gated by three
conditions (`Visual.skia.cs:540-543`):
```csharp
!visual._enablePictureCollapsingOptimization
 || visual._framesSinceSubtreeNotChanged < visual._pictureCollapsingOptimizationFrameThreshold   // 50
 || !applyChildOptimization
 || visual.GetSubTreeVisualCount() < visual._pictureCollapsingOptimizationVisualCountThreshold   // 100
```
Defaults: enabled, **50 clean frames**, **100 visuals** (`Visual.skia.cs:39-41`, exposed as
`FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization` /
`VisualSubtreeSkippingOptimizationCleanFramesThreshold` /
`...VisualCountThreshold`, `src/Uno.UI/FeatureConfiguration.cs:667-712`).

**Therefore, in the default configuration, every frame re-walks the entire visual tree**
(`RenderChildrenStep` → `child.Render(...)` recursion, `Visual.skia.cs:545-548`) unless a subtree has
been completely untouched for 50 consecutive frames *and* contains ≥100 visuals. Each visit does:
two `SKPath` pool rentals, `clipInRoot.Transform(...)` copies, up to two `SKPath.Op(Intersect)`
calls, a `TotalMatrix` read, `ContributeDamageOnPaint`, and an `sk_canvas_draw_picture`
(`Visual.skia.cs:401-469, 471-511`).

### 4.3 The scroll-specific invalidation cascade (**the biggest structural limiter found**)

Any `Visual` property write does *all three* of these:

```csharp
// src/Uno.UI.Composition/Composition/Compositor.skia.cs:258-263
partial void InvalidateRenderPartial(Visual visual)
{
    visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
    visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
    visual.CompositionTarget?.RequestNewFrame();
}
```

The in-repo `TODO`s acknowledge both are over-broad. Concretely, for a scroll offset change:

1. Classic `ScrollViewer` writes `visual.AnchorPoint = target`
   (`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:467`) or
   starts an `AnchorPoint` `KeyFrameAnimation` (line 496). `AnchorPoint` is backed by `SetProperty`
   (`src/Uno.UI.Composition/Composition/Visual.skia.cs:266-273`).
2. `InvalidatePaint()` unrefs the scrolled visual's own `_picture` and forces a re-record next frame
   (`Visual.skia.cs:234-243`).
3. `SetMatrixDirty()` on a `ContainerVisual` **recurses into every descendant**:
   ```csharp
   // src/Uno.UI.Composition/Composition/ContainerVisual.skia.cs:212-227
   internal override bool SetMatrixDirty()
   {
       if (base.SetMatrixDirty())
       {
           foreach (var child in Children.InnerList) { child.SetMatrixDirty(); }
           return true;
       }
       return false;
   }
   ```
   and *each* descendant's `base.SetMatrixDirty()` calls `InvalidateParentChildrenPicture(false)`
   (`Visual.skia.cs:140-146, 245-258`), which drops its parent's `_childrenPicture` and sets
   `ChildrenSKPictureInvalid` up the chain.
4. Net effect: **one scroll offset change invalidates the collapsed children-picture of every
   container inside the scrolled subtree**, resets `_framesSinceSubtreeNotChanged` to 0 everywhere
   (`Visual.skia.cs:389-399`), and guarantees a full-subtree re-walk on the next frame. The 50-frame
   collapsing optimization can therefore **never** engage inside a subtree that is being scrolled,
   and it also can't engage in a virtualized list where items are being materialized.

This is the dominant per-frame CPU cost of scrolling in Uno Skia, and it is O(number of visuals in
the scrolled content), not O(visible area).

### 4.4 Other unconditional per-frame repaint sources

- `Visual.RequiresRepaintOnEveryFrame` (`Visual.skia.cs:137`) → `InvalidateParentChildrenPicture` +
  direct paint every frame (lines 478-484). Used by effect brushes / backdrop-style visuals.
- `ContainerVisual.Children.CollectionChanged` → `InvalidateParentChildrenPicture(true)` +
  `CompositionTarget?.RequestNewFrame()` + `_subtreeVisualCount = null` up the chain
  (`ContainerVisual.skia.cs:28-46`). Item realization during virtualized scrolling hits this on every
  container change.
- `Opacity` changes recursively `InvalidatePaint()` the whole subtree
  (`src/Uno.UI.Composition/Composition/Visual.cs:196-217`).
- `ContainerVisual.ResetRenderOrder` does `Children.InnerList.Any(...)` + `OrderBy(...)` (LINQ,
  allocating) whenever `IsChildrenRenderOrderDirty` (`ContainerVisual.skia.cs:100-115`).
- `CompositionObject.PropagateChanged()` takes a `lock (_contextEntriesLock)` on **every** property
  set (`src/Uno.UI.Composition/Composition/CompositionObject.Context.cs:29-35`,
  called from `CompositionObject.cs:505`).
- Native-element hosting: when `ContentPresenter.HasNativeElements()`, every frame additionally runs
  `CalculateClippingPath` → a full `GetNativeViewPathAndZOrder` tree walk with `SKPath.Op` per visual
  (`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:46-48, 76-100`;
  `src/Uno.UI.Composition/Composition/Visual.skia.cs:594-634`). Without native elements this is
  short-circuited to a cached empty/inverted path (lines 26-31, 102-119) — good.

---

## 5. Where the swap/present is, and buffering

| Backend | Present call | Buffering | Blocking wait |
|---|---|---|---|
| Win32 GL | `PInvoke.SwapBuffers(_hdc)` — `Rendering/Win32WindowWrapper.Rendering.OpenGl.cs:235` | `PFD_DOUBLEBUFFER` (line 59) | `wglSwapIntervalEXT(1)` blocks the **render thread** |
| Win32 Vulkan | `VulkanContext.BlitAndPresent()` — `Rendering/Win32WindowWrapper.Rendering.Vulkan.cs:126` | `minImageCount + 1`, MAILBOX preferred → triple (`src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs:70-72, 104-109`) | `DwmFlush()` on the **render thread** (`Win32RenderPacer.cs:61`) |
| Win32 Software | `PInvoke.BitBlt(...)` — `Rendering/Win32WindowWrapper.Rendering.Software.cs:98` | single DIB section + window DC | `DwmFlush()` on the **render thread** (line 102) |
| X11 GLX | `glXSwapBuffers` — `Rendering/X11OpenGLRenderer.cs:71` | driver-managed | none from Uno; timer paces |
| X11 EGL | `EglSwapBuffers` — `Rendering/X11EGLRenderer.cs:118` | driver-managed | none from Uno |
| X11 Vulkan | `BlitAndPresent` — `Rendering/X11VulkanRenderer.cs:86` | MAILBOX/FIFO | none from Uno |
| macOS Metal | `[commandBuffer presentDrawable:]` + `commit` — `UNOMetalViewDelegate.m:75-79` | MTKView drawable pool (typically 3) | main-thread display pass |
| Android GL | GLSurfaceView's internal `eglSwapBuffers` after `OnDrawFrame` (`Rendering/UnoSKCanvasView.cs:160-229`) | EGL/BufferQueue | GLThread blocks in BufferQueue |
| Android Vulkan | `VulkanContext.RenderFrame(...)` → present (`Rendering/UnoSKVulkanView.cs:199-211`) | MAILBOX/FIFO | **none** |
| iOS Metal | `PresentDrawable` + `Commit` — `Rendering/UnoSKMetalView.cs:191-192` | MTKView drawable pool | CADisplayLink cadence |
| FB DRM | `drmModePageFlip` — `Rendering/DRMRenderer.cs:319` | GBM double buffer (`lock_front_buffer`/`release_buffer`, lines 309-316) | flip-event poll on its own thread |
| FB software | `ReadPixels` into `_fbDev.BufferAddress` — `Rendering/SoftwareRenderer.cs:67` | single fb + one SKSurface | `ioctl(FBIO_WAITFORVSYNC)` on the render thread (line 66) |
| WASM | `_canvas.Flush(); _renderer.Flush();` — `Rendering/BrowserRenderer.cs:104-107` | browser-managed | rAF |

**Does any explicit vsync wait block the UI thread?**
- Not in the steady state on Win32/X11/iOS/Android/FrameBuffer — the wait is on the render thread.
- **Yes on macOS and WASM**, because present *is* on the UI thread by construction (§1.3, §1.6).
- Bounded exception on Win32: `WaitForNextPresent(100 ms)` from the UI thread during
  `SynchronousRenderAndDraw` (`UI/Xaml/Window/Win32WindowWrapper.cs:425`).
- `TryExecuteOnNextRenderAsync` deliberately uses `TaskCreationOptions.RunContinuationsAsynchronously`
  so completing a GPU job never runs an awaiter inline on the render thread
  (`CompositionTarget.Rendering.skia.cs:387-392`).

**Application-level frame slot depth is 1.** `_lastRenderedFrame` is a single nullable tuple
(`CompositionTarget.Rendering.skia.cs:72`). If `Render()` produces a new picture before `Draw()`
consumed the previous one, the previous is dropped and released
(`lines 133-165`) and counted as "unpresented"
(`SkiaRenderHelper.FpsHelper.OnFrameRecorded`, `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:268-284`).
If `Draw()` runs with no new picture, the old one is re-blitted and counted as "dropped"
(`OnFramePresentRequested`, lines 292-324).

---

## 6. Theoretical vs practical max FPS, and the bottleneck

Diagnostics available in-product: `Application.Current.DebugSettings.EnableFrameRateCounter` drives
`FpsHelper` which reports **fps / dropped / unpresented / frame-time / draw-to-present delay**
(`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:121-537`; hooked at
`CompositionTarget.Rendering.skia.cs:126-129, 160, 243, 297-302`). Also
`#define REPORT_FPS` in `NativeDispatcher.cs:1` and `UnoSKMetalView.cs:143-151`, and
`#define PRINT_FRAME_TIMES` in `Compositor.skia.cs:1, 224-236`.

| Platform | Theoretical ceiling | Practical ceiling / bottleneck |
|---|---|---|
| Win32 GL | display refresh (swap interval 1) | **UI-thread `Render()` rate.** Present re-blits stale pictures when recording can't keep up. Full-tree walk (§4.2/4.3) is the cost. |
| Win32 Vulkan | display refresh (`DwmFlush`) | same; plus permanent timer-degradation risk after 3 `DwmFlush` failures (`Win32RenderPacer.cs:26, 67-76`) |
| Win32 Software | display refresh | CPU rasterization of the damage region + full-window `BitBlt` |
| X11 | `FeatureConfiguration.CompositionTarget.FrameRate` (default **60**, `src/Uno.UI/FeatureConfiguration.cs:118`) or XRandR fps when `SetFrameRateAsScreenRefreshRate` (default `true`, line 125) | **Timer quantization.** `FramePacer.RequestFrame` clamps the wait to `[1 ms, interval]` (`FramePacer.cs:51-57`) and uses `System.Timers.Timer` → thread-pool wake-up jitter, not phase-locked to the display. Expect beating/stutter even at nominally correct fps. |
| macOS | display refresh (60/120 ProMotion) | record + present serialized on the main thread; any UI-thread work directly costs frames |
| Android GL | display refresh | GLThread present is fine; UI-thread record is the limiter |
| Android Vulkan | **unbounded** (no pacer) | can spin above refresh while animating, burning CPU/GPU; needs a pacer |
| iOS Metal | `UIScreen.MaximumFramesPerSecond` (up to 120), min 30 via `CAFrameRateRange` (`UnoSKMetalView.cs:100-105`) | UI-thread record |
| WASM | rAF = display refresh | single-threaded: record, present, layout, input and GC all contend. Worst case of all targets. |
| FB DRM | CRTC vblank | fine |
| FB software | vblank via ioctl | `ReadPixels` (CPU copy of the whole surface) per frame |
| WPF | — | not in repo |

**Cross-cutting caps that apply everywhere:**
1. `InteractionTracker` inertia is hard-capped at ~58.8 Hz by a 17 ms `System.Threading.Timer`
   (§2.3). On a 120 Hz display, fling scrolling cannot be smooth by construction.
2. `FeatureConfiguration.CompositionTarget.FrameRate` default is **60** and only matters when
   `SetFrameRateAsScreenRefreshRate == false` (X11 always uses `FramePacer`, so it matters there in
   the fallback).
3. `NativeDispatcher.Android.MaxRenderSpan` = 11.1 ms hard-codes a 60 Hz assumption
   (`src/Uno.UI.Dispatching/Native/NativeDispatcher.Android.cs:25-29`).
4. `DispatcherAnimator.DefaultFrameRate = 60` (`DispatcherAnimator.skia.cs:12`).

---

## 7. Where the render loop can be starved by UI-thread work

### 7.1 Everything is on the UI thread (§3)

Layout, measure/arrange, input dispatch, data binding, GC (there is no server GC isolation for the UI
thread), *and* the entire composition record all share one thread. There is no independent animation
path, so UI-thread starvation is directly visible as frozen scrolling, not merely a stale frame.

### 7.2 The render slot is explicitly throttled against the Normal queue

`NativeDispatcher` has four queues (High/Normal/Low/Idle,
`src/Uno.UI.Dispatching/Native/NativeDispatcher.cs:26-32`,
`NativeDispatcherPriority.cs`) **plus** a special per-`CompositionTarget` render slot
(`_compositionTargets`, line 36). `DispatchItems` always tries the render slot first
(`TryGetRenderAction`, line 134, 206-234) — but:

```csharp
// NativeDispatcher.cs:214-216
if (details.normalItemsToProcessBeforeNextRenderAction == 0)
{
    _compositionTargets[compositionTarget] =
        (renderAction: null, normalItemsToProcessBeforeNextRenderAction: _queues[(int)Normal].Count);
```

i.e. after each render, **the render slot is blocked until as many Normal items have run as were
queued at that moment** (decremented at lines 156-165). This is deliberate anti-starvation for app
work, but it means a burst of Normal-priority items (layout ticks, `InteractionTracker.SetPosition`
marshals — §2.3 — bindings, `Loaded` events) directly delays the next recorded frame.

### 7.3 Layout tick and the "render opportunity"

`CoreServices.RequestAdditionalFrame()` enqueues `OnTick` at **Normal** priority
(`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:67-75`). `OnTick` calls `root.UpdateLayout()` for every
window, possibly twice (loaded events), then
`(…CompositionTarget)?.OnRenderFrameOpportunity()` (lines 108-126). Callers of
`RequestAdditionalFrame`: `EventManager.cs:34, 69`, `CustomEventManager.cs:60`,
`XamlRoot.crossruntime.cs:18, 26`.

`OnRenderFrameOpportunity` records *early* (before the platform asks) but only if
`SkiaRenderHelper.CanRecordPicture(rootElement)` — i.e. the root is not measure/arrange dirty
(`CompositionTarget.RenderScheduling.skia.cs:178-208`;
`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:33-34`). **If layout is still dirty when the tick runs,
the early-record is skipped** and the frame falls back to the (later) dispatcher render slot. Under
continuous re-layout — exactly what a virtualizing `ListView` does while scrolling — this early path
is systematically unavailable.

Note the `_renderedAheadOfTime` bookkeeping
(`CompositionTarget.RenderScheduling.skia.cs:71-73, 131-144, 186-207`): an ahead-of-time record
*consumes* the next scheduled render so the overall record rate is unchanged.

### 7.4 Platform-specific starvation vectors

- **Win32**: `Win32EventLoop.RunOnce` prioritizes `PM_QS_INPUT` over the dispatcher's posted message
  (`Native/Win32EventLoop.cs:122-123`). A dense mouse-move/wheel stream can therefore delay the
  render slot. Conversely, the render thread is immune (it's woken by an event, not a message) —
  which is why the `WM_PAINT` route was abandoned (`Rendering/Win32WindowWrapper.Rendering.cs:19-23`).
- **macOS / WASM**: present is on the UI thread; any long UI-thread operation costs a *presented*
  frame, not just a recorded one.
- **All**: `_pendingDamage` accumulates while the UI thread is stalled and is carried forward
  (`CompositionTarget.Rendering.skia.cs:139-144`), so a hitch is followed by one large-damage frame.

### 7.5 Per-frame allocation / GC pressure (partial inventory)

- `_runningAnimations.Keys.ToArray()` per frame with animations — `Compositor.skia.cs:206`
- `new FramePicture(picture)` per recorded frame — `CompositionTarget.Rendering.skia.cs:131`
- `new SKPath()` in `Draw` when no frame is available — line 248
- `RaiseRendering`: `FramePicture[]` + `List<(Window, object)>` + `RenderingEventArgs` per raise —
  lines 462-475
- `RenderChildrenStep` collapsing branch: `new SKPictureRecorder()` per collapsed subtree —
  `Visual.skia.cs:552`; same in the non-analytic shadow path, line 447
- `CalculateClippingPath`: `new SKPath()` + `new List<Visual>()` per frame **when native elements
  exist** — `SkiaRenderHelper.skia.cs:78, 84`
- `ContainerVisual.ResetRenderOrder`: LINQ `Any` + `OrderBy` — `ContainerVisual.skia.cs:104-111`
- Mitigations already in place: `Visual._pathPool` (`Visual.skia.cs:26`), the damage-snapshot
  `Stack<SKPath>` pool (`CompositionTarget.Rendering.skia.cs:69, 146, 156, 431`),
  `ArrayPool<string>` in `ReEvaluateAnimation` (`CompositionObject.cs:143, 179`).

---

## 8. Invalidation coalescing; render-on-demand vs continuous

### 8.1 Coalescing — yes, at three levels

1. **`RequestNewFrame` state machine** — the canonical one.
   `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:69-118`:
   three flags (`_renderRequested`, `_renderedAheadOfTime`, `_renderRequestedAfterAheadOfTimePaint`)
   under `_renderingStateGate`; `host.InvalidateRender()` is called **only** on the 0→1 transition
   (lines 93-113). The class comment (lines 29-43) shows repeated `RequestNewFrame` calls being
   ignored. Invariants asserted in `AssertRenderStateMachine` (lines 210-218).
2. **Platform-level coalescing**: Win32 `AutoResetEvent` (`RenderThread.cs:41-45`), Android
   `RequestRender()` on a `WhenDirty` GLSurfaceView, WASM `_pendingInvalidate`
   (`BrowserRenderer.cs:49-56`), DRM `_invalidateRenderCalledWhileWaitingForPageFlip`
   (`DRMRenderer.cs:286-297, 386-392`), X11 `FramePacer` (one timer arm).
3. **Rendering-event coalescing**: `_renderingRaiseScheduled` guarantees one `RaiseRendering` per
   batch of recorded frames (`CompositionTarget.Rendering.skia.cs:52, 448-452`).

The contract is explicitly stated: `OnNativePlatformFrameRequested` "does not assume that this method
will only be called once per `InvalidateRender` call, but the contract allows any number of repeated
calls, even if no new invalidations are requested"
(`CompositionTarget.RenderScheduling.skia.cs:159-165`).

### 8.2 Render-on-demand is the default; continuous mode exists

- **On demand** by default: a frame is only produced when something invalidates.
- **Continuous** while any of these hold:
  - a `CompositionTarget.Rendering` subscriber exists → `_isRenderingActive` → `Render()` calls
    `RequestNewFrame()` every frame (`CompositionTarget.Rendering.skia.cs:84-108, 167-170`).
    Any active `Storyboard`, `ScrollPresenter`, `ScrollView`, or gesture inertia puts the app here.
  - any composition animation or background transition is running
    (`Compositor.skia.cs:252-255`).
  - a visual has `RequiresRepaintOnEveryFrame` (`Visual.skia.cs:137, 478-484`).
- `Render()` also unconditionally re-arms `host.InvalidateRender()` at the end
  (`CompositionTarget.Rendering.skia.cs:172-175`) so that the just-recorded picture gets presented —
  meaning a single invalidation actually produces *two* host wake-ups (one to present the old picture
  and schedule the record, one to present the new picture).
- Idle detection exists only in the FPS counter (`FpsHelper.TimerTick`, 2 consecutive idle 1 Hz
  ticks → "Idle", `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:467-509`).
- `FeatureConfiguration.Rendering.SkipVisualTreePainting` skips only the paint walk while keeping
  scheduling/animations alive (`src/Uno.UI/FeatureConfiguration.cs:720-736`;
  `Compositor.skia.cs:40, 229-232`).

---

## 9. Ranked list of what caps scroll smoothness today

1. **No independent/compositor-thread animation.** All animation evaluation and all scroll-offset
   application are on the UI thread (§3). Any UI-thread hitch stops scroll motion outright.
2. **InteractionTracker inertia on a 17 ms thread-pool timer** with integer-ms sampling, marshalled
   as Normal-priority dispatcher work (§2.3). Caps fling at ~59 Hz and guarantees beating on
   120/144 Hz displays.
3. **`SetMatrixDirty` recursing over the whole descendant subtree and destroying every
   `_childrenPicture` in it, on every offset change** (§4.3). Makes the subtree-skipping optimization
   unusable exactly where it matters most, and makes per-frame cost O(visuals in content).
4. **`InvalidateRenderPartial` calling `InvalidatePaint()` for *any* property change**
   (`Compositor.skia.cs:258-263`) — a pure translation re-records the visual's own picture.
5. **Structural latency**: change → next platform frame → dispatcher render slot → record → *next*
   platform frame → present ⇒ ≥2 vsync from input to pixels; `CompositionTarget.Rendering`
   subscribers add a third (§1.0, §2.2).
6. **Animations sampled with `Stopwatch` wall-clock at record time**, never a predicted presentation
   timestamp (§2.1) — record-time jitter becomes positional jitter.
7. **X11 is not vsync-locked at all** (timer-paced `FramePacer`, no swap-interval control) (§1.2).
8. **Render-slot throttling against the Normal queue** (`normalItemsToProcessBeforeNextRenderAction`,
   §7.2) lets app work delay frames by design.
9. **`OnRenderFrameOpportunity` is disabled whenever layout is dirty** (§7.3) — i.e. during
   virtualized scrolling, the cheapest path to a low-latency frame is unavailable.
10. **Android Vulkan has no frame pacer** (§1.4) — free-running loop.
11. **Per-frame allocations** in the animation tick and rendering-event raise (§7.5).
12. **macOS and WASM serialize record + present on the UI thread** (§1.3, §1.6).

---

## 10. Quick file index

| Concern | File |
|---|---|
| Frame state machine, `RequestNewFrame`, render slot enqueue | `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs` |
| `Render()`, `Draw()`, damage, `Rendering` event, `FramePicture` | `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs` |
| Animation tick, `InvalidateRenderPartial`, transitions | `src/Uno.UI.Composition/Composition/Compositor.skia.cs` |
| `AnimationFrame` plumbing | `src/Uno.UI.Composition/Composition/CompositionAnimation.cs`, `CompositionObject.cs` |
| Keyframe time base | `src/Uno.UI.Composition/Composition/KeyFrameAnimations/KeyFrameEvaluator.cs` |
| Inertia timers (17 ms) | `src/Uno.UI.Composition/Composition/InteractionTracker/InteractionTracker*InertiaHandler.cs` |
| Tree walk, picture caches, collapsing optimization | `src/Uno.UI.Composition/Composition/Visual.skia.cs`, `ContainerVisual.skia.cs` |
| Damage-region computation | `src/Uno.UI.Composition/Composition/Visual.Damage.skia.cs` |
| Picture recording + FPS counter | `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs` |
| Retained layer (damage across frames) | `src/Uno.UI/Helpers/RetainedLayer.skia.cs` |
| Dispatcher queues + render slot | `src/Uno.UI.Dispatching/Native/NativeDispatcher.cs` |
| Frame pacing (timer) | `src/Uno.UI.Runtime.Skia/Hosting/FramePacer.cs` |
| Win32 render thread / pacer / backends | `src/Uno.UI.Runtime.Skia.Win32/Rendering/*` |
| X11 render thread | `src/Uno.UI.Runtime.Skia.X11/Rendering/*` |
| macOS host + native view | `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs`, `UnoNativeMac/UnoNativeMac/UNO{Window,MetalViewDelegate}.m` |
| Android views | `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSK{CanvasView,VulkanView}.cs` |
| iOS view | `src/Uno.UI.Runtime.Skia.AppleUIKit/Rendering/UnoSKMetalView.cs` |
| WASM renderer | `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Rendering/BrowserRenderer.cs`, `ts/Runtime/BrowserRenderer.ts` |
| FrameBuffer | `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/*` |
| Vulkan swapchain / present mode | `src/Uno.UI/Vulkan/Interop/VulkanDisplay.skia.cs` |
| Feature switches | `src/Uno.UI/FeatureConfiguration.cs` (`CompositionTarget`, `Rendering`), `src/Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.GestureRecognizer.cs` |
