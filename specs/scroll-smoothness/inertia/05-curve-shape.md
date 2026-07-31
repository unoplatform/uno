# 05 — Is the fling curve the problem? (shape analysis)

**Brief:** question the premise. Maybe the motion *is* smooth and what reads as "not smooth" is the
**shape** of the curve. Analyse `ScrollFlingSimulation` numerically, compare against Android's real
`OverScroller` SPLINE tables and against iOS, and check the termination for a discontinuity.

**Verdict up front:**

> **The interior of the curve is exonerated.** Uno's Android fling is monotone, C¹, starts at exactly
> the launch velocity, travels *exactly* Android's spline distance, and ends at *exactly* zero
> velocity with a final per-frame step of 1e-4 dip. Measured against the real AOSP SPLINE table it is
> **smoother than Android's own curve** (Android's 100-entry table produces a visible saw-tooth in
> per-frame displacement; Uno's analytic form does not). "It decelerates too fast at the start" is
> **false** (max 5.9% deviation). "It stops abruptly" is **false for the normal case**.
>
> Three real shape-level defects survive, and **only one of them explains the drag/inertia
> asymmetry**: the **launch seam** — the fling's *time* origin is the wall clock at UP-processing
> while its *position* origin is the last touch sample, and the gap between those two clocks is
> injected as a single anomalous frame step of **0.95x–1.5x the nominal step** at the exact moment
> the finger leaves the glass (§6). The other two — the hard edge stop (§5) and the 140 ms sub-pixel
> creep tail (§7) — are real but do not fit the "whole fling feels worse" report.

Reproduction scripts (throwaway): the AOSP spline table, Uno's simulation, Flutter's cubic and the
iOS exponential were all re-implemented and cross-checked. All numbers below are generated, not
estimated.

---

## 1. What the code actually computes

`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollFlingSimulation.cs`

```
:25  DecelerationRate r = ln(0.78)/ln(0.9)      = 2.3582018
:26  Inflexion                                  = 0.35
:27  Friction                                   = 0.015
:32  PhysicalCoefficient = 9.80665·39.37·96·0.84 = 31134.12      (DIP space, 96 dip/inch)
:62  referenceVelocity   = Friction·Coeff/Inflexion = 1334.32 dip/s
:69  androidDuration T_a = (|v₀|/refV)^(1/(r−1))
:70  duration        T   = r · Inflexion · T_a        = 0.82537 · T_a
:71  distance        D   = v₀ · T / r
:96  x(t) = start + D·(1 − (1 − t/T)^r)
:113 v(t) = v₀·(1 − t/T)^(r−1)
```

**Cross-check against AOSP `OverScroller.SplineOverScroller`** (`getSplineFlingDuration` /
`getSplineFlingDistance`):

* Android's spline duration is `exp(l/(r−1))` with `l = ln(0.35·v/(friction·coeff))` — this is
  **identical** to Uno's `androidDuration` at `:69`. ✔
* Android's spline distance is `friction·coeff·exp(r/(r−1)·l)`, which algebraically reduces to
  `0.35·v₀·T_a`. Uno's `distance` at `:71` = `v₀·(r·0.35·T_a)/r` = `0.35·v₀·T_a`. **Bit-for-bit the
  same distance.** ✔

| v₀ (dip/s) | Android T_a | Android D | Uno T | Uno D | T ratio | D ratio |
|---|---|---|---|---|---|---|
| 800  | 0.6862 s | 192.1 | 0.5663 s | 192.1 | **0.8254** | **1.0000** |
| 1500 | 1.0900 s | 572.2 | 0.8997 s | 572.2 | 0.8254 | 1.0000 |
| 2650 | 1.6573 s | 1537.1 | 1.3679 s | 1537.1 | 0.8254 | 1.0000 |
| 5000 | 2.6449 s | 4628.5 | 2.1830 s | 4628.5 | 0.8254 | 1.0000 |

