# 11 — Uno per-platform input plumbing: what breaks scroll smoothness

**Scope**: audit of every platform input source in `D:/Work/uno-worktrees/scrollsmooth/src/` that feeds
`IUnoCorePointerInputSource` (and therefore `InputManager.Pointers.Managed` → `ScrollContentPresenter` /
`InteractionTracker`). Everything below is read from source; line numbers are from the working tree at
commit `3668dcf516` (branch `dev/mazi/smooth-scroll`).

Anything I could not confirm in source is explicitly marked **UNVERIFIED**.

---

## 0. Executive summary — the cross-cutting defects

These are not platform-specific; they poison *every* platform's scroll path and are the highest-value fixes.

### 0.1 `MouseWheelDelta` is `int` — the entire wheel pipeline is integer-quantized

`src/Uno.UI/UI/Input/WinRT/PointerPointProperties.cs:254`

```csharp
public int MouseWheelDelta { get; internal set; }
```

Every platform that has a sub-pixel / high-resolution scroll signal must truncate or round it into this
`int` at the platform boundary. There is no fractional carry anywhere: the residue is dropped per event,
not accumulated. Concretely:

| Platform | Line | Loss |
|---|---|---|
| Win32 | `Win32WindowWrapper.Pointers.cs:150` (`GET_WHEEL_DELTA_WPARAM` → `short`) | OS already quantizes, but sub-120 PTP deltas survive to be killed later (§0.2) |
| X11 | `X11PointerInputSource.XInput.cs:285` `MouseWheelDelta = (int)Math.Round(wheelDelta)` | fractional valuator delta dropped every event |
| macOS | `UNOWindow.m:1416-1417` `(int32_t)(event.scrollingDeltaX * factor)` | **truncates**; a 0.9 px precise delta becomes 0 |
| WASM (Skia) | `BrowserPointerInputSource.cs:284` `MouseWheelDelta = (int)wheel.delta` | truncates fractional `deltaY` |
| WASM (DOM) | `BrowserPointerInputSource.wasm.cs:317` `MouseWheelDelta = (int)wheel.delta` | same |
| iOS | `AppleUIKitPointerInputSource.cs:487-488` `(int)(translation.X * multiplier)` | truncates (partly mitigated by the accumulator, §5.3) |
| FrameBuffer | `FrameBufferPointerInputSource.Mouse.cs:99,105` `(int)(axis * 120)` | truncates |
| Android | *n/a — no wheel support at all* (§4.4) | total |

**Effect on smoothness**: a slow, precise touchpad drag produces a stream of events whose per-event delta
truncates toward zero. The user sees "nothing happens until I scroll faster", then a jump. This is the
classic stair-step / dead-zone artefact.

### 0.2 `InteractionTracker` wheel path does **integer division by 120** → any |delta| < 120 scrolls zero

`src/Uno.UI/UI/Xaml/Internal/InputManager.Pointers.Managed.cs:349`

```csharp
tracker.ReceivePointerWheel(
    args.CurrentPoint.Properties.MouseWheelDelta / global::Microsoft.UI.Xaml.Controls.ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta,
    args.CurrentPoint.Properties.IsHorizontalMouseWheel);
```

`MouseWheelDelta` is `int` (`PointerPointProperties.cs:254`) and `ScrollViewerDefaultMouseWheelDelta` is
`internal const int ... = 120` (`ScrollContentPresenter.mux.cs:18`). This is **C# integer division**.
Receiver:

`src/Uno.UI.Composition/Composition/InteractionTracker/InteractionTracker.cs:137-145`

```csharp
internal void ReceivePointerWheel(int mouseWheelTicks, bool isHorizontal)
{
    // On WinUI, this depends on mouse setting "how many lines to scroll each time"
    // The default Windows setting is 3 lines, and each line is 16px.
    // ...
    var delta = mouseWheelTicks * 48;
    _state.ReceivePointerWheel(-delta, isHorizontal);
}
```

So the whole `ScrollView` / `ItemsView` (InteractionTracker-backed, Skia-only — the `#if __SKIA__` guard is
at `InputManager.Pointers.Managed.cs:341`) family:

* quantizes to **48 logical px steps** — never smooth, by construction;
* **completely ignores** any wheel event with `|MouseWheelDelta| < 120`, i.e. **every precision-touchpad
  event on Windows, every trackpad event on macOS, every fractional wheel event in the browser, and every
  X11 smooth-scroll valuator tick that yields < 120 units.**

This is the single most damaging defect found in this audit.

### 0.3 The "precise scroll" fast path is keyed on the *OS*, not on the *device*

`src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.cs:311` and `:335`

```csharp
if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
{
    var vScrollAmount = Math.Abs(delta) < ScrollViewerDefaultMouseWheelDelta
        ? (double)(-delta)
        : GetVerticalScrollWheelDelta(DesiredSize, -delta);
    success = Set(verticalOffset: VerticalOffset + vScrollAmount,
                  options: new(DisableAnimation: true, IsIntermediate: false));
}
else
{
    success = Set(verticalOffset: TargetVerticalOffset + GetVerticalScrollWheelDelta(DesiredSize, -delta),
                  disableAnimation: false);
}
```

The comment at `:309-310` even admits it:

> "On iOS/macOS, all wheel events currently take this immediate path because we do not have a reliable
> signal to distinguish touchpad/precise scrolling from discrete mouse-wheel input."

But the signal **does exist** and is already populated: `PointerPointProperties.IsTouchPad`
(`src/Uno.UI/UI/Input/WinRT/PointerPointProperties.cs:194`) is set by

* Win32: `Win32WindowWrapper.Pointers.cs:209` — `properties.IsTouchPad = pointerType is POINTER_INPUT_TYPE.PT_TOUCHPAD;`
* X11: `X11PointerInputSource.XInput.cs:284` — `IsTouchPad = info?.IsTouchpad ?? false`

…and is consumed **nowhere** in the scroll code (grep over `src/**` finds only the two producers, the
property definition, and a `ToString()` at `PointerPointProperties.cs:284`).

**Effect**: on Windows and Linux a precision touchpad goes down the *animated* branch
(`disableAnimation: false`), which targets `TargetVerticalOffset` and runs an animation per event. Rapid
per-frame touchpad events therefore chase a moving animated target — the documented failure mode described
in the comment at `ScrollContentPresenter.cs:302-308` (animation first step jumps `(target-visual)*0.149`;
target runs away to `ScrollableHeight`).

### 0.4 No input coalescing anywhere; every OS event runs the full routed-event + hit-test pipeline

There is **no** per-frame input coalescing in Uno. `InputManager.Pointers.Managed.OnPointerMoved`
(`:563-596`) does, for **every single** OS move event:

1. `HitTestOrRoot(...)` — full visual-tree hit test (`:576`);
2. allocate a `PointerRoutedEventArgs` (`:584`);
3. `RaiseLeaveEnter(...)` (`:588`);
4. `RaiseUsingCaptures(Move, ...)` — full bubbling route (`:591`);
5. `AfterMoveForManipulations(...)` (`:593`).

`OnPointerWheelChanged` (`:295-385`) does the same, **plus a second hit test after the scroll** to
reconcile hover (`:373` `HitTestOrRoot(args, _isOver, out originalSource, out var staleBranch, reason: "after_wheel")`).
There is a hand-rolled cache for this — but it is gated on `OperatingSystem.IsIOS()` only
(`:312`, `:332`, `:367`), with the comment at `:288-291` stating the post-scroll hit test costs "~17ms"
per event. **That cost is still paid on Windows, Linux, macOS and WASM.** A 17 ms hit test per wheel event
at 100+ events/s is a guaranteed frame-budget blow-out.

`PointerPredictor.GetPredictedPoints` is a NotImplemented stub
(`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Input/PointerPredictor.cs:39-41`), so no prediction anywhere.

---

## 1. Win32 (`Uno.UI.Runtime.Skia.Win32`)

### 1.1 What arrives

`Win32WindowWrapper.cs:211-214` (window creation):

```csharp
var success = PInvoke.RegisterTouchWindow(hwnd, 0);
if (!success) { this.LogError()?.Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: ..."); }
var success2 = PInvoke.EnableMouseInPointer(true);
if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.EnableMouseInPointer)} failed: ..."); }
```

So Uno opts **entirely** into the WM_POINTER stack. The dispatch switch is `Win32WindowWrapper.cs:363-375`:

```csharp
case PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUP or PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL
    or PInvoke.WM_POINTERENTER or PInvoke.WM_POINTERLEAVE or PInvoke.WM_POINTERUPDATE:
    ...
    OnPointer(msg, wParam, _hwnd);
    return new LRESULT(0);
```

**There is no `WM_MOUSEWHEEL`/`WM_MOUSEHWHEEL`/`WM_MOUSEMOVE` handling at all** — grep over
`src/Uno.UI.Runtime.Skia.Win32/**` for `WM_MOUSEWHEEL` returns zero hits. If `EnableMouseInPointer(true)`
fails (it returns FALSE and sets `ERROR_ACCESS_DENIED` when another component in the process already
disabled it, and it is a per-process one-way switch), Uno logs an error and continues with **no mouse input
at all**. No fallback path exists.

* **Frequency**: WM_POINTERUPDATE arrives at the device's report rate (125–1000 Hz for mice, 100–200 Hz for
  precision touchpads, ~120–240 Hz for touch digitizers). Windows *does* batch pointer input into "frames"
  and exposes them via `GetPointerFrameInfo` / `GetPointerInfoHistory`.
* **Uno reads only the single latest sample**: `Win32WindowWrapper.Pointers.cs:100`
  `PInvoke.GetPointerInfo(pointerId, out pointerInfo)`. Neither `GetPointerFrameInfo` nor
  `GetPointerInfoHistory` nor `SkipPointerFrameMessages` appears anywhere in the project (verified by grep).
  → **historical points are silently discarded** whenever the UI thread is behind.

### 1.2 Precision loss — position is truncated to integer *logical* pixels

`Win32WindowWrapper.Pointers.cs:105-115`:

```csharp
position = pointerInfo.ptPixelLocation;
rawPosition = pointerInfo.ptPixelLocationRaw;
var success = PInvoke.ScreenToClient(_hwnd, ref position);
...
var scale = XamlRoot!.RasterizationScale;
position    = new System.Drawing.Point((int)(position.X / scale),    (int)(position.Y / scale));
rawPosition = new System.Drawing.Point((int)(rawPosition.X / scale), (int)(rawPosition.Y / scale));
return pointerId;
```

Three separate losses:

1. `POINTER_INFO.ptHimetricLocationRaw` (the sub-pixel HIMETRIC position) is never read — Uno takes
   `ptPixelLocation`/`ptPixelLocationRaw`, which are already integer device pixels.
2. The division by `RasterizationScale` is then **truncated back to `int`**. At 150 % DPI the logical
   position quantizes to 1 logical px = 1.5 device px; at 200 % to 2 device px. Touch drags and pen drags
   therefore move in visible steps, and manipulation velocity is computed from a staircase.
3. The values are then widened again to `double` at `:132-133` / `:235-236` — the precision is gone.

For comparison, X11 divides by scale as `double` (`X11PointerInputSource.XInput.cs:324`) and macOS keeps
`CGFloat` (`MacOSWindowHost.cs:650`). Win32 is the only Skia desktop backend that truncates.

### 1.3 Timestamp: `GetMessageTime() * 1000` overflows `int`

`Win32WindowWrapper.Pointers.cs:124` and `:227`:

```csharp
timestamp: (ulong)(PInvoke.GetMessageTime() * 1000), // GetMessageTime is in ms
```

Win32 `GetMessageTime()` is declared `LONG GetMessageTime(void)` → CsWin32 emits `int`. `int * 1000` is
`int` arithmetic in C#, so it overflows once the system has been up for
`int.MaxValue / 1000 ≈ 2 147 483 ms ≈ 35.8 minutes`. After that the product wraps (and can be negative),
and `(ulong)` of a negative int produces a ~1.8e19 value. Consumers:

* `GestureRecognizer.Manipulation.cs:458` `var elapsedMicroseconds = timestamp - parentCommit.Timestamp;`
* `:465` `var velocitiesElapsedMicroseconds = velocitiesPoints.to.Timestamp - velocitiesPoints.from.Timestamp;`
* `:613 ComputeVelocities(delta, elapsedMicroseconds)`
* `:749-760` the "suspicious pointer event" patcher (`δTμs > 100_000` guard) — which will now trip
  constantly or never, depending on which side of the wrap you are.

→ **fling/inertia velocity on Win32 touch is garbage after ~36 min of uptime**, and non-monotonic at the
wrap point. Also note `PInvoke.GetMessageTime()` returns the time of the message last retrieved by
`GetMessage`/`PeekMessage` on the calling thread — not the pointer's own `POINTER_INFO.PerformanceCount`
(which is a QPC value and is available in `pointerInfo`, never read).

Resolution is **1 ms** even when correct; `POINTER_INFO.PerformanceCount` would give QPC resolution.

`frameId` is `Interlocked.Increment(ref _currentPointerFrameId)` (`:123`, `:226`) — a monotonic counter, not
a real input frame id, so nothing downstream can tell which events belong to the same OS frame.

### 1.4 Wheel: 120-detent only, no precision-touchpad pixel deltas

`Win32WindowWrapper.Pointers.cs:146-152`:

```csharp
if (msg is PInvoke.WM_POINTERWHEEL or PInvoke.WM_POINTERHWHEEL)
{
    properties = new()
    {
        MouseWheelDelta = Win32Helper.GET_WHEEL_DELTA_WPARAM(wParam),
        IsHorizontalMouseWheel = msg is PInvoke.WM_POINTERHWHEEL
    };
}
```

with `Win32Helper.cs:162`:

```csharp
public static short GET_WHEEL_DELTA_WPARAM(WPARAM wParam) => unchecked((short)((wParam >> 16) & 0xffff));
```

Windows Precision Touchpads *do* emit sub-detent multiples of `WHEEL_DELTA` (e.g. ±8, ±16, ±24 …), and
`short` preserves them. So the raw delta is fine — but:

* the SCP path (`ScrollContentPresenter.cs:344-349`, non-iOS/macOS) feeds it to
  `GetVerticalScrollWheelDelta` with `disableAnimation: false` → animated, laggy (§0.3);
* the InteractionTracker path integer-divides by 120 → **zero** (§0.2).

Note the wheel branch **drops every other property**: no `IsPrimary`, no `IsInRange`, no
`IsTouchPad`, no button state. `IsTouchPad` is only assigned in the *non-wheel* branch at `:209`, so a
`WM_POINTERWHEEL` from a `PT_TOUCHPAD` never carries `IsTouchPad = true`. Even if §0.3 were fixed to key on
`IsTouchPad`, Win32 would still report `false` for wheel events.

Horizontal-wheel policy (`:214-218`):

```csharp
// TouchPad horizontal scrolling uses WM_POINTERHWHEEL and does not set POINTER_FLAG_HWHEEL
// POINTER_FLAG_HWHEEL is set when mouse-scrolling with Shift held. We choose to handle this as
// a vertical scroll + shift instead ...
properties.IsHorizontalMouseWheel = (modifiers & VirtualKeyModifiers.Shift) == 0 && (wParam & (ulong)POINTER_FLAGS.POINTER_FLAG_HWHEEL) != 0;
```

This is in the non-wheel branch and therefore dead code for `WM_POINTERWHEEL`/`WM_POINTERHWHEEL`
(the `if` at `:146` already returned a different `properties` object for those messages).

### 1.5 Thread model — actually the best of the bunch

* `WndProc` (`Win32WindowWrapper.cs:219-238`) runs on the dispatcher thread; `OnPointer` raises the
  `IUnoCorePointerInputSource` events **synchronously** from the message pump. No cross-thread hop, no
  queueing, no allocation of a dispatcher work item per event. Lowest latency of all backends.
* The message loop deliberately prioritises input: `Win32EventLoop.cs:117-132`

```csharp
/// By default, uses PeekMessage to get input messages if available and does nothing if
/// the message pump is empty. Otherwise, blocks with GetMessage.
public static void RunOnce()
{
    // We need to prioritize input messages in some cases like wheel
    // scrolling where we don't want to wait for the queue to be empty
    // before continuing to scroll.
    if (PInvoke.PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE | PEEK_MESSAGE_REMOVE_TYPE.PM_QS_INPUT)
        || PInvoke.GetMessage(out msg, HWND.Null, 0, 0).Value != -1)
    { PInvoke.TranslateMessage(msg); PInvoke.DispatchMessage(msg); }
    ...
}
```

