# 03 — Apple frame clocks (iOS, tvOS, Mac Catalyst, macOS Skia)

Scope: can Uno's Skia pipeline get a **real** frame timestamp on Apple targets instead of the
phase-locked estimator in `Compositor.GetFrameTimestamp`, and what do mature stacks feed their
scroll curves?

Verified by reading source unless explicitly marked **UNVERIFIED**. Nothing outside
`specs/scroll-smoothness/clock/` was modified.

---

## 0. Verdict

| Target | Real timestamp available? | Best API | Category | Effort |
|---|---|---|---|---|
| iOS / tvOS / Mac Catalyst | **Yes, and the object is already in Uno's hand** | `CADisplayLink.TargetTimestamp` (+ `.Timestamp`) | (b) predicted present, exactly | **trivial** — read two properties in an existing callback |
| macOS 14+ | Yes | `[NSView displayLink:target:selector:]` → `CADisplayLink.targetTimestamp` | (b) predicted present | medium — ObjC + P/Invoke plumbing |
| macOS 10.15–13 | Yes | `CVDisplayLink` `inOutputTime->hostTime / CVGetHostClockFrequency()` | (b) predicted present | medium — same plumbing, deprecated API |
| macOS today (as shipped) | **No** — Uno's macOS host has no display link at all | — | — | — |

**Epoch answer, up front and it is the good one:** on every Apple platform,
`Stopwatch.GetTimestamp()` resolves to `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`, and
`CLOCK_UPTIME_RAW` *is* `mach_absolute_time()`. `CACurrentMediaTime()` is `mach_absolute_time()`
expressed in seconds, and `CADisplayLink.timestamp` / `.targetTimestamp` are in that base.
So:

```
Compositor.TimestampInTicks  ==  (long)(link.TargetTimestamp * TimeSpan.TicksPerSecond)
```

**No offset, no calibration, no drift correction.** This is not true on most platforms and it is
the single most valuable fact in this document — see §3 for why it is load-bearing rather than
merely convenient.

---

## 1. How Uno drives frames on Apple today

### 1.1 The shared pipeline (all Skia targets)

The record/replay split that creates the problem:

- `CompositionTarget.RenderScheduling.skia.cs:166-176` — `OnNativePlatformFrameRequested` is the
  native frame callback entry point. It **first** enqueues the *next* record onto the UI thread
  (`NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback)`, line 172), **then** replays
  the *previously* recorded picture (`Draw(canvas, resizeFunc)`, line 175).
- `CompositionTarget.RenderScheduling.skia.cs:120-157` — `EnqueueRenderCallback` runs on the UI
  thread and calls `Render()`.
- `CompositionTarget.Rendering.skia.cs:110` — `Render()` records the `SKPicture`
  (`SkiaRenderHelper.RecordPictureAndReturnPath`, line 117).
- `Compositor.skia.cs:308-313` — inside the record walk, `RenderRootVisual` raises `FrameStarting`
  with `GetFrameTimestamp(TimestampInTicks)`.
- `Compositor.cs:31-38` — `TimestampInTicks => (long)(Stopwatch.GetTimestamp() * s_tickFrequency)`,
  where `s_tickFrequency = TimeSpan.TicksPerSecond / Stopwatch.Frequency`. On Apple
  `Stopwatch.Frequency == 1_000_000_000`, so `s_tickFrequency == 0.01` and `TimestampInTicks` is
  mach-absolute nanoseconds / 100.
- `Compositor.skia.cs:244-290` — `GetFrameTimestamp`, the estimator this document is trying to
  replace.

The consequence for Apple specifically: **the frame callback (which knows the vsync time) and the
record (which reads the clock) are on different threads and separated by a dispatcher hop.** The
clock read in `RenderRootVisual` happens at "whenever the UI thread got around to it", which is the
jitter source.

### 1.2 iOS / tvOS / Mac Catalyst — `Uno.UI.Runtime.Skia.AppleUIKit`

`src/Uno.UI.Runtime.Skia.AppleUIKit/Rendering/UnoSKMetalView.cs`:

| Line | Code | Note |
|---|---|---|
| 27 | `private CADisplayLink _link;` | the link is a **field**, live for the view's lifetime |
| 37 | `_link = CADisplayLink.Create(() => this.Draw());` | **the callback discards the link.** `Timestamp`/`TargetTimestamp` are readable here and are thrown away |
| 72 | `Paused = true;` | MTKView's own display link is off |
| 75 | `EnableSetNeedsDisplay = false;` | Uno drives the draw itself |
| 89-126 | `StartRenderThread()` | dedicated `NSThread` at `NSQualityOfService.UserInteractive`, running its own `NSRunLoop` |
| 100-105 | `_link.PreferredFrameRateRange = new CAFrameRateRange { Minimum = 30, Preferred = fps, Maximum = fps }` | ProMotion-aware, iOS 15+ |
| 117 | `_link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default)` | link fires on the render thread |
| 130-133 | `QueueRender() => _link.Paused = false;` | on-demand: unpause to request a frame |
| 147-204 | `IMTKViewDelegate.Draw` | runs **synchronously inside the display-link callback** (`this.Draw()` at line 37 forces the delegate immediately) |
| 153 | `_link.Paused = true;` | re-pause; one frame per request |
| 176 | `_owner?.OnRenderFrameRequested(...)` | → `RootViewController.cs:114-122` → `OnNativePlatformFrameRequested` |

