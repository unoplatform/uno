# Drag vs. fling write path — line-by-line diff

Scope: everything between "a new scroll offset is decided" and "the pixels are on the glass", for
the two touch paths on Skia (Android reference platform).

Worktree: `D:/Work/uno-worktrees/scrollsmooth`, branch `dev/mazi/smooth-scroll`.
All line numbers are as-of this reading. Claims I could not confirm in code are marked **UNVERIFIED**.

---

## 0. TL;DR — the one difference that matters

Both paths end in the same `Set(...)` → `Update(...)` → `visual.AnchorPoint = target`.
The bodies are, for practical purposes, identical. The difference is **what the written value is a
function of**:

| | value written is a function of | who chooses the sampling instant |
|---|---|---|
| **Drag** | the *finger position* carried by the last `MotionEvent` | Android's input pipeline (regular, hardware-paced) |
| **Fling** | `Stopwatch.GetTimestamp()` **read at record time** (`Compositor.skia.cs:230`) | the .NET UI-thread scheduler (irregular) |

A drag frame that is recorded 4 ms early or 4 ms late paints **the same pixels** — the finger sample
did not change, only the latency did. A fling frame recorded 4 ms early or late paints a **different
position**, off by `v·Δt`. At 3000 px/s a ±4 ms record-phase wobble is ±12 px of positional noise per
frame — while the *presentation* cadence stays locked to vsync by
`ChoreographerFramePacer.WaitForNextFrame()` (`UnoSKVulkanView.cs:158`).

That is the asymmetry. Everything below is the enumeration that supports it, plus four smaller
fling-only differences that are independently worth fixing.

---

## 1. Call-site diff

### Drag
```
ApplicationActivity.DispatchTouchEvent            ApplicationActivity.cs:187-211   (Android UI thread, synchronous)
 -> AndroidCorePointerInputSource.OnNativeMotionEvent                  AndroidCorePointerInputSource.cs:71
 -> PointerMoved -> InputManager -> GestureRecognizer.Manipulation.Update
 -> Manipulation.NotifyUpdate                     GestureRecognizer.Manipulation.cs:332
 -> SCP.IDirectManipulationHandler.OnUpdated      ScrollContentPresenter.Managed.cs:793
 -> Set(h+dx, v+dy, zoom, new(DisableAnimation:true, IsTouch:true, IsIntermediate:true))   :862-866
```

### Fling
```
UnoSKVulkanView.RenderLoop (render thread)        UnoSKVulkanView.cs:143-159
 -> CompositionTarget.OnNativePlatformFrameRequested                 CompositionTarget.RenderScheduling.skia.cs:166
 -> NativeDispatcher.Main.EnqueueRender(...)      RenderScheduling.skia.cs:172         [hop to UI thread]
 -> NativeDispatcher.TryGetRenderAction           NativeDispatcher.cs:206-234          [*** variable delay ***]
 -> CompositionTarget.EnqueueRenderCallback -> Render()              CompositionTarget.Rendering.skia.cs:110
 -> SkiaRenderHelper.RecordPictureAndReturnPath   Rendering.skia.cs:119
 -> Compositor.RenderRootVisual                   Compositor.skia.cs:219
 -> frameTimestamp = TimestampInTicks             Compositor.skia.cs:230               [*** the clock read ***]
 -> FrameStarting(frameTimestamp)                 Compositor.skia.cs:234
 -> SCP.OnFlingFrame                              ScrollContentPresenter.Managed.cs:613
 -> Set(h, v, new(DisableAnimation:true, IsTouch:true, IsIntermediate:running))         :632
```

Alternate fling entry (fires *in addition*, see §5):
```
CoreServices.OnTick -> root.UpdateLayout()        CoreServices.cs:115
 -> CompositionTarget.OnRenderFrameOpportunity    RenderScheduling.skia.cs:178-208
 -> Render() -> ... -> FrameStarting -> OnFlingFrame   with a *second, later* timestamp
```

---

## 2. Inside `Set()` — where the two differ, statement by statement

`Set(...)` is `ScrollContentPresenter.Managed.cs:313-433`.

