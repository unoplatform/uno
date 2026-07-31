# Flutter inertia: what timestamp the ballistic simulation is evaluated at, and why that is the whole answer

Research note for the Uno inertia-smoothness investigation.
Flutter checkout: `D:/Work/flutter` @ `1add24630ae` (Fri Apr 24 2026).
Uno worktree: `D:/Work/uno-worktrees/scrollsmooth` (branch `dev/mazi/smooth-scroll`).

Every claim is cited `file:line`. Anything I could not read in source is marked **UNVERIFIED**.

Flutter is the right comparator: like Uno it runs scroll physics **on the UI thread**, in the same
task that builds and paints the frame. It has no compositor-thread scroll, no DirectManipulation
equivalent, no paint-only offset for sliver viewports (it re-lays-out the viewport every scroll
frame — `rendering/viewport.dart:685-695` adds `markNeedsLayout` as the offset listener). So
whatever makes Flutter's fling smooth is a scheduling/clock property, not a threading trick, and
is therefore adoptable by Uno as-is.

---

## 0. The one-sentence answer

**Flutter evaluates the fling at the frame's *target presentation time*, supplied by the display
hardware; Uno evaluates it at the wall-clock instant the paint walk happens to start.**

- Flutter: `t` comes from `Choreographer.doFrame(frameTimeNanos)` / `CADisplayLink.targetTimestamp`,
  and the engine comments on it explicitly: *"frameTime is not a delta; its the timestamp of the
  presentation"* — `engine/src/flutter/lib/ui/window/platform_configuration.cc:469-471`.
- Uno: `t` comes from `Stopwatch.GetTimestamp()` read at the top of the paint walk —
  `src/Uno.UI.Composition/Composition/Compositor.cs:38` sampled at
  `src/Uno.UI.Composition/Composition/Compositor.skia.cs:230`.

Uno is *already receiving* the correct timestamp on Android and throwing it away:

```csharp
// src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:97-100
private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
{
    public void DoFrame(long frameTimeNanos) => onFrame();   // ← frameTimeNanos discarded
}
```

Same on WASM — the `DOMHighResTimeStamp` argument of `requestAnimationFrame` is dropped at
`src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/BrowserRenderer.ts:47-51`.

Why this is the *asymmetry* and not just "a latency bug" is §5.

---

## 1. Q1 — exactly what timestamp is a `BallisticScrollActivity` tick evaluated at?

### 1.1 The framework half of the chain

```
ScrollDragController.end(details)                       widgets/scroll_activity.dart:419-442
  → delegate.goBallistic(velocity)                      widgets/scroll_position_with_single_context.dart:149-157
    → BallisticScrollActivity(this, simulation, context.vsync, …)     :153
      → AnimationController.unbounded(vsync: vsync)      widgets/scroll_activity.dart:596-605
          ..addListener(_tick)
          ..animateWith(simulation)
        → AnimationController._startSimulation(sim)      animation/animation_controller.dart:861-872
          → _ticker!.start()                             :866
            → Ticker.scheduleTick()                      scheduler/ticker.dart:290-303
              → SchedulerBinding.scheduleFrameCallback(_tick)   (a TRANSIENT callback)
```

The tick itself:

```dart
// scheduler/ticker.dart:271-284
void _tick(Duration timeStamp) {
  _animationId = null;
  _startTime ??= timeStamp;
  _onTick(timeStamp - _startTime!);      // elapsed
  if (shouldScheduleTick) scheduleTick(rescheduling: true);
}

// animation/animation_controller.dart:941-955
void _tick(Duration elapsed) {
  _lastElapsedDuration = elapsed;
  final double elapsedInSeconds = elapsed.inMicroseconds / Duration.microsecondsPerSecond;
  _value = clampDouble(_simulation!.x(elapsedInSeconds), lowerBound, upperBound);
  if (_simulation!.isDone(elapsedInSeconds)) { … stop(canceled: false); }
  notifyListeners();                     // → BallisticScrollActivity._tick → setPixels
}

// widgets/scroll_activity.dart:619-635
void _tick() {
  if (!applyMoveTo(_controller.value)) delegate.goIdle();
}
bool applyMoveTo(double value) => delegate.setPixels(value).abs() < precisionErrorTolerance;
```

