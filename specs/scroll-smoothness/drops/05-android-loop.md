# 05 — The Android render loop as the cause, and why Win32 is clean

Scope: `UnoSKVulkanView.RenderLoop` + `ChoreographerFramePacer` vs `Win32WindowWrapper.RenderThread` +
`Win32RenderPacer`. Question posed: *can the render thread wake and Draw before the UI thread has
recorded, how often, and is there a handshake ensuring one record per present?*

Everything below is **code review by inspection**. Nothing here was compiled or run. Claims that
depend on device behaviour are marked **UNVERIFIED**.

---

## 0. Headline answers

1. **Yes, the render thread can Draw before the UI thread has recorded.** The mechanism is exact and
   is *not* in the Android loop — it is in `CompositionTarget.RequestNewFrame`, which raises
   `IXamlRootHost.InvalidateRender()` at the moment a **visual is invalidated**, not at the moment a
   **picture exists**.
2. **There is no handshake.** Neither on Android nor on Win32. The wake signal is a bare boolean /
   auto-reset event with no frame identity. `FpsHelper` has to *reconstruct* the record↔present
   pairing after the fact from two generation counters — which is itself the proof that the pipeline
   does not carry one.
3. **The Android loop is not structurally different from Win32's in this respect.** Both can present
   stale content by the same route. So "the Android loop's design" is **not**, on its own, the
   answer. What differs is the *latency budget* and, critically, the *phase anchor* of the record.
4. The mechanism that does discriminate all three observations is stated in §4 and is a single
   inequality. It predicts drag ≈ 0, fling > 0, RedirectVisual = 0, with no special pleading.

---

## 1. The two loops, as they actually are

### Android — `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs:146-162`

```csharp
while (_surfaceReady && !_disposed)
{
    _renderEvent.Wait(TimeSpan.FromMilliseconds(100));   // 149
    _renderEvent.Reset();                                // 150
    if (!_surfaceReady || _disposed || !_renderRequested)
        continue;                                        // 152-153
    _renderRequested = false;                            // 155
    RenderFrame();                                       // 156  -> Draw + present
    _pacer.WaitForNextFrame();                           // 161  -> block to next Choreographer vsync
}
```

Wake source, `UnoSKVulkanView.cs:60-65`:

```csharp
public void InvalidateRender()
{
    ExploreByTouchHelper.InvalidateRoot();   // JNI into AndroidX, on the caller's thread
    _renderRequested = true;
    _renderEvent.Set();
}
```

`RenderFrame()` (`:202-230`) calls `CompositionTarget.OnNativePlatformFrameRequested`, then assigns
`ApplicationActivity.NativeLayerHost!.Path = nativeClipPath` (`:220`).

### Win32 — `src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32WindowWrapper.RenderThread.cs:52-89`

```csharp
while (!_disposed)
{
    _frameSignal.WaitOne();                 // 56  AutoResetEvent, unbounded
    _renderer.StartPaint();                 // 65
    var result = _drawFrame();              // 68  -> Draw
    if (result is { } frame)
    {
        _onClipPathUpdated(clipPath);       // 72  -> marshals to the dispatcher, does no work here
        _renderer.CopyPixels(width, height);// 73  -> present, and the pacer wait lives INSIDE this
        _presentedEvent.Set();              // 75
    }
    _renderer.EndPaint();                   // 85
}
```

Wake source, `Win32WindowWrapper.Rendering.cs:24`: `void IXamlRootHost.InvalidateRender() =>
_renderThread?.SignalNewFrame();` — i.e. `_presentedEvent.Reset(); _frameSignal.Set();`
(`RenderThread.cs:41-45`).

Pacer, `Win32WindowWrapper.Rendering.Vulkan.cs:112-134`: `BlitAndPresent()` then
`_pacer.WaitForNextFrame()` → `PInvoke.DwmFlush()` (`Win32RenderPacer.cs:61`), **in-thread, no hop**.

