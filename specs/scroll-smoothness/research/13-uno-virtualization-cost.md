# 13 — Cost of virtualization during scroll in Uno (ListView / ItemsRepeater), Skia-first

Scope: what actually executes per scroll tick inside Uno's virtualizing containers on **Skia**
(`__SKIA__` + `UNO_REFERENCE_API` + `UNO_HAS_ENHANCED_LIFECYCLE` + `UNO_HAS_MANAGED_SCROLL_PRESENTER`),
with WinUI (`D:/Work/microsoft-ui-xaml2`) as the reference. All claims below are anchored to
`file:line` in the working tree at `D:/Work/uno-worktrees/scrollsmooth`.

Everything marked **UNVERIFIED** was not confirmed by reading source and must not be trusted.

---

## 0. Build-flag map (this determines which code you are actually running)

`src/Uno.CrossTargetting.targets:69-71`:
```
<PropertyGroup Condition="$(IsCrossruntime)">
  <DefineConstants>$(DefineConstants);__CROSSRUNTIME__;UNO_REFERENCE_API</DefineConstants>
</PropertyGroup>
```
`src/Uno.CrossTargetting.targets:77-83` (Skia):
```
__SKIA__;SUPPORTS_RTL;UNO_SUPPORTS_NATIVEHOST;UNO_HAS_ENHANCED_LIFECYCLE
UNO_HAS_MANAGED_POINTERS;UNO_HAS_MANAGED_SCROLL_PRESENTER;HAS_INPUT_INJECTOR
HAS_COMPOSITION_API;UNO_HAS_BORDER_VISUAL
HAS_RENDER_TARGET_BITMAP
```
`src/Uno.CrossTargetting.targets:73-75` (WASM) also defines `UNO_HAS_ENHANCED_LIFECYCLE`.

Consequences that matter for this audit:

| Symbol | Skia | Effect on virtualization |
|---|---|---|
| `UNO_HAS_ENHANCED_LIFECYCLE` | **defined** | All `#if !UNO_HAS_ENHANCED_LIFECYCLE` Uno perf throttles in the ItemsRepeater viewport path are **compiled out** (see §3.3). EffectiveViewportChanged is queued through `EventManager` and drained inside the layout loop. |
| `UNO_REFERENCE_API` | **defined** | `ItemsWrapGrid`/`ItemsWrapGridLayout` are **not compiled** (see §6). `VirtualizingPanelLayout._layouter` is absent (`VirtualizingPanelLayout.cs:34-37`). |
| `UNO_HAS_MANAGED_SCROLL_PRESENTER` | **defined** | `ScrollContentPresenter.Managed.cs` is the scroller; offsets are applied as `Visual.AnchorPoint` (composition), not by re-arranging content. |

---

## 1. Q1 — What runs when `VerticalOffset` changes by 1px

There are **two independent realization pipelines** and they are wired to *different* signals:

* **ListView / ItemsStackPanel (`VirtualizingPanelLayout.managed.cs`)** → driven by
  `ScrollViewer.ViewChanged`. It **does not** use EffectiveViewport at all.
* **ItemsRepeater** → driven by `FrameworkElement.EffectiveViewportChanged`.

### 1.1 Common prefix: SCP applies the offset

`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:263-373`
`Set(...)` clamps and stores `HorizontalOffset`/`VerticalOffset`, then at :358-364 calls
`Update(contentElt, …)`.

`…/ScrollContentPresenter.Managed.cs:413-497` `Update(...)`:
* Immediate path (touch drag / inertia / `DisableAnimation`), :463-470
  ```csharp
  if (options is { DisableAnimation: true } or { IsTouch: true })
  {
      visual.StopAnimation(nameof(Visual.AnchorPoint));
      visual.StopAnimation(nameof(Visual.Scale));
      visual.AnchorPoint = target;
      visual.Scale = targetScale;
      Updated(horizontalOffset, verticalOffset, options.IsIntermediate);
  }
  ```
* Animated path (mouse wheel, programmatic `ChangeView` w/ animation), :471-497 —
  a `Vector2KeyFrameAnimation` on `AnchorPoint`, **`Duration = TimeSpan.FromSeconds(1)`** (:479)
  with `CreatePowerEasingFunction(compositor, Out, 10)` (:474), and a per-frame callback:
  ```csharp
  void OnFrame(CompositionAnimation? _) => Updated(GetAnimatedHorizontalOffset(), GetAnimatedVerticalOffset(), true);
  ```

`…/ScrollContentPresenter.Managed.cs:376-411` `Updated(...)` → `UpdateOffsets(...)`:
```csharp
Scroller?.OnPresenterScrolled(updatedHorizontalOffset, updatedVerticalOffset, isIntermediate);
...
ScrollOffsets = new Point(updatedHorizontalOffset, updatedVerticalOffset);
InvalidateViewport();
```

So a 1px offset delta produces exactly two downstream signals: `OnPresenterScrolled` and
`InvalidateViewport`.

**Thread/frame affinity of `OnFrame`:** animations are ticked from
`src/Uno.UI.Composition/Composition/Compositor.skia.cs:206-222` — *inside* `RenderRootVisual`,
**before** the paint walk at :231:
```csharp
foreach (var animation in _runningAnimations.Keys.ToArray())   // allocates an array per frame
{ ... animation.RaiseAnimationFrame(); ... }
...
rootVisual.RenderRootVisual(canvas, null, damage);
```
`RenderRootVisual` is reached from `SkiaRenderHelper.skia.cs:44`, called by
`CompositionTarget.Rendering.skia.cs:110-124 Render()` which asserts UI-thread
(`NativeDispatcher.CheckThreadAccess()`, :114).
⇒ **The new `AnchorPoint` is used to paint the current frame, but the realization work it triggers
runs on a later dispatcher item.** Content therefore moves one frame before items are realized for
the new position (see §7.1).

### 1.2 `InvalidateViewport` → EffectiveViewport (ItemsRepeater path)

`src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs:255-266`:
```csharp
[NotImplemented]
protected void InvalidateViewport()
{
    if (!IsScrollPort) { throw new InvalidOperationException(...); }
    PropagateEffectiveViewportChange();
}
```

`…/FrameworkElement.EffectiveViewport.cs:338-420 PropagateEffectiveViewportChange`:
1. **:349-353** — early-out if `!IsEffectiveViewportEnabled`
   (`:84` — `_childrenInterestedInViewportUpdates is { Count: > 0 } || _effectiveViewportChanged != null`).
   **For a plain ListView nobody subscribes, so the whole EVP walk costs one bool test per scroll
   tick.**
2. **:363** `GetParentViewport()` → `_parentViewport.GetRelativeTo(this)`.
3. **:364** `GetEffectiveViewport(parentViewport)`. On Skia the scroll port's viewport is the
   *layout slot*, independent of the offsets (`:302-308`):
   ```csharp
   #if __SKIA__ // The viewport on an IsScrollPort element should not be affected by its ScrollOffsets...
       var scrollport = LayoutInformation.GetLayoutSlot(this);
   #else
       var scrollport = new Rect(new Point(ScrollOffsets.X, ScrollOffsets.Y), LayoutInformation.GetLayoutSlot(this).Size);
   #endif
   ```
4. **:365** `viewportUpdated = _lastEffectiveViewport != viewport` — `ViewportInfo.Equals` compares
   only `Effective` (`FrameworkElement.EffectiveViewport.ViewportInfo.cs:83-84`), **exact `Rect`
   equality, no tolerance**.
5. **:379-399** if updated, enqueue the public event:
   ```csharp
   #if UNO_HAS_ENHANCED_LIFECYCLE
       this.GetContext().EventManager.EnqueueForEffectiveViewportChanged(this, new EffectiveViewportChangedEventArgs(parentViewport.Effective));
   #endif
   ```
6. **:403-417** recurse into children:
   ```csharp
   // the ScrollOffsets check is only relevant on skia...
   if (_childrenInterestedInViewportUpdates is { Count: > 0 } && (isInitial || viewportUpdated || _lastScrollOffsets != ScrollOffsets))
   {
       foreach (var child in _childrenInterestedInViewportUpdates)
           child!.OnParentViewportChanged(isInitial, isInternal, this, viewport);
   }
   ```
   Because on Skia the SCP's own effective viewport does **not** change with the offsets (step 3),
   the `_lastScrollOffsets != ScrollOffsets` term is what forces propagation — i.e. **every 1px of
   scroll re-walks the whole EVP-interested subtree.**

