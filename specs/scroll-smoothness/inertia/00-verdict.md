# Verdict — why touch inertia still feels less smooth than drag

Scope: `dev/mazi/smooth-scroll` @ worktree `D:/Work/uno-worktrees/scrollsmooth`.
Evidence label: **code review only** (inspection). No compile, no runtime. Every claim below is cited
to `file:line`; anything not verifiable from this worktree is marked **UNVERIFIED**.

Input: seven candidate angles plus seven refutation passes. Three explanations survive. This note
ranks them, re-verifies the top two independently, names the one change to make first, and lists what
the angles missed.

---

## 0. The one-sentence answer

> Drag renders a **position latched from the finger**; inertia renders a **position computed from a
> wall clock read at the moment the UI thread enters the record**. Presents are paced to vsync;
> records are not. So every millisecond of record-phase wobble is `v·Δt` pixels of position error for
> inertia and **exactly zero** for drag.

Everything else is second order.

---

## 1. Ranking

| # | Explanation | Explains the asymmetry? | Confidence | Magnitude |
|---|---|---|---|---|
| **1** | **Clock-argument asymmetry** — fling position is `f(t_record)`, drag position is a latch | Yes, mechanically | **High** (verified below) | ~2.6 dip per ms of jitter at v₀≈2650 dip/s; a 4 ms wobble is half a 120 Hz step |
| **2** | **Realize-after-record inversion** — the fling's offset is produced *inside* the record, so the picture is painted at `offset_n` with containers realized for `offset_{n-1}` | Yes, structurally fling-only | **High** (verified below) | One frame of leading-edge lag; mostly hidden by the 2× extended viewport |
| **3** | **Launch seam** — `_flingStartTimestamp` is a `now` read at pointer-up while the position origin is the last drag `Set` | Yes, but once per fling | **Medium** | First inertia frame advances by `v₀·(t_record₁ − t_up) ∈ [0, one period]` — i.e. can be a single frozen frame at peak velocity |

Refuted or demoted (do not carry into the workplan):

- **"The ahead-of-time render path is fling-only."** False. Drag arms it identically: drag's `Set`
  (`ScrollContentPresenter.Managed.cs:864-868`) takes the same branch at `:523-529`, writes
  `visual.AnchorPoint` at `:527` → `Visual.OnPropertyChangedCore` → `InvalidateRenderPartial` →
  `RequestNewFrame` (`Compositor.skia.cs:301`); and the same `Updated` → `InvalidateViewport`
  (`:469`) → `EnqueueForEffectiveViewportChanged` → `CoreServices.RequestAdditionalFrame`
  (`EventManager.cs:33`) enqueues the same Normal-priority `OnTick` that then records early
  (`CoreServices.cs:115` then `:124`). Scheduling is symmetric.
- **"`FrameStarting is not null` at `Compositor.skia.cs:291` is the fling-only branch that causes it."**
  Redundant. During a fling the `AnchorPoint` write at `ScrollContentPresenter.Managed.cs:527` — which
  happens inside `FrameStarting`, raised at `Compositor.skia.cs:234`, 57 lines earlier in the same
  call — has already called `RequestNewFrame` via `:301`. Line 291 changes nothing.
- **"An extra `Set(IsIntermediate:false)` / `InvalidateArrange` fires at finger lift."** False, verified.
  `_status = ManipulationStatus.Inertia` is assigned at `GestureRecognizer.Manipulation.cs:393`
  **before** `ManipulationInertiaStarting` is raised at `:397`, so `recognizer.CompleteGesture()`
  (`ScrollContentPresenter.Managed.cs:1008`) enters `Complete()` with `isInertial == true`
  (`GestureRecognizer.Manipulation.cs:293-294`). `_touchInertia` is never assigned non-null on the fling
  branch (every assignment in the file is `= null`), so `OnCompleted` early-returns at
  `ScrollContentPresenter.Managed.cs:1019-1023`. No arrange at lift.
