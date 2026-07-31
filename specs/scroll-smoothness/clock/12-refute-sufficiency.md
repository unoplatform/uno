# 12 — The case against plumbing a platform frame timestamp

Companion/adversary to `01-win32.md` … `08-avalonia.md`, which establish that a real vsync or
predicted-presentation timestamp is *obtainable* on most targets. This note asks the different
question: **is it worth obtaining, given that the in-process phase-locked estimator already shipped?**

Evidence label: **code review (inspection) only** on this worktree — no compile, no runtime, no device.
The one set of hard numbers used below (§1.1) is quoted from `01-win32.md §8` and `06-winui.md §5.2`,
which were produced by probes on this machine; I did not re-run them. Anything I could not verify from
source is marked **UNVERIFIED**.

---

## 0. Verdict up front

| Question | Answer |
|---|---|
| Does a real timestamp beat the estimator in steady state? | Yes, by **~0.1 dip rms** of position ripple at 2650 dip/s. That is 0.1 physical px at 1×, 0.3 px at 3×. |
| How much of the total error does the estimator already remove? | **~94 %** (2.39 dip rms → 0.15 dip rms). The platform source takes it to ~98 %. |
| Does the estimator have failure modes a real timestamp fixes? | Yes — **two** (warm-up, refresh-rate change). Both are bounded, both have cheaper in-process fixes. |
| Is there a residual that neither fixes? | Yes, and it is **larger than either**: record→present latency that can quantize to 1 or 2 refresh intervals. Up to a *full step* of displayed error. Unmeasured. |
| Does the shipped `GetFrameTimestamp` have outright bugs? | **Yes — two high-severity, two medium.** None of them is the ones you asked me to check for; those four are clean. |
| Recommendation | **Ship the estimator alone**, after fixing §4.1–§4.4. Do **not** start per-frame vsync plumbing yet. Revisit only against the §6.2 trigger, Win32-first, in WPF's shape (real anchor + phase-locked grid), never as a raw substitution. |

The load-bearing sentence: **the residual the platform source would remove is already below the
display's own quantization, while the residual neither source removes is 150× larger and has never
been measured.** Spending the platform-plumbing budget now optimizes the term that is already
invisible.

---

## 1. What the estimator actually does to the error

### 1.1 The measured baseline

From `01-win32.md §8` (Windows 11 26xxx, 120 Hz, QPF = 10 MHz, 400 iterations of
`DwmFlush` → sample → simulated 0–3 ms record cost, residuals against a least-squares-fitted grid):

```
record instant (what Uno fed drivers before the change): jitter rms 0.9009 ms, peak |resid| 2.8990 ms
DwmFlush return instant                                : jitter rms 0.1637 ms, peak |resid| 1.8468 ms
qpcVBlank sampled at the record                        : jitter rms 0.0178 ms, peak |resid| 0.0467 ms
```

and from `06-winui.md §5.2`, `COMPOSITION_FRAME_STATS.targetTime` deltas are exact integer multiples
of the refresh period **to within ±20 µs**, over 141 frames, while a UI-thread `Stopwatch` read over
the *same* frames wanders by milliseconds.

So the raw record-instant jitter is **σ ≈ 0.90 ms rms**, on an *idle synthetic* probe. Under a real
fling over virtualized content it will be worse; §1.5 discusses what that does to the conclusion.

### 1.2 The estimator's transfer function, derived from the shipped code

`Compositor.skia.cs:273-287`:

```csharp
_frameClock += period;
var error = raw - _frameClock;
if (Math.Abs(error) >= period) { _frameClock += Round(error/period, AwayFromZero) * period; }
else                           { _frameClock += error / 16; }
```

Model the raw record instant as `raw_n = n·P + j_n` with `j_n` white, std `σ`, and stay in the
gentle-pull branch. Let `e_n = raw_n − _frameClock_n` be the phase error after the update, and
`a = 15/16`:

```
error_n = j_n − j_{n−1} + e_{n−1}
e_n     = a·(e_{n−1} + j_n − j_{n−1})
step_n  = _frameClock_n − _frameClock_{n−1} = P + error_n/16
```

Solving the AR(1) in steady state (`Var(u) = 2σ²`, `Cov(e_{n−1}, u_n) = −aσ²`):

```
Var(e)     = 2a²σ²/(1+a)      → std(e)     ≈ 0.953·σ
Var(error) = 1.0323·σ²        → std(error) ≈ 1.016·σ
std(step deviation) = std(error)/16       ≈ 0.0635·σ
```

