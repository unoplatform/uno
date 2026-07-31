# Avalonia as prior art: what clock drives its frames, and does it have our defect?

Research note for the Uno scroll-smoothness / frame-clock effort.

**Question asked:** can we get the *real* frame time (vsync time, or predicted presentation time of the
frame being built) instead of reconstructing it with a phase-locked estimator? What do mature stacks
actually feed their scroll/animation curves? This note covers **Avalonia**.

**Source read:** `D:/Work/Avalonia`, commit `e81f3f7ff7802e8dd4dcd52137358bb08952ecc0`
(2026-04-23, "Changes in CommandBar icon foreground inheritance (#21251)"),
`build/SharedVersion.props` → `<Version>12.1.999</Version>` (12.x dev head).
All Avalonia paths below are relative to that root. All line numbers are from that commit.

Uno paths are relative to `D:/Work/uno-worktrees/scrollsmooth`.

Everything below was read from source. Claims I could not verify in code are marked **UNVERIFIED**.

---

## 0. Verdict, up front

**Avalonia has our defect, and in one place it has a strictly worse version of it.**

Avalonia's *platform layer* successfully obtains a genuine (a) vsync/frame-start time on four of its
backends — Android, Browser, macOS, iOS. It then **throws every one of them away** and substitutes an
unrelated `Stopwatch` read taken at whatever moment the render/UI thread got around to processing the
frame. Nothing downstream of `IRenderTimer` ever sees a platform timestamp:

- `DefaultRenderLoop.TimerTick(TimeSpan time)` accepts the timer's time and **never uses the
  parameter** — `IRenderLoopTask.Render()` takes no arguments
  (`src/Avalonia.Base/Rendering/RenderLoop.cs:121-146`, `src/Avalonia.Base/Rendering/IRenderLoopTask.cs:5`).
- Server-side composition animations evaluate against `ServerCompositor.ServerNow`, which is
  `Stopwatch.Elapsed` off a private `Stopwatch` sampled at the top of `RenderCore`
  (`.../Server/ServerCompositor.cs:29`, `:73`, `:253`).
- UI-thread animations, transitions and `RequestAnimationFrame` evaluate against
  `MediaContext._time.Elapsed` — a *second, unrelated* `Stopwatch`, sampled at the top of a
  `DispatcherPriority.Render` dispatcher operation
  (`src/Avalonia.Base/Media/MediaContext.Clock.cs:13`, `src/Avalonia.Base/Media/MediaContext.cs:134`).
- Avalonia's own touch scroll inertia runs on a **third** `Stopwatch`, read inside a
  `Dispatcher.UIThread.InvokeAsync(..., DispatcherPriority.Input)` continuation posted *from* the
  animation-frame callback — so its clock read is displaced from the frame boundary by an entire
  dispatcher queue drain (`src/Avalonia.Base/Input/GestureRecognizers/ScrollGestureRecognizer.cs:216-232`).

No Avalonia backend uses a **predicted presentation time** (b) anywhere. No Avalonia backend uses a
**measured presentation time** (c) anywhere. `DwmGetCompositionTimingInfo` is never P/Invoked;
`IDCompositionDevice::GetFrameStatistics` and `IDXGISwapChain::GetFrameStatistics` exist only as
never-called entries in generated MicroCom vtables.

**Usefulness to us:** Avalonia is *not* a model to copy for the clock. It is a strong negative data
point (the .NET UI stacks generally do not solve this) plus a **free proof of API availability**: its
platform layer already demonstrates that the real vsync timestamp is reachable on Android, Browser,
macOS and iOS with no exotic plumbing. The only thing Avalonia is missing is a `TimeSpan` parameter
threaded through three call sites.

---

## 1. `IRenderTimer` / `Tick(TimeSpan)` — what time is handed in, and where does it come from?

```csharp
// src/Avalonia.Base/Rendering/IRenderTimer.cs:22
Action<TimeSpan>? Tick { get; set; }
```

The interface is deliberately time-carrying. Here is what every implementation actually passes,
read from source:

