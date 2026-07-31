# Record-to-present latency, and why it hurts inertia but not drag

Scope: characterise when a recorded picture is actually presented on Skia in general and on Android
Vulkan in particular, establish whether that delay is constant, explain the drag/inertia asymmetry in
terms of a mechanism visible in code, quantify the tolerance, and enumerate what a predicted
presentation timestamp costs on each platform.

Worktree `dev/mazi/smooth-scroll`. Every line reference is to code in this worktree. Claims that
depend on OS behaviour outside this repo are marked **UNVERIFIED**.

---

## 0. Answer up front

1. **Record-to-present latency is variable, not constant.** It contains a modulo term
   (`next vsync − record time`) whose value sweeps the whole refresh period as the record's phase
   drifts, plus a discrete term (present vs. dropped vs. duplicated frame).
2. **But variable latency is the second-order problem.** The first-order problem, and the one that
   explains the asymmetry, is that the fling's *sample time* is a wall-clock read taken at record
   time — and record time is not paced to anything.
3. **The asymmetry has a one-line statement:** `OnFlingFrame` computes the position *from a clock*
   at record time (`ScrollContentPresenter.Managed.cs:615-617`); the drag path never reads a clock —
   it writes a position derived from a pointer sample at *input* time
   (`ScrollContentPresenter.Managed.cs:864-868`). Record-time jitter therefore turns into a position
   error of `v · δ` for inertia, and into nothing at all for drag.
4. **The tolerance is well under a millisecond.** At a typical launch velocity (~2650 logical px/s,
   derived in §5) a 1 ms sampling-phase error is a 2.65 px positional error, which is above the
   dynamic displacement-detection threshold at phone viewing distance. A single misassigned frame
   slot is a 44 px error.
5. **The fix shape:** evaluate frame drivers against the *predicted presentation timestamp*, not
   `Stopwatch.GetTimestamp()`. All four platforms already hand Uno that timestamp and Uno currently
   throws it away in four separate places (§6).

---

## 1. The chain, stage by stage

### 1.1 What triggers a record

Two distinct entry points reach `CompositionTarget.Render()`, and they have completely different
timing characteristics. This is central to everything below.

**Path A — render-thread-driven (the intended one).**

```
render thread: OnNativePlatformFrameRequested            RenderScheduling.skia.cs:166
  └─ NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)   :172   [High priority]
  └─ Draw(canvas, resizeFunc)   presents the PREVIOUS picture           :175
UI thread:   EnqueueRenderCallback                                       :120-157
  └─ Render()                                                           :152
```

**Path B — layout-driven, "ahead of time".**

```
anything that calls XamlRoot.InvalidateMeasure/Arrange, or
EventManager.EnqueueForEffectiveViewportChanged                          EventManager.cs:34
  └─ CoreServices.RequestAdditionalFrame()                               CoreServices.cs:67-75
      └─ NativeDispatcher.Main.Enqueue(OnTick, Normal)                   CoreServices.cs:73
UI thread:  OnTick → root.UpdateLayout()                                 CoreServices.cs:114
  └─ CompositionTarget.OnRenderFrameOpportunity()                        CoreServices.cs:124
      └─ Render()                                                        RenderScheduling.skia.cs:205
```

Path B is gated only by `RenderRequested && !_renderedAheadOfTime`
(`RenderScheduling.skia.cs:192-197`). During a fling `RenderRequested` is permanently true, because
`Compositor.RenderRootVisual` re-requests a frame at the end of every record whenever
`FrameStarting is not null` (`Compositor.skia.cs:291-294`). So Path B is *armed on every frame of a
fling*.

And a fling arms Path B itself, every frame:

```
OnFlingFrame → Set(...)                                 ScrollContentPresenter.Managed.cs:634
  → Update(...) → visual.AnchorPoint = target           :527
  → Updated(...) → UpdateOffsets → InvalidateViewport() :469
  → PropagateEffectiveViewportChange()                  FrameworkElement.EffectiveViewport.cs:265
  → EventManager.EnqueueForEffectiveViewportChanged     FrameworkElement.EffectiveViewport.cs:384
  → CoreServices.RequestAdditionalFrame()               EventManager.cs:34
```

The fling closes a loop through a **Normal-priority dispatcher item** back into the record.

### 1.2 What happens inside a record