`timeStamp` is whatever `SchedulerBinding` passes to transient callbacks, i.e.
`_currentFrameTimeStamp` (`scheduler/binding.dart:1258-1269`), which is
`_adjustForEpoch(rawTimeStamp)` (`:1229`, `_adjustForEpoch` at `:1116-1125` — an affine remap that
only subtracts the epoch origin and divides by `timeDilation`, default 1.0). So, modulo a constant
offset, **`timeStamp` is the raw timestamp the engine handed to `PlatformDispatcher.onBeginFrame`.**

Two details that matter for a port:

* **`_startTime` anchoring.** `Ticker.start()` only pre-seeds `_startTime` if it is called *inside*
  a frame (`ticker.dart:202-205`, phase strictly between `idle` and `postFrameCallbacks`). Pointer
  events are dispatched outside frames — `GestureBinding._handlePointerDataPacket` →
  `_flushPointerEventQueue` runs straight off the engine callback
  (`gestures/binding.dart:302-322`, `:347-353`), and `_handleDragEnd` → `_drag?.end(details)` is on
  that path (`widgets/scrollable.dart:887-890`). Therefore at drag-end the phase is `idle`,
  `_startTime` stays null, and it is set on the **first tick** to that tick's frame timestamp
  (`ticker.dart:276`). Consequence: the first ballistic frame evaluates `x(0.0)` = exactly the
  position the drag ended at. **The drag→fling handoff never contains a partial-frame jump.**
* **Velocity is read off the simulation, never differenced** (`animation_controller.dart:401-408`),
  so the reported `velocity` used by `resetActivity`/`applyNewDimensions`
  (`scroll_activity.dart:609-617`) is exact and re-creating the simulation mid-fling is a visual
  no-op.

### 1.2 The engine half of the chain — where the number actually comes from

```
Choreographer / CADisplayLink
  → VsyncWaiterAndroid::OnVsyncFromNDK(frame_nanos)      shell/platform/android/vsync_waiter_android.cc:64-81
  → VsyncWaiter::FireCallback(frame_start_time, frame_target_time, pause_secondary_tasks=true)
                                                          shell/common/vsync_waiter.cc:87-152, vsync_waiter.h:75
      → FrameTimingsRecorder::RecordVsync(start, target)  vsync_waiter.cc:137-140
      → Animator::BeginFrame(recorder)                    shell/common/animator.cc:61-119
          const fml::TimePoint frame_target_time = recorder->GetVsyncTargetTime();   // :114-115
          delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number);           // :118
      → Engine::BeginFrame(frame_time, n)                 shell/common/engine.cc:296-297
      → RuntimeController::BeginFrame                     runtime/runtime_controller.cc:299-303
      → PlatformConfiguration::BeginFrame                 lib/ui/window/platform_configuration.cc:454-492
```

`platform_configuration.cc:469-471` is the load-bearing comment:

```cpp
// frameTime is not a delta; its the timestamp of the presentation.
// This is just a type conversion.
int64_t microseconds = frameTime.ToEpochDelta().ToMicroseconds();
```

and `:472-481` clamps it monotonically ("Do not allow time traveling frametimes"), so the tick
timestamp sequence is guaranteed non-decreasing even if a platform vsync source misbehaves.

**Where `frame_target_time` is computed, per platform:**

| platform | formula | citation |
|---|---|---|
| Android (NDK Choreographer, API ≥ 29) | `frame_time = min(frameTimeNanos, now)`; `target = frame_time + 1e9 / g_refresh_rate_` | `vsync_waiter_android.cc:64-81` |
| Android (Java fallback) | `frame_time = now − (now − frameTimeNanos)`; `target = frame_time + refreshPeriodNanos` | `vsync_waiter_android.cc:84-101`, `io/flutter/view/VsyncWaiter.java:94-99` |
| iOS/macOS | `frame_start = now − (CACurrentMediaTime() − link.timestamp)`; `target = frame_start + (link.targetTimestamp − link.timestamp)` | `shell/platform/darwin/ios/framework/Source/vsync_waiter_ios.mm:116-135` |