| Backend | File:line | What it waits on | What it passes to `Tick` | Real vsync time available at that point? |
|---|---|---|---|---|
| **Android** | `src/Android/Avalonia.Android/ChoreographerTimer.cs:106-113`, `:86` | `AChoreographer_postFrameCallback64` | `TimeSpan.FromTicks(frameTimeNanos / 100)` — **the genuine Choreographer vsync time** | **Yes, and it is used** (up to the render loop) |
| **Browser** | `src/Browser/Avalonia.Browser/Rendering/BrowserRenderTimer.cs:43-48` | `requestAnimationFrame` | `TimeSpan.FromMilliseconds(timestamp)` — **the genuine rAF `DOMHighResTimeStamp`** | **Yes, and it is used** (up to the render loop) |
| **macOS** | `native/Avalonia.Native/src/OSX/PlatformRenderTimer.mm:73-79` → `src/Avalonia.Native/AvaloniaNativeRenderTimer.cs:56-59` | `CVDisplayLink` | `_stopwatch.Elapsed` — **`inNow` and `inOutputTime` are received and discarded at the ObjC boundary** | Yes — *thrown away in native code* |
| **macOS (again)** | `src/Avalonia.Base/Rendering/ThreadProxyRenderTimer.cs:82` | (wraps the above; bound at `src/Avalonia.Native/AvaloniaNativePlatform.cs:125`) | `_stopwatch.Elapsed` — **a second discard**, of a value that was already fake | — |
| **iOS** | `src/iOS/Avalonia.iOS/DisplayLinkTimer.cs:40-43` | `CADisplayLink` | `_st.Elapsed` — **`link.Timestamp` / `link.TargetTimestamp` are never read** | Yes — *never queried* |
| **Win32 / WinUI composition** | `src/Windows/Avalonia.Win32/WinRT/Composition/WinUiCompositorConnection.cs:106`, `:126` | DWM commit completion (`IAsyncAction`) | `_st.Elapsed` | No — nothing queried |
| **Win32 / DirectComposition** | `src/Windows/Avalonia.Win32/DComposition/DirectCompositionConnection.cs:98`, `:109` | `IDCompositionDevice::WaitForCommitCompletion` | `_stopwatch.Elapsed` | No |
| **Win32 / DXGI** | `src/Windows/Avalonia.Win32/DirectX/DxgiConnection.cs:80`, `:105`, `:120`, `:122` | `IDXGIOutput::WaitForVBlank` (falls back to `DwmFlush`) | `_stopwatch.Elapsed` | No |
| **Win32 fallback** | `src/Windows/Avalonia.Win32/Win32Platform.cs:90` → `src/Avalonia.Base/Rendering/DefaultRenderTimer.cs:72` | `System.Threading.Timer` at 1/60s | `TimeSpan.FromMilliseconds(Environment.TickCount)` — **~15.6 ms granularity** | No; not even vsync-paced |
| **X11** | `src/Avalonia.X11/X11Platform.cs:76-78` → `src/Avalonia.Base/Rendering/SleepLoopRenderTimer.cs:64-66` | `AutoResetEvent.WaitOne(timeout)` at 60 fps | `_st.Elapsed` | **No vsync source at all on X11** |
| **UI-thread mode (all)** | `src/Avalonia.Base/Rendering/UiThreadRenderTimer.cs:44`, `:48` | `DispatcherTimer(Render)` | `_clock.Elapsed` | No |

Reading of the table:

1. **Two backends already have the real thing** (Android `frameTimeNanos`, Browser rAF timestamp) and
   pass it correctly through `IRenderTimer`.
2. **Two more could get it for free** but do not: macOS discards `CVTimeStamp inNow`/`inOutputTime`
   *in the native callback signature itself* (`PlatformRenderTimer.mm:73` names both parameters and
   the body calls `_callback->Run()` with no arguments), and iOS never touches
   `CADisplayLink.Timestamp` / `.TargetTimestamp` despite holding the link object.
3. **Windows gets nothing**, on any of its four paths. Every Win32 timer blocks on a vsync-ish
   primitive and then reads `Stopwatch` *after the wait returns* — i.e. records "when this thread woke
   up", not "when the vblank was". The wake latency is exactly the jitter we are trying to remove.

**Category note:** Android `frameTimeNanos` and Browser rAF timestamp are both category **(a)
vsync/frame-start time**. Nothing in Avalonia produces category (b) predicted presentation time.
`CADisplayLink.TargetTimestamp` *is* a category-(b) value and is the one thing sitting unused on the
floor in this repo.

---

## 2. The load-bearing discovery: `RenderLoop` discards the tick time

Even where the real time arrives, it dies immediately:

```csharp
// src/Avalonia.Base/Rendering/RenderLoop.cs:121
private void TimerTick(TimeSpan time)
{
    ...
    for (int i = 0; i < _itemsCopy.Count; i++)
    {
        wantsNextTick |= _itemsCopy[i].Render();   // :146 — `time` is never passed on
    }
    ...
}
```

`time` is bound and then unused for the whole method body. The consumer contract has no slot for it:

```csharp
// src/Avalonia.Base/Rendering/IRenderLoopTask.cs:3-6
internal interface IRenderLoopTask
{
    bool Render();
}
```

The sole production implementer is `ServerCompositor` (`.../Server/ServerCompositor.cs:21`, `:185`).
So on **every** Avalonia backend, the value that reaches the animation system is generated *inside*
the compositor, from a clock the platform knows nothing about. Android's `frameTimeNanos` travels
from a native Choreographer callback, across two threads, into `DefaultRenderLoop.TimerTick`, and is
dropped on the floor one stack frame from the compositor that needed it.

---

## 3. The server (composition) clock

```csharp
// src/Avalonia.Base/Rendering/Composition/Server/ServerCompositor.cs
:29   public Stopwatch Clock { get; } = Stopwatch.StartNew();
:30   public TimeSpan ServerNow { get; private set; }
:73   internal void UpdateServerTime() => ServerNow = Clock.Elapsed;
:253  UpdateServerTime();            // first statement of RenderCore
```

Consumers:

- `.../Server/ServerObjectAnimations.cs:61` —
  `_cachedVariant = Animation.Evaluate(Owner._owner.Compositor.ServerNow, _cachedVariant);`
- `.../Composition/CompositionCustomVisualHandler.cs:72` — `CompositionNow => _host!.Compositor.ServerNow`
- key-frame animations subtract a start time: `.../Animations/KeyFrameAnimationInstance.cs:69`
  `var elapsed = now - _startedAt;`

The start time is *also* a `Stopwatch.Elapsed` read, but off the same `ServerCompositor.Clock`,
sampled on the **UI thread** at commit:

```csharp
// src/Avalonia.Base/Rendering/Composition/Compositor.cs:179
_nextCommit.CommittedAt = Server.Clock.Elapsed;
// → src/Avalonia.Base/Rendering/Composition/Server/SimpleServerObject.cs:22
//   DeserializeChangesCore(reader, batch.CommittedAt);
// → AnimationInstanceBase.Initialize(startedAt: ...)
```

So composition animations are at least internally consistent (one clock, two sampling points), but the
sampling point on the evaluation side is *"whenever the render thread entered `RenderCore`"* — which
is downstream of the vsync wait plus wake latency plus batch deserialization scheduling. This is
structurally identical to Uno's pre-estimator behaviour.

---

## 4. The UI-thread clock — a *second* independent `Stopwatch`

WPF-style animations, `Transitions`, and `RequestAnimationFrame` do **not** use `ServerCompositor.Clock`.
They use `MediaContext`:

```csharp
// src/Avalonia.Base/Media/MediaContext.Clock.cs:13
private readonly Stopwatch _time = Stopwatch.StartNew();

// src/Avalonia.Base/Media/MediaContext.cs:132-136
private void RenderCore()
{
    var now = _time.Elapsed;
    if (!_animationsAreWaitingForComposition)
        _clock.Pulse(now);
```

`Pulse(now)` fans the value out to queued animation-frame callbacks and to every `IClock` observer
(`MediaContext.Clock.cs:50-63`); `Clock.GlobalClock` resolves to this object
(`src/Avalonia.Base/Animation/Clock.cs:11`, `src/Avalonia.Base/Media/MediaContext.Clock.cs:12`).

`RenderCore` runs inside a `DispatcherOperation` at `DispatcherPriority.Render`
(`MediaContext.cs:102-104`). Pacing is *backpressure*, not vsync: the next render pass is only
scheduled once the previous composition batch reports `Processed`
(`src/Avalonia.Base/Media/MediaContext.Compositor.cs:41-55`, `:64-82`). When nothing is in flight the
fallback is a fixed 16 ms `DispatcherTimer`, with a comment that concedes the point:

```csharp
// src/Avalonia.Base/Media/MediaContext.cs:30-36
private readonly DispatcherTimer _animationsTimer = new(DispatcherPriority.Render)
{
    // Since this timer is used to drive animations that didn't contribute to the previous frame at all
    // We can safely use 16ms interval until we fix our animation system to actually report the next expected
    // frame
    Interval = TimeSpan.FromMilliseconds(16)
};
```

This is precisely the Uno situation: the UI-thread record pass rides *approximately* on the vsync
cadence via backpressure, but the timestamp handed to the curves is a clock read taken at the top of
a dispatcher operation, so it carries all of the dispatcher's scheduling wobble.

---

## 5. Avalonia's own scroll inertia — the worst offender

`ScrollGestureRecognizer` is Avalonia's touch fling. Relevant lines
(`src/Avalonia.Base/Input/GestureRecognizers/ScrollGestureRecognizer.cs`):

```csharp
:25    private Stopwatch? _stopWatch;              // a THIRD independent clock
...
:206       _stopWatch = Stopwatch.StartNew();
:207       _lastTime = _stopWatch.Elapsed;
:208       _inertiaStartTime = _lastTime;
:211       MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
...
:216   private void OnAnimationRequested(TimeSpan _)      // <-- frame time DISCARDED, named `_`
:217   {
:220       Dispatcher.UIThread.InvokeAsync(() =>          // <-- and then a dispatcher hop
:221       {
...
:228           var timeSpan = _stopWatch.Elapsed;         // <-- clock read here instead
:229           var elapsedSinceLastTick = timeSpan - _lastTime;
:230           _lastTime = timeSpan;
:232           var speed = inertia * Math.Pow(InertialResistance, (_lastTime - _inertiaStartTime).TotalSeconds);
:233           var distance = speed * elapsedSinceLastTick.TotalSeconds;
...
:260           MediaContext.Instance.RequestAnimationFrame(OnAnimationRequested);
:263       }, DispatcherPriority.Input);
```

Three separate defects stack here:

1. **The frame time is explicitly discarded.** The callback signature is `Action<TimeSpan>` and
   `MediaContext.Clock.cs:59` calls `callback(now)` with the frame's `now`. `ScrollGestureRecognizer`
   names that parameter `_`.
2. **The clock is then re-read from a private `Stopwatch`** that shares no sampling point with either
   the compositor clock or the media clock.
3. **The re-read happens after a dispatcher hop** at `DispatcherPriority.Input`. So the position
   evaluated for frame *N* is computed at whatever moment the Input-priority queue drained — which by
   construction is *after* pending input has been processed, and can be an arbitrary distance past the
   frame boundary. Under load this is worse than Uno's current raw-`TimestampInTicks`-at-record read,
   because Uno at least samples at a fixed point in the pipeline.

The curve shape makes this maximally sensitive. `speed = v0 * 0.15^t` is analytic in absolute `t`
(so absolute-time jitter shows up directly), and `distance = speed * dt` integrates a *measured* `dt`
(so inter-frame jitter shows up again, uncorrelated). Avalonia's inertia is jitter-sensitive on both
the absolute-time axis and the delta axis; Uno's analytic `x(t)` is sensitive on only one.

`ScrollGestureEndedEventArgs`/`InertialResistance = 0.15` at `:13-14`. Note also the iOS backend
bypasses this entirely and synthesizes `RawMouseWheelEventArgs` from a `CADisplayLink`, again ignoring
`link.Timestamp` and stamping the event with `Environment.TickCount64`
(`src/iOS/Avalonia.iOS/InputHandler.cs:332-334`, `:355-381`).

---

## 6. Clock census

Avalonia runs **four mutually unsynchronised monotonic clocks** in a single scrolling frame:

| # | Clock | Sampled where | Drives |
|---|---|---|---|
| 1 | `ServerCompositor.Clock` (`ServerCompositor.cs:29`) | render thread, top of `RenderCore` (`:253`) | composition animations, `CompositionCustomVisual` |
| 2 | `MediaContext._time` (`MediaContext.Clock.cs:13`) | UI thread, top of `RenderCore` (`MediaContext.cs:134`) | `IClock`/`Animation`/`Transitions`, `RequestAnimationFrame` |
| 3 | per-timer `Stopwatch` (`ThreadProxyRenderTimer.cs:13`, `AvaloniaNativeRenderTimer.cs:11`, `DisplayLinkTimer.cs:14`, `SleepLoopRenderTimer.cs:15`, `DxgiConnection`, `WinUiCompositorConnection.RunLoopHandler._st`) | timer thread, after the vsync wait returns | **nothing** — discarded by `RenderLoop.cs:146` |
| 4 | `ScrollGestureRecognizer._stopWatch` (`:25`) | UI thread, inside an Input-priority continuation (`:228`) | touch fling position |