The 0.8254 duration factor is **not a bug** — it is forced. For the power form `D(1−(1−t/T)^r)`,
`v(0) = D·r/T`; pinning `v(0)=v₀` *and* `D = 0.35·v₀·T_a` leaves exactly one `T`, namely
`r·0.35·T_a`. It is the price of choosing a single power law instead of Android's spline. Effect:
every Uno fling **ends 12–46 % of a second earlier than Android's** while covering the same ground —
snappier, not rougher.

> ⚠️ Flutter, which the file's comment cites as the source, does **not** use this power form. Current
> `ClampingScrollSimulation` keeps Android's full duration `T_a` and applies a cubic penetration fit
> `1.2t³ − 3.27t² + 3.065t`. That cubic is included in every comparison below.

---

## 2. Per-frame displacement at 120 Hz — first 30 frames (v₀ = 2650 dip/s)

Steady-state step at launch = 22.083 dip/frame.

| frame | t (ms) | Android dx | **Uno dx** | Flutter dx | Uno/Android |
|---:|---:|---:|---:|---:|---:|
| 0 | 0.0 | 22.0576 | **21.9920** | 21.9651 | 0.997 |
| 2 | 16.7 | 22.0211 | **21.6278** | 21.4951 | 0.982 |
| 4 | 33.3 | 21.9023 | **21.2651** | 21.0305 | 0.971 |
| 6 | 50.0 | 21.7239 | **20.9041** | 20.5710 | 0.962 |
| 8 | 66.7 | 21.4896 | **20.5447** | 20.1168 | 0.956 |
| 10 | 83.3 | 21.2342 | **20.1870** | 19.6679 | 0.951 |
| 12 | 100.0 | 20.8992 | **19.8309** | 19.2241 | 0.949 |
| 14 | 116.7 | 20.5484 | **19.4766** | 18.7857 | 0.948 |
| **15** | **125.0** | 20.5106 | **19.3000** | 18.5684 | **0.941 ← worst** |
| 18 | 150.0 | 19.7589 | **18.7729** | 17.9245 | 0.950 |
| 22 | 183.3 | 18.8319 | **18.0761** | 17.0843 | 0.960 |
| 26 | 216.7 | 17.8771 | **17.3863** | 16.2651 | 0.973 |
| 29 | 241.7 | 17.3051 | **16.8736** | 15.6645 | 0.975 |

**Answer to "does it decelerate too fast at the start?" — no.**

* First frame is **99.7 %** of Android's.
* Worst-case deficit over the whole launch phase is **5.9 %**, at t ≈ 125 ms.
* By t ≈ 250 ms Uno is *ahead* of Android, and it stays ahead (max position lead +55.8 dip at
  t ≈ 1003 ms for v₀ = 2650, +167.9 dip at t ≈ 1601 ms for v₀ = 5000).
* The step sequence is **strictly monotone decreasing with a smooth ratio** (0.9970 → 0.9888 →
  0.9821 → …). There is no frame-to-frame irregularity to perceive.

Note the *Android* column: 22.0576, 22.0572, 22.0211, 22.0185, 21.9023, 21.8963 — pairs of nearly
equal values then a drop. That is the 100-sample `SPLINE_POSITION` table being linearly interpolated
inside `computeScrollOffset`. **Real Android has a piecewise-linear velocity profile with a visible
saw-tooth; Uno's is analytic and cleaner.** If the shape were the problem, Uno should feel *better*
than Android here, not worse.

---

## 3. Per-frame displacement — last 30 frames (v₀ = 2650 dip/s)

**Uno** (T = 1368 ms, 165 frames):

| frame | t (ms) | dx (dip) | dx ratio to prev |
|---:|---:|---:|---:|
| 145 | 1208.3 | 1.1509 | 0.9315 |
| 150 | 1250.0 | 0.7532 | 0.9084 |
| 155 | 1291.7 | 0.4053 | 0.8619 |
| 159 | 1325.0 | 0.1744 | 0.7676 |
| 161 | 1341.7 | 0.0813 | 0.6478 |
| 162 | 1350.0 | 0.0429 | 0.5271 |
| 163 | 1358.3 | 0.0125 | 0.2924 |
| **164** | **1366.7** | **0.0001** | 0.0077 |