| Statement | line | Drag | Fling | Same? |
|---|---|---|---|---|
| zoom clamp block | 326-337 | `zoomFactor` is usually `null`; non-null only during pinch | always `null` | same in the common case |
| `maxOffset` source (h) | 343-345 | `Scroller.ScrollableWidth` | `Scroller.ScrollableWidth` | **same** |
| `ValidateInputOffset` | 346 / 362 | `Max(0, Min(o,max))` — no rounding, no quantization | identical | **same** |
| `NumericExtensions.AreClose` gate | 350, 366 | eps = `(|a|+|b|+10)·2.22e-16` — effectively exact equality (`NumericExtensions.cs:95-105`) | identical | **same — ruled out as a quantizer** |
| requested value | — | `HorizontalOffset + deltaX` (**relative**, delta already clamped to `GetScrollableOffsets()` at :801-802) | `Clamp(_flingH.GetPosition(elapsed), 0, maxH)` (**absolute**) | **differs** — see §4.2 |
| `_touchInertia?.Complete()` | 399-404 | skipped (`IsTouch`) | skipped (`IsTouch`) | **same** |
| `StopWheelDecay()` | 406-409 | called (no-op) | called (no-op) | **same** |
| `StopFling()` | 411-414 | skipped (`IsTouch`) | skipped (`IsTouch`) | **same** |
| `updated \|\| options.IsTouch` → `Update(...)` | 418-424 | always true → always writes | always true → always writes | **same** |
| `Scroller.OnPresenterZoomed` | 427-430 | only on pinch (→ `Update(isIntermediate:false)` → `InvalidateArrange`) | never | differs, but not on a plain vertical drag |

**Conclusion: `Set` itself is not the asymmetry.** Both take the identical branch.

## 3. Inside `Update()` — same branch, same writes

`Update(...)` is `:473-584`.

* Early-return guard `:494-520` (keep a nearly-finished `AnchorPoint` animation alive): requires
  `visual.TryGetAnimationController(nameof(AnchorPoint))` to be non-null. On the fling path,
  `OnCompleted` already ran `StopAnimation` (`:525`, via the `Set` at `:1026`) before `StartFling`,
  so no controller exists. On the drag path the same is true after the first move. **Not taken in
  either path.**
* `:523` `options is { DisableAnimation: true } or { IsTouch: true }` — **both** paths satisfy this
  twice over. So both run, in this exact order:
  * `visual.StopAnimation(nameof(Visual.AnchorPoint))` `:525`
  * `visual.StopAnimation(nameof(Visual.Scale))` `:526`
  * `visual.AnchorPoint = target` `:527`  ← **exactly one write per `Set` call, in both paths**
  * `visual.Scale = targetScale` `:528`
  * `Updated(h, v, options.IsIntermediate)` `:529`
* Value conversion `:487-489`: `(float)(-offset + centeringOffset)` — identical, no rounding.
  (`Math.Round` appears only in the *animated* branch, `:563-564`, which neither path uses.)

**Writes per frame**
* Drag: one write per delivered `MotionEvent` that produces a manipulation update. Uno reads only
  the current sample, never `getHistorical*`, so it is one write per delivered event. On Android
  moves are batched and delivered once per Choreographer input phase → **1 write/frame**
  (**UNVERIFIED** on 120/240 Hz digitizers; multi-touch is explicitly one update per pointer,
  `AndroidCorePointerInputSource.cs:98-103`).
* Fling: exactly one write per `Compositor.RenderRootVisual` call — but that is **not** the same as
  once per presented frame (§5).

## 4. Fling-only differences, ranked

### 4.1 The value is sampled from the wall clock at record time (**primary**)

`Compositor.skia.cs:228-231` reads `TimestampInTicks`, which is
`Stopwatch.GetTimestamp()` (`Compositor.cs:38`) — i.e. *now*, on the UI thread, at the moment the
picture starts being recorded. `OnFlingFrame` (`:615`) turns that straight into
`elapsed` and evaluates the analytic curve at it.

Three independent sources make that instant move relative to the display's present instant:

1. **Dispatcher gating.** `NativeDispatcher.TryGetRenderAction` (`NativeDispatcher.cs:206-234`)
   only releases the render action once `normalItemsToProcessBeforeNextRenderAction` reaches 0, and
   that counter is re-seeded to *the current Normal-queue depth* every time a render is consumed
   (`:216`). Both scroll paths keep pushing Normal items every frame — `RequestUpdate` →
   `Dispatcher.RunAsync(Normal, …)` (`ScrollViewer.cs:1308`) and `CoreServices.RequestAdditionalFrame`
   → `Enqueue(OnTick, Normal)` (`CoreServices.cs:73`) — so the delay before the record varies with
   whatever else the app queued that frame.
2. **Ahead-of-time records.** `OnRenderFrameOpportunity` (`RenderScheduling.skia.cs:178-208`) can
   pull the record arbitrarily far forward, to right after `UpdateLayout` (`CoreServices.cs:115-125`).
3. **Record→present decoupling.** `Render()` deposits the picture in `_lastRenderedFrame`
   (`Rendering.skia.cs:147`); the render thread presents whatever is in that slot at the next vsync
   (`Draw`, `:232-241`; pacing at `UnoSKVulkanView.cs:158`). Record-to-present latency is therefore
   0..1 frame, not a constant.

Drag is immune to all three because its written value does not depend on when the record happens.
Fling converts each of them into `Δx = v·Δt`.

### 4.2 There is no reference signal to correct against