Two conclusions, both important:

1. **The grid stays clean.** `std(e) ≈ 0.95σ` means the grid does *not* chase the noise — the raw
   record instant wanders around the grid by ~σ, exactly as intended.
2. **Step jitter is attenuated ~15.7×.** This is the whole benefit, and it is a fixed factor set by
   the `/16` constant, not by anything about the platform.

### 1.3 The residual, in dip

At the measured launch velocity `v₀ ≈ 2650 dip/s` (`00-verdict.md §2.3`), nominal 120 Hz step
`22.1 dip`:

| Frame-clock source | step-time jitter (rms) | position ripple (rms) | % of one step |
|---|---|---|---|
| raw `TimestampInTicks` (before this change) | 900 µs | **2.39 dip** | 10.8 % |
| **phase-locked estimator (shipped)** | ~57 µs | **0.15 dip** | **0.68 %** |
| `qpcVBlank` at the record (category a) | 17.8 µs | 0.047 dip | 0.21 % |
| `COMPOSITION_FRAME_STATS.targetTime` (category b) | ≤ 20 µs | ≤ 0.05 dip | ≤ 0.24 % |

The estimator removes **93.7 %** of the position ripple. A real timestamp removes **98 %**.

**The marginal gain of the platform source over the estimator is 0.10 dip rms.** At 1× that is a tenth
of a physical pixel; at 3× it is three tenths. It is below the raster grid the frame is drawn on. No
observer can see it, and no capture from `ScrollDiagnostics` (which records `-AnchorPoint.Y` as a
double, `ScrollContentPresenter.Managed.cs:185-190`) can distinguish it from rounding in the paint.

### 1.4 The one thing a real timestamp gives that the estimator structurally cannot

The derivation in §1.2 assumes white jitter. The estimator is a **rate-tracking loop** — it must
follow slow drift in the record instant or the grid diverges — so slow drift passes through:

> For a record-instant excursion that is slow compared to the 16-frame time constant, the estimator
> passes its **slew rate** into the step essentially 1:1.

Concretely: if measure/arrange cost walks by 3 ms over 60 frames (0.05 ms/frame), the estimator emits
0.05 ms/frame of step error = **0.13 dip** — the same order as the white-noise residual, not worse. To
get a *visible* residual you would need the record instant to drift by more than ~0.4 ms/frame
sustained, i.e. 24 ms over 60 frames, at which point the app is already missing its frame budget and
the motion is broken for reasons no clock fixes.

A real timestamp is immune to all of this by construction. That immunity is worth **0.1–0.2 dip**.

### 1.5 The honest counter to my own headline number

σ = 0.90 ms came from a probe with *uniform-random 0–3 ms* simulated record cost — white by
construction. Real record-phase noise during a fling over a `ListView` is **not** white: container
realization is correlated with scroll position, so it is quasi-periodic. The estimator's corner is at
`α·f_s/(2π) ≈ 1.2 Hz` at 120 Hz — realization bursts at one per 5–10 frames (12–24 Hz) sit an order of
magnitude above it and are attenuated normally; only structure slower than ~1 s would leak, and that
is drift, covered by §1.4.

**UNVERIFIED:** the actual spectrum of record-phase noise during a real fling. This is the single
measurement that could overturn §1.3, and it is obtainable *today* from the existing
`ScrollDiagnostics` capture (`00-verdict.md §4`, metric A) with no new code. Do that before spending
on platform plumbing.

---

## 2. The four named weaknesses, priced

### 2.1 Warm-up — the first 8 frames pass through raw

`Compositor.skia.cs:262-265` returns `raw` until `_frameDeltaCount >= 8`. And because `GetFrameTimestamp`
is only reached under `if (FrameStarting is { } …)` (`:308`), the ring is **only fed while a driver is
subscribed** — so those 8 frames are 8 *fling* frames, not 8 idle frames.

- **Bite:** 8 frames × full raw jitter, at the moment velocity is highest. 67 ms at 120 Hz, 133 ms at 60 Hz.
- **How often:** the ring never resets, so this is **once per process**, on the first fling (or first
  wheel decay) ever performed. Every subsequent fling inherits a full ring.
- **Compounding:** during warm-up `FrameIntervalInTicks` (`:220-222`) returns the hardcoded
  `TicksPerSecond/60`, and that value is what back-dates `_flingStartTimestamp`
  (`ScrollContentPresenter.Managed.cs:625`). On a 120 Hz panel the first fling of the process is
  back-dated by 16.7 ms instead of 8.3 ms — its **first frame jumps by two steps** (≈44 dip).