**Android** (T = 1657 ms, 199 frames): last step 0.0268 dip, residual velocity 3.68 dip/s.
**Flutter** (T = 1657 ms): last step 0.7784 dip, **residual velocity 108.1 dip/s = 4.08 % of v₀** —
Flutter's cubic genuinely *does* cut off with a jolt (`_flingVelocityPenetration(1) = 0.125`), and
its per-frame step even *increases* over the last 200 ms (0.683 → 0.869) because the cubic turns
back up. Uno does not have this defect.

| v₀ | sim | last dx (dip) | v(T) dip/s | v(T)/v₀ |
|---:|---|---:|---:|---:|
| 2650 | Android OverScroller (SPLINE) | 0.02684 | 3.684 | 0.139 % |
| 2650 | **Uno ScrollFlingSimulation** | **0.00010** | **0.000** | **0.000 %** |
| 2650 | Flutter ClampingScrollSimulation | 0.77845 | 108.075 | 4.078 % |
| 5000 | Android | 0.02226 | 6.951 | 0.139 % |
| 5000 | **Uno** | **0.00833** | **0.000** | **0.000 %** |
| 5000 | Flutter | 0.65099 | 203.915 | 4.078 % |

**Answer to "does it stop abruptly?" — no, it is the softest of the three.**

Formally: `x''(t) ∝ (1−t/T)^(r−2)` and `r−2 = 0.358 > 0`, so acceleration → 0 as t → T. (Jerk
`∝ (1−t/T)^(r−3)` with `r−3 = −0.642 < 0` does diverge — 26x the launch jerk one frame from the end —
but the displacement in that frame is 0.0125 dip, so it is unobservable. Not a finding.)

---

## 4. Termination path in code — is there a discontinuity at the stop?

`ScrollContentPresenter.Managed.cs:613-633`

```csharp
var h = Math.Clamp(_flingH.GetPosition(elapsed), 0, maxH);          // :620
var v = Math.Clamp(_flingV.GetPosition(elapsed), 0, maxV);          // :621

var running = elapsed < Math.Max(_flingH.Duration, _flingV.Duration)
    && (h > 0 && h < maxH || v > 0 && v < maxV);                    // :624-625

if (!running) { StopFling(); }                                      // :627-630

Set(horizontalOffset: h, verticalOffset: v,
    options: new(DisableAnimation: true, IsTouch: true, IsIntermediate: running));  // :632
```

**a) Is the last frame's position the resting position?** **Yes.** `GetPosition` clamps
`t/_duration` to `[0,1]` (`ScrollFlingSimulation.cs:95`), so at `elapsed ≥ Duration` it returns
`start + distance` = `FinalPosition` (`:119`) exactly. No terminal jump. ✔

**b) Does the final `Set(IsIntermediate:false)` trigger `InvalidateArrange`?** **Yes** — but drag does
the same, so it is *not* the asymmetry.

`Set` → `Update` → `Updated(h, v, isIntermediate:false)` (`:529`) → `ScrollViewer.OnPresenterScrolled`
(`ScrollViewer.cs:1234`). With `isIntermediate == false` it takes the `else` branch at `:1244` →
`Update(false)` → `:1328-1337`:

```csharp
if (!isIntermediate && (oldHorizontalOffset != HorizontalOffset || oldVerticalOffset != VerticalOffset))
{
    InvalidateArrange();      // ScrollViewer.cs:1336
}
```

