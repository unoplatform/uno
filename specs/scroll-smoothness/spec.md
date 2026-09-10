# Scroll smoothness on Skia targets — design

Status: design accepted, implementation staged.

This document is self-contained. Every claim below is either cited to a file in this repo, cited to a
public upstream source, or labelled as an on-device measurement. Implementation status, with the
commits that carry it, is in §8.

---

## 1. The problem, stated correctly

The brief was "make scrolling smoother, highest possible FPS on all Skia targets". Reading the
pipeline end to end changes what that means:

> **The physics model is not the bottleneck. The per-frame render cost is, and clock discipline is
> second.** Physics changes *feel*; it does not change FPS.

And the reported per-platform ordering (Win32 best → Android/iOS worse → mobile-browser WASM worst)
decomposes into a **device axis** and a **platform axis** that were being conflated:

> Win32-with-a-wheel is the only configuration in which Uno's scroll motion is produced by a single
> time-parameterised function evaluated exactly once per frame, *inside the frame it affects*, with
> no quantizer between the input and the visual.

Everything else adds at least one of: a 2-DIP motion quantizer (touch drag), a full frame of latency
(touch inertia), a serialized record+raster budget (WASM), an unaligned record phase (Android), or no
smoothing at all (Apple trackpad, Android wheel). On top of that sits an O(realized-visuals)
Skia-pathops cost per frame that is invisible on a desktop CPU and decisive on a phone.

---

## 2. What the four reference stacks actually do

| | Where scroll motion is computed | Thread | Per-frame UI-thread work |
|---|---|---|---|
| **WinUI legacy `ScrollViewer`** | `directmanipulation.dll`, curve and duration entirely inside the OS component | DManip delegate thread | Poll the content transform **once per tick** and mark the element dirty for bounds only — **no render walk** |
| **WinUI `ScrollView`/`ScrollPresenter`** | Composition `InteractionTracker` + two `ExpressionAnimation`s bound to `it.Position`/`it.Scale` | compositor thread | Notifications only (offsets, scrollbars, UIA, virtualization) |
| **Avalonia** | `ScrollContentPresenter.Offset` → `InvalidateArrange` → child arranged at `-Offset` | UI thread | A layout pass. Wheel is instantaneous, 50 DIP/unit, no easing |
| **Flutter** | Closed-form `Simulation.x(t)` evaluated at the **presentation timestamp** | UI thread | Sliver viewports relayout; `SingleChildScrollView` is paint-only |

Two conclusions the design rests on:

1. **Both WinUI stacks move scroll pixels off the UI thread.** That is the correct long-term
   architecture and it is what `C10` targets — but it is not reachable in one step, and Uno's
   existing `InteractionTracker` is *not* a shortcut to it (§6).
2. **Flutter proves you do not need a compositor thread to feel smooth** — you need one clock, one
   closed-form evaluation per frame, at the presentation timestamp. That is reachable now.

**Explicitly not adopted:**

- Avalonia's wheel model (50 DIP/unit, no animation) — instantaneous per detent, which is the
  judder this work is trying to remove.
- Avalonia's UI-thread `Dispatcher.InvokeAsync(Input)` fling loop — it is the pattern Uno already has
  and is trying to leave.
- A `CubicBezier` on the default programmatic scroll animation — WinUI supplies **no** easing
  function on the `Vector3KeyFrameAnimation` it builds for a programmatic scroll (public
  `microsoft-ui-xaml`, `dev/ScrollPresenter/ScrollPresenter.cpp`); inventing one is a silent parity
  deviation.
- **Pointer resampling as the headline fix.** Flutter ships it *off*
  (`GestureBinding.resamplingEnabled = false`, `gestures/binding.dart`) and it costs 38 ms of
  added latency. It is Tier C, opt-in, and only if A and B do not close the gap.
- Pointer prediction/extrapolation — neither WinUI stack ships it; it causes overshoot artifacts.

---

## 3. Root causes, ranked by provable impact

Ranked by the impact each was shown to have, most decisive first. This section, and §5, describe the
code **as found** at the start of this work; §8 says which of them are fixed today. §3.1 and §3.2
give the evidence for the top two.

| Rank | Cause | Shape |
|---|---|---|
| 1 | **Per-visual Skia pathops damage over the whole scrolled subtree, every frame** | O(realized visuals) × ~4 path booleans + allocs |
| 2 | **Children-picture cache destroyed subtree-wide every scroll frame** | O(realized visuals) walk; collapsing optimization structurally unreachable |
| 3 | **No frame clock** — six motion sources, none using presentation time, none agreeing | latency + jitter |
| 4 | **Touch drag quantized to ≥2 logical px** | discretization, touch-only |
| 5 | **WASM serializes record + raster** on one thread | halves FPS above 16.7 ms total |
| 6 | **Wheel = 1 s Power-10 keyframe restarted per detent**, each restart firing a non-intermediate `ViewChanged` → `InvalidateArrange` | per-detent hitch |
| 7 | `OnFrame` publishes a **one-frame-stale** offset → virtualization targets last frame's viewport | leading-edge blanks |

### 3.1 Cause 1 in detail — verified firsthand