- **Does a real timestamp fix it?** Yes, completely, from frame 1.
- **Cheaper fix:** seed the ring from the host's known refresh rate. Win32 already computes it
  (`Win32WindowWrapper.DisplayInformation.cs`, `DEVMODEW.dmDisplayFrequency`, cited in `01-win32.md §9`);
  Android has `Display.RefreshRate`. That is a *one-value-per-window* hint, not a per-frame timestamp —
  ~1 % of the plumbing cost for 100 % of this weakness.

**Verdict: real, cheap to fix without platform per-frame plumbing. Not an argument for the platform source.**

### 2.2 A refresh-rate change mid-fling — the estimator's genuinely bad case

A 32-sample median needs **17 new samples** before it flips. Trace 60 → 120 Hz mid-fling:

- `period` stays 16.67 ms while the real delta is 8.33 ms.
- `error = raw − (clock + period) ≈ −8.33 ms` each frame; after two frames `|error| ≥ period`, so the
  slip branch fires with `Round(−1.0) = −1` → **net advance for that frame is exactly zero**
  (`period + (−1)·period`).
- Result: the grid alternates `+16.67 ms, 0, +16.67 ms, 0 …` for ~17 frames (≈142 ms at 120 Hz).

The *average* rate stays correct; the *step* ripple is **100 %** — one frozen frame in two, at full
velocity. That is worse than the defect this whole change exists to fix.

- **How often:** on desktop, rare. **On Android, routine** — adaptive refresh means starting to scroll
  can itself bump the panel 60 → 120. This is the strongest single argument for the platform source
  anywhere in this document.
- **Does a real timestamp fix it?** Yes, on the first frame.
- **Cheaper fixes, in order:** (a) the same refresh-rate hint as §2.1, applied as a ring reset;
  (b) shrink the window to 8–12 samples (halves the lag, roughly doubles the white-noise residual to
  ~0.3 dip — still invisible); (c) detect a sustained departure (≥3 consecutive deltas outside
  ±25 % of the median) and reset the ring.

**Verdict: real, and the worst weakness. Fixable in-process for a fraction of the cost. On Android specifically,
`frameTimeNanos` is already delivered to `ChoreographerFramePacer.FrameCallback.DoFrame` and discarded
(`ChoreographerFramePacer.cs:99`) — but `02-android.md §5` shows it arrives on a private Looper thread
*after* the present, so it is not a one-liner either.**

### 2.3 A genuinely variable frame rate

Not jitter — a real 1-vsync / 2-vsync alternation (classic judder). With 50/50 deltas of `T` and `2T`,
`MedianFrameDelta` (`:292-299`) returns `sorted[16]` of 32 = the **upper** median = `2T`. A real `T`
frame then produces `error = −T`, which is `< period = 2T`, so it takes the *gentle* branch: the grid
runs ahead by `T` per short frame, corrected only at `T/16`. Drift accumulates until `|error| ≥ 2T`,
then a whole-period slip zeroes one frame's advance. Net: intermittent frozen frames.

- **Does a real timestamp fix it?** Yes — a real per-frame timestamp reports `T` and `2T` truthfully
  and the curve advances correctly.
- **But:** an app alternating 1 and 2 vsyncs is missing its budget every other frame. The curve being
  sampled correctly makes the *motion* correct; it does not make it *smooth*, because the display is
  showing 1-then-2 intervals of the same image regardless. This is a frame-budget problem wearing a
  clock costume.

**Verdict: real, fixed by the platform source, but fixing it does not deliver smoothness.**

### 2.4 "The median is the FRAME period, not the VSYNC period" — this is a feature, not a bug

The premise inverts. If the app produces 60 records/s on a 120 Hz panel and each record is presented
for two refresh intervals, then **the image on screen changes 60 times per second**, and the curve
must be sampled on a 60 Hz grid. The estimator measures the *presented* cadence, which is the correct
one. A naive "snap to the panel's vsync period" implementation would be **wrong here** — it would
either advance half as fast or compute positions for frames that are never shown.

This is exactly what Flutter's Windows embedder gets right by accident and its Linux embedder gets
wrong on purpose: `07-flutter.md §2.5` documents `VsyncWaiterFallback` snapping to a hardcoded 60 Hz
grid regardless of the actual display.

**Verdict: not a weakness. The estimator is *more* correct than a panel-period grid on this axis.**

---

## 3. The residual that NEITHER fixes — and it is the big one

### 3.1 Record→present latency variance