Because every *intermediate* fling frame goes through the deferred `RequestUpdate()` path
(`ScrollViewer.cs:1239-1243` → `:1301-1316`), `ScrollViewer.VerticalOffset` lags the presenter, so the
final tick almost always sees `old != new` and schedules an arrange. **Drag ends identically** —
`IDirectManipulationHandler.OnCompleted` (`:1026`) issues the same
`Set(DisableAnimation:true, IsTouch:true, IsIntermediate:false)`. Symmetric ⇒ does not explain the
asymmetry. (Whether that arrange *moves* anything — anchoring / `TrimOverscroll` — is out of this
brief's scope and is **UNVERIFIED** here; hand to the pipeline analysis.)

**c) `RecomputeOffsetsFromIntent` cannot fight the fling.** `IsScrollAnimationInProgress`
(`:128-143`) only detects a `KeyFrameAnimation` on `AnchorPoint`, which the fling does **not** use —
so the guard at `ScrollViewer.cs:1573-1578` is inert during a fling. It does not matter in practice
because `TryEnableDirectManipulation` calls `Scroller?.ClearOffsetIntents()` on touch press
(`:718`), disarming both intents for the whole gesture *and* the following fling. ✔ Not a factor.

---

## 5. Defect A — the **edge stop is a genuine discontinuity** ⚠️

`:624-625` terminates the moment the clamped position touches a bound, and `:632` applies the
clamped value. A fling that reaches the top/bottom of the extent therefore goes from its *current*
velocity to zero **in a single frame** — infinite deceleration, no overscroll, no glow, no bounce.

* Android's `EdgeEffect`/stretch decelerates the residual over ~150 ms.
* iOS rubber-bands and springs back.
* Uno: hard stop.

Note also the `||` structure: for a vertical-only scroller `maxH == 0`, so `h > 0 && h < maxH` is
always false and the whole condition rests on `v`. That is correct here, but it means a purely-horizontal
flick on a vertical-only list creates a `_flingH` with a nonzero duration that spins the render loop
(`Compositor.skia.cs:291` keeps requesting frames while `FrameStarting is not null`) while producing
no motion, for up to 2.2 s. Wasteful, and it keeps `Compositor.IsAnimating` true
(`Compositor.skia.cs:43`). **Minor / secondary.**

**Does it explain the asymmetry?** Only for flings that hit an end. Drag never produces infinite
deceleration because the finger cannot; when a drag hits the end the content simply stops following,
which reads as intentional. So this *is* asymmetric — but it should be reported as "it slams at the
end of the list", not "inertia is less smooth than drag".

---

## 6. Defect B — the **launch seam**, and the one that fits the report ⚠️⚠️

This is the shape defect at t = 0, and it is the only one whose mechanism explains why drag is fine
and inertia is not.

**Two different clocks are spliced together at the finger lift.**

* **Drag position is sample-derived.** `OnUpdated` accumulates raw manipulation deltas into
  `HorizontalOffset`/`VerticalOffset` (`:862-866`). The rendered position on any frame is therefore
  the position of the *newest touch sample processed before that frame's record* — it carries the
  sample's own timestamp implicitly, and it never consults a clock.
* **Fling position is clock-derived.** `StartFling` (`:588-600`) takes its **position** origin from
  `HorizontalOffset`/`VerticalOffset` (`:594-595`, i.e. the last touch sample) but its **time**
  origin from `compositor.TimestampInTicks` (`:593`) — a live `Stopwatch.GetTimestamp()` wall clock
  (`Compositor.cs:38`) read on the UI thread at the moment the UP / inertia-start is processed.
  `OnFlingFrame` then evaluates `elapsed = frameRecordTime − _flingStartTimestamp` (`:615`).

The two origins are separated by `Δ = t(UP processed on UI thread) − t(last motion sample)`, which is
**not modelled anywhere**. Every millisecond of `Δ` is `v₀` dip of injected error: at v₀ = 2650 dip/s,
**2.65 dip per millisecond**.

Simulated presented steps across the lift (touch 120 Hz, frames 120 Hz, v₀ = 2650 dip/s, nominal
step **22.083 dip**), varying only the phase of the UP within the frame interval:

| UP phase in frame | step before lift | **step across lift** | step after |
|---|---:|---:|---:|
| 5 %  | 22.083 | **20.897  (0.95x)** | 21.819 |
| 50 % | 22.083 | **33.102  (1.50x)** | 21.901 |
| 95 % | 22.083 | **23.187  (1.05x)** | 21.983 |

