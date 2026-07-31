# Avalonia scrolling: mechanism audit for smoothness

**Source tree:** `D:/Work/Avalonia`
**Commit audited:** `e81f3f7ff7802e8dd4dcd52137358bb08952ecc0` (2026-04-23, "Changes in CommandBar icon foreground inheritance (#21251)")
**Method:** direct source read. Every claim below cites `path:line`. Anything I could not verify in source is marked **UNVERIFIED**.

---

## 0. Executive summary (the shape of the answer)

Avalonia's scrolling is **100% UI-thread, layout-driven, and un-animated**:

1. `ScrollViewer.Offset` (a `StyledProperty<Vector>`) flows to `ScrollContentPresenter.Offset`, which on change calls `InvalidateArrange()` and, in `ArrangeOverride`, arranges its single child at `-Offset`. The child's `Bounds` change is what ultimately moves pixels, by being copied into `CompositionVisual.Offset` during the render-tree sync. There is **no render transform and no compositor-side scroll property animation** in the scroll path.
2. Mouse wheel is **instantaneous with a hard-coded 50 device-independent-pixels-per-delta-unit constant**, no easing, no animation, no accumulation, no smoothing.
3. Touch/pen inertia is a **UI-thread exponential-decay simulation** (`speed = v0 * 0.15^t`) re-driven every composition frame via `MediaContext.RequestAnimationFrame` → `Dispatcher.InvokeAsync(..., DispatcherPriority.Input)`. The velocity estimator is a direct port of Flutter's least-squares `VelocityTracker`.
4. Every scroll delta triggers a **full layout pass** (arrange at minimum; measure too when virtualizing), a **draw-list re-record** of the moved visual, and a **whole-viewport dirty rect** on the render thread.
5. `VirtualizingStackPanel` realizes **inline, synchronously, inside the same layout pass** (via `EffectiveViewportChanged` re-entrancy up to `MaxPasses = 10`). Default `CacheLength` is **0.0** — no buffer — so containers are materialized exactly at the viewport edge, in the frame in which they become visible. That is the single largest jank source.
6. Offsets are **layout-rounded to whole device pixels** (`LayoutHelper.RoundLayoutPoint`), so sub-pixel scroll deltas are quantized. At scaling 1.0 a 0.4 px delta moves nothing; a 0.6 px delta moves 1 px. This is a deliberate crispness/smoothness trade.

---

## 1. How the scroll offset is applied — trace from `Offset` to pixels

### 1.1 The property chain

`ScrollViewer.OffsetProperty` is a **StyledProperty with a coercion callback**:

```csharp
// src/Avalonia.Controls/ScrollViewer.cs:35-36
public static readonly StyledProperty<Vector> OffsetProperty =
    AvaloniaProperty.Register<ScrollViewer, Vector>(nameof(Offset), coerce: CoerceOffset);
```

```csharp
// src/Avalonia.Controls/ScrollViewer.cs:687-695
internal static Vector CoerceOffset(AvaloniaObject sender, Vector value)
{
    var extent = sender.GetValue(ExtentProperty);
    var viewport = sender.GetValue(ViewportProperty);
    var maxX = Math.Max(extent.Width - viewport.Width, 0);
    var maxY = Math.Max(extent.Height - viewport.Height, 0);
    return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
}
```

`ScrollContentPresenter` **adds owner** with the same coercion (`src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs:43-44`):

```csharp
public static readonly StyledProperty<Vector> OffsetProperty =
    ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>(new(coerce: ScrollViewer.CoerceOffset));
```

The two are kept in sync **bidirectionally and imperatively**:
* SV → SCP: template-priority binding created in `ScrollContentPresenter.AttachToScrollViewer()` (`ScrollContentPresenter.cs:360`): `IfUnset(OffsetProperty, p => Bind(p, owner.GetBindingObservable(ScrollViewer.OffsetProperty), Data.BindingPriority.Template))`.
* SCP → SV: `ScrollContentPresenter.cs:755` — `_owner?.SetCurrentValue(OffsetProperty, change.GetNewValue<Vector>());`

`ScrollBar.Value` is a third participant, bound to `ScrollViewer.Offset`'s ordinate (`Primitives/ScrollBar.cs:230`) and pushing back on change (`ScrollBar.cs:299-303`).

> **Smoothness note:** a single scroll delta therefore mutates at least 3 `AvaloniaProperty` values (SCP.Offset, SV.Offset, ScrollBar.Value), each of which allocates an `AvaloniaPropertyChangedEventArgs<T>` (`src/Avalonia.Base/AvaloniaObject.cs:790`, `src/Avalonia.Base/PropertyStore/ValueStore.cs:595,621`). Per-frame GC pressure during a fling is real but small.

### 1.2 Offset → arrange

```csharp
// src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs:746-756
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    if (change.Property == OffsetProperty)
    {
        if (!_arranging)
        {
            InvalidateArrange();
        }
        _owner?.SetCurrentValue(OffsetProperty, change.GetNewValue<Vector>());
    }
    ...
```

`ArrangeOverride` → `ArrangeWithAnchoring(finalSize)` → `ArrangeOverrideImpl(size, -Offset)`:

```csharp
// src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs:432-508 (excerpt)
var isAnchoring = Offset.X >= EdgeDetectionTolerance || Offset.Y >= EdgeDetectionTolerance;   // EdgeDetectionTolerance = 0.1, line 19
if (isAnchoring)
{
    EnsureAnchorElementSelection();
    ArrangeOverrideImpl(size, -Offset);              // line 462
    var anchorShift = TrackAnchor();
    if (anchorShift != default)
    {
        ... SetCurrentValue(OffsetProperty, newOffset) under _arranging = true ...
        ArrangeOverrideImpl(size, -Offset);          // line 495 — SECOND arrange in the same pass
    }
}
else
{
    ArrangeOverrideImpl(size, -Offset);              // line 500
}
Viewport = finalSize;                                // line 503
Extent  = ComputeExtent(finalSize);                  // line 504
_isAnchorElementDirty = true;                        // line 505
```

`ArrangeOverrideImpl` is inherited from `ContentPresenter` and simply offsets the child's arrange rect:

```csharp
// src/Avalonia.Controls/Presenters/ContentPresenter.cs:672-743 (excerpt)
var originX = offset.X;   // = -Offset.X
var originY = offset.Y;
...
var origin = new Point(originX, originY);
if (useLayoutRounding)
{
    origin = LayoutHelper.RoundLayoutPoint(origin, scale);   // line 735 — PIXEL SNAPPING
}
var boundsForChild = new Rect(origin, sizeForChild).Deflate(padding);
Child.Arrange(boundsForChild);                                // line 740
```

### 1.3 Arrange → `Bounds` → compositor

`Layoutable.Arrange` short-circuits when the rect is unchanged, otherwise runs `ArrangeCore`:

```csharp
// src/Avalonia.Base/Layout/Layoutable.cs:427-437
if (!IsArrangeValid || _previousArrange != rect)
{
    IsArrangeValid = true;
    ArrangeCore(rect);
    _previousArrange = rect;
}
```

```csharp
// src/Avalonia.Base/Layout/Layoutable.cs:744-751
var origin = new Point(originX, originY);
if (useLayoutRounding)
{
    origin = LayoutHelper.RoundLayoutPoint(origin, scale);
}
Bounds = new Rect(origin, size);
```

`Bounds` is in the `AffectsRender` set, so setting it queues a visual invalidation:

```csharp
// src/Avalonia.Base/Visual.cs:139-149
static Visual()
{
    AffectsRender<Visual>(
        BoundsProperty,     // line 142
        ClipProperty, ClipToBoundsProperty, IsVisibleProperty, OpacityProperty,
        OpacityMaskProperty, EffectProperty, HasMirrorTransformProperty);
```

```csharp
// src/Avalonia.Base/Visual.cs:418-421
public void InvalidateVisual() => PresentationSource?.Renderer.AddDirty(this);
```

`CompositingRenderer.UpdateCore` then copies `Bounds` into the composition visual **and re-records the draw list**:

```csharp
// src/Avalonia.Base/Rendering/Composition/CompositingRenderer.cs:147-174
foreach (var visual in _dirty)
{
    var comp = visual.CompositionVisual;
    if (comp == null) continue;
    visual.SynchronizeCompositionProperties();
    try
    {
        visual.Render(_recorder);
        comp.DrawList = _recorder.GetRenderResults();
    }
    finally { _recorder.Reset(); }
    visual.SynchronizeCompositionChildVisuals();
}
```

```csharp
// src/Avalonia.Base/Visual.Composition.cs:129-141
internal virtual void SynchronizeCompositionProperties()
{
    ...
    // TODO: Introduce a dirty mask like WPF has, so we don't overwrite properties every time
    comp.Offset = new (Bounds.Left, Bounds.Top, 0);      // line 137  <-- THIS is what moves pixels
    comp.Size = new (Bounds.Width, Bounds.Height);
    ...
```

### 1.4 Answer to Q1

> **The offset is applied by the layout system (arrange), not a render transform and not a compositor property animation.**
> The chain is: `Offset` set → `InvalidateArrange` → layout pass → `ScrollContentPresenter.ArrangeOverride` → `Child.Arrange(rect at -Offset)` → `Layoutable.Bounds` (layout-rounded) → `Visual.InvalidateVisual` → `CompositingRenderer.UpdateCore` → `CompositionVisual.Offset` → compositor batch commit → render thread draws.

Consequences that matter for smoothness:

