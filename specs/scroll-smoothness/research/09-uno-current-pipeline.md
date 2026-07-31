# Uno managed scroll pipeline — end-to-end audit (Skia targets)

Scope: `ScrollViewer` + `ScrollContentPresenter` (the "managed scroll presenter", `UNO_HAS_MANAGED_SCROLL_PRESENTER`),
the Skia compositor, the Skia frame loop, and the virtualization consumers.
All paths verified against source in `D:/Work/uno-worktrees/scrollsmooth/src` at branch `dev/mazi/smooth-scroll`.

Everything below is cited `file:line`. Where I could not verify something in source I say **UNVERIFIED**.

---

## 0. Ground truth: which symbols are on

- `__SKIA__`, `UNO_HAS_ENHANCED_LIFECYCLE`, `SUPPORTS_RTL`, `UNO_SUPPORTS_NATIVEHOST` are defined for Skia:
  `src/Uno.CrossTargetting.targets:78`.
- WASM gets `__WASM__;UNO_HAS_ENHANCED_LIFECYCLE;UNO_HAS_MANAGED_POINTERS;HAS_INPUT_INJECTOR`:
  `src/Uno.CrossTargetting.targets:74`.
- Consequence: on Skia, **`UNO_HAS_ENHANCED_LIFECYCLE` is ON**, which turns off several historical
  "don't invalidate on every viewport update" perf guards (see §8).

- Default `ScrollViewerUpdatesMode` = `AsynchronousIdle`:
  `src/Uno.UI/FeatureConfiguration.cs:483`.
- Snap delay 250 ms: `src/Uno.UI/FeatureConfiguration.cs:496`.
- Scroll indicator auto-hide 4 s: `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs:1645`.

---

## 1. The Skia frame loop (needed to read every call graph below)

The whole scroll pipeline runs on **one thread**: the UI thread (`NativeDispatcher.Main`). There is no
composition/render thread doing offset work.

### 1.1 Frame scheduling state machine

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.RenderScheduling.skia.cs`

- `ICompositionTarget.RequestNewFrame()` (`:86-118`) — sets `RenderRequested`, calls
  `host.InvalidateRender()` (`:110`). Coalesces: repeated calls while a request is pending are ignored (`:93-101`).
- The native windowing layer later calls `OnNativePlatformFrameRequested` (`:166-176`) on the
  **rendering/GPU thread**, which:
  - enqueues `EnqueueRenderCallback` on the UI thread via `NativeDispatcher.Main.EnqueueRender` (`:172`),
  - and *presents* the last recorded picture via `Draw(...)` (`:175`).
- `EnqueueRenderCallback` (`:120-157`) runs **on the UI thread**, and calls `Render()` (`:152`).
- `OnRenderFrameOpportunity` (`:178-208`) is an "early render" hook called from the layout tick
  (see §1.3) — it can call `Render()` ahead of the scheduled callback and set `_renderedAheadOfTime`.

### 1.2 `Render()` = record picture + tick animations, on the UI thread

`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs:110-201`

```
Render()                                  // NativeDispatcher.CheckThreadAccess() :114
 └─ SkiaRenderHelper.RecordPictureAndReturnPath(...)      :119
     └─ rootVisual.Compositor.RenderRootVisual(canvas, rootVisual, damage)
        (src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs:44)
```

`Compositor.RenderRootVisual` (`src/Uno.UI.Composition/Composition/Compositor.skia.cs:199-256`):

```csharp
foreach (var animation in _runningAnimations.Keys.ToArray())   // :206  <-- array alloc EVERY frame
{
    animation.RaiseAnimationFrame();                            // :210
}
...
if (!SkipVisualTreePainting) rootVisual.RenderRootVisual(canvas, null, damage);   // :229-232
...
if (_runningAnimations.Count > 0 || transitionsCount > 0)
    rootVisual.CompositionTarget?.RequestNewFrame();            // :252-255  self-sustaining loop
```

**This is the single most important structural fact of the audit**: the composition animation tick
(and therefore *the whole ScrollViewer bookkeeping chain*, see §4) executes **inside the picture-recording
pass, on the UI thread, immediately before the visual tree walk**.

### 1.3 Layout tick

`src/Uno.UI/UI/Xaml/Internal/CoreServices.cs:67-127`

- `RequestAdditionalFrame()` (`:67-75`) enqueues a **Normal-priority** `NativeDispatcher` job (`:73`),
  coalesced by `_isAdditionalFrameRequested`.
- `OnTick()` (`:77-127`) → `root.UpdateLayout()` (`:115`) → then
  `(root.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget)?.OnRenderFrameOpportunity()` (`:124`).
- `XamlRoot.InvalidateMeasure/InvalidateArrange` call `CoreServices.RequestAdditionalFrame()`
  (`src/Uno.UI/UI/Xaml/XamlRoot.crossruntime.cs:14-28`).

`UIElement.InnerUpdateLayout` (`src/Uno.UI/UI/Xaml/UIElement.cs:921-1012`) is an **iterative loop**,
`MaxLayoutIterations = 250` (`:889`, `:959`):

```
for i in 250:
   if measureDirty      -> root.Measure(bounds)          :971-974
   elif arrangeDirty    -> root.Arrange(bounds)          :975-978
   elif pendingViewport -> RaiseEffectiveViewportChangedEvents()   :980-983
   else                 -> RaiseSizeChangedEvents / RaiseLayoutUpdated; exit if clean   :984-1011
```

So EffectiveViewport events are raised **inside the layout loop**, and any measure invalidation they cause
(§8) forces another measure+arrange iteration in the *same* tick.

### 1.4 Dispatcher priority interaction (a real jank lever)

`src/Uno.UI.Dispatching/Native/NativeDispatcher.cs`

- `EnqueueRender` (`:237-263`) stores a per-`CompositionTarget` render action.
- `TryGetRenderAction` (`:206-234`) will run the render action **only if
  `normalItemsToProcessBeforeNextRenderAction == 0`**, and when it consumes it, it sets that counter to
  `_queues[Normal].Count` (`:216`).

Meaning: after each rendered frame, the dispatcher must drain *the whole Normal queue that existed at that
moment* before it will run the next render. The scroll pipeline enqueues Normal-priority work per scroll
event/frame (`RequestUpdate` §4.2; `CoreServices.RequestAdditionalFrame`; `DispatcherQueueTimer` ticks),
so a burst of scroll work directly delays the next frame.

---

## 2. Where the offset actually lives, and how it moves the pixels

There are **three** offset representations, and they can disagree:

| Representation | Owner | Where |
|---|---|---|
| `ScrollContentPresenter.HorizontalOffset/VerticalOffset` (plain CLR props) | logical/target | `ScrollContentPresenter.Managed.cs:96,98` |
| `Visual.AnchorPoint` (Vector2 on the *content* visual) | rendered position | `ScrollContentPresenter.Managed.cs:467,496` |
| `ScrollViewer.HorizontalOffset/VerticalOffset` (**DependencyProperty**) | published/public | `ScrollViewer.cs:556-593` |

`ScrollContentPresenter.Update(...)` (`ScrollContentPresenter.Managed.cs:413-507`) is the only writer of
`AnchorPoint`:

```csharp
var target = new Vector2((float)(-horizontalOffset + centeringOffsetX),
                         (float)(-verticalOffset   + centeringOffsetY));   // :427-429
...
if (options is { DisableAnimation: true } or { IsTouch: true })            // :463
{
    visual.StopAnimation(nameof(Visual.AnchorPoint));                      // :465
    visual.StopAnimation(nameof(Visual.Scale));                            // :466
    visual.AnchorPoint = target;                                           // :467
    visual.Scale = targetScale;                                            // :468
    Updated(horizontalOffset, verticalOffset, options.IsIntermediate);     // :469
}
else                                                                       // animated path
{
    var easing = CompositionEasingFunction.CreatePowerEasingFunction(compositor, Out, 10);  // :474
    var scrollAnimation = compositor.CreateVector2KeyFrameAnimation();     // :477
    scrollAnimation.InsertKeyFrame(1.0f, target, easing);                  // :478
    scrollAnimation.Duration = TimeSpan.FromSeconds(1);                    // :479  <-- 1 SECOND
    void OnFrame(CompositionAnimation? _) => Updated(GetAnimatedHorizontalOffset(), GetAnimatedVerticalOffset(), true);  // :481
    void OnStopped(object? _, EventArgs __) { ...; Updated(..., false); }  // :482-488
    double GetAnimatedHorizontalOffset() => Math.Round(-visual.AnchorPoint.X + centeringOffsetX);  // :490
    double GetAnimatedVerticalOffset()   => Math.Round(-visual.AnchorPoint.Y + centeringOffsetY);  // :491
    scrollAnimation.AnimationFrame += OnFrame;                             // :493
    scrollAnimation.Stopped += OnStopped;                                  // :494
    visual.StartAnimation(nameof(Visual.AnchorPoint), scrollAnimation);    // :496
}
```

Note the animation duration is a **flat 1 second regardless of distance**, with a `Power=10, Mode=Out`
easing (`PowerEasingFunction.EaseIn => MathF.Pow(t, Power)`,
`src/Uno.UI.Composition/Composition/KeyFrameAnimations/PowerEasingFunction.cs:23-24`).
`Out` mode with power 10 means ~14.9 % of the remaining distance is covered in the first frame at 60 Hz —
which is exactly what the comment at `ScrollContentPresenter.cs:303-307` complains about.

---

## 3. Full call graphs

### (a) Mouse wheel

Thread: **UI thread**, synchronously on the input event.

```
native window wheel event
 └─ InputManager.PointerManager._source.PointerWheelChanged            InputManager.Pointers.Managed.cs:141
     └─ OnPointerWheelChanged(args)                                    InputManager.Pointers.Managed.cs:295
         ├─ HitTestOrRoot(args, out originalSource)          FULL VISUAL-TREE HIT TEST   :319
         │   (the cache at :291-293/:310-316 is `OperatingSystem.IsIOS()`-ONLY)
         ├─ walk Visual.Parent chain looking for VisualInteractionSource{RedirectsPointerWheel}  :341-355
         │   (ScrollViewer/SCP never registers one -> loop always runs to the root and falls through)
         ├─ new PointerRoutedEventArgs(args, originalSource)            :357   (heap alloc)
         ├─ RaiseUsingCaptures(Wheel, ...)                              :360   -> bubbles to SCP handler
         └─ HitTestOrRoot(args, _isOver, ...) + RaiseLeaveEnter + SetSourceCursor   :366-380
             SECOND FULL VISUAL-TREE HIT TEST, per wheel event, non-iOS only

