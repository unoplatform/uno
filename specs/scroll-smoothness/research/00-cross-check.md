# Cross-check: contradictions, gaps, unverified claims, and the real bottleneck

Adjudication pass over research notes `01`–`14` in this directory. Every verdict below was
re-derived by reading the actual source in this worktree (`dev/mazi/smooth-scroll`, base
`3cff83601b`) or in `D:/Work/flutter`. Where I could not verify, the claim is marked
**UNVERIFIED** explicitly.

**Working-tree state check (matters for every "Uno current" claim):** the prior scroll attempt is
**not** in this worktree. `grep -c "_hasPendingScrollUpdate" src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs`
→ `0`; no `VelocityTracker` under `src/Uno.UI/UI/Input/`. Notes 09/10/11/13 therefore describe the
**base** pipeline, and note 12 describes a **different branch** (`dev/mazi/scroll-inertial-smoothness`).
No note conflated the two.

---

## 0. Executive verdict

Three things, up front, because they reframe the whole design:

1. **The dominant per-frame cost during a scroll is not layout, not virtualization, and not the
   physics model. It is `Visual.ContributeDamageOnPaint` running the expensive Skia *pathops*
   branch for every visual in the scrolled subtree, every frame.** A scroll makes
   `matrix != _lastRenderMatrix` true for *every* descendant, which defeats the early-out at
   `Visual.Damage.skia.cs:39` and forces ~1 `SKPathOp.Intersect` + 2 `SKPathOp.Union` + 3 `SKPath`
   allocations **per visual per frame** into a monotonically growing damage path
   (`Visual.Damage.skia.cs:27-105`, `DamageRegionExtensions.skia.cs:9-35`). No research note
   identified this. It is O(realized visuals), CPU-bound, on the UI thread, inside the record — which
   is exactly the shape that explains "Win32 fine, mobile bad".

2. **The note-14 hypothesis is directionally right but mechanistically wrong in two of its three
   legs.** Raw touch events *do* mutate `Visual.AnchorPoint` with no frame alignment (confirmed).
   But (a) they do **not** multiply frames — `RequestNewFrame()` coalesces
   (`CompositionTarget.RenderScheduling.skia.cs:86-118`); and (b) they do **not** multiply the
   composition invalidation — `SetMatrixDirty()` early-outs after the first write per frame
   (`Visual.skia.cs:140-146`). The real touch-vs-wheel asymmetry is a **2-logical-pixel motion
   quantizer** on drag (`GestureRecognizer.Manipulation.cs:33`, `:422`) plus a **guaranteed
   one-frame-late inertia tick** (`CompositionTarget.Rendering.skia.cs:198,439-452`). See §5.

3. **Flutter is not evidence for pointer resampling.** Note 08 proves
   `GestureBinding.resamplingEnabled = false` (`gestures/binding.dart:610`) with no production
   assignment anywhere in `packages/`. Flutter's default is the same raw-event-mutates-position
   model Uno has. What Flutter actually does differently is (i) presentation-time frame timestamps,
   (ii) one structurally-guaranteed animation tick per frame, (iii) the offset is a *value read once
   per frame by layout*, not a visual write per event. Any recommendation citing "Flutter's
   resampling model" as the fix is citing an opt-in that ships off.

---

## 1. Contradictions, adjudicated

### C1 — Does a scroll destroy the cached children-pictures *inside* the scrolled subtree?

| Note | Claim |
|---|---|
| 09 (uno-current) | "frees **every ancestor's** cached children-picture" |
| 10 (uno-renderloop) | "destroys **every cached `_childrenPicture` inside the scrolled subtree**" |
| 14 (firsthand) §2 | "the scrolled content's own `_childrenPicture` **survives** a scroll; only the ancestor chain's are dropped" |

**Verdict: note 10 is correct. Note 14 is wrong. Note 09 is right but incomplete.**

Note 14 reasoned only from `InvalidatePaint()`, which does call
`InvalidateParentChildrenPicture(includeSelf: false)` — an *upward* walk. It missed the recursion:

```csharp
// src/Uno.UI.Composition/Composition/Visual.skia.cs:140-146
internal virtual bool SetMatrixDirty()
{
    var matrixDirty = (_flags & VisualFlags.MatrixDirty) != 0;
    _flags |= VisualFlags.MatrixDirty;
    InvalidateParentChildrenPicture(false);
    return !matrixDirty;
}
```

```csharp
// src/Uno.UI.Composition/Composition/ContainerVisual.skia.cs:212-227
internal override bool SetMatrixDirty()
{
    if (base.SetMatrixDirty())
    {
        foreach (var child in Children.InnerList) { child.SetMatrixDirty(); }
        return true;
    }
    return false;
}
```

`InvalidateParentChildrenPicture(false)` starts at `Parent` (`Visual.skia.cs:245-258`). So when the
scroll visual `V` recurses into child `C`, `C`'s own call frees **`V._childrenPicture`** and sets
`ChildrenSKPictureInvalid` on `V`. Applied transitively, *every* `ContainerVisual` with at least one
child inside the scrolled subtree loses its cached children-picture, plus the whole ancestor chain to
the root.

The knock-on is the one all three notes agree on, and it is confirmed: `Visual.Render` resets
`_framesSinceSubtreeNotChanged = 0` whenever `ChildrenSKPictureInvalid` was set
(`Visual.skia.cs:388-398`), and `RenderChildrenStep` requires
`_framesSinceSubtreeNotChanged >= PictureCollapsingOptimizationFrameThreshold` (50,
`Visual.skia.cs:40`) **and** `GetSubTreeVisualCount() >= 100` (`:41`) to collapse
(`Visual.skia.cs:531-544`). **The picture-collapsing optimization can never engage inside a scrolling
subtree.**

### C2 — Is the invalidation cascade O(subtree) *per event* or *per frame*?