* Rendering is on a **dedicated render thread** (`Win32WindowWrapper.RenderThread.cs:14-109`) woken by
  `SignalNewFrame()` (`:41-45`), which coalesces bursts via an `AutoResetEvent`. `IXamlRootHost.InvalidateRender()`
  (`Win32WindowWrapper.Rendering.cs:24`) signals it directly rather than posting `WM_PAINT`, with the
  comment (`:19-23`) noting `WM_PAINT` is the lowest-priority message and would be starved by the
  dispatcher's own posted messages.
* Present is paced by `Win32RenderPacer` (`Win32RenderPacer.cs:53-89`), which blocks on `DwmFlush()` and
  degrades permanently to a timer after `DwmFlushFailureThreshold = 3` consecutive failures (`:26`, `:67-77`).

**Latency risk**: because input is dispatched synchronously on the UI thread and the UI thread also records
the `SKPicture`, a burst of 1000 Hz pointer moves each triggering a full hit test (§0.4) will starve frame
recording — the render thread will re-present the same picture at vsync (visual stall) while the UI thread
is buried in routed events.

### 1.6 Win32 defect list

| # | file:line | defect |
|---|---|---|
| W1 | `Win32WindowWrapper.Pointers.cs:113-114` | pointer position truncated to integer logical px after DPI divide → staircase drags, wrong velocities on HiDPI |
| W2 | `Win32WindowWrapper.Pointers.cs:124,227` | `GetMessageTime()*1000` overflows `int` after ~36 min uptime → broken inertia velocity |
| W3 | `Win32WindowWrapper.Pointers.cs:100` | only `GetPointerInfo` — `GetPointerInfoHistory`/`GetPointerFrameInfo` never used; batched samples lost |
| W4 | `Win32WindowWrapper.Pointers.cs:146-152` | wheel `properties` object omits `IsTouchPad`/`IsInRange`/`IsPrimary`; PTP wheel events indistinguishable from mouse wheel |
| W5 | `Win32WindowWrapper.cs:213-214` | no fallback if `EnableMouseInPointer` fails → silent total loss of mouse input |
| W6 | `Win32WindowWrapper.Pointers.cs:123,226` | `frameId` is a synthetic counter, not an OS frame id — downstream cannot coalesce per frame |
| W7 | — | PTP scroll takes the animated `disableAnimation:false` branch (§0.3) |
| W8 | — | PTP sub-120 deltas produce zero motion in `ScrollView`/`ItemsView` (§0.2) |

---

## 2. X11 (`Uno.UI.Runtime.Skia.X11`)

### 2.1 Two input paths

**Legacy core protocol** (`X11PointerInputSource.CoreProtocol.cs`) — used when XI2 ≥ 2.2 is unavailable.
Scroll is button 4/5/6/7 only (`:16-19`, `:62-76`):

```csharp
private const int SCROLL_UP = 4;  private const int SCROLL_DOWN = 5;
private const int SCROLL_LEFT = 6; private const int SCROLL_RIGHT = 7;
...
// Note that this makes scrolling discrete, i.e. there is no Scrolling delta. Instead, we get a separate
// Pressed/Released pair for each scroll wheel "detent".
props.MouseWheelDelta = ev.button is SCROLL_LEFT or SCROLL_UP ?
    ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta : -ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta;
```

i.e. hard-quantized to ±120. Also `CreatePointFromCurrentState` (`:112-141`) hardcodes `pointerId: 0`
(`:131` `0, // TODO: XInput`) and never sets `IsTouchPad`.

**XInput2** (`X11PointerInputSource.XInput.cs`) — the real path. Smooth scrolling *is* implemented via
`XIScrollClassInfo` valuators (`:221-253`):

```csharp
if (data.evtype is XiEventType.XI_Motion && info is { } info_)
{
    var maskLen = data.valuators.MaskLen * BitsPerByte;
    ...
        var oldValueExisted = valuatorsDict.TryGetValue(i, out var oldValue);
        valuatorsDict[i] = value;
        if (oldValueExisted && info_.Scrollers.TryGetValue(i, out var scrollInfo))
        {
            isHorizontalMouseWheel = scrollInfo.ScrollType == XiScrollType.Horizontal;
            wheelDelta = (oldValue - value) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta / scrollInfo.Increment;
        }
}
```

Emulated legacy events are correctly ignored (`:563-571`, checking `XiDeviceEventFlags.XIPointerEmulated`),
so there is no double-counting. Good.

### 2.2 Precision losses

**(a) Wheel delta rounded to int with no residue carry** — `:285`:

```csharp
MouseWheelDelta = (int)Math.Round(wheelDelta),
```

The valuator absolute position *is* stored full-precision (`:237 valuatorsDict[i] = value;`), but the
`oldValue` is unconditionally replaced by the new value, so the rounding residue is **lost, not carried**.
With libinput's typical `Scrolling Pixel Distance = 15` (documented in the source comment at `:685`), the
scale factor is `120/15 = 8` units per pixel, so a 1-px scroll → 8 units → survives rounding. But with a
high-resolution wheel (`libinput High Resolution Wheel Scroll Enabled`, `:687`) whose `Increment` is 120,
a hi-res tick of 1/8 detent → `15` units, and a finer tick → sub-1 → rounds to **0**.

**(b) Zero-delta wheel events are re-routed as moves** — `:585-589`:

```csharp
case XiEventType.XI_Motion when args.CurrentPoint.Properties.MouseWheelDelta != 0:
    X11XamlRootHost.QueueAction(_host, () => RaisePointerWheelChanged(args));
    break;
case XiEventType.XI_Motion:
    X11XamlRootHost.QueueAction(_host, () => RaisePointerMoved(args));
    break;
```

Combined with (a): a scroll event whose rounded delta is 0 is **silently converted into a pointer move**.
The scroll is not merely reduced, it is discarded.

**(c) Position truncated to `int` before `XTranslateCoordinates`** — `:308`:

```csharp
_ = XLib.XTranslateCoordinates(display, data.EventWindow, _host!.TopX11Window.Window,
        (int)data.event_x, (int)data.event_y, out var dataEventX, out var dataEventY, out _);
```

`XIDeviceEvent.event_x/event_y` are `double` (`x11Bindings_XInput.cs:257-258`) and carry sub-pixel
precision from XI2. Uno throws that away by casting to `int` for the translate call, then divides the
integer result by `scale` (`:324-325`). Same class of bug as Win32 W1, on the *device pixel* side.

**(d) Diagonal scroll is collapsed to one axis** — comment at `:217-219`:

> "we can get a 'diagonal scroll' where both the horizontal and the vertical positions change. Our
> PointerEventArgs don't support this, so in that case, we arbitrarily choose the direction to be the
> direction of the last scroller class to be present in data.valuators."

Two-axis trackpad scroll therefore loses one axis per event — visible as "sticky" diagonal scrolling.

**(e) First tick after enter is always dropped** — comment at `:213-216`: "the first 'tick' will not result
in a WheelChanged event, but will only be used to set the initial wheel position." `_valuatorValues` is
cleared on every Enter/Leave (`:402`) and on `XI_DeviceChanged` (`:624`). So each time the pointer re-enters
the window, one scroll event is lost.

### 2.3 Thread model — the worst of the bunch

Two dedicated X event threads per window (`X11XamlRootHost.x11events.cs:36-48`):

```csharp
new Thread(() => Run(RootX11Window)) { Name = $"Uno XEvents {_id} (Root)", IsBackground = true }.Start();
new Thread(() => Run(TopX11Window))  { Name = $"Uno XEvents {_id} (Top)",  IsBackground = true }.Start();
```

Each event is then marshalled to the UI thread (`X11XamlRootHost.x11events.cs:315-316`):

```csharp
public static void QueueAction(IXamlRootHost host, Action action)
    => host.RootElement?.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(action));
```

Consequences:

1. **Two allocations minimum per input event** (closure + `DispatchedHandler` + the `IAsyncAction` returned
   by `RunAsync`). At 1000 Hz that is ~3000 allocations/s of gen0 garbage feeding straight into GC pauses —
   which is exactly what a scroll frame budget cannot absorb.
2. **No coalescing**: every `XI_Motion` becomes its own dispatcher item
   (`X11PointerInputSource.XInput.cs:589`). Nothing collapses a burst.