- **"Virtualization costs more per frame during inertia."** No. `ScrollFlingSimulation.GetVelocity`
  is monotonically decreasing (`ScrollFlingSimulation.cs:112-113`) and the launch velocity is the fit
  over the drag (`ScrollContentPresenter.Managed.cs:1004-1009`), so the fling's per-frame delta is
  bounded above by the drag's delta at release and decays from there.
- **"The fling's own enqueues inflate the dispatcher render barrier."** Backwards.
  `normalItemsToProcessBeforeNextRenderAction` is snapshotted at `NativeDispatcher.cs:216` *before*
  the render action is handed out; items enqueued afterwards drain it (`:156-165`), they do not inflate it.

---

## 2. Rank 1, verified independently

### 2.1 The two paths converge on one write; only the argument differs

Both drag and fling reach `Update(...)` and take the same branch:

```
ScrollContentPresenter.Managed.cs:523    if (options is { DisableAnimation: true } or { IsTouch: true })
ScrollContentPresenter.Managed.cs:527        visual.AnchorPoint = target;
ScrollContentPresenter.Managed.cs:529        Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
```

- **Drag** supplies `HorizontalOffset + deltaX` / `VerticalOffset + deltaY`
  (`ScrollContentPresenter.Managed.cs:802-804`, `:864-868`). `deltaX/deltaY` come from
  `unhandledDelta.Translation`, i.e. pure finger geometry. **No clock is read anywhere on this path.**
  The only clock read on the drag side is `:836-838`, which feeds `ScrollVelocityTracker` and does not
  touch the rendered position.
- **Fling** supplies `_flingH.GetPosition(elapsed)` / `_flingV.GetPosition(elapsed)`
  (`ScrollContentPresenter.Managed.cs:622-623`) with
  `elapsed = (timestampInTicks - _flingStartTimestamp) / TimeSpan.TicksPerSecond` (`:617`).

`timestampInTicks` is the value handed to `FrameStarting`:

```
Compositor.skia.cs:230    var frameTimestamp = TimestampInTicks;
Compositor.skia.cs:231    CurrentFrameTimestampInTicks = frameTimestamp;
Compositor.skia.cs:234    frameStarting(frameTimestamp);
Compositor.cs:37          public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));
```

That is a free-running wall clock sampled at the instant the UI thread entered the record — not a
vsync, not a presentation time. And `ScrollFlingSimulation` is deliberately analytic in absolute time
(`ScrollFlingSimulation.cs:82-97`, and the class remark at `:11-15` says so), so **all** frame-to-frame
irregularity in the fling's output is irregularity in its clock argument.

### 2.2 Presents are paced; records are not

**Presents are paced.** Android's render thread presents and *then* blocks on Choreographer:

```
UnoSKVulkanView.cs:153    RenderFrame();
UnoSKVulkanView.cs:158    _pacer.WaitForNextFrame();      // ChoreographerFramePacer.cs:66-76
```

In steady state the pacer returns at vsync V, finds a request already pending, and presents at V+ε.
Presentation cadence is therefore one per refresh interval with locked phase. Win32 is the same shape
(`Win32RenderPacer`, DwmFlush-based).

**Records are not paced.** Three independent, verified sources of phase:

1. `NativeDispatcher.TryGetRenderAction` withholds the render action until
   `normalItemsToProcessBeforeNextRenderAction` reaches 0 and re-seeds it to the current Normal-queue
   depth on every handover (`NativeDispatcher.cs:206-234`, esp. `:214` and `:216`). Both scroll paths
   push Normal items every frame (`EventManager.cs:33` → `CoreServices.cs:66-74`;
   `ScrollViewer.cs:1301-1313`).
2. `OnRenderFrameOpportunity` records early, off the paced schedule, and marks
   `_renderedAheadOfTime` so the next paced tick produces no record
   (`CompositionTarget.RenderScheduling.skia.cs:178-208`; skip branch at `:131-141`). It is called from
   `CoreServices.OnTick` **after** `root.UpdateLayout()` (`CoreServices.cs:115` then `:124`) — so the
   record instant carries the whole measure/arrange/EVP cost of that tick.