Drag re-derives position from the finger every event (`:863-864`, `HorizontalOffset + deltaX` where
`deltaX` comes from `unhandledDelta.Translation`), so any timing error is a pure latency and never
accumulates or shows as velocity noise. The fling's position comes only from `elapsed`
(`:615, 620-621`); there is nothing to re-anchor to, so timing noise *is* the signal.

### 4.3 A non-intermediate `Set` fires at the *start* of the fling → `InvalidateArrange` at peak velocity

`OnInertiaStarting` calls `recognizer.CompleteGesture()` at `:1006`, **before** `StartFling` at `:1007`.
`Manipulation.Complete()` (`GestureRecognizer.Manipulation.cs:277-306`) is in status `Started`, so
`isInertial` is **false**, so `SCP.OnCompleted`'s early-out at `:1017` does not fire and it runs:

```csharp
Set(options: new ScrollOptions(DisableAnimation: true, IsTouch: true, IsIntermediate: false));  // :1026
```

`IsIntermediate:false` → `Updated(..., false)` (`:529`) → `Scroller.OnPresenterScrolled(..., false)`
(`:462`) → the **synchronous** branch `ScrollViewer.cs:1246` `Update(isIntermediate:false)` →
`InvalidateArrange()` at `ScrollViewer.cs:1336` (offsets will differ, because every intermediate
frame before it only went through the deferred `RequestUpdate` path at `ScrollViewer.cs:1241`), plus
`_snapPointsTimer.Start()` at `:1275` when snap points are configured.

So a full arrange pass is scheduled at exactly the instant the finger lifts and the content is
moving fastest. Drag never takes the non-intermediate path mid-gesture. The last fling frame does
the same thing (`:632` with `running == false`), which is fine — but the one at launch is not.

### 4.4 The fling can abort itself; a drag cannot

`OnFlingFrame:624-625`:
```csharp
var running = elapsed < Math.Max(_flingH.Duration, _flingV.Duration)
    && (h > 0 && h < maxH || v > 0 && v < maxV);
```
`maxV` is read fresh every frame from `Scroller.ScrollableHeight` (`:618`). In a virtualized list
that value is an *estimate* that moves as containers realize. If it momentarily lands at or below
the current `v`, the clamp at `:621` pins `v` to `maxV`, `v < maxV` goes false, `running` goes false,
and `StopFling()` runs (`:629`) — the fling dies mid-flight and simultaneously emits a
non-intermediate `Set` (→ `InvalidateArrange`). Drag has no such predicate: it just re-clamps a
delta each event and keeps going.

Note also the clamp inconsistency: `OnFlingFrame:617-618` falls back to
`Math.Max(0, ExtentWidth - ViewportWidth)` while `Set:345` falls back to
`ExtentWidth - ViewportWidth` (no `Max(0,…)`). Harmless today because `ValidateInputOffset` clamps to
`[0,max]` anyway, but the two are not the same expression.

### 4.5 The launch velocity is fitted against the *arrival* clock, not the input clock

`:834-836`:
```csharp
_velocityTracker.AddPosition(
    Visual.Compositor.TimestampInTicks / (double)TimeSpan.TicksPerMillisecond,
    args.Position);
```
`PointerPoint` carries a hardware `Timestamp` in microseconds and the manipulation itself uses it
(`GestureRecognizer.Manipulation.cs:447, 459, 466`), but the tracker is fed
`Stopwatch.GetTimestamp()` at the moment the managed event ran. Android batches moves, so several
samples that were captured milliseconds apart get near-identical time coordinates while the gap to
the previous batch is inflated. The quadratic fit then sees a distorted time axis. This does not
explain the *smoothness* asymmetry (it is a one-shot error at launch, and it can only make the
launch speed wrong, not jittery), but it does make fling distance inconsistent between flicks.

---

## 5. Frame accounting: `FrameStarting` ticks are not 1:1 with presented frames

This is the mechanism that turns 4.1's "the clock read moves around" into a *visible, repeating*
pattern rather than mild noise, and it only engages when something invalidates layout during the
scroll — i.e. exactly the virtualized-list case.

1. `Render()` #1 runs. `FrameStarting` → `OnFlingFrame(T0)` → `Set` → `Updated` → `InvalidateViewport`
   (`:469`) → `PropagateEffectiveViewportChange` (`FrameworkElement.EffectiveViewport.cs:265`) →
   `EventManager.EnqueueForEffectiveViewportChanged` → `CoreServices.RequestAdditionalFrame()`
   (`EventManager.cs:34`) → `Enqueue(OnTick, Normal)`.
   **The EVP event is queued, not raised** — it is only raised from `UpdateLayout`
   (`UIElement.cs:980-983`). So the paint walk that immediately follows at `Compositor.skia.cs:270`
   paints the **pre-realization** tree.