ScrollContentPresenter.PointerWheelScroll                              ScrollContentPresenter.cs:245
 ├─ Scroller?.ClearOffsetIntents()                                     :251
 ├─ e.GetCurrentPoint(null).Properties                                 :253
 ├─ (Skia desktop, no Ctrl/Shift) canScrollVertically branch           :333
 │    Set(verticalOffset: TargetVerticalOffset + GetVerticalScrollWheelDelta(DesiredSize, -delta),
 │        disableAnimation: false)                                     :346-349     <-- ANIMATED
 └─ e.Handled = success                                                :356
```

Wheel delta formula (`ScrollContentPresenter.mux.cs:18,24-27`):
`ScrollViewerDefaultMouseWheelDelta = 120`;
`Min(Floor(size.Height), Round(delta * Max(48.0, Round(size.Height*0.15,0)) / 120, 0))`.

`Set(...)` → `Update(...)` → `visual.StartAnimation("AnchorPoint", <new 1 s animation>)`.

**Per-frame after that**: `Compositor.RenderRootVisual` (§1.2) raises `AnimationFrame` →
- SCP's `OnFrame` (`ScrollContentPresenter.Managed.cs:481`) → `Updated(...)` → §4;
- then `CompositionObject.ReEvaluateAnimation` (`CompositionObject.cs:125-181`) → `animation.Evaluate()` →
  `Visual.AnchorPoint = <boxed Vector2>`.

Ordering hazard: `scrollAnimation.AnimationFrame += OnFrame` at `:493` happens **before**
`visual.StartAnimation` at `:496`, and `StartAnimation` adds `ReEvaluateAnimation` at
`CompositionObject.cs:97`. Delegate invocation order is subscription order, so **`OnFrame` reads
`visual.AnchorPoint` for frame N before the frame-N value has been written**. Every reported offset
(and therefore `ScrollViewer.VerticalOffset`, `ViewChanged`, item realization) is **one frame stale**.

Second hazard: a new wheel tick calls `StartAnimation` again, which first calls `StopAnimation`
(`CompositionObject.cs:90-93`) → `KeyFrameAnimation.Stop()` → `Stopped?.Invoke` (`KeyFrameAnimation.cs:60-64`)
→ SCP's `OnStopped` (`ScrollContentPresenter.Managed.cs:482-488`) → `Updated(..., isIntermediate: **false**)`.
So **every wheel tick emits a non-intermediate `ViewChanged`**, which in `ScrollViewer.Update` triggers
`InvalidateArrange()` (`ScrollViewer.cs:1336`) and (re)starts the 250 ms snap-points timer
(`ScrollViewer.cs:1254-1277`).

### (b) Touch pan (non-inertial)

Thread: **UI thread** (managed pointers / `DirectManipulation`).

```
pointer pressed
 └─ ScrollContentPresenter.TryEnableDirectManipulation                 ScrollContentPresenter.Managed.cs:510
     ├─ Scroller?.ClearOffsetIntents()                                 :519
     └─ InputManager.Pointers.RegisterDirectManipulationHandler(...)   :521

pointer moved (per native move event, NOT coalesced to vsync)
 └─ DirectManipulation -> GestureRecognizer.Manipulation.NotifyUpdate  GestureRecognizer.Manipulation.cs:332
     ├─ StageChanges()                                                 :433
     └─ new ManipulationUpdatedEventArgs(...)                          :425-427 (heap alloc per move)
         └─ DirectManipulation.OnDirectManipulationUpdated             DirectManipulation.cs:449
             └─ ScrollContentPresenter.IDirectManipulationHandler.OnUpdated   ScrollContentPresenter.Managed.cs:591
                 ├─ GetScrollableOffsets()                             :598  (record struct, no alloc)
                 ├─ clamp deltas                                       :599-600
                 └─ Set(h+dx, v+dy, options: new(DisableAnimation:true, IsTouch:true, IsIntermediate:true))  :650-654
                     └─ Update(...) -> immediate branch                :463-470
                         visual.StopAnimation("AnchorPoint")           :465
                         visual.StopAnimation("Scale")                 :466
                         visual.AnchorPoint = target                   :467
                         visual.Scale = targetScale                    :468   <-- always set, even when 1,1,1
                         Updated(h, v, isIntermediate: true)           :469
```

Per *pointer event* (not per frame): §4 runs, plus `Compositor.InvalidateRenderPartial` (§5).

### (c) Touch inertia

`ScrollContentPresenter.IDirectManipulationHandler.OnInertiaStarting`
(`ScrollContentPresenter.Managed.cs:669-801`):

- deceleration is platform-tuned: iOS uses a PastryKit-derived duration
  (`0.95` per frame @60 fps, min velocity `0.01`) `:727-736`; Android uses
  `DefaultDesiredDisplacementDeceleration / 2` `:739`; everything else uses
  `DefaultDesiredDisplacementDeceleration = .001` (`GestureRecognizer.Manipulation.InertiaProcessor.cs:65`) `:743`.
- If single-snap-point snapping applies, inertia is **not** run: the final resting offset is computed
  analytically and a single animated `Set` is issued `:748-784`.
- Otherwise `_touchInertia = args.Manipulation` `:788` and the recognizer's `InertiaProcessor` drives ticks.

Timer selection (§6):

```
GestureRecognizer.Manipulation.NotifyUpdate
  case Started when pointerRemoved && InertiaProcessor.TryStart(...)     :392
     _inertia.Start(startingArgs.UseCompositionTimer)                     :401
InertiaProcessor.Start(bool useCompositionTimer)                          InertiaProcessor.cs:184-210
   _timer = useCompositionTimer ? new CompositionInertiaProcessorTimer(Process)
                                : new DispatcherInertiaProcessorTimer(Process);   :194-196
```

`DirectManipulation.OnDirectManipulationInertiaStarting` sets
`args.UseCompositionTimer = WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForDirectManipulation`
(`DirectManipulation.cs:484`), whose default is **`true`**
(`Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.GestureRecognizer.cs:66,71`).

So **ScrollViewer touch inertia does use the composition timer, not the 30 FPS dispatcher timer** —
see §6 for the caveats.

Per inertia tick:
```
CompositionTarget.Rendering handler                    InertiaProcessor.cs:345-349
 └─ InertiaProcessor.Process(elapsed)                  :214-232
     ├─ UpdateCumulative(...)  (v0*t - d*t^2)          :234-247, GetValue :268-271
     └─ _owner.NotifyUpdate()                          :225
         └─ new ManipulationUpdatedEventArgs(...)      GestureRecognizer.Manipulation.cs:425-427
             └─ SCP.OnUpdated (args.IsInertial == true)  ScrollContentPresenter.Managed.cs:627-644
                 unhandledDelta = ManipulationDelta.Empty                :637
                 Set(..., new(DisableAnimation:true, IsTouch:true, IsIntermediate:true))  :639-643
```

### (d) ScrollBar drag

```
Thumb DragDelta
 └─ ScrollBar.OnThumbDragDelta                          ScrollBar.mux.cs:798-846
     ├─ offset = (zoom*change)/(trackLength - thumbSize)*(max-min)     :820 / :831
     ├─ Value = newValue                                :842   -> OnValueChanged -> UpdateTrackLayout  :729-733
     └─ RaiseScrollEvent(ScrollEventType.ThumbTrack)    :843
         └─ new ScrollEventArgs()                       :937   (heap alloc per drag delta)
             └─ ScrollViewer.OnVerticalScrollBarScrolled          ScrollViewer.cs:1175
                 ├─ (immediate, offset) = (true, e.NewValue)      :1189   (ThumbTrack -> no animation)
                 ├─ _verticalOffsetIntent = offset                :1194
                 └─ ChangeViewCore(null, offset, null, disableAnimation:true, shouldSnap:true)  :1196-1201
                     ├─ AdjustOffsetsForSnapPoints(...)           ScrollViewer.cs:1637 / SnapPoints.cs:14
                     │   └─ (Skia) ClampOffsetsToFocusedTextBox   SnapPoints.cs:53 -> ScrollViewer.skia.cs:8
                     │       └─ ShouldSnapToTouchTextBox: FocusManager.GetFocusedElement + FindFirstParent<ScrollViewer>()  ScrollViewer.skia.cs:42-45
                     └─ ChangeViewNative -> SCP.Set(disableAnimation:true)   ScrollViewer.Managed.cs:150-151