So the first inertial frame lands anywhere between **0.95x and 1.50x** the correct displacement,
selected by the arbitrary phase relationship between the input clock and the frame clock — i.e.
**randomly, on every single fling**, at the exact frame the user is watching most closely.

**Why this is asymmetric (the point of the brief):** during a drag the same dispatch delay exists,
but it is *common-mode* — it shifts the whole content by a constant lag behind the finger, and the
finger is the reference, so the eye reads it as "tracking". The moment the finger leaves, the finger
stops being a reference and the delay stops being common-mode: it converts, once, into a position
error, and a position error over one frame **is** a velocity error, which is exactly what the eye
detects as a hitch.

**Same family, persistent version (already hypothesised in the brief, and supported by this
analysis):** `OnFlingFrame` is handed the *record-start* wall clock (`Compositor.skia.cs:230`), not
the *present* time. The presented motion is `x(t_present − L(k))` with a varying record→present
latency `L`. A ±2 ms jitter in `L` at v₀ = 2650 dip/s modulates a 22 dip step by ±5.3 dip (**±24 %**)
— visible judder, for the whole fling. Drag is immune for the same reason as above: its position is
not a function of a clock.

---

## 7. Defect C — a fixed 140 ms sub-pixel creep tail, with no cutoff

Solving `v(t) = v₀(1−t/T)^(r−1) = V_thresh` with `T = r·0.35·(v₀/refV)^(1/(r−1))` — **v₀ cancels**:

```
T − t = r · Inflexion · (V_thresh / refV)^(1/(r−1))
```

| threshold | tail length | depends on v₀? |
|---|---:|---|
| < 1 dip/frame @120 Hz (120 dip/s) | **140.1 ms** | no |
| < 1 dip/frame @60 Hz (60 dip/s) | **84.1 ms** | no |
| < 0.5 dip/frame @120 Hz | **84.1 ms** | no |

Every Android fling in Uno, regardless of how hard you flick, ends with the same 140 ms of
entirely-sub-pixel motion. Measured against the alternatives this is actually the *best* of the three
(Android 212–253 ms, Flutter up to 332 ms), so it is not a fidelity problem — **but the wheel path
has an explicit cutoff and the fling does not**:

`ScrollDecaySimulation.cs:30-31`
```csharp
/// <summary>Below this the remaining motion is under a pixel per frame; snap and stop.</summary>
private const double MinVelocity = 8.0;
```
applied at `:80-83`. `ScrollFlingSimulation` has no equivalent, and `OnFlingFrame:624` has no
velocity term in its `running` test.

Consequence: if *anything* downstream quantizes the `AnchorPoint` translation to device pixels, those
140 ms become a stop-start stagger at the end of every fling. **UNVERIFIED** — I did not audit the
Skia transform/damage path for pixel snapping, and `AreClose` is not the culprit (its tolerance is
`(|a|+|b|+10)·2.22e-16`, `WrapPanel/NumericExtensions.cs:95-105`, so `Set` never drops a sub-pixel
update). This is a cheap thing to falsify — see §10.

---

## 8. The Apple branch is a separate, much worse story

`ScrollFlingSimulation.cs:77-79` — `Duration = ln(1/(|v|+1)) / ln(0.135)` is the time for velocity to
fall to **1 dip/s**, which is absurdly long:

| v₀ | Uno Apple duration | tail < 1 dip/frame @120 Hz | total distance |
|---:|---:|---:|---:|
| 800 | 3.34 s | 2389 ms (**71.5 %**) | 399 |
| 1500 | 3.65 s | 2394 ms (65.5 %) | 749 |
| 2650 | 3.94 s | 2395 ms (60.8 %) | 1323 |
| 5000 | 4.25 s | 2387 ms (56.1 %) | 2496 |

The exponential itself is right (real `UIScrollView.decelerationRate.normal` = 0.998/ms ⇒ 0.135/s;
total distance `v₀/2.0` matches iOS). The **termination threshold** is not: over half of every
iOS/macOS/MacCatalyst fling is sub-pixel creep, and `OnFlingFrame` will keep the render loop awake
for ~2.4 s of it. Real UIScrollView stops at ~0.5 pt/frame. **Fix: raise the threshold from 1 dip/s
to ~60–120 dip/s** (a one-line change to `:78`); it costs at most 0.5 dip of travel.

