# 12 — Review of the prior scroll-smoothness attempt (`dev/mazi/scroll-inertial-smoothness`)

**Range reviewed:** `0caeb14d69..921be614f3` (16 commits, +1330/−120 in `src`)
**Method:** read-only `git log` / `git diff` / `git show <sha>:<path>`; cross-checked against
`D:/Work/microsoft-ui-xaml2` (WinUI legacy core), `D:/Work/Avalonia`, `D:/Work/flutter`, and the
current Uno Skia render loop.
All line numbers below refer to the **final state at `921be614f3`** unless a base file is named
explicitly (`0caeb14d69:`).

---

## 1. Commit-by-commit narrative

| # | SHA | Subject | What it actually does |
|---|-----|---------|------------------------|
| 1 | `f0139fc722` | feat: Intermediate scrolling updates mode | Rewrites `ScrollViewer`'s deferred-update region under `#if __SKIA__`. Replaces `_hasPendingUpdate` + `RequestUpdate()` (dispatcher-posted `Update(isIntermediate:true)`) with a WinUI-shaped machine: `m_iViewChangedDelay` counter + `DelayViewChanged`/`FlushViewChanged`, `m_isInIntermediateViewChangedMode`, `RaiseViewChanged(...)`, and offset DPs applied at arrange time. |
| 2 | `5d804b6954` | test: Validate scrolling behavior | +196 lines of runtime tests for the new ViewChanged semantics. |
| 3 | `39111ab206` | chore: Align the scrolling notifications with WinUI | Adds `Scroller?.OnPresenterArranged()` in `ScrollContentPresenter.ArrangeOverride` (`ScrollContentPresenter.cs:263`) and `ScrollViewer.OnPresenterArranged()` (`ScrollViewer.cs:1526-1540`), completing the "offsets land during arrange" model. |
| 4 | `71d633da83` | fix: Adjust for tests | Adds the `UpdatesMode == Synchronous` short-circuit (`ScrollViewer.cs:1277-1290`) so tests can read offsets synchronously. |
| 5 | `048f8e29cf` | perf: Scrolling | **Physics model swap.** `InertiaProcessor` goes from constant-deceleration quadratic (`v0·t − d·t²`) to exponential decay (`v0·(1−e^(−k·t))/k`). Introduces `VelocityThreshold = 0.01`, changes defaults `.001/.0001/.001 → .003/.003/.003`, `DispatcherInertiaProcessorTimer.DefaultFramePerSeconds 30 → 60`, and switches `CompositionInertiaProcessorTimer` from `Stopwatch` to `RenderingEventArgs.RenderingTime`. |
| 6 | `8154198090` | feat: Scroll smoothness | Adds `VelocityTracker.cs` (Flutter/Avalonia least-squares port) and wires it into `Manipulation` behind `#if IS_UNO_UI_PROJECT`. Adds per-frame batching of touch updates in the SCP (`ScheduleDeferredTouchUpdate` / `CompositionTarget.Rendering`). |
| 7 | `965a165b81` | feat: Inertial smoothness | **Approach A (composition-driven inertia).** Pre-bakes the whole exponential decay curve as a `Vector2KeyFrameAnimation` on `Visual.AnchorPoint` (32 ms keyframes), calls `recognizer.CompleteGesture()` and stops ticking the inertia processor. `Updated()` called every 2nd frame. |
| 8 | `bd53f1e29e` | feat: Inertial smoothness II | Same approach, keyframe interval 32 ms → 16 ms, and `OnFrame` stops calling `Updated()` at all: only `OnPresenterScrolled` + `ScrollOffsets`, "CRITICAL: Do NOT call InvalidateViewport()… causes visible tearing". |
| 9 | `e85410fe0c` | feat: Inertial smoothness III | **Approach A fully reverted** (−187 lines). Back to processor-ticked inertia. Comment: "We DON'T defer during inertia because the inertia processor fires exactly once per frame during CompositionTarget.Rendering (BEFORE layout), and we need layout to process the new scroll position in the SAME frame to avoid tearing." |
| 10 | `26589d96bb` | feat: Inertial smoothness IV | **Approach B (throttling + snapping).** Adds `_inertiaFrameCount` / `InertiaViewportUpdateInterval = 3` (skip EVP on 2 of every 3 inertia frames) and pixel-snaps the visual: `visual.AnchorPoint = new Vector2(MathF.Round(target.X), MathF.Round(target.Y))` (`SCP.Managed:385`). |
| 11 | `c440c71f14` | feat: Velocity-based mouse wheel inertia | New nested `WheelInertia` class (`SCP.Managed:854-1009`). Each notch adds a velocity impulse; a `CompositionTarget.Rendering` loop integrates `v·e^(−k·t)` closed-form. Bounds-checks against the *projected* settle point so nested SVs chain. |
| 12 | `b3c37533fa` | test: Wait for wheel inertia to settle | Adds `Task.Delay(1500)` × 2 because `WaitForIdle(waitForCompositionAnimations:true)` no longer sees the wheel coast. |
| 13 | `6d6f786a6f` | feat: Tune wheel inertia… | Magic-constant tuning: `DecayPerMs 0.003 → 0.010`, new `VelocityBoost = 1.15`. |
| 14 | `0a09ca3725` | feat: Preserve sub-pixel pointer coordinates | Android drops `(int)` cast; Win32 routes touch/pen through `ptHimetricLocation*`; X11 preserves the fraction across `XTranslateCoordinates`. |
| 15 | `905701a21d` | feat: Drop manipulation translation deltas to zero | `DeltaTouch/DeltaPen/DeltaMouse.TranslateX/Y` 1–2 px → **0** (`GestureRecognizer.Manipulation.cs:39-41`). |
| 16 | `921be614f3` | fix: Address scroll inertia review feedback | Stops wheel coast on new manipulation, unsubscribes `Rendering` on unload, clamps the projected wheel target, requires a non-null LS estimate, passes old offsets to the automation peer, adds Flutter/Avalonia attribution, polls instead of sleeping in the *new* wheel test only. |

### Overall shape of the final state

Five independent changes were made at once:

1. **Event/DP plumbing rewrite** (Skia-only) — offsets now land in `ArrangeOverride`.
2. **Inertia physics model swap** — quadratic → exponential, shared by all platforms.
3. **Velocity estimation swap** — boundary-delta → least-squares polynomial fit.
4. **Wheel input model swap** — 1 s eased keyframe animation → per-frame impulse/decay integrator.
5. **Input precision fixes** — sub-pixel coordinates on Win32/X11/Android + zero delta thresholds.