```

Feedback loop: `ScrollViewer.VerticalOffset` is `{TemplateBinding VerticalOffset}` on the ScrollBar's
`Value` (`ScrollViewer.xaml:274`, horizontal at `:287`). So after `Update()` publishes the new
`VerticalOffset`, the binding pushes it **back** into `ScrollBar.Value` → `OnValueChanged` →
`UpdateTrackLayout` a **second** time per drag delta.

Other `ScrollBar` events that reach the SV: `SmallIncrement/Decrement` and `LargeIncrement/Decrement`
map to `±16` and `±ActualHeight` and use `disableAnimation: false` (i.e. the 1-second animation)
`ScrollViewer.cs:1183-1190`.

### (e) Keyboard

```
ScrollViewer.OnKeyDown                                  ScrollViewer.cs:1752-1887
 ├─ (ZoomMode.Enabled && Ctrl) -> zoom, ChangeView(..., disableAnimation:false)  :1766-1804
 ├─ HandleKeyDownForXYNavigation(args)                  :1811
 ├─ newOffset = switch(key) { Up/Down: ±GetDelta(ActualHeight), PageUp/Down: ±ActualHeight, Home:0, End:ScrollableHeight }  :1818-1829
 │   GetDelta(l): length=(int)Max(0, Round(l)-16); result = 2 + length/20*3 + {0|1|2|3}   :1865-1886
 └─ ScrollToVerticalOffset(newOffset)                   :1848
     └─ ChangeView(null, offset, null, disableAnimation:**true**)   :1394
```

Keyboard scrolling is therefore **instant, unanimated** on Skia (unlike WinUI). `HandleVerticalScroll` /
`HandleHorizontalScroll` are stubs (`ScrollViewer.cs:1911-1922`) — `LineUp/LineDown/PageUp/...`
(`:1400-1455`) are effectively **no-ops**.

### (f) `ChangeView` / `BringIntoView`

```
ScrollViewer.ChangeView(h, v, z, disableAnimation)      ScrollViewer.cs:1475-1523
 ├─ log (LogLevel.Debug guarded)                        :1477-1480
 ├─ if !_isInternalOffsetAdjustment: arm _horizontal/_verticalOffsetIntent   :1489-1499
 └─ ChangeViewCore(..., shouldSnap:true)                :1512-1517
     ├─ AdjustOffsetsForSnapPoints(ref h, ref v, z, canBypassSingle:true)   :1637
     └─ ChangeViewNative -> (SCP)Set(h, v, z, disableAnimation)             ScrollViewer.Managed.cs:151

BringIntoView:
UIElement.StartBringIntoView -> BringIntoViewRequested bubbles
 └─ ScrollContentPresenter.OnBringIntoViewRequested     ScrollContentPresenter.mux.cs:74-158
     ├─ SharedHelpers.IsAncestor(args.TargetElement, this, checkVisibility:true)   :83  (tree walk)
     ├─ ComputeBringIntoViewTargetOffsets(...)          :106-113
     └─ Scroller.ChangeView(tH, tV, zoomFactor, !args.AnimationDesired)   :136
ScrollContentPresenter.MakeVisible                      ScrollContentPresenter.cs:74-87
     └─ new BringIntoViewRequestedEventArgs{AnimationDesired=true,...} -> OnBringIntoViewRequested
```

`SetVerticalOffset` / `SetHorizontalOffset` (`ScrollContentPresenter.cs:360-374`) bypass `ChangeView`
and go straight to `Scroller?.SetVerticalOffsetIntent(offset)` + `Set(..., disableAnimation: true)`.

---

## 4. What happens per scroll delta (`Updated` → `OnPresenterScrolled` → `Update`)

### 4.1 `ScrollContentPresenter.Updated`

`ScrollContentPresenter.Managed.cs:376-411`

```csharp
var request = Interlocked.Increment(ref _stategyUpdateRequestId);              // :378
if (NativeDispatcher.Main.HasThreadAccess) UpdateOffsets(h, v, isIntermediate);// :380-383  (always true in practice)
else DispatcherQueue.TryEnqueue(() => { ... });                                // :386-392  (closure alloc)

void UpdateOffsets(h, v, isIntermediate)
{
    if (_lastScrolledEvent != (h, v, isIntermediate))                          // :398  dedup (ValueTuple, no alloc)
    {
        _lastScrolledEvent = (h, v, isIntermediate);
        Scroller?.OnPresenterScrolled(h, v, isIntermediate);                   // :402
    }
    ScrollOffsets = new Point(h, v);                                           // :408  (struct)
    InvalidateViewport();                                                      // :409  <-- ALWAYS, every delta
}
```

`InvalidateViewport()` is called **unconditionally**, even when the dedup above suppressed
`OnPresenterScrolled`, and even for intermediate frames.

### 4.2 `ScrollViewer.OnPresenterScrolled`

`ScrollViewer.cs:1234-1280`

```csharp
_pendingHorizontalOffset = h; _pendingVerticalOffset = v;                  // :1236-1237
if (isIntermediate && UpdatesMode != Synchronous)                          // :1239
{
    RequestUpdate();            // Dispatcher.RunAsync(Normal, ...) coalesced by _hasPendingUpdate   :1301-1316
    _snapPointsTimer?.Stop();                                              // :1242
}
else
{
    Update(isIntermediate);                                                // :1246
    if (!isIntermediate && (snap points configured || ShouldSnapToTouchTextBox()))
        _snapPointsTimer.Start();   // 250 ms one-shot DispatcherQueueTimer  :1254-1277
}
```

`RequestUpdate` (`:1301-1316`) allocates a closure + a `UIAsyncOperation` per *scheduling* (coalesced),
and posts it at **Normal** priority — which then gates the next render frame (§1.4).

### 4.3 `ScrollViewer.Update` — the per-frame cost centre

`ScrollViewer.cs:1318-1357`

```csharp
_hasPendingUpdate = false;
var oldH = HorizontalOffset; var oldV = VerticalOffset;      // 2 DP reads (boxed double unbox)
HorizontalOffset = _pendingHorizontalOffset;                 // :1325  DP SET  (boxes the double)
VerticalOffset   = _pendingVerticalOffset;                   // :1326  DP SET  (boxes the double)

if (!isIntermediate && offsets changed) InvalidateArrange();  // :1328-1337

if (AutomationPeer.ListenerExistsHelper(PropertyChanged) && GetAutomationPeer() is ScrollViewerAutomationPeer peer)
    peer.RaiseAutomationEvents(ExtentWidth, ExtentHeight, ViewportWidth, ViewportHeight, 0, 0, oldH, oldV);  // :1340-1352
    // NOTE: 4 more DP reads even when no listener? No - guarded by ListenerExistsHelper first. OK.