Note what is *not* in any of these: `TimePoint::Now()` used as the value. `Now()` appears only to
*reconstruct* the hardware vsync instant from a measured delay. The base is always the display's own
vsync timestamp, and the value handed to Dart is that base **plus one frame interval**, i.e. the
instant the pixels of this frame are expected to hit the panel.

### 1.3 Summary answer to Q1

> A `BallisticScrollActivity` tick evaluates `Simulation.x(t)` where
> `t = (this frame's target presentation time) − (the target presentation time of the first
> ballistic frame)`, in seconds. The timestamp originates from `Choreographer.doFrame`'s
> `frameTimeNanos` (Android) or `CADisplayLink.targetTimestamp` (Apple), is offset by exactly one
> display interval to become a presentation time, is monotonically clamped, and is delivered to the
> transient-callback phase before anything is built, laid out or painted.

---

## 2. Q2 — how does Flutter guarantee that the value computed in frame N is the value presented in frame N?

Stated precisely, Flutter guarantees five things. Only the first is a *presentation* guarantee; the
rest are the reason the first one is enough.

### 2.1 The value is computed *for* the presentation instant, not for "now"

Covered in §1.2. This is the guarantee. It is a *definitional* one: rather than trying to make
compute-time equal present-time, Flutter parameterises the motion by present-time. The offset
between "when we computed" and "when it shows" is not measured, corrected, or filtered — it is
simply not part of the equation.

### 2.2 Compute and record happen in one uninterruptible UI-thread task

```cpp
// shell/common/animator.cc:273-289 (AwaitVSync)
self->BeginFrame(std::move(frame_timings_recorder));
self->EndFrame();
```
`animator.h:116-122` states the contract: *"Animator's work during a vsync is split into two
methods, BeginFrame and EndFrame. The two methods should be called synchronously back-to-back."*

Inside that, `platform_configuration.cc:483-491` runs, in one C++ function with no yield:

```cpp
tonic::DartInvoke(begin_frame_.Get(), {microseconds, frame_number});   // → handleBeginFrame
UIDartState::Current()->FlushMicrotasksNow();
tonic::DartInvokeVoid(draw_frame_.Get());                              // → handleDrawFrame
```

and on the Dart side that is:

- `handleBeginFrame` — phase `transientCallbacks`, tickers run here → scroll offset written
  (`scheduler/binding.dart:1226-1274`);
- `handleDrawFrame` — phase `persistentCallbacks` (build/layout/paint) then `postFrameCallbacks`
  (`:1338-1376`).

While that task runs, the Dart event loop's *other* sources are **paused**:
`VsyncWaiter::FireCallback(..., pause_secondary_tasks)` defaults to `true` (`vsync_waiter.h:75`) and
calls `PauseDartEventLoopTasks()` before and `ResumeDartEventLoopTasks()` after
(`vsync_waiter.cc:112-146`, `:154-167`). No timer, no platform message, no pointer packet can land
between "physics computed" and "picture recorded".

### 2.3 One value per frame, enforced by assert

```dart
// widgets/scroll_position.dart:366-371
double setPixels(double newPixels) {
  assert(hasPixels);
  assert(
    SchedulerBinding.instance.schedulerPhase != SchedulerPhase.persistentCallbacks,
    "A scrollable's position should not change during the build, layout, and paint phases, "
    "otherwise the rendering will be confused.",
  );
```

The scroll offset is *immutable for the duration of the frame's layout/paint*. There is no way for a
late input event or a second physics evaluation to produce a torn frame. Layout-time corrections
that *are* legal go through `correctPixels`/`correctBy`, which deliberately do **not** notify, so
they are consumed by the same frame's re-layout loop (`scroll_position.dart:437-440`, `:457-465`,
`rendering/viewport.dart:1721-1740`).

### 2.4 The recorded picture is committed atomically, tagged with that frame's timing record

`Animator::EndFrame` moves the layer trees plus the `FrameTimingsRecorder` (which holds this frame's
vsync/target times) into a single `FrameItem` and completes the pipeline continuation
(`animator.cc:128-155`). The layer tree and the timestamp it was computed for travel together to the
raster thread.