```
Render()                                   Rendering.skia.cs:110   (UI thread, asserted at :114)
  RecordPictureAndReturnPath               :119-124
    └─ Compositor.RenderRootVisual         Compositor.skia.cs:219
         var frameTimestamp = TimestampInTicks;        :230   ← the fling's sample time
         CurrentFrameTimestampInTicks = frameTimestamp; :231
         frameStarting(frameTimestamp);                :234   ← OnFlingFrame runs HERE
         rootVisual.RenderRootVisual(...)              :270   ← paint walk, produces the SKPicture
  _lastRenderedFrame = (framePicture, path, damage)    Rendering.skia.cs:147   (under _frameGate)
  host.InvalidateRender()                              Rendering.skia.cs:171
```

`TimestampInTicks` is `Stopwatch.GetTimestamp()` scaled (`Compositor.cs:38`) — a raw monotonic clock
read, with no relationship to any display timing.

Note the ordering: **`OnFlingFrame` writes `AnchorPoint` at `:234`, before the paint walk at `:270`.**
That is what fix #3 bought — the write lands in *this* frame instead of the next. Correct, and
necessary, but it only removed a constant one-frame lag. Constant lag is invisible. What it did not
change is that the sample time is still "whenever the UI thread happened to start this record".

### 1.3 What happens between record and present

```
_lastRenderedFrame  ──(borrowed under _frameGate)──▶ Draw()   Rendering.skia.cs:232-246
                                                       └─ SkiaRenderHelper.RenderPicture :295-299
                    ◀──(ReturnFrame, put back if not superseded)  :412-434
```

Two consequences fall straight out of this slot design:

- **Duplicate present.** If `Draw` runs twice with no intervening `Render`, `ReturnFrame` (`:419-422`)
  puts the same frame back and it is replayed a second time. The content does not move for one
  refresh.
- **Dropped record.** If `Render` runs twice before a `Draw`, the slot is overwritten at `:147` and
  the older picture is released at `:159-162`. One evaluated fling position is computed and never
  shown.

Neither is a bug in the slot; it is the correct behaviour for a decoupled record/present pipeline.
But both are *position errors* for a clock-driven driver and *no-ops* for a position-driven one.

### 1.4 Android Vulkan specifically

```
UnoSKVulkanView.InvalidateRender()                        UnoSKVulkanView.cs:60-65
  _renderRequested = true; _renderEvent.Set();

RenderLoop (UnoVulkanRenderThread)                        UnoSKVulkanView.cs:134-168
  while (...)
  {
      _renderEvent.Wait(100 ms);                          :146
      _renderEvent.Reset();                               :147
      if (!_renderRequested) continue;                    :149-150
      RenderFrame();                                      :153
      _pacer.WaitForNextFrame();                          :158   ← blocks until Choreographer vsync
  }

RenderFrame()                                             :199-227
  _vulkanContext.RenderFrame(skSurface => {
      compositionTarget.OnNativePlatformFrameRequested(...)  :212
  });                                                     → GPU submit + vkQueuePresentKHR (MAILBOX)
```

`ChoreographerFramePacer.WaitForNextFrame` posts a frame callback onto its own Looper thread and
blocks on an `AutoResetEvent` (`ChoreographerFramePacer.cs:66-76`), with a 100 ms escape hatch
(`:30`).

So the render thread's period is: present frame N → block until vsync V → loop → observe the latched
`_renderEvent` → present frame N+1. Presents therefore occur at a fixed offset *after* a vsync, one
per vsync, as long as a picture is available. **The present side is well paced.** The record side is
not.

`VK_PRESENT_MODE_MAILBOX_KHR` (documented in `ChoreographerFramePacer.cs:16-19`) means
`vkQueuePresentKHR` returns immediately and SurfaceFlinger latches the newest queued image at *its*
next vsync — adding one further, and occasionally two, refresh periods before photons. **UNVERIFIED
in-repo** (SurfaceFlinger latch depth is an OS property), but it is a constant-ish additive term and
therefore not the interesting one.

Full Android chain, with what determines each term:

| Term | Determined by | Constant? |
|---|---|---|
| `t_r` → picture complete (`R`) | paint walk cost; grows with realized-item count | **No** — varies with content |
| picture complete → render thread observes | `_renderEvent` is already latched; the thread is blocked in `_pacer.WaitForNextFrame()` | **No** — this is `(next vsync − t_r − R) mod T` |
| render-thread raster + submit | GPU load, damage region size | roughly constant |
| `vkQueuePresentKHR` → SF latch | MAILBOX + SurfaceFlinger | +1 vsync typical, occasionally +2 |