UpdatePartial(isIntermediate);                                // :1354 (no Skia impl)
ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });  // :1356  ALLOC per update
```

The two DP sets are the expensive part, because of what they fan out to:

**`ScrollViewer.VerticalOffset` DP set → TemplateBinding → `ScrollBar.Value` →
`RangeBase.OnValueChanged` → `ScrollBar.OnValueChanged` (`ScrollBar.mux.cs:729-733`) →
`UpdateTrackLayout()` (`ScrollBar.mux.cs:1005-1058`)**, which does:

```csharp
UpdateIndicatorLengths(trackLength, out mouseIndicatorLength, out touchIndicatorLength);   // :1015
...
m_tpElementVerticalLargeDecrease.Height = largeDecreaseNewSize;    // :1041   Height => AffectsMeasure
...
newMargin = m_tpElementVerticalPanningRoot.Margin; newMargin.Top = indicatorOffset;
m_tpElementVerticalPanningRoot.Margin = newMargin;                 // :1054-1056  Margin => AffectsMeasure
```

`Height` is `FrameworkPropertyMetadataOptions.AffectsMeasure`
(`src/Uno.UI/UI/Xaml/FrameworkElement.crossruntime.cs:90-96`); `Margin` likewise
(`FrameworkElement.crossruntime.cs:183`).

⇒ **Every published offset change dirties the ScrollBar's measure**, and
`InvalidateMeasure` → `InvalidateParentMeasureDirtyPath` walks to the visual-tree root
(`UIElement.Layout.crossruntime.cs:26-52,67-78`) → `XamlRoot.InvalidateMeasure` →
`CoreServices.RequestAdditionalFrame()`. **A full layout tick is scheduled per scroll frame even for
completely static, non-virtualized content.**

`UpdateIndicatorLengths` also allocates nothing but writes several `Width`/`Height`/`Visibility` DPs on
thumb elements (`ScrollBar.mux.cs:1097-1276`).

---

## 5. `InvalidateViewport` and `InvalidateRender`: what each invalidates

### 5.1 `InvalidateViewport` (per scroll delta, `ScrollContentPresenter.Managed.cs:409`)

`src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs:256-266`

```csharp
protected void InvalidateViewport()
{
    if (!IsScrollPort) throw ...;
    PropagateEffectiveViewportChange();          // :265
}
```

`PropagateEffectiveViewportChange` (`:338-420`):

- `if (!IsEffectiveViewportEnabled) return;` (`:349-353`) — **the whole thing short-circuits when no
  descendant/self subscribed**. `IsEffectiveViewportEnabled` = `_childrenInterestedInViewportUpdates.Count>0 ||
  _effectiveViewportChanged != null` (`:84`).
- `GetParentViewport()` (`:363`) → `ViewportInfo.GetRelativeTo(this)`
  (`FrameworkElement.EffectiveViewport.ViewportInfo.cs:42-65`) → `UIElement.GetTransform(uiElement, parent).Inverse()`
  (`:47`). On Skia that's the fast path `from.Visual.TotalMatrix * inverse(to.Visual.TotalMatrix)`
  (`UIElement.cs:644-661`) — which **forces `TotalMatrix` recomputation** for both visuals (§5.2).
  Two `Rect` transforms follow (`:51,:56`).
- `GetEffectiveViewport` (`:269-323`) — for a scroll port, `LayoutInformation.GetLayoutSlot(this)` +
  `Rect.Intersect`.
- if the viewport changed, enqueue the event: `EventManager.EnqueueForEffectiveViewportChanged(this, new EffectiveViewportChangedEventArgs(...))` (`:384`) — **allocates event args**.
- then it **recurses into every subscribed child**: `:403-417`

```csharp
if (_childrenInterestedInViewportUpdates is { Count: > 0 } && (isInitial || viewportUpdated || _lastScrollOffsets != ScrollOffsets))
{
    foreach (var child in _childrenInterestedInViewportUpdates)
        child.OnParentViewportChanged(isInitial, isInternal, this, viewport);   // recursive
}
```

Note `_lastScrollOffsets != ScrollOffsets` (`:403`) is a **Skia-specific extra trigger**: when the SCP's
`ScrollOffsets` changed, children are notified **even if the computed viewport rect did not change**.

**Answer to "does InvalidateViewport walk the tree?"**: it walks the *subscribed-descendant graph*, not
the whole tree. Depth-1 cost when nothing subscribes; O(#subscribers in the subtree) when
`ItemsRepeater`/`CalendarPanel`/`SystemFocusVisual`/`TeachingTip` are present, with a matrix
computation + two rect transforms per hop.

`EnqueueForEffectiveViewportChanged` itself (`src/Uno.UI/UI/Xaml/Internal/EventManager.cs:29-35`):

```csharp
_effectiveViewportChangedQueue.RemoveAll(x => x.Element == element);   // :31  CLOSURE ALLOC + O(n) scan
_effectiveViewportChangedQueue.Add((element, args));                   // :32
CoreServices.RequestAdditionalFrame();                                 // :34  schedules a layout tick
```

That's a **lambda closure allocation and a linear scan per enqueue, per scroll frame, per element**.

### 5.2 `Visual.AnchorPoint = target` → what it invalidates

```
Visual.AnchorPoint setter          Visual.skia.cs:266-273
 └─ SetProperty(ref _anchorPoint, value)     CompositionObject.cs:386-397
     └─ OnPropertyChanged(...)               CompositionObject.cs:502-506
         ├─ OnPropertyChangedCore -> Compositor.InvalidateRender(this)    Visual.cs:192-194
         │   └─ Compositor.InvalidateRenderPartial(visual)                Compositor.skia.cs:258-263
         │       ├─ visual.SetMatrixDirty()          <-- RECURSIVE OVER THE WHOLE SUBTREE
         │       ├─ visual.InvalidatePaint()
         │       └─ visual.CompositionTarget?.RequestNewFrame()
         └─ PropagateChanged()   (context entries; empty for a plain content visual)  CompositionObject.Context.cs:29-35
```

`ContainerVisual.SetMatrixDirty` (`src/Uno.UI.Composition/Composition/ContainerVisual.skia.cs:212-227`):

```csharp
if (base.SetMatrixDirty())            // returns true only if it wasn't already dirty
    foreach (var child in Children.InnerList) child.SetMatrixDirty();