Plus two rounds of **symptom suppression** (EVP throttling `%3`, integer pixel snapping) added
after Approach A (composition-baked inertia) failed with visible tearing.

---

## 2. What is CORRECT and worth keeping

### 2.1 Exponential decay replacing the quadratic model — KEEP (the model, not the constants)

`GestureRecognizer.Manipulation.InertiaProcessor.cs:282-292`:

```csharp
internal static double GetValue(double v0, double k, double t)
{
    if (k <= 0) { return v0 * t; }
    var decay = Math.Exp(-k * t);
    return v0 * (1 - decay) / k;
}
```

This is the right family. The old base model was `v0*t − d*t²` (`0caeb14d69:` same file), i.e.
*constant deceleration*, which is not what any modern scroller uses. Exponential decay matches
Flutter's `FrictionSimulation` and Android's `OverScroller`. The sign-handling wart in the old
`GetValue` (`v0 >= 0 ? v0*t − d*t² : −(−v0*t − d*t²)`) is gone; the new form is linear in `v0` and
sign-correct by construction.

The closed-form integration is also correct and drift-free — `Process(TimeSpan elapsed)`
(`InertiaProcessor.cs:222-239`) evaluates `GetValue(v0, k, tAbsolute)` from `t0`, never accumulating
per-frame error. Same for `WheelInertia.OnFrame` (`SCP.Managed:961-971`), which uses the exact
`pos += v·(1−e^(−k·dt))/k; v *= e^(−k·dt)` step. Both are numerically sound.

`GetDecayRateFromDesiredDisplacement` (`InertiaProcessor.cs:325-334`) is now `k = |v0| / displacement`,
which is the exact inverse of `∫₀^∞ v0·e^(−kt) dt = v0/k`. The old formula
(`(v0*durationMs − displacement) / durationMs²`) was dimensionally incoherent. Keep.

### 2.2 Sub-pixel pointer coordinates — KEEP (Android and X11 unconditionally)

**Android** (`AndroidCorePointerInputSource.cs:229`) is a clean, unambiguous bug fix:

```csharp
// before: new Point((int)x - correction[0], (int)y - correction[1])
var position = new Point(x - correction[0], y - correction[1]).PhysicalToLogicalPixels();
```

`MotionEvent.GetX/GetY` return `float`; the `(int)` cast was throwing away real precision on every
touch sample. This alone quantised every Android drag to whole physical pixels.

**X11** (`X11PointerInputSource.XInput.cs:308-317`) is also correct: `XIDeviceEvent.event_x` is
`double` (`x11Bindings_XInput.cs:257`), `XTranslateCoordinates` takes `int`, and the window→window
transform is a pure integer translation — so flooring, translating, and re-adding the fraction is
exact, not an approximation. Keep.

### 2.3 The idea of an impulse/velocity wheel model — KEEP the concept

Replacing "restart a fixed 1 s `PowerEasing(Out, 10)` keyframe animation on every notch"
(`0caeb14d69:SCP.Managed` lines ~347-366, still present at `SCP.Managed:429-446` as the
`IsScrollInertiaEnabled == false` fallback) with an accumulating velocity impulse is the right
direction. The old model's failure mode is well known: a second notch mid-animation restarts the
animation from a new "current" position with a fresh 1 s ease-out, producing a snap-then-crawl.

The **projected-settle-point bounds check** in `TryStartWheelInertia` (`SCP.Managed:462-498`) is a
genuinely good idea and worth preserving in any rewrite: chaining to a parent `ScrollViewer` must be
decided against where the trajectory *will* end, not where the offset *is right now*, otherwise
rapid wheel input on an inner SV never bubbles.

```csharp
var prevProjH = _wheelInertia is { IsRunning: true } projH ? Math.Clamp(projH.ProjectedFinalH, 0, maxH) : HorizontalOffset;
...
var canAbsorb = (dxPx != 0 && newProjH != prevProjH) || (dyPx != 0 && newProjV != prevProjV);
```

The clamp on the baseline (added in `921be614f3`) is necessary and correct — the raw projection
`offset + v/k` is unbounded.

### 2.4 `DelayViewChanged` / `FlushViewChanged` — KEEP, it is a real WinUI mechanism

`ScrollViewer.cs:1402-1422`. This is a faithful port of WinUI's nestable counter. Grounded:
`ScrollViewer_Partial.cpp:13479-13480` inside `HandleManipulationDelta`:

```cpp
// Batch up any potential ViewChanging/ViewChanged events during HandleManipulationDelta into a single notification
DelayViewChanging();
DelayViewChanged();
```

and the flush at `ScrollViewer_Partial.cpp:4921` ("InvalidateScrollInfoImpl calling FlushViewChanged").
Correct primitive, under-applied (see §3.3).

### 2.5 The `VelocityTracker` **algorithm** — KEEP (the solver is faithful)

The QR/Gram-Schmidt least-squares solver in `VelocityTracker.cs:148-241` is a correct, allocation-free
transcription of Avalonia's `LeastSquaresSolver.Solve`
(`D:/Work/Avalonia/src/Avalonia.Base/Input/GestureRecognizers/VelocityTracker.cs:234-348`), which is
itself a transcription of Flutter's. I checked the indexing arithmetic element by element:

| Avalonia | Uno port | Verdict |
|---|---|---|
| `a[i,h]` with `_columns = m` | `aData[i*m + h]` (`:166`) | ✅ |
| `q.GetRow(j)` → `Slice(j*m, m)` | `qData.Slice(j*m, m)` (`:185`) | ✅ |
| `r[j,i]` with `_columns = n` | `rData[j*n + i]` (`:208`) | ✅ |
| back-sub `-= r[i,j]*c[j]`, `/= r[i,i]` | `rData[i*n+j]`, `rData[i*n+i]` (`:225,227`) | ✅ |
| `norm < 1e-10 → null` | `norm < 1e-10 → null` (`:193-197`) | ✅ |

The circular-buffer walk (`:88-113`) matches Flutter's `velocity_tracker.dart:207-231` exactly,
including the `age > horizon` / `delta > assumeStopped` break conditions and the `time[i] = −age`
convention that makes `coefficients[1]` the velocity *at the newest sample* rather than at the mean.
Uno's `if (n > m) return null` (`:153`) is even slightly safer than Flutter's `degree > x.length`.