### 1.5 Win32, iOS, WASM for comparison

| Target | Present pacing | Cite |
|---|---|---|
| Win32 | dedicated render thread, `DwmFlush()` after present; degrades to a timer after 3 failures | `Win32WindowWrapper.RenderThread.cs:52-90`, `Win32RenderPacer.cs:53-89`, `…Rendering.Vulkan.cs:117,131` |
| iOS | `CADisplayLink` drives `Draw()` | `UnoSKMetalView.cs:37` |
| WASM | `requestAnimationFrame` drives `requestRender` | `ts/Runtime/BrowserRenderer.ts:48-50` |
| Android | Choreographer pacer after present | `UnoSKVulkanView.cs:158`, `ChoreographerFramePacer.cs:66-76` |

All four present sides are vsync-derived. In every case the *record* is a separate dispatcher item on
the UI thread, so the structural problem is identical across platforms; only the magnitude of the
jitter differs.

---

## 2. Is the record-to-present latency constant? No — three variable terms

### Term 1: UI-thread scheduling delay before the record runs (`δ_sched`)

Path A posts the record as a High-priority render action. It is *not* dequeued ahead of everything
else. `NativeDispatcher.TryGetRenderAction` (`NativeDispatcher.cs:206-234`) only hands the render
action over when `normalItemsToProcessBeforeNextRenderAction == 0`, and every time it *does* hand
one over it re-arms that counter to the current Normal-queue depth:

```csharp
_compositionTargets[compositionTarget] =
    (renderAction: null,
     normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
                                                                        NativeDispatcher.cs:216
```

So the next record is deliberately held behind N Normal-priority items, where N is whatever was
queued at the moment of the previous record. During a fling the Normal queue is *not* empty — the
fling puts `OnTick` there itself (§1.1), plus `ScrollViewer.OnPresenterScrolled`'s deferred DP writes
and any virtualization continuation. `δ_sched` is therefore "the time to run N arbitrary work items",
which is content-dependent and varies frame to frame.

On Android there is an extra, platform-specific contributor: `EnqueueNative` is a plain
`_handler.Post(_implementor)` onto the main Looper (`NativeDispatcher.Android.cs:40-43`), which
places a *synchronous* message on `MessageQueue`. Android's `ViewRootImpl` installs a **sync
barrier** while a traversal is pending, which holds synchronous messages until the barrier is lifted
after the Choreographer traversal callback. Uno's record message is therefore periodically parked
behind the platform's own frame work, at a phase Uno does not control. **UNVERIFIED in-repo** —
this is documented Android `MessageQueue`/`Choreographer` behaviour, not something this worktree can
prove — but it matches the note already recorded at `research/14-orchestrator-firsthand.md:199-202`.

### Term 2: Path B firing at an arbitrary phase (`δ_phase`)

