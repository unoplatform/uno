# Inertia smoothness — hypothesis 02: virtualization cost per frame

**Hypothesis under test:** *a fling moves the content faster than a drag, so it realizes more items
per frame, so it blows the frame budget where a drag does not.*

**Verdict: REFUTED as the primary explanation of the drag/inertia asymmetry.**
The mechanism is real (realization cost *is* proportional to the per-frame offset delta) but it
cannot produce the asymmetry, because **the fling's per-frame delta is bounded above by the drag's
per-frame delta at the moment of release, and decays monotonically from there.** The fling never
realizes more items per frame than the last frame of the drag that launched it.

Two secondary findings survive and are worth keeping:
- the per-`ViewChanged` cost is **dominated by a delta-*independent* term** (`ArrangeElements` over
  every materialized line), which drag pays *more often* than the fling, not less;
- there is a genuine **positive feedback loop** (longer frame → larger delta → more realization →
  longer frame), but its loop gain is ~0.06 in the realistic regime; it only becomes dangerous at
  one **discontinuity**: the `isLargeScroll` full-rebuild cliff.

---

## 1. The path, end to end (verified)

### 1.1 Where a fling frame writes the offset

`ScrollContentPresenter.Managed.cs:613-633` — `OnFlingFrame(long timestampInTicks)`:

```
elapsed = (timestamp - _flingStartTimestamp) / TicksPerSecond          // :615
h/v     = clamp(_flingH/_flingV.GetPosition(elapsed), 0, max)          // :620-621
Set(h, v, options: new(DisableAnimation:true, IsTouch:true, ...))      // :632
```

This handler is subscribed to `Compositor.FrameStarting`
(`ScrollContentPresenter.Managed.cs:598`), which is raised **inside**
`Compositor.RenderRootVisual` (`Uno.UI.Composition/Composition/Compositor.skia.cs:226-235`),
immediately before the paint walk at `:270`. So the offset write happens inside the render/record
action.

### 1.2 Where the drag writes the offset

`ScrollContentPresenter.Managed.cs:862-866` — `IDirectManipulationHandler.OnUpdated` calls the same
`Set(..., new(DisableAnimation:true, IsTouch:true, IsIntermediate:true))`, once per manipulation
update (i.e. per pointer sample), from the input dispatch, **not** from the frame clock.

**Both paths converge on the identical `Set` overload with identical options.** Confirmed at
`:851-856` (inertial branch) and `:862-866` (drag branch) — the only difference is the caller.

### 1.3 What `Set` does with it

`ScrollContentPresenter.Managed.cs:418-424`:

```csharp
if (updated || options.IsTouch)      // IsTouch is true for BOTH drag and fling → always runs
{
    Update(contentElt, h, v, _zoomFactor, options);
}
```

`Update` (`:523-530`) writes `visual.AnchorPoint = target` and calls `Updated(...)`.

`Updated` → `UpdateOffsets` (`:455-470`):

```csharp
if (_lastScrolledEvent != (h, v, isIntermediate))       // :458 — gated on actual movement
{
    Scroller?.OnPresenterScrolled(h, v, isIntermediate); // :462
}
ScrollOffsets = new Point(h, v);                         // :468
InvalidateViewport();                                    // :469 — UNGATED, every frame
```

### 1.4 Two independent consumers hang off this

| Consumer | Trigger | Notes |
|---|---|---|
| **ListView / ItemsStackPanel / ItemsWrapGrid** (`VirtualizingPanelLayout.managed.cs`) | `ScrollViewer.ViewChanged` → `OnScrollChanged` (`:211`, `:259`) | the path this document quantifies |
| **ItemsRepeater** (`ViewportManagerWithPlatformFeatures.cs`) | `FrameworkElement.EffectiveViewportChanged` (`:286`) | separate, see §5 |

For a plain `ListView`, `InvalidateViewport()` (§1.3) is cheap: `PropagateEffectiveViewportChange`
early-returns at `FrameworkElement.EffectiveViewport.cs:349-353` when nothing in the subtree
subscribed to `EffectiveViewportChanged`. The only in-box subscribers are `ItemsRepeater`
(`ViewportManagerWithPlatformFeatures.cs:286`), `CalendarPanel`
(`CalendarPanel.ModernCollectionBasePanel.cs:348`), `TeachingTip` (`TeachingTip.mux.cs:1616-1618`)
and `SystemFocusVisual` (`SystemFocusVisual.cs:72`).