Both sources answer "what time is this frame for?". Neither answers "how many refresh intervals will
elapse before it is on glass?" The Win32 pipeline makes the variance concrete:

- The UI thread records into `_lastRenderedFrame` under `_frameGate`
  (`CompositionTarget.Rendering.skia.cs:135-155`, slot written at `:147`).
- A **separate** render thread picks it up in `Draw` (`:232-241`), presents, then blocks on
  `DwmFlush` (`Win32RenderPacer.cs:59-82`), driven by an `AutoResetEvent`
  (`Win32WindowWrapper.RenderThread.cs:17`, `:41-45`, `:52-89`).

Nothing couples the record's completion to a present deadline. If a record finishes just before the
render thread's wake in one frame and just after it in the next, those two records are presented 1 and
2 refresh intervals later respectively — **while their computed positions are uniformly spaced by
exactly one grid period.** The displayed motion then shows a 0-step frame followed by a 2-step frame:

> **~22 dip of displayed position error, versus the 0.10 dip the platform clock would recover.**
> 150× larger, and completely invisible to both clock designs.

This is the "if metric B does not move" branch that `00-verdict.md §4` already predicted, and it is
why the platform clock cannot be assumed to be the next step.

**UNVERIFIED:** how often the record actually crosses a present deadline during a fling. Measuring it
needs a present-side timestamp taken on the render thread — which is a *different*, cheaper piece of
plumbing than a vsync clock, and it is the one that should be built first if §1.5's spectrum comes
back clean.

### 3.2 `ReturnFrame` duplicate presents — real, but I checked and it is *not* a corruption

`Draw` borrows the frame (`_lastRenderedFrame = null`, `:238`), presents it, resets the damage path
(`:309`), then `ReturnFrame` (`:412-434`) puts the **same tuple** back if the UI thread has not
produced a newer record. A second present of that tuple therefore clips to an **empty** damage path
(`:285-292`) and draws nothing.

I initially read this as presenting a stale back buffer. **It is not**, on either Win32 path:

- Vulkan renders into a persistent *intermediate* image and blits that to the swapchain
  (`Win32WindowWrapper.Rendering.Vulkan.cs:118-127`), so "draw nothing" re-blits the identical, correct image.
- Software BitBlts a persistent raster surface, same reasoning.

So a duplicate present is a duplicate of **identical pixels** — a held frame, not a corrupt one. Its
effect on smoothness is exactly §3.1's: the displayed position holds for an extra refresh interval.
The frame clock's whole-period slip (`:276-281`) will correctly account for the *missing record*, but
nothing accounts for the *extra display interval* of the record that was held.

**UNVERIFIED** on Android/macOS/X11 render paths — I only read the Win32 renderers.

### 3.3 Realize-after-record

Unchanged from `00-verdict.md §3`: the fling's offset is produced inside the record
(`Compositor.skia.cs:316` raises `FrameStarting`, `:352` paints), so the picture shows `offset_n` with
containers realized for `offset_{n−1}`. Neither clock touches this. Restated here only so nobody
banks it against the clock work.

---

## 4. Bugs in `GetFrameTimestamp` as written

### 4.1 HIGH — one frame clock, N independent record loops

`Compositor` is a **process-wide singleton** (`Compositor.cs:17`, `:40`), and every XAML root visual is
created on it (`Panel.CreateElementVisual() => Compositor.GetSharedCompositor()…`, `Panel.cs:49`).
But records are **per `CompositionTarget`**, and there is one per `ContentRoot`
(`ContentRoot.cs:78`), with `ContentRoots` being a `List` (`ContentRootCoordinator.cs:17`, `:27`).

The kicker is `CoreServices.OnTick`, which loops **every window** in a single dispatcher tick:

```
CoreServices.cs:108   foreach (var window in ApplicationHelper.WindowsInternal)
CoreServices.cs:115       root.UpdateLayout();
CoreServices.cs:124       (…CompositionTarget)?.OnRenderFrameOpportunity();     // records, right here
```

So with two animating windows, two records land microseconds apart and both feed the **same**
`_frameDeltas` ring and advance the **same** `_frameClock`.

Consequences (analysis, not measurement):

- **2 roots:** the median lands on the real period, and the near-zero delta triggers the whole-period
  slip with `Round(−1) = −1`, i.e. zero advance. Both windows end up with identical, correctly-spaced
  timestamps. **It works by luck**, and the luck depends on which side of `sorted[16]` the tie falls.
