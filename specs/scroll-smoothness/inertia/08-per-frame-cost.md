# 08 — Per-frame cost during fling vs. drag

Scope: hunt for per-frame **work** that happens during a touch fling but not during a drag (or is
larger during a fling), in `dev/mazi/smooth-scroll` @ `D:/Work/uno-worktrees/scrollsmooth`.

**Headline result: there is no meaningful per-frame *work* asymmetry.** Every candidate in the brief
was checked against source and refuted — the drag path does *more* `Set` work per frame than the
fling path, not less. The asymmetry that survives is **not cost, it is scheduling**: the fling is the
only scroll mode that makes `Compositor.FrameStarting` non-null, and that single fact changes the
render state machine's behaviour in a way that deterministically drops vsync-aligned records and
re-times the fling's clock sample. Details in §2. Refuted candidates in §1 (recorded so nobody
re-litigates them).

All line numbers are as-of the current worktree.

---

## 1. Candidates checked and refuted

### 1.1 "`Set` is called with both axes during a fling but one axis during a drag" — FALSE

The drag path passes **both** offsets too.

- fling: `ScrollContentPresenter.Managed.cs:632`
  `Set(horizontalOffset: h, verticalOffset: v, options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: running))`
- drag: `ScrollContentPresenter.Managed.cs:862-866`
  `Set(horizontalOffset: HorizontalOffset + deltaX, verticalOffset: VerticalOffset + deltaY, zoomFactor: newZoomFactor, options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: true))`

Both take the `horizontalOffset is double` branch (`Set`, lines 339-355) *and* the
`verticalOffset is double` branch (357-371). The drag additionally passes `zoomFactor` (usually
`null`, so the `zoomFactor is float zoom` branch at 326 is skipped — but the extra nullable check
exists only on the drag side). Net: identical work, drag marginally heavier.

Both then reach the same `if (updated || options.IsTouch)` → `Update(...)` at 418-424 — note
`options.IsTouch` is true for *both*, so `Update` runs unconditionally on both paths even when the
offset did not change.

### 1.2 "`Scroller?.ScrollableWidth`/`ScrollableHeight` are read every fling frame; do they force layout?" — NO

`ScrollableHeight`/`ScrollableWidth` are read-only DPs:
`ScrollViewer.cs:527-539` and `541-556` — plain `(double)GetValue(...Property)`. They are *written*
from `UpdateDimensionProperties` (`ScrollViewer.cs:782, 786`), which runs from `AfterArrange`
(`ScrollViewer.cs:672-689`). **The getter does not force layout and does not allocate** (`GetValue`
returns a cached boxed `double`; unboxing is not an allocation).

