# 06 — Attacking the measurement: what "dropped" actually counts

Scope: take the instrument apart before trusting the 0-vs-20-vs-0 observation. Everything below is
read from the worktree at `dev/mazi/smooth-scroll` and cited `file:line`. Anything not established
from code is marked **UNVERIFIED**.

---

## 0. Verdict up front

**The stated definition of "dropped" is wrong on Android, in two independent ways.**

The task brief defines it as *"the native vsync fired and `Draw` ran, but no new picture had been
recorded since the previous `Draw`, so the same picture is re-blitted — it counts vsyncs that showed
stale content."* On Skia-on-Android/Vulkan:

1. **`Draw` is not vsync-driven.** It is *invalidate*-driven and *vsync-paced*. A vsync with no
   pending `InvalidateRender` produces no `Draw` and cannot produce a drop
   (`UnoSKVulkanView.cs:146-162`). So the counter cannot see a missed vsync at all.
2. **The stale re-blit paints nothing.** The presented frame's damage `SKPath` was already `Reset()`
   by the *previous* `Draw` (`CompositionTarget.Rendering.skia.cs:309`), so the repeat `Draw` runs
   `canvas.ClipPath(<empty path>)` (`:291`) and the picture draw is fully clipped out. It is not a
   re-blit; it is an empty paint that still presents.

What "dropped" therefore measures on Android is: **the number of times per second a
`host.InvalidateRender()` was issued *before* the picture it was asking for had been published.**
That is an ordering metric, not a vsync-miss metric.

And that ordering metric has exactly one structural source, which resolves all three cases:

> `Compositor.RenderRootVisual` re-requests a frame **from inside the record**
> (`Compositor.skia.cs:374`, plus `InvalidateRenderPartial` at `:378-383`), i.e. before the picture
> is published at `CompositionTarget.Rendering.skia.cs:147`. Whether that early request turns into a
> counted drop depends on whether a display vsync lands inside the window
> `[in-record invalidate → picture published]`. A drag never opens that window at all.

The 0-vs-20 gap is **real but mis-named**, and **one arm of the comparison is structurally incapable
of producing a non-zero number**. Detail and the strongest artifact case in §7.

### Three-way prediction table (the mechanism above)

| | in-record `InvalidateRender`? | window `[invalidate → publish]` | predicted `dropped` | observed |
|---|---|---|---|---|
| **Finger drag** | **No** — `FrameStarting is null`, no running animation, offset written from the pointer handler *before* `Render()` | **zero-width by construction** | **structurally 0** | ~0 ✅ |
| **Touch inertia (fling)** | **Yes** — `FrameStarting` subscribed for the whole fling (`ScrollContentPresenter.Managed.cs:601`) ⇒ `Compositor.skia.cs:374` fires every frame | **wide**: FrameStarting runs at `Compositor.skia.cs:307`, *first* thing in the record; the rest of a realized-ListView paint walk follows | **>0, scaling with record cost** | 20+ ✅ |
| **RedirectVisual sample** | **Yes** — Lottie/`AnimatedVisualPlayer` keeps `_runningAnimations.Count > 0` ⇒ same `:374` fires every frame | **sub-millisecond**: two `RedirectVisual`s + a 200×200 Lottie, trivial paint walk, tiny damage | **≈0** | 0 ✅ |

The RedirectVisual case is the one that kills the naive "animations are cheap because they don't
touch layout" story: **RedirectVisual opens the same window the fling does** (verified —
`RedirectVisualTests.xaml:55-61` hosts an autoplaying `LottieVisualSource`, and
`Compositor.skia.cs:374` fires for `_runningAnimations.Count > 0` exactly as it does for
`FrameStarting is not null`). It gets 0 drops only because its window is too narrow for a vsync to
land in.

---

## 1. What the code actually counts

`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs`, class `FpsHelper` (`:121-538`).