- **3+ roots:** the near-zero deltas outnumber the real ones, `sorted[count/2]` returns a *near-zero*
  period, and with `period ≈ ε` the slip branch computes `Round(error/ε)·ε ≈ error`, so
  `_frameClock ≈ raw`. **The estimator silently degrades to the raw clock it was written to replace** —
  no exception, no log, no symptom other than the original defect coming back.

**Fix:** the frame clock is a property of a *presentation surface*, not of the compositor. Move
`_frameDeltas` / `_frameDeltaIndex` / `_frameDeltaCount` / `_lastRawFrameTimestamp` / `_frameClock` /
`CurrentFrameTimestampInTicks` onto `CompositionTarget` (or key them by the `ICompositionTarget`
resolved from `rootVisual.CompositionTarget`), and pass the resulting timestamp into
`RenderRootVisual`. Note this is *also* what a real platform timestamp gives for free, since both
windows in one tick would receive the same vsync stamp — a genuine, if narrow, point for the platform side.

### 4.2 HIGH — the grid is not monotonic

From `:273-281`, the net advance of one call is:

```
advance = period · (1 + Round(error / period, AwayFromZero))
```

which is **negative** whenever `Round(error/period) ≤ −2`, i.e. `error ≤ −1.5·period`, i.e.

> `raw ≤ _frameClock_previous − 0.5·period`
> — a record landing more than half a period *before* the previous frame's grid value.

Reachable via §4.1 (two records in one tick), and via any burst where the render thread wakes the
record loop twice inside one refresh interval.

What it costs, specifically: `GetFrameTimestamp` feeds `OnFlingFrame`, which computes
`elapsed = (timestampInTicks − _flingStartTimestamp)/TicksPerSecond`
(`ScrollContentPresenter.Managed.cs:628`) and calls `ScrollFlingSimulation.GetPosition(elapsed)`. On
the non-Apple curve that is

```
ScrollFlingSimulation.cs:95   var u = 1.0 - Math.Clamp(t / _duration, 0.0, 1.0);
ScrollFlingSimulation.cs:96   return _start + _distance * (1.0 - Math.Pow(u, DecelerationRate));
```

For `t ≤ 0` the clamp yields `u = 1` and the position collapses to **`_start`** — the offset the fling
launched from. So a backward slip in the first frames of a fling **snaps the content back to the
launch point**, which is precisely the user-fighting artifact the project has ruled out.

**Fix:** clamp the multiplier, e.g. `var steps = Math.Max(0, Round(error/period));` — or, better,
make monotonicity an explicit post-condition of the method rather than an emergent property of the
arithmetic.

### 4.3 MEDIUM — two drivers mix the grid with the raw clock

`GetFrameTimestamp` introduces a **second time base**: the grid sits at the *mean* record offset, up to
~σ (≈1 ms) either side of `TimestampInTicks` at any instant. Any driver that latches raw and then
subtracts grid stamps is broken by construction.

- **`StartFling` does it right.** `_flingStartTimestamp = 0` at `ScrollContentPresenter.Managed.cs:597`,
  anchored on the first grid tick at `:621-625`. No mixing.
- **`AddWheelImpulse` does not.** `var now = compositor.TimestampInTicks;` at `:669` is fed to
  `_wheelDecayH.Start(HorizontalOffset, now)` at `:673`, then ticked with grid stamps. Contained by the
  `elapsed <= 0` guard in `ScrollDecaySimulation.cs:61-64` — worst case one skipped frame at wheel start.
- **`CompositionInertiaProcessorTimer` does not, and is unguarded:**
  ```
  GestureRecognizer.Manipulation.InertiaProcessor.cs:355   _startTimestamp = compositor.TimestampInTicks;   // raw
  GestureRecognizer.Manipulation.InertiaProcessor.cs:356   _handler = timestamp => onTick(TimeSpan.FromTicks(timestamp - _startTimestamp));  // grid
  ```
  The first `elapsed` can be **negative** by up to ~1 ms. `Process` then does
  ```
  GestureRecognizer.Manipulation.InertiaProcessor.cs:219   UpdateCumulative(elapsed.TotalMilliseconds, …)
  GestureRecognizer.Manipulation.InertiaProcessor.cs:221   _t0 + (ulong)elapsed.TotalMicroseconds
  ```
  Converting a negative `double` to `ulong` in an unchecked context is **unspecified** in C# and
  yields an arbitrary value on x64 — a corrupted `ManipulationState` timestamp on the first inertia tick
  of every non-scroll manipulation. This is a latent crash-class defect introduced by the clock change,
  not by the inertia processor.