```

⇒ setting `AnchorPoint` once marks **every descendant visual of the scrolled content** matrix-dirty.
`Visual.SetMatrixDirty` (`Visual.skia.cs:140-146`) additionally calls `InvalidateParentChildrenPicture(false)`
(`:245-258`), which walks *up* to the root freeing each ancestor's cached `_childrenPicture` and setting
`ChildrenSKPictureInvalid`. Children's own walks short-circuit at the first already-invalid ancestor.

Because `ChildrenSKPictureInvalid` is set on the whole content subtree every scroll frame:
- `Visual.Render` resets `_framesSinceSubtreeNotChanged = 0` and sets `_subtreeChangedThisFrame = true`
  (`Visual.skia.cs:388-398`);
- the **picture-collapsing optimization can never engage** during scroll — it needs
  `_framesSinceSubtreeNotChanged >= 50` and subtree visual count `>= 100`
  (`Visual.skia.cs:39-41`, gate at `:541-544`).
⇒ during scrolling, the full realized subtree is re-walked and re-emitted (`sk_canvas_draw_picture`
per visual) every frame, plus `ContributeDamageOnPaint` per visual
(`Visual.Damage.skia.cs:27-69`, path allocation/ops at `:71-173`), which for a moved visual unions
old + new bounds (`:56-62`).

Note leaf **rasterization** is preserved: `InvalidatePaint` is only called on the content visual itself,
so descendants keep their `_picture` (`Visual.skia.cs:472-497`). The per-frame cost is the *walk*,
the *damage path ops*, and the *matrix recomputation*, not re-rasterization.

---

## 6. The inertia timer

`GestureRecognizer.Manipulation.InertiaProcessor.cs`

- `DispatcherInertiaProcessorTimer` (`:312-330`): `DefaultFramePerSeconds = 30d` (`:316`),
  `_timer.Interval = TimeSpan.FromMilliseconds(1000d/30)` (`:321`), `IsRepeating = true` (`:322`).
- `CompositionInertiaProcessorTimer` (`:332-365`): subscribes `CompositionTarget.Rendering` (`:351`),
  ticks with `Stopwatch.Elapsed` (`:348`).
- Selection: `InertiaProcessor.Start(bool useCompositionTimer)` (`:184-199`). Outside
  `IS_UNO_UI_PROJECT` only the dispatcher timer exists (`:198`).

Who sets the flag:
- `DirectManipulation.OnDirectManipulationInertiaStarting` → `args.UseCompositionTimer = ...UseCompositionTimerForDirectManipulation` (`DirectManipulation.cs:484`)
- `UIElement.Pointers.cs:364` → `...UseCompositionTimerForUiElement`
- both default `true` (`WinRTFeatureConfiguration.GestureRecognizer.cs:66,71,77`).

**Conclusion: ScrollViewer touch inertia on Skia uses the composition timer (vsync-driven), not 30 FPS.**
The 30 FPS `DispatcherInertiaProcessorTimer` is the fallback for `Uno.UWP`-only builds and for anyone who
sets the feature flags to `false`.

Caveats that still bite:
1. `CompositionTarget.Rendering` is raised from `RaiseRendering`
   (`CompositionTarget.Rendering.skia.cs:458-487`), which is enqueued at **High** priority from
   `OnFramePictureRecorded` (`:448-452`) — i.e. **after** the frame's picture has already been recorded.
   The inertia tick's `AnchorPoint` write therefore lands in the **next** frame → one-frame latency.
2. `RaiseRendering` allocates a `FramePicture[]`, a `List<(Window, object)>` and a `RenderingEventArgs`
   **every raise** (`:462-475`).
3. Subscribing to `Rendering` sets `_isRenderingActive = true` (`:90-97`), which makes `Render()`
   unconditionally `RequestNewFrame()` (`:167-170`) — a continuous render loop for the whole inertia
   duration, which is correct but means the frame budget is fully consumed.
4. `DispatcherQueueTimer` on Skia is a **threadpool** `System.Threading.Timer` that marshals via
   `NativeDispatcher.Main.Enqueue(() => RaiseTick())` — closure alloc per tick, and it is re-armed
   one-shot (`Timeout.InfiniteTimeSpan`) so it drifts:
   `src/Uno.UI.Dispatching/Dispatching/DispatcherQueueTimer.others.cs:19-35`.

Separately, `ScrollViewer.ConstantVelocityScroller` (used for drag-autoscroll in ListView) runs a
`DispatcherTimer` at `FrameIntervalMS = 1000/40` = 25 ms
(`ScrollViewer.ConstantVelocity.cs:83,89`) and calls `ChangeView(..., disableAnimation: true)` per tick
(`:129`).

---

## 7. `OnPresenterScrolled` cost per frame — itemised

For one intermediate frame with `UpdatesMode = AsynchronousIdle` (the default):

| Step | Cost | Cite |
|---|---|---|
| `_lastScrolledEvent` tuple compare | ~free | `SCP.Managed.cs:398` |
| `Scroller.OnPresenterScrolled` | field writes + `RequestUpdate()` (coalesced) | `ScrollViewer.cs:1236-1242` |
| `ScrollOffsets = new Point(...)` | struct | `SCP.Managed.cs:408` |
| `InvalidateViewport()` | matrix recompute + rect transforms + recursive child walk + EVP enqueue (closure + O(n) `RemoveAll`) + `RequestAdditionalFrame` | `SCP.Managed.cs:409`; `FrameworkElement.EffectiveViewport.cs:338-420`; `EventManager.cs:29-35` |
| **later, Normal priority** `Update(true)` | 2 DP reads + 2 DP sets, each fanning into TemplateBinding → `ScrollBar.UpdateTrackLayout` → 2 `AffectsMeasure` DP writes → `InvalidateMeasure` to root | `ScrollViewer.cs:1318-1357`; `ScrollBar.mux.cs:729-733,1005-1058` |
| `ViewChanged?.Invoke(new ScrollViewerViewChangedEventArgs{...})` | heap alloc + **synchronous virtualization work** (§8) | `ScrollViewer.cs:1356` |
| non-intermediate only: `InvalidateArrange()` | full arrange dirty-path to root | `ScrollViewer.cs:1336` |
| non-intermediate only: `_snapPointsTimer.Start()` | threadpool timer rearm | `ScrollViewer.cs:1275` |

With `UpdatesMode = Synchronous` (set by `SetDirectManipulationStateChangeHandler`,
`ScrollViewer.Internal.cs:74`) all of the above runs **inline in the animation tick, inside
`Compositor.RenderRootVisual`, before the picture walk**.

---

## 8. Virtualization interaction

### 8.1 `ListViewBase` / `ItemsStackPanel` (`ManagedVirtualizingPanelLayout`)

Subscription: `VirtualizingPanelLayout.managed.cs:205-216`

```csharp
ScrollViewer.ViewChanged += OnScrollChanged;              // :211
ScrollViewer.SizeChanged += OnScrollViewerSizeChanged;    // :212
ScrollViewer.ExtentSizeChanged += OnScrollViewerExtentSizeChanged;  // :213
```

`OnScrollChanged` (`:259-331`) is **fully synchronous with the `ViewChanged` raise**:

```csharp
var delta = ScrollOffset - _lastScrollOffset;                        // :266
var isLargeScroll = Abs(delta) > ViewportExtent;                     // :270
if (isLargeScroll) { ClearLines(clearContainer:false); SetDynamicSeed(...); }   // :272-294
while (unappliedDelta > 0)                                           // :296
{
    var scrollIncrement = GetScrollConsumptionIncrement(fillDirection);   // :303 (size of the disappearing view)
    unappliedDelta -= scrollIncrement;                                    // :311
    UpdateLayout(extentAdjustment: sign * -unappliedDelta, isScroll: true);  // :313
    (ItemsControl as ListViewBase)?.TryLoadMoreItems(LastVisibleIndex);      // :316 (__SKIA__)
}
ArrangeElements(_availableSize, ViewportSize);                       // :320  arranges EVERY materialized line
UpdateCompleted();                                                   // :321
if (isLargeScroll) OwnerPanel.InvalidateMeasure();                    // :327
```

`UpdateLayout` (`:462-494`) → `UnfillLayout` + `FillLayout` → `AddView`
(`:1024-1054`) which does `OwnerPanel.Children.Add(view)` (`:1029`) and **`view.Measure(slotSize)`**
(`:1037`) synchronously per newly realized container. For a fresh container this includes template
inflation + full subtree measure.

⇒ **Yes, ListView realization is synchronous with the scroll delta.** Under
`UpdatesMode.AsynchronousIdle` this runs on the Normal-priority `RequestUpdate` job, not inside the
render tick; under `Synchronous` it runs inside `Compositor.RenderRootVisual`.

`ArrangeElements` (`:439-453`) iterates **all** materialized lines and calls `item.container.Arrange(...)`
on each, on **every** `ViewChanged` — including intermediate ones. That is O(realized items) arrange
calls per scroll update, independent of how many items actually moved in/out.

`OwnerPanel.ShouldInterceptInvalidate = true/false` around the fill
(`:465, :483`) suppresses measure invalidation escaping the panel
(`UIElement.Layout.crossruntime.cs:21,28,82`).

### 8.2 `ItemsRepeater` (`ViewportManagerWithPlatformFeatures`)

```
FrameworkElement.PropagateEffectiveViewportChange
 └─ EventManager.EnqueueForEffectiveViewportChanged        FrameworkElement.EffectiveViewport.cs:384
     ... raised during UIElement.InnerUpdateLayout          UIElement.cs:980-983
     └─ ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged   :498-515
         └─ UpdateViewport(args.EffectiveViewport)          :502 -> :570-609
             └─ TryInvalidateMeasure()                      :607 -> :647-663
                 └─ m_owner.InvalidateMeasure()             :661
```

Critically, the historical Uno throttle is `#if !UNO_HAS_ENHANCED_LIFECYCLE`:

```csharp
#if !UNO_HAS_ENHANCED_LIFECYCLE
    // Uno workaround [BEGIN]: For perf considerations, do not invalidate the tree on each viewport update
    if (m_owner.Layout is VirtualizingLayout vl
        && vl.IsSignificantViewportChange(m_owner.LayoutState, _uno_viewportUsedInLastMeasure, m_visibleWindow))
#endif
    {
        TryInvalidateMeasure();
    }
```
`ViewportManagerWithPlatformFeatures.cs:599-608`

⇒ **On Skia the significance filter is compiled out: every viewport change invalidates the repeater's
measure.** Combined with `InnerUpdateLayout`'s loop (§1.3), a scroll frame can produce
measure → arrange → EVP → measure → arrange → EVP … until the viewport stabilizes,
bounded only by `MaxLayoutIterations = 250`.

`EnsureScroller` also `#if !UNO_HAS_ENHANCED_LIFECYCLE`-guards the "don't listen if empty" optimization
(`:523-529`) and `TryInvalidateMeasure`'s "nothing to render" guard (`:651-654`).

### 8.3 Where `EffectiveViewport` is computed

`FrameworkElement.GetEffectiveViewport` — `src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs:269-323`.
For a scroll port on Skia it is `parentViewport.Clip ∩ LayoutInformation.GetLayoutSlot(this)` (`:302-317`)
— note the `#if __SKIA__` branch deliberately does **not** offset by `ScrollOffsets` (`:302-308`), and the
"pseudo-intersect" rule at `:294-298`.

It is recomputed:
- on every arrange that changes the element's rect/clip: `UIElement.skia.cs:344` → `OnViewportUpdated()`
  → `PropagateEffectiveViewportChange()` (`FrameworkElement.EffectiveViewport.cs:235-253`);
- on every scroll delta via `InvalidateViewport()` (§5.1).

---

## 9. Every place a scroll delta can cause a synchronous measure / arrange

1. `ScrollViewer.Update` → `InvalidateArrange()` on non-intermediate offset change —
   `ScrollViewer.cs:1336`. Dirty-path to root (`UIElement.Layout.crossruntime.cs:80-119`).
2. `ScrollViewer.VerticalOffset`/`HorizontalOffset` DP set → TemplateBinding → `ScrollBar.Value` →
   `OnValueChanged` → `UpdateTrackLayout` → `Height`/`Margin` (`AffectsMeasure`) →
   `InvalidateMeasure` to root. `ScrollViewer.xaml:274,287`; `ScrollBar.mux.cs:729-733,1041,1054-1056`.
3. `ScrollViewer.ScrollableHeight/Width`, `ExtentHeight/Width`, `ViewportHeight/Width` DP sets in
   `UpdateDimensionProperties` → TemplateBinding → `ScrollBar.Maximum`/`ViewportSize` →
   `OnMaximumChanged` → `UpdateTrackLayout` (same as above). `ScrollViewer.cs:720-786`;
   `ScrollBar.mux.cs:745-751`; `ScrollViewer.xaml:271,275,283,288`.
4. `ViewChanged` → `VirtualizingPanelLayout.OnScrollChanged` → `UpdateLayout` → `AddView` →
   `view.Measure(...)`. `VirtualizingPanelLayout.managed.cs:313, 1037`.
5. Same handler → `ArrangeElements` → `container.Arrange(...)` for **all** materialized lines.
   `VirtualizingPanelLayout.managed.cs:320, 439-452`.
6. `VirtualizingPanelLayout.UpdateLayout` → `OwnerPanel.UpdateLayout()` — a **fully synchronous nested
   layout pass** (only on `!isScroll`, i.e. from `MeasureOverride`). `:485-493`.
