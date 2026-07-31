# Firsthand pipeline reading (orchestrator notes)

Read directly, not via research agents. Every claim below is cited to code in this worktree
(`dev/mazi/smooth-scroll`, base `3cff83601b`).

## 1. The Skia frame pipeline is a record/replay split

`CompositionTarget` runs a two-stage pipeline:

| Stage | Thread | Work |
|---|---|---|
| `Render()` | **UI thread** | Walks the visual tree, ticks animations, records one `SKPicture`, computes damage |
| `Draw()` | render/GPU thread (platform-dependent) | Replays the last recorded `SKPicture` into the canvas, clipped to damage |

- `CompositionTarget.Rendering.skia.cs:110` — `Render()`, `NativeDispatcher.CheckThreadAccess()` at :114.
- `CompositionTarget.Rendering.skia.cs:224` — `Draw()`.
- `CompositionTarget.RenderScheduling.skia.cs:166` — `OnNativePlatformFrameRequested` is the platform's
  "draw now" callback; it enqueues the *next* UI-thread record via
  `NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)` (:172) and then presents the
  *previous* picture (:175).

**Consequence:** the animation tick rate equals the UI-thread record rate. Everything animated —
including all scrolling — is gated by how fast the UI thread can re-record the picture.

### Animation ticking

`Compositor.skia.cs:199-256 RenderRootVisual`:

```csharp
foreach (var animation in _runningAnimations.Keys.ToArray())   // :206  <-- per-frame array alloc
{
    animation.RaiseAnimationFrame();
}
...
if (_runningAnimations.Count > 0 || transitionsCount > 0)
{
    rootVisual.CompositionTarget?.RequestNewFrame();            // :254  self-sustaining loop
}
```

- Animations are evaluated **on the UI thread, inline with the picture record**. There is no
  server-side/compositor-thread animation.
- `_runningAnimations.Keys.ToArray()` allocates a `CompositionAnimation[]` **every frame** while any
  animation is running — which, during a wheel scroll, is every frame for a full second.

## 2. Paint invalidation on a scroll — better than expected, with one caveat

`Compositor.skia.cs:258-263`:

```csharp
partial void InvalidateRenderPartial(Visual visual)
{
    visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
    visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
    visual.CompositionTarget?.RequestNewFrame();
}
```

Any property change (including a pure translation) does both. But:

- `Visual.skia.cs:234 InvalidatePaint()` drops **this visual's own** `_picture` and calls
  `InvalidateParentChildrenPicture(includeSelf: false)` (:242) — so the scrolled content's own
  `_childrenPicture` **survives** a scroll; only the ancestor chain's children-pictures are dropped.
- `RenderChildrenStep` (`Visual.skia.cs:531`) records a cached subtree picture in the visual's
  **local space** (`rootTransform = Invert(visual.TotalMatrix)`, :555; `CreateLocalSession` re-applies
  the matrix at :1011-1018). A pure translation therefore does *not* invalidate that cache.

**Caveat — the cache almost never engages during a real scroll.** The picture-collapsing optimization
requires *both* (`Visual.skia.cs:540-543`):

| Gate | Value | Source |
|---|---|---|
| `PictureCollapsingOptimizationFrameThreshold` | **50 frames** of an unchanged subtree | `Visual.skia.cs:40` |
| `PictureCollapsingOptimizationVisualCountThreshold` | **100 visuals** | `Visual.skia.cs:41` |

In a virtualizing list, realization mutates the subtree constantly, so `_framesSinceSubtreeNotChanged`
(:396) resets and the collapse never engages. Result: the **whole realized subtree is re-walked and
re-recorded every scroll frame**.

## 3. `SetMatrixDirty` is O(subtree) per scroll delta

`ContainerVisual.skia.cs:212-227` recurses into every child. Cheap per node (a flag), with an early-out
when already dirty — but it is still a full subtree walk per offset change, on the UI thread.

## 4. Per-scroll-delta layout/viewport work

`ScrollContentPresenter.Managed.cs:395-410 UpdateOffsets` runs on **every** offset change and calls:

1. `Scroller?.OnPresenterScrolled(h, v, isIntermediate)` (:402)
2. `ScrollOffsets = new Point(h, v)` (:408)
3. `InvalidateViewport()` (:409)