2. Tail of `RenderRootVisual` (`Compositor.skia.cs:291-294`): `FrameStarting is not null` → request
   another frame. (`Compositor.IsAnimating` is also true for the whole fling, `:43`.)
3. Dispatcher runs the queued `OnTick` → `UpdateLayout()` raises EVP, panels invalidate, measure /
   arrange run → then `OnRenderFrameOpportunity()` (`CoreServices.cs:124`) →
   `RenderRequested && !_renderedAheadOfTime` → **`Render()` #2 at `T0+δ`**, `δ` = the layout cost.
   `FrameStarting` fires **again**, so `OnFlingFrame` ticks a second time with a fresh timestamp, and
   record #2 overwrites record #1 in `_lastRenderedFrame`.
4. `_renderedAheadOfTime` is now true, so the next `EnqueueRenderCallback`
   (`RenderScheduling.skia.cs:131-144`) **does not render**; it only clears the flag and re-requests.
   Meanwhile `Draw` borrows the slot and `ReturnFrame` (`Rendering.skia.cs:412-434`) puts the same
   picture back → **the same picture is presented twice**.

Net effect during a fling over a virtualized list: alternating short/long effective sample
intervals, one duplicated presented frame per cycle, and newly-realized rows always one frame late.

Under drag the identical scheduling happens — but step 1's write is finger-derived, so record #1 and
record #2 contain **the same** `AnchorPoint`, and the duplicated present is invisible. Step 1 also
happens *before* `UpdateLayout` on the drag path (input dispatch is a plain synchronous UI-thread
callback, `ApplicationActivity.cs:187-211`), so the drag's paint walk sees the realized items;
the fling's never can, because its write is emitted *inside* the record.

---

## 6. Things I checked and can rule out

* **Quantization / rounding.** `ValidateInputOffset` (`ScrollContentPresenter.cs:373-381`) is a plain
  clamp; `AreClose` (`NumericExtensions.cs:95-105`) is machine-epsilon; the float cast at `:487-489`
  is the same in both paths. No 2 px grid remains anywhere on either path.
* **Different `Update()` branch.** Both take `:523`. Both call both `StopAnimation`s.
* **`Updated()` running on a different thread.** `Render()` asserts UI-thread affinity
  (`Rendering.skia.cs:114`), so `NativeDispatcher.Main.HasThreadAccess` at `:440` is true for the
  fling exactly as it is for the drag → both take the synchronous `UpdateOffsets` branch, no
  `DispatcherQueue.TryEnqueue` hop.
* **Damage-region clipping of a write made inside the record.** Damage is derived during the walk
  from `_lastRenderBounds` (`Visual.skia.cs:313-320`) and `_pendingDamage` is snapshotted *after*
  `RecordPictureAndReturnPath` returns (`Rendering.skia.cs:145`), so a `FrameStarting` write is
  correctly reflected. Not a suspect.
* **`Set` stopping the fling reentrantly.** `options.IsTouch` is true on the fling's own `Set`, so
  `:411-414` does not call `StopFling` on itself.

Minor, non-behavioural: `FrameStarting` handlers are wrapped in `try/catch` that swallows to the log
(`Compositor.skia.cs:236-242`), so an exception in `OnFlingFrame` degrades to a silently frozen
fling; the drag path would surface it.

---

## 7. What would prove it

Cheapest discriminating measurement, no behaviour change:

Record, per presented frame, the triple `(frameTimestamp, presentTimestamp, anchorY)` — the first is
already available as `Compositor.CurrentFrameTimestampInTicks` (`Compositor.skia.cs:214`), the second
can be taken in `UnoSKVulkanView.RenderFrame` right after the pacer returns. Then:

* If `presentTimestamp` deltas are near-constant (vsync-locked, as they should be with the pacer)
  **while** `frameTimestamp` deltas are not, and `anchorY` deltas track `frameTimestamp` deltas
  rather than `presentTimestamp` deltas — hypothesis 4.1 is confirmed and the jerk is exactly
  `v·std(frameTimestamp − presentTimestamp)`.
* The one-line fix to test it: in `OnFlingFrame`, replace `timestampInTicks` with a *predicted
  present* time (`lastPresentTimestamp + k·vsyncInterval`) and check whether the reported roughness
  disappears. If it does not, 4.1 is not the dominant term and §5's duplicated-present pattern
  (countable directly: log every `Render()` and every `Draw()` that re-presents the same
  `FramePicture`) is the next candidate.
* Independent, even cheaper counter-check for §5: run the fling over a **non-virtualized** content
  (a tall `StackPanel` in a `ScrollViewer`, no `ListView`). If inertia is smooth there and rough on a
  `ListView`, the dominant term is the layout-driven double-record path in §5, not the clock read.
  Doing both experiments separates the two mechanisms cleanly.