### 2.5 If the frame *cannot* be presented on time, Flutter drops it rather than presenting it late

The layer-tree pipeline is depth 2 (depth 1 when platform and raster runners are the same thread) —
`animator.cc:32-42`. If the rasterizer is behind, `Produce()` returns null and:

```cpp
// animator.cc:99-108
if (!producer_continuation_) {
  TRACE_EVENT0("flutter", "PipelineFull");
  RequestFrame();
  return;                    // ← the entire Dart frame is skipped; no tick, no build
}
```

**The tickers do not run at all on a skipped frame.** Combined with the closed-form simulation, this
is free: the next frame evaluates `x(t)` at its own later target time and is exactly where it should
be. There is no accumulated `dt` to replay, no velocity spike, no catch-up. A dropped frame costs
temporal resolution and nothing else.

### 2.6 What is *not* guaranteed

Flutter does not guarantee the buffer is actually scanned out at `frame_target_time`. If the raster
thread overruns, the frame presents late and the content is briefly behind where it should be. What
Flutter guarantees is that **the error is a pure latency, identical for every element of the frame,
and self-correcting on the next frame** — never a per-frame *velocity* modulation. That distinction
is the entire subject of §5.

---

## 3. Q3 — what does Flutter do that Uno currently does not, adoptable without a compositor thread?

Five things, in descending order of expected effect. Each is a few dozen lines.

### 3.1 (Primary) Publish the frame's presentation timestamp and evaluate drivers against it

**What Uno does today.** `Compositor.RenderRootVisual` samples a free-running clock at the top of
the paint walk and hands that to every frame driver:

```csharp
// src/Uno.UI.Composition/Composition/Compositor.cs:38
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));

// src/Uno.UI.Composition/Composition/Compositor.skia.cs:226-243
if (FrameStarting is { } frameStarting)
{
    var frameTimestamp = TimestampInTicks;   // ← "now", at record time
    CurrentFrameTimestampInTicks = frameTimestamp;
    frameStarting(frameTimestamp);
}
```

```csharp
// src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:615-617
private void OnFlingFrame(long timestampInTicks)
{
    var elapsed = (timestampInTicks - _flingStartTimestamp) / (double)TimeSpan.TicksPerSecond;
```

and the anchor is likewise a "now" read taken during pointer-up handling:

```csharp
// ScrollContentPresenter.Managed.cs:588-596
_flingStartTimestamp = compositor.TimestampInTicks;
```

**Where the record instant sits relative to vsync, on Android.** The render thread renders *then*
paces:

```csharp
// src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:143-159
_renderEvent.Wait(TimeSpan.FromMilliseconds(100));
_renderEvent.Reset();
…
_renderRequested = false;
RenderFrame();                    // ← FrameStarting fires in here, clock sampled
_pacer.WaitForNextFrame();        // ← vsync wait happens AFTER
```

and `ChoreographerFramePacer.WaitForNextFrame` posts the frame callback lazily, on demand
(`ChoreographerFramePacer.cs:66-76`). So the phase between the previous vsync and the clock sample
is `(handler post latency) + (render-request wakeup) + (whatever CPU the frame needed before the
paint walk)`. That phase is **not constant** — it moves with GC, with how many items the scroll
realised this frame, with thread scheduling. Every millisecond of variation in it becomes a
position error of `v × 1 ms`, i.e. **3 px at 3000 px/s**, applied differently on each frame.

**The adoption.** Nothing here needs a compositor thread — it needs the number Android is already
handing us:

1. `ChoreographerFramePacer.FrameCallback.DoFrame(long frameTimeNanos)` currently discards its
   argument (`ChoreographerFramePacer.cs:99`). Capture it, convert to the `Stopwatch` timebase, and
   publish `presentationTimestamp = frameTimeNanos + refreshPeriodNanos`.
   `Choreographer.frameTimeNanos` is `CLOCK_MONOTONIC`, the same base `Stopwatch.GetTimestamp()`
   uses on Linux/Android — **UNVERIFIED for the .NET Android runtime specifically**; verify with a
   one-off log comparing the two before relying on it, or (safer) keep an offset calibrated once at
   startup, exactly as Flutter reconstructs `frame_time = Now() − delay`
   (`vsync_waiter_android.cc:89-92`).