| field | written by | read by |
|---|---|---|
| `_currentFrameGeneration` (`:185`) | `OnFrameRecorded` (`:283`), **UI thread** | `OnFramePresentRequested` (`:299`), render thread |
| `_lastPresentedGeneration` (`:186`) | `OnFramePresentRequested` (`:323`), render thread | both |
| `_droppedThisSecond` (`:177`) | `OnFramePresentRequested` (`:311`) | `TimerTick` (`:477`), **threadpool thread** |
| `_unpresentedThisSecond` (`:178`) | `OnFrameRecorded` (`:279`) | `TimerTick` (`:478`) |
| `_framesRenderedInLastSecond` (`:175`) | `EndFrame` (`:259`), render thread | `TimerTick` (`:475`) |

Call sites (`CompositionTarget.Rendering.skia.cs`):

- `OnFrameRecorded()` at `:157` — UI thread, in `Render()`, **after** the frame is published at `:147`.
- `OnFramePresentRequested()` at `:240` — render thread, inside `lock (_frameGate)` at the **top of
  `Draw`**, *before* the null check at `:243` and before any pixels are produced.
- `BeginFrame()`/`EndFrame()` at `:294` — render thread, wrapping **only the blit**
  (`SkiaRenderHelper.RenderPicture`), and only reached when a frame exists and bounds are non-zero.

Three consequences that matter and are not obvious from the panel:

1. **`fps` is the *`Draw` rate*, not the fresh-frame rate.** `EndFrame` (`:243-260`) increments
   `_framesRenderedInLastSecond` on every `Draw` that reaches the paint block — **including the ones
   counted as dropped**, because a "dropped" `Draw` still has a non-null `lastRenderedFrame` (the
   previous `Draw` put it back via `ReturnFrame`, `:412-434`). So the useful rate the panel never
   shows is `fps − dropped`. For the fling that is `100+ − 20 ≈ 80–100`; for the drag `100+ − 0 ≈
   100+`.
2. **`frameTime` measures the blit, not the frame.** `BeginFrame`/`EndFrame` bracket
   `RenderPicture` only (`:294-299`) — the UI-thread record cost, the `UpdateLayout()` cost, and the
   dispatcher latency are all outside it. A reader treating the panel's "ms" as frame cost is
   reading the wrong number.
3. **`delay` (draw-to-present) is biased low exactly when things are worst.** The sample at
   `SkiaRenderHelper.skia.cs:315-321` is taken only on the *fresh* branch; a dropped `Draw` returns
   at `:312` and contributes nothing. The ten-sample mean therefore silently excludes the pathological
   frames.

---

## 2. (a) Per vsync or per `Draw`? — and the Android loop

**Per `Draw` call.** `OnFramePresentRequested` is called once per `Draw`, unconditionally
(`Rendering.skia.cs:240`).

The question is then: what drives `Draw` on Android? `UnoSKVulkanView.RenderLoop` (`:137-171`):

```csharp
while (_surfaceReady && !_disposed)
{
    _renderEvent.Wait(TimeSpan.FromMilliseconds(100));   // :149
    _renderEvent.Reset();                                // :150
    if (!_surfaceReady || _disposed || !_renderRequested)
        continue;                                        // :152-153
    _renderRequested = false;                            // :155
    RenderFrame();                                       // :156  -> Draw
    _pacer.WaitForNextFrame();                           // :161  -> blocks on Choreographer vsync
}
```

`_renderRequested` is set only by `InvalidateRender()` (`:60-65`). So:

- **A vsync alone never produces a `Draw`.** No invalidate ⇒ `continue` ⇒ no `OnFramePresentRequested`
  ⇒ no drop. Android literally cannot count "we missed a vsync".
- **The pacer is *after* the draw, not before it.** The render thread spends most of each period
  parked in `ChoreographerFramePacer.WaitForNextFrame` (`ChoreographerFramePacer.cs:80-102`). An
  `InvalidateRender` arriving mid-record does not wake a `Draw` immediately; it sets a flag that is
  consumed **at the next vsync**.
- **`_renderRequested` is a bool, not a count.** Two invalidates arriving while the thread is parked
  collapse into one `Draw`. Two invalidates *separated by a `RenderFrame()`* produce **two** `Draw`s.

That last point is the whole mechanism. Contrast **Win32**: `RenderThread.RenderLoop`
(`Win32WindowWrapper.RenderThread.cs:52-90`) waits on an `AutoResetEvent` and calls `_drawFrame()`
on every wake with no `_renderRequested` re-check, and its pacing is inside `CopyPixels`/`DwmFlush`.
Different loop shape, same counter — which is one reason the Win32 harness result (121 Rendering
callbacks/s, 0% duplicate offsets) is **not** evidence that the counter behaves the same way there.