### Side by side

| | Android | Win32 |
|---|---|---|
| wake primitive | `ManualResetEventSlim` + `volatile bool _renderRequested` | `AutoResetEvent _frameSignal` |
| wake carries frame identity? | **no** | **no** |
| draw predicate | `_renderRequested == true` | "signal was set" |
| pacer position | **after** `RenderFrame()`, in the loop | **inside** `CopyPixels`, before `_presentedEvent.Set()` |
| pacer implementation | `Handler.Post` → 3rd Looper thread → `Choreographer.PostFrameCallback` → `Monitor.PulseAll` | `DwmFlush()` (or an in-process timer when degraded) |
| UI thread / dispatcher | **Android main Looper**, shared with ViewRootImpl, input, binder, platform Handlers (`NativeDispatcher.Android.cs:42`, `_handler.Post(_implementor)`) | dedicated message-only HWND on a dedicated thread, **Uno traffic only** (`Win32EventLoop.cs:51-63, 96-108`) |
| items per native post | 1 (`DispatchItems` runs exactly one action, then re-posts) | 1 (identical) |

Note the Android dispatcher fact that surprised me and is worth recording: **Skia-on-Android does not
use `NativeDispatcher.skia.cs`.** `Uno.UI.Dispatching.Skia.csproj` targets
`$(NetSkiaPreviousAndCurrent)` (generic only) and `Uno.UI.Runtime.Skia.Android.csproj:88` references
`Uno.UI.Dispatching.netcoremobile.csproj`. So the Skia Android head runs
`NativeDispatcher.Android.cs` and every dispatcher item is one `Handler.post` to the **Android main
Looper**. There is no `DispatchOverride` assignment anywhere under
`src/Uno.UI.Runtime.Skia.Android/` (verified by grep).

---