Bounds are safe: the do-while writes `x[sampleCount]` before incrementing, so the maximum index is
`HistorySize-1 = 19`.

### 2.6 `ClearOffsetIntents()` on wheel and on touch press — KEEP

`SCP.Managed:539` and `ScrollContentPresenter.cs:284`. This is the pre-existing "no fighting the
user" guard and the attempt preserved it while adding the new wheel path. Important: it is what
prevents `RecomputeOffsetsFromIntent` (`ScrollViewer.cs:1832-1869`) from snapping the offset back
mid-coast now that the wheel path no longer registers a composition animation (see §3.9).

### 2.7 Unsubscribing `CompositionTarget.Rendering` from `FlushPendingTouchUpdate` — KEEP

`SCP.Managed:517-528`. Correct lifetime fix: unsubscribing inside the flush (rather than only in the
`Rendering` callback) means `OnUnloaded` detaches from a **static** event instead of leaving the
presenter rooted forever. `CompositionTarget.Rendering` is `private static event` at
`CompositionTarget.Rendering.skia.cs:82`, so this is a real leak class.

---

## 3. What is WRONG, risky, or a band-aid

### 3.1 🔴 The "VSync-aligned RenderingTime" claim is false — the change is a no-op that adds jitter

`InertiaProcessor.cs:399-403` states:

```
// Prefer the VSync-aligned RenderingTime when available (Skia hosts provide this
// from the platform's VSync signal, e.g. Android Choreographer). Using VSync time
// ensures the physics position matches the actual display moment, eliminating
// micro-stutters caused by wall-clock / display-clock divergence.
```

At `921be614f3`, `RenderingEventArgs.RenderingTime` is produced here
(`921be614f3:src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:427-440`):

```csharp
internal static void InvokeRendering()
{
    if (NativeDispatcher.Main.HasThreadAccess)
        _rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
    else
        NativeDispatcher.Main.Enqueue(() =>
            _rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start))),
            NativeDispatcherPriority.High);
}
```

`_start` is `Stopwatch.GetTimestamp()` captured in a static field initializer (`:25`). So
`RenderingTime` is **a wall-clock `Stopwatch` reading taken at the moment the managed event is
raised** — not a VSync timestamp, not a present timestamp, and (in the off-thread branch, which is
the one taken since `InvokeRendering()` is called from `Draw`, `:298`) it is sampled *after* an
arbitrary dispatcher queue delay. Substituting it for the previous `Stopwatch.StartNew()`:

* provides **zero** VSync alignment;
* re-samples the clock inside a dispatcher continuation, so it carries UI-thread scheduling jitter
  — the very noise the change claims to remove;
* costs one dropped tick (`return` on the first frame, `:406-410`).

The one small real benefit is that `t0` becomes the first callback rather than `Start()`, removing a
fixed sub-frame phase offset. That is worth ~1 line, not the comment.

**Head is no better**: `RaiseRendering()` (current worktree,
`CompositionTarget.Rendering.skia.cs:458-487`) still builds `new RenderingEventArgs(Stopwatch.GetElapsedTime(_start), …)`
at raise time, and is scheduled from `OnFramePictureRecorded` — i.e. *after* the picture is recorded.
Any physics computed in a `Rendering` handler therefore lands in frame **N+1**. This is a structural
one-frame latency that the whole attempt is blind to.

### 3.2 🔴 Per-intermediate-tick `InvalidateArrange()` — a smoothness regression sold as WinUI parity

`ScrollViewer.cs:1292-1298`:

```csharp
// WinUI-aligned behavior: store pending offsets and defer DP updates to ArrangeOverride.
_hasPendingScrollUpdate = true;
_presenter?.InvalidateArrange();
```

This runs on **every** `OnPresenterScrolled`, including every intermediate touch-drag and inertia
tick. The base code did the opposite, deliberately, with a WinUI citation
(`0caeb14d69:ScrollViewer.cs`, `Update(bool)`):

```
// Intermediate ticks (DManip-driven inertia / drag) are skipped to match WinUI,
// which lets the compositor drive offsets during manipulation without re-running
// layout per frame.
if (!isIntermediate && (oldHorizontalOffset != HorizontalOffset || ...)) InvalidateArrange();
```

The "WinUI does this" justification is *technically true but architecturally inapplicable*. In WinUI:

* `ScrollContentPresenter::SetOffsetsWithExtents` does call `InvalidateArrange()`
  (`ScrollContentPresenter_Partial.cpp:1030-1033`), and DManip deltas reach it via
  `ScrollByPixelDelta → ScrollToHorizontalOffsetInternal` (`ScrollViewer_Partial.cpp:6221`);
* **but the pixels are moved by DirectManipulation on the compositor thread**. The arrange pass only
  re-seats layout/anchors; it is *not* on the visual critical path, and it can lag a frame with no
  visible effect.

In Uno's managed presenter the pixels move by writing `visual.AnchorPoint` **on the UI thread**, and
the frame is not recorded until layout completes (`Render()` →
`SkiaRenderHelper.RecordPictureAndReturnPath`). Forcing an arrange every tick puts a full
measure/arrange of the SCP subtree directly in front of every frame. This is almost certainly the
change that *created* the tearing/ghosting that commits 9–10 then fought with `%3` throttling and
pixel snapping.

**Verdict: this is the most likely net-negative change in the whole branch.**

### 3.3 🟠 The arrange-deferred offset model breaks synchronous `ScrollViewer` semantics

Under the new Skia path, `ScrollViewer.HorizontalOffset`/`VerticalOffset` are *only* written in
`OnPresenterArranged()` (`ScrollViewer.cs:1526-1540`). Consequences observable in the branch's own
tests:

* `ChangeView(..., disableAnimation: true)` returns before the DP reflects the new value; the tests
  must `await WindowHelper.WaitFor(() => sut.VerticalOffset == 150d)`
  (`Given_ScrollViewer.cs`, `When_ChangeView_DisableAnimation_Then_OffsetUpdatedAfterArrange`).
* If the presenter is never arranged (collapsed SV, detached subtree, an arrange short-circuit), the
  offsets **never** update — `_hasPendingScrollUpdate` stays latched.