### 1.5 The realization is deferred one dispatcher turn

`ScrollViewer.OnPresenterScrolled` (`ScrollViewer.cs:1234-1243`):

```csharp
if (isIntermediate && UpdatesMode != ScrollViewerUpdatesMode.Synchronous)
{
    RequestUpdate();     // :1241
}
```

Default `UpdatesMode` is `AsynchronousIdle` (`FeatureConfiguration.cs:483`); the only in-box
override to `Synchronous` is `ScrollViewer.SetDirectManipulationStateChangeHandler`
(`ScrollViewer.MuxInternal.cs:74`), called **only from `CalendarView`**
(`CalendarView_Partial.cs:851`, `:2039`). So for `ListView`/`ScrollViewer` the async path applies.

`RequestUpdate` (`ScrollViewer.cs:1301-1316`) coalesces onto a single
`Dispatcher.RunAsync(CoreDispatcherPriority.Normal)` work item. `CoreDispatcherPriority.Normal`
maps to `NativeDispatcherPriority.Normal` via `(NativeDispatcherPriority)(~priority + 2)`
(`Uno.UWP/UI/Core/CoreDispatcher.cs:74`).

That item runs `Update(isIntermediate:true)` → `ViewChanged?.Invoke(...)`
(`ScrollViewer.cs:1318-1357`) → `VirtualizingPanelLayout.OnScrollChanged`.

**Consequence:** the realization triggered by frame *N*'s offset write does **not** run inside
frame *N*'s record. It runs as a Normal dispatcher item afterwards, and the dispatcher's render
gating (`NativeDispatcher.cs:206-229`, `TryGetRenderAction` +
`normalItemsToProcessBeforeNextRenderAction` snapshotted at `:216`) generally serializes it before
the *next* record action. So realization cost sits directly in the critical path between frame *N*
and frame *N+1* — on the same thread that must produce frame *N+1*.

---

## 2. Measuring the realization work as a function of delta

### 2.1 The loop

`VirtualizingPanelLayout.managed.cs:259-331`, `OnScrollChanged`:

```csharp
var delta         = ScrollOffset - _lastScrollOffset;          // :266
var unappliedDelta = Abs(delta);                                // :268
var isLargeScroll  = Abs(delta) > ViewportExtent;               // :270

if (isLargeScroll) { ... ClearLines(); SetDynamicSeed(...); }   // :272-294  ← cliff, see §4

while (unappliedDelta > 0)                                      // :296
{
    var scrollIncrement = GetScrollConsumptionIncrement(fillDirection);  // :303 → one line's extent
    unappliedDelta -= scrollIncrement;                          // :311
    UpdateLayout(extentAdjustment: sign * -unappliedDelta, isScroll: true);  // :313
#if __SKIA__
    (ItemsControl as ListViewBase)?.TryLoadMoreItems(LastVisibleIndex);     // :316
#endif
}

ArrangeElements(_availableSize, ViewportSize);                  // :320  ← delta-INDEPENDENT
UpdateCompleted();                                              // :321  ← delta-INDEPENDENT
if (isLargeScroll) { OwnerPanel.InvalidateMeasure(); }           // :323-328
_lastScrollOffset = ScrollOffset;                               // :330
```

`GetScrollConsumptionIncrement` (`:349-362`) returns the actual extent of the line about to leave
the extended viewport, falling back to `_averageLineHeight`. So the loop runs
**N ≈ ceil(D / L)** iterations, minimum 1, where `D` = per-event offset delta and `L` = line
extent.

The comment at `:302-303` states the intent explicitly: *"Apply scroll in bite-sized increments.
This is crucial for good performance, since the delta may be in the 100s or 1000s of pixels, and we
want to recycle unseen views at the same rate that we dequeue newly-visible views."* — i.e. the
loop is a **memory/recycling** device, not a time budget. It trades one large `UpdateLayout` for
N smaller ones and adds N× the fixed `UpdateLayout` overhead.

### 2.2 Per-iteration and per-item cost

`UpdateLayout` (`:462-494`) = `UnfillLayout` (`:577-605`) + `FillLayout` (`:518-568`) +
`SetDynamicSeed` + `CorrectForEstimationErrors` (`:634-664`).