Per node in that walk, `ViewportInfo.GetRelativeTo`
(`FrameworkElement.EffectiveViewport.ViewportInfo.cs:42-65`) does:
```csharp
var parentToElement = UIElement.GetTransform(uiElement, usuallyTheParentOfElement).Inverse();
effective = parentToElement.Transform(Effective);
clip      = parentToElement.Transform(Clip);
```
`UIElement.GetTransform` on Skia (`UIElement.cs:644-661`) is the fast path:
```csharp
Matrix4x4.Invert(to.Visual.TotalMatrix /* root2To */, out var to2Root);
from2To = (from.Visual.TotalMatrix * to2Root).ToMatrix3x2();
```
`Visual.TotalMatrix` is cached and invalidated by a dirty flag
(`src/Uno.UI.Composition/Composition/Visual.skia.cs:154-225`), but the flag is set by
`Compositor.skia.cs:258-263`:
```csharp
partial void InvalidateRenderPartial(Visual visual)
{
    visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
    visual.InvalidatePaint();
    visual.CompositionTarget?.RequestNewFrame();
}
```
and `ContainerVisual.SetMatrixDirty` recurses the whole subtree
(`ContainerVisual.skia.cs:212-227`), short-circuiting only if already dirty
(`Visual.skia.cs:140-146`). ⇒ **Setting the SCP content's `AnchorPoint` once per scroll frame dirties
the matrix of every visual under the scroller**, and every `TotalMatrix` read afterwards recomputes.
For a ListView with 20 items × ~10 visuals each that's ~200 nodes walked + up to 200 matrix
recomputes per frame.

### 1.3 EVP delivery is drained *inside* the layout loop

`src/Uno.UI/UI/Xaml/Internal/EventManager.cs:29-35`:
```csharp
internal void EnqueueForEffectiveViewportChanged(FrameworkElement element, EffectiveViewportChangedEventArgs args)
{
    _effectiveViewportChangedQueue.RemoveAll(x => x.Element == element);   // O(n) + closure alloc
    _effectiveViewportChangedQueue.Add((element, args));
    CoreServices.RequestAdditionalFrame();
}
```

`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:67-127`:
```csharp
internal static void RequestAdditionalFrame()
{
    if (GetXamlRoot() is { Bounds: { Width: not 0, Height: not 0 } } &&
        Interlocked.CompareExchange(ref _isAdditionalFrameRequested, 1, 0) == 0)
        NativeDispatcher.Main.Enqueue(static () => OnTick(), NativeDispatcherPriority.Normal);
}
private static void OnTick()
{
    ...
    root.UpdateLayout();
    if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
    { CoreServices.Instance.EventManager.RaiseLoadedEvent(); root.UpdateLayout(); }   // :117-121
#if __SKIA__
    (root.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget)?.OnRenderFrameOpportunity();  // :124
#endif
}
```

`src/Uno.UI/UI/Xaml/UIElement.cs:948-1024` (`InnerUpdateLayout`, `MaxLayoutIterations = 250` at
`UIElement.cs:889`):
```csharp
for (var i = MaxLayoutIterations; i > 0; i--)
{
    if (root.IsMeasureDirtyOrMeasureDirtyPath)      root.Measure(bounds.Size);
    else if (root.IsArrangeDirtyOrArrangeDirtyPath) root.Arrange(bounds);
#if UNO_HAS_ENHANCED_LIFECYCLE
    else if (eventManager.HasPendingViewportChangedEvents) eventManager.RaiseEffectiveViewportChangedEvents();
    else { ... RaiseSizeChangedEvents(); if (dirty again) continue; RaiseLayoutUpdated(); if (clean) return; }
#endif
}
```
So the sequence for one scroll tick with an `ItemsRepeater` is:
`Measure → Arrange → RaiseEVP → (repeater InvalidateMeasure) → Measure → Arrange → RaiseEVP → …`
until the viewport stops changing, capped at 250 iterations, **all inside a single dispatcher item**.

### 1.4 `OnPresenterScrolled` → `ViewChanged` (ListView path)

`src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs:1234-1280`:
```csharp
if (isIntermediate && UpdatesMode != ScrollViewerUpdatesMode.Synchronous) { RequestUpdate(); _snapPointsTimer?.Stop(); }
else { Update(isIntermediate); ... }
```
Default `UpdatesMode`: `src/Uno.UI/FeatureConfiguration.cs:483`
`DefaultUpdatesMode = ScrollViewerUpdatesMode.AsynchronousIdle` (wired via
`ScrollViewer.Uno.cs:39-50`). Note `ScrollViewer.MuxInternal.cs:74` forces
`UpdatesMode = Synchronous` when a `IDirectManipulationStateChangeHandler` is attached.

`ScrollViewer.cs:1301-1316`:
```csharp
private void RequestUpdate()
{
    if (_hasPendingUpdate) return;
    _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (_hasPendingUpdate) Update(isIntermediate: true); });
    _hasPendingUpdate = true;
}
```
`ScrollViewer.cs:1318-1357 Update` sets `HorizontalOffset`/`VerticalOffset` DPs, and at :1328-1337
only invalidates arrange for **non-intermediate** changes:
```csharp
if (!isIntermediate && (oldHorizontalOffset != HorizontalOffset || oldVerticalOffset != VerticalOffset))
{
    // ... Intermediate ticks (DManip-driven inertia / drag) are skipped to match WinUI,
    // which lets the compositor drive offsets during manipulation without re-running layout per frame.
    InvalidateArrange();
}
```
then raises `ViewChanged` (:1356).

⇒ **`ListView` realization runs on a *deferred, coalesced* `CoreDispatcherPriority.Normal` item**,
one per dispatcher turn, never more than one queued at a time.

### 1.5 `ViewChanged` → `VirtualizingPanelLayout.OnScrollChanged`

Subscription: `src/Uno.UI/UI/Xaml/Controls/ListViewBase/VirtualizingPanelLayout.managed.cs:206-214`
(`ScrollViewer.ViewChanged += OnScrollChanged`).

`…/VirtualizingPanelLayout.managed.cs:259-331`:
```csharp
var delta = ScrollOffset - _lastScrollOffset;
var sign = Sign(delta);
var unappliedDelta = Abs(delta);
var fillDirection = sign > 0 ? Forward : Backward;
var isLargeScroll = Abs(delta) > ViewportExtent;

if (isLargeScroll) { unappliedDelta = 1; ClearLines(clearContainer: false); var index = (int)(ScrollOffset / _averageLineHeight); SetDynamicSeed(...); }  // :272-294

while (unappliedDelta > 0)
{
    var scrollIncrement = _scrollAdjustmentForCollectionChanges.HasValue ? ... : GetScrollConsumptionIncrement(fillDirection);   // :298-303
    if (scrollIncrement == 0) break;
    unappliedDelta -= scrollIncrement;
    unappliedDelta = Max(0, unappliedDelta);
    UpdateLayout(extentAdjustment: sign * -unappliedDelta, isScroll: true);                                                      // :313
#if __SKIA__
    (ItemsControl as ListViewBase)?.TryLoadMoreItems(LastVisibleIndex);                                                          // :316
#endif
}

ArrangeElements(_availableSize, ViewportSize);   // :320  — arranges *all* materialized items
UpdateCompleted();                               // :321
if (isLargeScroll) OwnerPanel.InvalidateMeasure();// :327
_lastScrollOffset = ScrollOffset;
```

`GetScrollConsumptionIncrement` (:349-362) returns the *extent of one line* (`GetActualExtent` of the
leading/trailing view) or `_averageLineHeight` if nothing materialized.

⇒ **For a 1px delta the loop runs exactly once** (unapplied 1 − lineHeight ⇒ clamped to 0), doing a
full `UpdateLayout` + `ArrangeElements` + `UpdateCompleted`. For a 200px wheel notch with 44px rows
the loop runs **5 times**, each a full `UnfillLayout`+`FillLayout`+`CorrectForEstimationErrors`.

---

## 2. Q2 — Synchronous vs deferred realization; budgets and throttles

### 2.1 ListView (`VirtualizingPanelLayout.managed.cs`) — **fully synchronous, no budget**

`UpdateLayout` (:462-494) is entirely synchronous:
```csharp
ResetReorderingIndex();
OwnerPanel.ShouldInterceptInvalidate = true;          // :465
UnfillLayout(extentAdjustment ?? 0);                  // :467 → RecycleLine → Generator.RecycleViewForItem
var linesAdded = FillLayout(extentAdjustment ?? 0);   // :468 → AddLine → CreateLine → DequeueViewForItem + AddView(view.Measure)
SetDynamicSeed(null, null);
CorrectForEstimationErrors();                         // :471 — O(realized) rect rewrite
if (!isScroll) UpdateCompleted();
OwnerPanel.ShouldInterceptInvalidate = false;         // :483
if (!isScroll && linesAdded > 0) OwnerPanel.UpdateLayout();  // :492 (not on the scroll path)
```
`AddView` (:1025-1056) is the realization step and it **measures the new container inline**:
```csharp
if (view.Parent == null) OwnerPanel.Children.Add(view);   // :1030 — visual-tree Enter walk
...
view.Measure(slotSize);                                   // :1037 — full subtree measure
```
There is **no time budget, no item-count cap, no yielding**. The only throttles are:
* the "bite-sized increments" loop (§1.5) which bounds *per iteration* work to one line, but still
  runs to completion within the same event;