* `ViewChanged` is now raised *from inside* `ScrollContentPresenter.ArrangeOverride`
  (`ScrollContentPresenter.cs:263` → `OnPresenterArranged` → `RaiseViewChanged` → public event → app
  code → possible `ChangeView`). Re-entrant layout is a layout-cycle hazard. WinUI mitigates this
  with `DelayViewChanged/DelayViewChanging` around the whole `HandleManipulationDelta`; the port only
  wraps `ChangeView` (`ScrollViewer.cs:1758-1775`) and never `DelayViewChanging`.
* `Update(isIntermediate)` and `RequestUpdate()` survive only in the `#else` branch
  (`ScrollViewer.cs:1546-1600`), so Skia and non-Skia now have two entirely different notification
  machines to maintain.

### 3.4 🔴 `MathF.Round` pixel snapping directly contradicts commits 14–15

`SCP.Managed:381-385`:

```csharp
// Pixel-snap to integer values to avoid subpixel text rendering artifacts
// (shimmer/ghosting from fractional offsets during inertia decay).
visual.AnchorPoint = new Vector2(MathF.Round(target.X), MathF.Round(target.Y));
```

Two commits later the branch goes to considerable lengths to preserve HIMETRIC / `MotionEvent` /
XInput2 sub-pixel precision (`0a09ca3725`) and drops the manipulation delta thresholds to 0 so
"every sub-pixel position delta flows through to ScrollViewer" (`905701a21d`,
`GestureRecognizer.Manipulation.cs:33-41`). All of that precision is then **discarded at the last
hop**, in *logical* pixels, not device pixels — so at 150 % scaling the snap is 1.5 device pixels.

Concrete failure: a slow 30 px/s drag is 0.5 px/frame at 60 Hz. With rounding, the content moves 1 px
every second frame — i.e. exactly the stair-stepping the branch set out to eliminate. It also
desynchronises `ScrollViewer.VerticalOffset` (fractional) from the rendered position (integer) by up
to 0.5 px.

The stated cause ("subpixel text rendering artifacts") is a **glyph rasterisation / cache problem**,
not a scroll-offset problem. Snapping the scroll offset is a band-aid over the text pipeline.

### 3.5 🔴 `InertiaViewportUpdateInterval = 3` — throttling the symptom

`SCP.Managed:52-59` and `:393-419`. The comment is admirably honest about what it is:

```
// PropagateEffectiveViewportChange() can trigger heavy layout (ItemsRepeater
// measure, item recycling). If that layout doesn't fully resolve in one UpdateLayout()
// pass, CanRecordPicture returns false and the frame is SKIPPED, causing visible
// ghosting. By throttling EVP to every Nth inertia frame, most frames are lightweight
// ... Items still materialize at ~20fps which is imperceptible.
```

Problems:

1. It names the real bottleneck (**EVP → synchronous layout → frame skipped because
   `CanRecordPicture` is false**) and then does not fix it.
2. "Items materialize at ~20 fps which is imperceptible" is false for fast flings — that is 3 frames
   of blank/recycled cells per realization step.
3. `3` is unmotivated. There is no model relating it to layout cost, item count, or frame budget.
4. It only applies to inertia (`options.IsIntermediate && (IsWheelInertia || (IsTouch && _touchInertia is not null))`),
   so an active finger drag still pays full EVP every tick — a drag is *more* latency-sensitive than
   a fling.
5. The `else` branch duplicates `Updated()`'s body inline (`:412-417`) — the `_lastScrolledEvent`
   dedupe logic now exists in two places and can drift.

### 3.6 🔴 Five unrelated magic decay constants, no unifying model

| Constant | Value | Site | Justification given |
|---|---|---|---|
| `DefaultDesiredDisplacementDeceleration` | `0.003` | `InertiaProcessor.cs:72` | "WinUI InteractionTracker 0.95/frame" — arithmetically checks out (`−ln 0.95 / 16.67 = 0.00308`) |
| iOS branch | derived | `SCP.Managed:711-730` | "PastryKit 0.95 friction" |
| Android branch | `0.0025` | `SCP.Managed:735` | "≈0.96 decay per frame" — checks out (`−ln 0.96 / 16.67 = 0.00245`) |
| `WheelInertia.DecayPerMs` | `0.010` | `SCP.Managed:863` | "empirically DManip settles in ~300-400 ms" |
| `WheelInertia.VelocityBoost` | `1.15` | `SCP.Managed:869` | "DManip seems to overshoot… by ~15-20 %" |

Specific defects:

**(a) The entire iOS block is dead arithmetic.** `SCP.Managed:711-730`:

```csharp
var frames    = Math.Log(PKScrollViewMinimumVelocity / v0, PKScrollViewDecelerationFrictionFactor);
var duration  = frames * PKScrollViewDesiredAnimationFrameRate;
inertia.DesiredDisplacementDeceleration = InertiaProcessor.GetDecelerationFromDesiredDuration(v0, duration);
```

with `GetDecelerationFromDesiredDuration(v0, d) = ln(|v0|/0.01)/d` (`InertiaProcessor.cs:310-319`)
and `PKScrollViewMinimumVelocity == VelocityThreshold == 0.01`. Substituting:

```
k = ln(v0/0.01) / ( ln(0.01/v0)/ln(0.95) · 16.667 )
  = ln(v0/0.01) · ln(0.95) / ( −ln(v0/0.01) · 16.667 )
  = −ln(0.95)/16.667  =  0.003077
```

`v0` cancels exactly. Twenty lines, three named constants and a `Math.Log` per fling all evaluate to
a constant that is already `DefaultDesiredDisplacementDeceleration`. The iOS branch is functionally
identical to the `else` branch.

**(b) The wheel-tuning comment's numbers are wrong.** `SCP.Managed:861` claims
"k = 0.010/ms ⇒ 90-px notch settles in ~330 ms". With `VelocityThreshold = 0.01`:
`v0 = 90 · 0.010 · 1.15 = 1.035 px/ms`, so `t = ln(1.035/0.01)/0.010 = 464 ms`, and total travel is
`v0/k = 103.5 px`, not 90. The documented figure is off by ~40 %. This is what unvalidated tuning
constants look like.

**(c) The touch model silently got ~3.7× shorter.** For a `v0 = 5 px/ms` fling: old quadratic model
(`d = 0.001`) travelled `v0·t − d·t²` to `t = v0/2d = 2500 ms` → **6250 px**. New model travels
`v0/k = 5/0.003` → **1667 px**. That is a large, user-visible behaviour change with no WinUI
measurement backing it.

### 3.7 🔴 `VelocityTracker` wiring bugs (the solver is fine; the plumbing is not) — see §5