- `UnfillLayout` / `FillLayout` are `while` loops over the lines actually crossing the extended
  viewport edges — across the *whole* `OnScrollChanged` call they fire **D/L times in total**
  (each line is materialized once and recycled once), not N×(D/L).
- `CorrectForEstimationErrors` is O(materialized) but **only when `neededCorrection != 0`**
  (`:651`), which requires the first materialized line to be item 0 or to sit at a negative offset
  — not the steady-state scrolling case.

Per realized line (`AddLine` `:993-1014` → `ItemsStackPanelLayout.CreateLine`
`ItemsStackPanelLayout.managed.cs:14-27`):

1. `Generator.DequeueViewForItem(index)` (`VirtualizingPanelGenerator.managed.cs:90-129`)
   — scrap lookup, template-id resolution, `Stack.Pop`, `Visibility = Visible` (a DP write), then
   **`ItemsControl.PrepareContainerForIndex(container, index)`** (`:126`) → DataContext push →
   binding re-evaluation over the whole item template subtree. On a cache miss it calls
   `GetContainerForIndex` (`:116`) → full container + template inflation.
2. `AddView` (`VirtualizingPanelLayout.managed.cs:1025-1056`) → **`view.Measure(slotSize)`**
   (`:1037`) → full measure pass of the item subtree.

Per `OnScrollChanged` call, delta-independent:

3. `ArrangeElements` (`:439-453`) — iterates **every** materialized line and calls
   `item.container.Arrange(...)` on **every** container.
4. `UpdateCompleted` (`:499-508`) — `ClearScrappedViews()` + `UpdateVisibilities()` over the
   generator caches.

### 2.3 Cost model

```
Cost(D) ≈  a·M                  fixed:    ArrangeElements over M materialized lines + generator housekeeping
         + b·(D/L)              variable: per-item PrepareContainer + Measure
         + c·ceil(D/L)          variable: per-iteration UpdateLayout overhead (bookkeeping only)
```

with (verified from code):

| Symbol | Definition | Source |
|---|---|---|
| `V` | viewport extent | `VirtualizingPanelLayout.managed.cs:125-140` |
| `E` | `CacheLength · V · 0.5` = cache buffer **per side** | `:155`, `:160` |
| `CacheLength` | **1.0** by default (not the WinUI 4.0) | `FeatureConfiguration.cs:323` |
| extended VP | `V + 2E` = **2·V** at default | `:163-183` |
| `M` | materialized lines ≈ `2V / L` | |

---

## 3. The numbers

Assumptions: `V = 800` dip viewport, `CacheLength = 1.0` ⇒ extended viewport 1600 dip, so
`M = 1600 / L` materialized containers.

### 3.1 Lines realized per frame

| Row height `L` | `M` | drag 1000 px/s @120Hz (8.3 px/f) | fling 3000 px/s @120Hz (25 px/f) | fling 8000 px/s @120Hz (66.7 px/f) |
|---|---|---|---|---|
| 40 dip (dense) | 40 | 0.21 | 0.63 | 1.67 |
| 48 dip (list item) | 33 | 0.17 | 0.52 | 1.39 |
| 80 dip | 20 | 0.10 | 0.31 | 0.83 |
| 120 dip (card) | 13 | 0.07 | 0.21 | 0.56 |

At 60 Hz, double every figure.

Loop iteration count `N = ceil(D/L)`, min 1: **1** for every drag cell and for the 3000 px/s fling
at `L ≥ 40`; **2** only for the 8000 px/s hard flick at `L ≤ 48`.

### 3.2 Fixed vs variable work per `ViewChanged`

At `L = 48`, `M = 33`:

| | container touches from `ArrangeElements` (fixed) | container touches from realization (variable) | variable share |
|---|---|---|---|
| drag 1000 px/s @120Hz | 33 | 0.17 | **0.5 %** |
| fling 3000 px/s @120Hz | 33 | 0.52 | **1.6 %** |
| fling 8000 px/s @120Hz | 33 | 1.39 | **4.0 %** |

**The delta-proportional term is a rounding error against the delta-independent term at every
realistic velocity.** The per-event cost of `OnScrollChanged` is essentially a constant `a·M`.
A 3× velocity increase raises total per-event cost by roughly 1 %, not 3×.

### 3.3 The whole fling's realization budget