`Visual.ContributeDamageOnPaint` (`Visual.Damage.skia.cs:27-69`) early-outs only when
`!contentChanged && !moved && !shadowSilhouetteChanged`. During a scroll,
`moved = matrix != _lastRenderMatrix` is **true for every visual in the subtree**, so the early-out
provably never fires. Each visual then pays, inside `Render()`, on the UI thread:

- `OutsetForAntialiasing` — `SKPaint.GetFillPath` (stroke-to-fill) + `Op(Union)` (`:178-189`)
- `contentPath.Op(clipPath, SKPathOp.Intersect, contentPath)` (`:96`)
- `damage.Union(regionPath)` → `region.Op(addition, Union, region)` against a **monotonically
  growing** accumulator (the damage accumulator, now `DamageRegion.skia.cs`)
- `damage.UnionRect(_lastRenderBounds)` → native `CreateRectPath` alloc + another `Op(Union)`

For a ListView with ~200 realized visuals that is ~800 Skia path booleans per frame, each more
expensive than the last. **And the result is degenerate**: when everything moved, the union *is* the
scroll port's clip rect — an O(1) answer computed in O(n) with the most expensive primitive available.

### 3.2 Cause 2 in detail — verified firsthand

```csharp
// Visual.skia.cs:140-146
internal virtual bool SetMatrixDirty()
{
    var matrixDirty = (_flags & VisualFlags.MatrixDirty) != 0;
    _flags |= VisualFlags.MatrixDirty;
    InvalidateParentChildrenPicture(false);   // <-- starts at Parent
    return !matrixDirty;
}
```

`ContainerVisual.SetMatrixDirty` recurses into every child (`ContainerVisual.skia.cs:212-227`), and
each child's call frees **its parent's** `_childrenPicture`. Transitively, every `ContainerVisual`
in the scrolled subtree loses its cache. `Visual.Render` then resets
`_framesSinceSubtreeNotChanged = 0` (`:396`), and `RenderChildrenStep` requires ≥50 clean frames and
≥100 visuals to collapse (`:40-41`, `:531-544`). **The picture-collapsing optimization can never
engage inside a scrolling subtree.**

This is safe to fix because a cached `_childrenPicture` is recorded in the visual's **local space**
(`rootTransform = Invert(visual.TotalMatrix)`, `:555`; re-applied on replay via `CreateLocalSession`,
`:1011-1018`) and is therefore transform-independent by construction.

---

## 4. Design

### 4.1 Principle

> **One clock. One closed-form evaluation per frame. One damage rect for a pure translation.**

Three invariants the implementation must establish:

- **I1 — Single frame timestamp.** Exactly one timestamp is sampled per frame, at `Render()` entry,
  and every animation/simulation in that frame is evaluated at it. Phase 2: make it the platform's
  *predicted presentation* time.
- **I2 — Motion is a function of time, not of event arrival.** Every scroll source (wheel, key,
  touch drag, touch inertia, programmatic) produces or updates a single closed-form `x(t)`
  evaluated once per frame, before the picture is recorded. Impulses **accumulate into** the running
  simulation rather than restarting it.
- **I3 — A pure translation costs O(1) damage, not O(visuals).** A frame whose only change is an
  ancestor transform contributes `oldSubtreeRect ∪ newSubtreeRect` (clamped to the scroll port clip)
  and skips the per-visual pathops entirely.

### 4.2 Staged plan

**Tier A — changes the frame budget. Do first; everything else is unmeasurable until these land.**

| | Intervention | Invariant |
|---|---|---|
| A1 | Scroll-aware damage fast path: when a frame's only invalidation is an ancestor transform, damage = `oldRect ∪ newRect`; skip `ContributeDamageOnPaint`'s pathops branch for the subtree (still refreshing `_lastRenderBounds`/`_lastRenderMatrix`) | I3 |
| A2 | Stop tearing down `_childrenPicture` on pure transform changes — `SetMatrixDirty` must not route through `InvalidateParentChildrenPicture` | I3 |
| A3 | One authoritative per-frame timestamp, sampled at `Render()` entry, threaded to every animation evaluation and to `RenderingEventArgs` | I1 |
| A4 | Remove the 2-logical-px drag quantizer: decouple "advance the scroll / feed the velocity tracker" (no threshold) from "raise the public `ManipulationDelta`" (keep the threshold) | I2 |
| A5 | Give touch inertia a **pre-record** tick — move it off `CompositionTarget.Rendering` (post-record) onto the pre-record hook in `Compositor.RenderRootVisual` | I1, I2 |

A1 and A2 are coupled and must land together: once a subtree's children-picture survives a move, its
descendants no longer run `ContributeDamageOnPaint`, so damage *must* be computed at subtree level.

**Tier B — small, safe, independently valuable.**