### The window, precisely

`Render()` (`Rendering.skia.cs:110-198`) in order:

| line | action |
|---|---|
| `:119-124` | `RecordPictureAndReturnPath` → `SkiaRenderHelper.skia.cs:44` → `Compositor.RenderRootVisual` |
| ↳ `Compositor.skia.cs:307` | **`FrameStarting` raised** — the fling driver runs here, writes the offset |
| ↳ `Compositor.skia.cs:325-341` | running animations tick (`RaiseAnimationFrame`) — Lottie runs here |
| ↳ `Compositor.skia.cs:351` | the paint walk (the expensive part for a realized ListView) |
| ↳ `Compositor.skia.cs:372-375` | **`RequestNewFrame()` if `_runningAnimations.Count > 0 \|\| transitions \|\| FrameStarting is not null`** |
| `:147` | **picture published** under `_frameGate` |
| `:157` | `OnFrameRecorded()` → generation++ |
| `:164-167` | `RequestNewFrame()` if `_isRenderingActive` |
| `:171` | **unconditional `host.InvalidateRender()`** — bypasses the `RequestNewFrame` state machine entirely |

`RequestNewFrame` (`RenderScheduling.skia.cs:86-118`) only reaches `host.InvalidateRender()` at `:110`
when `!_renderedAheadOfTime && !RenderRequested` (`:93`). So the in-record request at
`Compositor.skia.cs:374` fires an actual invalidate **only when `Render()` was entered from the render
action (`EnqueueRenderCallback`, `:145-152`)**, not from the ahead-of-time door
(`OnRenderFrameOpportunity`, `:178-208`, which sets `_renderedAheadOfTime = true` at `:195` *before*
calling `Render()` at `:205` — swallowing the request into `_renderRequestedAfterAheadOfTimePaint`).

**So a fling frame produces a phantom `Draw` when both hold:**
(i) that frame's `Render()` came via `EnqueueRenderCallback` rather than `OnRenderFrameOpportunity`,
**and** (ii) a vsync lands between `Compositor.skia.cs:374` and `Rendering.skia.cs:147`.

Per `inertia/04-frame-cadence.md` §3 the fling normally takes the ahead-of-time path, but only when
the Normal-priority `OnTick` wins the race against the withheld render action
(`NativeDispatcher.cs:206-234`). A ~17% minority taking the `EnqueueRenderCallback` path would
produce ~20 drops/s at ~120 `Draw`s/s. That is arithmetically consistent with the observation and is
**UNVERIFIED** — §8 says how to measure the split directly.

---

## 3. (b) Can "dropped" be incremented by something harmless?

Yes. Three confirmed sources, none of which is in play during a fling, but all of which prove the
counter is not a clean signal:

1. **The FPS overlay's own idle redraw.** `TimerTick` → `RequestRedraw?.Invoke()`
   (`SkiaRenderHelper.skia.cs:500`) → `CompositionTarget.cs:23-33` enqueues a dispatcher item that
   calls `host.InvalidateRender()` **with no `Render()` behind it**. That is a guaranteed `dropped++`.
   It fires once per idle entry (guarded by `_idleRedrawPending`, `:222-228`), so it cannot explain a
   fling — but it is a self-inflicted drop by the instrument.
2. **`TryExecuteOnNextRenderAsync`** (`Rendering.skia.cs:339-357`) calls `host.InvalidateRender()` at
   `:348` with no record. Guaranteed `dropped++` per call. Only user in the repo is
   `RenderTargetBitmap.skia.cs:64`, so not in play here.
3. **The double-invalidate per `Render`.** `:164-167` (`_isRenderingActive`) plus `:171`
   (unconditional). If any code subscribes `CompositionTarget.Rendering` — e.g. a diagnostics
   harness, including the runtime-test harness at
   `Given_ScrollSmoothness.cs` — `_isRenderingActive` becomes true and every `Render` issues two
   invalidates. **Instrumenting the problem with `CompositionTarget.Rendering` therefore changes the
   drop count.** Worth knowing before anyone repeats the on-device measurement that way.