7. `InvalidateViewport` → EVP enqueue → (next layout tick) → `ItemsRepeater` `InvalidateMeasure`.
   `SCP.Managed.cs:409`; `ViewportManagerWithPlatformFeatures.cs:661`.
8. `ScrollContentPresenter.MeasureOverride` → `Scroller?.OnScrollContentPresenterMeasured()` →
   `InvalidateArrange()` when anchoring is active — `ScrollContentPresenter.cs:205`;
   `ScrollViewer.Anchoring.cs:610-623`. Feeds back into the layout loop.
9. `ScrollViewer.AnchoringArrangeOverride` → **`UpdateDimensionProperties()` inline during arrange**
   (`ScrollViewer.Anchoring.cs:500`) → all six DP sets of (3) **inside** the arrange pass, plus
   `PerformPositionAdjustment` → `ChangeView(..., disableAnimation:true)`
   (`:427-452, :544, :554, :573, :590`) → a re-entrant `Set` during arrange.
10. `ScrollViewer.AfterArrange` → `UpdateDimensionProperties()` (`ScrollViewer.cs:672-687`) →
    `RecomputeOffsetsFromIntent()` (`:816`) → `ChangeViewInternal` → `ChangeView` → `SCP.Set` →
    `Updated` → `OnPresenterScrolled` → … (a second offset write inside the layout tick).
11. `TrimOverscroll` → `ChangeViewForOrientation` → `ChangeView(..., disableAnimation:true)` —
    `ScrollViewer.Managed.cs:160-189`, invoked from `UpdateDimensionProperties` (`ScrollViewer.cs:818-819`).
12. `_snapPointsTimer` tick → `DelayedMoveToSnapPoint` → `ChangeViewCore(..., disableAnimation:false)`
    → new 1-second animation. `ScrollViewer.cs:1367-1388`.
13. `OnScrollViewerSizeChanged` → `OwnerPanel?.InvalidateMeasure()` —
    `VirtualizingPanelLayout.managed.cs:333-336` (viewport resize, not per-delta).

---

## 10. Per-frame allocations (proven from source)

Composition / frame loop:
- `_runningAnimations.Keys.ToArray()` — **one array per rendered frame while any animation runs**:
  `Compositor.skia.cs:206`.
- `CompositionTarget.RaiseRendering`: `new FramePicture[...]`, `new List<(Window,object)>(...)`,
  `new RenderingEventArgs(...)` per raise: `CompositionTarget.Rendering.skia.cs:462-475`.
- `new FramePicture(picture)` per recorded frame: `CompositionTarget.Rendering.skia.cs:131`.
- `CompositionObject.ReEvaluateAnimation`: `ArrayPool<string>.Shared.Rent/Return` (pooled, good), but
  `animation.Evaluate()` returns `object` ⇒ **`Vector2` boxing per animated property per frame**:
  `CompositionObject.cs:174`, `KeyFrameAnimation.cs:44-53`.
- `KeyFrameEvaluator<T>.EvaluateInternal` — LINQ on the hot path, **per animation per frame**:
  ```csharp
  var lastKey = _keyFrames.Keys.Last();                                     // KeyFrameEvaluator.cs:92
  var nextKeyFrame = _keyFrames.Keys.FirstOrDefault(k => k >= currentFrame, lastKey);  // :103  CLOSURE
  var previousKeyFrame = _keyFrames.Keys.LastOrDefault(k => k <= currentFrame);        // :109  CLOSURE
  ```
  Two closure allocations + boxed `SortedDictionary.KeyCollection` enumerators, per frame.
- `Visual.Damage`: `SKPath` allocations are pooled (`_pathPool`, `Visual.skia.cs:26`;
  `Visual.Damage.skia.cs:76-77,180`) — good — but `Visual.Render` allocates
  `new SKPictureRecorder()` for shadow casters (`Visual.skia.cs:446`) and for picture collapsing (`:551`).

ScrollViewer / SCP:
- `new ScrollViewerViewChangedEventArgs { IsIntermediate = ... }` per `Update`: `ScrollViewer.cs:1356`.
- `ScrollContentPresenter.Update` animated path allocates, **per wheel tick**:
  `PowerEasingFunction` (`:474`), `Vector2KeyFrameAnimation` (`:477`), the `AnimationKeyFrame<Vector2>`
  entry in a `SortedDictionary` (`Vector2KeyFrameAnimation.cs:22`), the `OnFrame`/`OnStopped` display class
  (captures `visual`, `centeringOffsetX/Y`, `scrollAnimation`) (`:481-491`), the `Resolve`/`Lerp` closures
  and the `KeyFrameEvaluator<Vector2>` in `Vector2KeyFrameAnimation.Start` (`Vector2KeyFrameAnimation.cs:47-55`).
- `CompositionObject.TryGetAnimationController` **allocates a new `AnimationController` every call**
  (`CompositionObject.cs:261-270`) **and permanently subscribes it to `KeyFrameAnimation.Stopped`**
  (`AnimationController.cs:26`) — never unsubscribed (`Animation_Stopped` at `:100-104` only nulls fields).
  Called from `ScrollContentPresenter.IsScrollAnimationInProgress` (`SCP.Managed.cs:123`, hit on **every
  layout pass** via `RecomputeOffsetsFromIntent`, `ScrollViewer.cs:1574`) and from
  `ScrollContentPresenter.Update` (`:434`). ⇒ growing `Stopped` invocation list per animation + garbage.
- `RequestUpdate`: closure + `UIAsyncOperation` per scheduling (`ScrollViewer.cs:1308-1314`,
  `NativeDispatcher.EnqueueOperation` `NativeDispatcher.cs:343-405`).
- DP set/get of `double` boxes: `HorizontalOffset`/`VerticalOffset` are plain
  `DependencyProperty.Register(... typeof(double) ...)` (`ScrollViewer.cs:563-572, 583-592`), so each
  `SetValue`/`GetValue` round-trips through `object`.
- `ScrollViewer.Managed.cs` `record struct ScrollableOffsets` / `ScrollOptions` are structs — no alloc
  (`SCP.Managed.cs:847`, `:899`).
- `_trace?.Invoke($"...")` in `Set` — interpolated string is **only** built when trace logging is on
  (`_trace` is null otherwise, `SCP.Managed.cs:33-35`, call at `:347`). Safe.
- `AnchorRequestedEventArgs` is cached (`ScrollViewer.Anchoring.cs:282`). Good.
- Snap points: `GetSnapPointsInner(alignment).Distinct().OrderBy(f => f).ToList().AsReadOnly()` —
  4 allocations + a sort, and it is explicitly **not cached** (`// TODO: cache this call`,
  `ScrollViewer.SnapPoints.cs:263,294`; implementation `VirtualizingPanelLayout.cs:162-169`).

Input:
- `new PointerRoutedEventArgs(args, originalSource)` per wheel event: `InputManager.Pointers.Managed.cs:357`.
- `new ManipulationUpdatedEventArgs(...)` per pointer move / inertia tick:
  `GestureRecognizer.Manipulation.cs:425-427`.
- `new ScrollEventArgs()` per ScrollBar scroll event: `ScrollBar.mux.cs:937`.
- `EventManager.EnqueueForEffectiveViewportChanged`: `RemoveAll(x => x.Element == element)` closure
  (`EventManager.cs:31`) + `new EffectiveViewportChangedEventArgs(...)`
  (`FrameworkElement.EffectiveViewport.cs:384`).
- `DispatcherQueueTimer.DispatchRaiseTick`: `NativeDispatcher.Main.Enqueue(() => RaiseTick())` closure
  per tick (`DispatcherQueueTimer.others.cs:34`).
- `InteractionTracker.SetPosition`: `NativeDispatcher.Main.Enqueue(() => {...})` closure + `new
  InteractionTrackerValuesChangedArgs(...)` per tick (`InteractionTracker.cs:68-72`).

---

## 11. Why `AnchorPoint` and not `Offset` / `TransformMatrix`

**What `AnchorPoint` actually does in Uno's Skia compositor** — `Visual.skia.cs:174-184`:

```csharp
var totalOffset = GetTotalOffset();                 // Offset + ArrangeOffset,  Visual.skia.cs:927-932
var offsetMatrix = new Matrix4x4(1,0,0,0, 0,1,0,0, 0,0,1,0,
    totalOffset.X + AnchorPoint.X, totalOffset.Y + AnchorPoint.Y, 0, 1);   // :175-179
```

So Uno's `AnchorPoint` is **a raw pixel translation added to the offset**, *not* the WinUI normalized
[0..1] anchor fraction. (Deviation from WinUI — flagging it, since a WinUI-parity change here would break
scrolling.)

Reasons it is used instead of the alternatives, all verifiable:

1. **`Offset`/`ArrangeOffset` are owned by layout.** `UIElement.OnArrangeVisual` writes
   `visual.ArrangeOffset = new Vector3((float)rect.X, (float)rect.Y, 0) + _translation`
   (`src/Uno.UI/UI/Xaml/UIElement.skia.cs:360`) on every arrange. Any scroll offset stored there would be
   clobbered by the next arrange. `Offset` is the public WinUI `Visual.Offset` and is used by
   `UIElement.Translation`. `AnchorPoint` is the only free translation slot on the visual.