`RootViewController.cs:272-275` — `InvalidateRender() => _skCanvasView?.QueueRender();`

**Key structural fact:** `_link` is in scope for the entire duration of the callback, on the same
thread, and the callback is where `EnqueueRender` is issued. `_link.Timestamp` and
`_link.TargetTimestamp` can be latched at line 37/153 with no new API, no new thread, and no new
platform plumbing. This is a two-line change plus a publication path into the compositor.

`CADisplayLink.TargetTimestamp` is confirmed present in the bindings Uno builds against:
`Microsoft.iOS.Ref.net10.0_26.5/26.5.10284/ref/net10.0/Microsoft.iOS.xml` contains
`P:CoreAnimation.CADisplayLink.TargetTimestamp` and `P:CoreAnimation.CADisplayLink.Timestamp`
(alongside `Duration`, `Paused`, `PreferredFramesPerSecond`). `PreferredFrameRateRange` is present
in the assembly (already used at `UnoSKMetalView.cs:100`) though absent from the XML doc index.

### 1.3 macOS — `Uno.UI.Runtime.Skia.MacOS`

There is **no display link anywhere in the macOS host.** Frames are AppKit-scheduled invalidations:

- `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs:288` —
  `InvalidateRender() => NativeUno.uno_window_invalidate(_nativeWindow.Handle)`
- `UnoNativeMac/UnoNativeMac/UNOWindow.m:390-394` — `uno_window_invalidate` sets
  `renderingView.needsDisplay = true`
- `UnoNativeMac/UnoNativeMac/UNOWindow.m:324` — `v.enableSetNeedsDisplay = YES;` on the
  `UNOMetalFlippedView` (an `MTKView`)
- `UnoNativeMac/UnoNativeMac/UNOMetalViewDelegate.m:35-48` — `drawInMTKView:` calls back into
  managed code: `uno_get_metal_draw_callback()(window, size.width, size.height, drawable.texture)`
- `MacOSWindowHost.cs:86-129` — managed `MetalDraw(width, height, texture)`, line 107 calls
  `OnNativePlatformFrameRequested`
- `MacOSWindowHost.cs:193` — `NativeUno.uno_set_drawing_callbacks(&MetalDraw, &SoftDraw, &Resize)`

`MTKView`'s `drawInMTKView:` **carries no timestamp** — the callback signature is
`(MTKView *view)` and nothing else. MTKView's internal `CVDisplayLink` is not exposed. So on macOS
there is currently nothing to "stop discarding"; a timestamp source must be *added*.

Two further constraints on macOS:

1. `src/Uno.UI.Runtime.Skia.MacOS/Uno.UI.Runtime.Skia.MacOS.csproj:4-7` — the host targets
   `$(NetSkiaPreviousAndCurrent)` with `UNO_REFERENCE_API;HAS_UNO_SKIA`. It is **plain
   `net10.0`, not `net10.0-macos`** — there are no AppKit/CoreAnimation bindings available. All
   display-link work must live in `libUnoNativeMac` (ObjC) and cross via P/Invoke.
2. `UnoNativeMac/UnoNativeMac.xcodeproj/project.pbxproj:291` — `MACOSX_DEPLOYMENT_TARGET = 10.15`.
   `CADisplayLink` on macOS requires 14.0, so a runtime `@available` branch with a `CVDisplayLink`
   fallback is required.

### 1.4 Timestamps Uno already receives and discards