For the Android curve (`ScrollFlingSimulation.cs:62-73`), with
`referenceVelocity = Friction · PhysicalCoefficient / Inflexion = 0.015 · 31134.3 / 0.35 = 1334.3`:

```
v0 = 3000 px/s
androidDuration = (3000/1334.3)^(1/1.3582017)      = 1.816 s
duration        = 2.3582 · 0.35 · 1.816            = 1.499 s
distance        = 3000 · 1.499 / 2.3582            = 1907 px
```

(Sanity check against the recorded regression fixture: solving the same closed form for
`distance = 1531 px` gives `v0 ≈ 2640 px/s`, consistent with the "264 px flick" capture.)

So a 3000 px/s fling:
- travels **1907 px** over **1.499 s**;
- realizes **1907 / 48 ≈ 40 lines** in total — i.e. **~1.2 extended viewports' worth**;
- across 180 frames at 120 Hz that is **0.22 lines per frame on average**, peaking at 0.52 on the
  very first frame.

**The entire fling's incremental realization work is ~40 item measures spread over 1.5 seconds.**
Even at a pessimistic 2 ms per item that is 80 ms of work in a 1500 ms window — a **5.3 % duty
cycle**, and never more than one item in any single frame.

### 3.4 Why this refutes the hypothesis

`ScrollContentPresenter.Managed.cs:1002-1007`:

```csharp
var fitted = _velocityTracker.GetVelocity();                 // fit over the last ≤100 ms of DRAG
var vx = (fitted?.X ?? args.Velocities.Linear.X) * 1000;
var vy = (fitted?.Y ?? args.Velocities.Linear.Y) * 1000;
recognizer.CompleteGesture();
StartFling(vx, vy);
```

The launch velocity **is** the drag velocity at release — a least-squares fit over the last 100 ms /
20 samples of the drag (`ScrollVelocityTracker.cs:20-21`, `:50-95`). And
`ScrollFlingSimulation.GetVelocity(t)` (`:100-114`) is monotonically decreasing in `t`:

```csharp
var u = 1.0 - Math.Clamp(t / _duration, 0.0, 1.0);
return _velocity * Math.Pow(u, DecelerationRate - 1.0);      // u ↓, exponent > 0  ⇒  |v| ↓
```

(Apple branch: `_velocity * Math.Pow(0.135, t)` — also monotone.)

Therefore:

> **frame delta during the fling ≤ frame delta during the last 100 ms of the drag, always.**

If the drag was smooth at velocity `v`, the fling starting at `v` performs *identical* work in its
first frame and *strictly less* in every frame thereafter. The hypothesis requires the opposite
ordering. It cannot hold.

The one theoretical loophole: `ScrollVelocityTracker.SolveSlope` returns `coefficients[1]` of a
**quadratic** fit evaluated at the newest sample (`ScrollVelocityTracker.cs:101-197`), so a finger
that is *accelerating* at release can extrapolate slightly above every observed sample velocity.
That is a bounded few-percent effect, not the 3× the hypothesis needs. **UNVERIFIED** — no capture
measured.

### 3.5 The count goes the *wrong* way

- The fling calls `Set` **exactly once per rendered frame** — `OnFlingFrame` is a
  `Compositor.FrameStarting` subscriber (`ScrollContentPresenter.Managed.cs:598`), and
  `FrameStarting` is raised once per `RenderRootVisual` (`Compositor.skia.cs:226-235`).
- The drag calls `Set` **once per pointer sample** (`OnUpdated`, `:862-866`). Android touch
  digitisers commonly sample at ≥ the display rate. `RequestUpdate` coalesces per dispatcher drain
  (`ScrollViewer.cs:1301-1316`), so multiple `ViewChanged` per frame are possible whenever the
  dispatcher drains more than once between records.

Since the per-event cost is dominated by the delta-independent `a·M` term (§3.2), **drag can pay
that fixed cost more times per frame than the fling does.** The virtualization load during drag is
plausibly *higher*, not lower.

---

## 4. Throttling, budgets, and the feedback loop

### 4.1 Is anything throttled or budgeted?

**No time budget exists anywhere on this path.** Specifically:

- `OnScrollChanged` (`:259-331`) has no deadline, no work quota, no "stop and continue next frame".
- The `while (unappliedDelta > 0)` loop (`:296`) is bounded only by the delta itself.
- `OwnerPanel.ShouldInterceptInvalidate = true` around `UpdateLayout` (`:465`, `:483`) suppresses
  re-entrant measure invalidation — a *correctness* guard, not a budget.