**Second CompositionTarget / second window:** not a factor. `_fpsHelper` is per-`CompositionTarget`
(`Rendering.skia.cs:54`), the panel drawn is that target's own, and Android's `RenderFrame` resolves
exactly one target (`UnoSKVulkanView.cs:211`). A second window would get its own independent counter,
not pollute this one.

---

## 4. (c) Does enabling the counter change scheduling?

Yes — four measurable perturbations, in rough order of significance:

1. **Extra damage on every recorded frame.** `TryGetDamageBounds` (`:343-354`) is called from
   `Render()` at `Rendering.skia.cs:126-129` and unions the panel rect into `_pendingDamage`. With the
   counter **off**, a frame whose damage is empty presents nothing (empty `ClipPath`); with it **on**,
   every frame repaints at least the panel. This is small but it means the counter-on and counter-off
   pipelines are not the same pipeline.
2. **A cross-thread data race on a shared static `SKFont`.** `_font` (`:146`) is a static mutable
   native object. `MeasurePanel` → `MeasureWidth` → `_font.MeasureText` (`:402-406`) runs on the **UI
   thread** (via `TryGetDamageBounds` at `Rendering.skia.cs:126`), while `DrawFps`/`DrawCell`
   (`:356-417`) call `_font.MeasureText` and `canvas.DrawText(..., _font, ...)` on the **render
   thread** in the same frame. `SkFont` is not thread-safe. Consequence is at minimum garbage widths;
   at worst native-heap misbehaviour. This is a bug in the instrument regardless of the scroll
   question. *(Not the drop mechanism — flagged because anyone extending the overlay will hit it.)*
3. **A Normal-priority dispatcher item on the idle transition.** `RequestRedraw` posts via
   `NativeDispatcher.Main.Enqueue(...)` with the **default `Normal` priority**
   (`CompositionTarget.cs:25`, `NativeDispatcher.cs:265`) — the exact priority class that re-seeds
   `normalItemsToProcessBeforeNextRenderAction` and withholds the render action
   (`NativeDispatcher.cs:214-216`). One-shot per idle entry, so not a fling factor, but it is the
   instrument poking the very mechanism under investigation.
4. **Per-frame render-thread cost.** Each `Draw` now measures the panel twice (once on each thread),
   plus a rounded rect, five stroked icons and five text runs, all antialiased, inside the frame.
   Rough order 0.1–0.5 ms on a mobile GPU — **UNVERIFIED**, and it lands on the render thread between
   `RenderFrame()` and the pacer wait, so it eats pacing headroom rather than UI-thread headroom.

---

## 5. (d) Can a drag drop frames the counter cannot see?

**Yes, and this is the single most important caveat: the drag's zero is structurally guaranteed and
therefore carries no information.**

`dropped` can only increment when a `Draw` finds `current == lastPresented` (`:309-313`). During a
drag every `InvalidateRender` originates at `Rendering.skia.cs:171`, i.e. *after* `:147` published the
picture and `:157` bumped the generation. There is no in-record invalidate, because
`Compositor.skia.cs:372` requires `_runningAnimations.Count > 0 || transitionsCount > 0 ||
FrameStarting is not null` and a drag satisfies none of them (fling stopped at
`ScrollContentPresenter.Managed.cs:788`, wheel decay at `:406-409`, offset written from the pointer
handler at `:864-868`). **A drag can therefore have an arbitrarily expensive record, miss any number
of vsyncs, and still report `dropped == 0`.**

The two counters that *could* have caught it both fail:

- **`unpresented`** (`:268-284`) is the "recorded but never presented" counter — the correct one for
  drag-side loss. But it also cannot fire here, because the record/present pipeline is a strict
  ping-pong: `_shouldEnqueueRenderOnNextNativePlatformFrameRequested`
  (`RenderScheduling.skia.cs:74`) is consumed once per `Draw` at `:170` and re-armed once per
  `EnqueueRenderCallback` at `:125`, so at most one `Render()` exists per `Draw`. Record rate ≤ draw
  rate ⇒ `unpresented ≈ 0` always. (It is also computed with two separate `Interlocked.Read`s and a
  later `Increment` — `:275-283` — so it is not atomic even where it could fire.)