## 2. Where the "wake before record" comes from

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:86-118`:

```csharp
void ICompositionTarget.RequestNewFrame()
{
    ...
    if (!_renderedAheadOfTime && !RenderRequested) { RenderRequested = true; shouldEnqueue = true; }
    ...
    if (shouldEnqueue)
        host.InvalidateRender();        // line 110  <-- signal raised here
}
```

The picture is published far later, in `CompositionTarget.Rendering.skia.cs`:

```
:119  RecordPictureAndReturnPath(...)          // the paint walk
:147  _lastRenderedFrame = (framePicture,...)  // <-- picture becomes visible to Draw
:157  _fpsHelper.OnFrameRecorded()             // <-- generation++
:171  host.InvalidateRender()                  // second signal, this one is honest
```

So every render cycle raises **two** `InvalidateRender` calls, and the first one is a lie: it says
"there is something to present" when there is not.

The drop counter is defined against exactly that gap
(`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:292-324`):

```csharp
if (current == lastPresented) { Interlocked.Increment(ref _droppedThisSecond); return; }
```

**Define the vulnerability window** `W = [ first RequestNewFrame of the cycle → Rendering.skia.cs:147 ]`.
A Draw entered inside `W` increments `dropped`.

Normally the pacer hides `W` completely: after each present the render thread is blocked in
`WaitForNextFrame()` for the rest of the vsync period, so every signal raised inside that period is
absorbed into `_renderRequested` and only acted on at the next vsync. **A stale Draw happens iff the
render thread's vsync release lands inside `W`.**

---

## 3. Answer: how often, and is there a handshake

**How often:** at most **once per vsync** — the pacer caps presents at the refresh rate, so the loop
cannot spin out stale frames. That matches the observation (FPS stays 100+; only ~20 of ~120
presents/s are stale).

**Handshake:** none. Four specific gaps, all in current code:

- **G1.** `_renderRequested` is a bare `bool` (`UnoSKVulkanView.cs:35`). It cannot distinguish
  "a record has *started*" from "a picture is *ready*". Win32's `_frameSignal` has the same defect.
- **G2.** `_renderRequested = false` is executed at `:155`, **before** `RenderFrame()`. Correct for
  not losing a record that completes during the present — but it also means the loop has already
  discarded the only information it had by the time it looks at `_lastRenderedFrame`.
- **G3.** There is no "wait a bounded time for the in-flight record" path. The loop's only two
  behaviours are *draw now* or *sleep up to 100 ms*. A picture that becomes ready 0.2 ms after the
  vsync is therefore held for a **full extra period**.
- **G4.** `Draw` itself does not refuse to present stale content — it presents and merely *counts*
  the fact (`CompositionTarget.Rendering.skia.cs:221-330`, `SkiaRenderHelper.skia.cs:309-313`).

Consequence of G3+G4, and this is the important one: **one late record costs two vsyncs, not one.**
The vsync is spent showing a duplicate frame, and the finished picture then waits another whole
period. That is a judder amplifier: the visible cadence becomes `step, step, 0, 2·step`, which reads
far worse than a uniformly lower frame rate.

---

## 4. The criterion, and the three-way discrimination

Under the pacer, the present happens essentially **at** a vsync (the render thread wakes at V and
draws immediately). So the drop condition is not a phase lottery — it is deterministic per frame:

> **`dropped++` on frame *k* ⟺ `L_k + R_k > T − D_k`**
>
> `T` = vsync period (8.33 ms at 120 Hz)
> `D` = present duration on the render thread (picture replay + `GrContext.Flush` + `BlitAndPresent` + the clip-path work at `UnoSKVulkanView.cs:220`)
> `L` = latency from "present started" to "the record actually begins on the UI thread"
> `R` = record duration (`Render()`, i.e. `RenderRootVisual` + paint walk + damage bookkeeping)

`R` and `D` are the same ListView in the drag and fling cases. **The whole discrimination is in `L`,
and specifically in what sets the record's phase.**

### Drag — `L` is anchored to the display by the OS

The offset is written in the pointer handler. On Android the MotionEvent is delivered by the
Choreographer **input phase**, i.e. at the top of the frame. The pointer handler calls
`ScrollContentPresenter.Set` → `InvalidateViewport` → `EnqueueForEffectiveViewportChanged` →
`CoreServices.RequestAdditionalFrame` (`CoreServices.cs:67-75`), which enqueues a **Normal** item
`OnTick`. `OnTick` does `root.UpdateLayout()` and then, on Skia,
`OnRenderFrameOpportunity()` (`CoreServices.cs:123-125`) → **`Render()` runs right there**, ahead of
the render action (`CompositionTarget.RenderScheduling.skia.cs:178-208`).

So the drag's record starts at the top of the frame and is re-anchored to the display clock **every
frame** by the OS input pipeline. `L ≈ 0` and it is self-correcting. The whole of `W` closes long
before the render thread's next vsync release, which is a full period away.

### Fling — `L` is chained to the previous present, with no restoring force

There is no input. The only chain that can start a record is:

```
Draw (render thread)
  -> OnNativePlatformFrameRequested :170-173  NativeDispatcher.Main.EnqueueRender(...)
  -> EnqueueNative(High) -> Handler.post to the Android main Looper
  -> DispatchItems -> TryGetRenderAction (NativeDispatcher.cs:206-234)
       withheld while normalItemsToProcessBeforeNextRenderAction > 0, re-seeded to
       _queues[Normal].Count on every handover (:216)
  -> one Normal item per Looper round trip (DispatchItems runs exactly ONE action, :128-177)
  -> eventually EnqueueRenderCallback (or OnTick) -> Render()