* `ShouldInterceptInvalidate` (`UIElement.Layout.crossruntime.cs:21-31, 80-85`) which swallows
  `InvalidateMeasure`/`InvalidateArrange` **on the panel itself** so no root layout pass is scheduled
  during the fill/unfill;
* `isLargeScroll` (`> ViewportExtent`), which throws everything away and re-seeds from
  `ScrollOffset / _averageLineHeight` instead of walking (:272-294).

### 2.2 ItemsRepeater — realization synchronous inside the layout pass; *cache growth* deferred

Realization happens inside `FlowLayoutAlgorithm.Generate`
(`FlowLayoutAlgorithm.cs:308-447`, `EnsureElementRealized` at :340, `MeasureElement` at :342),
i.e. synchronously inside `ItemsRepeater.MeasureOverride` → `layout.Measure(...)`
(`ItemsRepeater.cs:254`). No budget there either.

Two deferral mechanisms exist:

**(a) Cache-buffer inflation at idle** —
`src/Uno.UI/UI/Xaml/Controls/Repeater/ViewportManagerWithPlatformFeatures.cs:318-352`:
```csharp
double maximumHorizontalCacheBufferPerSide = m_maximumHorizontalCacheLength * m_visibleWindow.Width / 2.0;
double maximumVerticalCacheBufferPerSide   = m_maximumVerticalCacheLength   * m_visibleWindow.Height / 2.0;
bool continueBuildingCache = m_horizontalCacheBufferPerSide < maximumHorizontalCacheBufferPerSide || ...;
if (continueBuildingCache)
{
    m_horizontalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;   // :340  (= 40.0, :25)
    m_verticalCacheBufferPerSide   += CacheBufferPerSideInflationPixelDelta;   // :341
    ...
    RegisterCacheBuildWork();                                                  // :348
}
```
`RegisterCacheBuildWork` (:631-645):
```csharp
var strongOwner = m_owner;                                   // assigned, never used (dead in the C# port)
m_cacheBuildAction = m_owner.Dispatcher.RunIdleAsync(_ => OnCacheBuildActionCompleted());
```
`OnCacheBuildActionCompleted` (:489-496) → `m_owner.InvalidateMeasure()`.

`CoreDispatcher.RunIdleAsync` (`src/Uno.UWP/UI/Core/CoreDispatcher.cs:81-82`) →
`NativeDispatcher.EnqueueIdleOperation` (`src/Uno.UI.Dispatching/Native/NativeDispatcher.cs:475-476`)
→ priority `Idle` (=3, `NativeDispatcherPriority.cs:3-9`). `DispatchItems`
(`NativeDispatcher.cs:128-177`) drains strictly by priority `for (var p = 0; p <= 3; p++)` and
**dequeues exactly one action per native tick**. ⇒ during continuous scrolling the idle cache build
is starved (good), and when scrolling stops it fires repeatedly.

**Cost of the post-scroll burst**: each idle callback → `InvalidateMeasure` → measure+arrange →
`OnOwnerArranged` grows the buffer by 40px → registers another idle callback. To reach the maximum
`2.0 × viewportHeight / 2 = viewportHeight` per side, that is `viewportHeight / 40` full
measure+arrange passes of the repeater. **For an 800px viewport: 20 full layout passes after every
scroll stop** (and `ResetCacheBuffer`, :611-621, zeroes the buffer on every `OnLayoutChanged`, and
on every `Horizontal/VerticalCacheLength` set, :124-150).

**(b) `BuildTreeScheduler` / `Phaser` (x:Phase only)** —
`src/Uno.UI/UI/Xaml/Controls/Repeater/BuildTreeScheduler.cs:14`
```csharp
private const double m_budgetInMs = 40.0;
```
`:55` `ShouldYield() => m_timer.ElapsedMilliseconds > m_budgetInMs`, drained from
`CompositionTarget.Rendering` (:57-84), sorting the pending list every tick (`:63`
`m_pendingWork.Sort(...)` with a lambda — delegate alloc per tick).
This is byte-for-byte the WinUI constant (`controls/dev/Repeater/BuildTreeScheduler.cpp:11`:
`double BuildTreeScheduler::m_budgetInMs = 40.0;`) — **a 40 ms budget is ~2.4 dropped frames at
60 Hz and ~4.8 at 120 Hz**. It only applies to `x:Phase` work, so most apps never hit it.

---

## 3. Q3 — Effective-viewport buffer / cache length constants, and whether they are honoured on Skia

### 3.1 ListView / ItemsStackPanel

`src/Uno.UI/UI/Xaml/Controls/ListViewBase/VirtualizingPanelLayout.managed.cs:155-183`:
```csharp
internal const double ExtendedViewportScaling = 0.5;
private double ViewportExtension => CacheLength * ViewportExtent * ExtendedViewportScaling;
private double ExtendedViewportStart { get { var unclampedStart = ViewportStart - ViewportExtension; return Math.Max(unclampedStart, 0); } }
private double ExtendedViewportEnd  => ViewportEnd + ViewportExtension;
```

`CacheLength` on the layout is generated by the DP mixin generator with default **4.0**:
`src/SourceGenerators/Uno.UI.SourceGenerators.Internal/Mixins/DependencyPropertyMixinGenerator.cs:267-273`
```csharp
new ClassDefinition("VirtualizingPanelLayout", "true", "public", new[] {
    ...
    new PropertyDefinition("CacheLength", "double", "4.0"),
}),
```
…but it is overwritten by the panel. `src/Uno.UI/UI/Xaml/Controls/ItemsStackPanel/ItemsStackPanel.cs:30-41,49-60`:
```csharp
public ItemsStackPanel()
{
    if (FeatureConfiguration.ListViewBase.DefaultCacheLength.HasValue)
        CacheLength = FeatureConfiguration.ListViewBase.DefaultCacheLength.Value;
}
...
_layout.BindToEquivalentProperty(this, nameof(CacheLength));   // :58 — OneWay binding panel → layout
```
`src/Uno.UI/FeatureConfiguration.cs:318-323`:
```csharp
/// Sets the value to use for ItemsStackPanel.CacheLength and ItemsWrapGrid.CacheLength if not set
/// explicitly... Setting this to null will leave the default value at the UWP default of 4.0.
public static double? DefaultCacheLength { get; set; } = 1.0;
```