2. **`AnchorPoint` skips the accessibility notification.** `Offset`, `ArrangeOffset`, `Size` and
   `IsVisible` all fire `VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged?.Invoke(this)`
   (`Visual.skia.cs:294-305`), routed to `AccessibilityRouter.OnVisualOffsetOrSizeChanged`
   (`src/Uno.UI.Runtime.Skia/Accessibility/AccessibilityRouter.cs:58`) and the WASM accessibility bridge
   (`Uno.UI.Runtime.Skia.WebAssembly.Browser/Accessibility/WebAssemblyAccessibility.cs:47`).
   `AnchorPoint` has **no** such partial hook. That is a genuine per-frame saving.
3. **`TransformMatrix` is more expensive** in `TotalMatrix`: `GetTransform()` starts from
   `TransformMatrix` and multiplies (`Visual.skia.cs:198-223`), whereas `AnchorPoint` folds into the
   already-computed `offsetMatrix` with one add and no extra multiply.

**Is it the cheapest property to animate?** In terms of *invalidation*, **no — all three are identical**.
Every `Visual` property setter funnels through `SetProperty` → `OnPropertyChanged` →
`Visual.OnPropertyChangedCore` → `Compositor.InvalidateRender(this)` →
`InvalidateRenderPartial` (`Compositor.skia.cs:258-263`), which does:

```csharp
visual.SetMatrixDirty();   // TODO: only invalidate matrix when specific properties are changed
visual.InvalidatePaint();  // TODO: only repaint when "dependent" properties are changed
visual.CompositionTarget?.RequestNewFrame();
```

⇒ **Any** property change (even `Comment`) marks the whole subtree matrix-dirty **and** discards this
visual's rasterized picture **and** frees every ancestor's `_childrenPicture`. The two `TODO`s at
`Compositor.skia.cs:260-261` are exactly the missing optimization. There is no cheap-property fast path.

Marginal wins of `AnchorPoint` today: (a) no accessibility callback, (b) one fewer matrix multiply.
Everything else is identical to `Offset`.

Also note `ScrollContentPresenter.Update` sets `visual.Scale = targetScale` on **every** immediate update
(`SCP.Managed.cs:468`) even when zoom is 1 — `SetProperty(ref _scale, ...)` early-returns when the value
is unchanged (`CompositionObject.cs:399-410`), so that one is free. The two `StopAnimation` calls at
`:465-466` are not: `StopAnimation` on a property with no animation is a dictionary miss
(`CompositionObject.cs:237-245`) — cheap, but it does run `_animations?.TryGetValue` twice per touch move.

---

## 12. `ScrollView` / `ScrollPresenter` (the ported WinUI 3 stack)

- Present and substantial: `ScrollView.cs` 93 678 bytes, `ScrollView.Properties.cs` 33 718 bytes,
  `ScrollPresenter.cs` ~7 200 lines, plus `ScrollBarController`, `SnapPointWrapper`,
  `InteractionTrackerAsyncOperation`, `ScrollPresenterAnchoring`, the full `Scrolling*EventArgs` set.
  Directory listing: `src/Uno.UI/UI/Xaml/Controls/ScrollView/`, `.../ScrollPresenter/`.
- **It has a real InteractionTracker path.**
  - `ScrollPresenter.EnsureInteractionTracker` → `InteractionTracker.CreateWithOwner(compositor, owner)`
    (`ScrollPresenter.cs:1718-1730`).
  - `EnsureScrollPresenterVisualInteractionSource` → `VisualInteractionSource.Create(scrollPresenterVisual)`
    added to `m_interactionTracker.InteractionSources` (`:1732-1747`).
  - Expression animations wired to the tracker:
    `compositor.CreateExpressionAnimation("Vector2(it.Position.X, it.Position.Y)")` with
    `SetReferenceParameter("it", m_interactionTracker)` (`ScrollPresenter.cs:1701-1714`).
  - Wheel is redirected to the tracker in the input manager:
    `InputManager.Pointers.Managed.cs:341-355` walks `Visual.Parent` looking for
    `VisualInteractionSource { RedirectsPointerWheel: true }` and calls `tracker.ReceivePointerWheel(...)`
    (`:349`), `#if __SKIA__` only.
- **The Uno `InteractionTracker` is a managed simulation, not an OS/compositor tracker**
  (`src/Uno.UI.Composition/Composition/InteractionTracker/`, 1 057 lines total):
  - inertia runs on a **threadpool `System.Threading.Timer` at a fixed 17 ms**:
    ```csharp
    private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60
    _timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
    ```
    `InteractionTrackerActiveInputInertiaHandler.cs:24, 48`
  - `SetPosition` marshals to the UI thread with a closure per tick and then raises
    `OnPropertyChanged(nameof(Position))` (`InteractionTracker.cs:62-74`).
  - `ExpressionAnimation.IsTrackedByCompositor => false` (`ExpressionAnimation.cs:23`) — expression
    animations are **not** ticked per frame; they re-evaluate only via `OnPropertyChangedCore` →
    `RaiseAnimationFrame` (`ExpressionAnimation.cs:25-30`) driven by the `CompositionObject.Context`
    weak-reference graph (`CompositionObject.Context.cs:81-102`).
  ⇒ the tracker is off-vsync (17 ms vs 16.67 ms → visible beat), thread-hops per tick, and its
  Position→Visual propagation is a chain of managed property-change callbacks, not a compositor
  expression evaluated on the render thread.
- `ScrollViewer` does **not** use `ScrollPresenter`; the two stacks are independent. `ScrollViewer`'s
  template part is `PART_Scroller`/`ScrollContentPresenter` (`ScrollViewer.cs:52-62, 958-959`),
  `ScrollView`'s is `PART_ScrollPresenter` (`ScrollView.cs:315`). All the stock controls
  (`ListView`, `ComboBox`, …) use `ScrollViewer`.

**UNVERIFIED**: functional completeness / correctness of `ScrollView` at runtime — I only inspected the
source shape (57 `TODO`/`MUX_ASSERT` occurrences in `ScrollView.cs`) and did not build or run it.

---

## 13. Concrete jank sources provable from the code

Ordered roughly by expected impact.

**J1 — The wheel scroll animation is a fixed 1-second `Power(10, Out)` keyframe animation that is
restarted from scratch on every wheel tick.**
`ScrollContentPresenter.Managed.cs:474-479, 496`. Consequences:
- first frame after each tick jumps ≈14.9 % of the remaining distance, then decays — with a wheel
  arriving every ~50 ms, the visual velocity is a sawtooth, not constant;
- the logical `HorizontalOffset/VerticalOffset` run far ahead of the visual (each tick adds to
  `TargetVerticalOffset`, `:347`), so realization is computed for a viewport the user cannot see yet.
  The code itself documents this for trackpads (`ScrollContentPresenter.cs:300-310`).

**J2 — Every wheel tick emits a *non-intermediate* `ViewChanged`.**
`StartAnimation` → `StopAnimation` → `KeyFrameAnimation.Stop` → `Stopped` → SCP `OnStopped` →
`Updated(..., isIntermediate: false)`.
`CompositionObject.cs:90-93`; `KeyFrameAnimation.cs:60-64`; `ScrollContentPresenter.Managed.cs:482-488`.
That drives `ScrollViewer.Update(false)` → `InvalidateArrange()` (`ScrollViewer.cs:1336`) and restarts the
250 ms `_snapPointsTimer` (`:1275`) on every tick — a full arrange + timer churn per wheel notch.

**J3 — Reported offsets lag the rendered offset by exactly one frame.**
`AnimationFrame += OnFrame` (`SCP.Managed.cs:493`) is subscribed **before**
`StartAnimation` adds `ReEvaluateAnimation` (`CompositionObject.cs:97`), so `OnFrame` reads
`visual.AnchorPoint` **before** the frame's value is written. Virtualization therefore always realizes for
frame N−1's viewport ⇒ blank strip at the leading edge during fast scroll.

**J4 — Touch inertia's `AnchorPoint` write is also one frame late.**
`CompositionTarget.Rendering` is raised from `OnFramePictureRecorded`
(`CompositionTarget.Rendering.skia.cs:448-452`), i.e. *after* the picture for this frame was recorded
(`:198`). The inertia tick (`InertiaProcessor.cs:345-349`) can only affect frame N+1.

**J5 — A ScrollBar in the template forces a measure invalidation to the visual-tree root on every
published offset change.**
`ScrollViewer.xaml:274` (`Value="{TemplateBinding VerticalOffset}"`) →
`ScrollBar.OnValueChanged` (`ScrollBar.mux.cs:729-733`) → `UpdateTrackLayout` →
`LargeDecrease.Height` (`:1041`) and `PanningRoot.Margin` (`:1054-1056`), both `AffectsMeasure`
(`FrameworkElement.crossruntime.cs:90-96, 183`) → `InvalidateParentMeasureDirtyPath` to root
(`UIElement.Layout.crossruntime.cs:67-78`) → `XamlRoot.InvalidateMeasure` →
`CoreServices.RequestAdditionalFrame`. **Static, non-virtualized content still pays a full layout tick
per scroll frame.**