2. Plumb it to `Compositor` as `CurrentFramePresentationTimestampInTicks` and pass **that** to
   `FrameStarting`, keeping `TimestampInTicks` for anything that genuinely wants "now".
3. Fall back to `TimestampInTicks + estimatedRefreshPeriod` where no platform timestamp exists —
   still better than "now", because the error becomes a constant instead of a variable.

Per-platform sources, all already available:

| Uno target | source of the presentation timestamp | Flutter's analogue |
|---|---|---|
| Skia Android | `Choreographer.IFrameCallback.DoFrame(frameTimeNanos)` + `Display.RefreshRate` | `vsync_waiter_android.cc:64-81` |
| Skia Win32 | `DwmGetCompositionTimingInfo` → `qpcVBlank` + `qpcRefreshPeriod` (today only `DwmFlush` is used — `src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:11-36`) | — |
| Skia WASM | the `DOMHighResTimeStamp` argument of `requestAnimationFrame`, currently dropped at `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/BrowserRenderer.ts:47-51` | — |
| Skia macOS/iOS | `CADisplayLink.targetTimestamp` | `vsync_waiter_ios.mm:116-135` |

Note the rAF case is *exact*: per spec the `DOMHighResTimeStamp` passed to a `requestAnimationFrame`
callback is the time the frame's rendering begins and is identical for all callbacks in that frame —
which is precisely what Uno needs and precisely what the current code throws away.

### 3.2 Anchor `t = 0` to a frame timestamp, not to the pointer-up instant

Flutter: `_startTime ??= timeStamp` on the **first tick** (`ticker.dart:276`), so the first fling
frame renders `x(0)` = the drag's final position.

Uno: `_flingStartTimestamp = compositor.TimestampInTicks` at pointer-up
(`ScrollContentPresenter.Managed.cs:593`), which is an arbitrary instant inside the frame interval.
The first `OnFlingFrame` therefore sees `elapsed` = (pointer-up → next record), typically 5–25 ms,
and applies `v × elapsed` of displacement **in a single frame**. At 3000 px/s that is 15–75 px in
one frame, of a magnitude that depends on where in the frame the finger happened to lift.

That is a randomly-sized step at the exact moment the user's attention is on the handoff. It is the
single most likely candidate for "drag is glass smooth, then inertia starts and something is off".

Fix: set `_flingStartTimestamp` lazily on the first `OnFlingFrame` (`if (_flingStartTimestamp == 0)
_flingStartTimestamp = timestampInTicks;`), which makes the first inertia frame a no-op and every
subsequent step exactly one display interval.

### 3.3 Skip the frame rather than tick late, and never replay lost time

Flutter drops the whole Dart frame when the pipeline is full (`animator.cc:99-108`) and this is safe
*only because* the simulation is closed-form in absolute time. Uno's `ScrollFlingSimulation` is
already closed-form (`ScrollFlingSimulation.GetPosition(double t)` at
`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollFlingSimulation.cs:82`), so Uno has half
of this property already. What Uno lacks is the guarantee that the `t` it uses corresponds to the
frame that will be shown — see 3.1. **Do not** add a "catch-up" or dt-clamping mechanism; it would
undo the property.

### 3.4 Make the offset immutable for the duration of the frame

Flutter asserts the scroll offset cannot change during build/layout/paint
(`scroll_position.dart:366-371`) and pauses the Dart event loop's secondary sources during the frame
task (`vsync_waiter.cc:112-146`). Uno's `FrameStarting` already gives the right *phase*
(`Compositor.skia.cs:200-209` documents exactly this), but nothing prevents a pointer event
delivered mid-paint (the Android render loop runs on its own thread — `UnoSKVulkanView.cs:134-159`)
from writing `HorizontalOffset` while the paint walk reads it. **UNVERIFIED** whether that race is
reachable in practice given Uno's dispatcher model; worth an explicit check, because a mid-paint
offset write produces exactly the "one frame in twenty looks wrong" symptom and would be invisible
in a jerk metric averaged over a whole fling.