**Effective Skia default: `CacheLength = 1.0` ⇒ `ViewportExtension = 0.5 × viewport` on each side**
(vs WinUI's documented 4.0 ⇒ 2 viewports each side). So Uno pre-realizes **4× less** than WinUI by
default. That is the single biggest lever for "blank strip at the leading edge while flinging".

Also note the DP itself is a `[NotImplemented]` stub on Skia with default `0.0`:
`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/ItemsStackPanel.cs:12-19`
```csharp
#if __SKIA__ || __NETSTD_REFERENCE__
[global::Uno.NotImplemented("__SKIA__", "__NETSTD_REFERENCE__")]
public static global::Microsoft.UI.Xaml.DependencyProperty CacheLengthProperty { get; } =
    DependencyProperty.Register(nameof(CacheLength), typeof(double), typeof(ItemsStackPanel),
        new FrameworkPropertyMetadata(default(double)));
#endif
```
⇒ if an app sets `FeatureConfiguration.ListViewBase.DefaultCacheLength = null` "to get the UWP
default of 4.0", on Skia the panel DP stays at **0.0**, the OneWay binding pushes 0.0 onto the
layout, and **the buffer collapses to zero**. That is a latent footgun, not the documented behaviour.

Secondary constant — the *container pool* size, `VirtualizingPanelLayout.managed.cs:424-434`:
```csharp
if (_generator != null && _averageLineHeight > 0)
{
    var cacheLimit = (int)(ViewportExtent / _averageLineHeight) * 2;
    _generator.CacheLimit = cacheLimit;
}
```
clamped by `VirtualizingPanelGenerator.managed.cs:24-29,65-74`:
```csharp
private const int MaxCacheLimit = 1024;
private const int MinCacheLimit = 10;
...
_cacheLimit = Math.Max(Math.Min(value, MaxCacheLimit), MinCacheLimit);
PurgeCache();
```
Recomputed **on every `ArrangeOverride`**, and every set calls `PurgeCache()` (:76-85) which walks all
template buckets.

### 3.2 ItemsRepeater

`src/Uno.UI/UI/Xaml/Controls/Repeater/ViewportManagerWithPlatformFeatures.cs:21-25,53-57`:
```csharp
// Pixel delta by which to inflate the cache buffer on each side...
private const double CacheBufferPerSideInflationPixelDelta = 40.0;
...
private double m_maximumHorizontalCacheLength = 2.0;
private double m_maximumVerticalCacheLength = 2.0;
```
Realization window (:199-211):
```csharp
var realizationWindow = GetLayoutVisibleWindow();
if (HasScroller)
{
    realizationWindow.X -= (float)(m_horizontalCacheBufferPerSide);
    realizationWindow.Y -= (float)(m_verticalCacheBufferPerSide);
    realizationWindow.Width  += (float)(m_horizontalCacheBufferPerSide) * 2.0f;
    realizationWindow.Height += (float)(m_verticalCacheBufferPerSide) * 2.0f;
}
```
Both match WinUI exactly (`controls/dev/Repeater/ViewportManager.cpp:15`, `:553-575`).

**Honoured on Skia?** Yes, but the *steady-state* buffer is 0 immediately after any layout change
and only reaches `viewportHeight` per side after `viewportHeight/40` idle round-trips (§2.2(a)).
During a sustained fling, idle never runs, so **the repeater effectively scrolls with a
near-zero cache buffer** unless the fling started long after the last layout change.

### 3.3 The Uno viewport-change throttle is dead code on Skia

`ViewportManagerWithPlatformFeatures.cs:570-609`:
```csharp
void UpdateViewport(Rect viewport)
{
    ...
    m_visibleWindow = currentVisibleWindow;

#if !UNO_HAS_ENHANCED_LIFECYCLE
    // Uno workaround [BEGIN]: For perf considerations, do not invalidate the tree on each viewport update
    // (Viewport updates are quite frequent, this would cause lot of unnecessary layout pass which would impact scroll perf, especially on Android).
    if (m_owner.Layout is VirtualizingLayout vl
        && vl.IsSignificantViewportChange(m_owner.LayoutState, _uno_viewportUsedInLastMeasure, m_visibleWindow))
    // Uno workaround [END]
#endif
    {
        TryInvalidateMeasure();
    }
}
```
`_uno_viewportUsedInLastMeasure` itself is `#if !UNO_HAS_ENHANCED_LIFECYCLE` (:72-74, set at :312-315).
The throttle implementations:
* `VirtualizingLayout.cs:48-56` — base: `const double delta = 50;` (any axis moves > 50px).
* `StackLayout.cs:423-454` — `minDelta = Math.Min(Uno_LastKnownAverageElementSize * 5, viewportSize)`.
* `FlowLayout.cs:451-461` — analogous.

**On Skia and WASM (`UNO_HAS_ENHANCED_LIFECYCLE` defined) these are compiled out.** Every
`EffectiveViewportChanged` — i.e. every sub-pixel viewport move — calls `TryInvalidateMeasure()`
(:647-663) → `m_owner.InvalidateMeasure()`.

WinUI does **not** invalidate unconditionally either; it has a rounding tolerance
(`controls/dev/Repeater/ViewportManager.cpp:1014-1027`):
```cpp
const float roundingTolerance = 0.01f;
if (std::abs(m_visibleWindow.X - effectiveViewport.X) > roundingTolerance || ... )
{
    SetVisibleWindow(effectiveViewport);
    TryInvalidateMeasure();
    return true;
}
```
Uno's port dropped the tolerance and, on enhanced-lifecycle platforms, the Uno substitute too.
(In practice the exact-`Rect` equality guard in `PropagateEffectiveViewportChange:365` prevents a
no-op event, so the observable difference is sub-pixel churn, not a hard loop.)

---

## 4. Q4 — Measure/arrange passes per scroll frame, 20 realized items

### 4.1 ListView + ItemsStackPanel, steady state (no line boundary crossed)

Per `ViewChanged` (one coalesced `Normal`-priority dispatcher item per turn):

| Step | Cost for 20 items |
|---|---|
| `UnfillLayout` (:577-605) | 2 while-condition evaluations, 0 recycles |
| `FillLayout` (:518-568) | `GetItemsStart/End` + 2 `GetNextUnmaterializedItem`, 0 adds |
| `CorrectForEstimationErrors` (:634-664) | 1 `GetMeasuredStart`; the rewrite loop runs only when `neededCorrection != 0` — but see §7.4 |
| `ArrangeElements` (:439-453) | **20 × (`GetBoundsForElement` + `GetElementArrangeBounds` + `container.Arrange(...)`)** |
| `UpdateCompleted` (:499-508) | `ClearScrappedViews()` (empty) + `UpdateVisibilities()` — walks the **entire** container cache (≤ `CacheLimit`, up to 1024) setting `Visibility = Collapsed` |

`container.Arrange(bounds)` short-circuits when the rect is unchanged
(`UIElement.Layout.crossruntime.cs:362-366`):
```csharp
if (firstArrangeDone && !IsArrangeDirtyOrArrangeDirtyPath && finalRect == m_finalRect)
{ ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath); return; }
```
and during pure scrolling the item rects in panel space **do not change** (the offset is applied on
the SCP content's `Visual.AnchorPoint`, not by re-arranging). So:

> **Steady-state ListView scroll frame = 0 full measure passes, 0 full arrange passes, 0 element
> measures, 20 short-circuited `Arrange` calls, plus one full walk of the recycle cache.**

The root layout loop (`UIElement.cs:948-955`) returns immediately when nothing is dirty. There is no
`RequestAdditionalFrame` from the ListView path at all, so the frame is driven purely by the
compositor animation.

### 4.2 ListView, frame that crosses a line boundary (~once every `itemHeight` px)

Add, per line:
* `RecycleLine` → `Generator.RecycleViewForItem` (`VirtualizingPanelGenerator.managed.cs:176-226`):
  `GetItemId` dict lookup, `FindOrCreate` on `_itemContainerCache`, `container.PrepareForRecycle()`
  (**full subtree walk**, `UIElement.cs:1321-1329`), and `parent.Children.Remove(container)` (:203-206)
  → `OnChildRemoved` (`UIElement.crossruntime.cs:118-127`) → `child.Shutdown()` +
  `ClearInheritedDataContext()` + **`child.Leave(leaveParams)` full subtree Leave walk**.
* `AddLine`→`CreateLine` (`ItemsStackPanelLayout.managed.cs:14-27`) → `Generator.DequeueViewForItem`
  (:90-129: scrap dict, id dict, per-template `Stack<FrameworkElement>` pop, `Visibility = Visible`),
  `ItemsControl.PrepareContainerForIndex` (`ItemsControl.cs:1353-1365`) →
  `PrepareContainerForItemOverride` (`ItemsControl.cs:1258-1322`: sets `ContentTemplate`,
  `ContentTemplateSelector`, `DataContext`, possibly `SetBinding(ContentProperty, new Binding())`) →
  `ListViewBase.PrepareContainerForItemOverride` (`ListViewBase.cs:1114-1125`) →
  `selectorItem.UpdateMultiSelectStates(...)` → `VisualStateManager.GoToState`
  (`SelectorItem.cs:285-303`) → `ContainerPreparedForItem` (`ListViewBase.cs:1127-1134`).
* `AddView` (:1025-1056): `OwnerPanel.Children.Add(view)` → **full `EnterImpl` walk of the container
  subtree** (`UIElement.mux.cs:1142-1290` → `DependencyObjectStore.mux.cs:149-219` →
  `EnterProperties` at `DependencyObjectStore.PropertySystem.mux.cs:45-105`, which enumerates
  `_properties.GetAllDetails()` and `GetValue(...)` per property, per node, plus
  `EstablishThemeOnEnterCore`), then `view.Measure(slotSize)` — **full measure of the whole item
  template**.
* `OnChildAdded` (`UIElement.crossruntime.cs:94-116`) →
  `eventManager.RequestRaiseLoadedEventOnNextTick()` → `RequestAdditionalFrame()` ⇒ on the next
  dispatcher tick, `CoreServices.OnTick` runs **`root.UpdateLayout()`, then `RaiseLoadedEvent()`,
  then `root.UpdateLayout()` again** (`CoreServices.cs:115-121`). `RaiseLoaded` runs
  `SelectorItem.OnLoaded` → `UpdateVisualStates(true)` (`SelectorItem.cs:356-361`) — **visual-state
  transitions with animations, for every newly realized container**.

> **Line-crossing ListView frame ≈ 1 recycle (2 subtree walks) + 1 realize (1 subtree Enter walk +
> 1 full template measure + template/binding/VSM setup) + 2 extra root `UpdateLayout()` passes on the
> next tick.**

### 4.3 ItemsRepeater + StackLayout, 20 realized items

Per EVP event, inside one `OnTick`/`UpdateLayout` (`UIElement.cs:959-1024`):

1. `root.Measure` → … → `ItemsRepeater.MeasureOverride` (`ItemsRepeater.cs:198-282`)
   * `m_viewManager.PrunePinnedElements()` (:232)
   * `layout.Measure(...)` → `FlowLayoutAlgorithm.Measure` (`FlowLayoutAlgorithm.cs:61-115`):
     `m_elementManager.OnBeginMeasure` → `DiscardElementsOutsideWindow` (`ElementManager.cs:73-103`),
     `GetAnchorIndex` (:171-305), `Generate(Forward)` + `Generate(Backward)` (:98-99),
     `EstimateExtent` (:111).
     `MeasureElement` (:159-167) calls `element.Measure(measureSize)` **for every realized element,
     every pass** — 20 calls, short-circuited by `DoMeasure` (`UIElement.Layout.crossruntime.cs:213-228`)
     unless dirty or `availableSize` changed.
   * children loop (:260-277): `Children.Count` iterations of `GetVirtualizationInfo(element)`
     (attached-DP `GetValue`, `ItemsRepeater.cs:479-483`). **`Children` includes recycled elements
     parked offscreen** (see §5.4), so this is O(realized + pool), not O(realized).
   * `m_viewportManager.SetLayoutExtent(extent)` (:280) → if the extent moved > 1px, hooks
     `LayoutUpdated` and calls `(m_scroller as UIElement).InvalidateArrange()`
     (`ViewportManagerWithPlatformFeatures.cs:226-259`).
2. `root.Arrange` → `ItemsRepeater.ArrangeOverride` (`ItemsRepeater.cs:284-349`)
   * `layout.Arrange(...)` → `FlowLayoutAlgorithm.ArrangeVirtualizingLayout` (:588-632) →
     `PerformLineAlignment` (:636-723) → **`element.Arrange(bounds)` for all 20**, with
     `bounds.X -= m_lastExtent.X; bounds.Y -= m_lastExtent.Y;` (:706-707). Because `m_lastExtent`
     (the layout origin) is recomputed from the running average each measure, these rects **do**
     change ⇒ arrange is *not* short-circuited (see §7.4).
   * `m_viewManager.OnOwnerArranged()` (:309), children loop (:311-344) with another
     `GetVirtualizationInfo` per child, `m_viewportManager.OnOwnerArranged()` (:346) → cache
     inflation + idle registration, `m_transitionManager.OnOwnerArranged()` (:347).
3. `RaiseEffectiveViewportChangedEvents` (`UIElement.cs:980-983`) — if arrange changed any clip, more
   EVP events were enqueued during arrange (`UIElement.skia.cs:327-346` → `ApplyClip` →
   `OnViewportUpdated` → `PropagateEffectiveViewportChange`), which can re-invalidate measure and
   loop again.

> **Minimum ItemsRepeater scroll frame = 1 repeater measure + 1 repeater arrange + 20 element
> `Measure` + 20 element `Arrange`, all inside the root layout loop; realistically 2–3 loop
> iterations. Bounded only by `MaxLayoutIterations = 250` (`UIElement.cs:889`).**
> Plus, after the scroll stops, `viewportHeight/40` more full passes from the idle cache build.

---

## 5. Q5 — Allocations on the per-scroll-frame path

### 5.1 Composition / frame loop

* `src/Uno.UI.Composition/Composition/Compositor.skia.cs:206`
  `foreach (var animation in _runningAnimations.Keys.ToArray())` — **one `CompositionAnimation[]` per
  rendered frame**, whenever any animation is running (i.e. throughout every wheel scroll).
* `src/Uno.UI/UI/Xaml/Internal/EventManager.cs:31`
  `_effectiveViewportChangedQueue.RemoveAll(x => x.Element == element)` — **closure allocation per
  enqueue** (captures `element`), plus an O(queue) scan.
* `src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs:384/390/397`
  `new EffectiveViewportChangedEventArgs(...)` per propagated node per tick.
* `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs:1356`
  `new ScrollViewerViewChangedEventArgs { … }` per `Update`.
* `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs:1308`
  `Dispatcher.RunAsync(..., () => …)` — closure + `UIAsyncOperation` per deferred update.

### 5.2 ListView layout — LINQ and iterators, several per scroll event

`Deque<T>.GetEnumerator` is a **`yield return` iterator** ⇒ heap allocation on every `foreach`:
`src/Uno.UI/UI/Xaml/Controls/ListViewBase/Deque.cs:287-294`
```csharp
public IEnumerator<T> GetEnumerator()
{
    int count = Count;
    for (int i = 0; i != count; ++i) yield return DoGetItem(i);
}
```
Call sites reached per scroll event / layout pass:

| Site | Code | Allocations |
|---|---|---|
| `VirtualizingPanelLayout.managed.cs:441` | `foreach (var line in _materializedLines)` in `ArrangeElements` | 1 iterator (×2 per tick: `OnScrollChanged` + `ArrangeOverride`) |
| `:653` | `foreach (var line in _materializedLines)` in `CorrectForEstimationErrors` | 1 iterator per `UpdateLayout` |
| `:821-824` | `_materializedLines.Select(l => GetMeasuredExtent(l.FirstView)).Average()` | iterator + `Select` iterator + lambda closure (`this` capture) — called from `MeasureOverride` (:389) **and** `EstimatePanelExtent` (:806), which itself runs in both `MeasureOverride` (:399) and `ArrangeOverride` (:436) ⇒ **3× per layout pass** |
| `:826-827` | `_materializedLines.Select(l => GetDesiredBreadth(l.FirstView)).MaxOrDefault()` | same, per `EstimatePanelSize` |
| `:829-831` | `_materializedLines.Select(l => GetActualBreadth(l.FirstView)).MaxOrDefault()` | same |
| `:933-936` | `_materializedLines.SelectMany(line => line.Items).Select(x => x.index).Min()` | 3 iterators + 2 closures — **every `MeasureOverride`** (`ScrapLayout`) |
| `:541` | `_materializedLines.None(line => line.Contains(reorderIndex))` | iterator + closure, only when reordering |
| `:1431-1432` | `Line.Contains` → `Items.Any(i => i.index == index)` | closure per call |
| `:948-951` | `foreach (var line in _materializedLines) ScrapLine(line);` | 1 iterator per `MeasureOverride` |
| `:907` | `var lines = _materializedLines.ToArray();` in `ClearLines` | array — on every large-scroll (:288) |
| `:1297-1300` | `_pendingCollectionChanges.Any(x => …)` | closure, `ScrollIntoView` only |

`VirtualizingPanelGenerator.managed.cs:287,304` — `_scrapCache.ToList()` / `_idCache.ToList()` in
`UpdateForCollectionChanges` (collection-change path, not per-frame, but O(n) allocations).

`VirtualizingPanelLayout.managed.cs:1201-1209` — `GetMethodTag()` and `GetDebugInfo()` build
interpolated strings, but every call site is guarded by `this.Log().IsEnabled(LogLevel.Debug)`
(e.g. :261-264, :381-384, :473-476), so they do not allocate when logging is off. ✔

### 5.3 ListView dictionary lookups per realize/recycle

`VirtualizingPanelGenerator.managed.cs:42-53`
```csharp
private readonly Dictionary<int, Stack<FrameworkElement>> _itemContainerCache = new ...;
private readonly Dictionary<int, int> _idCache = new ...;
private readonly Dictionary<int, FrameworkElement> _scrapCache = new ...;
```
Per realized item: `_scrapCache.TryGetValue` (+`Remove`) (:158-168), `_idCache.TryGetValue`
(:321) or `_idCache.Add` after `ResolveItemTemplate`/`IsItemItsOwnContainer` (:325-330),
`_itemContainerCache.TryGetValue` + `Stack.Pop` (:142-150).
Per recycled item: `GetItemId` (:178) + `_itemContainerCache.FindOrCreate(id, () => new Stack<…>())`
(:187 — the lambda is capture-free so it's cached by the compiler) + `Stack.Push`.

`UpdateVisibilities` (:269-280) iterates **every** bucket and every pooled container on every
`UpdateCompleted()` (i.e. every scroll event), writing `view.Visibility = Visibility.Collapsed`:
```csharp
foreach (var cache in _itemContainerCache)
    foreach (var view in cache.Value)
        view.Visibility = Visibility.Collapsed;
```
Note the comment at :275-277 ("We prefer not to unload it because recycling is cheaper if it stays in
the visual tree") no longer matches the code — `RecycleViewForItem` at :203-206 **does** remove the
container from `Panel.Children`. So this loop is per-frame `SetValue` traffic on detached elements.

### 5.4 ItemsRepeater allocations

* `ItemsRepeater.cs:230` and `:297` — **two `Disposable.Create(() => m_isLayoutInProgress = false)` per
  layout pass** (closure display-class + `AnonymousDisposable`), one in `MeasureOverride`, one in
  `ArrangeOverride`. WinUI uses a stack scope guard.
* `ViewManager.cs:711-754` `GetElementFromElementFactory` — the local function `GetElement()` captures
  `data`/`index` (display class) and contains
  ```csharp
  using var scopeGuard = Disposable.Create(() => { args.Data = null; args.Parent = null; });
  ```
  ⇒ **2 allocations per newly realized element**.
* `ItemsRepeater.cs:479-483` `TryGetVirtualizationInfo` → `element.GetValue(VirtualizationInfoProperty)`.
  Uno's DP store is a sparse `DependencyPropertyDetails?[]` with `short[]` offsets, not a dictionary
  (`src/Uno.UI/UI/Xaml/DependencyPropertyDetailsCollection.cs:22-30`), so this is an indexed lookup —
  but it is executed **twice per child per layout pass** (measure loop :268, arrange loop :319) plus
  inside `GetElementIfAlreadyHeldByLayout` (`ViewManager.cs:613-636`).
* `RepeaterLayoutContext.cs:13-18,75-78` — every context call resolves the owner through
  `WeakReference<ItemsRepeater>.TryGetTarget`:
  ```csharp
  private readonly WeakReference<ItemsRepeater> m_owner;
  ItemsRepeater GetOwner() => m_owner.TryGetTarget(out var owner) ? owner : throw …;
  ```
  `RealizationRectCore`/`VisibleRectCore`/`ItemCountCore`/`RecommendedAnchorIndexCore` are read many
  times per measure pass (e.g. `FlowLayoutAlgorithm.cs:76, 82, 189, 198, 336`), so this is
  O(realized) GC-handle dereferences per pass.
* `ItemsRepeater.SuggestedAnchor` (`ViewportManagerWithPlatformFeatures.cs:87-122`) walks up the
  visual tree from `m_scroller.CurrentAnchor` on every `RecommendedAnchorIndex` read
  (`RepeaterLayoutContext.cs:51-65`), which happens at least twice per measure
  (`FlowLayoutAlgorithm.cs:82` and `:198`).
* `BuildTreeScheduler.cs:63` — `m_pendingWork.Sort((lhs, rhs) => lhs.Priority - rhs.Priority)`
  allocates a `Comparison<WorkInfo>` delegate on every `Rendering` tick while phasing is active.
* `UIElement.cs:593-594` — `TransformToVisual` allocates a `MatrixTransform` (a `DependencyObject`,
  i.e. a whole DP store) per call. Used by `ScrollViewer.Anchoring.cs:405-423 GetDescendantBounds`,
  which runs **once per anchor candidate** inside `EnsureAnchorElementSelection`
  (`ScrollViewer.Anchoring.cs:290-348`) whenever `Horizontal/VerticalAnchorRatio` is not `NaN`.
  Each realized `ItemsRepeater` child registers itself as an anchor candidate
  (`ViewportManagerWithPlatformFeatures.cs:293-303` sets `CanBeScrollAnchor = true` →
  `UIElement.mux.cs:1350-1353` `UpdateAnchorCandidateOnParentScrollProvider(add: true)` →
  `ScrollViewer.Anchoring.cs:99-109` appends to `m_anchorCandidates`).

### 5.5 Where the *real* cost is (not allocations)

The dominant per-realization cost on the ListView path is the visual-tree **Enter/Leave** walk, not
LINQ:

`UIElement.mux.cs:1242` → `DependencyObjectStore.mux.cs:149-219`:
```csharp
if (@params.IsLive) { EnterProperties(namescopeOwner, @params); }   // :194-197
EnterSparseProperties(...);                                        // :199
if (IsActive) { EstablishThemeOnEnterCore(@params); }              // :207-210
```
`DependencyObjectStore.PropertySystem.mux.cs:57-64`:
```csharp
foreach (var propertyDetail in _properties.GetAllDetails())
{
    if (!ShouldEnterLeaveProperty(propertyDetail)) continue;
    var propertyValue = GetValue(propertyDetail!);
    ...
}
```
…recursed over every node of the item template, on *both* `Children.Add` and `Children.Remove`.
`UIElement.mux.cs:1280-1281` recurses `foreach (var child in _children)`.

**ItemsRepeater does not pay this**: `RecyclePool.PutElementCore`
(`src/Uno.UI/UI/Xaml/Controls/Repeater/RecyclePool.cs:47-65`) records the owner panel and
`TryGetElementCore` (:67-120) explicitly prefers a same-owner element
("*Prefer an element from the same owner or with no owner so that we don't incur the enter/leave cost
during recycling*", :75-77), and `ItemsRepeater.ArrangeOverride:322-331` parks pooled elements at
`ClearedElementsArrangePosition` (`ItemsRepeater.cs:65`, `(-10000, -10000)`) with size 0 instead of
detaching them.
Trade-off: `Children` grows monotonically with the pool (the pool is uncapped), so the two per-pass
children loops in `MeasureOverride`/`ArrangeOverride` are O(realized + pool).

---

## 6. GridView on Skia is **not virtualized at all**

`src/Uno.UI/UI/Xaml/Controls/ItemsWrapGrid/ItemsWrapGrid.cs:1`
```csharp
#if !IS_UNIT_TESTS && !UNO_REFERENCE_API
```
`src/Uno.UI/UI/Xaml/Controls/ItemsWrapGrid/ItemsWrapGridLayout.cs:1` — same guard.
`UNO_REFERENCE_API` is defined for all cross-runtime targets (`Uno.CrossTargetting.targets:69-71`),
so on Skia/WASM the only `ItemsWrapGrid` that exists is the generated stub:

`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/ItemsWrapGrid.cs:6-9`
```csharp
#if __SKIA__ || __NETSTD_REFERENCE__
	[global::Uno.NotImplemented]
#endif
	public partial class ItemsWrapGrid : global::Microsoft.UI.Xaml.Controls.Panel
```
— a bare `Panel`, **not** `IVirtualizingPanel`.

The default GridView style branches on type presence
(`src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml:18-19, 4276-4283`):
```xml
xmlns:itemswrapgridpresent   ="...?IsTypePresent(Microsoft.UI.Xaml.Controls.ItemsWrapGrid)"
xmlns:itemswrapgridnotpresent="...?IsTypeNotPresent(Microsoft.UI.Xaml.Controls.ItemsWrapGrid)"
...
<Setter Property="ItemsPanel">
  <Setter.Value>
    <ItemsPanelTemplate>
      <itemswrapgridpresent:ItemsWrapGrid Orientation="Horizontal" />
      <itemswrapgridnotpresent:WrapPanel />
    </ItemsPanelTemplate>
  </Setter.Value>
</Setter>
```
and `IsTypePresent` treats `[NotImplemented]` as absent
(`src/SourceGenerators/Uno.UI.SourceGenerators/ApiInformation.cs:16-30`):
```csharp
if (typeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "NotImplementedAttribute")) return false;
```
⇒ **On Skia, `GridView`'s default `ItemsPanel` is `WrapPanel`**
(`src/Uno.UI/UI/Xaml/Controls/WrapPanel/WrapPanel.cs:7` — `public partial class WrapPanel : Panel`,
measure/arrange at `WrapPanel.Layout.cs:19` and `:95`, no `IVirtualizingPanel`).
Every item is materialized, measured and arranged. A 5 000-item GridView builds 5 000 containers.

`ListView` is fine: `Generic.xaml:4195-4200` uses `<ItemsStackPanel Orientation="Vertical" />`, and
`ItemsStackPanel` is *not* `[NotImplemented]` on Skia
(`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/ItemsStackPanel.cs:6-8` — `#if false || false`).

**UNVERIFIED**: whether apps in practice hit this (they may always set an explicit `ItemsPanel`), and
whether `WrapPanel` reflows correctly for GridView semantics. Not runtime-validated here.

---

## 7. Q7 — Concrete virtualization-caused jank sources, with proof

### 7.1 Realization always lags the visual by ≥ 1 frame (blank leading edge)

*Proof chain*: `Compositor.skia.cs:206-231` ticks the `AnchorPoint` animation and paints in the same
`Render()` call → `ScrollContentPresenter.Managed.cs:481` `OnFrame` → `:376-393 Updated` →
either `Dispatcher.RunAsync(Normal)` (`ScrollViewer.cs:1308`) for the ListView path or
`RequestAdditionalFrame` → `NativeDispatcher.Main.Enqueue(OnTick, Normal)`
(`CoreServices.cs:73`) for the repeater path. `NativeDispatcher.DispatchItems`
(`NativeDispatcher.cs:128-177`) runs **one queued action per native tick**.
⇒ The pixels move first; the items catch up on a later dispatcher turn.

Amplifier: the wheel path uses a 1 s power-out animation
(`ScrollContentPresenter.Managed.cs:474-479`) whose first step covers a large fraction of the
remaining distance. The code itself documents this
(`ScrollContentPresenter.cs:302-310`):
> "*the animation's first step jumps (target-visual)\*0.149 pixels, causing a blank frame before items
> can be realized for the new position*"

With `CacheLength = 1.0` (§3.1) the buffer is only `0.5 × viewport`, so a single wheel notch can
outrun the realized range.

### 7.2 ItemsRepeater cache buffer is 0 during a fling and rebuilds in 40px idle steps

`ViewportManagerWithPlatformFeatures.cs:611-621 ResetCacheBuffer` zeroes both buffers on
`OnLayoutChanged` (:262-291) and on any `CacheLength` set (:124-150); regrowth is +40px per arrange
gated on an **Idle**-priority dispatcher item (:340-348, :631-645). Idle is the lowest of four queues
(`NativeDispatcher.cs:138-169`), so during continuous scrolling it never runs.
⇒ Fling with an empty buffer ⇒ realization is strictly reactive ⇒ blank strips.
⇒ Scroll stop ⇒ `viewportHeight/40` extra full measure+arrange passes (20 for an 800px viewport),
each realizing more items — a visible post-scroll hitch.

### 7.3 On Skia there is no viewport-change throttle for ItemsRepeater

`ViewportManagerWithPlatformFeatures.cs:599-608` — the Uno throttle is `#if !UNO_HAS_ENHANCED_LIFECYCLE`
and Skia defines that symbol. WinUI's own 0.01f tolerance
(`microsoft-ui-xaml2/controls/dev/Repeater/ViewportManager.cpp:1014-1019`) was not ported.
⇒ Every effective-viewport delta (including sub-pixel from `LayoutRounding`/DPI) invalidates measure
on the repeater, and the layout loop can iterate several times per tick.

### 7.4 Layout-origin drift makes *all* realized items re-arrange (documented item jumping)

`StackLayout.cs:186-232` (the Uno workaround comment is itself the proof):
```
// WinUI parity formula for extent.MajorStart (the layout origin) is:
//   firstRealizedLayoutBounds.MajorStart - firstRealizedItemIndex * averageElementSize
// ... averageElementSize is recomputed each measure from a 100-slot running mean, so the formula
// drifts a few pixels whenever a tall item enters or leaves the buffer. Items' IR-local Y is
// (algorithm Y - layout origin Y), so even a small drift in the origin shows up as items "jumping"
// during wheel scroll on chat-style lists with high-variance heights (issue #23042 / studio.live#816).
```
The 100-slot mean: `StackLayoutState.cs:12-15,64-78` (`BufferSize = 100`, ring buffer keyed by
`elementIndex % 100`).
Arrange consumes the origin at `FlowLayoutAlgorithm.cs:706-707`:
```csharp
bounds.X -= m_lastExtent.X;
bounds.Y -= m_lastExtent.Y;
...
element.Arrange(bounds);   // :721
```
⇒ when the origin drifts, **every** realized element gets a different `finalRect`, so
`UIElement.Arrange`'s early-out (`UIElement.Layout.crossruntime.cs:362-366`) fails, and each element
re-arranges → `OnArrangeVisual` + `OnViewportUpdated` (`UIElement.skia.cs:327-346`) → new damage,
new EVP events → potentially another layout iteration.
The mitigation (`StackLayout.cs:204-232`, `Uno_LastReportedExtentMajorStart`) pins the origin but is
released when `firstRealizedItemIndex == 0` or when items land above it (:215-220), so the jump
returns at those boundaries.

The ListView path has the same class of problem in
`VirtualizingPanelLayout.managed.cs:634-664 CorrectForEstimationErrors`:
```csharp
// TODO: this is crude, the better approach (and in line with Windows) would be to estimate the
// position of the element, and use that
neededCorrection = -start;
...
foreach (var line in _materializedLines) foreach (var item in line.Items)
{ var bounds = GetBoundsForElement(item.container); IncrementStart(ref bounds, neededCorrection); SetBounds(item.container, bounds); }
```
i.e. a global rewrite of all realized item bounds whenever the estimate is off — an O(realized)
reposition on the scroll frame.

### 7.5 `_averageLineHeight` is a plain mean over currently-materialized lines

`VirtualizingPanelLayout.managed.cs:821-824`:
```csharp
private void UpdateAverageLineHeight() =>
    _averageLineHeight = _materializedLines.Count > 0
        ? _materializedLines.Select(l => GetMeasuredExtent(l.FirstView)).Average() : 0;
```
It is recomputed from the *current window only* (no history, unlike StackLayout's 100-slot buffer),
which drives:
* the extent estimate (`EstimatePanelExtent`, :790-819) ⇒ scrollbar thumb size/position jitter and
  `ScrollableHeight` churn as heterogeneous items scroll through;
* the large-scroll seed (`:292-293` `var index = (int)(ScrollOffset / _averageLineHeight);`);
* the container-pool size (`:426`).

With variable-height items, the extent changes every frame ⇒ `ScrollViewer.ExtentSizeChanged` ⇒
`OnScrollViewerExtentSizeChanged` (:338-344) and offset clamping in
`ScrollContentPresenter.Managed.cs:307-321` against a moving `ScrollableHeight`.

### 7.6 Large-scroll path throws away the whole realized set

`VirtualizingPanelLayout.managed.cs:270-294`:
```csharp
var isLargeScroll = Abs(delta) > ViewportExtent;
if (isLargeScroll)
{
    unappliedDelta = 1;
    ClearLines(clearContainer: false);     // recycles every line
    var index = (int)(ScrollOffset / _averageLineHeight);
    SetDynamicSeed(IndexPath.FromRowSection(index - 1, 0), index * _averageLineHeight);
}
```
plus `OwnerPanel.InvalidateMeasure()` at :327. A single dispatcher turn that skips more than one
viewport (easy when the `Normal`-priority `ViewChanged` item is delayed and several animation frames
coalesce) therefore recycles **and re-realizes** the whole window: N Leave walks + N Enter walks +
N template measures in one frame.

### 7.7 Every newly realized ListView container schedules two extra root layout passes

`UIElement.crossruntime.cs:108-115` (`OnChildAdded`) → `RequestRaiseLoadedEventOnNextTick()` →
`CoreServices.cs:115-121`:
```csharp
root.UpdateLayout();
if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
{ CoreServices.Instance.EventManager.RaiseLoadedEvent(); root.UpdateLayout(); }
```
and `RaiseLoaded` (`UIElement.crossruntime.cs:50-73`) runs `RepropagateMentoredChildrenDataContext`,
`OnFwEltLoaded` and `UpdateHitTest`, which for `SelectorItem` means
`UpdateVisualStates(true)` (`SelectorItem.cs:356-361`) — **VSM transitions with animations on the
scroll frame**.

### 7.8 `UpdateVisibilities` walks the entire recycle pool every scroll event

`VirtualizingPanelLayout.managed.cs:499-508` → `VirtualizingPanelGenerator.managed.cs:269-280`
(see §5.3). With `CacheLimit` up to 1024 (`:24`) this is up to 1024 `Visibility` DP writes per scroll
event, on containers that are already detached from the tree.

### 7.9 `SetMatrixDirty` fan-out per scroll frame

`Compositor.skia.cs:258-263` + `ContainerVisual.skia.cs:212-227` (see §1.2). One `AnchorPoint` write
dirties every visual under the scroller. The `TODO: only invalidate matrix when specific properties
are changed` in `InvalidateRenderPartial` is the acknowledgement. This is not virtualization-specific
but it scales with the number of realized items, so it is a virtualization *amplifier*.

### 7.10 The ListView realizes/measures outside any layout pass

`AddView` (`VirtualizingPanelLayout.managed.cs:1025-1056`) calls `view.Measure(slotSize)` directly
from inside the `ViewChanged` handler, with `OwnerPanel.ShouldInterceptInvalidate = true`
(`:465`) so the panel cannot become dirty. `ShouldInterceptInvalidate` **discards** the invalidation
rather than deferring it (`UIElement.Layout.crossruntime.cs:26-31`):
```csharp
public void InvalidateMeasure()
{
    if (ShouldInterceptInvalidate || IsMeasureDirty || IsLayoutFlagSet(LayoutFlag.MeasuringSelf)) return;
    ...
}
```
This is what keeps steady-state scroll at 0 layout passes (good), but it means a genuinely required
re-measure raised during fill/unfill is silently dropped — a correctness/robustness hazard that
manifests as stale sizes until the next unrelated invalidation.

---

## 8. Q6 — Does `ViewportManager` defer work off the scroll frame? vs WinUI

**Yes, one mechanism, and it matches WinUI's shape but not its scheduling primitive.**

| Aspect | Uno (`ViewportManagerWithPlatformFeatures.cs`) | WinUI (`controls/dev/Repeater/ViewportManager.cpp`) |
|---|---|---|
| Inflation constant | `40.0` (:25) | `40.0` (:15) |
| Max cache length | `2.0` / `2.0` (:54-55) | `2.0` / `2.0` (ViewportManager.h) |
| Where inflated | `OnOwnerArranged` (:331-349) | `OnOwnerArranged` (:546-574) |
| Deferral primitive | `Dispatcher.RunIdleAsync` → `NativeDispatcherPriority.Idle` (:643, `CoreDispatcher.cs:81-82`, `NativeDispatcher.cs:475-476`) | `DispatcherQueue().TryEnqueue(...)` — **normal** priority (`ViewportManager.cpp:1139-1143`) |
| Re-entrancy guard | `m_cacheBuildAction == null` (:634-635); cleared in `OnCacheBuildActionCompleted` (:489-496) | `m_cacheBuildActionOutstanding` bool (:1131, :744-751) |
| Viewport-change guard | none on Skia (§3.3); `IsSignificantViewportChange` only when `!UNO_HAS_ENHANCED_LIFECYCLE` (:599-605) | `roundingTolerance = 0.01f` before `TryInvalidateMeasure` (:1015-1027) |
| Post-arrange anchor bookkeeping | absent | `RegisterPreparedElementsAsArranged()` / `RegisterPreparedAndArrangedElementsAsScrollAnchorCandidates()` (:485, :502-514) |
| Unshiftable-shift reconciliation in arrange | absent | :518-545 (`ResetUnshiftableShift` + `TryInvalidateMeasure`) |
| `EnsureScroller` fallback when no scroller found | commented out with a Uno note (:543-555) | live (`UpdateViewport({})`) |

Notes:
* Uno's `Idle` priority is arguably *better* for smoothness than WinUI's normal-priority
  `TryEnqueue`, because it cannot preempt a scroll frame — but it also means the cache **never**
  builds during a sustained scroll, which is the opposite of what the mechanism is for.
* `RegisterCacheBuildWork` in Uno keeps a dead `var strongOwner = m_owner;` (:642) — the C# lambda
  does not capture it, so the "keep the owner alive" intent from WinUI (:1137-1143) is lost. Harmless
  today (the `Dispatcher` action captures `this`, which references `m_owner`), but it is a divergence.
* There is **no** `CompositionTarget.Rendering`-driven deferral for realization itself. The only
  `Rendering` hooks are `BuildTreeScheduler.OnRendering` (`BuildTreeScheduler.cs:57-93`, x:Phase only)
  and the bring-into-view anchor reset (`ViewportManagerWithPlatformFeatures.cs:424-432, 454-480`).

---

## 9. Summary table — per-scroll-frame budget

| Container | Trigger | Passes / frame (20 items, steady state) | Passes / frame (boundary crossed) | Buffer |
|---|---|---|---|---|
| `ListView` + `ItemsStackPanel` | `ScrollViewer.ViewChanged` on a coalesced `Normal` dispatcher item (`ScrollViewer.cs:1301-1316`) | 0 root layout passes; 20 short-circuited `Arrange`; 1 full pool walk (`UpdateVisibilities`) | +1 Leave walk, +1 Enter walk, +1 full template `Measure`, +VSM, +2 root `UpdateLayout()` next tick | `0.5 × viewport` per side (`CacheLength=1.0 × 0.5`) |
| `ItemsRepeater` + `StackLayout` | `EffectiveViewportChanged` drained inside `InnerUpdateLayout` (`UIElement.cs:980-983`) | 1 repeater `Measure` + 1 `Arrange`, 20 element `Measure` + 20 element `Arrange`, ×(2–3 loop iterations) | + realize/recycle via `RecyclePool` (no Enter/Leave) | grows +40px/arrange at **Idle**, max `1.0 × viewport` per side |
| `GridView` on Skia | n/a | **all** items measured/arranged (`WrapPanel`) | n/a | none |

---

## 10. Ranked, code-anchored levers

1. **Raise the ListView cache length.** `FeatureConfiguration.cs:323` `DefaultCacheLength = 1.0`
   combined with `ExtendedViewportScaling = 0.5` (`VirtualizingPanelLayout.managed.cs:155`) gives a
   half-viewport buffer. WinUI's default is 4.0 (2 viewports/side). Also fix the
   `DefaultCacheLength = null` → `0.0` trap caused by the `[NotImplemented]` stub DP
   (`Generated/.../ItemsStackPanel.cs:12-19`).
2. **Stop detaching containers on recycle.** `VirtualizingPanelGenerator.managed.cs:203-206`
   (`parent.Children.Remove`) forces a full `Leave`+`Enter` walk per recycled container
   (`DependencyObjectStore.mux.cs:149-219`). ItemsRepeater's `RecyclePool` already proves the
   cheaper model (`RecyclePool.cs:75-77`). This is the single biggest per-item cost on the ListView
   path.
3. **Give the ItemsRepeater a non-zero starting cache buffer** (or inflate it on a `Rendering` tick
   rather than `Idle`), so a fling does not start with `m_horizontalCacheBufferPerSide == 0`
   (`ViewportManagerWithPlatformFeatures.cs:611-621`).
4. **Restore a viewport-change tolerance on Skia** — port WinUI's `roundingTolerance = 0.01f`
   (`ViewportManager.cpp:1015`) and/or re-enable `IsSignificantViewportChange` under
   `UNO_HAS_ENHANCED_LIFECYCLE` (`ViewportManagerWithPlatformFeatures.cs:599-605`,
   `StackLayout.cs:423-454`).
5. **Stabilise the layout origin** so arrange rects don't move every pass
   (`FlowLayoutAlgorithm.cs:706-707`, `StackLayout.cs:186-232`). Today's mitigation is partial.
6. **Fix GridView's default panel on Skia** (§6) — either implement `ItemsWrapGrid` for cross-runtime
   or route `GridView` to a virtualizing `UniformGridLayout`-based panel.
7. **De-LINQ the ListView per-frame path** — `UpdateAverageLineHeight` (:821-824, 3× per layout pass),
   `ScrapLayout`'s `SelectMany/Select/Min` (:933-936, once per measure), `Deque<T>`'s iterator
   `GetEnumerator` (`Deque.cs:287-294`). Add a struct enumerator to `Deque<T>` and keep running
   sums instead of recomputing `Average()`/`Max()`.
8. **Skip `UpdateVisibilities` for detached containers**
   (`VirtualizingPanelGenerator.managed.cs:269-280`) — it is up to 1024 DP writes per scroll event on
   elements that are not in the tree.
9. **Remove the two `Disposable.Create` allocations per repeater layout pass**
   (`ItemsRepeater.cs:230, 297`) and the two per realized element in
   `ViewManager.cs:711-754`.
10. **Reconsider the 1 s wheel animation** (`ScrollContentPresenter.Managed.cs:479`) — its first frame
    jump is explicitly called out as causing "*a blank frame before items can be realized*"
    (`ScrollContentPresenter.cs:302-310`).

---

## 11. Explicitly UNVERIFIED

* Actual frame timings / FPS numbers — nothing was executed; this is a static read of the sources.
* Whether the GridView→`WrapPanel` fallback (§6) is what users actually see, or whether app templates
  always override `ItemsPanel`.
* The real cost distribution inside `EnterProperties`/`EstablishThemeOnEnterCore` per container
  (no profiler run).
* Whether `ScrollViewer` anchoring (`ScrollViewer.Anchoring.cs`) is active for a default
  `ItemsRepeater`-in-`ScrollViewer` setup — `Horizontal/VerticalAnchorRatio` default to `NaN`
  (`ScrollViewer.Anchoring.cs:45-67`) which short-circuits `IsAnchoring`, but I did not confirm
  whether any Uno template or `ItemsRepeaterScrollHost` sets them.
* Behaviour on WASM (`UNO_HAS_ENHANCED_LIFECYCLE` also defined there, but
  `UNO_HAS_MANAGED_SCROLL_PRESENTER` is not) — the SCP/animation half of §1.1 does not apply.
* Native Android/iOS paths (`ListViewBase.iOSAndroid.cs`, `NativeListViewBase.cs`) — out of scope,
  not read.