3. The picture is deposited into `_lastRenderedFrame` (`CompositionTarget.Rendering.skia.cs:147`) and
   presented later by the render thread (`Draw`, `:221-241`), with `ReturnFrame` (`:412-433`)
   re-presenting on a starved vsync.

### 2.3 Why drag is immune and inertia is not — the mechanical form

The perceptual argument ("the finger masks it") is not sufficient and is not what this rests on. The
mechanical form is:

> **Drag's rendered value is piecewise-constant in wall-clock time; inertia's is continuous in it.**

Between two input arrivals, `visual.AnchorPoint` does not change. Recording at `V+1 ms` or `V+9 ms`
yields the *identical* picture. The fling's value differs by `v·8 ms` between those two instants. So
sub-refresh record-phase jitter maps to position error `v·δ` for inertia and `0` for drag.

Presented step, inertia: `v·(t_record[n] − t_record[n−1])`, shown at a fixed cadence.
Presented step, drag: the true finger motion since the previous latched sample.

Magnitude: inverting `ScrollFlingSimulation.Create` (`:62-73`) against the recorded 264 px → 1531 px
gives v₀ ≈ 2650 dip/s, i.e. **2.65 dip per ms** of phase error. At 120 Hz the nominal step is ~22 dip,
so 1 ms is a 12 % velocity ripple and 4 ms is half a step. This gets *worse* at higher refresh rates,
not better: the error is `v·δ` regardless of rate while the step halves.

**Required premise, UNVERIFIED (OS behaviour, outside this repo):** roughly one input batch arrives per
record. Android delivers `MotionEvent`s batched at the Choreographer input phase, and Uno dispatches
them synchronously on the UI thread (`ApplicationActivity.DispatchTouchEvent` → 
`AndroidCorePointerInputSource.OnNativeMotionEvent`, `AndroidCorePointerInputSource.cs:211+`), using
only the newest coordinate — no `getHistorical*` walk. If input were delivered far above frame rate and
unbatched, drag would pick up its own `±T_input` aliasing and the asymmetry would narrow. The product
owner's report ("drag glass smooth even fast, inertia not") is the empirical evidence that it does not.

---

## 3. Rank 2, verified independently

The fling's offset is produced **inside** the record. `FrameStarting` is raised at
`Compositor.skia.cs:226-234`; the paint walk is at `:270`. Everything `OnFlingFrame` triggers runs
between those two lines.

`Set` → `Updated` → `InvalidateViewport` (`ScrollContentPresenter.Managed.cs:469`) →
`PropagateEffectiveViewportChange` → `EnqueueForEffectiveViewportChanged`, which only **queues**:

```
EventManager.cs:28-33     _effectiveViewportChangedQueue.Add((element, args));
                          CoreServices.RequestAdditionalFrame();
```

The queue is drained *only* from `RaiseEffectiveViewportChangedEvents`, called *only* from
`InnerUpdateLayout` (`UIElement.cs:982`), which runs *only* from the next `OnTick`
(`CoreServices.cs:115`).

Consequence, fling-only:

- **Fling**: write `offset_n` at `Compositor.skia.cs:234` → paint walk at `:270` → the picture shows
  `offset_n` with containers realized for `offset_{n−1}`. Realization can never interpose.
- **Drag**: write `offset_n` from the pointer handler, *before* the tick → `OnTick` runs
  `UpdateLayout` (realizing for `offset_n`) → *then* `OnRenderFrameOpportunity` records. The picture
  and the realization agree.

This is a genuine, structural, fling-only one-frame ordering inversion. It presents as a lagging
leading edge / late item pop on virtualized content, **not** as velocity ripple — which is why it
ranks second, and why the extended viewport (`CacheLength` default 1.0, i.e. ~2× viewport) usually
covers it. It is not fixed by the clock change; call it out separately so nobody assumes it is.

---

## 4. The one change to make first