**Fix:** publish the grid as the only clock drivers may read against — e.g. a
`Compositor.LastFrameTimestampInTicks` that returns `_frameClock` — and make raw `TimestampInTicks`
off-limits for anything that will later receive a `FrameStarting` stamp.

### 4.4 MEDIUM-LOW — idle gaps are recorded as frame deltas

`GetFrameTimestamp` is only called under `if (FrameStarting is { } …)` (`:308`), and the ring state
persists across the unsubscribed gap. So the first record of every new fling pushes
`raw − _lastRawFrameTimestamp` — an arbitrary idle gap, possibly minutes — into `_frameDeltas` as if it
were a frame period.

- **The grid itself recovers in one frame**: the slip branch lands within ±0.5 period of `raw`. Fine.
- **The ring does not self-clean.** One outlier in 32 does not move the median. Many short flings in
  quick succession inject one outlier each; the ratio stays roughly 1 gap per N fling-frames, so the
  median only breaks for flings shorter than ~1 frame — **not reachable in practice**, but the design
  has no margin and no assertion protecting it.
- `FrameIntervalInTicks` (`:220-222`) reads the same median and feeds the launch back-date
  (`ScrollContentPresenter.Managed.cs:625`), so a poisoned median becomes a *position* error, not just
  a timing one.

**Fix:** reject deltas beyond a few multiples of the current median before they enter the ring, and
zero `_lastRawFrameTimestamp` when `FrameStarting` transitions to null.

### 4.5 Things you asked me to check that are **clean**

| Suspect | Finding |
|---|---|
| **Integer overflow on the delta ring index** | **No bug.** `_frameDeltaIndex = (_frameDeltaIndex + 1) % FrameClockWindow` (`:256`) operates on a value always in `[0,31]`, so the maximum intermediate is 32. It cannot overflow. (Even the draft form in `00-verdict.md §4`, `_rawDeltaIndex++ & 31`, would have been safe: `int.MinValue & 31 == 0`.) |
| **`error / 16` truncation toward zero for negative errors** | **No bug, no asymmetry.** C# integer division truncates toward zero for **both** signs, so the deadband is symmetric at ±15 ticks (±1.5 µs) — ±0.004 dip at 2650 dip/s. And it cannot ratchet: `period` is re-derived from the median every frame, whose quantization error is ≤1 tick (100 ns), an order of magnitude *inside* the deadband, so there is no uncorrected rate term to accumulate. |
| **First-frame behaviour** | **Correct.** `_lastRawFrameTimestamp == 0` (`:246-250`) sets both the anchor and `_frameClock` to `raw` and returns it. The only wrinkle is that `0` is used as the "unset" sentinel for a value derived from `Stopwatch.GetTimestamp()`; a genuine zero is unreachable in practice but the sentinel is undocumented. |
| **Median across a refresh-rate change** | **Correct code, bad behaviour** — see §2.2. `MedianFrameDelta` (`:292-299`) is itself right: `_frameDeltas.AsSpan(0, _frameDeltaCount)` is valid because the ring fills `0..count-1` in order during warm-up, `sorted[count/2]` is a standard upper median, and the `stackalloc[32]` always fits. The defect is the 17-frame flip latency, not the implementation. |
| **Repeated subscribe/unsubscribe of `FrameStarting`** | **Grid recovers, ring does not** — see §4.4. No leak: `Visual.Compositor.FrameStarting -= …` is paired in `StopFling` (`ScrollContentPresenter.Managed.cs:616`), `StopWheelDecay` (`:699`) and `CompositionInertiaProcessorTimer.Stop` (`InertiaProcessor.cs:362-366`). |
| **Thread-safety — is `RenderRootVisual` really UI-thread-only?** | **Verified, yes.** The `Compositor` overload (`Compositor.skia.cs:301`) has exactly **one** caller: `SkiaRenderHelper.RecordPictureAndReturnPath` (`SkiaRenderHelper.skia.cs:44`), whose only caller is `CompositionTarget.Render()` (`CompositionTarget.Rendering.skia.cs:119`), which opens with `NativeDispatcher.CheckThreadAccess()` (`:114`). The other four `RenderRootVisual` call sites — `RenderTargetBitmap.skia.cs:146`, `AlphaMaskSurface.skia.cs:52`, `CompositionVisualSurface.skia.cs:21`, `RedirectVisual.skia.cs:16` — all bind the **`Visual`** overload (`Visual.skia.cs:327`), which never touches the frame clock. Readers of the derived state (`FrameIntervalInTicks` from `OnFlingFrame`, `CurrentFrameTimestampInTicks` from `OnDiagnosticsFrame`) are on the same thread. **There is no data race.** The §4.1 defect is *not* concurrency — it is one clock serving N record loops on one thread. |