B1 `OnFrame`/`ReEvaluateAnimation` subscription order (published offset is one frame stale) ·
B2 the `int` wheel-delta dead zone (`delta / 120` integer-divides every precision-touchpad event to
**zero** — total functional failure of `ScrollView`/`ItemsView` on precision devices) ·
B3 wheel `ViewChanged` + `InvalidateArrange` storm ·
B4 `AnimationController` allocation + unbounded `Stopped` subscription growth ·
B5 re-key the "immediate wheel" branch from `OperatingSystem.IsIOS()||IsMacOS()` onto
`PointerPointProperties.IsTouchPad` ·
B6 **implement `MotionEventActions.Scroll` on Skia-Android — mouse wheel is currently entirely dead** ·
B7 restore the `ItemsRepeater` viewport-significance guard on Skia/WASM (it is behind
`#if !UNO_HAS_ENHANCED_LIFECYCLE`, which is defined on exactly those platforms) ·
B8 Win32 pointer timestamp overflow (`(ulong)(GetMessageTime() * 1000)` wraps at ~35.8 min) and
integer-truncated positions ·
B9 per-frame allocations (`_runningAnimations.Keys.ToArray()`, `RaiseRendering` arrays, LINQ in
`KeyFrameEvaluator`).

**Tier C — model and architecture. After A and B, and after measuring.**

C1 one continuous velocity-composing scroll model shared by wheel/key/touch (Flutter's
`FrictionSimulation` / `ClampingScrollSimulation` transcribe directly; §4.3 gives the closed forms,
and the wheel and fling halves already ship — see §8) ·
C2 least-squares velocity estimator (degree-2, 100 ms horizon, 20-sample ring, ≥3 samples, 40 ms
stopped cut-off) ·
C3 feed it every available sample — Android historical, iOS coalesced, browser
`getCoalescedEvents`, Win32 frame history (**Uno uses none today, on any backend**) ·
C4 pointer resampling, opt-in only ·
C5/C6 virtualization buffer and `GridView`/`ItemsWrapGrid` on Skia ·
C7 coalesce `ViewChanging`/`ViewChanged` to one pair per frame ·
C8 run WASM's record **inside** the rAF callback ·
C9 route Android's render enqueue through `Choreographer.postFrameCallback` — **since withdrawn**,
see F3 ·
C10 move scroll off the UI thread entirely.

### 4.3 Physics target (Tier C1), for reference

Per-platform parity models, all closed-form. The touch halves are implemented in
`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollFlingSimulation.cs`, which carries the
provenance of each constant in code; the wheel half is `ScrollDecaySimulation.cs` beside it.

- **Android**: `x(t) = x₀ + S·(1 − u^2.3582017)`, `dx(t) = v₀·u^1.3582017`, `u = 1 − clamp(t/T,0,1)`,
  where `T = 0.8253706·(|v₀|/vRef)^0.7362675` and `S = v₀·T/2.3582017`.
  **DIP caveat:** Flutter's `_physicalCoeff` substitutes 160 dpi (Android dp). For WinUI DIPs
  (1/96 in) the correct value is `9.80665·39.37·96·0.84 = 31134.12` px/s², otherwise Android flings
  travel 1.667× too far.
- **iOS**: `FrictionSimulation(drag: 0.135, …)` spliced into a `SpringSimulation` at a
  Newton-solved handoff time, velocity capped at 5000 px/s.
- **Programmatic** (WinUI parity): single-keyframe `Vector3KeyFrameAnimation`, **no easing
  function**, `duration = clamp(hypot(dx,dy)·5 ms, 50 ms, 1000 ms)`.

---

## 5. Platform-specific issues that prevent smooth scrolling

The second explicit deliverable, and — like §3 — the code **as found**; §8 says what is fixed today.
Paths are as they stand in the tree now, which for a few of them is not where the issue was first
read.