When `OnTick` runs (Normal priority, posted by the fling's own viewport invalidation), it calls
`OnRenderFrameOpportunity` → `Render()` (`CoreServices.cs:124` → `RenderScheduling.skia.cs:205`).
That record samples the fling curve at the moment `OnTick` happened to run — which is a function of
layout cost and queue depth, and has no vsync relationship whatsoever.

Worse, the two paths *interleave*. After a Path B record, `_renderedAheadOfTime = true`
(`RenderScheduling.skia.cs:195`), so the next Path A `EnqueueRenderCallback` **skips its render**
entirely (`:131-144`) and just clears the flag. The effective sequence alternates between
"sampled at OnTick time" and "sampled at render-callback time", i.e. between two different phases.
An alternating short/long sampling interval at a constant present rate is precisely a periodic
step-size modulation — the visual signature of a beat/judder.

### Term 3: The modulo term (`next vsync − record completion`)

Once the picture is in the slot, it waits for the render thread, which is parked on the pacer. The
wait is `(V_next − (t_r + R)) mod T`. Because `t_r` is not vsync-locked (Terms 1 and 2), its phase
relative to `V` drifts continuously, so this term sweeps `[0, T)` and **jumps discontinuously by a
full `T` = 16.67 ms** every time the phase crosses a vsync boundary. That crossing is exactly the
"duplicate present" / "dropped record" event of §1.3.

### Summary

```
L(n) = δ_sched(n) + δ_phase(n) + R(n) + ((V − t_r(n) − R(n)) mod T) + raster + SF_latch
```

Only the last two terms are approximately constant. **A constant `L` would be harmless** — it is
just latency, and the eye cannot see absolute latency in an unreferenced motion. What matters is
`L(n) − L(n−1)`, and more precisely the sampling interval `t_r(n) − t_r(n−1)` versus the presentation
interval `T`.

---

## 3. The asymmetry, stated as a mechanism

This is the part the task asks to be decisive about. "Frames are irregular" is not an answer, because
frames are equally irregular during a drag.

### Drag: the value is produced at input time, and merely *read* at record time

```
Activity.DispatchTouchEvent(ev)                          ApplicationActivity.cs:187-211
  → AndroidCorePointerInputSource.OnNativeMotionEvent    :207
  → ToManaged: position = (nativeArgs.GetX(i), GetY(i))  AndroidCorePointerInputSource.cs:226-229
  → PointerMoved → … → GestureRecognizer
  → IDirectManipulationHandler.OnUpdated                 ScrollContentPresenter.Managed.cs:795
      deltaX = clamp(-unhandledDelta.Translation.X, …)   :803-804
      Set(HorizontalOffset + deltaX, VerticalOffset + deltaY, …)   :864-868
  → Update → visual.AnchorPoint = target                 :527
```

**Not one clock read anywhere in that chain.** `AndroidCorePointerInputSource.ToManaged` does read
`EventTimeNanos` (`:210-221`) and puts it on the `PointerPoint`, but the *position* is taken straight
from the MotionEvent and the timestamp is never used to compute it.

Consequences:

- The recorded value is `p_finger(t_input)`, where `t_input` is set by the OS input pipeline.
- Record-time jitter cannot change that value. It can only change *how many* input-derived values
  land in a given picture: zero (the picture is a duplicate) or two (one sample is skipped).
- The error is **bounded and self-correcting**: every input sample re-anchors the content to where
  the finger actually is. It never accumulates and it never scales with velocity.

On Android there is a second, stronger reason drag looks clean: `Activity.DispatchTouchEvent` sits on
the `ViewRootImpl` input stage, which consumes batched touch input *at the Choreographer frame* and
resamples it to a fixed offset before the frame time. The positions Uno receives are therefore
already an evenly-spaced sampling of the finger trajectory at the display cadence, regardless of when
Uno's UI thread does anything. **UNVERIFIED in-repo** — this is Android platform behaviour
(`InputConsumer` / `LegacyResampler`, `RESAMPLE_LATENCY`), observable only on-device.

Corroborating datum from the same code: drag positions are truncated to whole **physical** pixels
before the DIP conversion (`AndroidCorePointerInputSource.cs:229`, `(int)x`, `(int)y`) — and drag is
nevertheless reported glass-smooth. Value precision is evidently not what the eye is complaining
about; interval regularity is.

### Inertia: the value *is* a function of the record clock

```csharp
private void OnFlingFrame(long timestampInTicks)
{
    var elapsed = (timestampInTicks - _flingStartTimestamp) / (double)TimeSpan.TicksPerSecond;
    ...
    var v = Math.Clamp(_flingV.GetPosition(elapsed), 0, maxV);
    Set(horizontalOffset: h, verticalOffset: v, …);
}                                          ScrollContentPresenter.Managed.cs:615-635
```

`timestampInTicks` is `Compositor.TimestampInTicks` read at `Compositor.skia.cs:230`, i.e. at the
start of the record. Therefore:

```
displayed_n = f(t_r(n))          shown at   V(n)
```

The step the eye sees between consecutive presented frames is

```
Δx_n = f(t_r(n)) − f(t_r(n−1)) ≈ v · (t_r(n) − t_r(n−1))
```

but it is shown over a fixed interval `T`. **Every millisecond of variation in the record's
scheduling becomes `v` pixels of step-size error.** Nothing anchors it back: unlike drag, there is no
external reference the position converges to.

### The one-sentence form

> Drag is a **position-driven** motion sampled by the frame; inertia is a **time-driven** motion
> whose clock is read at a moment the frame pipeline does not control. Jitter in that moment is
> invisible for the former and multiplies by velocity for the latter.

This also explains cleanly why fixes #1–#6 did not close it. Every one of them improved *what value*
is computed (quantization, curve shape, launch velocity, tick position within the frame) or *how
regularly frames are presented*. None of them changed *which instant the curve is evaluated at*. The
remaining defect is entirely in the argument to `f`.

### Where the wheel decay sits

`OnWheelDecayFrame` (`ScrollContentPresenter.Managed.cs:691-709`) has the identical structure and the
identical exposure. Jerk improved from 0.289 to 0.171 but did not go to zero, which is consistent
with a residual sampling-phase term rather than a curve-shape term.

---

## 4. Would a variable latency alone be visible during a fling but not a drag?

Yes, and it is worth separating the two effects because they call for the same fix but have different
magnitudes.

**Effect A — sampling-interval error (dominant).** As derived above: `Δx_n − vT = v·(δ_n − δ_{n−1})`.
This is a *per-frame* error, refreshed 60–120 times a second, i.e. a high-frequency positional noise
riding on the motion. The visual system is exquisitely sensitive to this during smooth pursuit
because the retinal image of a tracked feature should be stationary, and this term makes it vibrate.

**Effect B — latency variation (secondary).** `f(t_r)` is presented at `V`, so the content is stale by
`L = V − t_r`. If `L` were constant, the whole fling would simply be shifted in time by `L` — utterly
invisible. When `L` jumps by a full refresh period (the modulo term crossing a boundary), the content
lurches by `v·T` relative to where it "should" be. That is a single-frame hitch, perceptually a
discrete pop rather than a vibration.

For drag both effects are structurally absent:

- Effect A: `displayed_n` is not a function of `t_r`, so `δ` cancels out entirely.
- Effect B: staleness during a drag is finger-to-photon latency, which the user compensates for
  automatically (the same way a mouse cursor with 40 ms lag still feels continuous). And because the
  content is glued to a visible physical reference, a *constant* offset reads as lag, not as
  roughness.

The only drag artefact that survives is "a frame carried no new input sample" — and on Android the
OS's per-frame input resampling makes that rare by construction (**UNVERIFIED**, §3).

---

## 5. How much variance is perceptible? A budget

### Typical fling velocity

Fix #5 records that a 264 px flick now travels 1531 px. Inverting `ScrollFlingSimulation`'s Android
form (`ScrollFlingSimulation.cs:62-73`):

```
referenceVelocity = Friction · PhysicalCoefficient / Inflexion
                  = 0.015 · (9.80665 · 39.37 · 96 · 0.84) / 0.35 ≈ 1334 px/s
distance          = 0.35 · v · (v / 1334)^0.7363
1531              = 0.35 · v · (v / 1334)^0.7363   ⇒   v₀ ≈ 2650 logical px/s
```

Duration ≈ 1.37 s, so a launch-speed frame step is:

| Refresh | Step at v₀ = 2650 px/s |
|---|---|
| 60 Hz | 44.2 px |
| 90 Hz | 29.4 px |
| 120 Hz | 22.1 px |

### Angular size of the error

A WinUI DIP is 1/96 in = 0.2646 mm by definition. At a 30 cm phone viewing distance:

```
1 logical px ⇒ atan(0.2646 / 300) = 8.8·10⁻⁴ rad = 3.03 arcmin
```

Displacement-detection thresholds for a high-contrast edge under smooth pursuit are on the order of
**1–3 arcmin** (dynamic vernier acuity; degrades with tracking speed). So the perceptual threshold
for a single frame's misplacement is roughly **0.3 – 1.0 logical px** — and repeated, periodic
modulation is detected *below* single-event threshold because the visual system integrates it.

### The budget

`δ_tolerance = threshold / v`:

| Velocity (px/s) | 0.5 px error | 1.0 px error |
|---|---|---|
| 2650 (launch) | **0.19 ms** | 0.38 ms |
| 1500 | 0.33 ms | 0.67 ms |
| 1000 | 0.50 ms | 1.0 ms |
| 500 (tail) | 1.0 ms | 2.0 ms |

Cross-check against the frame-pacing literature: micro-stutter is generally reported as visible when
frame-time inconsistency exceeds ~5–10 % of the period, i.e. **0.8 – 1.7 ms at 60 Hz** — the same
order of magnitude, arrived at independently.

**Budget: σ(t_r interval) must be well under 1 ms, call it 0.5 ms, to be inaudible at launch speed.**

For scale, the errors the current pipeline can produce:

| Event | Sampling-time error | Position error at v₀ |
|---|---|---|
| One extra Normal-priority work item ahead of the record | 1–5 ms | 2.6 – 13 px |
| Path A / Path B phase alternation | up to ~T/2 ≈ 8 ms | up to 22 px |
| A duplicate present or dropped record | 16.67 ms | **44 px** |

44 px is roughly 5 % of a phone viewport height in a single frame. That is not a subtle artefact, and
its rate depends on load — which matches "sometimes it's fine, sometimes it isn't".

### The important corollary

Making the *record* perfectly regular is neither necessary nor sufficient. If `OnFlingFrame` were
handed the predicted presentation instant `V̂(n)`, then `displayed_n = f(V̂(n))` and the samples are
evenly spaced **even if the record itself jitters by 8 ms**, because `V̂` is derived from the display
clock, not the CPU's arrival time. A systematic bias in `V̂` (predicting 1 vs 2 vsyncs ahead) is a
constant latency and is invisible. That is a much cheaper and far more robust target than trying to
make the UI thread deterministic.

---

## 6. Obtaining a predicted presentation timestamp

The striking finding: **all four platforms already hand Uno the timestamp, in code Uno already runs,
and Uno discards it in all four places.**

### Android — `Choreographer` frame time (already wired, one line from being usable)

```csharp
private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
{
    public void DoFrame(long frameTimeNanos) => onFrame();   // ChoreographerFramePacer.cs:99
}
```

`frameTimeNanos` is the vsync time in `System.nanoTime` base. Options, cheapest first:

1. **`frameTimeNanos + k·T`.** Capture it in the pacer, publish it, and have the compositor use
   `V̂ = frameTimeNanos + k · refreshPeriod`. `T` from `Display.getRefreshRate()` (or
   `Display.getMode()`), `k` determined empirically for this pipeline — likely **2**, because the
   record happens on the UI thread *after* vsync N, is presented by the render thread at N+1, and
   MAILBOX-latched by SurfaceFlinger for N+2. `k` must be measured, not assumed.
2. **API 34+ `Choreographer.postVsyncCallback`** → `FrameData.getFrameTimelines()` →
   `FrameTimeline.getExpectedPresentationTimeNanos()`. This is the OS's own prediction, per timeline,
   and removes the need to guess `k`. Requires an API-level branch; `Build.VERSION.SdkInt` is already
   checked elsewhere in this codebase (`AndroidCorePointerInputSource.cs:210`).
3. `Display.getPresentationDeadlineNanos()` as a refinement of option 1.

Timebase caveat: `.NET Stopwatch` on Android and Java `System.nanoTime` are both expected to be
`CLOCK_MONOTONIC`, so no conversion should be needed — **UNVERIFIED**, and it must be checked before
anything is built on it, because a silently different epoch would produce a large constant offset
that looks like a working fix until the fling curve is inspected.

Structural note: the pacer's Looper is a *separate* thread from the render thread
(`ChoreographerFramePacer.cs:41-45`), so the value must be published across threads (a `volatile
long` set in `DoFrame`, read by whoever needs it) and, ultimately, made visible to the UI thread that
runs the record.

### Win32 — `DwmGetCompositionTimingInfo`

`Win32RenderPacer.WaitForNextFrame` calls `PInvoke.DwmFlush()` (`Win32RenderPacer.cs:61`), which
blocks until the vblank but returns no timestamp. Calling `DwmGetCompositionTimingInfo` immediately
after yields `DWM_TIMING_INFO.qpcVBlank` (the QPC of the last vblank), `qpcRefreshPeriod`, and
`cRefresh`. Then `V̂ = qpcVBlank + k · qpcRefreshPeriod`.

**Timebase is exact, with no conversion:** `Stopwatch.GetTimestamp()` on Windows *is*
`QueryPerformanceCounter`, and `qpcVBlank` is in the same units. This is the cleanest platform to
prototype on. The Win32 host already owns a dedicated render thread
(`Win32WindowWrapper.RenderThread.cs:52-90`), so it is also the platform where the record/present
split is most pronounced.

Note also `Win32RenderPacer` can permanently degrade to a timer after 3 `DwmFlush` failures
(`:66-77`); the predicted-present source must degrade with it rather than silently returning stale
vblank times.

### iOS / macOS — `CADisplayLink.TargetTimestamp`

```csharp
_link = CADisplayLink.Create(() => this.Draw());     // UnoSKMetalView.cs:37
```

`CADisplayLink` exposes `Timestamp` (when the previous frame was displayed) and **`TargetTimestamp`**
(when the next frame is expected to be displayed) — literally the predicted presentation time, no
arithmetic required, no `k` to guess. It is the cheapest of the four; the closure just has to read
`_link.TargetTimestamp` and publish it before calling `Draw()`.

Timebase: `CADisplayLink` timestamps are `CACurrentMediaTime()` seconds (mach absolute time);
.NET `Stopwatch` on Apple platforms is also mach-based. Unit conversion is needed (seconds → ticks);
epoch equivalence is **UNVERIFIED**.

Caveat: the record does not happen inside `Draw()`, it happens on the next dispatcher turn
(`RenderScheduling.skia.cs:172`), so `TargetTimestamp` captured at frame N is the prediction for
frame N — the record that follows it will be presented at N+1 or later. Same `k` question as Android,
but with an exact base to add to.

### WASM — `requestAnimationFrame` timestamp

```ts
static invalidate(instance: BrowserRenderer) {
    window.requestAnimationFrame(() => {          // ts/Runtime/BrowserRenderer.ts:48
        instance.requestRender();
    });
}
```

The rAF callback receives a `DOMHighResTimeStamp` argument, discarded here. Per spec it is the frame's
*rendering start* time, not a presentation time; there is no standard predicted-present API on the
web platform (`VideoFrameCallbackMetadata.expectedDisplayTime` exists but is video-only). So WASM has
to use `V̂ = rafTimestamp + k · T`, with `T` estimated from the running median of rAF deltas.

Timebase: rAF timestamps are `performance.now()` milliseconds since the time origin. Whether .NET's
`Stopwatch.GetTimestamp()` in the browser shares that origin is **UNVERIFIED** and must be checked;
if not, the rAF timestamp has to be plumbed through as its own clock and the fling's start timestamp
captured in the same clock.

WASM is also the platform where this matters most and helps least: it is single-threaded, so record
and raster serialise (see `research/14-orchestrator-firsthand.md:297-353`), and a predicted-present
timestamp does not buy back the throughput. It does still fix the *evenness*, which is the complaint.

### Linux (X11 / Wayland)

Not examined in this pass. Wayland's `presentation-time` protocol and GLX/EGL's
`OML_sync_control` / `EGL_ANDROID_presentation_time` are the equivalents. Out of scope here.

### Where the timestamp would have to land

Regardless of platform, the value must reach `Compositor.RenderRootVisual` and be used in place of
`TimestampInTicks` at `Compositor.skia.cs:230`:

```csharp
var frameTimestamp = TimestampInTicks;              // ← today
CurrentFrameTimestampInTicks = frameTimestamp;
frameStarting(frameTimestamp);
```

Because `CurrentFrameTimestampInTicks` is already the published per-frame time
(`Compositor.skia.cs:214`, read by `ScrollDiagnostics` via
`ScrollContentPresenter.Managed.cs:189`), substituting a predicted-present value there propagates to
every frame driver — fling, wheel decay, and `KeyFrameEvaluator` — with no per-driver change. That is
the correct seam.

One design constraint to respect: the value must be **monotonically non-decreasing and never
repeat within a fling**. If two records land in the same predicted-present slot, the second must
either be skipped or advanced, otherwise the fling stalls for a frame — trading a jitter artefact for
a freeze artefact.

---

## 7. Smallest proof

### 7.1 Zero-code-change measurement (do this first)

`ScrollDiagnostics` already records, for every frame, `FrameUs = Compositor.CurrentFrameTimestampInTicks / 10`
(`ScrollDiagnostics.cs:98`, fed from `ScrollContentPresenter.Managed.cs:189`), tagged with the phase
(`PhaseDrag = 1`, `PhaseInertia = 2`). So an existing capture already contains the fling's sampling
timestamps.

From one capture on Android with a drag immediately followed by a fling, compute:

1. `σ(Δ FrameUs)` during phase 2 vs. the nominal refresh period. **Prediction: several ms, with
   occasional excursions to ≥ 1 full period.** If it is under ~0.5 ms, this whole hypothesis is
   wrong and the cause is elsewhere.
2. The bimodality of `Δ FrameUs` in phase 2. **Prediction: two clusters** (Path A records vs. Path B
   ahead-of-time records), which would confirm §2 Term 2 specifically.
3. `σ(Δ Value) / mean(Δ Value)` in phase 1 vs. phase 2 at matched mean speed. **Prediction: phase 2
   is materially higher, even though `σ(Δ FrameUs)` is comparable in both.** This is the direct
   demonstration of the asymmetry: same record jitter, different positional consequence.

Item 3 is the load-bearing one. It distinguishes this hypothesis from "the frames are just
irregular", because it shows the irregularity is present in *both* phases and only converts into
position error in one.

### 7.2 Cheapest falsifying change

On **Win32** (exact timebase, no epoch risk, dedicated render thread):

1. After `PInvoke.DwmFlush()` in `Win32RenderPacer.WaitForNextFrame` (`Win32RenderPacer.cs:61`), call
   `DwmGetCompositionTimingInfo` and publish `qpcVBlank` + `qpcRefreshPeriod`.
2. In `Compositor.RenderRootVisual` (`Compositor.skia.cs:230`), replace `TimestampInTicks` with
   `qpcVBlank + 2 · qpcRefreshPeriod` (clamped monotonic).
3. Re-run the §7.1 measurement on a wheel decay (which uses the same `FrameStarting` seam,
   `ScrollContentPresenter.Managed.cs:691`) and compare jerk against the recorded 0.171 baseline.

If jerk drops materially with no other change, the mechanism is confirmed and porting to
Android/iOS/WASM is justified. If it does not, the hypothesis is refuted cheaply and on the platform
where every confounder (epoch mismatch, single-threading, MAILBOX latch depth) is absent.

### 7.3 Second, independent probe

Temporarily neutralise Path B: make `OnRenderFrameOpportunity` a no-op while
`Compositor.HasFrameStartingSubscribers` is true (`Compositor.skia.cs:211` already exposes exactly
that predicate). This removes the phase alternation of §2 Term 2 without touching the timestamp.
If inertia improves noticeably from that alone, Term 2 is the dominant contributor and is worth
fixing on its own merits, independently of predicted-present.

---

## 8. Unverified claims, collected

| Claim | Why it matters | How to settle |
|---|---|---|
| Android `ViewRootImpl` resamples batched touch to a fixed pre-vsync offset before `DispatchTouchEvent` | The strongest form of "drag is immune" | On-device: log `MotionEvent.EventTimeNanos` deltas during a drag and compare to Choreographer frame times |
| Android `MessageQueue` sync barriers delay Uno's `Handler.Post` record message | A named, fixable source of `δ_sched` on Android | Systrace / Perfetto: look for the record running after the traversal callback |
| SurfaceFlinger adds +1 (occasionally +2) vsync after a MAILBOX present | Sets `k` in `V̂ = frameTime + k·T` | Perfetto frame timeline, or `FrameTimeline.getExpectedPresentationTimeNanos()` on API 34+ |
| `Stopwatch.GetTimestamp()` shares an epoch with Java `System.nanoTime` (Android) | A mismatch silently breaks the whole fix | One-line probe logging both at the same instant |
| `Stopwatch.GetTimestamp()` shares an epoch with `CACurrentMediaTime()` (Apple) | Same | Same |
| `Stopwatch.GetTimestamp()` shares an origin with `performance.now()` (WASM) | Same | Same |
| Dynamic displacement threshold of 1–3 arcmin under smooth pursuit | Sets the budget in §5 | Literature; the budget is order-of-magnitude and does not need to be exact |

## 9. Incidental observation (out of scope, worth noting)

`UnoSKVulkanView.InvalidateRender` calls `ExploreByTouchHelper.InvalidateRoot()` on **every**
invalidation (`UnoSKVulkanView.cs:62`), i.e. once per recorded frame during a scroll, from the UI
thread via `Render()` at `Rendering.skia.cs:171`. If that posts or does non-trivial work it is a
per-frame UI-thread cost sitting directly on the record path, and therefore a contributor to
`δ_sched`. Not investigated here; flagged because it is on the hot path this document is about.