3. `X11_TouchBegin` / `TouchEnd` each queue **two** actions (`:603-604`, `:607-608`).
4. The dispatcher (`Uno.UWP/Helpers/EventLoop.cs:121-157`) drains the whole ready list into an
   `Action[]` per wake-up (`:144 ready = _readyList.ToArray();`) — another allocation per batch.

**And, worst of all — a synchronous X round trip *per event*, twice:**

`X11XamlRootHost.x11events.cs:149-153`, inside the per-event loop:

```csharp
foreach (var @event in GetEvents(x11Window.Display))
{
    _ = XLib.XQueryTree(x11Window.Display, x11Window.Window, out IntPtr root, out _, out var children, out _);
    _ = XLib.XFree(children);
    if (@event.AnyEvent.window == root) { ... }
```

`XQueryTree` is a *request-with-reply* — a full blocking round trip to the X server — issued **for every
single X event**, including every motion event, purely to obtain the (constant) root window id. Plus
`XTranslateCoordinates` at `X11PointerInputSource.XInput.cs:308` — a second round trip per pointer event.

At 1000 Hz mouse reporting that is 2000 X round trips per second on the event thread, each holding the
X display lock (`X11Helper.XLock`, `x11events.cs:136`, `:237`). Under X-over-network or a busy compositor
this alone caps input throughput and adds tens of ms of latency.

### 2.4 No historical/coalesced points

XI2 offers `XI_RawMotion` and (via the compositor) higher-rate raw samples. Uno subscribes only to
`XI_Motion` device events (`SubWindowXI2Mask` at `x11events.cs:15-17` and the top-level mask). No batching
API used. So on a UI-thread stall, the X server's own motion compression silently drops samples with no
history recovery.

### 2.5 X11 defect list

| # | file:line | defect |
|---|---|---|
| X1 | `X11XamlRootHost.x11events.cs:151` | `XQueryTree` round trip **per X event** — serialises input on server latency |
| X2 | `X11PointerInputSource.XInput.cs:308` | `XTranslateCoordinates` round trip per pointer event, **and** `(int)` truncation of sub-pixel `event_x/event_y` |
| X3 | `X11PointerInputSource.XInput.cs:285` | `(int)Math.Round(wheelDelta)` with no residue carry |
| X4 | `X11PointerInputSource.XInput.cs:585-589` | sub-0.5-unit scroll events degrade into `PointerMoved` — scroll silently dropped |
| X5 | `X11PointerInputSource.XInput.cs:217-219` | diagonal scroll collapsed to a single axis per event |
| X6 | `X11PointerInputSource.XInput.cs:402,624` | valuator reset on every Enter/Leave → first scroll tick after re-entry is always eaten |
| X7 | `X11XamlRootHost.x11events.cs:315-316` | `Dispatcher.RunAsync` per event; 2–3 allocations/event, no coalescing |
| X8 | `X11PointerInputSource.CoreProtocol.cs:131` | legacy path hardcodes `pointerId: 0`; no `IsTouchPad` |
| X9 | `X11XamlRootHost.x11events.cs:325-329` | `XModifierMask.Mod1Mask` maps to `VirtualKeyModifiers.Shift` (copy-paste bug) — Alt+wheel behaves like Shift+wheel → horizontal scroll on Alt+wheel |

(X9 verified at `X11XamlRootHost.x11events.cs:325-329`: `if ((state & XModifierMask.Mod1Mask) != 0) { modifiers |= VirtualKeyModifiers.Shift; }`, and `ControlMask` is tested twice, `:330-337`.)

---

## 3. macOS (`Uno.UI.Runtime.Skia.MacOS` + `UnoNativeMac`)

### 3.1 What arrives

All input funnels through `-[UNOWindow sendEvent:]`
(`UnoNativeMac/UnoNativeMac/UNOWindow.m:1217`). `NSEventTypeScrollWheel` → `MouseEventsScrollWheel`
(`:1261-1263`). There is **no separate `-scrollWheel:` override on the render view** — only `UNOWebView.m:354-360`
overrides `scrollWheel:` to forward to the next responder when web-view scrolling is disabled.

### 3.2 The headline defect — scroll deltas are truncated to `int32_t`

`UNOWindow.h:318-320`:

```objc
    // scrollwheel
    int32_t scrollingDeltaX;
    int32_t scrollingDeltaY;
```

`UNOWindow.m:1409-1420`:

```objc
// scrollwheel
if (mouse == MouseEventsScrollWheel) {
    // do not call if not in the scrollwheel event -> *** Assertion failure in -[NSEvent scrollingDeltaX] ...

    // trackpad / magic mouse sends about 10x more events than a _normal_ (PC) mouse
    // this is often refered as a line scroll versus a pixel scroll
    double factor = event.hasPreciseScrollingDeltas ? 1.0 : 10.0;
    data.scrollingDeltaX = (int32_t)(event.scrollingDeltaX * factor);
    data.scrollingDeltaY = (int32_t)(event.scrollingDeltaY * factor);
```

So:

* `hasPreciseScrollingDeltas` **is** read — but only to pick a multiplier, never propagated to managed code.
  `NativeMouseEventData` (`Native/NativeUno.cs:27-…`) has no `hasPreciseScrollingDeltas` field, so
  `IsTouchPad` is never set on macOS (grep confirms: only Win32 and X11 set it).
* Precise trackpad deltas (`factor = 1.0`) are `CGFloat` in the range ~0.1–20 px per event at 60–120 Hz.
  `(int32_t)` **truncates toward zero**: every event with |delta| < 1.0 px becomes **0**. A slow, deliberate
  trackpad scroll produces a stream of zeros → nothing moves at all until the user speeds up.
  There is no accumulator on the native side and none on the managed side.
* For a discrete mouse (`factor = 10.0`), one notch (`scrollingDeltaY == ±1` line) → `±10`.
  That is fed straight into `MouseWheelDelta` (`MacOSWindowHost.cs:688,692`) where the WinUI convention is
  **120 per notch**. So on macOS one physical wheel notch is 1/12 of a WinUI notch, and
  `GetVerticalScrollWheelDelta` (`ScrollContentPresenter.mux.cs:25`) would give
  `round(10 * max(48, .15*H) / 120)` ≈ 10 px for a 800 px viewport. In practice this is masked because
  macOS takes the `OperatingSystem.IsMacOS()` immediate branch (`ScrollContentPresenter.cs:335-343`) where
  `|delta| < 120` ⇒ raw pixel offset — i.e. **a mouse notch scrolls exactly 10 px**. Far too little; WinUI
  scrolls `max(48, 15% of viewport)`.

### 3.3 Momentum / phase: not consumed, not distinguished

`NSEvent.momentumPhase` and `NSEvent.phase` are **never read** (grep over `src/Uno.UI.Runtime.Skia.MacOS/**`
for `momentumPhase`/`NSEventPhase` returns nothing). macOS *does* deliver OS-generated momentum
`scrollWheel:` events after the fingers lift, and Uno consumes them as ordinary wheel events — so momentum
*is* effectively honoured, but:

* Uno cannot tell a user-driven event from a momentum event, so it cannot stop momentum on a new touch,
  cannot report `IsInertial`, and cannot hand off to `InteractionTracker`'s inertia state.
* Because of §3.2 truncation, the *tail* of the momentum curve (deltas decaying below 1 px) is entirely
  truncated to zero → momentum visibly **stops dead** instead of easing out. This is the most visible
  macOS smoothness artefact.

### 3.4 Two-axis events raise only one event

`MacOSWindowHost.cs:681-694`:

```csharp
if (data.EventType == NativeMouseEvents.ScrollWheel)
{
    var y = data.ScrollingDeltaY;
    if (y == 0)
    {
        // Note: if X and Y are != 0, we should raise 2 events!
        properties.IsHorizontalMouseWheel = true;
        properties.MouseWheelDelta = data.ScrollingDeltaX;
    }
    else
    {
        properties.MouseWheelDelta = y;
    }
}
```

Same class of bug as X5 — the source comment admits it. Diagonal trackpad scroll loses X whenever Y ≠ 0.

### 3.5 Other macOS observations

* `frameId = (uint)(ts * 10.0)` (`UNOWindow.m:1426`) — a **10 Hz** frame id. Every event within the same
  100 ms window shares a frame id. `ToolTipService.cs:274` (`if (e.FrameId == m_LastEnteredFrameId) return;`)
  and `DragOperation.cs:63` (`src.FrameId <= _lastFrameId`) both key off `FrameId`; on macOS this will
  suppress legitimate distinct events.