- `Generator.CacheLimit` is recomputed in `ArrangeOverride` (`:424-434`) as
  `(ViewportExtent / _averageLineHeight) * 2`, clamped to `[10, 1024]`
  (`VirtualizingPanelGenerator.managed.cs:24-29`, `:65-74`) — a *memory* cap, not a time cap.
- **A throttle that does exist elsewhere is compiled out on Skia**: `ViewportManagerWithPlatformFeatures.cs:599-608`
  guards `TryInvalidateMeasure()` behind `vl.IsSignificantViewportChange(...)` inside
  `#if !UNO_HAS_ENHANCED_LIFECYCLE`. `UNO_HAS_ENHANCED_LIFECYCLE` is defined for **both Skia and
  WASM** (`src/Uno.CrossTargetting.targets:74`, `:78`), so on Skia every effective-viewport change
  invalidates `ItemsRepeater`'s measure — see §5.

### 4.2 Does a slow frame make the next frame worse?

**Yes — the loop exists, and it is strongly damped.**

The fling is evaluated in absolute time (`OnFlingFrame:615`, and see the `ScrollFlingSimulation`
doc comment at `:10-15`: *"analytic in absolute time rather than integrated per tick, so a late or
early frame produces the correct position"*). A frame that arrives `Δt` late therefore carries
`v·Δt` extra offset, which costs `b·v·Δt/L` extra realization time, which delays the next frame
further.

Loop gain:

```
g = b · v / L
```

With `b = 1 ms` per realized item, `v = 3000 px/s`, `L = 48 px`:
`g = 0.001 s × 62.5 items/s = 0.0625`. Steady-state amplification `1/(1-g) = 1.07`.

The loop only diverges at `g ≥ 1`, i.e. `v ≥ L / b = 48 px / 1 ms = 48 000 px/s`. Unreachable.
**In the linear regime the feedback is real but negligible — a ~7 % frame-time inflation.**
(`b` is an order-of-magnitude estimate — **UNVERIFIED**, not measured on device.)

### 4.3 The cliff — the one place the loop bites

`VirtualizingPanelLayout.managed.cs:270-294`:

```csharp
var isLargeScroll = Abs(delta) > ViewportExtent;              // :270
if (isLargeScroll)
{
    unappliedDelta = 1;                                        // :284
    ClearLines(clearContainer: false);                         // :288  ← recycle EVERY line
    var index = (int)(ScrollOffset / _averageLineHeight);
    SetDynamicSeed(IndexPath.FromRowSection(index - 1, 0), index * _averageLineHeight);  // :293
}
...
if (isLargeScroll) { OwnerPanel.InvalidateMeasure(); }          // :323-328  ← full panel measure
```

This is a **step function**, not a gain. Crossing `D > V` costs `≈ M · b` (re-realize a whole
extended viewport: ~33 items ≈ 33 ms at `b = 1 ms`, a **4-frame hitch at 120 Hz**) *plus* a forced
`InvalidateMeasure` → full panel measure pass.

To trigger it during a 3000 px/s fling you need one `ViewChanged` to carry more than `V = 800` px,
i.e. **267 ms** between consecutive dispatcher drains of the coalesced `RequestUpdate`. That
requires an already-catastrophic stall (GC pause, first-touch template inflation, a blocking
resource load). So the cliff is a **hitch amplifier**, not a hitch originator: it converts a single
long stall into a multi-frame one.

Note this cliff is **symmetric** between drag and fling — a drag stalled for 267 ms at the same
velocity trips it identically.

---

## 5. The `ItemsRepeater` variant

Different trigger, same conclusion. `ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged`
(`:498-515`) → `UpdateViewport` (`:570-609`) → `TryInvalidateMeasure()` (`:607`).

On Skia the `IsSignificantViewportChange` gate is compiled out (§4.1), so **every** viewport change
invalidates the repeater's measure. The subsequent `VirtualizingLayout.MeasureOverride` walks the
realized range; its cost is O(realized elements) and, like `ArrangeElements`, is **independent of
the per-frame delta**. Same shape as §3.2: fling and drag pay the same fixed cost per event, and
drag can pay it more often.

Additionally `OnOwnerArranged` (`:318-352`) inflates the cache buffer by
`CacheBufferPerSideInflationPixelDelta` **on every arrange** until it reaches
`m_maximum*CacheLength · visibleWindow / 2` (default `2.0`, `:54-55`), re-registering cache-build
work each time (`:348`). That is a per-arrange cost that ramps during any sustained scroll —
again velocity-independent.

---

## 6. What this rules in and rules out

**Ruled out** (with the mechanism named, not just asserted):
- "The fling realizes more items per frame than a drag." — impossible by construction
  (§3.4): fling launch velocity *is* drag release velocity, and decays monotonically.
- "Virtualization work scales with velocity enough to matter." — it does scale linearly, but the
  scaling term is 0.5–4 % of the per-event cost (§3.2), and the whole fling's realization budget is
  ~40 item measures over 1.5 s (§3.3).
- "There's a runaway feedback loop through realization." — the loop exists but has gain ~0.06
  (§4.2).

**Ruled in as residual risk** (secondary, not the asymmetry):
- The `isLargeScroll` cliff (§4.3) turns any ≥267 ms stall into a multi-frame rebuild, and it lands
  *during* the fling where a drag would have the finger to hide it.
- The delta-independent `a·M` term is the actual steady-state virtualization load, and nothing
  budgets it. It is worth optimising for both phases — `ArrangeElements` (`:439-453`) re-arranges
  every materialized container on every `ViewChanged` even when only one line moved.
- On Skia the `ItemsRepeater` viewport throttle is compiled out (§4.1, §5).

**Where the asymmetry must actually live** (out of scope for this document, but this analysis
narrows it): the per-frame *work* is the same or lower for the fling, so the difference must be in
*timing/visibility*, not in *load* — consistent with the standing hypothesis that a drag is
self-correcting (position tracks the finger, so latency variation shows as an invisible position
error) whereas the fling has no reference (so the same latency variation shows as a visible
velocity error). Virtualization is a **contributor to frame-time variance shared by both phases**,
which the fling merely *reveals*.

---

## 7. Smallest proof (confirm or refute this document in one build)

`ScrollDiagnostics` (`src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollDiagnostics.cs`) already has a
lock-protected ring buffer with a phase tag (`PhaseDrag = 1`, `PhaseInertia = 2`, `:71-74`) and a
settle-triggered dump — it perturbs nothing inside the frame.

Add one sample kind and one call site:

1. `ScrollDiagnostics.SampleKind` (`:29-36`) — add `Realize = 2`.
2. In `VirtualizingPanelLayout.OnScrollChanged` (`:259-331`), bracket the body with
   `Stopwatch.GetTimestamp()` and, just before `_lastScrollOffset = ScrollOffset;` (`:330`), record
   `delta`, the loop iteration count, `linesAdded` (already returned by `FillLayout`, `:546`), `M`
   (`_materializedLines.Count`), and the elapsed microseconds.

Then capture one drag → fling on device and check three predictions:

| # | Prediction if this document is right | Prediction if the hypothesis is right |
|---|---|---|
| 1 | max per-event `delta` in `Phase=2` (inertia) ≤ max per-event `delta` in `Phase=1` (drag) | inertia deltas exceed drag deltas |
| 2 | `OnScrollChanged` elapsed µs is nearly flat vs `delta` (slope ≪ intercept) | elapsed µs scales with `delta` |
| 3 | summed `linesAdded` over the whole fling ≈ distance / `L` ≈ 40 for a 3000 px/s fling | far more, or clustered spikes |

If prediction 2 fails — elapsed µs tracks `delta` with a large slope — then `b` is much bigger than
the 1 ms/item estimate used here, `g` in §4.2 must be recomputed, and the hypothesis deserves
re-opening. Everything else in this document holds regardless of `b`, because §3.4 is a structural
argument about velocity ordering, not a cost argument.

A cheaper (but weaker) pre-check needing no code change: set
`FeatureConfiguration.ListViewBase.DefaultCacheLength = 0` before UI init
(`FeatureConfiguration.cs:323`). That collapses the extended viewport from `2V` to `V`, roughly
halving `M` and therefore the `a·M` fixed term, while leaving `b·(D/L)` — the delta-proportional
term the hypothesis is about — **unchanged**. If inertia smoothness improves noticeably, the
problem is the fixed term (this document's §6 residual). If it doesn't change, virtualization cost
is not the driver at all.