They are all `Stopwatch`, so they share a *base*; they differ only in start offset and — crucially —
in **sampling point**, which is where all the jitter lives.

---

## 7. The epoch question, answered concretely

`Stopwatch.GetTimestamp()` bottoms out in `minipal_hires_ticks()`. Verified from dotnet/runtime `main`:

```c
// src/native/minipal/time.c  — Windows branch
int64_t minipal_hires_ticks() { LARGE_INTEGER ts; QueryPerformanceCounter(&ts); return ts.QuadPart; }
int64_t minipal_hires_tick_frequency() { LARGE_INTEGER ts; QueryPerformanceFrequency(&ts); return ts.QuadPart; }

// src/native/minipal/time.c  — non-Windows branch
int64_t minipal_hires_tick_frequency(void) { return tccSecondsToNanoSeconds; }   // 1e9
int64_t minipal_hires_ticks(void)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#else
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToNanoSeconds)) + (int64_t)(ts.tv_nsec);
#endif
}
```

Reached via `SystemNative_GetTimestamp` (`src/native/libs/System.Native/pal_time.c`) ←
`Interop.Sys.GetTimestamp()` ← `Stopwatch.Unix.cs` (`GetFrequency()` returns `1_000_000_000`).

| Platform | `Stopwatch.GetTimestamp()` is | Platform frame-time clock | Match? |
|---|---|---|---|
| **Windows** | `QueryPerformanceCounter`, units = `QueryPerformanceFrequency` | `DWM_TIMING_INFO.qpcVBlank` / `qpcCompose` / `qpcRefreshPeriod` are documented as **QPC** units | **Exact — same counter, same epoch, same units.** No offset, no scale. |
| **Android / Linux** | `clock_gettime(CLOCK_MONOTONIC)`, ns | `AChoreographer` `frameTimeNanos` is documented as the `System.nanoTime()` timebase; Android's `System.nanoTime` is `CLOCK_MONOTONIC` ns | **Exact — same clock, same epoch, same units (ns).** Convert to Uno 100ns ticks by `/ 100`. |
| **macOS / iOS** | `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`, ns | `CVTimeStamp.hostTime` / `CADisplayLink.Timestamp` are on the mach absolute-time base (`CACurrentMediaTime()`, seconds) | Same monotonic base; **unit conversion only** (seconds ↔ ns), no epoch offset. Man-page-level, **UNVERIFIED at code level** that `CLOCK_UPTIME_RAW` ≡ `mach_absolute_time` on all Apple platforms. |
| **Browser (WASM)** | `clock_gettime(CLOCK_MONOTONIC)` under the WASI/emscripten shim, ns | rAF `DOMHighResTimeStamp` is ms relative to `performance.timeOrigin` | **Needs a constant offset.** Establish once by sampling both at startup. **UNVERIFIED** what emscripten's `CLOCK_MONOTONIC` is anchored to in the .NET WASM runtime. |

**Bottom line on epoch:** on the two targets that matter most for this work (Win32 and Android), the
platform vsync timestamp is *already on exactly the same clock as `Stopwatch.GetTimestamp()`* — the
same QPC counter and the same `CLOCK_MONOTONIC` nanoseconds respectively. Uno can substitute a real
frame time for `Compositor.TimestampInTicks` with a pure unit scale and **no epoch reconciliation at
all**. Avalonia never needed to solve an epoch problem because it never got as far as having one.

---

## 8. What Uno already receives and discards

Two places where Uno is in exactly Avalonia's position — holding the real value and dropping it:

```csharp
// src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:99
private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
{
    public void DoFrame(long frameTimeNanos) => onFrame();   // frameTimeNanos discarded
}
```

`frameTimeNanos` here is a category-(a) vsync time on `CLOCK_MONOTONIC` ns — the identical value
Avalonia's `ChoreographerTimer.cs:106-113` captures. It is already in the process, already on the
right clock, and is thrown away one line from where it could be published.