| Platform | Issue | Cite |
|---|---|---|
| **All Skia** | O(realized-visuals) Skia-pathops damage per frame | `Visual.Damage.skia.cs:27-105` |
| **All Skia** | `_childrenPicture` cache destroyed subtree-wide per scroll frame | `Visual.skia.cs:140-146, 245-258` |
| **All Skia** | No per-frame timestamp; `TimestampInTicks` re-reads `Stopwatch` per animation | `Compositor.cs`; `KeyFrameAnimations/KeyFrameEvaluator.cs` |
| **All Skia** | Touch inertia ticks *after* the record → structurally one frame late | `UI/Xaml/Media/CompositionTarget.Rendering.cs` (the `Rendering` raise, enqueued after the record) |
| **All Skia** | Touch drag quantized to ≥2 logical px | `UI/Input/WinRT/GestureRecognizer.Manipulation.cs` |
| **All Skia** | Two-point velocity estimate (first vs last sample), no fit/horizon/outlier rejection | `UI/Input/WinRT/GestureRecognizer.Manipulation.cs` |
| **All Skia** | **No backend uses coalesced / historical / predicted pointer samples** | repo-wide: no input source calls `MotionEvent.GetHistorical*`, `getCoalescedEvents()`, `UIEvent.CoalescedTouches` or `GetPointerFrameInfo` |
| **All Skia** | `InteractionTracker` inertia on a threadpool `Timer` @ 17 ms, mutating composition off the UI thread | `InteractionTrackerActiveInputInertiaHandler.cs:18-19, 24, 47-48`; `InteractionTrackerPointerWheelInertiaHandler.cs:15-18, 54-55` |
| **All Skia** | `int` wheel delta ÷ 120 ⇒ **0** for every precision device on the tracker path | `InputManager.Pointers.Managed.cs:349` |
| **All Skia** | Render slot blocked behind the full Normal dispatcher backlog | `NativeDispatcher.cs:206-217` |
| **Win32** | Pointer timestamp overflows at ~35.8 min; positions truncated to integer logical px | `Win32WindowWrapper.Pointers.cs:113-114, 124, 227` |
| **Android** | **Mouse wheel entirely dead** — no `MotionEventActions.Scroll` case | `AndroidCorePointerInputSource.cs:125-196` |
| **Android** | Touch positions truncated to integer **physical** px, then converted to DIP | `AndroidCorePointerInputSource.cs:226-229` |
| **Android** | Render record posted via `Handler.Post`, not `Choreographer` → suspected arbitrary phase vs vsync. **F3 refutes this**: the measured frame intervals are bimodal at 1x/2x the refresh period, not spread | `NativeDispatcher.Android.cs:39-43` |
| **Android** | `MotionEvent` historical samples never read | `AndroidCorePointerInputSource.cs:71-119` |
| **iOS/macOS** | Wheel/trackpad bypasses smoothing entirely (`OperatingSystem.IsIOS()` &#124;&#124; `IsMacOS()` → `DisableAnimation:true`), keyed on OS rather than on device kind | `ScrollContentPresenter.cs:311-325, 335-343` |
| **iOS** | `UIEvent` is passed to `TouchesMoved` and never read → coalesced/predicted touches discarded | `AppleUIKitPointerInputSource` |
| **WASM** | Record + raster **serialized** on one thread; record enqueued as a task outside the rAF callback; finger-to-photon ≥2 frames | `BrowserRenderer.cs:65-115` |
| **WASM** | `pointermove` handled per-event, no `getCoalescedEvents()` | `ts/Runtime/BrowserPointerInputSource.ts:78` |
| **Skia/WASM** | `ItemsRepeater` viewport-significance throttle is dead code (behind `#if !UNO_HAS_ENHANCED_LIFECYCLE`) | `ViewportManagerWithPlatformFeatures.cs:599-608` |
| **Skia/WASM** | `ItemsWrapGrid` compiled out ⇒ `GridView` silently falls back to a non-virtualizing `WrapPanel` | `ItemsWrapGrid.cs:1` |

---

## 6. Why not "just migrate to `ScrollPresenter`"

Uno already carries a ~289 KB `ScrollPresenter` port, a 2717-line `ScrollView`, and a managed
`InteractionTracker`. Migrating `ScrollViewer` onto them is **not** a shortcut, because the parts
that produce motion have the same class of defect plus new ones:

- `InteractionTrackerActiveInputInertiaHandler` and `InteractionTrackerPointerWheelInertiaHandler`
  both tick on a `System.Threading.Timer` at a fixed **17 ms** — a threadpool timer beating against
  a 16.67 ms vsync (~2.5 s beat period), and 2× off on a 120 Hz display.
- Both mutate composition state from a **threadpool thread**.
- Both sample `Stopwatch.ElapsedMilliseconds` — **integer** milliseconds.
- The wheel handler is **constant velocity for 0.25 s**, with no deceleration curve at all.
- `ReceivePointerWheel` receives an integer-divided delta that is **0** for every precision device.

Fix the motion layer first (Tier A). Treat the `ScrollPresenter` migration as an independent parity
question (C10), and fix these tracker defects as part of it.

---

## 7. Measurement

`Application.Current.DebugSettings.EnableFrameRateCounter` → `SkiaRenderHelper.FpsHelper` already
reports **dropped** frames (vsync fired with nothing new) separately from **unpresented** frames (the
UI thread recorded work that never reached the screen), plus mean frame time and draw-to-present
delay. That split is the diagnostic that matters. SamplesApp exposes it as `ShowFpsIndicator`.

Added by this work: `ScrollSmoothnessBenchmark` sample
(`src/SamplesApp/SamplesApp.Samples/Windows/UI/Xaml/Controls/ScrollViewerTests/`), which reports the
metric the eye actually responds to — **the coefficient of variation of the per-frame offset delta**.
A scroll averaging 8 px/frame but alternating 0/16 reads as judder at a nominal 60 FPS; CV catches
that where FPS does not.

Three measurements settle the ranking empirically, in order:

1. **A1's premise** — count visuals taking the pathops branch per frame during a ListView drag.
   Expect hundreds.
2. **A2's premise** — count `_childrenPicture` frees per frame. Expect ≈ the number of
   `ContainerVisual`s in the scrolled subtree.
3. **A4's premise** — log the distribution of `changeSet.Delta.Translation.Y` during a slow drag.
   Expect a spike at exactly ±2 and a hole in (−2, 2).

## 8. Implementation status

Every commit named below is an ancestor of this branch's tip. Where a row says "same", it landed in
the commit named on the row above it.

| Item | Status | Commit |
|---|---|---|
| A1 — scroll-aware damage (bounds for moved-unchanged visuals; rect accumulator) | **done** | `cc4c8fd10f perf(composition): Make scroll-frame damage O(1) per moved visual`, refined by `1902373fbc`, `5222e530dc`, `b03850d6c2`, `4f39931c99` |
| A3 (partial) — `Compositor.FrameStarting`, one timestamp per frame, raised before the record | **done** | `b97d67a174 feat(composition): Add a uniform frame clock for per-frame motion drivers`, plus `d69707a611` (tick before layout, not inside the record) and `e93ad55d27` |
| A4 — remove the 2-DIP drag quantizer | **done** | `109156f9b2 fix(scroll): Deliver touch and wheel deltas unquantized` |
| B2 — `int` wheel-delta dead zone | **done** | same |
| A5 — touch inertia ticks before the frame it affects (`CompositionInertiaProcessorTimer` on `FrameStarting`) | **done** | `02627307f1 perf(scroll): Drive wheel and touch scrolling from the frame clock` |
| B1 — `OnFrame` publishes the current frame's offset | **done** | same |
| C1 (wheel + touch) — `ScrollDecaySimulation` replacing the restarted ease, `ScrollFlingSimulation` + `ScrollVelocityTracker` for touch | **done** | same |
| Measurement harness (`ScrollSmoothnessBenchmark`) | **done** | `110e7786bf test(samples): Add a scroll smoothness benchmark sample` |
| Frame drivers unsubscribe from the target they hooked, not from whichever is current | **done** | `fc72f5f2ca fix(scroll): Unsubscribe frame drivers from the target they were hooked to` |
| A3 (fix) — frame drivers tick once per native frame, not once per dispatcher pump (F5) | **done** | `9ffcd8fb74 fix(skia): Tick frame drivers once per frame` |
| A3 (guard) — drop a target's frame drivers when its host unregisters (F6) | **done, but only reachable on the hosts that unregister** — see F6 | `f9a114616b fix(skia): Drop frame drivers when the target unregisters` |
| A2 — preserve `_childrenPicture` across a pure transform change | **not started** | — |
| A3 (rest) — thread the frame timestamp through *all* animation evaluation and `RenderingEventArgs` | **not started** | — |
| Scroll diagnostics (opt-in, per-frame telemetry) | **not started** — F5 was measured with a temporary counter that was not kept | — |
| Android — `Choreographer` pacing for the render thread (C9) | **not planned** — F3 measured the jitter as missed deadlines, not phase; C9 would not fix it | — |
| A5 on non-Skia targets — `CompositionInertiaProcessorTimer` still falls back to post-record `CompositionTarget.Rendering` there | **not started** | — |
| B3–B9, rest of Tier C | **not started** | — |

### A1 as shipped — why `preferBounds` ignores `shadowSilhouetteChanged`

`ContributeDamageOnPaint` computes `preferBounds = moved && !contentChanged`
(`Visual.Damage.skia.cs:56-57`), which reads as an oversight next to the four signals the early-out
above it tests: `shadowSilhouetteChanged` is not excluded. It does not need to be, and the code no
longer says why, so it is recorded here.

`shadowSilhouetteChanged` can only be true when `ShadowState is not null`. The only thing
`preferBounds` does is steer away from the exact-geometry branch in `TryGetPaintDamageRegion`, and
that branch is itself gated on `ShadowState is null`. So for every visual that can raise the flag,
`preferBounds` has no branch to steer, and adding the term would change nothing.

(Commit `7eaff0f74e docs(composition): Clarify preferBounds guard` added this as a code comment;
`5222e530dc` rewrote the surrounding comment and dropped it. That commit message also claims to drop
a stray BOM, which it does not — the BOM removals are `32929ef7b1`, on `Visual.PaintingSession.skia.cs`
and `SkiaRenderHelper.cs`.)

### A2 — why it is not landed yet, and what it needs

A2 is rank 2 and is **coupled to the rest of A1**. Once a visual's `_childrenPicture` survives a
move, `RenderChildrenStep` replays it and the descendants never run `ContributeDamageOnPaint`, so
their `_lastRenderBounds`/`_lastRenderMatrix` go stale and the subtree contributes *no* damage —
under-damage, i.e. visual corruption. A2 therefore requires the subtree-level damage fast path:

1. Split `SetMatrixDirty()` into the originating call (which must keep invalidating the parent's
   children-picture, because the visual's position *relative to its parent* changed) and a
   `SetInheritedMatrixDirty()` used by the recursion (which must not, because a descendant's
   position relative to its parent is unchanged when an ancestor moves).
2. Cache the collapsed subtree's local-space bounds at record time, and on each replay contribute
   `oldBounds ∪ newBounds` mapped by `TotalMatrix` — O(1) for the whole subtree.
3. Keep `_lastRenderMatrix` refreshed for the collapsed root so the next real render is correct.

### Validation performed

- **Compile**: `Uno.UI.csproj` (Skia flavour) and `SamplesApp.Skia.Generic` (Release, net10.0) — clean.
- **Runtime** (Skia Desktop / Win32): 135 tests across
  `Windows_UI_Composition`, `Windows_UI_Input`, `Given_ScrollViewer`, `ScrollViewerTests`,
  `Given_ScrollViewer_Zoom`, `ListViewTests` → **131 passed, 4 failed, 3 skipped**.
  The 4 failures are **identical to the pre-change baseline** on this machine
  (`When_Home_End_PageDown_PageUp`, `When_NonRound_Content_Height`,
  `When_Presenter_Doesnt_Take_Up_All_Space`, `When_ScrollViewer_Resized`) — all fractional-DPI
  layout assertions (e.g. expected 175, actual 175.19999), unrelated to this work.
- **Fails-before / passes-after**: `When_SlowTouchDrag_Then_ScrollAdvancesEveryMove` fails with the
  quantizer restored, reporting offsets `[30, 32, 32, 34, …]` — 3 advances over 6 one-pixel moves.
  That is the measurement §7 predicted for A4: a spike at exactly ±2 and a hole in (−2, 2).

**Not yet measured**: the A1 frame-budget win. It needs the instrumented counts described in §7 on a
device where the cost is decisive (a phone or a mobile browser), which this Win32 machine is not.

## 9. Field findings (from on-device testing)

F1–F4 were observed by the product owner on real hardware, not derived from the code, and each still
needs its own investigation. F5 was measured afterwards on the desktop and WASM heads and is fixed.
F6 was found by reading the frame-driver code that F5 produced; half of it is fixed, half is recorded.

### F1 — WASM in a mobile browser is still the worst case

Wheel/trackpad scrolling in a desktop browser benefits from the decay model (§4.2), but touch on a
phone browser remains poor. Consistent with the platform axis in §5: WASM serializes record + raster
on one thread and enqueues the record *outside* the rAF callback (C8). A5 has since landed, so
inertia is no longer a frame late there, and F5 removed the free-running driver tick that was costing
WASM several layout passes per presented frame — but C8 remains, and it is the larger half. Likely
warrants its own change set rather than being folded into the scroll work, since the fix is in the
WASM host's frame scheduling, not in scrolling.

### F2 — rAF is capped at 60 Hz unless touch is active (Android browser)

On a 120 Hz Android phone, the browser runs `requestAnimationFrame` at 60 Hz while idle and boosts to
120 Hz only while a touch is in progress. **The practical consequence is that a fling runs at half
the frame rate of the drag that produced it**, with the cadence changing at the exact moment the
finger lifts.

Two implications for the design:

1. **Any motion model must be correct under a frame rate that changes mid-flight.**
   `ScrollDecaySimulation` and `ScrollFlingSimulation` both are — they evaluate closed-form at
   absolute elapsed time, so a 60 Hz frame after a run of 120 Hz frames yields the correct position
   rather than drifting. The parabolic inertia processor meets the same standard now that A5 has
   landed: `CompositionInertiaProcessorTimer` passes `FrameStarting`'s timestamp minus its anchor, so
   `InertiaProcessor.Process` receives absolute elapsed time and `UpdateCumulative` evaluates the
   parabola at it rather than integrating per tick.
2. **Frame-rate-dependent tuning is invalid.** Any constant chosen by eye at 120 Hz will be wrong at
   60 Hz and vice versa. This is a second reason (alongside A3) not to retune physics constants
   before the clock work is complete.

Whether Uno can request a sustained high refresh rate from the browser is unverified; there is no
standard API for it, unlike native Android's `Surface.setFrameRate`.

### F3 — Android (native Skia): measured, the jitter is frame production, not input

On-device capture, Samsung Fold 7 (120 Hz panel, Vulkan render path — `UnoSKVulkanView`, not the GL
`UnoSKCanvasView` most of the earlier analysis assumed). 1560 samples, segmented on >120 ms gaps so
pauses between gestures do not contaminate the statistics.

| Phase | Stream | n | dt mean | dt sd | dt p95 | jerk mean | jerk p90 |
|---|---|---|---|---|---|---|---|
| Drag | **input** | 310 | 8.45 ms | **1.35** | 8.70 | **0.090** | **0.232** |
| Drag | **frame** | 229 | 11.25 ms | **4.12** | 16.91 | **0.532** | **1.105** |
| Inertia | tick | 488 | 9.34 ms | 6.56 | 14.85 | 0.556 | 1.082 |
| Inertia | frame | 489 | 9.63 ms | 8.14 | 14.96 | 0.665 | 0.867 |

**Pointer input is near-perfect**: 8.45 ms mean with 1.35 ms standard deviation — a clean 118 Hz
stream. **Frame production is not**: 3× the interval spread and **6× the jerk** of the input driving
it. The frames are failing to render clean input evenly.

Frame-interval histogram over the whole capture:

```
  0-3ms   ###### 60      <- near-duplicate records, ~8% of frames
  4-7ms   ########## 103
  8-11ms  ######################################## 382   <- 120Hz cadence
 12-15ms  ########## 95
 16-19ms  ######## 78                                    <- dropped to 60Hz cadence
 20ms+    14 (tail to 78ms)
```

The panel and the input are both at ~120 Hz; frame production alternates between the 120 Hz and
60 Hz cadences, with a tail. That beat is what reads as "small jitters".

**Implications, in order:**

1. **This is not an input problem.** Coalescing, resampling and velocity estimation (C2–C4) would not
   have moved this number. Confirms §3's ranking: clock discipline over physics.
2. **The ~8 % of frames at 0–3 ms are near-duplicate records** — the same double-record-per-present
   pattern measured on Win32. Wasted UI-thread work at best.
3. **Inertia inherited the frame irregularity** — at capture time it still ticked on
   `CompositionTarget.Rendering`, and added its own: 116 ms maximum gap. A5 has since moved it to
   `Compositor.FrameStarting`, which removes the structural frame of latency but not the underlying
   frame-production spread, and does nothing at all for drag. This capture has not been repeated
   since A5 and F5 landed.
4. **Not phase misalignment — missed deadlines.** An earlier reading of this data blamed
   `Handler.Post` scheduling the record at an arbitrary phase relative to vsync (P3/C9). That is
   wrong, and the histogram refutes it: arbitrary phase produces a *continuous* spread, whereas the
   measured distribution is **bimodal at 1x and 2x the refresh period**, which is the signature of a
   pipeline already quantised to the display whose producer sometimes misses a deadline.
   **C9 (Choreographer alignment) would not fix this and should not be promoted.**

5. **The Android present mode is the live candidate.** The two Android render views differ, and the
   difference matters:

   | View | Present | Vsync-locked? |
   |---|---|---|
   | `UnoSKCanvasView` (GL) | `GLSurfaceView` + `eglSwapBuffers` | **yes** — the swap blocks |
   | `UnoSKVulkanView` | swapchain, **MAILBOX preferred**, FIFO fallback | **no** — MAILBOX returns immediately |

   `src/Uno.UI/Vulkan/Interop/VulkanDisplay.cs:104-107` prefers `VK_PRESENT_MODE_MAILBOX_KHR`, which by definition replaces
   the pending image rather than waiting, discarding frames that were never shown. Combined with a
   free-running render thread (`UnoSKVulkanView.RenderLoop` waits on an event, not a frame callback),
   presentation times are uneven even when production is healthy. Pacing comes only indirectly, from
   swapchain-acquire back-pressure — which is exactly enough to quantise intervals to 1x/2x the
   refresh period without making them *even*.

   **Next experiment:** force `VK_PRESENT_MODE_FIFO_KHR` on Android and re-measure this histogram. If
   the 16 ms bucket collapses, the present mode is the cause. If it does not, the misses are
   UI-thread record cost and that is where to look instead.

### F4 — Defects found while investigating, filed separately

| Issue | What | Scope |
|---|---|---|
| [#23937](https://github.com/unoplatform/uno/issues/23937) | The Vulkan render path leaks a `VkCommandBuffer` and `VkFence` **every frame** — the drain is gated behind an `autoFree` flag that defaults false and is never passed. ~7 200/minute at 120 fps. Causes the process to abort inside the driver during `vkCmdBlitImage`. | **All Vulkan hosts** (Win32, X11, Android). Predates this branch (`a9a4027136`). |
| not yet filed | `Visual._picture` / `_childrenPicture` are raw `IntPtr` never released on dispose — libc heap, not GPU memory, so it cannot produce the crash above. | Skia, all targets |
| not yet filed | `VulkanContext.Dispose()` leaves resources behind on surface teardown; `VulkanDisplay.EnsureSwapchainAvailable`'s `while (true)` cannot terminate under a persistent `VK_SUBOPTIMAL_KHR`. | All Vulkan hosts |
| not yet filed | Incremental Android builds of the SamplesApp head drop the `kotlinx-coroutines-android` AAR, so `androidx.window` throws in `onStart`. A clean build always fixes it. Reproduced ~4 times. | Android build |

### F5 — The frame-driver tick free-ran between vsyncs (fixed)

Measured with a temporary counter on the UI thread (driver ticks, records, presents, layout passes
per second, and the `FrameClock` interval), driving the wheel decay synthetically so no real pointer
was involved. Win32 host at 120 Hz (Debug), WASM host in Chromium at 120 Hz (Release):

| Head | Before | After |
|---|---|---|
| Win32 | ticks 15 000–19 000/s, layouts 15 000–19 000/s, presents 120/s, clock interval **0.04 ms** | ticks 120/s, records 120/s, presents 120/s, layouts 120/s, clock interval **8.4 ms** |
| WASM | ticks 520–796/s, layouts 519–796/s, presents 114–119/s, clock interval **0.4–0.5 ms** | ticks 100–110/s, records 101–113/s, presents 101–115/s, layouts 100–110/s, clock interval **8.3 ms** |

**Cause.** `CoreServices` ticks on demand and `RaiseFrameStarting` re-requested the tick
unconditionally while any driver was subscribed. No Skia dispatcher pump is vsync-throttled
(`setImmediate`/`postMessage` on WASM, `PostMessage` on Win32), so with a driver subscribed the
tick ran back to back: every pass stepped the decay by a wall-clock delta of microseconds, ran a
full `UpdateLayout`, and fed the `FrameClock` an interval so small that its phase-locked grid never
engaged — the first fling step was effectively zero and the rest sampled dispatcher jitter into the
motion. On WASM this was 4–7 layout passes per presented frame on the single thread that also
rasterises, which is the budget F1 complains about.

**Fix.** The driver tick is armed once per native frame, from the frame's UI-thread render callback
(`EnqueueRenderCallback` → `ArmFrameDriverTick`), and `RaiseFrameStarting` runs only when armed;
after raising it requests the *frame* (not the tick), so a driver that wrote nothing still gets its
next tick from the frame chain. Extra input-driven ticks between two frames now only do layout. Covered
by `Given_CompositionTarget.When_Frame_Driver_Writes_Then_Ticked_Once_Per_Frame` (206 129 ticks for
120 frames before the fix) and `When_Frame_Driver_Writes_Nothing_Then_Still_Ticked_Every_Frame`
(1 frame before the fix — the chain used to depend on the driver writing something).

**Measurement caveats.** The benchmark sample subscribes `CompositionTarget.Rendering` for its own
telemetry, which keeps the render loop presenting at the refresh rate even while nothing moves — so
"presents/s" is the refresh rate whether or not a driver is running, and only the *ratios* above are
meaningful. The counter reports on driver ticks, so its first line after an idle gap covers the whole
gap; those lines were discarded.

**Follow-ups this unblocks.** With the drivers on the presented cadence, the levers worth measuring
next are: feeding the rAF `DOMHighResTimeStamp` (currently discarded by `BrowserRenderer.ts`) into the
WASM `FrameClock` as the frame's timestamp, pointer `getCoalescedEvents` for touch on WASM (F1), and
the record-time allocations in the damage path (§3.1). None of them was measured here.

### F6 — A frame driver only ticks on a target a window owns

`CoreServices.OnTick` raises `FrameStarting` — like the ahead-of-time record opportunity, which predates
this work — only for the roots in `ApplicationHelper.WindowsInternal`. A `XamlIslandRoot` content root
(the `DesktopWindowXamlSource` hosting path) takes the branch above that loop, which does layout only, so
a driver subscribed to such a target would never tick and wheel/fling motion would not advance there.
Not exercised: island hosting on Skia has no sample or test that scrolls, and that branch already skipped
`OnRenderFrameOpportunity` before this work — recorded here rather than fixed blind.

A neighbouring hole was *partly* closed. A target whose host unregisters — a closed window — never
presents again, so a driver still attached to it can never receive the frame that would bring its next
tick, and the compositor's frame-driver count (what `Compositor.IsAnimating` reports, process-wide)
would stay raised for the rest of the session, hanging every later
`UITestHelper.WaitForIdle(waitForCompositionAnimations: true)`.
`CompositionTarget.Rendering.cs`'s `XamlRootMap.Unregistered` handler now routes to
`OnTargetUnregistered`, which drops the target's drivers via `CompositionTarget.ClearFrameDrivers`.

**The guard only runs on the hosts that unregister.** It hangs off `XamlRootMap.Unregistered`, which
only fires where a host calls `XamlRootMap.Unregister` on teardown — Headless
(`HeadlessWindowWrapper`), Win32 (`Win32WindowWrapper`) and X11 (`X11XamlRootHost`) do. Skia-Android
and AppleUIKit do not, so on those heads a closed window's target is never unregistered,
`OnTargetUnregistered` never fires, and a driver still attached at that point is not dropped. The test
that covers this, `Given_CompositionTarget.When_Window_Closed_Then_Frame_Drivers_Dropped`, runs on a
host that unregisters and so demonstrates the guard only there; it cannot demonstrate it on a host
that never reaches it. Closing that gap means making the remaining hosts unregister, which is a
separate change — check the callers of `XamlRootMap.Unregister` for the current list rather than
trusting this paragraph.

In practice the exposure is small: both scroll drivers already unsubscribe from `OnUnloaded`, so this
is defensive hardening rather than the fix for an observed hang.

## 10. Known-open questions

None of these block Tier A:

- **The `InteractionTracker` inertia handlers are still off-clock and off-thread.** Both
  `InteractionTrackerActiveInputInertiaHandler` and `InteractionTrackerPointerWheelInertiaHandler`
  start a `System.Threading.Timer` at a fixed `IntervalInMilliseconds = 17`
  (`InteractionTrackerActiveInputInertiaHandler.cs:18-19, 24, 47-48`;
  `InteractionTrackerPointerWheelInertiaHandler.cs:15-18, 54-55`), tick on a threadpool thread, mutate
  composition state from there, and time themselves off `Stopwatch.ElapsedMilliseconds` — integer
  milliseconds. The wheel handler is still constant velocity for 0.25 s with no deceleration curve
  (`:35-36`). None of A3, A5 or C1 touched this path: it is reached through `ScrollPresenter`/
  `ScrollView`, not through `ScrollContentPresenter`. It is part of C10 (§6), and it is the reason
  §6's conclusion still stands.
- Whether Uno's `ScrollView` port carries WinUI's `s_minimumVelocity` baseline-cancellation logic.
- Whether ListView containers can safely stay parented while recycled.
- The Win32 HIMETRIC coordinate assumption from the prior attempt (`GetPointerDeviceRects` has zero
  hits in the repo).
- Whether `RetainedLayer.Present`'s full-surface blit is material on mobile GPUs — a *GPU* cost,
  separable from A1's *CPU* cost.
- The WinUI and Avalonia behaviour described in §2 was read from their sources at the time and has
  not been re-verified against a current checkout; treat it as background, not as a citation.