`InvalidateViewport()` → `PropagateEffectiveViewportChange()`
(`FrameworkElement.EffectiveViewport.cs:256-266`) walks the subtree of the scroll port, recomputing
viewport rects and raising `EffectiveViewportChanged` — which is what drives virtualization
realization. **This happens synchronously, per scroll delta, on the UI thread, inside the frame
record.**

`ScrollViewer.OnPresenterScrolled` (`ScrollViewer.cs:1234`) defers the DP writes when
`isIntermediate && UpdatesMode != Synchronous` via `RequestUpdate()` (:1301) — a
`Dispatcher.RunAsync(Normal, …)` guarded by `_hasPendingUpdate`. That part is already reasonable.

## 5. Mouse wheel: a 1-second animation restarted on every detent

`ScrollContentPresenter.cs:245-358 PointerWheelScroll`:

- Non-Apple platforms: `Set(verticalOffset: TargetVerticalOffset + GetVerticalScrollWheelDelta(...), disableAnimation: false)`
  (:346-348) → `Update()` starts a **`Vector2KeyFrameAnimation` on `Visual.AnchorPoint`** with
  `Duration = TimeSpan.FromSeconds(1)` and
  `CreatePowerEasingFunction(compositor, Out, 10)` (`ScrollContentPresenter.Managed.cs:474-479`).
- Apple platforms: bypass the animation entirely and set the offset instantly, gated on a
  `|delta| < 120` "is this a trackpad?" heuristic (:311-325, :335-343).

Problems:

1. **Power-10 ease-out over 1 s** puts ~15 % of the distance in the first frame and then dribbles for
   the rest of a second. The tail keeps `_runningAnimations` non-empty, so
   `RequestNewFrame()` fires every frame for a full second after the last detent — the render loop
   never goes idle during wheel scrolling.
2. Each detent **restarts** the animation from the current visual position toward a new absolute
   target, with an ad-hoc "don't restart if within 4 px² and <50 ms remaining" guard
   (`ScrollContentPresenter.Managed.cs:434-460`).
3. The Apple branch is a platform heuristic, not a model — and it means macOS/iOS get *no* wheel
   smoothing at all.

## 6. Touch: raw input events drive the visual directly — no vsync alignment

This is the likely explanation for "Android/iOS/WASM feel worse than Win32".

- `GestureRecognizer.Manipulation.cs:231 Update(IList<PointerPoint>)` → `NotifyUpdate()` (:250) →
  `ManipulationUpdated` (:425-427) → `ScrollContentPresenter.OnUpdated`
  (`ScrollContentPresenter.Managed.cs:591`) → `Set(..., DisableAnimation: true, IsTouch: true)` →
  `visual.AnchorPoint = target` → `RequestNewFrame()`.
- **Every OS touch-move event moves the content immediately.** There is no resampling, no alignment
  to the display refresh, and no coalescing. Input arriving at a rate/phase that does not match vsync
  produces uneven per-frame deltas — visible as stutter even at a nominal 60 FPS.
- Win32 is mostly exercised with a **mouse wheel**, which goes through the animation path and is
  therefore implicitly frame-aligned. That asymmetry matches the reported per-platform feel.

### Velocity estimation is a crude two-point estimate

`GestureRecognizer.Manipulation.cs:462-467`:

```csharp
var velocitiesPoints = _currents.StateHistory.GetBoundaries(static p => p.Timestamp);
var velocitiesElapsedMicroseconds = velocitiesPoints.to.Timestamp - velocitiesPoints.from.Timestamp;
var velocitiesDelta = ComputeDelta(velocitiesPoints.from, velocitiesPoints.to, parentCommit.SumOfDelta);
velocities = ComputeVelocities(velocitiesDelta, velocitiesElapsedMicroseconds);
```

First-vs-last sample over the rolling history. No least-squares fit, no outlier rejection, no horizon
cutoff. Compare Flutter's `VelocityTracker` (least-squares degree-2 fit over a 100 ms horizon).

## 7. Inertia physics: constant deceleration, not the platform model

`GestureRecognizer.Manipulation.InertiaProcessor.cs:268-274`:

```csharp
internal static double GetValue(double v0, double d, double t)
    => v0 >= 0 ? v0 * t - d * Math.Pow(t, 2) : -(-v0 * t - d * Math.Pow(t, 2));
private static bool IsCompleted(double v0, double d, double t)
    => Math.Abs(v0) - d * 2 * t <= 0;
```