---

## 9. Not covered by the shape, but found while reading — snap-point ScrollViewers never reach this code

`ScrollContentPresenter.Managed.cs:958-996`. If `HorizontalSnapPointsType`/`VerticalSnapPointsType` is
`OptionalSingle`/`MandatorySingle`, or `ShouldSnapToTouchTextBox()`, inertia is handled by the
**old** path:

* it still uses `InertiaProcessor.DefaultDesiredDisplacementDeceleration / 2` on Android (`:951` —
  the "magic /2" is still in the tree, just unreachable from the fling branch);
* it ends with `Set(h, v, disableAnimation: false, …)` at `:995`, which routes into `Update`'s
  animation branch (`:531-583`) — a **1-second `PowerEasingFunction(Out, 10)` key-frame animation**.
  A power-10 ease-out starts at `10·D/T`; for a 1537 dip snap that is a **15 370 dip/s** opening
  velocity, i.e. a violent lurch, followed by a long crawl.

If the product owner's test surface has snap points configured (FlipView, snapping carousels, or a
`TextBox`-in-`ScrollViewer` case), **none of the fling work is active there at all** and the
observed roughness would be entirely this old path. Worth confirming before chasing anything else.

---

## 10. Conclusions, ranked, with the cheapest disproof for each

| # | Claim | Confidence | Cheapest proof |
|---|---|---|---|
| 1 | The curve interior is **not** the cause — it is monotone, C¹, and cleaner than Android's own spline (§2, §3) | **High** — computed from the real AOSP table | Already done; re-run the tables |
| 2 | The **launch seam** injects a 0.95x–1.5x step on frame 1 of every fling and is the only shape defect whose mechanism is asymmetric to drag (§6) | **Medium-high** | Log `Δ = _flingStartTimestamp − (time of the newest `_velocityTracker` sample)` in `StartFling`; if `Δ·v₀` is tens of dips it is confirmed. Fix: back-date the origin to that sample's time — the tracker already stores it (`ScrollVelocityTracker.cs:42`, fed from `TimestampInTicks` at `ScrollContentPresenter.Managed.cs:834-836`) — instead of re-reading the clock at `:593`. Note this removes the *dispatch* gap only; the hardware-sample→processing latency is still unmodelled because the tracker stamps samples with the processing clock, not the input event's own timestamp |
| 3 | The record-vs-present latency turns into a persistent ±24 % step modulation for inertia only (§6, brief's own hypothesis) | **Medium** | Feed `OnFlingFrame` a *predicted present* timestamp (record time + measured pipeline latency) and see if judder drops. On Android the Choreographer pacer already knows the vsync deadline |
| 4 | The **edge stop** is a true infinite-deceleration discontinuity (§5) | **High** (code-evident) | Fling into the end of a list; compare with a native Android list |
| 5 | The 140 ms sub-pixel tail matters **only if** something downstream snaps to device pixels (§7) | **Low / UNVERIFIED** | Add a `MinVelocity` cutoff mirroring `ScrollDecaySimulation.cs:31` and see if the last 140 ms of every fling stops "stepping" |
| 6 | Duration is 82.54 % of Android's — a fidelity/feel difference, not a smoothness one (§1) | **High** | Swap `GetPosition` for Flutter's cubic (or the real spline table) and A/B it; distance is unchanged |
| 7 | The Apple branch runs 3.3–4.3 s with >55 % sub-pixel creep (§8) | **High** | One-line threshold change at `ScrollFlingSimulation.cs:78` |
| 8 | Snap-point ScrollViewers still use the old parabola + 1 s power-10 ease (§9) | **High** (code-evident) | Check whether the repro surface sets any `SnapPointsType` |

**If only one thing is changed next: #2.** It is a two-line change, it is the only shape defect that
is asymmetric by construction, and it fires on every fling at the moment of maximum user attention.