### 3.5 (Cheap, orthogonal) Flutter's resampling philosophy — the same time axis for input

Flutter has pointer resampling that interpolates touch samples to `presentationTime + samplingOffset`
with

```dart
// gestures/binding.dart:218-224
// Sampling offset is relative to presentation time. If we produce frames
// 16.667 ms before presentation and input rate is ~60hz, worst case latency
// is 33.334 ms. …
const Duration _defaultSamplingOffset = Duration(milliseconds: -38);
```

It is **off by default** (`gestures/binding.dart:610`, `bool resamplingEnabled = false`), so it is
*not* what makes stock Flutter drags smooth, and I would not port it now. Cite it only for the
principle it encodes: Flutter's frame timestamp is understood, framework-wide, as a *presentation*
time, and both the input path and the physics path are expressed on that one axis.

---

## 4. Q4 — is Flutter's fling smooth under a *changing* frame rate (120 → 60 Hz on finger-lift)?

Yes, and the reason is structural rather than adaptive. Four mechanisms, in order of importance.

### 4.1 Presentation-time parameterisation makes a cadence change self-compensating

This is the key one and it is worth doing the arithmetic, because a "record-time" implementation
gets it *wrong in a specific, visible way* at exactly this transition.

Let `V_k` be real vsync instants and `P` the display interval.

**Flutter.** Frame `k` is built in the vsync task starting at `V_k`, and evaluates the simulation at
`T_k = V_k + P_k` (`vsync_waiter_android.cc:71-72`). It presents at `V_k + P_k`. So
`presented_position(t) = x(t)` for every frame in both the 120 Hz and 60 Hz regimes. When `P` doubles
from 8.33 ms to 16.67 ms, `T_k` steps up by 8.33 ms on the transition frame — **and so does the
actual presentation instant**. The two changes cancel exactly. The motion stays on the analytic
curve; the only thing that changes is the sampling density along it.

**A record-time implementation (Uno today).** Frame `k` evaluates at `V_k + φ_k` where `φ_k` is the
CPU phase from vsync to paint-walk start, then presents at `V_k + P`. The displayed position is
`x(V_k + φ_k)` shown at `V_k + P`, i.e. an error of `v·(φ_k − P)`. Two problems:

* `φ_k` varies frame to frame → per-frame position noise of `v·Δφ`. Because the eye integrates
  *differences* between consecutive frames, the perceived per-frame step is
  `v·(P + φ_k − φ_{k−1})`. At 60 Hz with `φ` jitter of just ±2 ms, the step varies by up to
  ±4 ms / 16.67 ms = **±24 %** frame-to-frame, on a curve whose true frame-to-frame change over
  a whole second-long fling is a smooth few percent. That is a signal well above the visual
  jitter-detection threshold.
* At the 120 → 60 transition, `P` doubles but `φ_k` does not, so the constant part of the error
  jumps by ~8.3 ms of travel in one frame, and the *jitter amplitude* also grows because `φ` now
  has more room to move inside a longer interval.

Note the direction of the effect: this is not "Uno's fling is laggy". A constant lag is invisible.
It is "Uno's fling is *modulated*", which is exactly what the product owner is reporting.

### 4.2 The evaluation instants are hardware vsync timestamps, so their spacing is truthful

Whatever the refresh rate is, `frameTimeNanos` / `link.timestamp` are the display's own instants.
The sequence `T_k` therefore has exactly the spacing of the real frames — including the irregular
spacing of a variable-refresh panel. A closed-form `x(T_k)` sampled at truthful instants produces a
sequence of positions that is *correct by construction* at every one of them. Under a `Stopwatch`
clock the spacings are truthful only about the *clock*, not about the display.

### 4.3 The refresh period tracks the mode change

