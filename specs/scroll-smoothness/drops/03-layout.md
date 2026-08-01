# 03 — Per-frame layout & effective-viewport cost as the cause of fling "dropped" frames

Scope of this note: investigate whether the per-fling-frame layout / virtualization /
effective-viewport work is what makes the Android fling drop frames while a drag and the
RedirectVisual sample do not.

**Verdict up front:**

- The **effective-viewport leg of the leading hypothesis is dead** for a plain `ListView`.
  `InvalidateViewport()` early-returns before it ever reaches
  `EnqueueForEffectiveViewportChanged` / `RequestAdditionalFrame`. **VERIFIED by code.**
- The **per-frame layout leg is real but symmetric**. Every single piece of layout work a
  fling frame does, a drag frame also does, through the identical call chain, at a
  comparable rate. **As a standalone explanation of drag-vs-fling it is DEAD.**
- What survives, and what I believe is the actual answer, is a **two-factor** mechanism:
  the *counting* asymmetry comes from `Compositor.RenderRootVisual` self-re-requesting a
  frame whenever `FrameStarting is not null` (fling) or an animation is running
  (RedirectVisual) but **not** during a drag; the *fling-vs-RedirectVisual* asymmetry comes
  from the per-frame layout/dispatcher tax that only the fling pays. Layout cost is the
  second factor, not the first.

---

## 1. What actually runs, per fling frame, traced end to end

### 1.1 Inside the record

`Compositor.RenderRootVisual` raises `FrameStarting` **before** the paint walk
(`src/Uno.UI.Composition/Composition/Compositor.skia.cs:300-320`). The fling driver is
subscribed there (`ScrollContentPresenter.Managed.cs:601`), so per frame:

```
RenderRootVisual
 └ FrameStarting → OnFlingFrame                      (SCP.Managed.cs:617-644)
    └ Set(h, v, IsTouch: true, IsIntermediate: true) (SCP.Managed.cs:643)
       ├ Update(contentElt, …)  → visual.AnchorPoint = target   (SCP.Managed.cs:416-422, 485-489)
       └ Updated(h, v, isIntermediate)                          (SCP.Managed.cs:434-469)
          └ UpdateOffsets
             ├ Scroller.OnPresenterScrolled(h, v, true)         (SCP.Managed.cs:460)
             ├ ScrollOffsets = new Point(h, v)                  (SCP.Managed.cs:466)
             └ InvalidateViewport()                             (SCP.Managed.cs:467)
```

A drag reaches the **same** `Set(…, IsTouch: true, IsIntermediate: true)` from
`OnManipulationDelta` (`SCP.Managed.cs:873-877`). Below `Set` the two paths are
byte-for-byte identical. Keep that in mind for §4.

### 1.2 `InvalidateViewport` — dead end for a `ListView`

`InvalidateViewport()` → `PropagateEffectiveViewportChange()`
(`src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs:256-266`), whose **first
statement** is:

```csharp
if (!IsEffectiveViewportEnabled)
{
    return;                                  // FrameworkElement.EffectiveViewport.cs:349-353
}
```

and

```csharp
private bool IsEffectiveViewportEnabled
    => _childrenInterestedInViewportUpdates is { Count: > 0 } || _effectiveViewportChanged != null;
                                             // FrameworkElement.EffectiveViewport.cs:84
```

The chain is opt-in from the leaves up: a subscriber calls `RequestViewportUpdates` on its
parent, which recursively enables the ancestors (`:89-193`). Grepping every consumer of
`EffectiveViewportChanged` in `src/Uno.UI` gives exactly four live subscribers:

| Subscriber | File | Relevant to a plain ListView fling? |
|---|---|---|
| `ItemsRepeater` viewport manager | `UI/Xaml/Controls/Repeater/ViewportManagerWithPlatformFeatures.cs` | No — `ListView` uses `ItemsStackPanel`, not `ItemsRepeater` |
| `CalendarPanel` | `UI/Xaml/Controls/CalendarView/Primitives/CalendarPanel.ModernCollectionBasePanel.cs` | No |
| `TeachingTip` | `UI/Xaml/Controls/TeachingTip/TeachingTip.mux.cs` | No |
| `SystemFocusVisual` | `UI/Xaml/Controls/FocusVisual/SystemFocusVisual.cs:80` | Only while a focus visual is shown |