---

## 5. What the mature stacks actually do — the prior-art argument

This is where the "obviously we should use the real value" intuition falls apart. From the sibling notes:

| Stack | Has a real vsync/present time? | Feeds it to the curve? |
|---|---|---|
| **WinUI 3 / dxaml** | Yes (`DCompositionGetStatistics.targetTime`, exact to ±20 µs) | **No.** Zero hits for `DCompositionGetFrameId`/`COMPOSITION_FRAME_STATS`/`nextEstimatedFrameTime` in `dxaml`. The UI clock is a plain QPC read taken *before* the vblank wait (`compositorscheduler.cpp:317` vs `:345`). WinUI's dependent animations have **our** jitter. It avoids the scroll defect by moving the curve into the compositor (DManip / InteractionTracker), not by fixing the clock. — `06-winui.md §0.2-0.3` |
| **Avalonia** | Yes, on Android/Browser/macOS/iOS | **No.** `DefaultRenderLoop.TimerTick(TimeSpan)` ignores its parameter; three unrelated `Stopwatch`es drive server animations, UI animations and scroll inertia. — `08-avalonia.md §0` |
| **Flutter, Windows** | No vsync input at all | **Synthesizes a grid**: `SnapToNextTick(now, 0, frame_interval)` with the period from the *refresh rate* and the phase anchor hardcoded to zero. It never reads `qpcVBlank`. — `07-flutter.md §0.4, §2.4` |
| **Flutter, Linux (GTK)** | No | Snaps to a hardcoded **60 Hz** grid regardless of the display. — `07-flutter.md §2.5` |
| **Flutter, Android/iOS/macOS** | Yes | **Yes** — passes the *predicted presentation time*, category (b). — `07-flutter.md §0.1` |
| **WPF** | Yes | **Yes, and in exactly the shape Uno should copy**: `_estimatedNextPresentationTime`, a vsync grid **anchored on the compositor-reported present time**, with half-frame hysteresis so it doesn't re-derive every frame. — `06-winui.md §0.4`, `MediaContext.cs:1080-1181` |

Two readings, and both support the recommendation:

1. **On Windows and Linux, "just use the real value" is not what anybody does.** Flutter reconstructs
   it, more crudely than Uno now does (constant phase anchor vs. a tracked one). Uno's estimator is
   already *better* than the shipped Flutter Windows embedder.
2. **Where a stack does use the real value, it uses it as an *anchor for a grid*, not as the value.**
   WPF is the proof. Nobody feeds a raw per-frame platform timestamp straight to a curve, because a
   real timestamp still needs hysteresis to avoid re-deriving a new estimate every frame. **The
   estimator is not the thing you throw away when the platform value arrives — it is the thing the
   platform value plugs into.** That reframes the choice: this is not "estimator *or* platform", it is
   "build the grid now, improve its anchor later, or never."

---

## 6. Recommendation

### 6.1 Ship the estimator alone. Do not start per-frame vsync plumbing.

Justified by numbers:

- The estimator removes **93.7 %** of the position ripple (2.39 → 0.15 dip rms). §1.3
- The platform source would remove a further **0.10 dip rms** — below one physical pixel at every
  density Uno ships on. §1.3
- The residual that **neither** removes is up to **22 dip** (a full step) and has never been measured. §3.1
- The estimator's two genuine failure modes (§2.1, §2.2) are both fixed by a **refresh-rate hint** —
  one value per window, already computed on Win32 (`DEVMODEW.dmDisplayFrequency`) and trivially
  available on Android (`Display.RefreshRate`) — at ~1 % of the cost of per-frame vsync plumbing on
  five targets.
- Two of the five targets (Windows, Linux) have no mature-stack precedent for per-frame plumbing at
  all; the mature stacks reconstruct there. §5

**Blocking prerequisites — do these before calling the estimator shipped:**

| # | Fix | Severity |
|---|---|---|
| 1 | Move the frame-clock state from `Compositor` to `CompositionTarget` (§4.1) | HIGH — silently degrades to raw with 3+ content roots |
| 2 | Clamp the whole-period slip so the grid cannot move backward (§4.2) | HIGH — snaps a fling back to its launch offset |
| 3 | Publish the grid for latching; stop drivers reading raw `TimestampInTicks` (§4.3) | MEDIUM — unspecified `(ulong)` conversion in `InertiaProcessor.Process` |
| 4 | Reject idle gaps at the ring boundary; reset on unsubscribe (§4.4) | MEDIUM-LOW |
| 5 | Seed the period from the host refresh rate; reset the ring on a refresh change (§2.1, §2.2) | MEDIUM — kills both remaining estimator-specific weaknesses |