Constant-deceleration (parabolic) inertia with `DefaultDesiredDisplacementDeceleration = .001` (:65).
Per-platform tweaks are applied in `ScrollContentPresenter.Managed.cs:717-744`:

- iOS: a duration derived from PastryKit's 0.95-per-frame decay, converted back to a *constant*
  deceleration — i.e. an exponential model flattened into a parabolic one.
- Android: `DefaultDesiredDisplacementDeceleration / 2` — a bare magic number.
- Everything else: the default.

Neither Android's `OverScroller` spline nor iOS's exponential-friction model is actually reproduced.

### Inertia tick source

`InertiaProcessor.Start(useCompositionTimer)` (:184-210) picks between:

- `CompositionInertiaProcessorTimer` (:333) — hooks `CompositionTarget.Rendering`, i.e. one tick per
  recorded frame. **This is the default** (`WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation`
  and `…ForUiElement` both default to `true`, `WinRTFeatureConfiguration.GestureRecognizer.cs:66-77`).
- `DispatcherInertiaProcessorTimer` (:312) — a `DispatcherQueueTimer` at **30 FPS** (:316).

So inertia is frame-driven by default. Good — but note the timer measures its own `Stopwatch.Elapsed`
(:347) rather than the frame's presentation time, so tick timing is sampled at handler-invocation
time, not at the frame boundary.

## 8. Per-platform frame sources — all correct, so this is *not* the differentiator

| Target | Frame source | Threading | Cite |
|---|---|---|---|
| Win32 | `DwmFlush()` vsync, degrading to a timer after 3 failures | **Dedicated render thread** | `Win32RenderPacer.cs:53-89`, `Win32WindowWrapper.RenderThread.cs:52-90` |
| Android | `GLSurfaceView` GL thread, `RenderMode.WhenDirty` + `RequestRender()` | GL thread; `eglSwapBuffers` blocks | `UnoSKCanvasView.cs:53,61-66,160` |
| iOS | `CADisplayLink` → `MTKView.Draw` | `Paused=true`, `EnableSetNeedsDisplay=false`, link-driven | `UnoSKMetalView.cs:27,37,72-78,132` |
| WASM | `window.requestAnimationFrame` | single-threaded | `ts/Runtime/BrowserRenderer.ts:48`, `Rendering/BrowserRenderer.cs:47` |

All four are legitimate vsync-aligned sources. **The frame source is not the problem.**

The Android-specific risk is scheduling *phase*, not source: `NativeDispatcher.EnqueueNative` uses
`_handler.Post(_implementor)` (`NativeDispatcher.Android.cs:40-43`), a plain main-Looper post, so the
UI-thread record lands at an arbitrary phase relative to the Choreographer frame. A Choreographer
path exists (`RunAnimation`/`PostFrameCallback`, :49-59) but the render record does not use it.

## 9. Android touch input drops the historical samples

`AndroidCorePointerInputSource.OnNativeMotionEvent` (:71-119) reads only the current
`MotionEvent` coordinates. `MotionEvent.getHistoricalX/Y` — Android's batched
higher-frequency samples — are never read. That loses both precision for velocity estimation and
intermediate positions that a resampler would need.

## 10. Measurement instrument already exists

`Application.Current.DebugSettings.EnableFrameRateCounter` drives `SkiaRenderHelper.FpsHelper`
(`SkiaRenderHelper.cs:121-215`), which already reports FPS, **dropped frames**, **unpresented
frames**, and frame time. SamplesApp exposes it as `ShowFpsIndicator`
(`SampleChooserViewMode.Properties.cs:519`). This is the before/after evidence instrument.

## Working hypothesis going into the design

Ranked by expected impact on perceived smoothness:

1. **No input→frame alignment for touch.** Raw pointer events mutate the visual directly. Fix:
   resample/align pointer deltas to the frame clock (Flutter's model).
2. **Wheel uses a 1 s power-10 keyframe animation restarted per detent**, with an Apple-only bypass.
   Fix: one continuous, velocity-composing scroll model shared by all platforms.
3. **Virtualization work runs synchronously inside the scroll frame** via
   `InvalidateViewport()` → `PropagateEffectiveViewportChange()`.
4. **Inertia physics is constant-deceleration with per-platform magic numbers**, not the real
   platform models.
5. **Per-frame allocations** in the animation tick and the picture-collapsing thresholds (50 frames /
   100 visuals) that never engage for virtualized content.