* **The moved child's draw list is re-recorded every scroll frame** (`visual.Render(_recorder)` at `CompositingRenderer.cs:160`). For a `Panel`/`ItemsPresenter` that's just a background rect, so cheap; for a content visual with real drawing content it re-records its own geometry each frame. Descendants are *not* re-recorded (their `Bounds` relative to their parent don't change), which is the saving grace.
* **`ClipToBounds` on the SCP is forced true** (`ScrollContentPresenter.cs:111`: `ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);`), so the composition applies a clip per frame.
* **Dirty rect is effectively the whole viewport** every scroll frame: the moved subtree's old and new transformed bounds are both added (`src/Avalonia.Base/Rendering/Composition/Server/ServerCompositionVisual/ServerCompositionVisual.Update.cs:74-100,145-166`). The tracker is configurable (`ServerCompositionTarget.cs:56-66`, default `MaxDirtyRects = 8`, `MultiDirtyRectTracker`), but a full-viewport translation cannot be reduced to small rects — **there is no "scroll blit / bitmap shift" optimization anywhere in the pipeline** (searched; none found).
* **Pixel snapping**: `UseLayoutRounding` defaults to `true` and inherits (`src/Avalonia.Base/Layout/Layoutable.cs:133-134`). `RoundLayoutPoint` at scaling 1.0 does plain `Math.Round` (`src/Avalonia.Base/Layout/LayoutHelper.cs:203-213`). So a fling that produces 0.4 px/frame produces *zero* movement, then a 1 px jump — sub-pixel smoothness is deliberately sacrificed for crisp text. At 1.5×/2× scaling the quantum is 1/scale DIP, so it's less visible.

---

## 2. Wheel handling

### 2.1 The handler

```csharp
// src/Avalonia.Controls/Presenters/ScrollContentPresenter.cs:699-744
protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
{
    if (Extent.Height > Viewport.Height || Extent.Width > Viewport.Width)
    {
        var scrollable = Child as ILogicalScrollable;
        var isLogical = scrollable?.IsLogicalScrollEnabled == true;
        var x = Offset.X; var y = Offset.Y;
        var delta = e.Delta;

        // Shift+wheel horizontal fallback for platforms that don't do it natively
        if (e.KeyModifiers == KeyModifiers.Shift && MathUtilities.IsZero(delta.X))
            delta = new Vector(delta.Y, delta.X);
        else
            delta = AdjustDeltaForFlowDirection(delta, FlowDirection);

        if (Extent.Height > Viewport.Height)
        {
            double height = isLogical ? scrollable!.ScrollSize.Height : 50;   // line 723 — MAGIC 50
            y += -delta.Y * height;
            y = Math.Max(y, 0);
            y = Math.Min(y, Extent.Height - Viewport.Height);
        }
        if (Extent.Width > Viewport.Width)
        {
            double width = isLogical ? scrollable!.ScrollSize.Width : 50;     // line 731 — MAGIC 50
            x += -delta.X * width;
            ...
        }

        Vector newOffset = SnapOffset(new Vector(x, y), delta, true);
        bool offsetChanged = newOffset != Offset;
        SetCurrentValue(OffsetProperty, newOffset);
        e.Handled = !IsScrollChainingEnabled || offsetChanged;
    }
}
```

**Constants and semantics:**

| Thing | Value | Citation |
|---|---|---|
| Pixels per unit of wheel delta (physical scroll) | **50** DIP | `ScrollContentPresenter.cs:723,731` |
| Pixels per unit of wheel delta (logical scroll) | `ILogicalScrollable.ScrollSize` | `ScrollContentPresenter.cs:723,731` + `Primitives/ILogicalScrollable.cs:37` |
| `ScrollViewer.SmallChange` default (keyboard/line buttons, *not* wheel) | **16** DIP | `ScrollViewer.cs:158,169` (`DefaultSmallChange = 16`) |
| Animation / easing on wheel | **none** | no timer, no clock subscription, no transition in the handler |

There is **no "lines per detent" honoring of the OS setting** (no `SPI_GETWHEELSCROLLLINES` anywhere; grep found only the `wheelDelta = 120.0` divisor). The per-platform delta normalization is:

| Platform | Raw → `PointerWheelEventArgs.Delta` | Effective px/detent (× 50) | Citation |
|---|---|---|---|
| Win32 | `(HIWORD(wParam)) / 120.0` | 1.0 → **50 px** per classic detent; precision touchpads emit fractional 120ths → proportionally smaller | `src/Windows/Avalonia.Win32/WindowImpl.AppWndProc.cs:27,403-433` |
| macOS (precise / trackpad) | `scrollingDeltaY / 50` | **1:1 pixels** (AppKit already applies momentum + acceleration) | `native/Avalonia.Native/src/OSX/AvnView.mm:267-278` |
| macOS (classic wheel) | `scrollingDeltaY / 5` | ~**10 px** per AppKit line unit | same |
| Browser | `-(deltaY / 50)` | **1:1 CSS pixels** (does *not* interpret `deltaMode`) | `src/Browser/Avalonia.Browser/BrowserInputHandler.cs:165-168` |
| X11 (legacy buttons 4-7) | `±1` per button press | **50 px** | `src/Avalonia.X11/X11Window.cs:596-605` |
| X11 (XI2 smooth scroll valuators) | `(old - new) / scroller.Increment` | 50 px per increment; fractional for high-resolution devices | `src/Avalonia.X11/XI2Manager.cs:420-455` |

> **Smoothness verdict for wheel:** Instantaneous, unanimated, uncoalesced across events. On a classic notched mouse this produces a hard 50 px jump per detent — perceptually the least smooth path in the framework. On precision touchpads it inherits whatever pixel-level smoothness the OS provides, which is why Avalonia "feels smooth" on macOS/Windows-precision-touchpad and "feels steppy" on a notched wheel. The `Delta` is a `double`, so fractional deltas do flow through; the only thing that re-quantizes them is the layout rounding of §1.4.

### 2.2 Wheel forwarding from the scrollbar

Hovering over the scrollbar and wheeling still scrolls: `ScrollBar.OnPointerWheelChanged` re-raises a synthesized `PointerWheelEventArgs` on the SCP (`Primitives/ScrollBar.cs:262-281`) — one extra event-args allocation per wheel tick in that case.

### 2.3 Keyboard / page / line

All instantaneous, all `SetCurrentValue(OffsetProperty, ...)`:

```csharp
// src/Avalonia.Controls/ScrollViewer.cs:396-431
public void LineDown() => SetCurrentValue(OffsetProperty, Offset + new Vector(0, _smallChange.Height));
public void PageDown() => SetCurrentValue(OffsetProperty, Offset.WithY(Math.Min(Offset.Y + _viewport.Height, ScrollBarMaximum.Y)));
public void ScrollToEnd() => SetCurrentValue(OffsetProperty, new Vector(double.NegativeInfinity, double.PositiveInfinity));
```

`BringDescendantIntoView` (`ScrollContentPresenter.cs:239-281`) is likewise an immediate jump — **no smooth "scroll into view" animation exists in Avalonia**.

### 2.4 Search result: no smooth-scroll feature exists

`grep -rniE "smooth ?scroll|smoothscroll" --include=*.cs --include=*.axaml --include=*.md src/` → **zero hits**. There is no `SmoothScroll`, no scroll easing, no scroll transition, no `IsSmoothScrollingEnabled`.

---

## 3. Touch/pen: `ScrollGestureRecognizer` inertia model

File: `src/Avalonia.Base/Input/GestureRecognizers/ScrollGestureRecognizer.cs` (265 lines).

### 3.1 Constants

```csharp
// lines 12-20
// Pixels per second speed that is considered to be the stop of inertial scroll
internal const double InertialScrollSpeedEnd = 5;
public   const double InertialResistance     = 0.15;
...
private readonly static int s_defaultScrollStartDistance =
    (int)((AvaloniaLocator.Current?.GetService<IPlatformSettings>()?.GetTapSize(PointerType.Touch).Height ?? 10) / 2);
```

* `ScrollStartDistance` default = **5 DIP** (touch tap size 10 / 2; `src/Avalonia.Base/Platform/DefaultPlatformSettings.cs:17-25` — `TouchTapSize = 10`, and Win32 overrides only the *mouse* tap size, `src/Windows/Avalonia.Win32/Win32PlatformSettings.cs:17-24`).
* `InertialResistance = 0.15` is the **per-second** decay base.
* `InertialScrollSpeedEnd = 5` px/s is the stop threshold.

Velocity clamps live in the Flutter-derived tracker:

```csharp
// src/Avalonia.Base/Input/GestureRecognizers/VelocityTracker.cs:63-68
private const int    AssumePointerMoveStoppedMilliseconds = 40;
private const int    HistorySize                          = 20;
private const int    HorizonMilliseconds                  = 100;
private const int    MinSampleSize                        = 3;
private const double MinFlingVelocity                     = 50.0;   // logical px / second
private const double MaxFlingVelocity                     = 8000.0;
```

`VelocityTracker.cs:1-2` states outright: *"Code in this file is derived from https://github.com/flutter/flutter/blob/master/packages/flutter/lib/src/gestures/velocity_tracker.dart"*. It is a weighted **least-squares degree-2 polynomial fit** over up to 20 samples within a 100 ms horizon (`VelocityTracker.cs:97-178`, `LeastSquaresSolver.Solve` at 234-348), returning `pixels/second` with a confidence value. If no sample arrived in the last 40 ms it returns zero velocity (`VelocityTracker.cs:99-102`).

> **Quirk:** `GetFlingVelocity()` uses `ClampMagnitude(MinFlingVelocity, MaxFlingVelocity)` (`VelocityTracker.cs:200-203`), and `ClampMagnitude` **raises** sub-minimum non-zero velocities *up* to 50 px/s rather than suppressing the fling (`VelocityTracker.cs:25-30`). Flutter treats `minFlingVelocity` as a *threshold*. Net effect in Avalonia: releasing after a slow drag still produces a small ~23 px creep (see §3.4 math). Zero velocity is correctly special-cased to `Vector.Zero` (`VelocityTracker.cs:193-196`, and the `length != 0.0` guards at lines 21/28 with the explicit comment about avoiding NaN reaching `ScrollGestureEventArgs`).

### 3.2 Gesture lifecycle

* **`PointerPressed`** (lines 103-118): only for `PointerType.Touch or PointerType.Pen` with left button pressed. Mouse *never* produces scroll gestures. Creates a new `VelocityTracker`, seeds it with the press timestamp.
* **`PointerMoved`** (lines 120-163): after exceeding `ScrollStartDistance`, `_trackedRootPoint` is corrected by exactly `ScrollStartDistance` so *"scrolling does not start with a skip of ScrollStartDistance"* (lines 133-136) — a nice anti-jump detail. Then it raises one `ScrollGestureEventArgs(_gestureId, vector)` **per pointer move event** (line 153-154), where `vector = _trackedRootPoint - rootPoint` (i.e. the incremental delta). Identical consecutive deltas are dropped (`if (oldDelta == _delta) return;`, line 147). If handled, the pointer is captured (line 159).
* **`PointerReleased`** (lines 189-214): computes fling velocity and decides whether to start inertia.

```csharp
// lines 191-212
_inertia = _velocityTracker?.GetFlingVelocity().PixelsPerSecond ?? Vector.Zero;
e.Handled = true;
if (_inertia == null || _inertia == Vector.Zero
    || e.Timestamp == 0 || _lastMoveTimestamp == 0
    || e.Timestamp - _lastMoveTimestamp > 200          // stale-move cutoff: 200 ms
    || !IsScrollInertiaEnabled)
    EndGesture();
else
{
    _tracking = null;
    _stopWatch = Stopwatch.StartNew();
    _lastTime = _stopWatch.Elapsed;
    _inertiaStartTime = _lastTime;
    _currentInertiaGestureId = _gestureId;
    Target!.RaiseEvent(new ScrollGestureInertiaStartingEventArgs(_gestureId, _inertia.Value));
    MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
}
```

### 3.3 The inertia timer — what actually drives it

```csharp
// lines 216-263
private void OnAnimationRequested(TimeSpan _)
{
    // Calculate the current speed and dispatch the next inertia event. This is done asynchronously
    // so we have run the events with Input priority
    Dispatcher.UIThread.InvokeAsync(() =>
    {
        if (_gestureId != _currentInertiaGestureId || _stopWatch == null || _inertia is not Vector inertia)
            return;

        var timeSpan = _stopWatch.Elapsed;
        var elapsedSinceLastTick = timeSpan - _lastTime;
        _lastTime = timeSpan;

        var speed    = inertia * Math.Pow(InertialResistance, (_lastTime - _inertiaStartTime).TotalSeconds);
        var distance = speed * elapsedSinceLastTick.TotalSeconds;
        var scrollGestureEventArgs = new ScrollGestureEventArgs(_gestureId, distance);
        Target!.RaiseEvent(scrollGestureEventArgs);

        if (!scrollGestureEventArgs.Handled || scrollGestureEventArgs.ShouldEndScrollGesture)
        { EndGesture(); return; }

        // EndGesture using InertialScrollSpeedEnd only in the direction of scrolling
        if (CanVerticallyScroll && CanHorizontallyScroll && Math.Abs(speed.X) < InertialScrollSpeedEnd && Math.Abs(speed.Y) <= InertialScrollSpeedEnd)
        { /* NO-OP */ }
        else if (CanVerticallyScroll   && Math.Abs(speed.Y) <= InertialScrollSpeedEnd) { EndGesture(); return; }
        else if (CanHorizontallyScroll && Math.Abs(speed.X) <  InertialScrollSpeedEnd) { EndGesture(); return; }

        // Reschedule on the next animation frame. TopLevel.RequestAnimationFrame isn't available on
        // the Base project, so we use the global MediaContext
        MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
    }, DispatcherPriority.Input);
}
```

The "timer" is therefore **`MediaContext`'s render-frame clock**, not a `DispatcherTimer`:

```csharp
// src/Avalonia.Base/Media/MediaContext.Clock.cs:44-48, 79
public void RequestAnimationFrame(Action<TimeSpan> action)
{
    _parent.ScheduleRender(false);
    _queuedAnimationFrames.Enqueue(action);
}
...
public void RequestAnimationFrame(Action<TimeSpan> action) => _clock.RequestAnimationFrame(action);
```

`Pulse` (`MediaContext.Clock.cs:50-63`) swaps queues and invokes the callbacks at the top of every `RenderCore` (`MediaContext.cs:132-136`).

**Thread affinity & ordering per inertia step:**
1. Render-priority `DispatcherOperation` runs → `MediaContext.RenderCore` → `_clock.Pulse(now)` → `OnAnimationRequested` fires **on the UI thread**.
2. It does *not* apply the delta there; it posts a **new `DispatcherPriority.Input` operation**.
3. `Input` < `Render`, so that op runs *after* the current render pass completes and after all higher-priority work. It raises `ScrollGestureEvent` → `ScrollContentPresenter.OnScrollGesture` → `SetCurrentValue(OffsetProperty, …)` → `InvalidateArrange` → `MediaContext.BeginInvokeOnRender` → `ScheduleRender(true)`.
4. The offset therefore lands **one frame after** the frame that computed it. Distance accounting is still correct (`elapsedSinceLastTick` is real elapsed time), so the fling *distance* is right, but there is a constant ~1-frame latency and the sampling is at whatever rate the compositor is ticking.

### 3.4 The inertia curve, numerically

`v(t) = v₀ · 0.15^t` (t in seconds), stop when `|v| ≤ 5 px/s`.

* Stop time: `t_end = ln(5/v₀) / ln(0.15)`.
* Total distance: `∫₀^t_end v₀·0.15^t dt = v₀·(1 − 0.15^t_end) / −ln(0.15) ≈ v₀ / 1.897`.

| v₀ (px/s) | t_end (s) | distance (px) |
|---|---|---|
| 8000 (max clamp) | 3.89 | ≈ 4216 |
| 2000 | 3.16 | ≈ 1053 |
| 500 | 2.43 | ≈ 262 |
| 50 (min clamp) | 1.21 | ≈ 23 |

For comparison the decay is *much* steeper than Flutter's `FrictionSimulation` default (`_kDecelerationRate`/ `0.135` per **millisecond-scaled** formulation) — Avalonia's fling loses 85% of its speed in the first second. Perceptually this reads as a "short, quickly-braking" fling rather than an iOS-style long glide.

### 3.5 Snap points interaction with inertia

`ScrollContentPresenter.OnScrollGestureInertiaStartingEnded` (lines 642-696) pre-computes where inertia *would* land by **numerically integrating the same curve at a fixed 16 ms step**:

```csharp
// ScrollContentPresenter.cs:679-695
double GetDistance(double speed)
{
    var time = Math.Log(ScrollGestureRecognizer.InertialScrollSpeedEnd / Math.Abs(speed))
             / Math.Log(ScrollGestureRecognizer.InertialResistance);
    double timeElapsed = 0, distance = 0, step = 0;
    while (timeElapsed <= time)
    {
        double s = speed * Math.Pow(ScrollGestureRecognizer.InertialResistance, timeElapsed);
        distance += (s * step);
        timeElapsed += 0.016f;
        step = 0.016f;
    }
    return distance;
}
```

The resulting predicted offset is snapped (`SnapOffset`) and stored per gesture id; subsequent `OnScrollGesture` calls **clamp** the running offset to that snap point (`ScrollContentPresenter.cs:601-617`). So snapping is not a separate animation — it just truncates the existing inertia curve at the snap target. On gesture end, a final hard `SnapOffset` is applied (`ScrollContentPresenter.cs:634-640`), which can visibly *jump* if inertia ended off-target.

Note the guard at lines 657-660: if **both** axes have snap points enabled, the whole prediction is skipped (`return;`) — snap-during-inertia is single-axis only.

### 3.6 Where the recognizer is attached

Only in the default themes' `ScrollViewer` templates, on the `ScrollContentPresenter`:

```xml
<!-- src/Avalonia.Themes.Fluent/Controls/ScrollViewer.xaml:37-41 -->
<ScrollContentPresenter.GestureRecognizers>
  <ScrollGestureRecognizer CanHorizontallyScroll="{Binding CanHorizontallyScroll, ElementName=PART_ContentPresenter}"
                           CanVerticallyScroll="{Binding CanVerticallyScroll, ElementName=PART_ContentPresenter}"
                           IsScrollInertiaEnabled="{Binding (ScrollViewer.IsScrollInertiaEnabled), ElementName=PART_ContentPresenter}"/>
</ScrollContentPresenter.GestureRecognizers>
```
(also `src/Avalonia.Themes.Simple/Controls/ScrollViewer.xaml:21`, `src/Avalonia.Themes.Fluent/Controls/MenuScrollViewer.xaml:82`, and DataGrid's themes at `external/Avalonia.Controls.DataGrid/.../Themes/Fluent.xaml:542`).

`IsScrollInertiaEnabled` defaults to **true** (`ScrollViewer.cs:138-141`).

---

## 4. Per-frame UI-thread work during a scroll

### 4.1 Frame scheduling model

```csharp
// src/Avalonia.Base/Media/MediaContext.cs:76-105 (excerpt)
private void ScheduleRender(bool now)
{
    if (_nextRenderOp != null) { if (now) _nextRenderOp.Priority = DispatcherPriority.Render; return; }
    var priority = DispatcherPriority.Render;
    if (_inputMarkerOp == null)
    {
        _inputMarkerOp = _dispatcher.InvokeAsync(_inputMarkerHandler, DispatcherPriority.Input);
        _inputMarkerAddedAt = _time.Elapsed;
    }
    else if (!now && (_time.Elapsed - _inputMarkerAddedAt).TotalSeconds > MaxSecondsWithoutInput)
        priority = DispatcherPriority.Input;   // input starvation guard
    var renderOp = new DispatcherOperation(_dispatcher, priority, _render, throwOnUiThread: true);
    _nextRenderOp = renderOp;
    _dispatcher.InvokeAsyncImpl(renderOp, CancellationToken.None);
}
```

`MaxSecondsWithoutInput` comes from `DispatcherOptions.InputStarvationTimeout`, default **1 second** (`src/Avalonia.Base/Threading/DispatcherOptions.cs:20`). That is a *very* long fallback: if layout+render exceed the frame budget continuously, input can be starved for up to a second before the render op is demoted.

`RenderCore` runs layout (via `_invokeOnRenderCallbacks`) then commits:

```csharp
// src/Avalonia.Base/Media/MediaContext.cs:132-159
private void RenderCore()
{
    var now = _time.Elapsed;
    if (!_animationsAreWaitingForComposition)
        _clock.Pulse(now);                    // fires RequestAnimationFrame callbacks (inertia!)

    for (var c = 0; c < 10; c++)
    {
        FireInvokeOnRenderCallbacks();        // -> LayoutManager.ExecuteQueuedLayoutPass()
        if (_clock.HasNewSubscriptions) { _clock.PulseNewSubscriptions(); continue; }
        break;
    }

    if (_requestedCommits.Count > 0 || _clock.HasSubscriptions)
    {
        _animationsAreWaitingForComposition = CommitCompositorsWithThrottling();
        if (!_animationsAreWaitingForComposition && _clock.HasSubscriptions)
            _animationsTimer.Start();         // 16 ms fallback timer, MediaContext.cs:30-36
    }
}
```

Back-pressure: only **one composition batch may be in flight**:

```csharp
// src/Avalonia.Base/Media/MediaContext.Compositor.cs:64-82
private bool CommitCompositorsWithThrottling()
{
    if (_pendingCompositionBatches.Count > 0) return true;   // previous commit not processed yet
    if (_requestedCommits.Count == 0) return false;
    foreach (var c in _requestedCommits.ToArray()) CommitCompositor(c);
    return true;
}
```

If the render thread falls behind, the UI thread stops producing frames until `CompositionBatchFinished` (`MediaContext.Compositor.cs:41-56`) — the scroll then advances in one larger step rather than tearing. Frame *pacing* comes from the platform render timer: `WinUiCompositorConnection` / `DxgiConnection` (`_output.WaitForVBlank()`, `src/Windows/Avalonia.Win32/DirectX/DxgiConnection.cs:104`) / `DirectCompositionConnection` on Windows, `DisplayLinkTimer` on iOS, `ChoreographerTimer` on Android, `AvaloniaNativeRenderTimer` on macOS, `BrowserRenderTimer` on the web. All are `RunsInBackground => true` (background render thread) except `UiThreadRenderTimer`.

### 4.2 What happens on the UI thread per scroll delta

1. **Input dispatch** (synchronous on Win32: `Input(e)` straight from `WndProc`, `src/Windows/Avalonia.Win32/WindowImpl.AppWndProc.cs:973`).
2. `SetCurrentValue(OffsetProperty, …)` on SCP → coercion → 1 `AvaloniaPropertyChangedEventArgs<Vector>` → mirror to `ScrollViewer.Offset` → another one → `ScrollBar.Value` binding → another one.
3. `InvalidateArrange()` on the SCP → `LayoutManager.InvalidateArrange` + `InvalidateVisual` (`src/Avalonia.Base/Layout/Layoutable.cs:464-474`) → `QueueLayoutPass()` → `MediaContext.BeginInvokeOnRender` (`LayoutManager.cs:348-355`) → `ScheduleRender(true)`.
4. Render op runs → `LayoutManager.ExecuteLayoutPass()`:
   * up to `MaxPasses = 10` outer iterations (`LayoutManager.cs:23,149-158`), each `InnerLayoutPass()` doing up to 10 measure/arrange cycles (`LayoutManager.cs:232-243`).
   * after each inner pass, `RaiseEffectiveViewportChanged()` (`LayoutManager.cs:357-395`) recomputes each registered listener's viewport by walking its full ancestor chain (`CalculateEffectiveViewport`, lines 398-437) and loops again if that dirtied anything.
5. `CompositingRenderer.UpdateCore` re-syncs + re-records the dirty visuals, then `RequestCompositionBatchCommitAsync`.
6. `ScrollViewer.OnLayoutUpdated` → `RaiseScrollChanged()` allocates a `ScrollChangedEventArgs` and bubbles a routed event **every layout pass in which anything changed** (`ScrollViewer.cs:858-875`).

### 4.3 Does scroll trigger `InvalidateMeasure`?

* **Non-virtualized content: no.** Only `InvalidateArrange` (`ScrollContentPresenter.cs:748-752`). `MeasureOverride` is skipped entirely (measure is valid), and `Layoutable.Arrange` short-circuits for children whose rect is unchanged (`Layoutable.cs:427`). But note `Layoutable.ArrangeCore` still calls `ArrangeOverride(size)` on the scrolled child, and a plain `Panel.ArrangeOverride` **iterates all children** every scroll frame calling `Arrange(rect)` — each returns immediately, but the loop itself is O(N). A non-virtualized `StackPanel` with 10 000 children pays a 10 000-iteration loop per scroll frame.
* **Virtualized content: yes, essentially every frame.** See §5.
* `Extent`/`Viewport` changes re-coerce `Offset` (`ScrollContentPresenter.cs:766-781`, `ScrollViewer.cs:759-766`), which can feed back into another arrange.

### 4.4 Scroll anchoring cost (per frame)

`ArrangeWithAnchoring` sets `_isAnchorElementDirty = true` at the end of **every** arrange (`ScrollContentPresenter.cs:505`). The next arrange then runs `EnsureAnchorElementSelection()` (lines 856-896), which iterates **all** registered anchor candidates and, for each, calls `TranslateBounds` → `Control.TranslatePoint` (an ancestor-walk transform computation):

```csharp
// ScrollContentPresenter.cs:872-885
foreach (var element in _anchorCandidates)
{
    if (element.IsVisible && GetViewportBounds(element, out var bounds))
    {
        var distance = (Vector)bounds.Position;
        var candidateDistance = Math.Abs(distance.Length);
        if (candidateDistance < bestCandidateDistance) { bestCandidate = element; bestCandidateDistance = candidateDistance; }
    }
}
```

`VirtualizingStackPanel.ArrangeOverride` registers **every realized, viewport-intersecting element** as an anchor candidate on every arrange (`VirtualizingStackPanel.cs:267-280`). So the anchoring pass is O(visible items × tree depth) per scroll frame. And when the anchor shifts, the SCP **arranges the entire content twice in one pass** and mutates `Offset` mid-arrange (`ScrollContentPresenter.cs:466-496`).

Anchoring is *disabled* near the origin: `isAnchoring = Offset.X >= 0.1 || Offset.Y >= 0.1` (line 454).

---

## 5. Virtualization interaction

`ItemsRepeater` **does not exist** in this tree (grep found it only in `.csproj`/`AssemblyInfo`/`ItemsSourceView` comments). The virtualizing panels are `VirtualizingStackPanel` (1430 lines) and `VirtualizingCarouselPanel`.

### 5.1 VSP is *pixel*-scrolled, not logically scrolled

`VirtualizingStackPanel` does **not** implement `ILogicalScrollable` (only `DateTimePickerPanel`, `VirtualizingCarouselPanel`, `ItemsPresenter` and the presenters/`ScrollViewer` do). It participates via `EffectiveViewportChanged` instead:

```csharp
// src/Avalonia.Controls/VirtualizingStackPanel.cs:95-103
public VirtualizingStackPanel()
{
    _recycleElement = RecycleElement; ...
    _bufferFactor = Math.Max(0, CacheLength);
    EffectiveViewportChanged += OnEffectiveViewportChanged;
}
```

That means smooth pixel scrolling (good), at the cost of an extent that is **estimated**:

```csharp
// VirtualizingStackPanel.cs:73
private double _lastEstimatedElementSizeU = 25;     // initial guess: 25 DIP per item
```

```csharp
// VirtualizingStackPanel.cs:737-749
private Size CalculateDesiredSize(Orientation orientation, int itemCount, in MeasureViewport viewport)
{
    var sizeU = 0.0;
    if (viewport.lastIndex >= 0)
    {
        var remaining = itemCount - viewport.lastIndex - 1;
        sizeU = viewport.realizedEndU + (remaining * _lastEstimatedElementSizeU);
    }
    return orientation == Orientation.Horizontal ? new(sizeU, viewport.measuredV) : new(viewport.measuredV, sizeU);
}
```

`_lastEstimatedElementSizeU` is the running average of realized elements' desired sizes (`VirtualizingStackPanel.cs:770-797`). With non-uniform item heights the **extent changes as you scroll**, which re-coerces `Offset` and rescales the scrollbar thumb — the classic "thumb creeps/jitters while scrolling a heterogeneous list". Not a frame-rate problem, but a perceived-smoothness problem.

### 5.2 Realization is inline and synchronous, in the same layout pass

Sequence within one `MediaContext` render tick:

1. `LayoutManager.InnerLayoutPass()` → arrange with new offset → child `Bounds` change.
2. `LayoutManager.RaiseEffectiveViewportChanged()` (`LayoutManager.cs:357-395`) recomputes VSP's viewport → `VirtualizingStackPanel.OnEffectiveViewportChanged` (line 1222) → possibly `InvalidateMeasure()` (line 1328).
3. Because `RaiseEffectiveViewportChanged` returns `true` when it dirtied the queues, the outer `for (var pass = 0; pass < MaxPasses; ++pass)` loop (`LayoutManager.cs:149-158`) **runs the layout again in the same pass**.
4. `VirtualizingStackPanel.MeasureOverride` → `RealizeElements` creates/recycles containers and measures them **synchronously** (`VirtualizingStackPanel.cs:891-967`, notably `var e = GetOrCreateElement(items, index); e.Measure(availableSize);` at lines 917-920 and 950-952).

So there is **no blank frame** and no deferred/idle realization — but the cost of materializing new containers (template inflation, bindings, measure) lands **entirely inside the frame in which the item scrolls into view**. That is a spike, not a stall: it converts into a dropped frame when a row is expensive.

### 5.3 `CacheLength` default is 0 — no buffer

```csharp
// VirtualizingStackPanel.cs:58-60
public static readonly StyledProperty<double> CacheLengthProperty =
    AvaloniaProperty.Register<VirtualizingStackPanel, double>(nameof(CacheLength), 0.0,
        validate: v => v is >= 0 and <= 2);
```

`_bufferFactor = CacheLength` and `bufferSize = viewportSize * _bufferFactor` (`VirtualizingStackPanel.cs:1236`). **With the default 0.0 the extended viewport equals the viewport**, so:

* `CalculateExtendedViewport` (lines 1169-1220) returns the raw viewport.
* In `OnEffectiveViewportChanged`, "Case 1a: The new viewport exceeds the old extended viewport" (lines 1252-1257) is true on essentially *every* scroll step in the scroll direction → `needsMeasure = true` → `InvalidateMeasure()` per frame.
* Realization happens exactly at the edge — the item is created in the frame in which its first pixel becomes visible.

Raising `CacheLength` (max 2.0 = two viewports of buffer each side) is the documented escape hatch; the code even contains dedicated optimizations to avoid redundant measures at list boundaries (`_hasReachedStart` / `_hasReachedEnd`, lines 1280-1294, with the comment *"This prevents redundant Measure-Arrange cycles when at list beginning."*).

### 5.4 Anti-hitch measures that *do* exist in VSP

* **Recycling pool** keyed by recycle key (`VirtualizingStackPanel.cs:1145-1160`, `GetRecycledElement`/`RecycleElement`), so containers are reused rather than re-templated.
* **Double-buffered realized element lists**: measure writes into `_measureElements`, then the two are swapped (`VirtualizingStackPanel.cs:226-228`) — avoids mutating the live list mid-measure.
* **Disjunct-viewport fast path**: on a big jump, recycle everything at once (`VirtualizingStackPanel.cs:218-220`).
* **`_isWaitingForViewportUpdate`**: while a `ScrollIntoView` is in flight, `MeasureOverride` returns an estimated size instead of doing work (`VirtualizingStackPanel.cs:197-200`, with the explanatory comment at lines 655-660).
* **Focused-element pinning** so the focused container isn't recycled out from under the user (`VirtualizingStackPanel.cs:230-232, 286-295`).
* **Scroll anchoring** registration (lines 267-280) to keep the visual position stable when realized sizes differ from estimates.

### 5.5 `VirtualizingCarouselPanel` note

Contains the only jitter-related comment in the controls tree: *"Clamp so totalDelta cannot cross zero (absorbs touch jitter)."* (`src/Avalonia.Controls/VirtualizingCarouselPanel.cs:1163`).

---

## 6. Logical scrolling (the "steppy" path)

When the SCP's child is an `ILogicalScrollable` with `IsLogicalScrollEnabled == true`:

* SCP's own measure/arrange are bypassed entirely (`ScrollContentPresenter.cs:401-403, 424-426`) — the child does its own thing.
* The wheel scrolls by `ILogicalScrollable.ScrollSize` **units, integer-quantized** (`ScrollContentPresenter.cs:723,731`).
* Touch gestures accumulate fractional remainders per gesture id so partial units aren't lost:

```csharp
// ScrollContentPresenter.cs:554-572
if (isLogical)
{
    var logicalUnits = delta.Y / logicalScrollItemSize.Y;
    delta = delta.WithY(delta.Y - logicalUnits * logicalScrollItemSize.Y);
    dy = logicalUnits;
}
```
(Note: `logicalUnits` here is a `double`, so this is *not* integer quantization in the touch path; the remainder bookkeeping is preserved in `_activeLogicalGestureScrolls`, lines 592-597.)

In this tree only `DateTimePickerPanel` and `VirtualizingCarouselPanel` actually enable logical scrolling, so mainstream lists are pixel-scrolled.

---

## 7. Scrollbar drag path

* `Thumb` raises `DragDeltaEvent` **once per pointer-move event** with the incremental vector (`src/Avalonia.Controls/Primitives/Thumb.cs:107-120`).
* `Track.ThumbDragged` either applies immediately or defers:

```csharp
// src/Avalonia.Controls/Primitives/Track.cs:463-505
private void ThumbDragged(object? sender, VectorEventArgs e)
{
    if (IgnoreThumbDrag) return;
    if (DeferThumbDrag) { _deferredThumbDrag = e; InvalidateArrange(); }
    else ApplyThumbDrag(e);
}
...
private void ThumbDragCompleted(object? sender, EventArgs e) => ApplyDeferredThumbDrag();
```

`DeferThumbDrag` is template-bound to `ScrollViewer.IsDeferredScrollingEnabled` (`src/Avalonia.Themes.Fluent/Controls/ScrollBar.xaml:151,234`; Simple theme lines 31, 87), which defaults to **false** (`ScrollViewer.cs:146-148`). So dragging the thumb scrolls live, one layout pass per pointer move. `IsDeferredScrollingEnabled = true` is the escape hatch for very expensive content: the content only updates on thumb release.

---

## 8. Input → frame plumbing (latency characteristics)

* **Win32 input is delivered synchronously from `WndProc`** — no dispatcher hop: `if (e != null && Input != null) { Input(e); … }` (`src/Windows/Avalonia.Win32/WindowImpl.AppWndProc.cs:973-975`).
* **Posted dispatcher work outranks OS input** on Win32 by design:

```csharp
// src/Windows/Avalonia.Win32/Win32DispatcherImpl.cs:24-32
public void Signal() =>
    // Messages from PostMessage are always processed before any user input,
    // so Win32 should call us ASAP
    PostMessage(_messageWindow, (int)WindowsMessage.WM_DISPATCH_WORK_ITEM, ...);
```

* The dispatcher **never yields to pending input for jobs above `Input` priority**:

```csharp
// src/Avalonia.Base/Threading/Dispatcher.Queue.cs:172-176
// We don't stop for executing jobs queued with >Input priority
if (job.Priority > DispatcherPriority.Input)
    ExecuteJob(job);
```
`DispatcherPriority.Render` > `Input` (`src/Avalonia.Base/Threading/DispatcherPriority.cs:32,97`). Combined effect on Win32: **each input event tends to get its own layout+render pass** (the posted render signal is dequeued before the next queued mouse/touch message). Great for latency; bad for throughput, because there is no natural coalescing of a burst of wheel/touch-move events into one frame. The only guards are the single-in-flight composition batch (§4.1) and the 1-second input-starvation demotion.
* **Avalonia has no pointer-move coalescing on the UI side.** The only coalescing is platform-level intermediate-point capture (iOS `GetCoalescedTouches`, `src/iOS/Avalonia.iOS/InputHandler.cs:94-101`; Browser `getCoalescedEvents`, `src/Browser/Avalonia.Browser/BrowserInputHandler.cs:76-105`; Win32 `CreateIntermediatePoints`, `WindowImpl.AppWndProc.cs:397`), and those are exposed as `IntermediatePoints` for ink-style consumers, **not** used by `ScrollGestureRecognizer` (which only reads `e.GetPosition(null)`, `ScrollGestureRecognizer.cs:124`).
* macOS/Native backend pumps input jobs before dispatching: `Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);` (`src/Avalonia.Native/TopLevelImpl.cs:237`).

---

## 9. Known smoothness limitations visible in the code

Ranked by likely perceptual impact.

1. **No compositor-side scrolling at all.** Every pixel of movement requires a UI-thread layout pass and a compositor batch commit. If the UI thread stalls (GC, expensive item template, app code on `ScrollChanged`), the scroll stalls. The compositor *can* animate `Offset` server-side — Avalonia already does it for pull-to-refresh (`src/Avalonia.Controls/PullToRefresh/ScrollViewerIRefreshInfoProviderAdapter.cs:154-189`, an implicit `Vector3KeyFrameAnimation` targeting `"Offset"` with `this.FinalValue`, 150 ms) — and the render thread keeps ticking itself while animations are live (`ServerCompositor.RenderCore`: *"Request a tick if we have active animations"*, `src/Avalonia.Base/Rendering/Composition/Server/ServerCompositor.cs:280-282`). **The machinery exists; scroll simply doesn't use it.**
2. **Virtualization realizes at the viewport edge with zero buffer by default** (`CacheLength = 0.0`, `VirtualizingStackPanel.cs:58-60`). Every newly-visible row's template inflation happens inside the visible frame.
3. **Wheel is a hard 50 px jump per detent, unanimated** (`ScrollContentPresenter.cs:723,731`). No easing, no per-frame interpolation, no OS lines-per-detent.
4. **Layout rounding quantizes offsets to whole device pixels** (`LayoutHelper.RoundLayoutPoint`, `Layoutable.cs:746-751`, `ContentPresenter.cs:733-736`) — sub-pixel fling velocities produce stepped motion at scaling 1.0.
5. **Scroll anchoring can arrange twice per frame and mutate `Offset` mid-arrange** (`ScrollContentPresenter.cs:466-496`), and re-selects the anchor by walking every candidate's transform chain every frame (`ScrollContentPresenter.cs:856-896` + VSP registering all visible items each arrange, `VirtualizingStackPanel.cs:267-280`).
6. **Estimated extent** in VSP makes the scrollbar and `Offset` coercion drift while scrolling heterogeneous lists (`VirtualizingStackPanel.cs:73, 737-749`).
7. **Inertia is applied one frame late** and is gated by render-frame cadence: `RequestAnimationFrame` → `Dispatcher.InvokeAsync(..., DispatcherPriority.Input)` (`ScrollGestureRecognizer.cs:216-261`). Under compositor back-pressure (`MediaContext.Compositor.cs:64-72`) the inertia tick frequency drops; distance stays correct but motion becomes chunky.
8. **`ClampMagnitude(MinFlingVelocity, …)` boosts tiny velocities to 50 px/s** instead of suppressing them (`VelocityTracker.cs:25-30, 200-203`) → ~23 px of unrequested drift after a slow release.
9. **Full-viewport dirty rect every scroll frame**; no scroll-blit optimization (`ServerCompositionVisual.Update.cs:74-100,145-166`; tracker config at `ServerCompositionTarget.cs:56-66`).
10. **Explicit TODO: `"Introduce a dirty mask like WPF has, so we don't overwrite properties every time"`** (`src/Avalonia.Base/Visual.Composition.cs:135`) — every dirty visual re-pushes ~12 composition properties per frame.
11. **`InputStarvationTimeout` default of 1 s** (`DispatcherOptions.cs:20`) means a persistently over-budget render loop can starve input for up to a second before the render op is demoted — the comment itself notes *"This may need to be lowered on resource-constrained platforms"*.
12. **Snap-point support is single-axis during inertia** — if both axes have snap points the whole prediction is skipped (`ScrollContentPresenter.cs:657-660`), and the end-of-gesture `SnapOffset` (line 639) is an unanimated jump.
13. **Per-frame allocations**: `ScrollGestureEventArgs` per move/inertia tick (`ScrollGestureRecognizer.cs:153, 234`), `ScrollChangedEventArgs` per changed layout pass (`ScrollViewer.cs:868`), one `AvaloniaPropertyChangedEventArgs<T>` per property mutation (`AvaloniaObject.cs:790`).
14. **Non-virtualized panels pay O(children) per scroll frame** because `ArrangeCore` re-invokes `ArrangeOverride` on the scrolled child even though only its origin moved (`Layoutable.cs:669-753`, `Layoutable.cs:760-778`).

---

## 10. Constant reference table

| Constant | Value | Location |
|---|---|---|
| `ScrollContentPresenter.EdgeDetectionTolerance` | 0.1 | `ScrollContentPresenter.cs:19` |
| Wheel pixels per delta unit (physical scroll) | 50 | `ScrollContentPresenter.cs:723,731` |
| `ScrollViewer.DefaultSmallChange` | 16 | `ScrollViewer.cs:158` |
| `ScrollViewer.LargeChange` (non-logical) | `Viewport` | `ScrollViewer.cs:747` |
| `ScrollGestureRecognizer.InertialResistance` | 0.15 (per second, exponential base) | `ScrollGestureRecognizer.cs:14` |
| `ScrollGestureRecognizer.InertialScrollSpeedEnd` | 5 px/s | `ScrollGestureRecognizer.cs:13` |
| Default `ScrollStartDistance` | 5 DIP (touch tap size 10 / 2) | `ScrollGestureRecognizer.cs:19` + `DefaultPlatformSettings.cs:17` |
| Stale-move cutoff before fling is cancelled | 200 ms | `ScrollGestureRecognizer.cs:200` |
| Snap-prediction integration step | 0.016 s | `ScrollContentPresenter.cs:690-691` |
| `VelocityTracker.HistorySize` | 20 samples | `VelocityTracker.cs:64` |
| `VelocityTracker.HorizonMilliseconds` | 100 ms | `VelocityTracker.cs:65` |
| `VelocityTracker.MinSampleSize` | 3 | `VelocityTracker.cs:66` |
| `VelocityTracker.AssumePointerMoveStoppedMilliseconds` | 40 ms | `VelocityTracker.cs:63` |
| `MinFlingVelocity` / `MaxFlingVelocity` | 50 / 8000 px/s | `VelocityTracker.cs:67-68` |
| `VirtualizingStackPanel.CacheLength` default / max | 0.0 / 2.0 | `VirtualizingStackPanel.cs:58-60` |
| Initial per-item size estimate | 25 DIP | `VirtualizingStackPanel.cs:73` |
| `LayoutManager.MaxPasses` | 10 | `LayoutManager.cs:23` |
| Layout-storm abort threshold | 153 callbacks | `MediaContext.cs:197-198` |
| Animations fallback timer | 16 ms | `MediaContext.cs:30-36` |
| `DispatcherOptions.InputStarvationTimeout` | 1 s | `DispatcherOptions.cs:20` |
| Default `MaxDirtyRects` | 8 | `ServerCompositionTarget.cs:58` |
| Win32 wheel divisor | 120.0 | `WindowImpl.AppWndProc.cs:27` |
| macOS wheel divisor (precise / classic) | 50 / 5 | `native/Avalonia.Native/src/OSX/AvnView.mm:269-273` |
| Browser wheel divisor | 50 | `BrowserInputHandler.cs:168` |
| ScrollBar auto-hide show/hide delays | 0.5 s / 2 s | `Primitives/ScrollBar.cs:71-78` |

---

## 11. Things I could not verify

* **UNVERIFIED:** whether, in practice on Win32, a burst of `WM_MOUSEWHEEL` messages ever coalesces into a single layout pass. The code paths (`Win32DispatcherImpl.Signal` comment at line 25-26 + `Dispatcher.Queue.cs:172-176`) strongly imply one layout+render per input message, but I did not instrument a running app to confirm.
* **UNVERIFIED:** actual measured frame times / dropped-frame counts for any scenario. This audit is static only.
* **UNVERIFIED:** whether `ServerCompositor`'s dirty-rect tracker degenerates to a single full-viewport rect during scroll on all backends (the `RegionDirtyRectTracker` vs `MultiDirtyRectTracker` choice depends on `platformRender.SupportsRegions`, `ServerCompositionTarget.cs:56-66`).
* **UNVERIFIED:** Android/iOS touch sample rates reaching `ScrollGestureRecognizer` (i.e. whether `PointerMoved` arrives at 60 Hz or at the digitizer rate).