`ListViewBase`, `ScrollViewer`, `ScrollContentPresenter`, `ItemsStackPanel` and
`VirtualizingPanelLayout` **never** subscribe. Uno's managed virtualization is driven by
`ScrollViewer.ViewChanged`, not by effective viewport:

```csharp
ScrollViewer.ViewChanged += OnScrollChanged;   // VirtualizingPanelLayout.managed.cs:211
```

**Consequence:** for a plain `ListView` fling,
`EventManager.EnqueueForEffectiveViewportChanged` (`Internal/EventManager.cs:29-35`) is
never called, so the `CoreServices.RequestAdditionalFrame()` on `:34` never fires, so the
Normal-priority dispatcher item described in the brief's leading hypothesis **is never
enqueued from this path**. `RaiseEffectiveViewportChangedEvents` (`UIElement.cs:982`) never
runs either. That specific leg of the leading hypothesis is **falsified**.

(Caveat, **UNVERIFIED**: I have not seen the product owner's actual page. If it contains an
`ItemsRepeater`, or if a keyboard focus visual is up over the list, the chain lights up and
this conclusion changes. Cheap check in §6.)

### 1.3 `OnPresenterScrolled` — the real per-frame enqueue

```csharp
if (isIntermediate && UpdatesMode != ScrollViewerUpdatesMode.Synchronous)
{
    RequestUpdate();                        // ScrollViewer.cs:1239-1243
    _snapPointsTimer?.Stop();
}
```

`FeatureConfiguration.ScrollViewer.DefaultUpdatesMode` is `AsynchronousIdle`
(`FeatureConfiguration.cs:483`), so this branch is taken. `RequestUpdate` is:

```csharp
private void RequestUpdate()
{
    if (_hasPendingUpdate) { return; }
    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { … Update(isIntermediate: true); });
    _hasPendingUpdate = true;               // ScrollViewer.cs:1301-1316
}
```

So **one Normal-priority dispatcher item per fling frame** (coalesced to at most one
outstanding). This is a Normal item enqueued from *inside* the record — which is the shape
the leading hypothesis wanted, but sourced from `ScrollViewer.RequestUpdate`, not from the
effective-viewport queue.

Note what `Update(isIntermediate: true)` **does not** do: the `InvalidateArrange()` at
`ScrollViewer.cs:1328-1337` is explicitly gated on `!isIntermediate`. So the SV itself does
not dirty layout during inertia. Good.

### 1.4 The deferred turn: virtualization

`Update` raises `ViewChanged` (`ScrollViewer.cs:1356`) →
`VirtualizingPanelLayout.OnScrollChanged` (`VirtualizingPanelLayout.managed.cs:259-331`):

```csharp
var delta = ScrollOffset - _lastScrollOffset;
var isLargeScroll = Abs(delta) > ViewportExtent;      // :266-270
if (isLargeScroll) { ClearLines(…); SetDynamicSeed(…); }   // :272-294
while (unappliedDelta > 0)
{
    var scrollIncrement = GetScrollConsumptionIncrement(fillDirection);  // = one line's extent, :349-362
    unappliedDelta -= scrollIncrement;
    UpdateLayout(extentAdjustment: sign * -unappliedDelta, isScroll: true);   // :313
}
ArrangeElements(_availableSize, ViewportSize);          // :320
UpdateCompleted();                                      // :321
```

Per-frame cost decomposition:

| Work | Runs when | Cost |
|---|---|---|
| `UnfillLayout` (`:577-605`) | every ViewChanged | two `while` guards that fail immediately when nothing left the extended viewport → ~2 `GetMeasuredEnd/Start` calls |
| `FillLayout` (`:518-568`) | every ViewChanged | two `while` guards that fail immediately when nothing entered → ~2 `GetItemsStart/End` calls |
| `RecycleLine` / `AddLine` → `AddView` → **`view.Measure(slotSize)`** (`:1025-1037`) | **only on line boundary crossing** | one full container measure per realized line |
| `ArrangeElements` (`:439-453`) | **every** ViewChanged | iterates every materialized line; each `container.Arrange(bounds)` **early-returns** when the rect is unchanged and the element is not dirty (`UIElement.Layout.crossruntime.cs:362-366`) — the arrange bounds are panel-space and do not change with scroll (the offset is applied by the SCP's `Visual.AnchorPoint`, not by re-arranging children) |
| `UpdateCompleted` → `Generator.ClearScrappedViews()` + `UpdateVisibilities()` (`:499-508`) | every ViewChanged | small; wrapped in `ShouldInterceptInvalidate` |

**Answer to "does a measure/arrange pass run every fling frame?"** — Not a *tree* pass. The
panel's `MeasureOverride`/`ArrangeOverride` are **not** invoked per fling frame:
`OwnerPanel.InvalidateMeasure()` in `OnScrollChanged` is inside the `isLargeScroll` branch
only (`:323-328`), and `UpdateLayout(isScroll: true)` skips the forced
`OwnerPanel.UpdateLayout()` (`:485-493`, gated on `!isScroll`). What *does* run every frame
is `ArrangeElements`, i.e. an O(materialized-lines) loop of no-op `Arrange` calls —
~15-30 early-returning calls, sub-100µs territory.

**Answer to "does container realization run every frame?"** — No, only on line-boundary
crossing. The `while (unappliedDelta > 0)` loop consumes the delta in **one-line
increments** (`:296-318`), so a fast fling with a 200px/frame delta runs ~3-4
`UpdateLayout` iterations and realizes ~3-4 containers in that frame; a slow fling with a
5px/frame delta runs one iteration that realizes nothing.

**This is the single most damaging fact for the layout-cost hypothesis:** realization cost
scales *with* fling velocity, but the reported symptom is *"worse the slower the fling
gets"*. The cost curve runs the wrong way.

### 1.5 The layout work nobody attributed to the ListView: the ScrollBar

Independent of virtualization, `ScrollViewer.Update` writes `VerticalOffset`, which is
template-bound into the vertical `ScrollBar`:

```xml
Value="{TemplateBinding VerticalOffset}"        <!-- ScrollViewer.xaml:274 -->
```

```csharp
protected override void OnValueChanged(double oldValue, double newValue)
{
    base.OnValueChanged(oldValue, newValue);
    UpdateTrackLayout();                         // ScrollBar.mux.cs:729-733
}
```

and `UpdateTrackLayout` writes **layout-affecting DPs** every time
(`ScrollBar.mux.cs:1005-1057`):

```csharp
m_tpElementVerticalLargeDecrease.Height = largeDecreaseNewSize;   // :1041
newMargin.Top = indicatorOffset;
m_tpElementVerticalPanningRoot.Margin = newMargin;                // :1055-1056
```

Nothing sets `ShouldInterceptInvalidate` on the ScrollBar subtree, so these propagate:
`InvalidateMeasure` → parent chain → `IsVisualTreeRoot` →
`XamlRoot.InvalidateMeasure()` (`UIElement.Layout.crossruntime.cs:45-51`) →
`CoreServices.RequestAdditionalFrame()` (`XamlRoot.crossruntime.cs:14-20`) →

```csharp
NativeDispatcher.Main.Enqueue(static () => OnTick(), NativeDispatcherPriority.Normal);
                                            // CoreServices.cs:73
```

→ `OnTick` → `root.UpdateLayout()` → `InnerUpdateLayout` → a genuine
`Measure`/`Arrange` pass over the dirty ScrollBar path (`UIElement.cs:948-1012`).

So the honest per-scroll-update Normal-queue budget is **two** items, not one:

1. `ScrollViewer.RequestUpdate` → `Update` → `ViewChanged` → virtualization
2. `CoreServices.RequestAdditionalFrame` → `UpdateLayout` → ScrollBar measure/arrange

both CAS/flag-coalesced to one outstanding each. On Android every dispatcher item costs a
**separate main-`Looper` message** — `EnqueueNative` is a bare `_handler.Post(_implementor)`
(`NativeDispatcher.Android.cs:39-42`) and `DispatchItems` runs exactly **one** item per
message (`NativeDispatcher.cs:128-177`). So a fling frame needs ≥3 main-Looper round trips
(render action + 2 Normal items) inside an 8.33 ms budget, interleaved with Android's own
input and Choreographer traffic on the same Looper.

(**UNVERIFIED**: whether the vertical ScrollBar is actually realized in the PO's build —
the WinUI3 merged style carries `x:Load="False"` on `VerticalScrollBar`
(`mergedstyles.xaml:11064`), so it only materializes once `ComputedVerticalScrollBarVisibility`
turns visible. With `Auto` visibility and scrollable content it does. Confirm on device.)

---

## 2. Why the "dropped" counter is not a plain frame-time metric

This matters for scoring every hypothesis, so it is worth stating explicitly.

- `dropped` is incremented in `Draw` on the **render thread** when no new picture has been
  recorded since the previous `Draw` (`SkiaRenderHelper.skia.cs:292-313`, called from
  `CompositionTarget.Rendering.skia.cs:240`).
- `Draw` only runs when the UI thread asked for it: `RenderLoop` waits on `_renderEvent`,
  which is set by `InvalidateRender` (`UnoSKVulkanView.cs:60-65, 137-171`), and paces to
  vsync **after** drawing.
- FPS is `_framesRenderedInLastSecond`, incremented in `EndFrame` on the **UI thread** —
  i.e. FPS counts **records**, not presents (`SkiaRenderHelper.skia.cs:243-260`).

So "FPS 100+, dropped 20+" decodes as: **~120 paced Draws/s but only ~100 records/s.**
"FPS 100+, dropped ~0" decodes as: **~100 Draws/s and ~100 records/s — one Draw per
record.**

The critical structural fact:

```csharp
if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
{
    rootVisual.CompositionTarget?.RequestNewFrame();   // Compositor.skia.cs:372-375
}
```

- **Fling**: `FrameStarting is not null` (`SCP.Managed.cs:601`) ⇒ every record
  self-requests the next frame ⇒ the render thread keeps drawing at the vsync rate whether
  or not the UI thread produced anything. Stale presents are *possible and counted*.
- **RedirectVisual**: `_runningAnimations.Count > 0` (Lottie/AVP) ⇒ same self-sustaining
  loop ⇒ stale presents are *possible and counted*.
- **Drag**: `FrameStarting` is null and no animation runs. `RequestNewFrame` only comes
  from `InvalidateRenderPartial` when a Visual property actually changes
  (`Compositor.skia.cs:378-383`), i.e. **once per offset write**. A `Draw` therefore
  cannot happen unless a record preceded it. `dropped ≈ 0` is **structural**, not evidence
  of speed.

Corollary: a drag whose UI-thread frame costs 12 ms shows up as *FPS 83 / dropped 0*, not
as dropped frames. The counter simply cannot see drag jank.

---

## 3. Hypotheses and their three-way predictions

### H-L1 — "Effective-viewport propagation enqueues a Normal item per fling frame"

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicted | drops | drops | clean |
| observed | ~0 | 20+ | clean |

**DEAD, twice over.** (a) The path early-returns at
`FrameworkElement.EffectiveViewport.cs:349-353` for a plain ListView — it is never even
reached. (b) Even if it were reached, it is reached identically from the drag path, so it
predicts drag drops.

### H-L2 — "Per-frame virtualization (realize/recycle) starves the frame"

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicted | drops (same work, same rate) | drops | clean |
| observed | ~0 | 20+ | clean |

**DEAD as a standalone explanation.** Both paths funnel through the same
`Set(…, IsTouch: true, IsIntermediate: true)` and the same deferred `ViewChanged`. A drag
at 120 Hz touch sampling crosses exactly as many item boundaries per second as a fling at
the same velocity. Additionally, realization cost rises with velocity while the reported
severity rises as the fling *slows* — the wrong sign.

### H-L3 — "Per-frame ScrollBar `UpdateTrackLayout` dirties layout to the root every frame"

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicted | drops | drops | clean |
| observed | ~0 | 20+ | clean |

**DEAD as a standalone explanation**, same symmetry argument. Real work, verified by code
(`ScrollBar.mux.cs:1041, 1055-1056` → `XamlRoot.InvalidateMeasure` →
`CoreServices.RequestAdditionalFrame`), but paid on both paths.

### H-L4 (survives) — "Self-sustaining frame request × per-frame UI-thread tax"

Two factors, each necessary, neither sufficient:

- **Factor A (visibility):** `Compositor.skia.cs:372-375` self-requests a frame whenever
  `FrameStarting is not null` *or* an animation is running. This is true for the fling and
  for RedirectVisual, false for a drag. Only under Factor A can a vsync find stale content
  and be counted.
- **Factor B (cost):** the fling's UI thread must, per frame, run the render action **plus**
  two Normal-priority dispatcher items (virtualization + ScrollBar-driven `UpdateLayout`),
  each a separate Android main-`Looper` message (`NativeDispatcher.Android.cs:39-42`,
  `NativeDispatcher.cs:128-177`), plus periodic container measures. RedirectVisual runs the
  render action and nothing else — no layout, no viewport, no extra Looper messages.
  Additionally `NativeDispatcher.TryGetRenderAction` re-seeds
  `normalItemsToProcessBeforeNextRenderAction` from the *current* Normal-queue depth every
  time it hands out a render action (`NativeDispatcher.cs:206-234`), so a Normal item still
  queued at the moment the render action is taken **withholds the next frame's render
  action** behind it.

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| Factor A (self-sustaining frames) | **no** | yes | yes |
| Factor B (per-frame layout/dispatcher tax) | yes (invisible) | yes | **no** |
| predicted `dropped` | **~0 by construction** | **>0** | **0** |
| observed | ~0 | 20+ | 0 |

**Consistent with all three.** Layout cost is Factor B — it is why the fling differs from
RedirectVisual. It is *not* why the fling differs from the drag.

Sub-claim this note deliberately does **not** settle: whether the ~20 lost records/s come
mostly from raw CPU overrun (>8.33 ms of UI-thread work) or from render-action withholding
by `normalItemsToProcessBeforeNextRenderAction`. §6 separates them.

### H-L5 (open, cheap to test) — "It is only the phase, not the cost"

A drag writes the offset from the input phase of the frame, so the two Normal items are
enqueued *early* and are drained in the gap before the vsync render action. A fling writes
the offset from *inside* the render action, so both Normal items land after it and collide
with the next frame's start. Under this hypothesis the *amount* of layout work is
irrelevant; only where in the frame it is enqueued matters.

| | drag | inertia | RedirectVisual |
|---|---|---|---|
| predicted | clean | drops | clean (no items at all) |
| observed | ~0 | 20+ | 0 |

Also consistent. H-L4 and H-L5 differ only in whether *reducing* the per-frame layout work
fixes the fling without moving the driver. That is a clean experimental fork (§6, E4/E5).

---

## 4. The drag question, answered plainly

> CRITICAL: this must explain why DRAG does not drop frames, since a drag changes the offset
> too and would trigger the same layout work. If it cannot, say so plainly and mark the
> hypothesis dead.

**It cannot, and the standalone hypothesis is dead.** Below `ScrollContentPresenter.Set`
there is no `IsIntermediate`/`IsTouch` branch that distinguishes drag from inertia — both
call `Set(horizontalOffset, verticalOffset, options: new(DisableAnimation: true,
IsTouch: true, IsIntermediate: true))` (fling: `SCP.Managed.cs:643`; drag:
`SCP.Managed.cs:873-877`) and therefore run the identical `Updated` → `OnPresenterScrolled`
→ `RequestUpdate` → `ViewChanged` → virtualization → ScrollBar `UpdateTrackLayout` chain.
Same work, same coalescing, comparable rate. Any hypothesis whose mechanism lives strictly
below `Set` predicts symmetric behaviour and is refuted by fact 1.

The asymmetry lives **above** `Set` — in *who calls it and from what phase*, and in the
`Compositor.skia.cs:372-375` self-re-request that only exists while `FrameStarting` is
subscribed. Layout cost is an amplifier of that asymmetry, not its source.

---

## 5. What this note rules in / rules out

**Ruled out (code-verified):**

- Effective-viewport propagation plays no part in a plain-`ListView` fling
  (`FrameworkElement.EffectiveViewport.cs:349-353`; no ListView-side subscriber).
- No panel `MeasureOverride`/`ArrangeOverride` per fling frame; no root measure pass from
  the virtualizer except on `isLargeScroll` (`VirtualizingPanelLayout.managed.cs:323-328`).
- `ArrangeElements` is not a real arrange pass — every call early-returns unless the rect
  changed (`UIElement.Layout.crossruntime.cs:362-366`).
- Container realization is boundary-triggered, not per-frame, and scales *with* velocity.

**Ruled in (code-verified, cost not yet measured on device):**

- Two Normal-priority dispatcher items per scroll update, each a separate Android
  main-`Looper` message.
- Per-scroll-update ScrollBar layout dirtying that reaches the visual-tree root.
- `TryGetRenderAction`'s re-seeding can withhold a render action behind those Normal items.

---

## 6. Experiments (cheapest first)

**E1 — Is the effective-viewport chain even live? (2 min, on device or Win32)**
Break/log in `FrameworkElement.PropagateEffectiveViewportChange` past the
`IsEffectiveViewportEnabled` guard while flinging the PO's page. Expected: never hit.
If it *is* hit, find the subscriber — my §1.2 conclusion is wrong for that page.

**E2 — Remove the ScrollBar from the equation (2 min, on device)**
Set `ScrollViewer.VerticalScrollBarVisibility="Hidden"` on the list and re-measure
`dropped` during a fling. This deletes the `UpdateTrackLayout` →
`XamlRoot.InvalidateMeasure` → `RequestAdditionalFrame` item entirely. A large drop in the
counter implicates the ScrollBar layout item specifically.

**E3 — Remove virtualization from the equation (5 min, on device)**
Replace the `ListView` with an `ItemsControl`/`ScrollViewer` over a fixed `StackPanel` of
the same visual complexity (no `ViewChanged` subscriber at all). Fling it. If `dropped`
stays at 20+, layout/virtualization is exonerated outright and the answer is purely
phase/scheduling (H-L5).

**E4 — Make the SV update synchronous (2 min, on device)**
`Uno.UI.Xaml.Controls.ScrollViewer.SetUpdatesMode(sv, ScrollViewerUpdatesMode.Synchronous)`.
This moves the virtualization + ScrollBar work *into* the record (removing one Looper
round trip and the render-action withholding) while keeping the total work identical.
- `dropped` falls → the problem is **dispatcher scheduling**, not CPU cost (H-L5).
- `dropped` unchanged or worse → the problem is **CPU cost** inside the frame (H-L4/B).

**E5 — Falsify on Win32 with the existing harness**
`src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_ScrollSmoothness.cs` already
measures duplicate offsets per `CompositionTarget.Rendering` callback and reports 121
callbacks/s with 0% duplicates. Add a variant that inflates the per-frame layout tax —
e.g. an `ItemsStackPanel` over a heavy `DataTemplate`, or an artificial Normal-priority
item enqueued from `FrameStarting` — and see whether Win32's duplicate rate becomes
non-zero. If Win32 stays at 0% even under an inflated tax, layout cost is not the
mechanism and the answer is Android-Looper-specific.

**E6 — Confound to avoid**
Do **not** measure with `FeatureConfiguration.ScrollViewer.EnableDiagnostics` on. It
subscribes `CompositionTarget.Rendering` (`SCP.Managed.cs:164-172`), which sets
`_isRenderingActive` and makes `Render()` self-request a frame every tick
(`CompositionTarget.Rendering.skia.cs:164-167`) — i.e. it turns **Factor A on for the drag
too**, and a previously "clean" drag will start reporting drops. That would look like a
finding and is an artefact.

---

## 7. Files cited

- `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs`
- `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs`
- `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.xaml`
- `src/Uno.UI/UI/Xaml/Controls/ListViewBase/VirtualizingPanelLayout.managed.cs`
- `src/Uno.UI/UI/Xaml/Controls/ScrollBar/ScrollBar.mux.cs`
- `src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs`
- `src/Uno.UI/UI/Xaml/Internal/EventManager.cs`
- `src/Uno.UI/UI/Xaml/Internal/CoreServices.cs`
- `src/Uno.UI/UI/Xaml/XamlRoot.crossruntime.cs`
- `src/Uno.UI/UI/Xaml/UIElement.cs`, `src/Uno.UI/UI/Xaml/UIElement.Layout.crossruntime.cs`
- `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs`
- `src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs`
- `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs`
- `src/Uno.UI.Composition/Composition/Compositor.skia.cs`
- `src/Uno.UI.Dispatching/Native/NativeDispatcher.cs`, `NativeDispatcher.Android.cs`
- `src/Uno.UI.Runtime.Skia.Android/Rendering/UnoSKVulkanView.cs`, `ChoreographerFramePacer.cs`
- `src/SamplesApp/SamplesApp.Samples/Windows_UI_Composition/RedirectVisualTests.xaml`
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_ScrollSmoothness.cs`