* Android NDK path: `target = frame_time + 1e9 / g_refresh_rate_` where `g_refresh_rate_` is a global
  updated by `VsyncWaiterAndroid::OnUpdateRefreshRate` (`vsync_waiter_android.cc:117-122`), fed from
  Java:

  ```java
  // shell/platform/android/io/flutter/view/VsyncWaiter.java:33-40
  public void onDisplayChanged(int displayId) {
    if (displayId == Display.DEFAULT_DISPLAY) {
      float fps = primaryDisplay.getRefreshRate();
      VsyncWaiter.this.refreshPeriodNanos = (long) (1000000000.0 / fps);
      VsyncWaiter.this.flutterJNI.setRefreshRateFPS(fps);
    }
  }
  ```

  Because this is a `DisplayManager.DisplayListener` callback it can lag the actual mode switch by a
  frame or two; during that window `target_time` is off by up to one interval. **The base
  `frame_time` is still exact**, so the artifact is a single ≤8.3 ms offset step, not ongoing jitter.
  I did not find any smoothing or hysteresis around this — **UNVERIFIED** whether it is perceptible;
  I would expect not, at ~25 px at 3000 px/s spread over one frame, and it self-corrects.
* Apple path has no such lag at all: `target − start` is recomputed **per frame** from
  `link.targetTimestamp − link.timestamp` (`vsync_waiter_ios.mm:120-121`), which is CoreAnimation's
  own per-frame prediction and therefore tracks ProMotion's variable cadence exactly. Flutter even
  derives `_refreshRate` *from* it rather than the other way round (`:130`).

### 4.4 Nothing in the fling path is rate-dependent

Worth stating because it is easy to get wrong when porting: there is no per-frame decay factor, no
`v *= k` step, no fixed-step integrator anywhere in the chain.
`AnimationController._tick` (`animation_controller.dart:941-955`) does one closed-form evaluation;
`FrictionSimulation.x/dx` are analytic (`physics/friction_simulation.dart:118-134`);
`ClampingScrollSimulation.x/dx` are analytic (`widgets/scroll_simulation.dart:250-265`);
`SpringSimulation` picks one of three closed-form solutions (`physics/spring_simulation.dart:285-301`).
The `Simulation` contract explicitly permits statelessness and only requires monotonically
non-decreasing query times (`physics/simulation.dart:23-31`) — which the monotonic clamp at
`platform_configuration.cc:472-481` guarantees.

Uno's `ScrollFlingSimulation` already satisfies this. The gap is the clock, not the curve.

---

## 5. Why this explains the ASYMMETRY (drag fine, inertia not)

The rule for this investigation is that "frames are irregular" is not an explanation, because drag
would suffer equally. Here is why it does not — stated as a code property, not a story.

**Drag's per-frame value is a function of the finger. Inertia's per-frame value is a function of the
clock.**

Uno's drag path never reads a clock:

```csharp
// ScrollContentPresenter.Managed.cs:795-868 (IDirectManipulationHandler.OnUpdated)
var deltaX = Math.Clamp(-unhandledDelta.Translation.X, scrollable.Left, scrollable.Right);
var deltaY = Math.Clamp(-unhandledDelta.Translation.Y, scrollable.Up, scrollable.Down);
…
Set(horizontalOffset: HorizontalOffset + deltaX, verticalOffset: VerticalOffset + deltaY,
    options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: true));   // :857, :868
```

The offset is `Σ finger deltas`. It is written from the pointer-event dispatch, and the frame simply
records whatever value is current. Now perturb the record phase by `Δφ`:

* **Drag:** the displayed value changes only in that a slightly newer or older *finger sample* is
  included. The mapping finger→content is unchanged and exact. The error is a pure latency against
  the finger — and the finger is the user's own reference, moving with its own biological jitter,
  which masks a few milliseconds of variation completely. Crucially the error **cannot accumulate**
  and **cannot become a velocity error**, because the value is not derived from elapsed time at all.
* **Inertia:** `elapsed = timestampInTicks − _flingStartTimestamp`
  (`ScrollContentPresenter.Managed.cs:617`) puts `Δφ` *directly into the position*, as `v·Δφ`. The
  frame-to-frame step becomes `v·(P + φ_k − φ_{k−1})`, i.e. the record-phase jitter is
  differentiated into an **apparent velocity modulation**. And unlike drag, there is no external
  reference to mask it: the trajectory is mathematically smooth, so the visual system has a strong
  internal prediction and any deviation is 100 % of the visible noise rather than a small addition
  to the hand's own noise.