Summarised here, detailed in §5: one tracker shared across **all** pointers; the pointer-**up**
sample is fed in (Flutter deliberately does not); `Angular`/`Expansion` velocities hard-zeroed;
no min/max fling clamp; estimate computed on *every* pointer move instead of once at fling time; the
pre-existing iOS release-sample guard bypassed.

### 3.8 🟠 Delta thresholds → 0 is a global change with global cost

`GestureRecognizer.Manipulation.cs:39-41` sets `TranslateX/Y = 0` for touch, pen **and mouse**. The
gate is at `Manipulation.cs:450`:

```csharp
case ManipulationStatus.Started when changeSet.Delta.IsSignificant(_deltaThresholds):
```

With zero thresholds, every non-zero pointer delta raises a full `ManipulationDelta`. On a 1000 Hz
pen digitizer that is ~16 manipulation events per frame, each of which (during a drag) does
`Set → Update → AnchorPoint write` plus a `StageChanges()` that now runs **two QR decompositions**
(§5.5). The per-frame batching (`ScheduleDeferredTouchUpdate`, `SCP.Managed:500-512`) only defers the
`Updated()` half; the recogniser-side cost is unbatched.

It is also a *global gesture-recogniser* change: every `ManipulationDelta` consumer in the framework
and in user apps now receives an order of magnitude more events. The WinUI justification
("`IInteractionContext` fires `INTERACTION_ID_MANIPULATION` on every meaningful input change with no
displacement gate") is asserted, not cited — I could not verify it in
`D:/Work/microsoft-ui-xaml2`. **UNVERIFIED.**

The right shape is almost certainly: keep a small threshold for *event raising*, but coalesce
positions into the velocity tracker without a threshold.

### 3.9 🟠 `WheelInertia` internal state can desynchronise from the real offset

`AddImpulse` (`SCP.Managed:906-927`) only re-seeds `_hOffset/_vOffset` from the owner when
`!_running`. While coasting, the loop integrates its own private position and unconditionally pushes
it via `Set(...)` every frame (`:985-988`). Anything that changes the offset without going through
`Set` (arrange-time clamping, extent change, an anchoring correction, `OnPresenterArranged`) is
overwritten on the next tick — a visible snap-back. `Set` does call `_wheelInertia?.Stop()` for
non-wheel callers (`:296-301`), which covers most paths, but not writes that bypass `Set`.

Related: because the wheel coast is no longer a `CompositionAnimation`,
`IsScrollAnimationInProgress` (`SCP.Managed:130-145`) is `false` for its whole duration, so
`RecomputeOffsetsFromIntent`'s guard (`ScrollViewer.cs:1840`) no longer protects it. Today this is
saved only by `ClearOffsetIntents()` on wheel (`ScrollContentPresenter.cs:284`) — a fragile
coupling, and exactly the class of bug the "no fighting the user" rule exists to prevent.

Same reason: `UITestHelper.WaitForIdle(waitForCompositionAnimations: true)` no longer waits for wheel
scrolling, which is why four `Task.Delay(1500)` calls had to be added
(`Given_ScrollViewer.cs:739, 754, 1576, 1584`) — **6 seconds of unconditional sleep** in the suite,
and `921be614f3` only converted the *new* test to polling.

### 3.10 🟡 Pre-existing hazard the attempt noticed but did not fix

`SCP.Managed:359-369` (unchanged from base):

```csharp
if (visual.TryGetAnimationController(nameof(Visual.AnchorPoint)) is { } controller
    && Vector2.DistanceSquared(visual.AnchorPoint, target) < 4
    && controller.Remaining < TimeSpan.FromMilliseconds(50))
{
    return;   // silently drops the scroll update
}
```

The branch itself documents (`SCP.Managed:137-141`) that "a KeyFrameAnimation that completed
naturally stays in the owning CompositionObject's animation dictionary (only `StopAnimation` removes
it)". Therefore, after any animated scroll, `TryGetAnimationController` keeps returning non-null and
`Remaining` is `<= 0`, so **any** subsequent update whose target is within 2 px of the current
`AnchorPoint` is dropped. During slow drags and the tail of every inertia that is *every* update. The
attempt wrote the diagnosis into a comment and then left the bug in place.

### 3.11 🟡 Smaller issues

* `Updated()` still has the `DispatcherQueue.TryEnqueue` + `Interlocked` request-id path
  (`SCP.Managed:316-334`) that is dead on all scroll paths — pure overhead and complexity.
* `WheelInertia.OnFrame` reads `Scroller?.ScrollableWidth/Height` (DP reads) every frame
  (`:965-966`).
* `_touchUpdateHandler`/`_frameHandler` subscribe/unsubscribe from a static event on every touch
  update burst; while any handler is attached, `Render()` unconditionally calls `RequestNewFrame()`
  (`CompositionTarget.Rendering.skia.cs:153-156`), so the app renders continuously.
* `VelocityTracker.Reset()` (`:60-64`) is dead code — never called (a fresh `Manipulation` is created
  per gesture at `GestureRecognizer.Manipulation.cs:127`).
* `#if IS_UNO_UI_PROJECT` gating of the velocity tracker means it is present only in
  `Uno.UI.Reference.csproj` / `Uno.UI.Skia.csproj` (the only two projects defining it) — the shared
  `Uno.UWP` build of `GestureRecognizer.Manipulation.cs` silently uses the old estimator. Two
  divergent velocity behaviours from one file.
* `DispatcherInertiaProcessorTimer.DefaultFramePerSeconds 30 → 60` (`InertiaProcessor.cs:367`) is
  directionally right but a `DispatcherQueueTimer` at 16.67 ms will never be frame-aligned; on the
  non-Skia platforms that use it this trades one jitter source for a faster one.

---

## 4. Does it address the real bottleneck?

**Mostly no — and in one place it makes the bottleneck worse.**

The branch's own comments identify the real bottleneck three separate times:

1. `bd53f1e29e`: *"CRITICAL: Do NOT call InvalidateViewport() / PropagateEffectiveViewportChange()
   here. That triggers layout invalidation (item recycling, re-arrangement) which causes visible
   tearing."*
2. `e85410fe0c`: *"we need layout to process the new scroll position in the SAME frame to avoid
   tearing"*
3. `26589d96bb` / `SCP.Managed:52-57`: *"If that layout doesn't fully resolve in one `UpdateLayout()`
   pass, `CanRecordPicture` returns false and the frame is SKIPPED, causing visible ghosting."*