```

`L` is therefore *pure main-Looper latency accumulated after the present*, multiplied by however many
Normal items the fling's own viewport invalidation put in front of the render action. It has **no
anchor to the display clock at all** — nothing pulls it back toward the top of the frame. Every
millisecond of Looper delay eats directly into the same period the record has to finish inside.

And the fling raises the early signal at the **worst possible moment**:
`Compositor.RenderRootVisual` raises `FrameStarting` at `Compositor.skia.cs:307-315`, **before** the
paint walk at `:351`. `OnFlingFrame` (`ScrollContentPresenter.Managed.cs:617`) writes the offset
there, so `W` opens at the very top of the record and stays open for the entire ListView paint.

### RedirectVisual — `R ≈ 0` and there are no Normal items

`RedirectVisualTests.xaml` is a `StackPanel` with two `Image`s, two 100–200 px `Canvas`es and a Lottie
`AnimatedVisualPlayer`. Composition animations also tick before the paint walk
(`Compositor.skia.cs:326-342`) and `:372-375` re-requests a frame, so this page **also** raises the
early signal — the early-signal fact alone does *not* explain it. What explains it is that its `R` is
sub-millisecond, it triggers no measure/arrange and no viewport invalidation, so
`_queues[Normal]` stays empty, `normalItemsToProcessBeforeNextRenderAction` stays 0, and
`TryGetRenderAction` hands the render action over on the very first `DispatchItems` after the
present. `L + R ≪ T − D` by a wide margin, every frame.

### Why Win32 is clean on the same fling

Same structure, every term smaller and — decisively — **uncontended**:

- `L`: the Win32 UI thread is a dedicated, Uno-only message pump on a message-only HWND
  (`Win32EventLoop.cs:51-63`). Nothing else posts to it. On Android the same role is the **Android
  main Looper**, shared with ViewRootImpl traversals, the input pipeline, binder callbacks and every
  platform Handler in the process. `Handler.post` messages are *synchronous* messages, so a pending
  `ViewRootImpl` traversal sync barrier blocks them until the next `doFrame`. **UNVERIFIED** that a
  barrier is actually pending during a Uno fling — with SurfaceView + our own render thread there
  should be no traversals, and `ClippedRelativeLayout.Path`'s `Invalidate()` only fires on an actual
  clip-path change (`ApplicationActivity.cs:508-521`) — but this is the only mechanism I can find
  that turns a sub-millisecond post into a multi-millisecond one, and it is cheap to test (§6, E2).
- `D`: `DwmFlush()` is an in-thread block (`Win32RenderPacer.cs:61`). Android's pacer costs a
  cross-thread `Handler.Post` per wait (`ChoreographerFramePacer.cs:88`) before the frame callback is
  even registered.
- `T`: 8.33 ms on the Fold 7 vs 8.26 ms at 121 Hz on the measured Win32 box — comparable — but the
  Win32 CPU is far faster, so `R` and `L` are a small fraction of it. **The measured Win32 result
  (121 callbacks/s, 0% duplicate offsets) is consistent with Win32 simply having margin, not with
  Win32 having a different design.** That is a falsifiable claim: see E4.

### Three-way table — the surviving mechanism

| | early signal raised? | `R` (record cost) | `L` phase anchor | predicted `dropped` | observed |
|---|---|---|---|---|---|
| **Drag** | yes (pointer handler) | ListView layout + paint | **OS input clock, top of frame, re-anchored every frame** | ≈ 0 | ~0 ✔ |
| **Inertia** | yes (`FrameStarting`, top of record) | ListView layout + paint | **previous present + Looper latency + N Normal items; no restoring force** | > 0, tail-driven | 20+ ✔ |
| **RedirectVisual** | yes (animation tick) | ≈ 0, no Normal items | previous present + Looper latency (minimal) | ≈ 0 | 0 ✔ |

"Worse the slower the fling gets" is **perceptual, not a rate change** (**UNVERIFIED**): as the fling
decelerates the per-frame step shrinks, so a doubled frame followed by a double step is a much larger
*fraction* of the step and becomes far more visible. Nothing in the inequality predicts the drop rate
itself rising with lower velocity.

---

## 5. Hypotheses evaluated, including the brief's leading one

Every hypothesis gets its three-way prediction. Anything that predicts a drop for drag or for
RedirectVisual is wrong by construction.

### H1 — "the Android loop's design lets it draw before the record" (my brief's framing)

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | drops | drops | drops |

**REJECTED as stated.** The gap is real (§2, §3) but it is in `RequestNewFrame` /
`CompositionTarget`, shared with Win32, and it fires on all three cases. It is a *necessary
precondition*, not a discriminator. Keep it as the thing to *fix* (§7), not as the *cause*.

### H2 — "the fling enqueues Normal-priority work from inside the record, delaying the next record past its vsync" (the brief's leading hypothesis)

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | **drops** — the drag enqueues the *same* Normal item from `InvalidateViewport`, and does so *before* the render action too | drops ✔ | no drops ✔ |

**PARTIALLY WRONG as stated.** The drag path enqueues exactly the same `RequestAdditionalFrame`
Normal item, so "the fling enqueues Normal work" cannot be the difference. Worse, the brief's framing
has the causality backwards for the drag: `TryGetRenderAction` withholding the render action behind
Normal items is what lets `OnTick` → `OnRenderFrameOpportunity` → `Render()` run **early**, which is
why the drag records at the top of the frame at all. The withholding *helps* the drag.

What survives from H2 is the *timing* half, generalised: it is not "Normal-priority work exists", it
is **"the fling's record has no phase anchor and its start is `previous present + Looper latency`"**.
That is H3.

### H3 — the phase/budget inequality of §4

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | ≈ 0 (L anchored to the display by input delivery) | > 0 (L unanchored, R large, tail-driven) | 0 (R ≈ 0, no Normal items) |

**SURVIVES.** Only hypothesis I found that fits all three without special pleading. Falsifiable by
E2 and E4.

### H4 — `ChoreographerFramePacer` late registration / phase noise (§7, R1+R2)

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | neutral — the drag's record phase is set by input, not by the present, so a jittery pacer wake does not move `W` | **worsens** — the fling's record phase *is* chained to the present, so pacer jitter is injected straight into `L` | neutral — `W` ≈ 0, so the wake phase cannot land inside it |

**SURVIVES as a contributing factor, not as the cause.** Consistent with all three. Cheap to test
(E5).

### H5 — per-present render-thread cost specific to Android

`RenderFrame()` assigns `NativeLayerHost.Path` every present (`UnoSKVulkanView.cs:220`), and the
setter runs `value.ToSvgPathData()` — an SKPath→string serialisation plus a string compare — on
**every frame** (`ApplicationActivity.cs:507-508`). Win32's equivalent just marshals a delegate
(`Win32WindowWrapper.Rendering.cs:38-43`). Likewise `InvalidateRender` does a JNI
`ExploreByTouchHelper.InvalidateRoot()` per invalidation, i.e. twice per fling frame on the UI thread
*inside `W`* (`UnoSKVulkanView.cs:62`).

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | drops (raises `D` for everyone) | drops | drops |

**REJECTED as the cause** — it hits all three equally. **Keep as a cost to remove**: it inflates `D`
and therefore shrinks the `T − D` budget for every case, and it is pure waste. Cache the SVG string
against the `SKPath` identity, or compare `SKPath` handles before serialising.

### H6 — the `_renderEvent.Wait()` / `Reset()` race loses wake-ups

Examined and **REJECTED**: `InvalidateRender` writes `_renderRequested = true` **before**
`_renderEvent.Set()` (`UnoSKVulkanView.cs:63-64`), and the loop reads `_renderRequested` **after**
`Reset()` (`:150-152`). A `Set` lost to the `Reset` is still covered by the flag; a flag read as
false implies the `Set` had not happened yet, so the next `Wait` returns immediately. Worst case is
one wasted loop iteration. No lost wake-up.

---

## 6. `ChoreographerFramePacer` — post-fix assessment

The `AutoResetEvent` defect is genuinely gone. The current code counts frames under a monitor
(`ChoreographerFramePacer.cs:35-36, 70-77, 90-101`), so a callback left outstanding by a timed-out
wait increments `_frameCount` and cannot satisfy a later wait — the later wait re-reads `seen` after
its own post. Correct.

Residual defects, all by inspection, all **UNVERIFIED** at runtime:

**R1 — late registration.** `WaitForNextFrame` does `_handler.Post(() => _choreographer.PostFrameCallback(...))`
(`:88`). The frame callback is only registered after that Runnable is dequeued on the pacer's Looper.
If the present finishes shortly before a vsync, registration misses it and the pacer sleeps ≈2
periods. Effect: an occasional missed present and, more importantly, **an unstable phase between the
render thread's wake and the UI thread's record** — the exact variable H3 says decides the drop.

**R2 — the `seen` read is racy in the opposite direction to the comment.** Line 92-93 claims reading
`_frameCount` *after* posting means "only a vsync from here on counts". In the interleaving where the
Runnable is dequeued, `PostFrameCallback` registered, and `DoFrame` fires **all before line 93
executes**, `_frameCount` has already been incremented, `seen` captures the post-increment value, and
the loop waits an **extra full vsync**. Narrow window, same class of effect as R1.

**R3 — `MaxWait` is both the "not composited" timeout and the per-wait timeout** (`:30, :96`). At
120 Hz a 100 ms timeout is 12 periods; a wait that times out for a real reason has already cost 12
frames. Not implicated in the observation, but worth a note.

**R4 — the pacer is per-view but `Dispose()` is called from `SurfaceDestroyed`
(`UnoSKVulkanView.cs:121`) and never re-created.** `SurfaceCreated` starts a new render thread
(`:84-89`) but `_pacer` is a `readonly` field initialised once (`:34`). After a surface
destroy/recreate cycle (fold/unfold on a Fold 7 is exactly this), the pacer is disposed and
`WaitForNextFrame` early-returns on `_disposed` (`:82`) — **the render thread free-runs, unpaced, for
the rest of the process.** This is a separate bug from the observation, but on the exact device in
question it is one fold away. Worth fixing regardless. *(Also: `_pacer.Dispose()` runs while the
render thread may still be inside `WaitForNextFrame`; `_ready.Dispose()` at `:123` is not guarded
against a concurrent `_ready.Wait` at `:82`.)*

Crucially, **none of R1–R4 predicts drops for RedirectVisual** (its `W` ≈ 0, so no wake phase can
land inside it) **nor for drag** (its record phase comes from input, not from the present). So the
pacer is admissible as a contributor. It is not sufficient on its own — it does not explain why the
fling's record is late in the first place.

---

## 7. What the analysis implies as a fix, with its three-way prediction

Make `_renderRequested` mean **"a picture newer than the last presented one exists"** instead of
**"someone signalled"**. Concretely: keep `RequestNewFrame`'s `InvalidateRender` as the *idle kick*
(removing it would stop the loop ever starting), but gate the actual `RenderFrame()` on an unpresented
generation — the two counters already exist in `FpsHelper`
(`SkiaRenderHelper.skia.cs:185-186`); promote them (or an equivalent) onto `CompositionTarget` and
have the loop re-enter the pacer wait when there is nothing new.

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicts | unchanged, ≈ 0 | `dropped` → ≈ 0; a late record still costs latency but no longer costs a **wasted present plus a doubled frame** | unchanged, 0 |

Label: this is a **root-cause fix for the wasted-present half** of the problem and **defensive
hardening for the latency half**. It does not make `L + R` fit in `T`; it stops a miss from costing
two periods and a duplicate frame. If the fling still feels juddery afterwards, the remaining cause
is `L` (main-Looper latency) or `R` (paint cost) and belongs to a different note.

---

## 8. Experiments, cheapest first

**E1 — falsify H3's core claim on-device, 20 minutes.**
Gate `RenderFrame()` on an unpresented generation as in §7 and re-run the same fling.
- `dropped` → ≈ 0 **and it feels smooth** ⇒ the wasted present + doubled period *was* the judder.
- `dropped` → ≈ 0 **and it still feels juddery** ⇒ the drop counter was an accounting artefact of the
  early signal; the real problem is `L`/`R` and E2 is next.
- `dropped` stays high ⇒ my model of the pacer absorbing intra-period signals is wrong; re-derive.

**E2 — measure the four terms, decisive between "phase" and "cost".**
Instrument, per frame: (a) pacer release → `Draw` entry; (b) `Draw` entry → present complete (`D`);
(c) `EnqueueRender` post → `EnqueueRenderCallback`/`OnTick` entry (`L`); (d) `Render()` duration
(`R`). Fling 5 s, histogram.
- (c) with a fat tail past ~2 ms ⇒ main Looper / Normal-queue. Check for a `ViewRootImpl` sync barrier
  with `adb shell dumpsys gfxinfo` / a `Looper` message trace.
- (d) alone exceeding `T` ⇒ paint cost, not the loop.
- (a) bimodal at ≈0 and ≈8.3 ms ⇒ pacer late registration (R1/R2).
Run the same instrumentation for the drag and for RedirectVisual. **H3 predicts (c) is small and
tightly clustered for the drag, small for RedirectVisual, and fat-tailed for the fling.** If (c) is
identical across drag and fling, H3 is dead.

**E3 — kill H5's cost cheaply.** Cache the SVG string against the `SKPath` instance in
`ClippedRelativeLayout.Path` and re-measure `D`. Expected: measurable drop in `D`, small improvement
in `dropped`, no change to which case drops. If it changes *which* case drops, H5 was more than a
cost.

**E4 — falsify "Win32 is clean only because it has margin", on Win32, no device needed.**
`src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_ScrollSmoothness.cs` already measures
121 callbacks/s at 0% duplicate offsets. Re-run it with an artificial per-frame cost injected so that
`L + R` approaches `T` — e.g. a `Compositor.FrameStarting` handler registered *after* the scroll
driver that spins for a configurable number of milliseconds. Prediction under H3:
- fling duplicate-offset rate rises above 0% and rises **sharply** once the injected cost crosses
  `T − D − L`;
- the drag-equivalent (`PointerMoved`-driven) path stays at 0% at the *same* injected cost, because
  its `L` is anchored to input delivery;
- a Composition-animation-driven page stays at 0% until the injected cost alone exceeds the period.
If all three degrade together, H3's "phase anchor" claim is wrong and the difference is pure cost.
**This is the experiment I would run first if I could not touch the device** — it validates the fix
of §7 without a Fold 7.

**E5 — isolate the pacer (H4).** Register the Choreographer frame callback **once** and keep it
self-re-posting on the pacer thread, with `WaitForNextFrame` only doing the monitor wait — removing
the per-wait `Handler.Post` entirely. Re-measure (a) from E2. If (a)'s bimodality disappears and
`dropped` falls materially, R1/R2 are real contributors.

---

## 9. Explicitly not established

- That the main-Looper latency `L` actually differs between drag and fling on the device. This is the
  load-bearing claim of H3 and it is **UNVERIFIED**; E2 is exactly the test.
- That a `ViewRootImpl` sync barrier is ever pending during an Uno fling. Plausible mechanism, no
  evidence.
- That Android delivers MotionEvents at the Choreographer input phase in this configuration
  (SurfaceView, no ViewRootImpl traversals of our own). This is standard Android behaviour but
  **UNVERIFIED here**, and H3's drag arm depends on it.
- The claim that the fling drop rate does not rise as the fling decelerates (the "worse when slower"
  observation is attributed to perception). **UNVERIFIED** — E2 with a velocity bucket would settle it.
- Nothing in this note was compiled or executed. Evidence class: **code review only.**