**File:** `src/Uno.UI.Composition/Composition/Compositor.skia.cs`
**Method:** `RenderRootVisual` (lines 219-243), single line `:230`.

Replace the raw clock read handed to `FrameStarting` with a **monotone, vsync-grid-quantized frame
clock**. Because presents are paced one-per-refresh, the *mean* of the raw record deltas over a window
equals the refresh period exactly even though the individual deltas jitter — so the period can be
estimated in-process with no platform plumbing:

```csharp
// new private fields on Compositor (skia partial)
private long _lastFrameTimestamp;
private readonly long[] _rawDeltas = new long[32];
private int _rawDeltaIndex;

private long GetFrameTimestamp()
{
    var raw = TimestampInTicks;
    if (_lastFrameTimestamp == 0)
    {
        _lastFrameTimestamp = raw;
        return raw;
    }

    var rawDelta = raw - _lastFrameTimestamp;
    _rawDeltas[_rawDeltaIndex++ & 31] = rawDelta;
    var period = Median(_rawDeltas);

    // Resync rather than crawl back after a stall or an idle gap.
    if (period <= 0 || rawDelta > period * 8)
    {
        _lastFrameTimestamp = raw;
        return raw;
    }

    var steps = Math.Clamp((long)Math.Round(rawDelta / (double)period), 1, 4);
    _lastFrameTimestamp += steps * period;
    if (_lastFrameTimestamp > raw + period)
    {
        _lastFrameTimestamp = raw; // never run ahead of the real clock by more than one period
    }

    return _lastFrameTimestamp;
}
```

then at `:230`: `var frameTimestamp = GetFrameTimestamp();`

Why this and not Choreographer's `frameTimeNanos`:

- `ChoreographerFramePacer.FrameCallback.DoFrame(long frameTimeNanos) => onFrame();`
  (`ChoreographerFramePacer.cs:99`) does discard it — but it fires on a **private Looper thread**,
  **after** `RenderFrame()` already presented (`UnoSKVulkanView.cs:153` then `:158`), and it is
  `CLOCK_MONOTONIC` while `_flingStartTimestamp` (`ScrollContentPresenter.Managed.cs:593`) and the
  velocity tracker (`:836-838`) are `Stopwatch.GetTimestamp()`. Plumbing it is a per-platform change
  with an epoch reconciliation. Do it **second**, once the quantizer has proved the mechanism.

Why this seam and not `ScrollFlingSimulation`: `FrameStarting` is the pipeline's only pre-record
per-frame hook (`Compositor.skia.cs:200-211`), so the fix is inherited by **every** frame driver —
fling and wheel decay today (`ScrollContentPresenter.Managed.cs:599` and `:666`), anything added later.
It touches no drag code at all, by construction.

This is also exactly what `specs/scroll-smoothness/spec.md` already committed to: *"one clock, one
closed-form evaluation per frame, at the presentation timestamp"* — minus the platform vsync plumbing.

### The measurement that proves or refutes it on a device

`ScrollDiagnostics` already emits everything needed. Per recorded frame it writes
`F <phase> <wallUs> <frameUs> <src> <value>` where `frameUs = CurrentFrameTimestampInTicks/10`
(`ScrollDiagnostics.cs:98`, fed from `ScrollContentPresenter.Managed.cs:189`) and `value = −AnchorPoint.Y`.
Enable with `FeatureConfiguration.ScrollViewer.EnableDiagnostics` (`FeatureConfiguration.cs:502`).
Phases: `1 = drag`, `2 = inertia` (`ScrollDiagnostics.cs:71-74`).

**One capture:** slow steady drag → flick → let it settle. Then compute, per phase:

| Metric | Prediction if rank 1 is right | Prediction if it is wrong |
|---|---|---|
| **A.** `σ(Δ frameUs)` | **similar in both phases** — scheduling is symmetric, that is the point | — |
| **B.** `σ(Δ value) / mean(Δ value)` at matched mean speed | **materially worse in phase 2 than phase 1** | comparable in both |
| **C.** `corr(Δ value, Δ frameUs)` | **≈ +1 in phase 2, ≈ 0 in phase 1** | ≈ 0 in phase 2 |