```csharp
// src/Uno.UI.Composition/Composition/Compositor.cs:38
public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));
// consumed at src/Uno.UI.Composition/Composition/Compositor.skia.cs:312
var frameTimestamp = GetFrameTimestamp(TimestampInTicks);
```

On Win32, `src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:61` calls `PInvoke.DwmFlush()`
and returns `void` — the same "wait, then read `Stopwatch`" shape as every Avalonia Win32 timer.
Unlike Choreographer, `DwmFlush` genuinely carries no timestamp; the QPC vblank time has to be pulled
separately from `DwmGetCompositionTimingInfo` (`DWM_TIMING_INFO.qpcVBlank`, plus `qpcRefreshPeriod`
for the period and `cRefresh`/`cDXRefresh` for drop detection). That call is not currently made
anywhere in the Uno tree.

---

## 9. What to take from Avalonia, and what not to

**Take:**

- **The existence proof.** `ChoreographerTimer.cs:106-113` and `BrowserRenderTimer.cs:43-48` are
  working, shipping code that carries a real category-(a) frame time from the OS into managed code on
  Android and WASM. There is no research risk on those two targets.
- **The `Action<TimeSpan>`-carrying timer shape** (`IRenderTimer.cs:22`) is the right interface
  design — Avalonia's mistake is purely that the *consumer* contract (`IRenderLoopTask.Render()`)
  dropped the parameter. Uno's `Compositor.FrameStarting` already carries `long`, so Uno is ahead here.
  (Aside, out of scope for this note: `Compositor.skia.cs:209` declares `event Action<long>?`, which
  the repo's own `AGENTS.md` rule forbids in favour of `EventHandler<T>`.)

**Do not take:**

- The multi-`Stopwatch` design. Four unsynchronised clocks in one frame is a bug farm; Uno should keep
  exactly one frame timestamp per frame (`CurrentFrameTimestampInTicks`, `Compositor.skia.cs:214`) and
  make every driver read it.
- `ScrollGestureRecognizer`'s dispatcher hop (`:220`, `:263`). Evaluating a motion curve inside an
  `InvokeAsync(..., Input)` continuation posted from the frame callback guarantees the evaluation
  instant is decorrelated from the frame instant. Uno's pre-record `FrameStarting` hook
  (`Compositor.skia.cs:308-317`) is the correct shape and must stay synchronous.
- `distance = speed * measuredDt` integration (`ScrollGestureRecognizer.cs:233`). Analytic `x(t)` is
  strictly better under jitter: it has one jitter input instead of two, and it self-corrects, whereas
  a `dt`-integrator accumulates every error permanently.

**Net recommendation for the clock work:** keep the phase-locked estimator as the *fallback* — it is
the only thing that works on X11 and on the degraded `Win32RenderPacer` timer path, and it is more
than Avalonia has anywhere. But do not treat it as the endpoint. On Android the real value is one
parameter away (`ChoreographerFramePacer.cs:99`), and on Win32 it is one `DwmGetCompositionTimingInfo`
call away, both on an epoch that already matches `Stopwatch.GetTimestamp()` bit-for-bit.

---

## 10. UNVERIFIED / not established here

- That `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)` is bit-identical to `mach_absolute_time()` converted
  to ns on all current Apple platforms. Man-page-level claim; not read from Darwin sources.
- What emscripten's `CLOCK_MONOTONIC` is anchored to under the .NET WASM runtime, and therefore the
  exact rAF↔`Stopwatch` offset. Not read from emscripten/runtime source.
- Android docs' statement that `AChoreographer` `frameTimeNanos` is in the `System.nanoTime()`
  timebase — taken from documentation, not from AOSP source. The *use* of it as a vsync time is
  verified in Avalonia code (`ChoreographerTimer.cs:112`, `:86`); the timebase claim is doc-level.
- Whether any Avalonia backend behaves differently at runtime from what the source implies. This note
  is a **code review** of Avalonia, not a runtime measurement of it. Nothing here was executed.
- Avalonia's Headless and LinuxFramebuffer/DRM backends were not examined in detail beyond confirming
  they bind `RenderLoop.FromTimer(...)` like everything else
  (`src/Headless/Avalonia.Headless/AvaloniaHeadlessPlatform.cs:89`,
  `src/Linux/Avalonia.LinuxFramebuffer/LinuxFramebufferPlatform.cs:67`) and are therefore subject to
  the same `RenderLoop.cs:146` discard.