Notes 09, 10 and 13 all phrase it per-delta ("one AnchorPoint write per scroll frame therefore
dirties every visual under the scroller", note 13; "O(subtree) per scroll delta", note 14 §3).

**Verdict: per *frame*, not per event.** `Visual.SetMatrixDirty()` returns `!matrixDirty`
(`Visual.skia.cs:140-146`) and `ContainerVisual` recurses **only when that is true**
(`ContainerVisual.skia.cs:214`). The `TotalMatrix` getter clears `MatrixDirty` when read
(`Visual.skia.cs:157-162`), which happens for every rendered visual during the paint walk. So:

- 1st `AnchorPoint` write after a render → full O(subtree) flag walk + O(subtree)
  children-picture frees.
- 2nd..Nth write in the same frame → `base.SetMatrixDirty()` returns `false`, no recursion; and
  `InvalidateParentChildrenPicture` early-outs at the first already-flagged ancestor
  (`Visual.skia.cs:248`). Effectively O(1).

This matters: it removes "raw touch event storms multiply composition invalidation" from the causal
chain. The composition cost is per-frame and is paid identically by wheel scrolling.

### C3 — Does a scroll frame re-*record* every visual's picture?

Note 09: "the entire realized subtree is re-walked **and re-emitted**". Note 10 rec 10: "Avoid
re-recording a visual's own picture when only its transform changed".

**Verdict: only the visual whose property changed is re-recorded; descendants are replayed.**
`Compositor.InvalidateRenderPartial(visual)` calls `InvalidatePaint()` on **that one visual**
(`Compositor.skia.cs:258-263`); descendants receive only `SetMatrixDirty` via the recursion. In the
paint step, `contentChanged = (visual._flags & VisualFlags.PaintDirty) != 0`
(`Visual.skia.cs:487`) — false for descendants, so their `_picture` is drawn with
`sk_canvas_draw_picture`, not re-recorded.

Note 10's rec 10 is still valid but its blast radius is **one visual**, not N. Note 09's "re-emitted"
is a walk + draw-picture, not a re-record. This down-ranks both relative to C1/G1.

### C4 — "Align pointer input to the frame clock (Flutter's model)"

Note 14's leading hypothesis and its ranked fix #1.

**Verdict: the *problem statement* is confirmed; the *cited model* is refuted.**

- Confirmed: `GestureRecognizer.Manipulation.Update` → `NotifyUpdate()` → `ManipulationUpdated`
  (`GestureRecognizer.Manipulation.cs:231-251`, `:420-427`) → `ScrollContentPresenter.OnUpdated`
  → `Set(..., DisableAnimation: true, IsTouch: true, IsIntermediate: true)`
  (`ScrollContentPresenter.Managed.cs:645-655`) → `visual.AnchorPoint = target`
  (`ScrollContentPresenter.Managed.cs:467`). No resampling, no coalescing, no frame alignment.
- Refuted as the Flutter model: `packages/flutter/lib/src/gestures/binding.dart:610`
  `bool resamplingEnabled = false;`. Note 08 §1.4 grepped `packages/` and found production
  assignments **only in tests**. It also applies to `PointerDeviceKind.touch` only
  (`gestures/binding.dart:100`). Flutter's default drag path mutates `ScrollPosition.pixels` on the
  raw pointer event and lets the *next frame* read it.

The transferable Flutter mechanisms are different ones, and note 08 states them:
presentation-time timestamps (`platform_configuration.cc:469`), the transient-callback swap-out that
guarantees one tick per frame (`scheduler/binding.dart:1260`), and the
`assert(schedulerPhase != persistentCallbacks)` on `setPixels` (`scroll_position.dart:367-371`).

### C5 — "Adopt one-work-item-per-dispatcher-callback interleave" (note 04, rec 2, `impact: high`)

**Verdict: already implemented in Uno. This recommendation is a no-op.**

```csharp
// src/Uno.UI.Dispatching/Native/NativeDispatcher.cs:128-177
private static void DispatchItems()
{
    Action? action = @this.TryGetRenderAction();
    if (action is null) { /* dequeue exactly ONE item from the highest non-empty queue */ }
    ...
    if (Interlocked.Decrement(ref @this._globalCount) > 0) { @this.EnqueueNative(@this._currentPriority); }
    RunAction(@this, action);
}
```

One item per native callback, then re-post. On Android `EnqueueNative` is `_handler.Post(_implementor)`
(`NativeDispatcher.Android.cs:40-43`), i.e. one Looper message per item — exactly the interleave
`xcpwindow.cpp:1246-1257` describes. Note 04's rec 2 rests on an unverified assumption about Uno.

### C6 — "Uno hard-codes a 60 Hz frame rate" (note 10, rec 15)

**Verdict: calibration needed.** `FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate`
defaults to **`true`** (`src/Uno.UI/FeatureConfiguration.cs:125`); `FrameRate = 60` (`:118`) is the
fallback when the refresh rate can't be read. The genuinely hard-coded 60s are
`NativeDispatcher.Android.cs:25-29` (`MaxRenderSpan`) and `DispatcherAnimator.skia.cs` — both off the
scroll critical path (`MaxRenderSpan` only bounds the Choreographer `RunAnimation` queue, which the
render record does **not** use).

### C7 — "offset change → EVP → layout → `CanRecordPicture` false → frame dropped" (note 12)

**Verdict: overstated.** `CanRecordPicture` gates **only** the *early* record opportunity:

```csharp
// src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs:178-208
internal void OnRenderFrameOpportunity()
{
    if (SkiaRenderHelper.CanRecordPicture(ContentRoot.VisualTree.RootElement)) { ... Render(); }
}
```

`EnqueueRenderCallback` (`:120-157`) calls `Render()` **unconditionally** when `RenderRequested`. A
measure-dirty root loses the *early* record (which is a latency optimization), it does not lose the
frame. `SkiaRenderHelper.skia.cs:33-34` confirms the predicate is purely
`{ IsArrangeDirtyOrArrangeDirtyPath: false, IsMeasureDirtyOrMeasureDirtyPath: false }`.

### C8 — Is ListView realization synchronous with the scroll delta?

Note 09: "ListView realization is fully synchronous with the delta, re-arranging every materialized
line." Note 13: "driven by `ScrollViewer.ViewChanged` on a coalesced Normal-priority dispatcher item
and does zero root layout passes in steady state."

**Verdict: note 13 is correct for the default configuration.**

```csharp
// src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs:1239-1244
if (isIntermediate && UpdatesMode != Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous)
{
    RequestUpdate();          // :1301 → Dispatcher.RunAsync(Normal, …) guarded by _hasPendingUpdate
    _snapPointsTimer?.Stop();
}
```

`UpdatesMode` comes from `FeatureConfiguration.ScrollViewer.DefaultUpdatesMode = AsynchronousIdle`
(`FeatureConfiguration.cs:483`, read at `ScrollViewer.cs:105`). The only code that forces
`Synchronous` is `ScrollViewer.SetDirectManipulationStateChangeHandler`
(`ScrollViewer.MuxInternal.cs:74`), whose only callers are `CalendarView_Partial.cs:851,2039` and
`CalendarViewGeneratorHost.cs:256`. ListView subscribes `ScrollViewer.ViewChanged`
(`VirtualizingPanelLayout.managed.cs:211`), which is raised from `Update()` — i.e. once per
coalesced dispatcher turn.

Note 09's claim holds only under `ScrollViewerUpdatesMode.Synchronous` (CalendarView, or opt-in).

### C9 — "Every scroll frame pays a full layout pass to the root because of the ScrollBar" (note 09, rec 5)

**Verdict: real mechanism, overstated cost.** The mechanism is confirmed:
`ScrollBar.OnValueChanged → UpdateTrackLayout` (`ScrollBar.mux.cs:729-733`) writes
`LargeDecrease.Width/Height` (`:1032-1042`) and `PanningRoot.Margin` (`:1044-1056`), both
`AffectsMeasure`.

But two calibrations: (1) the write happens once per *published* offset change, which for
intermediate scrolls is once per coalesced dispatcher turn (C8), not per pointer event; (2) Uno's
`InvalidateMeasure` uses a **MeasureDirtyPath**: an O(depth) upward walk
(`UIElement.Layout.crossruntime.cs:26-77`), and layout descends only the dirty path. It is a
dirty-path layout pass, not an O(tree) measure. Still worth removing, but it is not the frame killer
note 09 implies.

### C10 — Touch inertia tick source

Note 02 rec 3 says Uno "integrates inertia on the UI thread … `IInertiaProcessorTimer`", implying a
timer. **Calibration:** the *default* is frame-driven, not timer-driven.
`WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation` and
`…ForUiElement` both default to `true`
(`src/Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.GestureRecognizer.cs:66-77`), selecting
`CompositionInertiaProcessorTimer`, which hooks `CompositionTarget.Rendering`
(`GestureRecognizer.Manipulation.InertiaProcessor.cs:333-352`). The 30 FPS
`DispatcherInertiaProcessorTimer` (`:312-330`) is the fallback and is also the *only* option outside
`IS_UNO_UI_PROJECT` (`:193-199`).

The defect is not "it's a timer" — it is **when** in the frame it runs (see G4) and that it samples
`Stopwatch.Elapsed` rather than a frame timestamp, with an in-source comment saying exactly why
(`:344-345`: "*we are not using the `RenderingEventArgs.RenderingTime` as we are not able to have the
value at t0*").

### C11 — Two incompatible mouse-wheel models coexist in Uno (not a note contradiction; a code defect)

Notes 03, 09, 11 and 14 each describe *one* of them and none says they are simultaneously live:

| Path | Delta model | Motion model |
|---|---|---|
| `ScrollContentPresenter.PointerWheelScroll` | `min(extent, round(delta * max(48, round(0.15·extent)) / 120))` — `ScrollContentPresenter.mux.cs:18-27` | 1 s `Vector2KeyFrameAnimation` on `AnchorPoint`, `Power(Out, 10)` — `ScrollContentPresenter.Managed.cs:474-479` |
| `InteractionTracker` (ScrollView / ItemsView) | `(int)(MouseWheelDelta / 120) * 48` — `InputManager.Pointers.Managed.cs:349`, `InteractionTracker.cs:137-145` | constant velocity for exactly 0.25 s on a 17 ms threadpool `Timer` — `InteractionTrackerPointerWheelInertiaHandler.cs:16,36,66-73` |

And they are mutually exclusive per element: `InputManager.Pointers.Managed.cs:340-355` walks the
visual ancestors and `return`s as soon as it finds a `VisualInteractionSource { RedirectsPointerWheel: true }`,
so the routed `PointerWheelChanged` never reaches `ScrollContentPresenter` on those elements.

---

## 2. Calibrations on individual recommendations

| Note / rec | Status |
|---|---|
| 04 rec 2 — one-item-per-dispatcher-callback | **Already implemented** (C5) |
| 04 rec 8 — normalize wheel from raw deltas | Correct target, but note the trailing `Math.Round` in `ScrollContentPresenter.mux.cs:25-27` is a *second* quantizer on top of the `int MouseWheelDelta` quantizer at the source (`PointerPointProperties.cs:254`). Fixing the formula without widening the type fixes nothing. |
| 09 rec 1 / 10 rec 1 — split `InvalidateRenderPartial` | Correct and high value, but the win is **the children-picture cascade (C1)**, not `InvalidatePaint` (C3). Phrase the fix as "a transform-only change must not free descendants' `_childrenPicture`", i.e. change `ContainerVisual.SetMatrixDirty` to not route through `InvalidateParentChildrenPicture`. |
| 09 rec 5 — ScrollBar forces layout | Real, but dirty-path not full-tree, and once per dispatcher turn not per event (C9) |
| 09 "ListView realization synchronous" | Wrong for default config (C8) |
| 10 rec 15 — hard-coded 60 Hz | Partially wrong (C6) |
| 10 rec 10 — don't re-record on transform change | Affects 1 visual, not N (C3) |
| 14 §2 — children-picture survives | Wrong (C1) |
| 14 fix #1 — resample to frame clock, "Flutter's model" | Problem real, model citation wrong (C4) |
| 02 rec 3 — move inertia off the UI-thread timer | Default is already frame-hooked; the defect is phase + clock source (C10, G4) |
| 12 — "frame dropped because `CanRecordPicture` false" | Overstated (C7) |

---

## 3. Gaps: questions critical to the design that no note answered — answered here

### G1 — What actually dominates the per-frame CPU cost during a scroll? **The damage region.**

This is the biggest finding of the cross-check and it is absent from all 14 notes.

```csharp
// src/Uno.UI.Composition/Composition/Visual.Damage.skia.cs:27-68
private void ContributeDamageOnPaint(bool contentChanged, SKPath? damage, SKPath clip)
{
    if (damage is null) { return; }
    var matrix = TotalMatrix;
    var moved = !_hasLastRenderBounds || matrix != _lastRenderMatrix;      // :35
    var shadowSilhouetteChanged = ShadowState is not null && _subtreeChangedThisFrame;
    if (!contentChanged && !moved && !shadowSilhouetteChanged) { return; } // :39  <-- the early-out

    if (TryGetPaintDamageRegion(clip, out var bounds, out var regionPath))
    {
        if (regionPath is not null) { damage.Union(regionPath); ... }      // :47
        else                       { damage.UnionRect(bounds); }
        if (_hasLastRenderBounds && (matrix != _lastRenderMatrix || bounds != _lastRenderBounds))
        {
            damage.UnionRect(_lastRenderBounds);                           // :57
        }
        ...
    }
}
```

**During a scroll, `matrix != _lastRenderMatrix` is true for every visual in the scrolled subtree**,
so the early-out at `:39` never fires and *both* union branches run. What each of those calls costs:

```csharp
// src/Uno.UI.Composition/Composition/DamageRegionExtensions.skia.cs:9-35
public static void Union(this SKPath region, SKPath addition)
{
    if (addition.IsEmpty) { return; }
    if (region.IsEmpty) { addition.Transform(SKMatrix.Identity, region); }
    else { region.Op(addition, SKPathOp.Union, region); }        // :22  full Skia pathops boolean
}

public static void UnionRect(this SKPath region, SKRect rect)
{
    ...
    using var scratch = SkiaExtensions.CreateRectPath(rect);     // :33  native SKPath alloc
    region.Union(scratch);                                       // :34  → another Op(Union)
}
```

and `TryGetPaintDamageRegion` (`Visual.Damage.skia.cs:71-105`) itself allocates two pooled `SKPath`s,
transforms the content path by `TotalMatrix`, outsets for AA and runs
`contentPath.Op(clipPath, SKPathOp.Intersect, contentPath)`.

**Per visual, per frame, during a scroll:** ≈ 1 `Op(Intersect)` + 2 `Op(Union)` + 1 native
`CreateRectPath` allocation + 2 pooled path transforms — all `SkPathOps`, all on the UI thread,
inside `Render()`, accumulating into a `region` path that grows monotonically across the walk
(making each subsequent `Op` more expensive).

Three consequences:

1. Cost is **O(realized visuals)**, independent of viewport size or scroll speed.
2. The result is **degenerate**: when everything in the subtree moved, the union *is* the scroll
   port's clip rect. All that work produces an answer that could be computed in O(1).
3. It is a pure CPU cost, so it scales inversely with CPU speed → this is the mechanism that best
   explains the reported platform ordering (Win32 ≫ Android/iOS ≫ mobile WASM) for the *same*
   content and the *same* input model.

**Intervention (new, top-ranked):** add a scroll fast path — when a frame's only invalidation is an
ancestor transform, set `damage = oldScrollPortRect ∪ newScrollPortRect` (clamped to the frame) and
skip `ContributeDamageOnPaint` for the subtree entirely. Descendants still need `_lastRenderBounds`/
`_lastRenderMatrix` refreshed, which is two field writes, not a path op.

### G2 — Is there a single per-frame timestamp anywhere in Uno Skia? **No.**

```csharp
// src/Uno.UI.Composition/Composition/Compositor.cs:38
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));
```

```csharp
// src/Uno.UI.Composition/Composition/KeyFrameAnimations/KeyFrameEvaluator.cs:56-59
var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
var elapsed = new TimeSpan(nowTimestamp - _totalPause - _startTimestamp);
```

`TimestampInTicks` is a *property* that re-reads `Stopwatch.GetTimestamp()` on every access, and
`Compositor.RenderRootVisual` calls `animation.RaiseAnimationFrame()` in a loop
(`Compositor.skia.cs:206-220`). So **two animations in the same frame observe two different
timestamps**, and none of them observes the presentation time. This is the exact opposite of WinUI's
`RefreshAlignedClock` (one timestamp per tick, note 04) and Flutter's `currentFrameTimeStamp`
(note 07 §5.3, note 08 §3.2), both of which are cited as *the* structural reason those stacks look
smooth under load.

`RenderingEventArgs` has the same defect at the other end:
`new RenderingEventArgs(Stopwatch.GetElapsedTime(_start), frameData)`
(`CompositionTarget.Rendering.skia.cs:475`) — sampled at *raise* time, inside a dispatcher
continuation. Note 12's finding that the "VSync-aligned RenderingTime" justification is factually
wrong is **confirmed**.

### G3 — Why does *dragging* stutter specifically? **A 2-logical-pixel motion quantizer.**

```csharp
// src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.cs:33-35
internal static readonly Thresholds DeltaTouch = new() { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
internal static readonly Thresholds DeltaPen   = new() { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
internal static readonly Thresholds DeltaMouse = new() { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };
```

```csharp
// src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.cs:420-427
case ManipulationStatus.Started when changeSet.Delta.IsSignificant(_deltaThresholds):
case ManipulationStatus.Inertia: // No IsSignificant check for inertia, we prefer smooth animations!
    CommitChanges(changeSet);
    _recognizer.ManipulationUpdated?.Invoke(...);
```

```csharp
// src/Uno.UI/UI/Input/WinRT/ManipulationDelta.cs:54-58
internal bool IsSignificant(Thresholds t)
    => Math.Abs(Translation.X) >= t.TranslateX || Math.Abs(Translation.Y) >= t.TranslateY
    || Math.Abs(Rotation) >= t.Rotate || Math.Abs(Expansion) >= t.Expansion;
```

Touch selects `DeltaTouch` at `GestureRecognizer.Manipulation.cs:154-160`. `changeSet.Delta` is the
delta **since the last commit**, so motion is not lost — it is **quantized**: the content does not
move at all until ≥2 logical px have accumulated, then jumps the whole accumulated amount. A finger
moving at 60 DIP/s at 60 fps produces 1 DIP/frame → the view advances **every other frame, by 2 DIP**.
That is textbook visible stutter, and it is worst at the slow speeds where users notice most.

Note that inertia is explicitly exempt ("*we prefer smooth animations!*") — so the drag stutters and
the fling that follows is continuous, which is exactly the "sticky then smooth" complaint shape.

Mouse (`DeltaMouse = 1`) is half as bad, and the **mouse wheel does not go through this path at all**
(`ScrollContentPresenter.PointerWheelScroll`, `ScrollContentPresenter.cs:245-358`). This is a genuine
input-device asymmetry that maps precisely onto "Win32 with a wheel feels fine".

*(Note 12 records that the prior attempt zeroed these thresholds and that its reviewer objected on
event-volume grounds. The objection is about `ManipulationDelta` event volume for app handlers, not
about the scroll path; the correct fix is to decouple "advance the scroll" from "raise a public
`ManipulationDelta`", which is exactly what note 12's rec 11 proposes.)*

### G4 — Where in the frame does touch inertia run? **After the picture is recorded → one frame late.**

```csharp
// src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs
Render() { ... RecordPictureAndReturnPath(...);  /* :119 */  ... OnFramePictureRecorded(this, framePicture); /* :198 */ }

private static void OnFramePictureRecorded(...)      // :439
{
    ...
    if (_isRenderingActive && !_renderingRaiseScheduled)
    {
        _renderingRaiseScheduled = true;
        NativeDispatcher.Main.Enqueue(RaiseRendering, NativeDispatcherPriority.High);   // :451
    }
}
```

`CompositionInertiaProcessorTimer` subscribes `CompositionTarget.Rendering`
(`GestureRecognizer.Manipulation.InertiaProcessor.cs:346`). So the inertia tick for frame *N* runs
*after* frame *N*'s picture exists, and its `visual.AnchorPoint` write lands in frame *N+1*.
Structural, unavoidable one-frame latency on every touch fling — on top of the record→present
latency, which on WASM is another frame (§5).

By contrast the composition animation tick (used by wheel) runs **inside** the record, before the
paint walk (`Compositor.skia.cs:206-220`, called from `SkiaRenderHelper.RecordPictureAndReturnPath:44`),
so a wheel-scroll value written this frame is *in* this frame's picture.

### G5 — Is there a pre-record, once-per-frame hook to build a frame-clocked scroll driver on?

`Render()` (`CompositionTarget.Rendering.skia.cs:110`) goes straight into
`SkiaRenderHelper.RecordPictureAndReturnPath` (`:119`), which calls
`rootVisual.Compositor.RenderRootVisual` (`SkiaRenderHelper.skia.cs:44`), whose *first* action is the
animation loop (`Compositor.skia.cs:206`).

**The animation loop in `Compositor.RenderRootVisual` is the only existing pre-record,
once-per-frame hook in Uno Skia.** Everything else — `CompositionTarget.Rendering`,
`CoreServices.OnTick`, `ScrollViewer.RequestUpdate` — is either post-record or a separate dispatcher
item. Any frame-clocked scroll driver must either live there or require a new `PreRender` phase at
`Render()` entry. This is the concrete insertion point the design needs and no note named it.

### G6 — `OnFrame` reads a stale `AnchorPoint`. **Confirmed, one-line fix.**

```csharp
// src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs
scrollAnimation.AnimationFrame += OnFrame;                                  // :493
scrollAnimation.Stopped       += OnStopped;                                 // :494
visual.StartAnimation(nameof(Visual.AnchorPoint), scrollAnimation);         // :496
```

```csharp
// src/Uno.UI.Composition/Composition/CompositionObject.cs:90-98
if (_animations?.ContainsKey(propertyName) == true) { StopAnimation(propertyName); }   // :92
_animations[propertyName] = animation;
animation.AnimationFrame += ReEvaluateAnimation;                                        // :97
```

Delegate invocation is in subscription order, so `OnFrame` (which reads `visual.AnchorPoint`,
`ScrollContentPresenter.Managed.cs:490-491`) runs **before** `ReEvaluateAnimation` writes it.
Every downstream consumer — `ScrollViewer.VerticalOffset`, `ViewChanged`, ListView realization,
EffectiveViewport — targets the previous frame's viewport. Note 09 rec 4 confirmed.

The same `:92` `StopAnimation` also explains note 09's "ViewChanged storm": restarting the animation
on the next wheel detent stops the previous one, which fires `KeyFrameAnimation.Stopped`
(`KeyFrameAnimation.cs:60-64`) → `OnStopped` → `Updated(..., isIntermediate: false)` → a
non-intermediate `Update()` → `InvalidateArrange()` (`ScrollViewer.cs:1327-1334`). Confirmed.

### G7 — Does *any* Uno backend use coalesced / historical / predicted pointer samples? **No, anywhere.**

Repo-wide grep for `getCoalescedEvents`, `GetCoalescedTouches`, `GetPredictedTouches`,
`getHistoricalX`, `Axis.Vscroll`, `GetPointerFrameInfoHistory` → zero production hits.
Spot-verified at the three sites that matter:

- iOS: `AppleUIKitPointerInputSource.TouchesMoved(UIView, NSSet, UIEvent? evt)` takes `evt` and never
  reads it; it loops `foreach (UITouch touch in touches)` and raises one `PointerMoved` per touch.
- Android: `AndroidCorePointerInputSource.ToManaged` reads only `nativeArgs.GetX(pointerIndex)` /
  `GetY(pointerIndex)` and truncates: `new Point((int)x - correction[0], (int)y - correction[1]).PhysicalToLogicalPixels()`.
  There is **no** `MotionEventActions.Scroll` case in `OnNativeMotionEvent` — it falls into the
  `default:` warning branch (`AndroidCorePointerInputSource.cs:190-195`). **Mouse wheel is entirely
  dead on Skia-Android.**
- Uno's own manipulation entry point always passes exactly one point:
  `_recognizer.ProcessMoveEvents([args.CurrentPoint])`
  (`src/Uno.UI/UI/Xaml/Internal/DirectManipulation.cs:240`, `:261`) — a one-element array allocated
  per pointer move.

Note 11's claims here are **confirmed** and can be treated as fact.

### G8 — The `int` wheel-delta dead zone. **Confirmed, total functional failure on the tracker path.**

```csharp
// src/Uno.UI/UI/Xaml/Internal/InputManager.Pointers.Managed.cs:349
tracker.ReceivePointerWheel(args.CurrentPoint.Properties.MouseWheelDelta / ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta, ...);
```

`MouseWheelDelta` is `public int` (`PointerPointProperties.cs:254`);
`ScrollViewerDefaultMouseWheelDelta` is `internal const int = 120`
(`ScrollContentPresenter.mux.cs:18`). C# integer division ⇒ **any |delta| < 120 yields exactly 0**,
and `InteractionTracker.ReceivePointerWheel` then computes `delta = 0 * 48 = 0`
(`InteractionTracker.cs:137-145`). Every precision-touchpad event, every macOS trackpad event, every
fractional browser wheel event produces zero scroll in `ScrollView` / `ItemsView` on Skia.

### G9 — Does scroll bookkeeping delay the next frame? **Yes, by the full Normal backlog.**

```csharp
// src/Uno.UI.Dispatching/Native/NativeDispatcher.cs:206-217
private Action? TryGetRenderAction()
{
    ... if (details.normalItemsToProcessBeforeNextRenderAction == 0)
        {
            _compositionTargets[compositionTarget] =
                (renderAction: null, normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
            ...
        }
}
```

The next render action is blocked until that many Normal items have run — and the Normal queue is
where scroll bookkeeping lives: `ScrollViewer.RequestUpdate` (`ScrollViewer.cs:1301-1311`,
`CoreDispatcherPriority.Normal`), `CoreServices.RequestAdditionalFrame`
(`CoreServices.cs:67-75`, `NativeDispatcherPriority.Normal`) which is what drains the
`EffectiveViewportChanged` queue, and `InteractionTracker.SetPosition`
(`InteractionTracker.cs:62-74`, `NativeDispatcher.Main.Enqueue` default priority). Confirmed.

### G10 — Where does per-event work actually still land, given C2?

With the composition cascade amortized to O(1) per event (C2) and ListView realization coalesced
(C8), the residual **per-pointer-event** cost is:

1. Full visual-tree hit test + `PointerRoutedEventArgs` allocation + routed-event bubble
   (`InputManager.Pointers.Managed.cs`, `OnPointerMoved`).
2. `GestureRecognizer` `StageChanges` (two-point velocity estimate, `GestureRecognizer.Manipulation.cs:462-467`).
3. `ScrollContentPresenter.Set` → `Update` → `visual.StopAnimation(AnchorPoint)` +
   `visual.StopAnimation(Scale)` + `visual.AnchorPoint = target` + `visual.Scale = targetScale`
   (`ScrollContentPresenter.Managed.cs:463-469`) — four `SetProperty` calls, each routing through
   `Compositor.InvalidateRenderPartial`.
4. `Updated()` → `UpdateOffsets` → `PropagateEffectiveViewportChange()`
   (`ScrollContentPresenter.Managed.cs:398-410`, `FrameworkElement.EffectiveViewport.cs:256-266`),
   a **synchronous** walk over `_childrenInterestedInViewportUpdates`, per event — even though the
   resulting `EffectiveViewportChanged` events are queued
   (`EventManager.cs:28-34`, drained in `CoreServices.OnTick`).

Item 4 is the only structurally unbounded one, and it is bounded in practice by the number of
EVP-interested descendants (nested scroll ports, `ItemsRepeater`s), not by realized item count.
**Conclusion: per-event work is real but modest; it is not the FPS limiter.** Per-frame work is.

---

## 4. Recommendations resting on unverified assumptions

Flagged so the design does not build on them.

| # | Rec | Unverified assumption | What would settle it |
|---|---|---|---|
| U1 | 04 rec 2 (one-item interleave) | that Uno drains the dispatcher queue | **Settled — false** (C5) |
| U2 | 10 rec 15 (hard-coded 60 Hz) | that `SetFrameRateAsScreenRefreshRate` is off | **Settled — it defaults true** (C6) |
| U3 | 01 rec 3 (freeze arrange to pre-manipulation offset) | that Uno's arrange currently moves scrolled pixels | Uno moves content via `Visual.AnchorPoint` (`ScrollContentPresenter.Managed.cs:467`), so arrange already does not relocate scroll pixels. The rec's *value* is preventing a mid-scroll re-arrange from fighting the visual — worth doing, but the stated failure mode needs a concrete repro before it is prioritized. |
| U4 | 02 rec 8 / 01 rec 8 (predicted present time) | that Uno can obtain a presentation timestamp on all backends | Verified available in principle (CADisplayLink `TargetTimestamp`, Choreographer `frameTimeNanos`, rAF `DOMHighResTimeStamp`, DWM timing) but **no Uno code plumbs any of them today** — `Win32RenderPacer` uses `DwmFlush()` as a *blocking wait*, not as a timestamp source. Treat as new work, not a wiring change. |
| U5 | 12 (Win32 `ptHimetricLocation`) | that HIMETRIC shares an origin with `ptPixelLocation` and that 1 logical px == 1/96 in | Still **UNVERIFIED**; `GetPointerDeviceRects` has zero hits in the repo. Note 12's own risk rating (`high`) stands. |
| U6 | 03 rec 4 (`s_minimumVelocity` baseline cancellation) | that Uno's `ScrollView` port carries the `ScrollView.cpp:2383-2455` logic | Not checked in this pass. **UNVERIFIED.** |
| U7 | 13 rec 2 (keep recycled containers parented) | that keeping them in `Panel.Children` does not break measure/arrange or hit-testing | Not checked. Note 13 rates risk `high`; agreed. **UNVERIFIED.** |
| U8 | 05/06 (Avalonia) — all | Avalonia sources were not re-read in this pass. Notes 05/06 are internally consistent and heavily cited; taken as reported, but every Avalonia line number here is **second-hand**. |
| U9 | 01/02/03/04 (WinUI) — all | WinUI sources were not re-read in this pass. Same caveat: line numbers are second-hand. The one WinUI claim I *did* need — that WinUI moves scroll pixels off the XAML UI thread — is asserted consistently by four independent notes with non-overlapping citations, so I treat it as established. |

---

## 5. The product-owner question: why Android / WASM / iOS feel worse than Win32

**Hypothesis under test (note 14 §6):** "raw touch/pointer events mutate the visual directly with no
alignment to the frame clock, whereas Win32 is mostly exercised via mouse wheel, which goes through
an animation and is therefore implicitly frame-aligned."

**Verdict: confirmed as an observation, refuted as a complete explanation. It conflates a
device-type asymmetry with a platform asymmetry, and the device-type asymmetry has a different
proximate cause than 'frame alignment'.**

### 5.1 What is confirmed

- **Touch mutates the visual per raw event.** `DirectManipulation.ProcessMove` →
  `_recognizer.ProcessMoveEvents([args.CurrentPoint])` (`DirectManipulation.cs:240,261`) →
  `Manipulation.Update` → `NotifyUpdate` → `ManipulationUpdated`
  (`GestureRecognizer.Manipulation.cs:231-251, 420-427`) →
  `IDirectManipulationHandler.OnUpdated` → `Set(..., DisableAnimation:true, IsTouch:true)`
  (`ScrollContentPresenter.Managed.cs:645-655`) → `visual.AnchorPoint = target`
  (`:467`). No resampling, no coalescing, no frame clock. ✔
- **Wheel goes through a composition animation** on non-Apple platforms
  (`ScrollContentPresenter.cs:346-348` → `ScrollContentPresenter.Managed.cs:471-497`), and that
  animation is evaluated **exactly once per recorded frame, inside the record, before the paint
  walk** (`Compositor.skia.cs:206-220`). So wheel motion is frame-aligned by construction. ✔
- **Win32 is the only Skia target where the wheel is the primary scroll device**, and Skia-Android
  has no wheel path at all (G7). ✔

### 5.2 What is refuted

- **"Raw events cause extra frames."** No: `RequestNewFrame()` is idempotent within a frame
  (`CompositionTarget.RenderScheduling.skia.cs:93-101`). N events between two records produce one
  record.
- **"Raw events multiply the invalidation cost."** No: C2 — the O(subtree) cascade early-outs after
  the first write per frame.
- **"Flutter proves resampling is the answer."** No: C4 — Flutter's resampler is opt-in and off.
- **"It is the frame source."** No: Win32 (`DwmFlush`), Android (`GLSurfaceView` GL thread,
  `RenderMode.WhenDirty`, `UnoSKCanvasView.cs:53,62-66`), iOS (`CADisplayLink`) and WASM (`rAF`) are
  all legitimate vsync sources. Note 14 §8 is correct on this.
- **"Android is single-threaded like WASM."** No: Android's `OnDrawFrame` runs on the
  `GLSurfaceView` GL thread (`UnoSKCanvasView.cs:145-160`), so record and raster do overlap, as on
  Win32.

### 5.3 What actually differs — decomposed into device and platform

**Device axis (touch vs wheel), applies on every platform including Win32:**

| # | Mechanism | Cite |
|---|---|---|
| D1 | **Drag motion is quantized to ≥2 logical px.** `DeltaTouch.TranslateX/Y = 2`; `IsSignificant` is `>=`, OR across axes; no such gate in the `Inertia` case. | `GestureRecognizer.Manipulation.cs:33, 154-160, 420-427`; `ManipulationDelta.cs:54-58` |
| D2 | **Inertia is exactly one frame late.** `CompositionTarget.Rendering` is raised from a High-priority dispatcher item enqueued *after* the picture was recorded. | `CompositionTarget.Rendering.skia.cs:198, 439-452`; `…InertiaProcessor.cs:333-352` |
| D3 | **Velocity is a two-point first-vs-last estimate** over the rolling history, no fit, no horizon, no outlier rejection — so the fling's initial speed is noisy. | `GestureRecognizer.Manipulation.cs:462-467` |
| D4 | **Inertia physics is parabolic constant-deceleration** with per-platform magic constants, matching neither Android's `OverScroller` spline nor iOS's exponential model. | `…InertiaProcessor.cs:268-276`; `ScrollContentPresenter.Managed.cs:711-744` |
| D5 | Touch position precision is lost at the source on Android (`(int)x` in **physical** px, so 1/density DIP) and on Win32 (`(int)(x/scale)` in **logical** px, so 1 DIP). | `AndroidCorePointerInputSource.cs:226-229`; `Win32WindowWrapper.Pointers.cs:113-114` |

D1 is the one that best matches the subjective report: *a slow drag advances in 2-DIP steps while the
fling that follows is continuous.* On a 3×-density phone that is 6 physical pixels of dead-band per
step.

**Platform axis (same input model, different cost/latency):**

| # | Mechanism | Cite |
|---|---|---|
| P1 | **The per-frame damage cost (G1) is CPU-bound and O(realized visuals).** Mobile CPUs are 3–10× slower than a desktop, so the same content blows the budget on Android/iOS/WASM and fits on Win32. | `Visual.Damage.skia.cs:27-105`; `DamageRegionExtensions.skia.cs:9-35` |
| P2 | **WASM serializes record + raster on one thread.** `BrowserRenderer.RenderFrame` is the rAF callback; it calls `OnNativePlatformFrameRequested`, which *enqueues* the next record to the same-thread dispatcher and *then* draws the previous picture. Budget is `record + raster`, not `max(record, raster)`; finger-to-photon is ≥2 frames. | `BrowserRenderer.cs:65-115`; `CompositionTarget.RenderScheduling.skia.cs:166-176` |
| P3 | **Android's render record lands at an arbitrary phase.** `NativeDispatcher.EnqueueNative` is `_handler.Post(_implementor)` — a plain main-Looper post. A Choreographer path exists (`RunAnimation`/`PostFrameCallback`) but the render record does not use it. | `NativeDispatcher.Android.cs:39-43, 49-59` |
| P4 | **Android has no wheel at all**, so it can never fall back to the smooth path. | `AndroidCorePointerInputSource.cs:125-196` (no `MotionEventActions.Scroll` case) |
| P5 | **Apple platforms bypass the wheel animation entirely** (`OperatingSystem.IsIOS() \|\| IsMacOS()` → `DisableAnimation:true`), so macOS/iOS trackpad scroll gets no smoothing from the framework — only whatever the OS pre-smooths. | `ScrollContentPresenter.cs:311-325, 335-343` |
| P6 | **Mobile rasterizes 2.5–4× the pixels** at the same logical size. Structural, not fixable in Uno. | — |

### 5.4 The corrected statement of the finding

> Win32-with-a-wheel is the only configuration in which Uno's scroll motion is produced by a single
> time-parameterised function evaluated exactly once per frame, *inside the frame it affects*, with
> no quantizer between the input and the visual. Every other combination adds at least one of:
> a 2-DIP motion quantizer (touch drag), a full frame of latency (touch inertia), a serialized
> record+raster budget (WASM), an unaligned record phase (Android), or no smoothing at all
> (Apple wheel/trackpad, Android wheel). On top of that, the per-frame render cost is dominated by
> an O(realized-visuals) Skia-pathops damage computation whose cost is invisible on a desktop CPU
> and decisive on a phone.

---

## 6. The real bottleneck, ranked by provable impact

Stated bluntly, because the brief asked for it: **the physics model is not the bottleneck. The
per-frame render cost is, and the clock discipline is second.** The physics changes *feel*; it does
not change FPS. Ranking:

| Rank | Cause | Cost shape | Confidence | Cite |
|---|---|---|---|---|
| **1** | **Per-visual Skia pathops damage over the whole scrolled subtree, every frame** (G1) | O(realized visuals) × ~3 pathops + 3 allocs | **High** — mechanism read end-to-end; the early-out provably cannot fire during scroll | `Visual.Damage.skia.cs:27-105`; `DamageRegionExtensions.skia.cs:9-35` |
| **2** | **Children-picture cache destroyed subtree-wide every scroll frame** (C1) → full tree walk, collapsing optimization structurally unreachable | O(realized visuals) walk + draw-picture | **High** | `Visual.skia.cs:140-146, 245-258, 388-398, 531-544`; `ContainerVisual.skia.cs:212-227` |
| **3** | **No frame clock: six motion sources, none using presentation time, none agreeing** (G2, G4, note 14 cross-cutting table) | latency + jitter, not throughput | **High** | `Compositor.cs:38`; `KeyFrameEvaluator.cs:56-59`; `CompositionTarget.Rendering.skia.cs:198,439-475`; `InteractionTracker*InertiaHandler.cs:16,47-48` |
| **4** | **Touch drag quantized to ≥2 logical px** (D1/G3) | discretization, device-specific | **High** | `GestureRecognizer.Manipulation.cs:33,420-427` |
| **5** | **WASM record+raster serialization** (P2) | halves FPS above 16.7 ms total | **High** | `BrowserRenderer.cs:65-115` |
| **6** | **Wheel model: 1 s Power-10 keyframe restarted per detent + `Stopped`→non-intermediate `ViewChanged`→`InvalidateArrange` per detent** | per-detent hitch + sawtooth velocity | **High** | `ScrollContentPresenter.Managed.cs:471-497`; `CompositionObject.cs:92`; `ScrollViewer.cs:1318-1334` |
| **7** | **`OnFrame` publishes a one-frame-stale offset** (G6) → virtualization always targets last frame's viewport | leading-edge blanks | **High** | `ScrollContentPresenter.Managed.cs:490-496`; `CompositionObject.cs:97` |
| **8** | **ItemsRepeater viewport-significance throttle is dead code on Skia/WASM** | extra measure/arrange per viewport delta on repeater content | **High** | `ViewportManagerWithPlatformFeatures.cs:599-608`; `Uno.CrossTargetting.targets:69-71,74,78` |
| **9** | **ScrollBar `Value` → `UpdateTrackLayout` → AffectsMeasure writes** (C9) | one dirty-path layout per published offset | **Medium** (real, but calibrated down) | `ScrollBar.mux.cs:729-733, 1032-1056` |
| **10** | **Render slot blocked on full Normal backlog** (G9) | frame postponement under load | **Medium** | `NativeDispatcher.cs:206-217` |
| **11** | **Per-frame / per-call allocations**: `_runningAnimations.Keys.ToArray()` per frame; `new AnimationController(...)` + permanent `Stopped` subscription per `TryGetAnimationController`; LINQ closures in `KeyFrameEvaluator`; `FramePicture[]` + `List` + `RenderingEventArgs` per `RaiseRendering` | Gen0 pressure → GC pauses land inside the scroll | **Medium** | `Compositor.skia.cs:206`; `CompositionObject.cs:261-270`, `AnimationController.cs:26`; `KeyFrameEvaluator.cs:92,103,109`; `CompositionTarget.Rendering.skia.cs:462-475` |
| **12** | **Inertia physics model** (parabolic, magic constants) and **velocity estimation** (two-point) | changes distance/feel, **not** FPS | **High** (that it is *not* an FPS cause) | `…InertiaProcessor.cs:268-276`; `GestureRecognizer.Manipulation.cs:462-467` |
| **13** | **`int` wheel-delta dead zone on the InteractionTracker path** (G8) | total functional failure for precision devices in `ScrollView`/`ItemsView` — a correctness bug, not a smoothness one | **High** | `InputManager.Pointers.Managed.cs:349`; `InteractionTracker.cs:137-145` |

**What this ranking implies for sequencing:** items 1 and 2 are *render-pipeline* fixes that pay off
on every platform, every input device, and every content type, and they are the ones whose cost
scales with the thing mobile is bad at. They should land before anything is retuned, because
retuning physics against a variable frame rate is measuring noise.

---

## 7. Consolidated, deduplicated, ranked intervention list

Deduplicated across all 14 notes plus the new findings. `[N]` marks the originating note(s);
`[new]` marks findings from this cross-check.

### Tier A — do these first; they change the frame budget

| # | Intervention | Why first | Cite |
|---|---|---|---|
| **A1** | **Scroll-aware damage fast path.** When a frame's only invalidation is an ancestor transform, compute damage as `oldScrollPortRect ∪ newScrollPortRect` and skip `ContributeDamageOnPaint`'s pathops branch for the whole subtree (still refreshing `_lastRenderBounds`/`_lastRenderMatrix`). `[new]` | Removes ~3 Skia pathops + 3 `SKPath` allocs **per visual per frame**, for a result that is degenerate anyway. Largest single CPU win; scales exactly with the thing that makes mobile worse. | `Visual.Damage.skia.cs:27-68, 71-105`; `DamageRegionExtensions.skia.cs:9-35` |
| **A2** | **Stop tearing down the children-picture cache on pure transform changes.** A cached `_childrenPicture` is recorded in the visual's *local* space and re-applies `TotalMatrix` on replay, so it is transform-independent and must survive an ancestor move. Concretely: `ContainerVisual.SetMatrixDirty`'s recursion must not route through `InvalidateParentChildrenPicture`. `[09,10,14,new]` | Currently the collapsing optimization (50 clean frames / 100 visuals) can *never* engage inside a scrolling subtree. Turns an O(realized visuals) walk into O(1) for unchanged subtrees. | `Visual.skia.cs:140-146, 245-258, 388-398, 531-556`; `ContainerVisual.skia.cs:212-227` |
| **A3** | **Introduce one authoritative per-frame timestamp**, sampled once at `Render()` entry, and pass it to every animation evaluation and to `RenderingEventArgs`. Then (phase 2) make it a *predicted presentation* time from the platform. `[04,06,07,08,10,new]` | Today `Compositor.TimestampInTicks` re-reads `Stopwatch.GetTimestamp()` per animation, so two animations in one frame see different times, and none sees present time. This is the precondition for any closed-form physics to be evaluated correctly. | `Compositor.cs:38`; `KeyFrameEvaluator.cs:56-59`; `Compositor.skia.cs:206-220`; `CompositionTarget.Rendering.skia.cs:475` |
| **A4** | **Remove the 2-logical-pixel drag quantizer.** Separate "advance the scroll / feed the velocity tracker" (no threshold) from "raise the public `ManipulationDelta` event" (keep a threshold). `[12,new]` | The single clearest mechanism behind "dragging on mobile feels worse than wheeling on Win32". Cheap, local, and testable. | `GestureRecognizer.Manipulation.cs:33-35, 154-160, 420-427`; `ManipulationDelta.cs:54-58` |
| **A5** | **Give touch inertia a pre-record tick.** Move the inertia driver off `CompositionTarget.Rendering` (post-record) onto the pre-record hook in `Compositor.RenderRootVisual`, or add an explicit `PreRender` phase at `Render()` entry. `[09,10,new]` | Removes a *structural* full frame of latency from every fling, and lets inertia use A3's timestamp instead of its own `Stopwatch`. G5 identifies the exact insertion point. | `CompositionTarget.Rendering.skia.cs:110-119, 198, 439-452`; `…InertiaProcessor.cs:333-352`; `Compositor.skia.cs:199-220` |

### Tier B — correctness and latency fixes that are small and safe

| # | Intervention | Cite |
|---|---|---|
| **B1** | Fix the `OnFrame` / `ReEvaluateAnimation` subscription order so the published offset is the current frame's (subscribe `OnFrame` after `StartAnimation`, or read the evaluated value directly). One line. `[09]` | `ScrollContentPresenter.Managed.cs:490-496`; `CompositionObject.cs:97` |
| **B2** | Widen the wheel delta contract: keep `public int MouseWheelDelta` for parity, add an internal `double` carrying the precise delta, and remove the integer division at the tracker call site. `[03,11]` | `InputManager.Pointers.Managed.cs:349`; `PointerPointProperties.cs:254`; `InteractionTracker.cs:137-145` |
| **B3** | Stop the non-intermediate `ViewChanged` + `InvalidateArrange` storm on wheel: don't let `StartAnimation`'s implicit `StopAnimation` fire the previous animation's `Stopped` handler as a *completion*. `[09]` | `CompositionObject.cs:90-93`; `KeyFrameAnimation.cs:60-64`; `ScrollContentPresenter.Managed.cs:482-488`; `ScrollViewer.cs:1327-1334` |
| **B4** | Cache the `AnimationController` (or add a non-allocating `TryGetAnimationRemaining`) and unsubscribe `Animation_Stopped`. Today every call allocates and permanently grows the `Stopped` invocation list. `[09]` | `CompositionObject.cs:261-270`; `AnimationController.cs:21-27, 100-104` |
| **B5** | Re-key the "precise/immediate" wheel branch from `OperatingSystem.IsIOS()\|\|IsMacOS()` onto `PointerPointProperties.IsTouchPad`, and populate `IsTouchPad` on the wheel path on Win32/macOS. `[11]` | `ScrollContentPresenter.cs:311, 335`; `Win32WindowWrapper.Pointers.cs:146-152, 209` |
| **B6** | Implement `MotionEventActions.Scroll` on Skia-Android (`Axis.Vscroll`/`Hscroll`) — mouse wheel is currently completely dead. `[11]` | `AndroidCorePointerInputSource.cs:125-196`; `PointerHelpers.Android.cs` |
| **B7** | Restore a viewport-change guard for `ItemsRepeater` on Skia/WASM — the existing one is behind `#if !UNO_HAS_ENHANCED_LIFECYCLE`, which is defined on exactly those platforms. Port WinUI's `roundingTolerance = 0.01f` at minimum. `[09,13]` | `ViewportManagerWithPlatformFeatures.cs:599-608`; `Uno.CrossTargetting.targets:69-78` |
| **B8** | Fix the Win32 pointer timestamp overflow (`(ulong)(GetMessageTime() * 1000)`, `int` arithmetic, wraps at ~35.8 min) and stop truncating positions to integer logical px. `[11,12]` | `Win32WindowWrapper.Pointers.cs:113-114, 124, 227` |
| **B9** | De-allocate the per-frame path: reuse a buffer instead of `_runningAnimations.Keys.ToArray()`; pool `FramePicture[]`/`List`/`RenderingEventArgs` in `RaiseRendering`; index-scan instead of LINQ in `KeyFrameEvaluator`. `[09,10]` | `Compositor.skia.cs:206`; `CompositionTarget.Rendering.skia.cs:462-475`; `KeyFrameEvaluator.cs:92,103,109` |

### Tier C — model and architecture; do after A and B, and after measuring

| # | Intervention | Notes |
|---|---|---|
| **C1** | **One continuous, velocity-composing scroll model shared by wheel, key and touch**, replacing (a) the 1 s Power-10 keyframe restarted per detent, (b) the constant-velocity 0.25 s tracker wheel handler, and (c) the parabolic inertia. Closed-form `x(t)` evaluated at A3's timestamp; impulses *accumulate* into the running simulation instead of restarting it. `[03,07,09,14]` | Flutter's `FrictionSimulation` / `ClampingScrollSimulation` (note 07 §8) are directly transcribable and are already closed-form. WinUI's ScrollPresenter constants (`s_offsetsChangeMsPerUnit=5`, min 50 ms, max 1000 ms) are the parity target for *programmatic* scrolls only. |
| **C2** | **Least-squares velocity estimator** (degree-2, 100 ms horizon, 20-sample ring, ≥3 samples, 40 ms stopped cut-off), one tracker per pointer id, called only at inertia start. `[06,07,08,12]` | Note 12 documents five wiring defects in the prior attempt's version — reuse the solver, not the wiring. |
| **C3** | **Feed the estimator every available sample**: Android historical samples, iOS coalesced touches, browser `getCoalescedEvents`, Win32 pointer frame history. Uno uses **none** today (G7). `[04,11]` | Improves velocity fidelity without adding latency; distinct from resampling. |
| **C4** | **Pointer resampling — opt-in only, and only if C1–C3 do not close the gap.** `[08,14]` | Flutter ships it off (`resamplingEnabled = false`); the offset is `-38 ms` of added latency. Do not lead with this. |
| **C5** | Raise the ListView realization buffer (`DefaultCacheLength = 1.0` × `ExtendedViewportScaling = 0.5` gives only 0.5 viewport/side) and fix the `null` path that yields 0.0 on Skia. `[13]` | `FeatureConfiguration.cs:323`; `VirtualizingPanelLayout.managed.cs:155-183` |
| **C6** | Fix `GridView` virtualization on Skia — `ItemsWrapGrid` is compiled out by `#if !UNO_REFERENCE_API`, so `GridView` falls back to a non-virtualizing `WrapPanel`. `[13]` | `ItemsWrapGrid.cs:1`; `Uno.CrossTargetting.targets:69-71` |
| **C7** | Batch/coalesce input-driven scroll notifications: at most one `ViewChanging`/`ViewChanged` pair per frame, bracketing the whole per-frame delta handler (WinUI's `DelayViewChanging`/`FlushViewChanging`). `[01,02,04,12]` | Uno already coalesces via `RequestUpdate`; the missing piece is bracketing the *manipulation delta* handler, not just `ChangeView` (note 12 rec 12). |
| **C8** | Route WASM's record inside the rAF callback (or give the record a deadline) so the per-frame budget stops being `record + raster` serialized. `[14 §12, new]` | `BrowserRenderer.cs:65-115`; `CompositionTarget.RenderScheduling.skia.cs:166-176` |
| **C9** | Route Android's render enqueue through `Choreographer.postFrameCallback` (re-post *before* signalling, per Avalonia's `ChoreographerTimer`) instead of `_handler.Post`. `[06,14]` | `NativeDispatcher.Android.cs:39-59` |
| **C10** | Move scroll off the UI thread entirely (compositor-thread transform + expression, WinUI/DManip/InteractionTracker shape). `[01,02,03,06]` | The correct long-term architecture, but note 14 §11 is right that migrating to `ScrollPresenter`/`InteractionTracker` **as it exists today** trades one set of defects for another (17 ms threadpool timer, cross-thread composition mutation, 0.25 s constant-velocity wheel). Fix the motion layer first; treat the migration as an independent parity question. |
| **C11** | Do **not** ship `MathF.Round` pixel snapping on the scroll offset, and do not throttle EVP to every 3rd inertia frame. `[12]` | Both are symptom suppression that fight A1/A2/A4. Note 12's analysis stands. |

### Explicitly rejected

| Rejected | Reason |
|---|---|
| Avalonia's wheel model (50 DIP/unit, no animation) `[05]` | Least smooth part of that framework; note 05 rejects it itself. |
| Avalonia's UI-thread `Dispatcher.InvokeAsync(Input)` fling loop `[06]` | Note 06 rejects it itself; it is the pattern Uno already has and is trying to leave. |
| Adding a `CubicBezier` to the default programmatic scroll animation `[03]` | WinUI supplies **no** easing function (`ScrollPresenter.cpp:3282`); inventing one is a silent parity deviation. |
| Pointer prediction / extrapolation `[04]` | No prediction anywhere in dxaml/xcp; overshoot artifacts; and Uno has bigger, provable costs first. |
| "Adopt one-item-per-dispatcher-callback" `[04 rec 2]` | Already implemented (C5). |
| Retuning inertia constants before A3 lands | Physics tuned against a jittering, non-presentation clock is unmeasurable. |

---

## 8. What to measure, before and after

The instrument already exists and no note is wrong about it:
`Application.Current.DebugSettings.EnableFrameRateCounter` → `SkiaRenderHelper.FpsHelper`, which
separately reports **dropped** frames (`OnFramePresentRequested`) and **unpresented** frames
(`OnFrameRecorded`), plus mean frame time and draw-to-present delay. SamplesApp exposes it as
`ShowFpsIndicator`. That split is the diagnostic that matters: *unpresented* means the UI thread
recorded work that never reached the screen; *dropped* means vsync fired with nothing new.

Three measurements would settle the ranking in §6 empirically, in order:

1. **A1's premise.** Instrument `ContributeDamageOnPaint` with a counter of visuals taking the
   pathops branch per frame, during a ListView touch drag and during a Win32 wheel scroll. If the
   count is in the hundreds, A1 is confirmed as rank 1.
2. **A2's premise.** Counter for `_childrenPicture` frees per frame during a scroll. Expect
   ≈ number of `ContainerVisual`s in the scrolled subtree.
3. **D1's premise.** Log the distribution of `changeSet.Delta.Translation.Y` at
   `GestureRecognizer.Manipulation.cs:422` during a slow drag on a phone. Expect a spike at exactly
   ±2 and a hole in (−2, 2).

---

## 9. Still open

- **U6** — whether Uno's `ScrollView` port carries WinUI's `s_minimumVelocity` baseline-cancellation
  and correlation-id logic (`ScrollView.cpp:2383-2455`). Not checked.
- **U7** — whether ListView containers can safely stay parented while recycled.
- **U5** — the Win32 HIMETRIC coordinate assumption from the prior attempt.
- Whether `_childrenInterestedInViewportUpdates` grows with realized item count in a `ListView`
  (bounding G10's item 4). Not measured.
- Whether the `RetainedLayer.Present` full-surface blit (note 10 rec 11) is material relative to A1
  on mobile GPUs. Not measured; it is a *GPU* cost while A1 is a *CPU* cost, so they are separable.
- Avalonia (notes 05, 06) and WinUI (notes 01–04) line citations were not independently re-verified
  in this pass and are second-hand here.