Count per fling frame: 2 in `OnFlingFrame` (617-618) + 2 in `Set` (345, 361) = 4 DP reads.
Count per drag frame: 2 in `Set` + `GetScrollableOffsets()` (`ScrollContentPresenter.Managed.cs:73-104`),
which reads 8 CLR properties and allocates nothing (`ScrollableOffsets` is a `record struct`, 1058).
Both are order-of-100ns. Not the story. (Cosmetic: `OnFlingFrame`'s two reads are redundant with
`Set`'s two — worth collapsing, but it buys nothing measurable.)

### 1.3 "Does the fling driver's `Set` path allocate per frame?" — NO, and less than the drag path

- `ScrollOptions` is a `record struct` (`ScrollContentPresenter.Managed.cs:1110`) — no box.
- `[CallerMemberName]`/`[CallerLineNumber]` are compile-time literals — no alloc.
- `_trace?.Invoke($"…")` at 397: `_trace` is null unless `LogLevel.Trace` is on (33-35), and the
  null-conditional **short-circuits before evaluating the interpolated argument**. Verified inert.
- `Updated` (436-471): `Interlocked.Increment`, a struct tuple compare, `new Point(...)` (struct).
  The `DispatcherQueue.TryEnqueue` closure at 446-452 is *not* taken — see §2.1, both paths run on
  the UI thread, so `HasThreadAccess` is true and `UpdateOffsets` runs inline.
- `Update` (473-584): `visual.TryGetAnimationController`, two `StopAnimation` calls, `AnchorPoint`
  and `Scale` writes (523-530). No allocation on the `DisableAnimation/IsTouch` branch.

The only per-frame allocations in this chain are downstream and shared by both paths:
`new EffectiveViewportChangedEventArgs(...)` per element (`FrameworkElement.EffectiveViewport.cs:384`)
and the closure + `Predicate<T>` allocated by `_effectiveViewportChangedQueue.RemoveAll(x => x.Element == element)`
(`EventManager.cs:31`), which is also **O(queue length)** per enqueued element.
Worth fixing on general principle, but it is *not* the fling/drag differentiator.

### 1.4 "`ScrollDiagnostics` is compiled in — confirm it is inert when disabled" — CONFIRMED INERT, with a caveat

`ScrollDiagnostics.Record` returns on the first line when disabled (`ScrollDiagnostics.cs:77-82`),
before touching the clock or the lock. `CurrentPhase` is a plain `byte` setter. The
`CompositionTarget.Rendering` diagnostics subscription is only installed when enabled
(`ScrollContentPresenter.Managed.cs:164-172`).

Two notes:

1. The only `ScrollDiagnostics.Record` call in the scroll hot path is on the **drag** side
   (`ScrollContentPresenter.Managed.cs:829-830`). `OnFlingFrame` records nothing. So diagnostics, if
   anything, taxes drag and not fling — the opposite of the reported symptom.
2. **Caveat — enabling diagnostics changes the render scheduling being measured.** Subscribing to
   `CompositionTarget.Rendering` sets `_isRenderingActive` (`CompositionTarget.Rendering.skia.cs:90-96`),
   which makes `Render()` call `RequestNewFrame()` on *every* frame (164-167) and enqueue a
   High-priority `RaiseRendering` per recorded frame (445-449), which allocates a `FramePicture[]`,
   a `List<(Window, object)>` and a `RenderingEventArgs` every frame (459-472). That is exactly the
   `RequestNewFrame`-every-frame behaviour that §2 identifies as fling-only in production —
   **so with diagnostics on, drag inherits the fling's scheduling regime, and the captured jerk
   numbers understate the production drag/fling gap.** Any A/B based on `EnableDiagnostics` should be
   read with this in mind.

### 1.5 "Is anything O(realized visuals) that runs during fling but not during drag?" — NO

The one O(subtree) walk in the write path is `ContainerVisual.SetMatrixDirty()`
(`ContainerVisual.skia.cs:212-227`), reached from `Compositor.InvalidateRenderPartial`
(`Compositor.skia.cs:297-302`) on the `AnchorPoint` write. It is guarded by the
already-dirty check in `Visual.SetMatrixDirty` (`Visual.skia.cs:140-146`) — it recurses only on the
clean→dirty transition. One recursion per frame in both modes. During a drag there are typically
*several* `AnchorPoint` writes per frame (one per pointer sample, 120-240 Hz vs. 60-90 Hz frames), so
drag does one recursion + N cheap early-outs; fling does exactly one recursion. Drag ≥ fling.

`InvalidatePaint` / `InvalidateParentChildrenPicture` (`Visual.skia.cs:234-254`) are O(depth to root),
identical on both paths.

The effective-viewport fan-out (`FrameworkElement.EffectiveViewport.cs:338-420`) is O(subscribed
descendants) and runs once per `Set` on both paths, via `Updated` → `InvalidateViewport` (469 → 256-266).

### 1.6 "Does the GestureRecognizer inertia processor also tick during the fling?" — NO

`ScrollContentPresenter` calls `recognizer.CompleteGesture()` **before** `StartFling`
(`ScrollContentPresenter.Managed.cs:1006-1007`). In `GestureRecognizer.Manipulation.cs:397-401`,
`_inertia.Start(...)` is guarded by `if (_status is ManipulationStatus.Inertia) // The manipulation
might have been completed in the event handler`. So the processor's own timer
(`GestureRecognizer.Manipulation.InertiaProcessor.cs:342-367`, which on Skia is *also* a
`Compositor.FrameStarting` subscriber) is never started. Exactly one `FrameStarting` subscriber per
fling. No double-tick.

### 1.7 Minor, non-causal

`ScrollFlingSimulation.Duration` recomputes `Math.Log(...) / Math.Log(AppleDrag)` on **every access**
on the Apple branch (`ScrollFlingSimulation.cs:77-79`), and `OnFlingFrame` reads it twice per frame
(624). Two `Math.Log` per frame on iOS/macOS. Cache it for tidiness; it is not measurable.

---

## 2. What actually differs: the fling is the only mode that makes `FrameStarting` non-null

### 2.1 Where each mode's `Set` runs

- **Drag** — Android delivers `MotionEvent`s synchronously on the UI thread's native message pump:
  `ApplicationActivity.DispatchTouchEvent` (`ApplicationActivity.cs:187-212`) →
  `AndroidCorePointerInputSource.OnNativeMotionEvent` (`AndroidCorePointerInputSource.cs:71-119`) →
  … → `IDirectManipulationHandler.OnUpdated` → `Set`. This is **outside** the `NativeDispatcher`
  queue and **outside** `Render()`.
- **Fling** — `OnFlingFrame` is invoked from `Compositor.RenderRootVisual`
  (`Compositor.skia.cs:226-243`), which is called from inside the picture recording
  (`SkiaRenderHelper.skia.cs:40-50`, `RenderRootVisual` at line 44 sits between `BeginRecording` and
  `EndRecording`), which is called from `CompositionTarget.Render()`
  (`CompositionTarget.Rendering.skia.cs:110-198`, UI thread — `NativeDispatcher.CheckThreadAccess()`
  at 114).

So the fling's offset is produced *inside* the record, and its value is a function of
`Compositor.TimestampInTicks` sampled at `Compositor.skia.cs:230` — i.e.
`Stopwatch.GetTimestamp()` (`Compositor.cs:38`). **Not a vsync timestamp, not a presentation
timestamp: the wall clock at whatever moment the UI thread got around to starting the record.**

### 2.2 The fling-only branch: `RequestNewFrame` at the end of every record

`Compositor.skia.cs:291-294`:

```csharp
if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
{
    rootVisual.CompositionTarget?.RequestNewFrame();
}
```

- During a **fling**, `FrameStarting is not null` (subscribed at `ScrollContentPresenter.Managed.cs:598`)
  → this fires on **every** record.
- During a **drag**, `FrameStarting` is null, `_runningAnimations` is empty, `_backgroundTransitions`
  is empty → this **never** fires. (`_isRenderingActive` at `CompositionTarget.Rendering.skia.cs:164`
  is likewise false in a plain `ListView`/`ScrollViewer` — the `CompositionTarget.Rendering`
  subscribers in the tree are `ItemsRepeater`/`ScrollView`/`ScrollPresenter` paths, not the classic
  ones. It is true when `ScrollDiagnostics` is enabled — see §1.4.)

This is the only per-frame branch found that is taken during a fling and not during a drag.

### 2.3 Why that matters: it eats the vsync-aligned record

The render state machine has an "ahead of time" path
(`CompositionTarget.RenderScheduling.skia.cs:178-208`), driven from the layout tick:
`CoreServices.OnTick` (`CoreServices.cs:77-127`) does `root.UpdateLayout()` then calls
`OnRenderFrameOpportunity()` at line 124. `OnTick` is a **Normal**-priority dispatcher item scheduled
by `CoreServices.RequestAdditionalFrame()` (`CoreServices.cs:67-75`), and the fling *causes* that
scheduling every frame: `Set` → `Updated` → `InvalidateViewport` →
`PropagateEffectiveViewportChange` → `EventManager.EnqueueForEffectiveViewportChanged`, which calls
`CoreServices.RequestAdditionalFrame()` at `EventManager.cs:34`.

Sequence during a fling (all on the UI thread):

1. `OnRenderFrameOpportunity` sees `RenderRequested && !_renderedAheadOfTime` and the tree clean
   (`CanRecordPicture`, `SkiaRenderHelper.skia.cs:33-34` — clean because `UpdateLayout()` just ran).
   It sets `_renderedAheadOfTime = true` **before** calling `Render()` (192-205).
2. `Render()` → record → `RenderRootVisual` → samples the clock (230) → `OnFlingFrame` → `Set`.
3. End of `RenderRootVisual`: line 291 fires (fling ⇒ `FrameStarting != null`) →
   `RequestNewFrame()` → `_renderedAheadOfTime` is true ⇒
   **`_renderRequestedAfterAheadOfTimePaint = true`** (`RenderScheduling.skia.cs:98-101`). Guaranteed,
   every time.
4. Next vsync: the Android render thread presents the last picture and posts `EnqueueRender`
   (`UnoSKVulkanView.cs:199-227` → `CompositionTarget.OnNativePlatformFrameRequested`, 166-176).
   `EnqueueRenderCallback` runs, sees `_renderedAheadOfTime` **and**
   `_renderRequestedAfterAheadOfTimePaint`, and takes the branch commented
   *"Doing nothing this tick and rescheduling another tick"* (`RenderScheduling.skia.cs:131-141`).
   **No record is produced for that vsync.**
5. The same picture is presented again, and the next record happens whenever the loop comes back
   round — one vsync later, or at the next `OnTick` opportunity.

During a **drag**, step 3 does not happen from the compositor; `_renderRequestedAfterAheadOfTimePaint`
is set only if a *new pointer sample* arrives after the ahead-of-time record — and in that case the
skipped vsync is benign, because the picture already carries the newest finger position.

### 2.4 Why this reads as "less smooth" only for inertia

`ScrollFlingSimulation` is deliberately **analytic in absolute time**
(`ScrollFlingSimulation.cs:12-16, 82-97`) — a late frame yields the correct position rather than
accumulating error. That is correct if the sampled time is the *presentation* time. It is not: it is
the record-start wall clock (§2.1). So the position presented at vsync `T` is `x(T − δ)`, where
`δ = T − t_record`, and §2.3 makes `δ` swing by up to a full frame interval, frame to frame,
alternating between "ahead-of-time record from a Normal dispatcher item" and "record at the
vsync-aligned render slot" — plus an occasional fully duplicated frame followed by a
double-distance catch-up.

At 2000 px/s a 5 ms wobble in `δ` is 10 logical px of position error per frame. That is exactly the
signature of "moves, but not glass".

**Drag is structurally immune.** Its written value is a pointer coordinate, not a function of the
clock. Record it early, late, twice, or skip a vsync — the value written is the same, so `δ` shows up
only as *latency*, and latency against your own finger is invisible. This is why every fix so far
(quantization, velocity fit, curve shape, Choreographer pacing) improved inertia without closing the
gap: they all corrected `x(t)`, and the defect is in `t`.

### 2.5 Secondary contributor (same root, worth noting)

`NativeDispatcher.TryGetRenderAction` (`NativeDispatcher.cs:206-234`) throttles the render slot behind
the Normal queue: when a render action is dequeued it re-arms
`normalItemsToProcessBeforeNextRenderAction = _queues[Normal].Count` (216), decremented one per Normal
item dispatched (156-165). During a fling the record itself enqueues Normal items — `OnTick` via
`EventManager.cs:34`, and `ScrollViewer.RequestUpdate`'s `Dispatcher.RunAsync(CoreDispatcherPriority.Normal, …)`
(`ScrollViewer.cs:1301-1316`) — *after* that snapshot was taken. Those items therefore inflate the
barrier for the *following* render, adding another variable-length delay between vsync and record.
Same class of defect: it perturbs `t`, not `x`.

---

## 3. Smallest proofs

Ordered cheapest-first. (1) alone should settle it.

1. **Instrument `δ` directly, no behaviour change.** In `Compositor.RenderRootVisual`
   (`Compositor.skia.cs:230`) record `frameTimestamp`; in `UnoSKVulkanView.RenderFrame`
   (`UnoSKVulkanView.cs:199-227`), just after `OnNativePlatformFrameRequested` returns, record the
   clock again. Log `present − record` per frame for a 1 s drag and a 1 s fling. Prediction: during a
   drag the distribution is irrelevant (the offset doesn't depend on it); during a fling the spread of
   `δ` is ≥ half a frame interval, with periodic ~2-frame outliers where
   `_renderRequestedAfterAheadOfTimePaint` skipped a record. Add a counter on the
   `RenderScheduling.skia.cs:135` "doing nothing this tick" branch and confirm it is ~non-zero during
   fling and ~zero during drag.

2. **One-line falsification of §2.2/§2.3.** Change `Compositor.skia.cs:291` from
   `FrameStarting is not null` to `false` (temporarily; the fling already re-requests via the
   `AnchorPoint` write → `InvalidateRenderPartial` → `RequestNewFrame`, `Compositor.skia.cs:297-302`,
   so frames should keep coming). If inertia visibly improves, the ahead-of-time/skip interaction is
   confirmed as a real contributor. If it does not, §2.3 is not the dominant term and §2.4's timing
   story stands on §2.1 alone.

3. **Decisive fix-shaped test for §2.4.** Give `OnFlingFrame` a *predicted presentation* timestamp
   instead of `TimestampInTicks`: pass `frameTimestamp + oneVsyncInterval` (Android: from
   `Choreographer`'s `frameTimeNanos`, already available in
   `ChoreographerFramePacer.FrameCallback.DoFrame`, `ChoreographerFramePacer.cs:99`). If the fling
   smooths out, the mechanism is proven and the shipping fix is "drive `FrameStarting` from a
   vsync-derived clock, not `Stopwatch.GetTimestamp()`".

4. **Cheap negative control.** Comment out `Updated(...)` at
   `ScrollContentPresenter.Managed.cs:529` for one build (breaks virtualization; visual test only) so
   the fling stops scheduling `OnTick`/`RequestUpdate`. If smoothness improves markedly, §2.3/§2.5's
   dispatcher-churn path is load-bearing; if not, the pure clock-sampling story (§2.4) is the whole
   of it.

---

## 4. One-paragraph summary for the spec

The remaining fling/drag gap is not per-frame cost. Drag and fling execute the same `Set` → `Update`
→ `Updated` chain with the same arguments (both axes, `IsTouch: true`), and the drag path runs it
*more* often per frame. The difference is that a fling is the only scroll mode with a
`Compositor.FrameStarting` subscriber, and (a) its offset is `x(t)` where `t` is
`Stopwatch.GetTimestamp()` sampled at record start rather than at presentation, and (b) that same
non-null `FrameStarting` makes `Compositor.RenderRootVisual:291` request a new frame at the end of
every record, which — via `_renderedAheadOfTime` /
`_renderRequestedAfterAheadOfTimePaint` — deterministically turns every ahead-of-time fling record
into a skipped vsync-aligned record. Drag's written value does not depend on when the record ran, so
it absorbs all of this as invisible latency; the fling converts it directly into velocity error.
