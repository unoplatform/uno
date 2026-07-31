# Scroll smoothness on Skia targets — design

Status: design accepted, implementation staged.
Research: `specs/scroll-smoothness/research/` (15 notes, ~16 000 lines, all citation-backed).
Read `research/00-cross-check.md` first — it adjudicates contradictions between the other notes and
corrects two of them.

---

## 1. The problem, stated correctly

The brief was "make scrolling smoother, highest possible FPS on all Skia targets". The research
changes what that means:

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
| **WinUI legacy `ScrollViewer`** | `directmanipulation.dll`, curve and duration entirely inside the OS component | DManip delegate thread | Poll `GetContentTransform` **once per tick**; mark `DirtyFlags::Independent` (bounds only, **no render walk**) |
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

- Avalonia's wheel model (50 DIP/unit, no animation) — that note rejects it itself.
- Avalonia's UI-thread `Dispatcher.InvokeAsync(Input)` fling loop — it is the pattern Uno already has
  and is trying to leave.
- A `CubicBezier` on the default programmatic scroll animation — WinUI supplies **no** easing
  function (`ScrollPresenter.cpp:3282`); inventing one is a silent parity deviation.
- **Pointer resampling as the headline fix.** Flutter ships it *off*
  (`GestureBinding.resamplingEnabled = false`, `gestures/binding.dart:610`) and it costs 38 ms of
  added latency. It is Tier C, opt-in, and only if A and B do not close the gap.
- Pointer prediction/extrapolation — absent from dxaml entirely; causes overshoot artifacts.

---

## 3. Root causes, ranked by provable impact

Full table and citations: `research/00-cross-check.md` §6. Summary:

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
  growing** accumulator (`DamageRegionExtensions.skia.cs:22`)
- `damage.UnionRect(_lastRenderBounds)` → native `CreateRectPath` alloc + another `Op(Union)` (`:33-34`)

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
`FrictionSimulation` / `ClampingScrollSimulation` transcribe directly; see
`research/07-flutter-scroll-physics.md` §8 for C#-ready code) ·
C2 least-squares velocity estimator (degree-2, 100 ms horizon, 20-sample ring, ≥3 samples, 40 ms
stopped cut-off) ·
C3 feed it every available sample — Android historical, iOS coalesced, browser
`getCoalescedEvents`, Win32 frame history (**Uno uses none today, on any backend**) ·
C4 pointer resampling, opt-in only ·
C5/C6 virtualization buffer and `GridView`/`ItemsWrapGrid` on Skia ·
C7 coalesce `ViewChanging`/`ViewChanged` to one pair per frame ·
C8 run WASM's record **inside** the rAF callback ·
C9 route Android's render enqueue through `Choreographer.postFrameCallback` ·
C10 move scroll off the UI thread entirely.

### 4.3 Physics target (Tier C1), for reference

Per-platform parity models, all closed-form, all transcribed in
`research/07-flutter-scroll-physics.md` §8:

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

The second explicit deliverable. Everything below is code-cited in the research notes.

| Platform | Issue | Cite |
|---|---|---|
| **All Skia** | O(realized-visuals) Skia-pathops damage per frame | `Visual.Damage.skia.cs:27-105` |
| **All Skia** | `_childrenPicture` cache destroyed subtree-wide per scroll frame | `Visual.skia.cs:140-146, 245-258` |
| **All Skia** | No per-frame timestamp; `TimestampInTicks` re-reads `Stopwatch` per animation | `Compositor.cs:38`; `KeyFrameEvaluator.cs:56-59` |
| **All Skia** | Touch inertia ticks *after* the record → structurally one frame late | `CompositionTarget.Rendering.skia.cs:198, 439-452` |
| **All Skia** | Touch drag quantized to ≥2 logical px | `GestureRecognizer.Manipulation.cs:33, 420-427` |
| **All Skia** | Two-point velocity estimate (first vs last sample), no fit/horizon/outlier rejection | `GestureRecognizer.Manipulation.cs:462-467` |
| **All Skia** | **No backend uses coalesced / historical / predicted pointer samples** | repo-wide; see `00-cross-check.md` G7 |
| **All Skia** | `InteractionTracker` inertia on a threadpool `Timer` @ 17 ms, mutating composition off the UI thread | `InteractionTracker*InertiaHandler.cs:16, 47-48` |
| **All Skia** | `int` wheel delta ÷ 120 ⇒ **0** for every precision device on the tracker path | `InputManager.Pointers.Managed.cs:349` |
| **All Skia** | Render slot blocked behind the full Normal dispatcher backlog | `NativeDispatcher.cs:206-217` |
| **Win32** | Pointer timestamp overflows at ~35.8 min; positions truncated to integer logical px | `Win32WindowWrapper.Pointers.cs:113-114, 124, 227` |
| **Android** | **Mouse wheel entirely dead** — no `MotionEventActions.Scroll` case | `AndroidCorePointerInputSource.cs:125-196` |
| **Android** | Touch positions truncated to integer **physical** px, then converted to DIP | `AndroidCorePointerInputSource.cs:226-229` |
| **Android** | Render record posted via `Handler.Post`, not `Choreographer` → arbitrary phase vs vsync | `NativeDispatcher.Android.cs:39-43` |
| **Android** | `MotionEvent` historical samples never read | `AndroidCorePointerInputSource.cs:71-119` |
| **iOS/macOS** | Wheel/trackpad bypasses smoothing entirely (`OperatingSystem.IsIOS()||IsMacOS()` → `DisableAnimation:true`), keyed on OS rather than on device kind | `ScrollContentPresenter.cs:311-325, 335-343` |
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
(`src/SamplesApp/SamplesApp.Samples/Windows_UI_Xaml_Controls/ScrollViewerTests/`), which reports the
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