- **`fps`** would show a drag shortfall, and arguably does: **100+ on a 120 Hz panel is already a
  ~17% shortfall**, yet the drag "feels glass smooth". See §7.

---

## 6. A separate, one-sided bias in the estimator

`Render()` publishes the picture **inside** `lock (_frameGate)` (`:135-155`, publish at `:147`) but
increments the generation **outside** it (`:157`). `Draw` reads both **inside** `_frameGate` (`:233-241`).

A `Draw` that acquires `_frameGate` in the window between `:155` and `:157` therefore takes the
**new** picture while reading the **old** generation → it presents a fresh frame and counts a
**drop**. The reverse error is impossible. The estimator is biased in one direction only.

The window is a handful of instructions and the UI thread does not yield inside it, so the rate is
probably low — **UNVERIFIED**. Note that it is a *real* correctness defect in the instrument
independent of magnitude: moving `OnFrameRecorded()` inside the `_frameGate` block would close it at
zero cost.

---

## 7. The strongest adversarial case, and how much survives

### The strongest case that 0-vs-20 is an artifact

1. **The two arms are not comparable.** The drag arm cannot produce a non-zero value (§5). Comparing
   "0" to "20" is comparing a metric that is switched off to one that is switched on. Any hypothesis
   that "explains" the drag's zero by appealing to scheduling is explaining something that scheduling
   did not cause.
2. **The metric's name misdescribes the event.** It is not a missed vsync (§2) and it is not a
   re-blit (§0.2). It is "an invalidate outran its picture".
3. **The 1 Hz aggregate hides the distribution.** `TimerTick` reports totals once per second
   (`:475-478`). Twenty drops evenly spread across 120 frames and twenty drops in one 170 ms burst
   are perceptually completely different and read identically on the panel.
4. **`fps` already includes the drops** (§1.1), so the panel's two numbers are not independent, and
   the headline "100+ FPS" during a fling overstates the fresh-frame rate by the drop count.
5. **The drag's own numbers undercut the causal story.** A drag at "100+ FPS" on a 120 Hz panel is
   producing ~100 fresh frames/s against 120 refreshes — roughly the *same* fresh-frame rate the
   fling achieves (`100+ − 20 ≈ 80–100`). If a ~17% frame-rate shortfall felt "glass smooth" during a
   drag, a ~20% shortfall cannot be *the* reason the fling feels stuttery. **The drop count is more
   plausibly a correlate of the fling's real defect than its cause.**

### How much survives

Most of it, but re-scoped:

- **The mechanism is real and platform-specific.** The in-record `InvalidateRender`
  (`Compositor.skia.cs:374`) genuinely does fire before the picture exists, on both the fling and
  RedirectVisual, and genuinely does produce an extra `Draw` on the Android loop when a vsync lands
  in the window. This is code, not statistics.
- **The consequence is real, and worse than "a wasted paint".** Because the pacer is *after* the draw
  (`UnoSKVulkanView.cs:156-161`), a phantom `Draw` consumes a vsync slot and then parks the render
  thread until the *next* vsync. The fresh picture is therefore presented **one full refresh period
  late**, and the previous frame is shown for two refreshes. Twenty of those per second is ~17% of
  presented frames duplicated.
- **And duplication hurts inertia far more than drag** — which is the reconciliation of §7.5 above,
  and is already established in `inertia/04-frame-cadence.md` §4: a duplicated frame during a fling
  is a *velocity* error against content the eye is tracking; a late frame during a drag is a
  *latency* error against a finger the user is watching. Same counter value, different percept.
- **What does not survive** is the framing "the fling misses vsyncs / the UI thread is too slow to
  make its deadline". Android's counter cannot observe that. If that is the real defect, this
  instrument is not the one that will show it.

### The leading hypothesis, scored

The brief's hypothesis — *a fling enqueues Normal-priority dispatcher work from inside the record, so
the next record is delayed past its vsync* — predicts the drag and RedirectVisual correctly but
**mis-predicts the counter's mechanics**: on Android a record delayed past its vsync produces **no
`Draw` at all** and therefore **no drop**. It needs the extra step established here (the *early*
invalidate at `Compositor.skia.cs:374`) to reach a non-zero counter. The two are compatible — the
Normal-priority `OnTick` work is what makes the `[invalidate → publish]` window wide — but the
dispatcher-delay story alone is not sufficient to move this particular number.