* `timestamp = (uint64)(ts * 1000000.0)` (`:1427`) — correct µs, full resolution. Best timestamp of the
  desktop backends.
* `sendEvent:` returns early when the app is not active (`:1218-1221`) — scroll over an inactive window
  does nothing (macOS normally supports inactive-window scroll).
* Thread model: `sendEvent:` runs on the AppKit main thread and calls straight into the managed
  `OnMouseEvent` (`MacOSWindowHost.cs:599-646`) which invokes handlers synchronously. No queue hop. Good.
* No coalescing, no `NSEvent` history.

### 3.6 macOS defect list

| # | file:line | defect |
|---|---|---|
| M1 | `UNOWindow.h:319-320` + `UNOWindow.m:1416-1417` | scroll delta truncated to `int32_t`; sub-pixel precise deltas → 0; momentum tail dies abruptly |
| M2 | `UNOWindow.m:1415` | `hasPreciseScrollingDeltas` used only as a multiplier, never surfaced → `IsTouchPad` always false on macOS |
| M3 | `UnoNativeMac/**` | `momentumPhase`/`phase` never read → cannot distinguish user scroll from OS momentum, cannot cancel momentum |
| M4 | `MacOSWindowHost.cs:684-693` | diagonal scroll collapsed; source comment acknowledges "we should raise 2 events!" |
| M5 | `UNOWindow.m:1415-1417` | non-precise notch scaled to ±10, not ±120 → mouse wheel scrolls ~10 px/notch instead of `max(48, 15% viewport)` |
| M6 | `UNOWindow.m:1426` | `frameId` quantized to 10 Hz → collides across genuinely distinct events; breaks `ToolTipService`/`DragOperation` frame-id de-dup |
| M7 | `UNOWindow.m:1218-1221` | no scroll dispatch when the app is not the active app |

---

## 4. Android (`Uno.UI.Runtime.Skia.Android`)

### 4.1 What arrives

`ApplicationActivity.cs:160-185` (`DispatchGenericMotionEvent`) and `:187-212` (`DispatchTouchEvent`) both
forward to `AndroidCorePointerInputSource.Instance.OnNativeMotionEvent(ev, _locationInWindow, nativelyHandled)`
(`:180`, `:207`) and always `return true`.

`AndroidCorePointerInputSource.OnNativeMotionEvent` (`:71-119`) →
`OnNativeMotionEvent(MotionEventActions action, PointerEventArgs args)` (`:121-197`).

### 4.2 **There is no `ACTION_SCROLL` handling. Mouse wheel and trackpad scroll do not exist on Android.**

The action switch (`AndroidCorePointerInputSource.cs:125-196`) covers `HoverEnter`, `HoverExit`, `Down`,
`PointerDown`, `Up`, `PointerUp`, `Move`, `HoverMove`, `Cancel`, and the stylus-with-barrel pseudo-actions.
`MotionEventActions.Scroll` (`ACTION_SCROLL`, value 8) falls into `default:` at `:190-195`:

```csharp
default:
    if (this.Log().IsEnabled(LogLevel.Warning))
    {
        this.Log().Warn($"We receive a native motion event of '{action}', but this is not supported and should have been filtered out in native code.");
    }
    break;
```

Corroborating: `PointerHelpers.GetProperties` (`src/Uno.UWP/Extensions/PointerHelpers.Android.cs:123-185`)
never assigns `MouseWheelDelta`. A repo-wide grep for `Axis.Vscroll` / `Axis.Hscroll` / `AXIS_VSCROLL`
returns **zero** hits outside a trace string (`AndroidCorePointerInputSource.cs:77` logs
`nativeArgs.GetAxisValue(Axis.Wheel)` for diagnostics only).

→ `PointerWheelChanged` is **never raised on Android**. External mouse / trackpad / DeX / ChromeOS scroll
is dead. Touch scrolling still works (via manipulations), but this is a hard functional gap.

### 4.3 Historical points are discarded

`MotionEvent` batches samples: `getHistorySize()`, `getHistoricalX(pointerIndex, pos)`,
`getHistoricalEventTime(pos)`. Android delivers **one** `ACTION_MOVE` per frame containing all intermediate
samples at the digitizer rate (commonly 120–360 Hz on modern devices). Uno reads only the *current* sample:

`AndroidCorePointerInputSource.cs:226-227`

```csharp
var x = nativeArgs.GetX(pointerIndex);
var y = nativeArgs.GetY(pointerIndex);
```

Grep for `GetHistoricalX` / `HistorySize` across `src/**` returns nothing. So:

* fling velocity is computed from ~60 Hz samples instead of the true digitizer rate → **flings are weaker
  and less consistent than native Android**;
* fast drags visibly "cut corners" because the intermediate samples are never seen.

### 4.4 Precision loss on position

`AndroidCorePointerInputSource.cs:229`:

```csharp
var position = new Point((int)x - correction[0], (int)y - correction[1]).PhysicalToLogicalPixels();
```

`MotionEvent.getX()` returns a `float` with sub-pixel precision (Android digitizers report ~1/16 px).
Uno truncates to `int` **physical** pixels before converting to logical. At density 3.0 that is a 1/3
logical-px quantization plus truncation bias — small, but it is a systematic bias toward zero that
directly perturbs velocity integration.

### 4.5 Timestamp

`:210-221`:

```csharp
if ((int)global::Android.OS.Build.VERSION.SdkInt >= 34)
{
    var nativeTimestamp = nativeArgs.EventTimeNanos;
    frameId = (uint)nativeTimestamp;
    ts = (ulong)nativeTimestamp / 1000; // ns to µs
}
else
{
    var nativeTimestamp = nativeArgs.EventTime;
    frameId = (uint)nativeTimestamp;
    ts = (ulong)nativeTimestamp * 1000; // ms to µs
}
```

Correct µs on both branches (ns resolution on API 34+). `frameId = (uint)nanos` truncates to the low 32
bits of a nanosecond counter — wraps every ~4.29 s, and is effectively random; it is not a frame id.

### 4.6 Multi-touch move fan-out

`:91-104`: when `pointerCount > 1 && action == Move`, Uno raises a separate `PointerMoved` for **every**
pointer index, each with its own full hit test + routed event (§0.4). A 5-finger gesture therefore costs
5× the pipeline per OS event.

### 4.7 Thread model

`DispatchTouchEvent`/`DispatchGenericMotionEvent` run on the Android UI thread and raise the events
synchronously. Rendering: `UnoSKCanvasView` is a `GLSurfaceView` (`Rendering/UnoSKCanvasView.cs:25`) with
`RenderMode = Rendermode.WhenDirty` (`:53`); `InvalidateRender()` (`:61-66`) calls `RequestRender()`
("Request the call of IRenderer.OnDrawFrame for one frame"), so the GL render runs on GLSurfaceView's own
thread. The Vulkan path uses an explicit `"UnoVulkanRenderThread"` (`Rendering/UnoSKVulkanView.cs:83-88`).
Reasonable. No Choreographer-based input pacing (no `Choreographer` reference anywhere in the project).

### 4.8 Android defect list

| # | file:line | defect |
|---|---|---|
| A1 | `AndroidCorePointerInputSource.cs:125-196` + `PointerHelpers.Android.cs:123-185` | **no `ACTION_SCROLL` / `AXIS_VSCROLL` handling — `PointerWheelChanged` is never raised on Android** |
| A2 | `AndroidCorePointerInputSource.cs:226-227` | historical samples (`getHistoricalX/Y/EventTime`) never read → weak, inconsistent flings; corner-cutting on fast drags |
| A3 | `AndroidCorePointerInputSource.cs:229` | `(int)x`, `(int)y` truncation in physical px before `PhysicalToLogicalPixels()` |
| A4 | `AndroidCorePointerInputSource.cs:213,219` | `frameId` = low 32 bits of a ns/ms counter; wraps in ~4.3 s (API 34+) |
| A5 | `AndroidCorePointerInputSource.cs:91-104` | multi-touch move fans out to N full routed-event dispatches |
| A6 | — | Android fling is fully reimplemented in `GestureRecognizer`/`InteractionTracker`; the OS `OverScroller`/`VelocityTracker` are not used, so Android-native feel (fling curve, edge stretch) is not matched |