So the asymmetry is not "inertia has worse frames". It is:

> **Drag samples an external signal — jitter in *when* you sample only costs latency.
> Inertia samples time itself — jitter in *when* you sample is converted 1:1 into position error,
> and then differentiated by the eye into velocity error.**

Flutter removes the term entirely by making the sampled time *be* the presentation time
(`platform_configuration.cc:469-471`), so `φ ≡ P` by definition and `Δφ ≡ 0` no matter how long the
build took. That is why Flutter can afford to run a full sliver re-layout on the UI thread on every
fling frame (`rendering/viewport.dart:685-695`, `:1692-1765`) and still feel right: a heavy frame
moves *when* the work happens, not *what* is drawn.

Secondary contributor, same root, worth fixing at the same time: the drag→fling handoff step of
random size described in §3.2.

---

## 6. Concrete adoption plan (ordered, cheapest first)

1. **Lazy fling anchor** — 2 lines in `ScrollContentPresenter.Managed.cs` (§3.2). No new API, no
   platform work. Removes the random-sized first inertia step. Do this first because it is
   independently correct even if step 2 is deferred.
2. **Presentation timestamp plumbing** (§3.1):
   - `ChoreographerFramePacer` captures `frameTimeNanos` and the current `Display.RefreshRate`;
   - `Compositor` gains `CurrentFramePresentationTimestampInTicks`, set from the platform value when
     available and from `TimestampInTicks + refreshPeriod` otherwise;
   - `FrameStarting` is raised with the **presentation** timestamp;
   - `OnFlingFrame` and `OnWheelDecayFrame` (`ScrollContentPresenter.Managed.cs:615`, `:666`) need
     no change beyond receiving the better number.
3. **Win32 / WASM sources** — `DwmGetCompositionTimingInfo` and the rAF argument (§3.1 table).
4. **Verify the mid-paint offset-write race** on Android (§3.4).

### Smallest measurement that confirms or refutes the diagnosis before any of it

Log, for ~120 consecutive fling frames on Android: `frameTimeNanos` (from `DoFrame`), the
`TimestampInTicks` value sampled in `RenderRootVisual`, and the emitted offset. Then compute
`φ_k = record − vsync` and its standard deviation.

* If `σ(φ)` is ≳ 1 ms while the vsync intervals themselves are tight, the diagnosis holds and the
  predicted position noise is `v·σ(φ)` — compare it against the measured jerk.
* If `σ(φ)` is ≪ 1 ms, this hypothesis is refuted and the remaining suspects are the presentation
  path (MAILBOX queue depth) rather than the evaluation clock.

This is a pure instrumentation change and costs nothing to revert.

---

## 7. Explicitly UNVERIFIED

1. That `Choreographer.frameTimeNanos` and `Stopwatch.GetTimestamp()` share a timebase under the
   .NET Android runtime. Flutter sidesteps the question by reconstructing from a measured delay
   (`vsync_waiter_android.cc:89-92`); Uno should do the same or calibrate once.
2. Whether the one-frame staleness of Android's `g_refresh_rate_` at a mode switch
   (`vsync_waiter_android.cc:117-122`, `VsyncWaiter.java:33-40`) is perceptible. Argued not, not
   measured.
3. Whether a pointer event can write `HorizontalOffset`/`VerticalOffset` while Uno's Android render
   thread is inside the paint walk (§3.4). Flutter structurally cannot
   (`scroll_position.dart:366-371`, `vsync_waiter.cc:112-146`); Uno's dispatcher model was not
   traced far enough here to say.
4. The exact position of `FrameStarting` relative to vsync on Win32 and WASM. Only the Android
   render loop was read (`UnoSKVulkanView.cs:143-159`).
5. Perceptual thresholds cited in §5 are general motion-perception reasoning, not measured on this
   app. The arithmetic (`v·Δφ`, ±24 % step variation) is derived from the code paths cited and is
   sound; the claim that it is *the* perceptible defect is what step 6's measurement tests.