| Site | What arrives | What Uno does |
|---|---|---|
| `AppleUIKit/Rendering/UnoSKMetalView.cs:37` | `_link.Timestamp`, `_link.TargetTimestamp` (live for the whole callback) | discarded — `CADisplayLink.Create(() => this.Draw())` |
| `AppleUIKit/Devices/Input/AppleUIKitPointerInputSource.cs:328-329` | trackpad-scroll flush link, same two properties | discarded |
| `AppleUIKit/Devices/Input/AppleUIKitPointerInputSource.cs:430-431` | momentum link, same two properties | discarded; `UpdateInertiaScrolling` (lines 444-447) decays **per frame**, not per second (`_momentumVelocityX *= InertiaDecelerationRate`) — frame-rate dependent, separate defect |
| `Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:99` | `DoFrame(long frameTimeNanos)` | discarded — `=> onFrame();` (cross-reference, not this document's target) |
| macOS `UNOMetalViewDelegate.m:35` | nothing | n/a — none available |

Note that Uno **already reads the CoreAnimation time base on Apple**:
`AppleUIKitPointerInputSource.cs:297, 309, 339, 429` call `CoreAnimation.CAAnimation.CurrentMediaTime()`
(= `CACurrentMediaTime()`) for gesture windowing. So the base is already in the codebase; it is
simply never joined up with the frame clock.

---

## 2. What the platform can state, by category

Categories per the brief: **(a)** vsync/frame-start time, **(b)** *predicted* presentation time of
the frame being built, **(c)** *measured* presentation time reported after the fact. Only (a) and
(b) can drive a curve.

### 2.1 `CADisplayLink` — iOS 3.1+, tvOS, Mac Catalyst 13+, macOS 14+

| Property | Category | Meaning |
|---|---|---|
| `timestamp` | **(a)** | time value associated with the frame that was **last displayed** — i.e. the vsync this callback is aligned to |
| `targetTimestamp` | **(b)** | time value associated with the **next** frame — i.e. **the presentation time of the frame you draw during this callback** |
| `duration` | — | *nominal* frame interval for the current `preferredFrameRateRange`, **not** the actual one |
| `preferredFrameRateRange` | — | `CAFrameRateRange`, already set at `UnoSKMetalView.cs:100-105` |

Both timestamps are `CFTimeInterval` (double seconds) in the `CACurrentMediaTime()` base — see §3.

`targetTimestamp - timestamp` is the **actual predicted interval for this specific frame**. Under
ProMotion / adaptive-sync this varies frame to frame, which is precisely the case where a
median-of-32 estimator is structurally wrong and the platform value is exact. This is the strongest
technical argument for switching, independent of jitter.

`targetTimestamp` is confirmed in the bindings (§1.2) and confirmed in real use by Flutter
(§5.1, `vsync_waiter_ios.mm:120`).

### 2.2 `CVDisplayLink` — macOS, deprecated

The output callback signature is
`(CVDisplayLinkRef, const CVTimeStamp *inNow, const CVTimeStamp *inOutputTime, CVOptionFlags, CVOptionFlags*, void*)`:

| Field | Category |
|---|---|
| `inNow->hostTime` | **(a)** |
| `inOutputTime->hostTime` | **(b)** — predicted output time of the frame being rendered |

`hostTime` is in host-clock units; `hostTime / CVGetHostClockFrequency()` yields seconds in the
`mach_absolute_time` base. Verified in use: Flutter's macOS shell does exactly this at
`FlutterDisplayLink.mm:156-158`, then compares the result directly against `CACurrentMediaTime()`
at `FlutterVSyncWaiter.mm:78-79` — which is only sound if they share a base.

CVDisplayLink is deprecated as of macOS 14 in favour of the AppKit-vended display links
(§2.3). It still functions; Flutter and Avalonia both still ship it.
*(Deprecation timing verified from documentation/secondary sources, not from an SDK header —
marked **UNVERIFIED at source level**.)*

### 2.3 `NSView/NSWindow/NSScreen.displayLink(target:selector:)` — macOS 14+

Returns an AppKit-managed `CADisplayLink`, so `timestamp`/`targetTimestamp` become available on
macOS with the same semantics as iOS. AppKit additionally tracks which display the view is on and
adapts the rate, and auto-suspends when the view is off-screen — behaviour Uno would otherwise
have to hand-roll for multi-monitor mixed-refresh setups.

Confirmed present in the macOS bindings shipped locally: the string table of
`Microsoft.macOS.Ref/14.0.8478/ref/net8.0/Microsoft.macOS.dll` contains
`displayLinkWithTarget:selector:` immediately adjacent to `addToRunLoop:forMode:` and
`targetTimestamp` (i.e. the `CADisplayLink` binding block), and `CADisplayLink` /
`CVDisplayLinkOutputCallback` type names are both present. Uno's macOS host does not consume those
bindings (§1.3), so this is evidence of API availability, not of a usable managed path.
*Availability floor of macOS 14.0 is **UNVERIFIED at source level** (documentation/secondary
sources only).*

### 2.4 `CAMetalDisplayLink` — iOS 17+ / macOS 14+

Present in the iOS bindings Uno builds against (`CoreAnimation.CAMetalDisplayLink`,
`CAMetalDisplayLinkUpdate`, `CAMetalDisplayLinkDelegate` in `Microsoft.iOS.xml`; the string
`TargetPresentationTimestamp` is present in `Microsoft.iOS.dll` 26.5).

`CAMetalDisplayLinkUpdate` carries `timestamp`, `targetTimestamp`, **`targetPresentationTimestamp`**
and a ready-to-use `CAMetalDrawable`. `targetPresentationTimestamp` is category **(b)** and is the
most precise of the lot for a Metal renderer, because the drawable is vended together with the
prediction. Worth noting as the eventual destination, but it would restructure
`UnoSKMetalView`'s drawable acquisition (currently `CurrentDrawable` at line 186) and it raises the
floor to iOS 17. **Not recommended for this change** — `CADisplayLink.targetTimestamp` gets the
same phase for two lines of code.

### 2.5 Measured presentation — category (c), NOT usable

`IMTLDrawable.PresentedTime` and `addPresentedHandler:` (both present in the iOS bindings) report
when a drawable *was* actually shown, in the `CACurrentMediaTime` base. This is after-the-fact
telemetry. It can be used to **validate** the prediction (see §6.3) but it cannot drive `x(t)` —
by the time it exists, the frame it describes has been displayed.

---

## 3. EPOCH — verified end to end

This is the load-bearing question, because two of Uno's three per-frame drivers **mix a
`Stopwatch`-epoch anchor with frame-clock ticks**:

- `src/Uno.UI/UI/Input/WinRT/GestureRecognizer.Manipulation.InertiaProcessor.cs:355-357`
  ```csharp
  _startTimestamp = compositor.TimestampInTicks;                       // Stopwatch epoch
  _handler = timestamp => onTick(TimeSpan.FromTicks(timestamp - _startTimestamp));  // frame epoch
  compositor.FrameStarting += _handler;
  ```
- `src/Uno.UI/UI/Xaml/Controls/ScrollContentPresenter/ScrollContentPresenter.Managed.cs:669-673`
  ```csharp
  var now = compositor.TimestampInTicks;   // Stopwatch epoch
  _wheelDecayH.Start(HorizontalOffset, now);
  ```
  then ticked from `FrameStarting` (line 677) into `ScrollDecaySimulation.Tick`, which differences
  the two (`ScrollDecaySimulation.cs:58`).

(The touch fling at `ScrollContentPresenter.Managed.cs:598-626` is immune — it deliberately anchors
`_flingStartTimestamp` on the first *frame* timestamp.)

If a platform frame timestamp on a foreign epoch were fed to `FrameStarting`, those two drivers
would compute a garbage `elapsed` on their first tick — an instant jump to the end of the decay
curve, or a negative elapsed. So "same epoch?" is not a nicety; it decides whether the change is
two lines or requires re-anchoring every driver.

### 3.1 `.NET Stopwatch.GetTimestamp()` on Apple — read from runtime source

`dotnet/runtime`, `main` (fetched 2026-07-31):

`src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs`
```csharp
private static long GetFrequency()
{
    const long SecondsToNanoSeconds = 1000000000;
    return SecondsToNanoSeconds;
}

public static long GetTimestamp() => Interop.Sys.GetTimestamp();
```

`src/native/libs/System.Native/pal_time.c`
```c
int64_t SystemNative_GetTimestamp(void)
{
    return minipal_hires_ticks();
}
```

`src/native/minipal/time.c`
```c
int64_t minipal_hires_tick_frequency(void)
{
    return tccSecondsToNanoSeconds;
}

int64_t minipal_hires_ticks(void)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#else
    struct timespec ts;
    int result;
    result = clock_gettime(CLOCK_MONOTONIC, &ts);
    ...
#endif
}
```

`HAVE_CLOCK_GETTIME_NSEC_NP` is Darwin-only (`clock_gettime_nsec_np` is an Apple `_np` extension),
so **on iOS / tvOS / Mac Catalyst / macOS the Apple branch is taken**:
`Stopwatch.GetTimestamp()` = `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`, `Stopwatch.Frequency` = 1e9.

This path is shared by CoreCLR, Mono and NativeAOT — all three route `Interop.Sys.GetTimestamp`
through the same `libSystem.Native` / minipal. *(Shared-CoreLib routing is verified by the source
layout — `System.Private.CoreLib` is shared and `Stopwatch.Unix.cs` is not runtime-flavour
conditioned — but not verified by running on a device: **UNVERIFIED at runtime**.)*

### 3.2 `CLOCK_UPTIME_RAW` = `mach_absolute_time()` — read from Apple's libc

`apple-oss-distributions/Libc`, `main`, `gen/clock_gettime.c`, in `clock_gettime_nsec_np()`:
```c
case CLOCK_UPTIME_RAW:
    mach_time = mach_absolute_time();
```
(for contrast, the same file maps `CLOCK_MONOTONIC_RAW` to `mach_continuous_time()` and
`CLOCK_MONOTONIC` to wall-time-minus-boot-time.)

### 3.3 `CACurrentMediaTime()` and `CADisplayLink` timestamps

`CACurrentMediaTime()` is `mach_absolute_time()` converted to seconds via `mach_timebase_info`, and
`CADisplayLink.timestamp` / `.targetTimestamp` are documented as values in that same base.
*(Documentation-level, plus strong corroborating source evidence: Flutter mixes them arithmetically
at `vsync_waiter_ios.mm:117` — `CACurrentMediaTime() - link.timestamp` — and at
`FlutterVSyncWaiter.mm:78-79` compares a CVDisplayLink-derived timestamp against
`CACurrentMediaTime()`. Neither would be meaningful across epochs. Marked **UNVERIFIED at Apple
source level** — CoreAnimation is closed source.)*

### 3.4 The conversion

```
Stopwatch.GetTimestamp()          = mach_absolute_time expressed in ns
Compositor.TimestampInTicks       = that × 0.01                    (100 ns ticks)
link.TargetTimestamp              = mach_absolute_time expressed in seconds (double)

frameTimestampInTicks = (long)(link.TargetTimestamp * TimeSpan.TicksPerSecond)   // ×1e7
```

`epochMatchesStopwatch: yes.` No offset term. A sanity assertion is still worth writing once
(`|CACurrentMediaTime()*1e7 - TimestampInTicks|` should be microseconds, not milliseconds) —
cheap, and it fails loudly if a future runtime changes the minipal clock choice.

**Sleep caveat.** `CLOCK_UPTIME_RAW`/`mach_absolute_time` do not advance while the system is
asleep. Both sides stop together, so they stay consistent — but a wake produces a large forward gap
on *neither* clock, i.e. the app resumes with `elapsed` roughly where it left off. The existing
whole-period slip branch in `GetFrameTimestamp` (`Compositor.skia.cs:276-281`) exists partly to
handle idle gaps; whatever replaces it must keep an equivalent guard for background/foreground
transitions. `UnoSKMetalView` pauses the link but the *drivers* are not stopped by that.
**UNVERIFIED**: exact resume behaviour on iOS after a long background period.

### 3.5 Double precision

`CFTimeInterval` is a `double` counting seconds since boot. At a 10-day uptime (~8.6e5 s) the
double ULP is ~1.2e-10 s ≈ 0.12 ns; at 100 days, ~1 ns. Converting to 100 ns ticks loses nothing
meaningful. Not a concern.

---

## 4. Which value, exactly, and where to inject it

### 4.1 The frame the record belongs to

Walking Uno's iOS ordering (§1.1, §1.2), at display-link callback *N*:

1. `Draw` replays the picture recorded during interval *(N-1, N)* and commits it →
   it is presented at `TargetTimestamp_N`.
2. `OnNativePlatformFrameRequested` enqueues `Render()` on the UI thread →
   that record happens during *(N, N+1)* and is replayed at callback *N+1* →
   presented at `TargetTimestamp_{N+1}`.

So the picture whose `FrameStarting` fires after callback *N* will be on screen at
`TargetTimestamp_{N+1} ≈ TargetTimestamp_N + period`, with
`period = TargetTimestamp_N − Timestamp_N`.

**Recommended value to latch at callback N:**
```
predictedPresentTicks = (long)((2 * link.TargetTimestamp - link.Timestamp) * TimeSpan.TicksPerSecond)
```
i.e. `targetTimestamp + (targetTimestamp − timestamp)`.

Note the phase is exact either way — `TargetTimestamp_N` alone would also be a perfectly uniform
grid and would fix the jitter. The extra period is what makes the *position* correct for the moment
the pixels actually appear, removing ~1 frame of constant inertia lag on top of the smoothness fix.

**Ship the two separately.** The `+period` term changes absolute position, so it will show up as a
one-time step at the drag→fling handoff (the drag path latches finger geometry and reads no clock,
§ brief). Land uniformity first, measure, then decide on the latency term.

### 4.2 Injection shape

The latch happens on the render thread; the read happens on the UI thread inside
`RenderRootVisual`. Sketch (not implemented — no files outside `clock/` were touched):

- `Compositor` gains an internal `NotifyFrameTiming(long predictedPresentInTicks)` writing a
  `volatile long` (or `Interlocked.Exchange`), plus a monotonic guard so a stale value is never
  replayed.
- `GetFrameTimestamp` prefers the latched value when it is fresh (published since the last raise)
  and **falls back to the existing estimator otherwise**. The estimator must stay: headless
  (`Uno.UI.Runtime.Skia.Headless`), Linux framebuffer, macOS < 14 without the CVDisplayLink work,
  and any host that never publishes a vsync all need it.
- `UnoSKMetalView.cs:37` becomes
  `CADisplayLink.Create(() => { LatchFrameTiming(_link); this.Draw(); })` — or, cleaner, switch to
  the `CADisplayLink.Create(NSObject, Selector)` overload so the link arrives as the callback
  argument (both overloads are in the bindings; see `Microsoft.iOS.xml`).

### 4.3 Multi-window / multi-display wrinkle

`FrameStarting` and `GetFrameTimestamp` live on the **shared** compositor
(`Compositor.GetSharedCompositor()`), but display links are per-view. Two windows on displays with
different refresh rates already confound the current estimator; a single latched value confounds it
identically. Not a regression, but it should be written down: if per-window frame clocks are ever
needed, the state belongs on `CompositionTarget`, not `Compositor`. On iOS/tvOS this is moot
(one screen in practice); on macOS it is real.

### 4.4 macOS plumbing sketch

1. In `libUnoNativeMac`, attach a display link per `UNOWindow`:
   `if (@available(macOS 14.0, *)) { [view displayLinkWithTarget:… selector:…] }` else
   `CVDisplayLinkCreateWithCGDisplay` + `CVDisplayLinkSetOutputHandler`.
2. Normalise both to seconds in the mach base: `link.targetTimestamp` directly, or
   `inOutputTime->hostTime / CVGetHostClockFrequency()`.
3. Extend the drawing callback ABI —
   `MacOSWindowHost.cs:193` / `NativeUno.cs:208` `uno_set_drawing_callbacks`, and
   `UNOMetalViewDelegate.m:48` `uno_get_metal_draw_callback()(...)` — with a
   `double predictedPresentSeconds` parameter, so the value arrives exactly where
   `OnNativePlatformFrameRequested` is already called (`MacOSWindowHost.cs:107`).
   Managed and native ship together from this repo, so the ABI change is contained; it does require
   a macOS build (`UnoNativeMac/build.sh` → xcodebuild), which is why this side is *medium* effort.
4. Decide whether to keep AppKit's `needsDisplay` scheduling (`UNOWindow.m:392`) and use the
   display link purely as a **timestamp observer**, or restructure to iOS-style paused/on-demand
   link-driven drawing. The observer-only variant is far cheaper and sufficient for the phase fix,
   because `drawRect`-driven `drawInMTKView:` still lands near vsync — but it means the latched
   value can be one callback stale. Prefer observer-only first; measure with
   `IMTLDrawable.PresentedTime` (§6.3) before restructuring.

---

## 5. What the mature stacks actually feed their scroll curves

### 5.1 Flutter, iOS — feeds the **predicted presentation time**

Local clone `D:/Work/flutter`, commit `1add24630aef9b084a1c2c1031221b469b72b360` (2026-04-24).

`engine/src/flutter/shell/platform/darwin/ios/framework/Source/vsync_waiter_ios.mm:116-137`
```objc
- (void)onDisplayLink:(CADisplayLink*)link {
  CFTimeInterval delay = CACurrentMediaTime() - link.timestamp;
  fml::TimePoint frame_start_time = fml::TimePoint::Now() - fml::TimeDelta::FromSecondsF(delay);

  CFTimeInterval duration = link.targetTimestamp - link.timestamp;
  fml::TimePoint frame_target_time = frame_start_time + fml::TimeDelta::FromSecondsF(duration);
  ...
  recorder->RecordVsync(frame_start_time, frame_target_time);
  ...
  _callback(std::move(recorder));
}
```

Note what Flutter is doing in lines 117-118: it **re-bases** rather than assuming a shared epoch,
because `fml::TimePoint::Now()` is `std::chrono::steady_clock`
(`engine/src/flutter/fml/time/time_point.cc:51-53`), which is not necessarily the CA base. It
measures the delta *at the callback instant* and subtracts. **Uno does not need this step** — §3
establishes that `Stopwatch` and `CACurrentMediaTime` are literally the same counter — but Flutter's
pattern is the safe fallback if that ever stops holding.

Then the chain to the animation clock, all verified:

| File:line | Code |
|---|---|
| `shell/common/animator.cc:114-118` | `const fml::TimePoint frame_target_time = frame_timings_recorder_->GetVsyncTargetTime(); dart_frame_deadline_ = frame_target_time.ToEpochDelta(); delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number);` |
| `shell/common/engine.cc:296-297` | `Engine::BeginFrame(fml::TimePoint frame_time, …) { runtime_controller_->BeginFrame(frame_time, …); }` |
| `runtime/runtime_controller.cc:299-303` | → `platform_configuration->BeginFrame(frame_time, frame_number)` |
| `lib/ui/window/platform_configuration.cc:454` | `PlatformConfiguration::BeginFrame(fml::TimePoint frameTime, …)` → Dart `onBeginFrame` |
| `packages/flutter/lib/src/scheduler/binding.dart:1226-1231` | `handleBeginFrame(Duration? rawTimeStamp) { _firstRawTimeStampInEpoch ??= rawTimeStamp; _currentFrameTimeStamp = _adjustForEpoch(rawTimeStamp ?? _lastRawTimeStamp); … }` |
| `packages/flutter/lib/src/scheduler/ticker.dart:204` | `_startTime = SchedulerBinding.instance.currentFrameTimeStamp;` |
| `packages/flutter/lib/src/scheduler/ticker.dart:271` | `void _tick(Duration timeStamp) { … _onTick(timeStamp - _startTime!); }` |

**Conclusion:** every Flutter animation — including `BallisticScrollActivity` /
`ScrollPhysics.createBallisticSimulation`, which run on an `AnimationController` → `Ticker` — is
evaluated at `frame_target_time`, i.e. **category (b), the predicted presentation time of the frame
being built**, delivered by `CADisplayLink.targetTimestamp`. Ticker anchors its own start on the
same clock (`ticker.dart:204`), so no epoch mixing exists in Flutter — the hazard identified in §3
for Uno is structurally absent there.

### 5.2 Flutter, macOS — CVDisplayLink `inOutputTime->hostTime`

`engine/src/flutter/shell/platform/darwin/macos/framework/Source/FlutterDisplayLink.mm`:

- lines 105-109: `CVDisplayLinkCreateWithCGDisplay(display_id, &entry.display_link);`
  `CVDisplayLinkSetOutputHandler(…, ^(CVDisplayLinkRef, const CVTimeStamp* in_now, const CVTimeStamp* in_output_time, …)`
- lines 156-161:
  ```objc
  CFTimeInterval timestamp = (CFTimeInterval)inNow.hostTime / CVGetHostClockFrequency();
  CFTimeInterval target_timestamp = (CFTimeInterval)inOutputTime.hostTime / CVGetHostClockFrequency();
  [client didFireWithTimestamp:timestamp targetTimestamp:target_timestamp];
  ```
- The delegate contract (lines 23-24) is literally `didFireWithTimestamp:targetTimestamp:` — the
  same pair as iOS, normalised so the rest of the engine is platform-agnostic.
- `FlutterVSyncWaiter.mm:67-93` then does phase work with them, including
  `CFTimeInterval minStart = targetTimestamp - _displayLink.nominalOutputRefreshPeriod;`
  compared against `CACurrentMediaTime()` — deliberately **not** delivering a vsync too early,
  because rendering too far ahead of the target hurts frame pacing.

Flutter had not (at this commit) migrated macOS to the macOS 14 AppKit display link.

### 5.3 Avalonia — has both timestamps and discards both

Local clone `D:/Work/Avalonia`, commit `e81f3f7ff7802e8dd4dcd52137358bb08952ecc0` (2026-04-23).

`src/iOS/Avalonia.iOS/DisplayLinkTimer.cs`
```csharp
private Stopwatch _st = Stopwatch.StartNew();
...
var link = CADisplayLink.Create(OnLinkTick);      // line 18 — link discarded
...
private void OnLinkTick()                          // lines 40-43
{
    _tick?.Invoke(_st.Elapsed);                    // reads its own stopwatch at callback time
}
```

`native/Avalonia.Native/src/OSX/PlatformRenderTimer.mm:73-79`
```objc
static CVReturn OnTick(CVDisplayLinkRef displayLink, const CVTimeStamp *inNow,
                       const CVTimeStamp *inOutputTime, CVOptionFlags flagsIn,
                       CVOptionFlags *flagsOut, void *displayLinkContext)
{
    PlatformRenderTimer *object = (PlatformRenderTimer *)displayLinkContext;
    object->_callback->Run();                      // inNow and inOutputTime both dropped
    return kCVReturnSuccess;
}
```

Avalonia is in **exactly Uno's current position** on both Apple targets: the vsync-paced callback
is correct, and then the animation time is re-derived from a free-running stopwatch read at
callback entry. This is a useful data point in both directions — it means the pattern is common,
and it means Avalonia is not a source to copy here.

### 5.4 Summary of the three stacks

| Stack | Apple frame clock fed to animations | Category |
|---|---|---|
| Flutter iOS | `CADisplayLink.targetTimestamp` (re-based to steady_clock) | **(b)** |
| Flutter macOS | `CVTimeStamp inOutputTime->hostTime / CVGetHostClockFrequency()` | **(b)** |
| Avalonia iOS | `Stopwatch.Elapsed` read at callback entry | — (none) |
| Avalonia macOS | wall-clock in the render loop; CVTimeStamp discarded | — (none) |
| Uno today | `Stopwatch.GetTimestamp()` at record time + median-grid estimator | — (reconstructed) |

---

## 6. Risks, caveats, and how to prove it

### 6.1 Where the platform value is strictly better than the estimator

- **Variable refresh (ProMotion).** `targetTimestamp - timestamp` is the true predicted interval for
  *this* frame. A median over 32 samples cannot represent a rate that changes frame to frame; it
  will lag every transition and be wrong throughout. Uno explicitly opts into a wide range at
  `UnoSKMetalView.cs:100-105` (`Minimum = 30`), so this is not hypothetical.
- **Startup.** `GetFrameTimestamp` returns the raw jittery clock for the first 8 frames
  (`Compositor.skia.cs:262-265`) — exactly the frames at the start of a fling, when velocity is
  highest and error per ms is largest.
- **Rate changes** (external display connect, low-power mode, thermal throttling) converge in
  ~32 frames with the estimator and instantly with the platform value.

### 6.2 Where the estimator must survive

Keep it as the fallback for: `Uno.UI.Runtime.Skia.Headless`, `Uno.UI.Runtime.Skia.Linux.FrameBuffer`,
macOS < 14 if the CVDisplayLink path is not taken, and any callback where the latched value is
stale. The existing runtime tests (`src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/Given_Compositor.cs:40, 62, 90`)
cover `GetFrameTimestamp` directly and should keep passing unchanged.

### 6.3 How to prove the change on device

- **Epoch assertion (cheap, do it first):** at startup, log
  `CAAnimation.CurrentMediaTime() * 1e7` against `Compositor.TimestampInTicks`. §3 predicts a
  difference of microseconds. If it is milliseconds or worse, §3.1/§3.3 is wrong on that OS version
  and everything downstream must be re-anchored.
- **Uniformity:** with a fling running, log `Δ(frameTimestamp)` per frame. Today it is
  the estimator's grid (uniform by construction, but phase-drifting against reality); the raw
  `TimestampInTicks` deltas are the jitter to compare against; `targetTimestamp` deltas should be
  uniform *and* phase-locked to reality.
- **Prediction accuracy:** `IMTLDrawable.PresentedTime` / `addPresentedHandler:` (category (c))
  gives the ground truth. `presentedTime − predictedTargetTimestamp` per frame is the direct
  measure of whether the prediction is worth using, and whether the `+period` term in §4.1 is the
  right number of frames for Uno's specific record/replay depth. **This is the measurement that
  settles §4.1** — do it before shipping the latency term.
- `ScrollDiagnostics` already records frame samples with
  `Visual.Compositor.CurrentFrameTimestampInTicks` (`ScrollContentPresenter.Managed.cs:189`), so the
  capture harness exists.

### 6.4 Smaller findings surfaced on the way

- `AppleUIKitPointerInputSource.UpdateInertiaScrolling` (lines 444-447) decays velocity by a
  constant factor **per frame**, not per second. On a 120 Hz ProMotion device the trackpad momentum
  decays twice as fast as on a 60 Hz one. It also has a `CADisplayLink` in hand
  (line 430) whose `Timestamp` would give it a proper `dt`. Out of scope here; worth its own item.
- `ChoreographerFramePacer.DoFrame(long frameTimeNanos) => onFrame();`
  (`src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:99`) discards Android's
  vsync time the same way. Covered by the Android document, noted here only because the fix shape —
  latch in the frame callback, publish to the compositor, prefer over the estimator — is identical
  across all three platforms and should be designed once.

---

## 7. Sources

**Uno** (worktree `D:/Work/uno-worktrees/scrollsmooth`, branch `dev/mazi/smooth-scroll`) — all
file:line references inline above.

**Flutter** — `D:/Work/flutter`, commit `1add24630aef9b084a1c2c1031221b469b72b360`.

**Avalonia** — `D:/Work/Avalonia`, commit `e81f3f7ff7802e8dd4dcd52137358bb08952ecc0`.

**.NET runtime** — `dotnet/runtime` `main`, fetched 2026-07-31:
`src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Unix.cs`,
`src/native/libs/System.Native/pal_time.c`,
`src/native/minipal/time.c`.

**Apple libc** — `apple-oss-distributions/Libc` `main`, `gen/clock_gettime.c`.

**Apple bindings inspected locally**:
`C:/Program Files/dotnet/packs/Microsoft.iOS.Ref.net10.0_26.5/26.5.10284/ref/net10.0/Microsoft.iOS.{dll,xml}`,
`C:/Program Files/dotnet/packs/Microsoft.macOS.Ref/14.0.8478/ref/net8.0/Microsoft.macOS.{dll,xml}`.

**Documentation-level only (marked UNVERIFIED above)**: `CACurrentMediaTime` ↔ `mach_absolute_time`
equivalence; `CADisplayLink` timestamp base; macOS 14 availability of
`NSView/NSWindow/NSScreen.displayLink(target:selector:)` and the corresponding CVDisplayLink
deprecation. Sources: [Apple Developer Forums — "What does CADisplayLink's timestamp property mean?"](https://developer.apple.com/forums/thread/52826),
[Audio Host Time On iOS (QA1643)](https://developer.apple.com/library/ios/qa/qa1643/_index.html),
[CADisplayLink and its applications](https://dmtopolog.com/cadisplaylink-and-its-applications/),
[In-Process Animations and Transitions with CADisplayLink, Done Right](https://philz.blog/in-process-animations-and-transitions-with-cadisplaylink-done-right/),
[dotnet/macios — CoreVideo macOS xcode16.0 b1](https://github.com/dotnet/macios/wiki/CoreVideo-macOS-xcode16.0-b1).