---

## 5. Apple UIKit / iOS (`Uno.UI.Runtime.Skia.AppleUIKit`)

### 5.1 Touch input

`TopViewLayer` (`Devices/Input/TopViewLayer.cs:94-116`) overrides `TouchesBegan/Moved/Ended/Cancelled` and
forwards to `AppleUIKitCorePointerInputSource` (`:96,102,108,114`).

`AppleUIKitPointerInputSource.TouchesMoved` (`:140-162`) iterates the `NSSet` and raises `PointerMoved`
per `UITouch`. Position uses **`touch.GetPreciseLocation(source)`** (`:536`) — the only backend that keeps
full sub-pixel precision on the position. Good.

**Not used**: `UIEvent.GetCoalescedTouches(touch)` and `UIEvent.GetPredictedTouches(touch)`. The `evt`
parameter is accepted at `:140` and never read. Grep for `CoalescedTouches`/`PredictedTouches` across
`src/**` returns nothing.

On a 120 Hz ProMotion device UIKit delivers `touchesMoved:` at the *display* rate but the digitizer runs at
240 Hz; `coalescedTouches` is the only way to see those samples. Without it:

* velocity used for fling handoff is under-sampled;
* whenever a frame is dropped, the intermediate touch samples are lost entirely (UIKit does not re-deliver
  them outside `coalescedTouches`);
* `predictedTouches` (which is exactly how native iOS hides the ~1 frame of touch latency) is unavailable,
  so Uno drags lag native UIScrollView by a visible frame.

### 5.2 Trackpad / mouse scroll: fully reimplemented, with an OS-inertia bypass

`TopViewLayer.SetupScrollGestureRecognizer` (`:28-47`) creates **two** `UIPanGestureRecognizer`s
(`:33-34`), one for `UIScrollTypeMask.Continuous` and one for `Discrete`, with
`MaximumNumberOfTouches = 0`, `AllowedTouchTypes = []`, `ShouldReceiveEvent = (_, evt) => evt.Type == UIEventType.Scroll`
(`:55-65`). This is a scroll-only recognizer, so trackpad/mouse scroll never reaches `touchesMoved:`.

`HandleContinuousScroll` (`:290-353`):

* `Changed` accumulates `gesture.TranslationInView(source)` into `_activeScrollPendingX/Y` and calls
  `gesture.SetTranslation(CGPoint.Empty, source)` (`:319-325`);
* dispatch is deferred to a `CADisplayLink` (`:326-330`) → **one `PointerWheelChanged` per vsync**. This is
  the only backend in Uno that does per-frame input coalescing, and it is the right shape.
* `FlushContinuousScroll` (`:358-388`) truncates to integer and **carries the fraction**:

```csharp
var scrollX = (nfloat)Math.Truncate(_activeScrollPendingX);
var scrollY = (nfloat)Math.Truncate(_activeScrollPendingY);
// Subtract only the integer part so the fractional remainder (e.g. 3.7 → 0.7)
// carries over to the next frame. ...
_activeScrollPendingX -= (double)scrollX;
_activeScrollPendingY -= (double)scrollY;
```

This is the correct pattern that every other backend is missing.

### 5.3 Inertia is reimplemented in managed code, not taken from the OS

`AccumulateInertiaScrolling` (`:408-432`) + `UpdateInertiaScrolling` (`:444-481`) run a hand-rolled
exponential decay on a `CADisplayLink`:

```csharp
private const double InertiaDecelerationRate = 0.95;        // :41
private const double InertiaMagnitudeThreshold = 0.5;       // :46
private const double InertiaMaxVelocity = 50.0;             // :48
private const double RapidFlickWindowSeconds = 0.35;        // :51
private const double InertiaVelocityScale = 60.0;           // :54
```

Also with sub-pixel carry (`:459-469`). Notes:

* `InertiaDecelerationRate = 0.95` **per display frame** — on a 120 Hz ProMotion device that decays twice as
  fast in wall-clock time as on a 60 Hz device. `InertiaVelocityScale = 60.0` (`:54`, comment: "Divide
  velocity (pts/s) by display refresh rate") is likewise hardcoded to 60. **Momentum duration and distance
  are therefore refresh-rate dependent**: a flick on an iPad Pro travels roughly half as far as on a 60 Hz
  iPhone. This is a genuine, visible smoothness/consistency defect.
* macOS/iPadOS *does* deliver OS momentum for trackpad scroll (via `UIScrollTypeMask.Continuous` momentum
  phases). Uno instead ends the gesture and synthesises its own — so the OS momentum and Uno's momentum can
  both be active. **UNVERIFIED** whether UIKit suppresses momentum deltas when the recognizer takes the
  event; not determinable from this repo alone.

### 5.4 Scale constants

`:35` `private const int ScrollWheelDeltaMultiplier = -1;` — the multiplier applied in
`CreateScrollGestureEventArgs:485-488`:

```csharp
var multiplier = isNaturalScrollingEnabled ? -ScrollWheelDeltaMultiplier : ScrollWheelDeltaMultiplier;
var scrollDeltaX = (int)(translation.X * multiplier);
var scrollDeltaY = (int)(translation.Y * multiplier);
```

`(int)` truncation again — but harmless here because `FlushContinuousScroll` already produced an integer.

`:39-40` `DiscreteScrollLineSize = 10.0`, `DiscreteScrollScale = 120.0 / 10.0 = 12` — discrete notches are
normalized to the 120 convention. Correct, and notably *not* what macOS does (§3.2/M5).

Axis selection (`:490-493`):

```csharp
var absX = Math.Abs(scrollDeltaX);
var absY = Math.Abs(scrollDeltaY);
var isHorizontal = absX > absY * 1.5;
var wheelDelta = isHorizontal ? scrollDeltaX : scrollDeltaY;
```

Same one-axis-per-event limitation (the 1.5 hysteresis at least avoids axis flapping).

### 5.5 Render pacing

`UnoSKMetalView` (`Rendering/UnoSKMetalView.cs`) runs a `CADisplayLink` on a dedicated
`NSQualityOfService.UserInteractive` thread (`:91` `_renderThread = new Thread(...)`,
`:117` `_link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default)`), with
`PreferredFrameRateRange { Minimum = 30, Preferred = Maximum = UIScreen.MainScreen.MaximumFramesPerSecond }`
(`:100`, `:103-104`, fed from `:77-78 PreferredFramesPerSecond = UIScreen.MainScreen.MaximumFramesPerSecond`).
MTKView's own link is disabled (`:72 Paused = true`, `:75 EnableSetNeedsDisplay = false`). The link is
unpaused by `QueueRender()` (`:130-132 _link.Paused = false;`) and re-paused at the top of `Draw`
(`:153 _link.Paused = true;`). So a frame is only produced when something calls `QueueRender()` — a scroll
animation that fails to invalidate simply stops.

The three `CADisplayLink`s (render `UnoSKMetalView.cs:37`, active-scroll flush
`AppleUIKitPointerInputSource.cs:328`, momentum `:430`) are independent — the two scroll links are added to
`NSRunLoop.Main` (`:329`, `:431`) while the render link lives on the render thread's runloop
(`UnoSKMetalView.cs:117`), so scroll dispatch and frame production are not phase-locked. That is a latency
jitter source (a scroll flush can land just after the render pass has started, costing a full frame).

### 5.6 iOS defect list

| # | file:line | defect |
|---|---|---|
| I1 | `AppleUIKitPointerInputSource.cs:140-162` | `UIEvent.GetCoalescedTouches` / `GetPredictedTouches` never used → under-sampled velocity, ~1 frame of avoidable touch latency vs native |
| I2 | `AppleUIKitPointerInputSource.cs:41,54` | `InertiaDecelerationRate`/`InertiaVelocityScale` are per-frame constants hardcoded to 60 Hz → momentum distance halves on 120 Hz ProMotion |
| I3 | `AppleUIKitPointerInputSource.cs:490-493` | one axis per event (1.5× hysteresis) |
| I4 | `AppleUIKitPointerInputSource.cs:328-330` vs `UnoSKMetalView.cs:116` | scroll-flush display link and render display link are on different runloops → phase jitter |
| I5 | `InputManager.Pointers.Managed.cs:312-337, 367` | the wheel hit-test cache and post-wheel-hit-test skip are `OperatingSystem.IsIOS()`-only; the same ~17 ms cost is paid on every other platform |

---

## 6. WebAssembly

Two distinct implementations exist and both are live:

* **Skia-on-WASM**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/BrowserPointerInputSource.ts`
  + `Devices/Input/BrowserPointerInputSource.cs`
* **Legacy DOM WASM** (maintenance-only): `src/Uno.UI/ts/Runtime/BrowserPointerInputSource.ts`
  + `src/Uno.UI/Runtime/BrowserPointerInputSource.wasm.cs`

### 6.1 Listener registration and passivity

Skia (`Skia.WebAssembly.Browser/ts/.../BrowserPointerInputSource.ts:69-80`):

```ts
const element = document.body;
element.addEventListener("pointerover",  ..., { capture: true });
...
element.addEventListener("pointermove",  ..., { capture: true });
element.addEventListener("wheel",        ..., { capture: true, passive: false });
```

Legacy DOM (`Uno.UI/ts/Runtime/BrowserPointerInputSource.ts:63-73`):

```ts
const element = document.body;
element.addEventListener("pointerover",  ..., { capture: false });
...
element.addEventListener("wheel",        ..., { capture: false });   // <-- no passive: false
```

Per the DOM spec's "passive by default" intervention, `wheel`/`mousewheel`/`touchstart`/`touchmove`
listeners registered on `window`, `document` or `document.body` default to `passive: true`. The legacy path
therefore registers a **passive** wheel listener on `document.body`, and its later
`evt.preventDefault()` (`:126-128`) is a no-op that also logs a console violation. The Skia path
correctly opts out. *(The code fact is cited; the browser default is spec/UA behaviour, not something this
repo can prove — treat the consequence as **inference**, the `passive: false` asymmetry itself as verified.)*

### 6.2 `deltaMode` handling

Both TS files do the same (`Skia …:158-168`, `legacy …:87-97`):

```ts
switch (evt.deltaMode) {
    case WheelEvent.DOM_DELTA_LINE: // Actually this is supported only by FF
        const lineSize = BrowserPointerInputSource.wheelLineSize;
        wheelDeltaX *= lineSize; wheelDeltaY *= lineSize;
        break;
    case WheelEvent.DOM_DELTA_PAGE:
        wheelDeltaX *= document.documentElement.clientWidth;
        wheelDeltaY *= document.documentElement.clientHeight;
        break;
}
```

`DOM_DELTA_PIXEL` (the default in Chrome/Safari/Edge and for all trackpads) passes through unscaled.
`wheelLineSize` (`Skia …:205-229`) computes the initial `font-size` of a hidden `div` and then:

```ts
this._wheelLineSize = fontSize ? parseInt(fontSize) : 16;
// Based on observations, even if the event reports 3 lines (the settings of windows),
// the browser will actually scroll of about 6 lines of text.
this._wheelLineSize *= 2.0;
```

Note `parseInt` on a computed `font-size` like `"16px"` yields 16 — fine — but a fractional font size
(`15.5px`) truncates. Minor.

**The units never get normalized to WinUI's 120.** A Chrome mouse notch is `deltaY = 100`
(`DOM_DELTA_PIXEL`); Uno passes 100 straight into `MouseWheelDelta`. Then:

* SCP path → `GetVerticalScrollWheelDelta` divides by 120 → the browser notch scrolls **83 %** of the
  intended distance;
* InteractionTracker path → `100 / 120 == 0` → **zero scroll for a mouse notch in `ScrollView`/`ItemsView`
  on WASM** (§0.2). This is a total functional failure, not a smoothness nit.

### 6.3 Truncation

`Skia …/BrowserPointerInputSource.cs:284` and `Uno.UI/Runtime/BrowserPointerInputSource.wasm.cs:317`:

```csharp
MouseWheelDelta = (int)wheel.delta
```

Chrome/Safari trackpad `deltaY` is fractional (often 0.x–4.x per event at 60–120 Hz). `(int)` truncates.
Worse, the *event is still dispatched* — the guard is on the raw `double`, not on the truncated value
(`BrowserPointerInputSource.cs:128-141`):

```csharp
case HtmlPointerEvent.wheel:
    if (wheelDeltaY is not 0) { that.PointerWheelChanged?.Invoke(that, args); }
    if (wheelDeltaX is not 0) { ... that.PointerWheelChanged?.Invoke(that, args); }
```

so Uno raises a full routed `PointerWheelChanged` (hit test, bubble, post-wheel hit test) carrying
`MouseWheelDelta = 0`. Pure cost, zero motion.

Positive: the Skia WASM path is the **only** one that correctly raises **two** events for a diagonal wheel
(`:129-141`) — X and Y each get their own event. (The legacy DOM path also does; see
`BrowserPointerInputSource.wasm.cs` around the same switch.)

### 6.4 Coalescing / prediction / RAF

* `PointerEvent.getCoalescedEvents()` and `getPredictedEvents()` are **never** used (grep). Browsers already
  coalesce `pointermove` to ~1/rAF, so Uno sees the coalesced sample only — the underlying 240 Hz digitizer
  samples on a modern touch device are discarded.
* Rendering is rAF-driven: `Skia.WebAssembly.Browser/ts/Runtime/BrowserRenderer.ts:46-50`

```ts
static invalidate(instance: BrowserRenderer) {
    window.requestAnimationFrame(() => { instance.requestRender(); });
}
```

* Input dispatch is **synchronous from the JS event handler into managed code**
  (`BrowserPointerInputSource.cs:83` applies `NativeDispatcher.Main.SynchronizationContext` then invokes
  handlers inline). So a wheel burst runs the whole routed pipeline inside the JS task, *before* the next
  rAF — good for latency, but a long burst delays rAF and drops the frame.
* `evt.preventDefault()` is called for essentially every pointer/wheel event
  (`Skia …:198-202`) except ctrl+wheel when `BrowserInputHelper.isBrowserZoomEnabled`. Intentional.

### 6.5 WASM defect list

| # | file:line | defect |
|---|---|---|
| B1 | `Skia…/BrowserPointerInputSource.cs:284`, `Uno.UI/Runtime/BrowserPointerInputSource.wasm.cs:317` | `(int)` truncation of fractional `deltaY` → trackpad dead zone |
| B2 | `Skia…/BrowserPointerInputSource.cs:128-141` | wheel events with truncated delta 0 still run the full routed pipeline |
| B3 | `…ts:151-168` | browser pixel deltas (100/notch) never normalized to the 120 convention → 83 % notch distance on SCP, **0 px** on InteractionTracker |
| B4 | `Uno.UI/ts/Runtime/BrowserPointerInputSource.ts:72` | `wheel` listener on `document.body` without `passive: false` → `preventDefault()` is a no-op on the legacy DOM target (inference from the spec's passive-by-default rule) |
| B5 | — | `getCoalescedEvents()` / `getPredictedEvents()` unused |
| B6 | `…ts:221-225` | `wheelLineSize` = `parseInt(font-size) * 2.0` — a heuristic magic number for `DOM_DELTA_LINE` (Firefox) |

---

## 7. Linux FrameBuffer (`Uno.UI.Runtime.Skia.Linux.FrameBuffer`) — minor target, real bug

`Devices/Input/FrameBufferPointerInputSource.Mouse.cs:84-107`:

```csharp
else if (type == LIBINPUT_EVENT_POINTER_AXIS)
{
    double GetAxisValue(libinput_pointer_axis axis)
    {
        var source = libinput_event_pointer_get_axis_source(rawPointerEvent);
        return source == libinput_pointer_axis_source.Wheel
            ? libinput_event_pointer_get_axis_value_discrete(rawPointerEvent, axis)
            : libinput_event_pointer_get_axis_value(rawPointerEvent, axis);
    }

    var wheelMultiplier = _isMouseWheelReversed ? 1 : -1;

    if (libinput_event_pointer_has_axis(rawPointerEvent, libinput_pointer_axis.ScrollHorizontal) != 0)
    {
        properties.IsHorizontalMouseWheel = true;
        properties.MouseWheelDelta = wheelMultiplier * (int)(GetAxisValue(libinput_pointer_axis.ScrollHorizontal) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
        ...
```

* For `axis_source == Wheel`, `..._get_axis_value_discrete` returns **detents** (±1) → ×120 = ±120. Correct.
* For finger/continuous sources, `..._get_axis_value` returns a **distance in libinput scroll units**
  (equivalent to one wheel click ≈ 15 units for most touchpads) → ×120 gives ~1800 per event.
  **~15× over-amplification for touchpad scrolling.**
* `else if` at `:102` → diagonal scroll drops the vertical axis when horizontal is present.
* `IsTouchPad` is never set.
* `LIBINPUT_EVENT_POINTER_AXIS` is the deprecated libinput event; `SCROLL_WHEEL`/`SCROLL_FINGER`/
  `SCROLL_CONTINUOUS` are the modern replacements and give the axis source directly.

---

## 8. Tizen

`src/Uno.UI.Runtime.Skia.Tizen/TizenCorePointerInputSource.cs` exists; not examined in depth (out of the
requested scope, and Tizen is not a scroll-smoothness target). **UNVERIFIED**.

---

## 9. Consolidated cross-platform capability matrix

| Capability | Win32 | X11 | macOS | Android | iOS | WASM(Skia) | WASM(DOM) | FB |
|---|---|---|---|---|---|---|---|---|
| Wheel events raised at all | ✅ | ✅ | ✅ | ❌ **A1** | ✅ | ✅ | ✅ | ✅ |
| Sub-pixel wheel delta preserved | n/a (OS int) | ❌ X3 | ❌ M1 | — | ✅ (carry) | ❌ B1 | ❌ B1 | ❌ |
| Fractional residue carried across events | ❌ | ❌ | ❌ | — | ✅ `:365-371` | ❌ | ❌ | ❌ |
| Sub-pixel *position* preserved | ❌ W1 | ❌ X2 | ✅ | ❌ A3 | ✅ `:536` | ✅ | ✅ | ✅ |
| Precision-touchpad flag surfaced | partial (not on wheel, W4) | ✅ `:284` | ❌ M2 | — | n/a | ❌ | ❌ | ❌ |
| …and actually consumed by scroll code | ❌ (§0.3) | ❌ | ❌ | — | ❌ | ❌ | ❌ | ❌ |
| Historical / coalesced samples used | ❌ W3 | ❌ | ❌ | ❌ A2 | ❌ I1 | ❌ B5 | ❌ B5 | ❌ |
| Predicted samples used | ❌ | ❌ | ❌ | ❌ | ❌ I1 | ❌ | ❌ | ❌ |
| Per-frame input coalescing | ❌ | ❌ | ❌ | ❌ | ✅ `:326-330` | ❌ | ❌ | ❌ |
| Diagonal scroll → 2 events | ❌ | ❌ X5 | ❌ M4 | — | ❌ I3 | ✅ `:129-141` | ✅ | ❌ |
| OS momentum consumed | n/a | n/a | implicit, tail truncated M1/M3 | ❌ A6 | reimplemented I2 | n/a | n/a | n/a |
| Input dispatched on UI thread w/o queue hop | ✅ | ❌ X7 | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| µs-resolution monotonic timestamp | ❌ W2 (ms + overflow) | ms | ✅ µs | ✅ ns/µs | ✅ | ms (`performance.now` ×1000) | ms | ✅ µs |

---

## 10. Ranked fix list (highest smoothness ROI first)

1. **`InputManager.Pointers.Managed.cs:349` — replace integer division.**
   Change `ReceivePointerWheel(int mouseWheelTicks, …)` (`InteractionTracker.cs:137`) to take a `double`
   (or a pixel delta) and compute `delta = mouseWheelTicks * 48.0` in floating point. Today
   `ScrollView`/`ItemsView` cannot be scrolled at all by any precision device on any platform.

2. **Make `MouseWheelDelta` sub-integer-capable, or add a `double MouseWheelDeltaPrecise`.**
   Without this, every platform boundary must keep truncating. If the public `int` must stay for WinUI
   parity, add an internal `double` alongside it in `PointerPointProperties` and thread it through
   `ScrollContentPresenter.PointerWheelScroll` and `InteractionTracker.ReceivePointerWheel`.

3. **`ScrollContentPresenter.cs:311,335` — key the immediate/precise branch on
   `properties.IsTouchPad` (plus a delta-magnitude heuristic), not `OperatingSystem.IsIOS()||IsMacOS()`.**
   Then fix the two producers so the flag is actually set for wheel events:
   Win32 `Win32WindowWrapper.Pointers.cs:146-152` (add `IsTouchPad` to the wheel branch), macOS
   `UNOWindow.m:1415` (plumb `hasPreciseScrollingDeltas` through `MouseEventData`/`NativeMouseEventData`).

4. **macOS `UNOWindow.h:319-320` — change `int32_t scrollingDeltaX/Y` to `double`** and remove the
   truncation at `UNOWindow.m:1416-1417`. This single change fixes the macOS dead zone *and* the abrupt
   momentum stop. Also normalize the non-precise path to 120/notch instead of ×10 (M5).

5. **Adopt the iOS accumulator pattern (`AppleUIKitPointerInputSource.cs:358-388`) on every backend**:
   accumulate fractional deltas, dispatch integer part, carry the remainder. Cheap, localized, and removes
   the dead zone everywhere.

6. **Add per-frame input coalescing in `InputManager`** (or in each source). Today every OS sample runs a
   full hit test + routed dispatch (`InputManager.Pointers.Managed.cs:563-596`, `:295-385`). Coalescing
   moves to one dispatch per frame and keeps the discarded samples in a history buffer for velocity.

7. **Remove the per-event X round trips on X11**: hoist `XQueryTree`
   (`X11XamlRootHost.x11events.cs:151`) out of the event loop (the root window is constant per display),
   and cache the window offset instead of calling `XTranslateCoordinates` per pointer event
   (`X11PointerInputSource.XInput.cs:308`). Also stop truncating `event_x/event_y` to `int`.

8. **Win32 `Win32WindowWrapper.Pointers.cs:113-114` — keep `double` positions**
   (divide by scale without the `(int)` cast) and use `POINTER_INFO.ptHimetricLocationRaw` /
   `PerformanceCount` for sub-pixel position and QPC timestamps. Fix the `GetMessageTime()*1000` overflow
   (`:124,227`) — cast to `ulong` *before* multiplying, or use `pointerInfo.dwTime`/`PerformanceCount`.

9. **Android: implement `ACTION_SCROLL`** (`AndroidCorePointerInputSource.cs:125`) reading
   `Axis.Vscroll`/`Axis.Hscroll` and mapping to `MouseWheelDelta`, and **consume historical samples**
   (`getHistorySize`/`getHistoricalX/Y/EventTime`) for velocity.

10. **iOS: use `UIEvent.GetCoalescedTouches`/`GetPredictedTouches`** (`AppleUIKitPointerInputSource.cs:140`)
    and make `InertiaDecelerationRate`/`InertiaVelocityScale` (`:41,54`) time-based rather than
    per-frame constants pinned to 60 Hz.

11. **WASM: normalize wheel units** (browser 100 px/notch → 120) and stop dispatching wheel events whose
    truncated delta is 0 (`BrowserPointerInputSource.cs:128-141`). Add `passive: false` to the legacy DOM
    wheel listener (`Uno.UI/ts/Runtime/BrowserPointerInputSource.ts:72`).

12. **FrameBuffer**: divide `libinput_event_pointer_get_axis_value` by the ~15-unit click distance before
    scaling by 120 (`FrameBufferPointerInputSource.Mouse.cs:99,105`), and change the `else if` at `:102`
    to raise both axes.

---

## 11. Things explicitly checked and NOT found (so nobody re-searches)

* No `GetPointerFrameInfo`, `GetPointerInfoHistory`, `SkipPointerFrameMessages` anywhere.
* No `WM_MOUSEWHEEL` / `WM_MOUSEHWHEEL` / `WM_INPUT` handling.
* No `Axis.Vscroll` / `Axis.Hscroll` / `MotionEventActions.Scroll` handling.
* No `getHistoricalX` / `getHistorySize` (Android).
* No `GetCoalescedTouches` / `GetPredictedTouches` (iOS) — `PointerPredictor.GetPredictedPoints` is a
  `NotImplemented` stub (`src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Input/PointerPredictor.cs:39-41`).
* No `getCoalescedEvents()` / `getPredictedEvents()` (WASM).
* No `momentumPhase` / `NSEventPhase` (macOS).
* No `Choreographer` usage (Android).
* `PointerPointProperties.IsTouchPad` has **zero** consumers outside `ToString()`.