That is a precise, correct diagnosis: **on Uno Skia, scroll offset changes and the layout work they
trigger are on the same thread and inside the same frame as picture recording; if layout does not
converge, the frame is dropped.** Nothing in the branch changes that structure. Instead:

* Approach A (composition-baked keyframes, commits 7–8) tried to decouple the *visual* from managed
  per-frame work — the right instinct — but by pre-baking a fixed curve it became uninterruptible and
  desynchronised from layout, producing the tearing that caused the revert. The failure was in the
  execution (pre-baked keyframes + skipped viewport propagation), not in the goal.
* Approach B (commits 10, 13) accepts the coupling and rations it: skip EVP on ⅔ of frames, and hide
  the residual sub-pixel motion with `MathF.Round`. Both are symptom suppression.

Meanwhile the branch *adds* a new per-frame layout cost — `InvalidateArrange()` on every intermediate
tick (§3.2) — which is very likely why the tearing showed up in the first place.

The two changes that do attack root causes are the **input-precision fixes** (§2.2, real quantisation
removed at the source) and the **physics model swap** (§2.1, replaces a wrong model rather than
tuning a wrong model). Everything else is either plumbing churn or compensation.

**Also untouched and unnoticed:**

* **Win32 pointer timestamps are millisecond-resolution `GetMessageTime()`**
  (`Win32WindowWrapper.Pointers.cs:148, 251`). `GetMessageTime` is `GetTickCount`-based (~15.6 ms
  granularity by default). Every pointer sample within a frame can carry the *same* timestamp. Feeding
  that into a least-squares fit over `time[]` yields near-duplicate abscissae → `norm < 1e-10` →
  `LeastSquaresSolve` returns `null` → silent fallback to the old estimator. Fixing sub-pixel
  *position* while leaving the *time* axis quantised to a frame is fixing the smaller half of the
  problem. (`POINTER_INFO` exposes `dwTime` and a QPC `PerformanceCount`; neither is used anywhere in
  the repo — verified by grep. Whether CsWin32 surfaces them here: **UNVERIFIED**.)
* **Android historical samples are dropped.** `AndroidCorePointerInputSource.cs:226-227` reads only
  `GetX(pointerIndex)`/`GetY(pointerIndex)`; `MotionEvent.HistorySize`/`GetHistoricalX/Y` are never
  consulted. Android batches multiple digitizer samples into one `MotionEvent`; ignoring them throws
  away most of the input resolution on exactly the platform where fling quality matters most.
* **The one-frame `Rendering`-after-record latency** (§3.1) is never acknowledged.

---

## 5. Is the `VelocityTracker` implementation sound?

**Solver: yes. Integration: no — five defects, two of them likely to make flings *worse* than the
estimator it replaced.**

### 5.1 The math (see §2.5) — sound

2nd-degree weighted least squares via Gram-Schmidt QR, sample window and break conditions identical
to Flutter/Avalonia:

| Parameter | Flutter (`velocity_tracker.dart:143-146`) | Avalonia (`VelocityTracker.cs:63-66`) | Uno (`VelocityTracker.cs:46-49`) |
|---|---|---|---|
| history size | 20 | 20 | 20 ✅ |
| horizon | 100 ms | 100 ms | 100 000 µs ✅ |
| assume-stopped | 40 ms | 40 ms | 40 000 µs ✅ |
| min samples | 3 | 3 | 3 ✅ |

Unit handling is correct: `time[sampleCount] = -(double)age / 1000.0` (`:108`) converts µs→ms, and
`coefficients[1]` is therefore px/ms, matching `ManipulationVelocities.Linear`'s convention (which
`ComputeVelocities` also produces, `Manipulation.cs:679-687`). Unsigned-underflow is guarded on
`delta` (`:97`) — a correct adaptation of Flutter's `.abs()`.

The `sampleCount >= 2` linear fallback (`:130-139`) has no Flutter equivalent (Flutter returns a
zero-velocity estimate) but is harmless and arguably better; the oldest-sample recovery
`_samples[(index + 1) % HistorySize]` is index-correct in both loop-exit paths.

### 5.2 🔴 Defect 1 — one tracker shared by all pointers

`GestureRecognizer.Manipulation.cs:85` declares `private readonly VelocityTracker _velocityTracker = new();`
per `Manipulation`, and `Update` feeds it inside a loop over **all** points of the matching device
type (`:259-268`):

```csharp
foreach (var point in updated)
    if (_deviceType == (PointerDeviceType)point.PointerDevice.PointerDeviceType)
        if (_currents.TryUpdate(point)) { hasUpdate = true; _velocityTracker.AddPosition(point.Timestamp, point.Position.X, point.Position.Y); }
```

With two fingers, samples from finger 1 and finger 2 interleave in one 20-slot circular buffer. The
polynomial is then fitted to a zig-zag between two unrelated screen positions. Flutter keeps
`Map<int, VelocityTracker> _velocityTrackers` keyed by pointer id
(`monodrag.dart:380, 413, 666`). This is a straightforward correctness bug for any two-finger pan.

Compounding it: the tracker samples **raw pointer positions**, whereas the manipulation's own
velocity is derived from `ComputeDelta(..., parentCommit.SumOfDelta)` on the *centroid* and
accounts for deltas already consumed by a parent. The two are not the same quantity.

### 5.3 🔴 Defect 2 — the pointer-up sample is fed in

`Manipulation.cs:292-298` (in `Remove`):

```csharp
_velocityTracker.AddPosition(removed.Timestamp, removed.Position.X, removed.Position.Y);
```

Flutter deliberately does **not** do this. `monodrag.dart:656-667` gates on
`PointerDownEvent || PointerMoveEvent || PointerPanZoomStartEvent || PointerPanZoomUpdateEvent` —
`PointerUpEvent` is excluded, and `_checkEnd` (`:875-881`) reads the tracker as-is.

The reason is exactly the case Uno's own pre-existing code already handles
(`Manipulation.cs:671-676`):

```csharp
// On iOS (18.3) the last pointer event (release) is usually only 8.3 ms (120 fps) after and 2 px
// away from last move. This cause velocities to be very low and not relevant, so we prefer to keep
// the last known velocities.
if (elapsedMicroseconds < 10_000 && !delta.IsSignificant(_velocitiesThresholds)) return null;
```