| Item | Status | Commit |
|---|---|---|
| A1 — scroll-aware damage (bounds for moved-unchanged visuals; rect accumulator) | **done** | `perf(composition): Make scroll-frame damage O(1) per moved visual` |
| A4 — remove the 2-DIP drag quantizer | **done** | `fix(scroll): Remove drag quantizer, wheel dead zone and stale offset` |
| B1 — `OnFrame` publishes the current frame's offset | **done** | same |
| B2 — `int` wheel-delta dead zone | **done** | same |
| Measurement harness (`ScrollSmoothnessBenchmark`) | **done** | `test(scroll): Add scroll smoothness benchmark sample` |
| A2 — preserve `_childrenPicture` across a pure transform change | **not started** | — |
| A3 — one authoritative per-frame timestamp | **not started** | — |
| A5 — pre-record inertia tick | **not started** | — |
| B3–B9, Tier C | **not started** | — |

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

- **Compile**: `Uno.UI.Skia.csproj` and `SamplesApp.Skia.Generic` (Release, net10.0) — clean.
- **Runtime** (Skia Desktop / Win32): 135 tests across
  `Windows_UI_Composition`, `Windows_UI_Input`, `Given_ScrollViewer`, `ScrollViewerTests`,
  `Given_ScrollViewer_Zoom`, `ListViewTests` → **131 passed, 4 failed, 3 skipped**.
  The 4 failures are **identical to the pre-change baseline** on this machine
  (`When_Home_End_PageDown_PageUp`, `When_NonRound_Content_Height`,
  `When_Presenter_Doesnt_Take_Up_All_Space`, `When_ScrollViewer_Resized`) — all fractional-DPI
  layout assertions (e.g. expected 175, actual 175.19999), unrelated to this work.
- **Fails-before / passes-after**: `When_SlowTouchDrag_Then_ScrollAdvancesEveryMove` fails with the
  quantizer restored, reporting offsets `[30, 32, 32, 34, …]` — 3 advances over 6 one-pixel moves.
  This is the empirical confirmation of `research/00-cross-check.md` G3, which predicted exactly a
  spike at ±2 and a hole in (−2, 2).

**Not yet measured**: the A1 frame-budget win. It needs the instrumented counts described in §7 on a
device where the cost is decisive (a phone or a mobile browser), which this Win32 machine is not.

## 9. Field findings (from on-device testing)

Observed by the product owner on real hardware, not derived from the code. Each needs its own
investigation; none is addressed by what has landed so far.

### F1 — WASM in a mobile browser is still the worst case

Wheel/trackpad scrolling in a desktop browser benefits from the decay model (§4.2), but touch on a
phone browser remains poor. Consistent with the platform axis in §5: WASM serializes record + raster
on one thread and enqueues the record *outside* the rAF callback (C8), and touch inertia is still one
frame late (A5). Likely warrants its own change set rather than being folded into the scroll work,
since the fix is in the WASM host's frame scheduling, not in scrolling.

### F2 — rAF is capped at 60 Hz unless touch is active (Android browser)

On a 120 Hz Android phone, the browser runs `requestAnimationFrame` at 60 Hz while idle and boosts to
120 Hz only while a touch is in progress. **The practical consequence is that a fling runs at half
the frame rate of the drag that produced it**, with the cadence changing at the exact moment the
finger lifts.

Two implications for the design:

1. **Any motion model must be correct under a frame rate that changes mid-flight.**
   `ScrollDecaySimulation` already is — it integrates closed-form over an arbitrary interval, so a
   60 Hz frame after a run of 120 Hz frames yields the correct position rather than drifting. The
   parabolic inertia processor should be held to the same standard when it moves onto
   `Compositor.FrameStarting` (A5).
2. **Frame-rate-dependent tuning is invalid.** Any constant chosen by eye at 120 Hz will be wrong at
   60 Hz and vice versa. This is a second reason (alongside A3) not to retune physics constants
   before the clock work is complete.

Whether Uno can request a sustained high refresh rate from the browser is unverified; there is no
standard API for it, unlike native Android's `Surface.setFrameRate`.

## 10. Known-open questions

Carried from `research/00-cross-check.md` §9 — none block Tier A:

- Whether Uno's `ScrollView` port carries WinUI's `s_minimumVelocity` baseline-cancellation logic.
- Whether ListView containers can safely stay parented while recycled.
- The Win32 HIMETRIC coordinate assumption from the prior attempt (`GetPointerDeviceRects` has zero
  hits in the repo).
- Whether `RetainedLayer.Present`'s full-surface blit is material on mobile GPUs — a *GPU* cost,
  separable from A1's *CPU* cost.
- WinUI (notes 01–04) and Avalonia (notes 05–06) line citations were not independently re-verified in
  the cross-check pass and are second-hand there.