---

## 8. Falsifying experiments

Ordered by cost. All are cheap; (E1) is decisive.

**E1 — Count invalidates against records (on-device, ~10 lines, no behaviour change).**
Add two `Interlocked` counters printed alongside the panel: `invalidatesPerSecond` (incremented in
`UnoSKVulkanView.InvalidateRender`, `:60`) and `drawsPerSecond` (incremented at the top of
`RenderLoop`'s post-`continue` body, `:155`). Predictions:

| | invalidates/s vs records/s |
|---|---|
| drag | **1:1** |
| fling | **~1.2:1** (one extra per phantom) |
| RedirectVisual | **2:1** *(both `:374` and `:171` fire, but they collapse into one `Draw` because the render thread is parked)* |

The RedirectVisual row is the discriminator: it must show ~2 invalidates per record **and still
0 drops**. If it shows 1:1, the in-record invalidate is not firing there and this mechanism is wrong.

**E2 — Widen the window artificially (on-device, one line).**
Insert a `Thread.SpinWait` of ~4 ms immediately after `Compositor.skia.cs:374` (in-record request)
and run the **RedirectVisual** sample. Prediction: RedirectVisual starts dropping ~20+/s with no
other change. If it does not, the window hypothesis is falsified. Reverse control: insert the same
spin at `Rendering.skia.cs:172` (after the invalidate) — drops must **not** appear.

**E3 — Close the window (on-device, one line; also a candidate fix).**
Move `Compositor.skia.cs:372-375`'s `RequestNewFrame()` out of `RenderRootVisual` and have `Render()`
issue it after `:157`, so no invalidate ever precedes its picture. Prediction: fling `dropped` → ~0,
fling `fps` → ~120, and the *felt* smoothness improves by roughly one refresh period of
present latency. If `dropped` goes to zero but the fling still feels stuttery, the drop counter was a
correlate and the real defect is the sampling-phase error of `inertia/03`+`04`. **Either outcome is
informative** — this is the experiment that separates the two stories.

**E4 — Falsify on Win32 (harness, no device).**
`Given_ScrollSmoothness.cs` measures duplicate offsets, not drops. Add a `dropped`/`fps` readout to it
and run the same fast/medium/slow flings. Prediction: **Win32 shows drops too**, because the same
`Compositor.skia.cs:374` fires there — but fewer, because `Win32RenderPacer` blocks inside
`CopyPixels`/`DwmFlush` rather than in a separate post-draw wait, and the Win32 loop has no
`_renderRequested` re-check (`Win32WindowWrapper.RenderThread.cs:52-90`). If Win32 shows **exactly
zero** drops across all three speeds, the mechanism is Android-loop-specific and E3's fix should be
scoped to `UnoSKVulkanView`, not to `Compositor`.

**E5 — Close the estimator bias (free, do it regardless).**
Move `_fpsHelper.OnFrameRecorded()` from `Rendering.skia.cs:157` to inside the `lock (_frameGate)`
block, immediately after `:147`. Prediction: a small, uniform reduction in `dropped` on every
platform and every scenario. If the fling's 20 drops collapse to ~0 from this alone, §6's race was
the whole effect and everything above is over-thought — cheap to rule out first.

---

## 9. Instrument defects worth fixing regardless of the outcome

1. `OnFrameRecorded` outside `_frameGate` — one-sided drop over-count (§6, E5).
2. Static `SKFont` used concurrently from the UI and render threads (§4.2).
3. `fps` includes dropped `Draw`s; the panel should show fresh frames/s, or show both (§1.1).
4. `delay` samples only fresh frames, biasing it low precisely under load (§1.3).
5. The doc comment at `SkiaRenderHelper.skia.cs:286-291` ("the native VSync fired… we'll re-blit the
   same picture") is wrong on both clauses for the Android/Vulkan path (§0).
6. `unpresented`'s read-modify-write is non-atomic across `:275-283`.