**B** is the load-bearing number: it is precisely what separates this hypothesis from "frames are just
irregular", because **A** says the irregularity is the same in both phases. **C** is the direct
signature — `Δx = v·Δt` by construction.

Then apply the quantizer and re-run. Expected: **A** unchanged, **B** drops to drag's level, **C**
collapses to ~0 in phase 2. If **B** does not move, the quantizer is not the answer and the residual is
downstream of the record (present-latency variance, duplicate presents via `ReturnFrame`,
`CompositionTarget.Rendering.skia.cs:412-433`) — which this capture cannot see. That would need a
present-side timestamp taken in `UnoSKVulkanView.RenderFrame` immediately after
`OnNativePlatformFrameRequested` returns (`UnoSKVulkanView.cs:212-218`).

**Free separating experiment for rank 2, same session, no code:** run the identical flick on a tall
non-virtualized `StackPanel` inside a `ScrollViewer`, and on a `ListView`. Smooth on the StackPanel and
rough on the ListView ⇒ layout cost feeding `t_record` and the realize-after-record inversion are
material. Rough on both ⇒ the clock is the whole story.

**Instrumentation caveat that must be stated in any report using these numbers.** Enabling diagnostics
subscribes `CompositionTarget.Rendering` (`ScrollContentPresenter.Managed.cs:171`), which sets
`_isRenderingActive` (`CompositionTarget.Rendering.skia.cs:90-96`), which makes every `Render`
re-request a frame (`:164-167`) and enqueue an allocating `RaiseRendering` at High priority
(`:444-448`). Absolute jitter figures are therefore inflated in **both** phases. The metrics above are
within-phase ratios and a cross-phase comparison, so the conclusion survives; the absolute microseconds
do not.

---

## 5. Is the motion already correct and the complaint really about the curve?

**No — but the curve is not the problem either. The complaint is about sampling, plus the handoff.**

Arguing both sides from code:

**The curve is essentially right.** `ScrollFlingSimulation` is analytic, monotone, C¹, reproduces
Android's spline distance exactly (`ScrollFlingSimulation.cs:62-73`, `:95-97`), and terminates at
`v(T) = 0` exactly (`:112-113`) — a *softer* stop than Android's own SPLINE. That is not a source of
roughness.

**The sampling of that curve is provably non-uniform** (§2), and there is a one-off short first frame
(§1 rank 3). Neither is a property of the curve. So "the motion is already correct" is false.

**One genuine curve deviation exists, and it is a *feel* item, not a smoothness item.** Uno's duration
is `DecelerationRate * Inflexion * androidDuration` = **0.8254×** Android's, for an identical distance
(`ScrollFlingSimulation.cs:69-71`). Uno covers Android's fling distance in ~83 % of Android's time — a
measurably snappier, faster-decaying fling than the platform's. If the complaint survives the clock
fix, ask the product owner one targeted question: does the fling feel **rough**, or does it feel
**fast / short / stops too soon**? "Fast/short" is the curve, and it is a one-constant change. "Rough"
is still the pipeline.

---

## 6. What the angles missed

1. **Drag positions are truncated to whole physical pixels; fling positions are not.**
   `AndroidCorePointerInputSource.ToManaged` builds the position as
   `new Point((int)x - correction[0], (int)y - correction[1]).PhysicalToLogicalPixels()`
   (`AndroidCorePointerInputSource.cs:226-229`) — a **truncation**, not a round, applied before the
   logical conversion. Drag therefore advances on a 1/scale dip grid (0.33 dip at 3×) while the fling
   writes an unrounded double. This is the surviving cousin of the 2-dip quantizer that was removed; it
   can only ever hurt *drag*, so it is not the asymmetry — but it also biases the samples fed to
   `ScrollVelocityTracker` (`ScrollContentPresenter.Managed.cs:836-838`), and hence the launch velocity.
   Separate ticket: round instead of truncate, and feed the tracker the pre-quantization coordinate.