**J6 — On Skia, `ItemsRepeater`'s "significant viewport change" throttle is compiled out.**
`ViewportManagerWithPlatformFeatures.cs:599-608` (`#if !UNO_HAS_ENHANCED_LIFECYCLE`).
Every viewport update ⇒ `InvalidateMeasure` ⇒ another measure+arrange iteration inside
`InnerUpdateLayout`'s loop (`UIElement.cs:959-1012`, cap 250). Multiple full layout passes per scroll
frame are structurally possible.

**J7 — `ListView` realization is synchronous with the delta and re-arranges *all* materialized items.**
`VirtualizingPanelLayout.managed.cs:296-320`. The `while (unappliedDelta > 0)` loop can run many
`UpdateLayout` iterations for one delta; `ArrangeElements` (`:439-452`) arranges every materialized line
regardless of what moved; `AddView` measures fresh containers inline (`:1037`). A large delta additionally
takes the `isLargeScroll` path which clears **all** lines and re-realizes from an estimated seed
(`:272-294`) — an obvious hitch.

**J8 — Every wheel event does two full visual-tree hit tests on Skia desktop.**
`InputManager.Pointers.Managed.cs:319` (route) and `:366-372` (post-scroll over-state) —
the caching added to avoid "a ~17 ms visual tree traversal on every single scroll event"
(comment at `:289-293`) is gated on `OperatingSystem.IsIOS()` (`:310, :332`). Desktop Skia pays both.

**J9 — Setting `AnchorPoint` invalidates far more than it needs to.**
`Compositor.InvalidateRenderPartial` (`Compositor.skia.cs:258-263`) unconditionally calls
`SetMatrixDirty()` (recursive over the whole content subtree, `ContainerVisual.skia.cs:212-227`) **and**
`InvalidatePaint()` **and** `InvalidateParentChildrenPicture` up to the root (`Visual.skia.cs:245-258`).
Effect: the picture-collapsing optimization (`Visual.skia.cs:39-41`, gate `:541-544`) can never engage
while scrolling — it requires 50 consecutive unchanged frames. So every scroll frame re-walks and
re-emits the entire realized subtree with per-visual `SKPath` damage ops
(`Visual.Damage.skia.cs:27-69, 71-173`).

**J10 — Per-frame LINQ + boxing in the animation evaluator.**
`KeyFrameEvaluator.EvaluateInternal` allocates two closures and boxes a `SortedDictionary` key
enumerator every evaluation (`KeyFrameEvaluator.cs:92, 103, 109`); `KeyFrameAnimation.Evaluate` returns
`object` so the `Vector2` is boxed (`KeyFrameAnimation.cs:44-53`);
`Compositor.RenderRootVisual` allocates `_runningAnimations.Keys.ToArray()` (`Compositor.skia.cs:206`).
This is small per item but it is squarely in the 16 ms budget and produces steady Gen0 pressure ⇒
periodic GC pauses precisely during scrolling.

**J11 — `TryGetAnimationController` allocates and leaks a `Stopped` subscription on every call.**
`CompositionObject.cs:261-270` + `AnimationController.cs:21-27` (subscribe) with no unsubscribe
(`:100-104` only nulls fields). Called from `IsScrollAnimationInProgress` (`SCP.Managed.cs:123`) which
`RecomputeOffsetsFromIntent` hits on **every layout pass** (`ScrollViewer.cs:1573-1578`), and from
`Update` (`SCP.Managed.cs:434`). Growing invocation list ⇒ growing `Stop()` cost + garbage.

**J12 — Layout-driven offset corrections can fight the user.**
`AfterArrange` → `UpdateDimensionProperties` → `RecomputeOffsetsFromIntent()` → `ChangeViewInternal` →
`ChangeView(disableAnimation:true)` (`ScrollViewer.cs:672-687, 816, 1566-1621`), and
`TrimOverscroll` → `ChangeView` (`ScrollViewer.Managed.cs:160-189`). The code has explicit mitigations —
`ClearOffsetIntents()` on wheel (`ScrollContentPresenter.cs:251`) and on touch press
(`SCP.Managed.cs:519`), plus a `viewportChanged` gate on `TrimOverscroll` (`ScrollViewer.cs:810-820`) and
an `IsScrollAnimationInProgress` bail-out (`:1573-1578`) — but the correction still runs
**inside the arrange pass** when anchoring is on (`ScrollViewer.Anchoring.cs:500`) and re-enters
`ChangeView` from `PerformPositionAdjustment` (`:427-452`).

**J13 — Render frames are gated behind the Normal dispatcher queue.**
`NativeDispatcher.TryGetRenderAction` sets `normalItemsToProcessBeforeNextRenderAction = _queues[Normal].Count`
when it consumes a render action (`NativeDispatcher.cs:216`). Scroll work is enqueued at Normal priority
(`ScrollViewer.RequestUpdate` `:1308`, `CoreServices.RequestAdditionalFrame` `:73`,
`DispatcherQueueTimer.DispatchRaiseTick`), so heavy realization work directly postpones the next frame
instead of running in parallel with it.

**J14 — Snap-point queries are uncached and allocate.**
`ScrollViewer.SnapPoints.cs:263, 294` (`// TODO: cache this call`) →
`VirtualizingPanelLayout.cs:168`: `GetSnapPointsInner(...).Distinct().OrderBy(f => f).ToList().AsReadOnly()`
per `ChangeViewCore` with `shouldSnap: true` (every scrollbar drag delta, every `ChangeView`).

**J15 — Keyboard scrolling is not animated, and `LineUp/LineDown/PageUp/PageDown` are dead.**
`ScrollViewer.cs:1391-1395` (`disableAnimation: true`) and `:1911-1922`
(`HandleVerticalScroll`/`HandleHorizontalScroll` are `//UNO TODO` stubs). Perceived as "jumpy"
rather than "janky", but it is a smoothness gap vs WinUI.

**J16 — `AnchoringArrangeOverride` runs `UpdateDimensionProperties()` *during* arrange**
(`ScrollViewer.Anchoring.cs:500`), which sets six DPs, each of which can push a TemplateBinding into the
ScrollBar and dirty measure mid-arrange (see J5) — a documented recipe for extra layout iterations.
`EnsureAnchorElementSelection` additionally calls `GetDescendantBounds` →
`descendant.TransformToVisual(content)` per candidate (`:405-423, :330-341`), i.e. O(anchor candidates)
matrix chains per arrange. Only active when `Horizontal/VerticalAnchorRatio` is not `NaN`
(default `NaN`, `:45-61`; short-circuit at `:469-476`).

---

## 14. Thread-affinity summary

| Work | Thread | Cadence |
|---|---|---|
| Pointer/wheel dispatch, `SCP.Set`, `Update`, `AnchorPoint` write | UI | per input event |
| Composition animation tick (`RaiseAnimationFrame`) → `OnFrame` → `OnPresenterScrolled` | UI, **inside `Render()`** | per rendered frame |
| Picture recording (`rootVisual.RenderRootVisual`) | UI (`Render`, `CompositionTarget.Rendering.skia.cs:114`) | per rendered frame |
| Picture presentation (`Draw`) | native rendering/GPU thread (`OnNativePlatformFrameRequested:175`) | per native frame request |
| Layout (`UpdateLayout`) + `OnRenderFrameOpportunity` | UI, Normal-priority dispatcher job | per `RequestAdditionalFrame` |
| `ScrollViewer.Update` when `AsynchronousIdle` | UI, Normal-priority `Dispatcher.RunAsync` | coalesced, ≥1 per scroll burst |
| Touch inertia `Process` (composition timer) | UI, High-priority via `RaiseRendering` | per recorded frame |
| Touch inertia `Process` (dispatcher timer fallback) | threadpool `Timer` → UI via `NativeDispatcher.Main.Enqueue` | 33.3 ms |
| `_snapPointsTimer`, `_indicatorResetTimer`, `ConstantVelocityScroller` | threadpool `Timer` → UI | 250 ms / 4 s / 25 ms |
| `InteractionTracker` inertia (ScrollPresenter only) | threadpool `Timer` → UI via `Enqueue` | 17 ms |

---

## 15. Open questions / things I could not verify from source

- Whether native pointer-move events are coalesced to one per frame on the Win32/X11/macOS Skia hosts
  before reaching `InputManager` — I did not read the per-host input sources. If they are not coalesced,
  touch pan does one `AnchorPoint` write + one `InvalidateViewport` per raw move event, i.e. more than
  once per frame. **UNVERIFIED.**
- Actual measured cost of a single `HitTestOrRoot` on Skia desktop (the `~17ms` figure in
  `InputManager.Pointers.Managed.cs:289-291` is an iOS observation carried in a comment).
- Whether `ScrollView`/`ScrollPresenter` renders and scrolls correctly at runtime today.
- Whether `_isRenderingActive` (set by any `CompositionTarget.Rendering` subscriber, including the
  inertia timer) measurably raises idle CPU after inertia completes — depends on `Stop()` running
  promptly (`InertiaProcessor.cs:354-361`), which it does on the completion path (`:227-231`).