The least-squares path bypasses that guard entirely. Worse, because the LS estimate is non-null and
non-zero, it passes `effectiveVelocities.IsAnyAbove(default)` at `Manipulation.cs:524` and
**overwrites** `_lastRelevantVelocities`, destroying the good fallback. Net effect: under-estimated
fling velocity, precisely on the platform the guard was written for.

### 5.4 🔴 Defect 3 — angular and expansion velocities are hard-zeroed

`Manipulation.cs:496-507`:

```csharp
velocities = new ManipulationVelocities { Linear = new Point(vx, vy), Angular = 0, Expansion = 0 };
```

Whenever the LS estimate is available (i.e. almost always for touch), rotation and scale velocities
are reported as zero. `InertiaProcessor.TryStart` gates rotate/scale inertia on
`Abs(velocities.Angular) > thresholds.Rotate` and `Abs(velocities.Expansion) > thresholds.Expansion`
(`InertiaProcessor.cs:118-123`), so **pinch-zoom and rotate inertia are silently disabled** by this
change.

### 5.5 🟠 Defect 4 — the estimate is computed on every update, not at fling time

`StageChanges()` (`Manipulation.cs:461-528`) is `[Pure]` and called from every `NotifyUpdate()`. The
LS path therefore runs two QR decompositions per pointer move — and with delta thresholds now at 0
(§3.8), that is every raw digitizer sample. Flutter's own doc comment
(`velocity_tracker.dart:132-136`):

> To obtain a velocity, call `getVelocity` or `getVelocityEstimate`. This will compute the velocity
> based on the data added so far. **Only call these when you need to use the velocity, as they are
> comparatively expensive.**

`AddPosition` is O(1); `GetVelocityEstimate` should be called once, in `OnInertiaStarting`.

### 5.6 🟠 Defect 5 — no fling-velocity clamp, no confidence gate

Flutter clamps to `[kMinFlingVelocity, kMaxFlingVelocity] = [50, 8000]` px/s
(`gestures/constants.dart:90-95`) and Avalonia mirrors it (`VelocityTracker.cs:67-68, 200-203`). The
Uno port drops both `Confidence` and the clamp, so a degenerate fit (duplicate timestamps — very
likely on Win32, §4) can emit an arbitrarily large velocity straight into
`DesiredDisplacementDeceleration` and produce a runaway fling.

### 5.7 🟡 Defect 6 — the stale-tracker check is missing

Flutter/Avalonia keep a `Stopwatch _sinceLastSample` and return zero velocity if the last sample is
older than 40 ms (`velocity_tracker.dart:181-189`, Avalonia `:99-102`). The Uno port drops the
stopwatch entirely (only the `Sample` record survives). It is *partly* compensated by the
`delta > AssumePointerMoveStoppedMicroseconds` break, but only because the release sample is now fed
in (Defect 2) — i.e. two bugs partially cancelling.

---

## 6. Are the Win32 / X11 / Android input changes correct?

### Android — ✅ correct, unconditionally keep

`AndroidCorePointerInputSource.cs:229`. `MotionEvent.GetX/GetY` are `float`; removing `(int)` is a
pure precision restoration with no coordinate-space assumptions. Ship it on its own.

### X11 — ✅ correct

`X11PointerInputSource.XInput.cs:305-317`. `event_x`/`event_y` are `double`
(`x11Bindings_XInput.cs:257, 289`); `XTranslateCoordinates` is integer-only; the window→window map is
a pure integer translation, so `floor + translate + re-add fraction` is exact. Correct and minimal.

Unrelated caveat (not introduced here): `timeInMicroseconds = (ulong)(data.time * 1000)` (`:319`)
quantises timestamps to 1 ms, which caps velocity-estimator quality on X11.

### Win32 — ⚠️ plausible but unproven, and built on two unverified assumptions

`Win32WindowWrapper.Pointers.cs:91, 108-142`:

```csharp
private const double HimetricPerLogicalPx = 2540.0 / 96.0;   // 1 logical px = 1/96 inch
...
var screenLogicalX      = screenHimetric.X / HimetricPerLogicalPx;
var clientOriginLogicalX = (screenPx.X - clientPx.X) / scale;
return new Point(screenLogicalX - clientOriginLogicalX, ...);
```

What is right:

* Restricting HIMETRIC to `PT_TOUCH`/`PT_PEN` and leaving mouse on `ptPixelLocation*` is correct —
  Win32 mouse coordinates are integer screen pixels and HIMETRIC adds nothing.
* Deriving the screen→client offset from the **pixel** pair and subtracting it in logical units is
  the right structural idea given `ScreenToClient` is integer-only.
* Both call sites of `ReadCommonWParamInfo` were migrated to `Windows.Foundation.Point` (`:144, 165`);
  no stragglers.

What is unverified / risky:

1. **`2540/96` assumes `ptHimetricLocation` is an absolute physical-screen HIMETRIC coordinate whose
   origin coincides with the virtual-screen origin used by `ptPixelLocation`, and that Uno's logical
   pixel is physically 1/96 inch.** The second assumption is false whenever the user's DPI scaling
   does not match the panel's physical PPI (i.e. most of the time — 150 % scaling on a 96-PPI panel).
   The documented Win32 recipe for high-resolution pointer data uses `GetPointerDeviceRects` to map
   the device's HIMETRIC rect onto the display rect; **there is no `GetPointerDeviceRects` call
   anywhere in the repo** (verified by grep). If the assumption is wrong the result is not a
   precision improvement but a *systematically offset and mis-scaled* touch position.
2. Mixing two coordinate derivations (HIMETRIC for the screen position, pixel-delta for the client
   origin) means any disagreement between them shows up as a constant offset, which on a multi-monitor
   virtual desktop is a very plausible failure.
3. There is no evidence in the branch of a runtime validation (no test, no sample, no measurement).

**Verdict: hold the Win32 hunk until it is validated empirically** (log `ptPixelLocation` vs
HIMETRIC-derived logical position side by side at 100 %, 150 %, 200 % scaling and on a secondary
monitor). And note that the *bigger* Win32 defect — `GetMessageTime()` millisecond timestamps
(`:148, 251`) — is untouched.

---

## 7. Verdict

### Overall: **HYBRID — salvage ~30 %, rewrite the core.**

The branch contains three genuinely good ideas (exponential physics, least-squares velocity,
sub-pixel input) buried under a plumbing rewrite that probably *caused* the artefacts, followed by
two rounds of symptom suppression. Cherry-pick the ideas; do not carry the plumbing.