2. **The fling's `running` predicate can kill a fling that is still moving.**
   `ScrollContentPresenter.Managed.cs:626-627`:
   `elapsed < Math.Max(_flingH.Duration, _flingV.Duration) && (h > 0 && h < maxH || v > 0 && v < maxV)`.
   `h`/`v` are already clamped to `[0, max]` at `:622-623`, and `maxH`/`maxV` are re-read from
   `Scroller.ScrollableWidth/Height` every frame (`:619-620`). Two consequences: (a) a fling launched
   from offset 0 survives only because `elapsed > 0` on the very first tick — fragile; (b) any transient
   shrink of a virtualized extent estimate pins the position to `max`, makes the predicate false, and
   aborts **both axes** via `StopFling()` (`:631`). Drag has no such predicate. Symptom would be an
   abrupt mid-flight stop, not ripple — a different bug from the reported one, but a real one.
   **UNVERIFIED** that virtualized extents actually shrink mid-fling.

3. **The fling has no minimum-velocity cutoff, unlike the wheel.** `ScrollDecaySimulation` stops at
   `MinVelocity = 8.0` (`ScrollDecaySimulation.cs:30-31`) with the comment "below this the remaining
   motion is under a pixel per frame". `ScrollFlingSimulation` has no equivalent, and `OnFlingFrame`
   runs to full `Duration` (`:626`). The tail is therefore ~100+ ms of sub-pixel creep that keeps the
   render loop alive, keeps `ScrollDiagnostics.CurrentPhase = PhaseInertia`, and delays the final
   `IsIntermediate: false` arrange. Cosmetic, but it pollutes every capture's tail.

4. **Epoch mismatch blocks the "obvious" platform fix.** Any future change that sources the frame
   timestamp from Choreographer (`frameTimeNanos`, `CLOCK_MONOTONIC`) or `CADisplayLink.targetTimestamp`
   must reconcile with `Stopwatch.GetTimestamp()`, which is what `_flingStartTimestamp`
   (`ScrollContentPresenter.Managed.cs:593`) and the velocity tracker (`:836-838`) already use. Without
   reconciliation the first fling frame jumps by the epoch difference. Several angles described this as
   "one line" — it is not.

5. **Wheel decay integrates incrementally but is affected identically.** `ScrollDecaySimulation.Tick`
   advances from `_lastTimestampInTicks` (`ScrollDecaySimulation.cs:58-59`), so it looks immune — it is
   not: exponential decay integrated piecewise over exact intervals equals the closed form, so its
   position is still `x(t_record)`. The quantizer fix improves wheel too, which is a free second
   confirmation signal against the recorded 0.171 jerk baseline (and one that can be measured on Win32,
   where the timebase is exact and there is no single-threaded-input confounder).

6. **`Compositor.CurrentFrameTimestampInTicks` is already published (`Compositor.skia.cs:214`) and
   already flows into the diagnostics record (`ScrollContentPresenter.Managed.cs:189`).** Quantizing it
   at the source therefore also improves the fidelity of every future capture, with no change to
   `ScrollDiagnostics`.

---

## 7. Recommended order of work

1. **Quantize the frame clock** (§4) — one file, one seam, fixes fling and wheel, touches no drag code.
   Prove with metric **B**/**C** before and after.
2. **Back-date the launch anchor** (rank 3): set `_flingStartTimestamp` lazily on the first
   `OnFlingFrame` minus one nominal period, instead of eagerly at `:593`. Two lines. Removes the
   0-to-full-step stall at the exact moment the finger leaves the glass.
3. **Then** decide on the realize-after-record inversion (§3) — it needs the offset to be produced
   before `UpdateLayout`, not inside the record, which is a real pipeline change and should not be
   bundled with 1 or 2.
4. Log the three items in §6 (1, 2, 3) as separate defects. None of them is the asymmetry.
