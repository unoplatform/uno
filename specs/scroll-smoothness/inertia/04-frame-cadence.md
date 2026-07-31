# 04 — Frame cadence: does a frame requested from *inside* a frame behave differently?

**Question asked:** during a drag, frames are requested by input events (outside `Render()`); during a
fling, frames are requested from inside `Render()` (`Compositor.RenderRootVisual` re-requests while
`FrameStarting` has subscribers, and `OnFlingFrame`'s `Set` invalidates). Does that self-sustaining
loop produce a different cadence?

**Answer: yes, and the difference is structural, not statistical.** During a fling the picture is
recorded by a *different scheduler path* than during a drag — not by the vsync-aligned render action,
but by the Normal-priority layout tick, on **every** frame, by construction. The vsync-aligned render
action is deliberately turned into a no-op. This is visible entirely in code.

Everything below is read from the worktree at `dev/mazi/smooth-scroll`. Claims are cited `file:line`.
Anything I could not establish from code is marked **UNVERIFIED**.

---

## 1. The state machine, precisely

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs`

Three flags, all under `_renderingStateGate` (`:69-74`).

### `RequestNewFrame` (`:86-118`) — two different outcomes

```csharp
if (!_renderedAheadOfTime && !RenderRequested)      // :93
{
    RenderRequested = true;
    shouldEnqueue = true;                          // -> host.InvalidateRender()  :110
}
else if (_renderedAheadOfTime)                     // :98
{
    _renderRequestedAfterAheadOfTimePaint = true;  // :100  — NO invalidate
}
```

### `EnqueueRenderCallback` (`:120-157`) — the skip

```csharp
if (_renderedAheadOfTime)                          // :131
{
    _renderedAheadOfTime = false;
    if (_renderRequestedAfterAheadOfTimePaint)     // :134
    {
        _renderRequestedAfterAheadOfTimePaint = false;
        ((ICompositionTarget)this).RequestNewFrame();   // :138 — re-arm, and…
    }
    // …fall through. Render() is NOT called this tick.
}
else if (RenderRequested)                          // :145
{
    RenderRequested = false;
    Render();                                      // :152
}
```

The trace string at `:137` states the intent outright: *"rendered ahead of time and got a new frame
request since. Doing nothing this tick and rescheduling another tick"*.

### `OnRenderFrameOpportunity` (`:178-208`) — the early-record door

```csharp
if (SkiaRenderHelper.CanRecordPicture(ContentRoot.VisualTree.RootElement))   // :185
{
    if (RenderRequested && !_renderedAheadOfTime)  // :192
    {
        RenderRequested = false;
        _renderedAheadOfTime = true;
        Render();                                  // :205
    }
}
```

`CanRecordPicture` = layout is clean (`src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:33-34`).

**So: a `RequestNewFrame()` issued from inside a `Render()` that was entered via
`OnRenderFrameOpportunity` is swallowed (`:98-101`) and converted into a *deliberate skip* of the next
vsync-aligned render (`:131-139`). A `RequestNewFrame()` from an input event outside `Render()`
normally takes the `:93` branch and invalidates immediately.**

That is a literal answer to the question. But it is not yet the asymmetry — an input event that
happens to land while `_renderedAheadOfTime` is true takes the swallowed branch too. The asymmetry is
in *who arms `RenderRequested`*.

---

## 2. The asymmetry: `RenderRequested` has a different source in the two modes

### Fling — armed from inside `Render()`, every frame, unconditionally, twice

`src/Uno.UI.Composition/Composition/Compositor.skia.cs`

1. `RenderRootVisual` raises `FrameStarting` (`:226-243`) → `OnFlingFrame`
   (`ScrollContentPresenter.Managed.cs:615-635`) → `Set` → `Update` → `visual.AnchorPoint = target`
   (`ScrollContentPresenter.Managed.cs:527`).
   `AnchorPoint`'s setter is `SetProperty` (`Visual.skia.cs:266-273`) → `OnPropertyChangedCore` →
   `Compositor.InvalidateRender(this)` (`Visual.cs:194`) → `InvalidateRenderPartial`
   (`Compositor.skia.cs:297-302`) → **`visual.CompositionTarget?.RequestNewFrame()`** (`:301`).
2. And again at the bottom of the same `RenderRootVisual`:
   ```csharp
   if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)  // :291
   {
       rootVisual.CompositionTarget?.RequestNewFrame();                                     // :293
   }
   ```
   `FrameStarting is not null` is true for the whole fling (subscribed at
   `ScrollContentPresenter.Managed.cs:599`, unsubscribed at `:612`).

⇒ `RenderRequested` is `true` the instant `Render()` returns, on every fling frame, guaranteed.

### Drag — armed only from input, outside `Render()`

During a drag there is **no** `FrameStarting` subscriber (the fling is stopped in `OnStarted`,
`ScrollContentPresenter.Managed.cs:788`; the wheel decay in `Set`, `:406-409`) and no running
`AnchorPoint` animation (the drag path takes the `DisableAnimation/IsTouch` branch,
`:523-530`). So `Compositor.skia.cs:291` is false and `Render()` leaves `RenderRequested == false`.

⇒ `OnRenderFrameOpportunity` (which requires `RenderRequested == true`, `:192`) is a **no-op** unless
a fresh pointer sample arrived since the last record.

---

## 3. What that does to the cadence

`CoreServices.OnTick` (`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:77-127`) is the layout tick:

```csharp
_isAdditionalFrameRequested = 0;          // :79 — re-arms immediately
…
root.UpdateLayout();                      // :115
…
(…CompositionTarget)?.OnRenderFrameOpportunity();   // :124
```

It is enqueued at **Normal** priority by `RequestAdditionalFrame` (`:67-75`), and during *any* scroll
it is enqueued every frame: `Updated` → `InvalidateViewport` (`ScrollContentPresenter.Managed.cs:469`)
→ `PropagateEffectiveViewportChange` (`FrameworkElement.EffectiveViewport.cs:265`) → children are
walked because `_lastScrollOffsets != ScrollOffsets` (`:395`) → `EnqueueForEffectiveViewportChanged`
(`:381`) → **`CoreServices.RequestAdditionalFrame()`** (`EventManager.cs:34`).

The dispatcher deliberately interleaves the two: when a render action is dequeued,
`TryGetRenderAction` sets `normalItemsToProcessBeforeNextRenderAction = _queues[Normal].Count`
(`NativeDispatcher.cs:216`), so the *next* render action cannot run until that many Normal items have
been dispatched (`:214`, decremented at `:156-165`).

### Fling steady state (per display refresh)

| # | Where | What happens |
|---|-------|--------------|
| 1 | render thread, vsync | `OnNativePlatformFrameRequested` → `EnqueueRender(ERC)` (`RenderScheduling:172`), `Draw` presents the previous picture (`:175`) |
| 2 | UI thread, ERC | `_renderedAheadOfTime` is true → clear it, clear `_renderRequestedAfterAheadOfTimePaint`, `RequestNewFrame()`, **skip `Render()`** (`:131-139`) |
| 3 | UI thread, Normal | `OnTick`: `UpdateLayout()` (`CoreServices:115`), then `OnRenderFrameOpportunity()` (`:124`) → `RenderRequested` is true → **`Render()` runs here** (`RenderScheduling:205`), sets `_renderedAheadOfTime = true` |
| 4 | inside that `Render` | `FrameStarting` → `OnFlingFrame(timestamp)` → new offset → `RequestNewFrame` swallowed into `_renderRequestedAfterAheadOfTimePaint` (`:100`); a fresh `OnTick` is enqueued for the next cycle |
| 5 | goto 1 | |

Frame **count** is unchanged (one record per vsync — which is exactly what the `OnRenderFrameOpportunity`
comment at `:180-182` promises). Frame **phase** is not.

### Drag steady state

Step 3 finds `RenderRequested == false` and does nothing. The record happens in step 2's
`else if (RenderRequested) → Render()` (`:145-152`) — i.e. from the render action, immediately after
the vsync that enqueued it. (When a pointer sample happens to arrive before the `OnTick`, the drag
takes the ahead-of-time path too; the difference is that the fling takes it **100 % of the time by
construction**, while a drag takes it only when input arrival and the layout tick coincide.)

---

## 4. Why phase jitter is fatal to inertia and harmless to drag

The fling's recorded position is `x(t_record)` — `OnFlingFrame` reads
`Compositor.CurrentFrameTimestampInTicks` sampled at `Compositor.skia.cs:230`, immediately before the
paint walk. Per §3, `t_record ≈ vsync_n + ercDispatch_n + updateLayoutCost_n`, because the timestamp is
sampled *after* `root.UpdateLayout()` (`CoreServices:115` runs before `:124`).

Presentation is at a fixed cadence — `Draw` at each vsync, and on Android the render thread is now
explicitly paced (`UnoSKVulkanView.cs:158` → `ChoreographerFramePacer.WaitForNextFrame`).

So the displacement the eye sees between two presented frames is

```
Δ_n = x(vsync_n + L_n) − x(vsync_{n−1} + L_{n−1}) = v · (T + L_n − L_{n−1})
```

The error term is the **difference of consecutive layout costs**. Virtualization makes that term
spiky by nature: most frames realize nothing, then one frame materializes a container. A single
5 ms realization spike makes one presented frame ~30 % long and the next ~30 % short — a double
hitch, the classic inertia judder signature.

**Drag is immune because its recorded value is not a function of `t_record`.** `OnUpdated`
(`ScrollContentPresenter.Managed.cs:864-868`) writes `HorizontalOffset + deltaX` from the *pointer
sample*. Whether the record lands at `vsync+1 ms` or `vsync+9 ms`, the value written is the same
last-known finger position. Phase jitter therefore costs drag a small bounded **latency**, not a
**velocity error** — and latency against a finger the user is watching is far less perceptible than a
velocity modulation of content moving on its own. This is the asymmetry the rules asked for: same
irregular frames, but only one of the two modes converts irregularity into a wrong displacement.

---

## 5. Second, independent consequence of the same mechanism: realize-after-record

The ordering inside `OnTick` inverts between the two modes.

- **Drag**: pointer event → `Set(offset_n)` → viewport event enqueued. *Then* `OnTick`:
  `UpdateLayout()` raises `RaiseEffectiveViewportChangedEvents` (`UIElement.cs:980-983`) and loops
  until layout is clean *and* no viewport events are pending (`:950-955`, `:991-996`). *Then*
  `OnRenderFrameOpportunity` records. **The picture is painted with containers realized for
  `offset_n`.**
- **Fling**: `OnTick` runs `UpdateLayout()` first, *then* `OnRenderFrameOpportunity` → `Render` →
  `FrameStarting` → `Set(offset_n)` — the offset is produced *inside* the record. The viewport event
  it enqueues can only be serviced by the *next* `OnTick`. **The picture is painted at `offset_n` with
  containers realized for `offset_{n−1}`.**

Symptom: a one-frame-late leading edge during inertia only (blank/clipped strip at the direction of
travel), never during drag. Independent of the phase jitter above, same root cause: the fling's
position is produced from inside the frame it is drawn into.

---

## 6. Measurement blind spot (important before trusting any existing trace)

`ScrollDiagnostics.IsEnabled` (`ScrollDiagnostics.cs:66`, gated on
`FeatureConfiguration.ScrollViewer.EnableDiagnostics`) makes the presenter subscribe
`CompositionTarget.Rendering` (`ScrollContentPresenter.Managed.cs:171`). That add-handler sets
`_isRenderingActive = true` (`CompositionTarget.Rendering.skia.cs:90-97`), which makes **every**
`Render()` re-request a frame unconditionally:

```csharp
if (_isRenderingActive) { ((ICompositionTarget)this).RequestNewFrame(); }   // Rendering.skia.cs:164-167
```

With diagnostics enabled, a **drag** therefore also becomes self-sustaining, also arms
`RenderRequested` from inside `Render()`, and also drops into the ahead-of-time loop. **The
instrumentation erases the very asymmetry it is meant to measure.** Any jerk comparison collected
with `EnableDiagnostics = true` is measuring a drag that is not the production drag.

(The jerk numbers in the spec — 0.289 → 0.171 for the wheel — are unaffected, since the wheel decay
subscribes `FrameStarting` anyway. **UNVERIFIED**: whether the drag-vs-inertia comparisons in the
existing dumps were collected with diagnostics on. They must have been, since that is the only way
`ScrollDiagnostics.Record` fires.)

---

## 7. What is *not* claimed

- I have not observed this at runtime. The whole chain is established by code reading only.
  **UNVERIFIED**: that the interleave in §3 is the actual steady state on device rather than an
  oscillation between the two paths.
- One transient inefficiency exists but is probably not the felt problem: entering the loop, the ERC's
  `Render()` records a picture that the immediately following ahead-of-time `Render()` overwrites in
  `_lastRenderedFrame` (`Rendering.skia.cs:147`) and releases unpresented (`:151-154`, `:159-162`) — a
  whole paint walk thrown away. In the steady state of §3 the ERC skips, so this costs one wasted
  record per *entry* into the loop, not per frame. **UNVERIFIED** how often the loop is re-entered.
- `Win32WindowWrapper.cs:421` also calls `OnRenderFrameOpportunity`, but only from
  `SynchronousRenderAndDraw` (size/move/show) — irrelevant to scrolling.

---

## 8. Cheapest ways to confirm or kill this

Ordered cheapest first.

1. **No code change at all.** Fling a `ScrollViewer` whose content is a plain non-virtualized
   `StackPanel` with no `EffectiveViewportChanged` subscribers, and compare against the same fling on a
   `ListView`. With no subscribers, `PropagateEffectiveViewportChange` never reaches
   `EnqueueForEffectiveViewportChanged` (`FrameworkElement.EffectiveViewport.cs:381`), so no
   `RequestAdditionalFrame`, no `OnTick`, no `OnRenderFrameOpportunity` — the fling stays on the
   vsync-aligned render action. **Prediction: the StackPanel fling is noticeably smoother than the
   ListView fling.** If they feel identical, this hypothesis is dead.

2. **Two counters.** Increment a counter at `RenderScheduling.skia.cs:152` (ERC path) and another at
   `:205` (ahead-of-time path); dump the split per phase. Prediction: fling ≈ 100 % ahead-of-time,
   drag ≈ 0 %. Also log `frameTimestamp` from `Compositor.skia.cs:230` and check that consecutive
   deltas during a fling are *not* a clean 16.67 ms while the presents are.

3. **One-line refutation test.** Bail out of `OnRenderFrameOpportunity` while a frame driver is
   running — the predicate already exists as `Compositor.HasFrameStartingSubscribers`
   (`Compositor.skia.cs:211`):

   ```csharp
   // RenderScheduling.skia.cs, top of OnRenderFrameOpportunity
   if (Visual/*root*/.Compositor.HasFrameStartingSubscribers) { return; }
   ```

   That forces the fling back onto the vsync-aligned render action (step 2 of §3 stops skipping,
   because nothing sets `_renderedAheadOfTime` any more). If the fling becomes smooth, confirmed. This
   is a probe, not a fix — it trades away the early-record latency win for every driver.

4. If confirmed, the real fix direction is to stop evaluating inertia against wall-clock time at
   record time and instead evaluate it against the **target present time** (vsync + pipeline depth),
   which both platforms already hand us: `Choreographer.FrameCallback.DoFrame(long frameTimeNanos)`
   is currently discarded in `ChoreographerFramePacer.cs:99`. That makes `t` independent of layout
   cost and closes §4 and §5 at once. (Out of scope for this note.)

---

## 9. One-line summary

During a drag, `RenderRequested` is armed by input from outside `Render()`, so the record happens on
the vsync-aligned render action. During a fling it is armed from *inside* `Render()`
(`Compositor.skia.cs:293` and `:301`), which is always true on exit, which makes
`OnRenderFrameOpportunity` fire on every Normal-priority layout tick (`CoreServices.cs:124`), which
makes the next render action deliberately skip its `Render()`
(`CompositionTarget.RenderScheduling.skia.cs:131-139`). The fling is therefore clocked by
`vsync + UpdateLayout cost` instead of by `vsync`, and because inertia position is `x(t_record)` while
drag position is `x(last finger sample)`, that phase jitter becomes a per-frame **velocity error** for
inertia and only a bounded **latency** for drag.