Fixes 1 and 2 are not optional. As written, the estimator is a **regression risk** on multi-window
apps and can produce exactly the snap-back artifact the project has ruled out — both of which are
worse than the 2.39 dip it was written to remove.

### 6.2 The trigger to revisit — and it is not "the estimator wasn't good enough"

Run the `00-verdict.md §4` capture with the estimator in place (`FeatureConfiguration.ScrollViewer.EnableDiagnostics`).

- **Metric B collapses to drag's level →** the clock work is done. The remaining 0.10 dip is not worth
  five platforms of plumbing. **Close the clock track.**
- **Metric B does not move →** the residual is downstream of the record (§3.1/§3.2), and a platform
  *clock* would not have helped either. The next instrument is a **present-side timestamp on the
  render thread**, not a vsync clock on the UI thread. Build that instead.
- **Metric A shows record-phase noise with structure slower than ~1 s (§1.4/§1.5) →** that is the one
  spectrum the estimator cannot suppress, and the only measurement that would justify the platform
  source on its own merits.

### 6.3 If it is ever revisited, do it in this shape

- **Win32 first.** The epoch is free — `Stopwatch.GetTimestamp()` *is* QPC, verified by bracketed reads
  (`06-winui.md §6`), and `s_tickFrequency` is already the documented scale factor
  (`Compositor.cs:33-38`). `COMPOSITION_FRAME_STATS.targetTime` is a category-(b) predicted present time,
  quantized to ±20 µs, from one P/Invoke pair. And the machine to measure the result on is this one.
- **As an anchor, never a substitution.** Feed the platform value into `GetFrameTimestamp` as the
  *phase reference* and keep the grid, the hysteresis and the whole-period slip. That is WPF's
  `_estimatedNextPresentationTime`, and it is also the only shape where the fallback path (Linux, GL,
  degraded `DwmFlush`, `01-win32.md §9`) stays on the same code as the fast path instead of being an
  untested second implementation.
- **Never mid-fling source switching** without routing the change through the whole-period slip — a
  real vsync timestamp sits at a *different phase* than the record instant, so a bare handover is a
  step discontinuity in the curve argument, i.e. exactly the artifact being fixed.
- The epoch objection from `00-verdict.md §6.4` ("epoch mismatch blocks the obvious platform fix") is
  **refuted by the sibling notes** on every major target: Windows QPC ≡ `Stopwatch` (`06-winui.md §6`,
  measured); Apple `CLOCK_UPTIME_RAW` ≡ `mach_absolute_time` ≡ `CACurrentMediaTime` (`03-apple.md §0, §3`,
  read from runtime source); Android/Linux `CLOCK_MONOTONIC` both sides (`02-android.md §3`,
  `05-x11.md §3`); browser `performance.now()` both sides (`04-wasm.md §3`). Cost of the platform path
  is therefore *lower* than §6.4 assumed — which changes the price, not the value. The value is still
  0.10 dip.

---

## 7. Explicitly UNVERIFIED

1. Every number in §1.3 is a **derivation** (§1.2) applied to a **quoted measurement** (§1.1) taken by a
   synthetic probe, not by Uno. No Uno build was compiled or run for this note.
2. The spectrum of real record-phase noise during a fling over virtualized content (§1.5). This is the
   one input that could overturn §1.3.
3. How often the record actually crosses a present deadline (§3.1). Nothing in this repo measures it.
4. §3.2's "duplicate present is harmless" conclusion covers only the **Win32** Vulkan and software
   renderers. Android, macOS, X11 and the Win32 OpenGL path were not read.
5. §4.1's per-root-count outcomes (works-by-luck at 2, degrades-to-raw at 3+) are analysis of the
   arithmetic, not observation. The *sharing* itself is verified from source; the specific interleaving
   depends on which targets have `RenderRequested` set in a given tick.
6. Whether adaptive refresh actually changes the panel rate mid-fling on the target Android devices
   (§2.2) — the mechanism is documented, the occurrence on Uno's supported hardware is not confirmed here.
7. All external-stack claims in §5 are quoted from the sibling notes in this folder, which cite their
   own sources; I re-read none of `dxaml`, `flutter`, `Avalonia` or `wpf` for this document.