### Per-file recommendation

| File | Verdict | Detail |
|---|---|---|
| `src/Uno.UI.Runtime.Skia.Android/Devices/Input/AndroidCorePointerInputSource.cs` | **SALVAGE — take as-is** | 1-line, correct, no assumptions. Land separately. Follow up with `GetHistoricalX/Y`. |
| `src/Uno.UI.Runtime.Skia.X11/Devices/Input/X11PointerInputSource.XInput.cs` | **SALVAGE — take as-is** | Exact, minimal, correct. |
| `src/Uno.UI.Runtime.Skia.Win32/Devices/Input/Win32WindowWrapper.Pointers.cs` | **HOLD — validate first** | Structure is fine; `2540/96` and the origin assumption are unproven (§6). Higher-value fix in the same file: replace `GetMessageTime()` with a QPC/`dwTime` source. |
| `src/Uno.UI/UI/Input/WinRT/VelocityTracker.cs` | **SALVAGE the solver, REWRITE the API** | Keep `LeastSquaresSolve` + the buffer walk verbatim. Add back: `Confidence`, min/max fling clamp, `_sinceLastSample`. Make it per-pointer (`Dictionary<uint, VelocityTracker>` or a tracker on each tracked pointer). |
| `src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.cs` | **REWRITE the wiring; RECONSIDER the thresholds** | Drop the `Remove()` feed (§5.3). Restore `Angular`/`Expansion` from the existing estimator instead of zeroing (§5.4). Call `GetVelocityEstimate()` only from the inertia-start path (§5.5). Re-evaluate `Delta*.TranslateX/Y = 0` — separate "feed the tracker" from "raise a manipulation event" (§3.8). Remove the `#if IS_UNO_UI_PROJECT` split or move the tracker into the shared file. |
| `src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs` | **SALVAGE the model, REWRITE the constants + timer** | Keep `GetValue`/`IsCompleted`/`GetCompletionTime`/`GetDecayRateFromDesiredDisplacement` (§2.1). Delete the `RenderingTime` branch or fix `RenderingEventArgs` to carry a real present/VSync timestamp first (§3.1). Consolidate the decay constants behind one documented model; delete the iOS block in `SCP.Managed` that computes a constant (§3.6a). Validate the ~3.7× shorter fling against WinUI before shipping. |
| `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs` | **REWRITE** | Keep: `TryStartWheelInertia`'s projected-bound chaining (§2.3), the `Rendering`-unsubscribe-on-flush fix (§2.7). Drop: `MathF.Round` snapping (§3.4), `InertiaViewportUpdateInterval` (§3.5), `VelocityBoost`, the duplicated inline `Updated()` body. Fix while here: the stale-`AnimationController` drop guard at `:359-369` (§3.10). |
| `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.cs` | **REWRITE the wheel hunk; DROP the arrange hunk** | The `TryStartWheelInertia` call sites (`:335, 361`) are fine in principle. `Scroller?.OnPresenterArranged()` (`:263`) is the entry point of the arrange-deferred model — drop with §3.2/§3.3. |
| `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs` | **DISCARD the `#if __SKIA__` block; keep two primitives** | Keep `DelayViewChanged`/`FlushViewChanged` (§2.4) and — if a genuine WinUI-parity need is shown — `Enter/LeaveIntermediateViewChangedMode`. Discard `_hasPendingScrollUpdate` + per-tick `InvalidateArrange()` + `OnPresenterArranged` (§3.2, §3.3): it regresses the base's deliberate "no layout per intermediate tick" behaviour and forks Skia from every other target. |
| `src/Uno.UI.RuntimeTests/.../Given_ScrollViewer.cs` | **HYBRID** | Keep the ViewChanged-semantics tests as *specification* (they encode intended behaviour even if the implementation changes). Replace all four `Task.Delay(1500)` (`:739, 754, 1576, 1584`) with the `WaitForInertiaToSettle` polling helper already written at `:883-899`. Do not restore `When_LotOfWheelEvents_Then_IgnoreIrrelevant` in its old form — it asserted on the keyframe-animation primitive. |

### Sequencing suggestion for a rewrite

1. **Land the input-precision fixes alone** (Android + X11 now; Win32 after validation). They are
   independent, low-risk, and measurable.
2. **Fix the clock before the physics.** Give `RenderingEventArgs` a real frame timestamp and decide
   where in the frame `Rendering` fires. Everything else is guesswork until then.
3. **Attack the actual bottleneck**: get scroll-offset visual updates off the
   "arrange + EVP + layout must converge or the frame is dropped" path, rather than rationing EVP.
   Approach A (commits 7–8) was the right instinct with the wrong mechanism — an *interruptible*,
   continuously-evaluated composition animation (expression/`InteractionTracker`-style) is the shape
   to aim at, not pre-baked keyframes.
4. **Then** re-land physics + velocity with a single documented decay model and per-pointer trackers.

---

## 8. Open questions

* Does `RenderingEventArgs.RenderingTime` have any consumer that depends on it being wall-clock? If
  not, it can be changed to a real frame/present timestamp without a breaking change.
* Is `Rendering` intended to fire before or after picture recording? Current head fires *after*
  (`OnFramePictureRecorded`), which guarantees ≥1 frame of latency for any physics driven from it.
* What is WinUI's actual legacy-`ScrollViewer` wheel coast distance and duration per notch? Every
  wheel constant in this branch is an unverified guess. A throwaway WinUI app measuring
  `VerticalOffset` vs time for a single notch would settle `DecayPerMs` and `VelocityBoost` in
  minutes.
* Does `IInteractionContext` really fire `INTERACTION_ID_MANIPULATION` with no displacement gate
  (the stated justification for zero delta thresholds)? Not verifiable from the sources at
  `D:/Work/microsoft-ui-xaml2`.
* Does `POINTER_INFO.ptHimetricLocation` need `GetPointerDeviceRects` normalisation on Uno's
  supported configurations, or is the flat `2540/96` conversion adequate? Needs a measurement, not
  an argument.
* Why does a frame get skipped when `CanRecordPicture` returns false — is a one-pass `UpdateLayout()`
  a hard requirement, or can a partially-converged tree still be recorded? This is the single
  highest-leverage question for scroll smoothness and the branch never asks it.
* Is the ~3.7× reduction in fling distance (§3.6c) desirable? It is a large behavioural change that
  no test or measurement in the branch covers.
