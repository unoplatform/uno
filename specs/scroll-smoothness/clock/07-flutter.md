# Flutter: where the frame timestamp actually comes from

Research note for the Uno scroll-smoothness clock question: *can we get the real vsync / predicted
presentation time instead of reconstructing it?* Flutter is the prior art examined here.

**Sources read (absolute paths, all verified by reading the file):**

- Flutter monorepo: `D:/Work/flutter`, HEAD `1add24630aef9b084a1c2c1031221b469b72b360`
  (`Fri Apr 24 2026`, "Roll Skia from 4c8bedd3c932 to 290a056fcd0e (#185518)")
  - engine C++/ObjC/Java under `D:/Work/flutter/engine/src/flutter/`
  - framework Dart under `D:/Work/flutter/packages/flutter/lib/src/`
  - `dart:ui` under `D:/Work/flutter/engine/src/flutter/lib/ui/`
- .NET runtime: read from `raw.githubusercontent.com/dotnet/runtime/main` (branch `main`, fetched
  2026-07-31) — `src/native/minipal/time.c`,
  `src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Windows.cs`,
  `src/native/libs/System.Native/pal_time.c`.
- Uno: `D:/Work/uno-worktrees/scrollsmooth/src/...`
- Win32 empirical probe run on this machine (Windows 11 Pro 10.0.29595, 120 Hz) — §7.2.

Companion notes: `../research/07-flutter-scroll-physics.md`, `../research/08-flutter-input-frame.md`.
Section 3 of `08-flutter-input-frame.md` already established the frame ordering; this note is
strictly about **which number is on the wire and what clock it lives on**.

---

## 0. Executive summary

1. **Flutter passes the *predicted presentation time*, category (b) — not "now", not vsync-start.**
   `Animator::BeginFrame` ends with
   `delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number)` where `frame_target_time =
   frame_timings_recorder_->GetVsyncTargetTime()` (`animator.cc:113-118`), and
   `PlatformConfiguration::BeginFrame` carries the comment
   `// frameTime is not a delta; its the timestamp of the presentation.`
   (`platform_configuration.cc:469`). That value becomes `SchedulerBinding.handleBeginFrame`'s
   `rawTimeStamp`.

2. **The engine holds *both* (a) and (b) and hands the framework only (b).**
   `FrameTimingsRecorder::RecordVsync(vsync_start, vsync_target)` (`vsync_waiter.cc:139-140`) stores
   the pair; `GetVsyncStartTime()` is used only for telemetry (`FramePhase.vsyncStart`), never for
   animation.

3. **How real (b) is depends entirely on the platform, and on two of Uno's four targets it is a
   pure fiction:**

   | Flutter target | vsync start (a) | target/present (b) | Realness |
   |---|---|---|---|
   | Android (NDK) | `AChoreographer` `frameTimeNanos` — **real** | `frame_start + 1/refreshRate` — derived | real phase, derived target |
   | Android (Java, API<29) | `now − (System.nanoTime() − frameTimeNanos)` — **real**, delta-converted | `frame_start + refreshPeriodNanos` — derived | real phase, derived target |
   | iOS | `now − (CACurrentMediaTime() − link.timestamp)` — **real** | `frame_start + (link.targetTimestamp − link.timestamp)` — **real** | fully real |
   | macOS | `targetTimestamp − nominalOutputRefreshPeriod` | CVDisplayLink `targetTimestamp` — **real** | fully real |
   | **Windows** | `SnapToNextTick(now, 0, frame_interval)` — **synthesized** | `+ frame_interval` | **fake** — no vsync input at all |
   | **Linux (GTK)** | `VsyncWaiterFallback`: snap to a 60 Hz grid | `+ 16.667 ms` | **fake**, and not even the real refresh rate |

4. **Flutter's Windows embedder does exactly what Uno just implemented, only cruder.** It
   quantizes `now` onto a uniform grid whose period comes from `DwmGetCompositionTimingInfo`'s
   *refresh rate* and whose phase anchor is the constant zero
   (`flutter_windows_engine.cc:671-678`, `.h:502`). It never reads `qpcVBlank`. So the mature-stack
   answer to "does anybody actually plumb a real vsync timestamp on Windows?" is **no** — Flutter
   reconstructs it. Uno's estimator is *better* than Flutter's on this target (§6.2).

5. **Measured presentation time (category (c)) exists but never feeds a curve.** `FrameTiming`
   (`platform_dispatcher.dart:2165`) reports `rasterFinish`/`rasterFinishWallTime` after the fact,
   surfaced through `SchedulerBinding.addTimingsCallback` for telemetry only. Nothing in the
   animation path consumes it.

6. **There is no `TargetTimeAndVsyncId` / frame-timeline concept in this revision.** A repo-wide
   grep for `TargetTimeAndVsyncId`, `vsyncId`, `postVsyncCallback` and `FrameTimeline` over
   `engine/src/flutter/**` returns **zero hits**. Android's `AChoreographer_postVsyncCallback`
   (API 33, which *does* expose per-frame-timeline deadline + present time) is **not used**;
   Impeller's Android choreographer wrapper only binds `AChoreographer_postFrameCallback64` /
   `AChoreographer_postFrameCallback` (`impeller/toolkit/android/choreographer.cc:51-70`).

7. **When Flutter crosses a clock domain it never assumes epochs match — it does a paired read of
   both clocks and applies the delta.** iOS: `frame_start = fml::TimePoint::Now() −
   (CACurrentMediaTime() − link.timestamp)` (`vsync_waiter_ios.mm:117-118`). Android/Java:
   `delay = System.nanoTime() − frameTimeNanos` on the Java side (`VsyncWaiter.java:95-100`),
   `frame_time = fml::TimePoint::Now() − delay` on the C++ side
   (`vsync_waiter_android.cc:89-90`). macOS has a whole class for it,
   `FlutterTimeConverter` (`FlutterTimeConverter.mm:27-33`). **This is the single most directly
   copyable technique in this note** — it is robust without needing to prove epoch equality.

---

## 1. The full chain: `handleBeginFrame(rawTimeStamp)` back to the OS

```
platform vsync source (Choreographer / CADisplayLink / CVDisplayLink / a timer)
  → VsyncWaiter<Platform>::AwaitVSync callback
  → VsyncWaiter::FireCallback(frame_start_time, frame_target_time, pause_secondary_tasks)
        vsync_waiter.cc:87-152
        └─ FrameTimingsRecorder::RecordVsync(frame_start_time, frame_target_time)   :139-140
        └─ posts the callback to the UI task runner                                 :130-146
  → Animator::AwaitVSync callback → Animator::BeginFrame(recorder)                  animator.cc:61
        └─ recorder->RecordBuildStart(fml::TimePoint::Now())                        animator.cc:71
        └─ frame_target_time = recorder->GetVsyncTargetTime()                       animator.cc:113-114
        └─ dart_frame_deadline_ = frame_target_time.ToEpochDelta()                  animator.cc:115
        └─ delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number)          animator.cc:118
  → Shell::OnAnimatorBeginFrame                                                     shell.cc:1371
  → Engine::BeginFrame(frame_time, frame_number)                                    engine.cc:296-297
  → RuntimeController::BeginFrame                                                   runtime_controller.cc:299-303
  → PlatformConfiguration::BeginFrame(frameTime, frame_number)                      platform_configuration.cc:454
        └─ int64_t microseconds = frameTime.ToEpochDelta().ToMicroseconds();        :470
        └─ monotonicity clamp against last_microseconds_                            :472-480
        └─ DartInvoke(begin_frame_, {microseconds, frame_number})                   :483-487
  → hooks.dart:401  void _beginFrame(int microseconds, int frameNumber)
  → PlatformDispatcher.instance._beginFrame(microseconds)                           hooks.dart:402
  → SchedulerBinding.handleBeginFrame(Duration rawTimeStamp)                        scheduler/binding.dart:1226
```

The load-bearing three lines, verbatim:

`D:/Work/flutter/engine/src/flutter/shell/common/animator.cc:112-118`
```cpp
  const fml::TimePoint frame_target_time =
      frame_timings_recorder_->GetVsyncTargetTime();
  dart_frame_deadline_ = frame_target_time.ToEpochDelta();
  uint64_t frame_number = frame_timings_recorder_->GetFrameNumber();
  delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number);
```

`D:/Work/flutter/engine/src/flutter/lib/ui/window/platform_configuration.cc:469-480`
```cpp
  // frameTime is not a delta; its the timestamp of the presentation.
  // This is just a type conversion.
  int64_t microseconds = frameTime.ToEpochDelta().ToMicroseconds();
  if (last_microseconds_ > microseconds) {
    // Do not allow time traveling frametimes
    // github.com/flutter/flutter/issues/106277
    FML_LOG(ERROR) << "Reported frame time is older than the last one; clamping. " ...
    microseconds = last_microseconds_;
  }
```

`D:/Work/flutter/engine/src/flutter/flow/frame_timings.h:52-58`
```cpp
  /// Timestamp of the vsync signal.
  fml::TimePoint GetVsyncStartTime() const;

  /// Timestamp of when the frame was targeted to be presented.
  ///
  /// This is typically the next vsync signal timestamp.
  fml::TimePoint GetVsyncTargetTime() const;
```

**Answer to "does Flutter pass the VSYNC time or a `now` read": neither, exactly — it passes the
*next* vsync (the target/present time). `now` is read separately and recorded as `buildStart`
(`animator.cc:71`), which is the jittery quantity Flutter deliberately keeps out of the animation
path.**

---

## 2. Per-platform vsync sources, read line by line

### 2.1 Android — `frameTimeNanos`, real, `CLOCK_MONOTONIC`

`shell/platform/android/vsync_waiter_android.cc:29-46` — the modern path posts an NDK
Choreographer frame callback on the **UI task runner**:

```cpp
void VsyncWaiterAndroid::AwaitVSync() {
  const static bool use_choreographer =
      impeller::android::Choreographer::IsAvailableOnPlatform();
  if (use_choreographer) {
    ...
        choreographer.PostFrameCallback([weak_this](auto time) {
          auto time_ns = std::chrono::time_point_cast<std::chrono::nanoseconds>(time)
                             .time_since_epoch().count();
          OnVsyncFromNDK(time_ns, weak_this);
        });
```

`vsync_waiter_android.cc:64-81`:

```cpp
void VsyncWaiterAndroid::OnVsyncFromNDK(int64_t frame_nanos, void* data) {
  auto frame_time = fml::TimePoint::FromEpochDelta(
      fml::TimeDelta::FromNanoseconds(frame_nanos));
  auto now = fml::TimePoint::Now();
  if (frame_time > now) {
    frame_time = now;                       // never let vsync be in the future
  }
  auto target_time = frame_time + fml::TimeDelta::FromNanoseconds(
                                      1000000000.0 / g_refresh_rate_);
  ...
  ConsumePendingCallback(weak_this, frame_time, target_time);
}
```

Two things matter here:

- **(a) is real** — `frame_nanos` is the raw Choreographer frame time, reinterpreted *directly* as
  a `steady_clock` epoch delta with no conversion. That is only legal because they are the same
  clock: `impeller/toolkit/android/choreographer.h:52-59` declares
  `using FrameClock = std::chrono::steady_clock;` and
  `choreographer.cc:33-35` does
  `Choreographer::FrameTimePoint{std::chrono::nanoseconds(p_nanos)}` on the raw
  `AChoreographer_postFrameCallback64` value, which Android documents as `CLOCK_MONOTONIC`.
- **(b) is *derived*, not obtained** — `target = frame_time + 1/refreshRate`, where
  `g_refresh_rate_` is a `std::atomic_uint` fed from Java (`OnUpdateRefreshRate`,
  `vsync_waiter_android.cc:117-122`, itself from `Display.getRefreshRate()`,
  `VsyncWaiter.java:36-39`). Note it is an **integer** rounded refresh rate, so on a 59.94 Hz or
  120.003 Hz panel the derived period is slightly wrong. Flutter has no true "predicted present"
  on Android.

Legacy Java path (`vsync_waiter_android.cc:84-101`, `VsyncWaiter.java:95-100`) — the paired-read
conversion:

```java
    public void doFrame(long frameTimeNanos) {
      long delay = System.nanoTime() - frameTimeNanos;      // VsyncWaiter.java:96
      if (delay < 0) { delay = 0; }
      flutterJNI.onVsync(delay, refreshPeriodNanos, cookie); // :100
    }
```
```cpp
  auto frame_time =
      fml::TimePoint::Now() - fml::TimeDelta::FromNanoseconds(frameDelayNanos);  // :89-90
  auto target_time =
      frame_time + fml::TimeDelta::FromNanoseconds(refreshPeriodNanos);          // :91-92
```

They send the **age of the vsync**, not the vsync timestamp — so the receiving side never has to
know the sender's epoch.

### 2.2 iOS — `CADisplayLink.timestamp` / `.targetTimestamp`, both real

`shell/platform/darwin/ios/framework/Source/vsync_waiter_ios.mm:116-132`:

```objc
- (void)onDisplayLink:(CADisplayLink*)link {
  CFTimeInterval delay = CACurrentMediaTime() - link.timestamp;
  fml::TimePoint frame_start_time = fml::TimePoint::Now() - fml::TimeDelta::FromSecondsF(delay);

  CFTimeInterval duration = link.targetTimestamp - link.timestamp;
  fml::TimePoint frame_target_time = frame_start_time + fml::TimeDelta::FromSecondsF(duration);
  ...
  recorder->RecordVsync(frame_start_time, frame_target_time);
```

This is the only target where **both (a) and (b) come from the OS**. Again note the paired-read
conversion on line 117-118 rather than an epoch assumption.

### 2.3 macOS — CVDisplayLink `targetTimestamp`, real, plus a deliberate *delay*

`shell/platform/darwin/macos/framework/Source/FlutterVSyncWaiter.mm:66-97`:

```objc
- (void)onDisplayLink:(CFTimeInterval)timestamp targetTimestamp:(CFTimeInterval)targetTimestamp {
  ...
    // CVDisplayLink callback is called one and a half frame before the target
    // timestamp. That can cause frame-pacing issues if the frame is rendered too early,
    // it may also trigger frame start before events are processed.
    CFTimeInterval minStart = targetTimestamp - _displayLink.nominalOutputRefreshPeriod;
    CFTimeInterval current = CACurrentMediaTime();
    CFTimeInterval remaining = std::max(minStart - current - kTimerLatencyCompensation, 0.0);
    ...
    [FlutterRunLoop.mainRunLoop performAfterDelay:remaining block:^{
        ...
        _block(minStart, targetTimestamp, *_pendingBaton);
    }];
```

with `static const CFTimeInterval kTimerLatencyCompensation = 0.001;` (`:38`, "It's preferable to
fire the timers slightly early than too late due to scheduling latency").

Handed to the engine through the embedder API with an explicit clock conversion
(`FlutterEngine.mm:899-907`):

```objc
    block:^(CFTimeInterval timestamp, CFTimeInterval targetTimestamp, uintptr_t baton) {
      uint64_t timeNanos = [timeConverter CAMediaTimeToEngineTime:timestamp];
      uint64_t targetTimeNanos = [timeConverter CAMediaTimeToEngineTime:targetTimestamp];
      ...
      engine->_embedderAPI.OnVsync(_engine, baton, timeNanos, targetTimeNanos);
    }
```

`FlutterTimeConverter.mm:27-33` — the paired read, generalized into a class:

```objc
- (uint64_t)CAMediaTimeToEngineTime:(CFTimeInterval)time {
  ...
  return (time - CACurrentMediaTime()) * NSEC_PER_SEC + engine.embedderAPI.GetCurrentTime();
}
```

`FlutterEngineGetCurrentTime()` is `fml::TimePoint::Now().ToEpochDelta().ToNanoseconds()`
(`embedder.cc:3340-3342`).

### 2.4 Windows — **synthesized; no vsync input whatsoever**

`shell/platform/windows/flutter_windows_engine.cc:671-678`:

```cpp
void FlutterWindowsEngine::OnVsync(intptr_t baton) {
  std::chrono::nanoseconds current_time =
      std::chrono::nanoseconds(embedder_api_.GetCurrentTime());
  std::chrono::nanoseconds frame_interval = FrameInterval();
  auto next = SnapToNextTick(current_time, start_time_, frame_interval);
  embedder_api_.OnVsync(engine_, baton, next.count(),
                        (next + frame_interval).count());
}
```

`flutter_windows_engine.cc:43-51` (comment says "Lifted from vsync_waiter_fallback.cc"):

```cpp
static std::chrono::nanoseconds SnapToNextTick(
    std::chrono::nanoseconds value,
    std::chrono::nanoseconds tick_phase,
    std::chrono::nanoseconds tick_interval) {
  std::chrono::nanoseconds offset = (tick_phase - value) % tick_interval;
  if (offset != std::chrono::nanoseconds::zero())
    offset = offset + tick_interval;
  return value + offset;
}
```

and the phase anchor is a constant zero — `flutter_windows_engine.h:502`:

```cpp
  std::chrono::nanoseconds start_time_ = std::chrono::nanoseconds::zero();
```

which is **never assigned anywhere** (grep for `start_time_` in `shell/platform/windows/` returns
only the declaration and the single use). So the Windows "vsync" grid is literally
`ceil(steady_clock_now / refresh_period) * refresh_period` — a grid anchored to the machine's boot
epoch, with no relationship to where the display's vblank actually falls.

Windows *does* touch DWM, but only for the **period**, never the phase
(`flutter_windows_engine.cc:680-696`):

```cpp
  uint64_t interval = 16600000;
  DWM_TIMING_INFO timing_info = {};
  timing_info.cbSize = sizeof(timing_info);
  HRESULT result = DwmGetCompositionTimingInfo(NULL, &timing_info);
  if (result == S_OK && timing_info.rateRefresh.uiDenominator > 0 &&
      timing_info.rateRefresh.uiNumerator > 0) {
    interval = ... rateRefresh ...;
  }
```

`DWM_TIMING_INFO.qpcVBlank` ("the query performance counter value before the vertical blank",
per the `dwmapi.h` reference on Microsoft Learn) is sitting right there in the same struct and is
**not read**.

The only other DWM use on Windows is a `DwmFlush()` on the raster thread purely to smooth resizes
(`flutter_windows_view.cc:768-771`, "Blocking the raster thread until DWM flushes alleviates
glitches where previous size surface is stretched over current size view") — not a pacer, not a
clock.

### 2.5 Linux (GTK) — `VsyncWaiterFallback`, a 60 Hz grid

`grep -rln "vsync_callback" shell/platform/linux/` → **no matches**. With `vsync_callback` null the
embedder does not construct a `VsyncWaiterEmbedder` (`embedder.cc:2200-2205`), so the shell falls
back to `VsyncWaiterFallback` (`shell/common/vsync_waiter_fallback.cc:36-58`):

```cpp
void VsyncWaiterFallback::AwaitVSync() {
  constexpr fml::TimeDelta kSingleFrameInterval =
      fml::TimeDelta::FromSecondsF(1.0 / 60.0);
  auto frame_start_time =
      SnapToNextTick(fml::TimePoint::Now(), phase_, kSingleFrameInterval);
  auto frame_target_time = frame_start_time + kSingleFrameInterval;
```

Hard-coded 60 Hz, phase anchored at construction time (`phase_(fml::TimePoint::Now())`, `:30`).
**UNVERIFIED**: I did not exhaustively read the GTK embedder to rule out a vsync path that does not
mention the string "vsync"; the grep is the evidence.

---

## 3. `FrameTiming` — measured, and telemetry-only

`lib/ui/platform_dispatcher.dart:2102-2132`:

```dart
enum FramePhase {
  /// The timestamp of the vsync signal given by the operating system.
  vsyncStart,
  /// When the UI thread starts building a frame.
  buildStart,
  buildFinish,
  rasterStart,
  rasterFinish,
  /// When the raster thread finished rasterizing a frame in wall-time.
  rasterFinishWallTime,
}
```

Populated at `flow/frame_timings.cc:202-230` (`RecordRasterEnd`):

```cpp
  raster_end_ = fml::TimePoint::Now();
  raster_end_wall_time_ = fml::TimePoint::CurrentWallTime();
  ...
  timing_.Set(FrameTiming::kVsyncStart, vsync_start_);
  timing_.Set(FrameTiming::kBuildStart, build_start_);
  timing_.Set(FrameTiming::kBuildFinish, build_end_);
  timing_.Set(FrameTiming::kRasterStart, raster_start_);
  timing_.Set(FrameTiming::kRasterFinish, raster_end_);
  timing_.Set(FrameTiming::kRasterFinishWallTime, raster_end_wall_time_);
```

Note carefully: `vsyncStart` here is **(a) `vsync_start_`, not the target `vsync_target_`**, and
`vsync_target_` is not exported to `FrameTiming` at all. Derived metrics
(`platform_dispatcher.dart:2247-2258`):

```dart
  Duration get vsyncOverhead =>
      _rawDuration(FramePhase.buildStart) - _rawDuration(FramePhase.vsyncStart);
  Duration get totalSpan =>
      _rawDuration(FramePhase.rasterFinish) - _rawDuration(FramePhase.vsyncStart);
```

`rasterFinish` is category **(c)** — measured, after the fact, and it reaches Dart via
`PlatformConfiguration::ReportTimings` → `SchedulerBinding.addTimingsCallback`. **Nothing in the
animation or scroll path reads it.** It is a profiler feed.

### 3.1 A stale docstring worth knowing about

`platform_dispatcher.dart:2221-2225` claims:

> The build starts approximately when `PlatformDispatcher.onBeginFrame` is called. The `Duration`
> in the `PlatformDispatcher.onBeginFrame` callback is exactly the
> `Duration(microseconds: timestampInMicroseconds(FramePhase.buildStart))`.

**This is false against the code in this revision.** `buildStart` is
`fml::TimePoint::Now()` sampled inside `Animator::BeginFrame` (`animator.cc:71`), whereas the
`Duration` delivered to `onBeginFrame` is `frame_target_time` (`animator.cc:113-118`). They differ
by `vsyncOverhead + one refresh period`. Do not let this docstring talk anyone into believing
Flutter animates on a "now" read.

---

## 4. Does a Simulation get evaluated at the *built* frame's timestamp? Yes — exactly.

The chain is unbroken and reads no clock of its own:

```
handleBeginFrame(rawTimeStamp)                      scheduler/binding.dart:1226
  _currentFrameTimeStamp = _adjustForEpoch(rawTimeStamp ?? _lastRawTimeStamp);   :1229
  phase = transientCallbacks;  callbacks.forEach(... _invokeFrameCallback(cb, _currentFrameTimeStamp!) ...)  :1258-1269
    → Ticker._tick(Duration timeStamp)              scheduler/ticker.dart:270-277
        _startTime ??= timeStamp;
        _onTick(timeStamp - _startTime!);
      → AnimationController._tick(Duration elapsed) animation/animation_controller.dart:941-947
          final double elapsedInSeconds =
              elapsed.inMicroseconds.toDouble() / Duration.microsecondsPerSecond;
          _value = clampDouble(_simulation!.x(elapsedInSeconds), lowerBound, upperBound);
          if (_simulation!.isDone(elapsedInSeconds)) { ... }
```

and the scroll fling is precisely that (`widgets/scroll_activity.dart:584-604`):

```dart
class BallisticScrollActivity extends ScrollActivity {
  BallisticScrollActivity(super.delegate, Simulation simulation, TickerProvider vsync, this.shouldIgnorePointer) {
    _controller = AnimationController.unbounded(..., vsync: vsync)
      ..addListener(_tick)
      ..animateWith(simulation).whenComplete(_end);
  }
```

So: **`simulation.x(t)` is evaluated at `t = (predicted presentation time of the frame currently
being built) − (predicted presentation time of the frame the fling started on)`.** No `DateTime.now`,
no `Stopwatch`, no re-read anywhere in the path.

### 4.1 How far ahead of *actual* presentation is that stamp?

Flutter's target time is "the next vsync after the one that woke us". The frame being built will
realistically be presented one pipeline stage later than that, because the layer-tree pipeline has
depth 2 (`animator.cc:31-42`) — frame N rasterizes while frame N+1 builds. So on Android/iOS the
handed-out timestamp is typically **one refresh period optimistic**, sometimes two.

**This does not matter, and that is the important insight for Uno.** A *constant* offset between the
timestamp fed to `x(t)` and the moment the pixels appear is invisible: it shifts the whole animation
in time by a fixed amount, which the eye cannot see. What is visible is **non-uniformity of
successive stamps**, because the rendered delta per frame is `x(t_n) − x(t_{n-1})`. Flutter's design
optimizes for uniformity, not for absolute accuracy — which is exactly why it can get away with a
fully synthesized grid on Windows and Linux and still look smooth.

The one place Flutter uses the target time as a *deadline* rather than a *timestamp* is
`dart_frame_deadline_` (`animator.cc:115`), handed to the Dart VM so GC can be scheduled to avoid
overrunning the frame.

---

## 5. `currentFrameTimeStamp` vs `currentSystemFrameTimeStamp` vs `timeDilation`

`scheduler/binding.dart:1082-1084`:

```dart
  Duration? _firstRawTimeStampInEpoch;
  Duration _epochStart = Duration.zero;
  Duration _lastRawTimeStamp = Duration.zero;
```

`_adjustForEpoch` (`:1116-1125`) is the only transform applied:

```dart
  Duration _adjustForEpoch(Duration rawTimeStamp) {
    final Duration rawDurationSinceEpoch = _firstRawTimeStampInEpoch == null
        ? Duration.zero
        : rawTimeStamp - _firstRawTimeStampInEpoch!;
    return Duration(
      microseconds:
          (rawDurationSinceEpoch.inMicroseconds / timeDilation).round() +
          _epochStart.inMicroseconds,
    );
  }
```

| Getter | Value | Cite | Use it for |
|---|---|---|---|
| `currentFrameTimeStamp` | `_adjustForEpoch(raw)` — rebased to the epoch start **and divided by `timeDilation`** | `:1127-1137` | driving animations |
| `currentSystemFrameTimeStamp` | `_lastRawTimeStamp` — the untouched engine value | `:1139-1153` | anything that must share a clock domain with input events |

The doc on `currentSystemFrameTimeStamp` (`:1147-1150`) is candid about the platform variance found
in §2:

> On most platforms, this is a more or less arbitrary value, and should generally be ignored. On
> Fuchsia, this corresponds to the system-provided presentation time...

`timeDilation` (`:37-53`) is the debug slow-motion knob; its setter calls `resetEpoch()` first
(`:52`) so that changing it rebases rather than retroactively rescaling all elapsed time:

```dart
  void resetEpoch() {
    _epochStart = _adjustForEpoch(_lastRawTimeStamp);
    _firstRawTimeStampInEpoch = null;
  }                                                                    // :1103-1106
```

**Design point worth stealing:** two distinct timestamps with an explicit contract — a *dilatable,
rebased* one for animation, and a *raw, never-adjusted* one for cross-domain correlation. The
pointer resampler is required to use the raw one, with the reason spelled out in a comment
(`gestures/binding.dart:218-221`, quoted in `../research/08-flutter-input-frame.md` §1.5):
"it's critical that sample time is in the same clock as the event time stamps, and never adjusted or
scaled like `currentFrameTimeStamp`".

---

## 6. The epoch question, answered concretely

### 6.1 What clock is Flutter's frame timestamp on?

`fml::TimePoint::Now()` (`fml/time/time_point.cc`, non-Fuchsia branch):

```cpp
TimePoint TimePoint::Now() {
  if (gSteadyClockSource) { return gSteadyClockSource.load()(); }
  const int64_t nanos = NanosSinceEpoch(std::chrono::steady_clock::now());
  return TimePoint(nanos);
}
```

so: **nanoseconds since `std::chrono::steady_clock`'s epoch**, on every non-Fuchsia platform.

### 6.2 How that compares to .NET `Stopwatch.GetTimestamp()`, per platform

`Stopwatch.GetTimestamp()` bottoms out in `minipal_hires_ticks()`
(`src/native/minipal/time.c`, dotnet/runtime `main`):

```c
// Windows
int64_t minipal_hires_ticks() { LARGE_INTEGER ts; QueryPerformanceCounter(&ts); return ts.QuadPart; }
int64_t minipal_hires_tick_frequency() { LARGE_INTEGER ts; QueryPerformanceFrequency(&ts); return ts.QuadPart; }

// non-Windows
#define tccSecondsToNanoSeconds 1000000000
int64_t minipal_hires_tick_frequency(void) { return tccSecondsToNanoSeconds; }
int64_t minipal_hires_ticks(void) {
#if HAVE_CLOCK_GETTIME_NSEC_NP
  return (int64_t)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#else
  struct timespec ts;
  int result = clock_gettime(CLOCK_MONOTONIC, &ts);
  assert(result == 0 && "clock_gettime(CLOCK_MONOTONIC) failed");
  return ((int64_t)(ts.tv_sec) * (int64_t)(tccSecondsToNanoSeconds)) + (int64_t)(ts.tv_nsec);
#endif
}
```

(`CLOCK_MONOTONIC_COARSE` is used only by `minipal_lowres_ticks()`, not by `Stopwatch`.)

| Target | .NET `Stopwatch.GetTimestamp()` | The platform vsync clock | Same epoch? |
|---|---|---|---|
| **Windows** | `QueryPerformanceCounter`, freq = QPF | `DWM_TIMING_INFO.qpcVBlank` is documented as a QPC value | **Yes** — *empirically verified*, §7.2 |
| **Android / Linux** | `clock_gettime(CLOCK_MONOTONIC)`, ns, freq = 1e9 | `Choreographer.doFrame(frameTimeNanos)` = `System.nanoTime()` = `CLOCK_MONOTONIC` | **Yes**, and units already match (ns). Note libstdc++/libc++ `steady_clock` is also `CLOCK_MONOTONIC`, which is why Flutter's NDK path can reinterpret the raw nanos with no conversion (`choreographer.cc:33-35`) |
| **macOS / iOS** | `clock_gettime_nsec_np(CLOCK_UPTIME_RAW)`, ns | `CACurrentMediaTime()` / `CADisplayLink.timestamp` | **Very likely yes** — both are `mach_absolute_time` derivatives. **UNVERIFIED**: I verified the .NET side from source but the Apple side only from documented behaviour, not source. Use the paired-read conversion (§2.2/§2.3) and the question goes away |

**Uno-specific unit note:** `Compositor.TimestampInTicks` is
`(long)(Stopwatch.GetTimestamp() * s_tickFrequency)` where
`s_tickFrequency = TimeSpan.TicksPerSecond / Stopwatch.Frequency`
(`src/Uno.UI.Composition/Composition/Compositor.cs:35-38`) — i.e. **100 ns `TimeSpan` ticks**. Any
raw platform timestamp must be scaled the same way before it can be mixed with
`CurrentFrameTimestampInTicks`:

- Android: `frameTimeNanos / 100` → TimeSpan ticks (exact; no rounding drift if you keep the
  remainder, but 100 ns granularity is 4 orders of magnitude below the jitter we care about).
- Windows: `qpc * s_tickFrequency` — on this machine QPF is 10 MHz so `s_tickFrequency == 1.0`
  exactly, but that is **not** guaranteed on other hardware, so do the multiply.

---

## 7. What this means for Uno

### 7.1 What Uno already receives and throws away

`src/Uno.UI.Runtime.Skia.Android/Rendering/ChoreographerFramePacer.cs:97-100`:

```csharp
	private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
	{
		public void DoFrame(long frameTimeNanos) => onFrame();
	}
```

**`frameTimeNanos` is the exact quantity Flutter builds its entire jank story on, and Uno discards
it on that line.** It is `CLOCK_MONOTONIC`, i.e. the same clock as `Stopwatch.GetTimestamp()` on
Android, differing only by the ns→100 ns unit scale. The pacer already runs one callback per vsync
(`ChoreographerFramePacer.cs:66-76`) and is already the thing the Vulkan render loop blocks on
(`UnoSKVulkanView.cs:155-158`), so the plumbing exists; only the value needs forwarding.

On Win32 the equivalent is `Win32RenderPacer.WaitForNextFrame`
(`src/Uno.UI.Runtime.Skia.Win32/Rendering/Win32RenderPacer.cs:53-89`): `PInvoke.DwmFlush()` at
line 61 *returns at a vsync*, and nobody timestamps that return.

### 7.2 Empirical Win32 probe (run on this machine)

Windows 11 Pro 10.0.29595, 120 Hz display, .NET SDK 10.0.300, console app,
`dotnet run` (probe sources in the session scratchpad, not committed).

**Result 1 — `Stopwatch.GetTimestamp()` *is* `QueryPerformanceCounter`, bit for bit:**

```
Stopwatch.Frequency       = 10000000
QueryPerformanceFrequency = 10000000
Stopwatch.IsHighResolution = True
max |Stopwatch.GetTimestamp - QPC| outside bracket (ticks) = 0..1   (bracketed sw1 <= qpc <= sw2)
```

So a QPC-domain vsync timestamp (`qpcVBlank`) needs **no epoch conversion at all** on Windows —
only the `s_tickFrequency` scale, which is `1.0` here.

**Result 2 — `DwmGetCompositionTimingInfo` failed on this machine:**

```
DwmIsCompositionEnabled hr=0x00000000 enabled=1
DwmFlush                hr=0x00000000 (blocked 7.520 ms)
DwmGetCompositionTimingInfo(NULL, ...)  hr=0x88980090
  ... also with hwnd = GetDesktopWindow(), GetShellWindow()
  ... also with cbSize in {224, 232, 288, 296, 304, 312, 320, 328}  (320 is the correct x64 size)
  ... also immediately after a successful DwmFlush()
```

`0x88980090` is in the MIL/WGX facility (`0x8898xxxx`). **UNVERIFIED**: I could not resolve this
HRESULT to a documented symbol, and I did not test on a second machine or a second Windows build.
Treat the conclusion as: *`DwmGetCompositionTimingInfo` is not dependable enough to be the only
source of the Win32 vsync phase.* (This may also explain why Flutter's Windows embedder tolerates
its failure by falling back to a hard-coded 16.6 ms — `flutter_windows_engine.cc:684`.)

**Result 3 — how good an anchor the `DwmFlush()` return instant is** (200 consecutive
flush-and-timestamp iterations on the same machine):

```
median interval 8.3270 ms  (120.09 Hz)
min 7.1762   p05 7.9743   p95 8.6369   max 9.7858 ms
mean |residual| vs a uniform grid at the median period = 0.5045 ms
max  |residual|                                        = 1.8819 ms
```

So the `DwmFlush()` return is a **real but noisy** vsync anchor: ~0.5 ms mean, ~1.9 ms worst-case
phase error, one-sided (wake latency only ever runs late). At the 2650 dip/s launch velocity from
the problem statement that is 1.3 dip mean / 5.0 dip worst — better than the multi-millisecond
record-phase wobble, but **not good enough on its own**. It should feed the estimator's phase, not
replace it.

### 7.3 Recommendations, ordered

1. **Forward the timestamps you already have, and keep the estimator.** The right shape is not
   "estimator *or* real vsync" but "estimator *phase-locked to* real vsync": keep advancing
   `_frameClock` by exactly one median period per frame (which is what structurally guarantees no
   zero-step and no double-step), and replace the *error term* input — currently `raw`, the record
   instant — with the most recent real vsync timestamp, projected forward by whole periods. The
   `error/16` pull and the whole-period slip logic in
   `src/Uno.UI.Composition/Composition/Compositor.skia.cs:244-290` stay exactly as they are; only
   the reference signal gets 5-10x cleaner.

2. **Android: forward `frameTimeNanos`.** Change `ChoreographerFramePacer.FrameCallback.DoFrame`
   (`ChoreographerFramePacer.cs:99`) to pass the value through, publish it as
   `Interlocked.Exchange(ref _lastVsyncTicks, frameTimeNanos / 100)`, and let the compositor read
   it. This is the highest-quality signal available on any Uno target: real, exact, same epoch, and
   already being delivered.

3. **Win32: timestamp the `DwmFlush()` return.** `Stopwatch.GetTimestamp()` on the line after
   `PInvoke.DwmFlush()` in `Win32RenderPacer.cs:61`, published the same way. Optionally *try*
   `DwmGetCompositionTimingInfo().qpcVBlank` once at startup and prefer it when it succeeds — it is
   the exact quantity and needs no conversion (§7.2 Result 1) — but it must be treated as an
   optional upgrade, not a dependency (§7.2 Result 2).

4. **Cross the clock domain by paired read, never by assumption.** Even where the epochs provably
   match today (Android, Win32), Flutter's discipline is worth copying because it costs nothing and
   cannot be wrong:
   `vsyncTicksInStopwatchDomain = Compositor.TimestampInTicks − (nowInPlatformClock − vsyncInPlatformClock)`.
   Cites: `vsync_waiter_ios.mm:117-118`, `VsyncWaiter.java:95-100` +
   `vsync_waiter_android.cc:89-90`, `FlutterTimeConverter.mm:27-33`.

5. **Do not chase a *predicted presentation* time.** No mature stack has a real one on Windows or
   Linux; Android's is `vsync + 1/round(refreshRate)`; only Apple's display links expose a genuine
   one. And per §4.1 the absolute offset is invisible anyway — a constant lead is free, jitter is
   the whole cost. If you want a nominal lead for parity, `vsync + FrameIntervalInTicks` matches
   Flutter's Android/Java derivation exactly.

6. **Adopt the monotonicity clamp.** `platform_configuration.cc:472-480` refuses to hand Dart a
   timestamp older than the previous one. Uno's estimator can, in principle, step backwards when a
   period recomputation shrinks the median while `error/16` is pulling down; a
   `Math.Max(_frameClock, previous + 1)` guard is one line and removes a whole class of
   position-goes-backwards bug.

7. **Split the two timestamps like Flutter does** if a resampler ever lands: a driver-facing
   timestamp (griddable, potentially dilatable) and a raw one guaranteed to share the clock domain
   of input event timestamps. `scheduler/binding.dart:1127-1153`.

8. **Add the telemetry before tuning.** `FrameTiming`'s `vsyncOverhead = buildStart − vsyncStart`
   (`platform_dispatcher.dart:2247`) is precisely the record-phase jitter this whole effort is
   about. Uno can compute it for free once a real vsync timestamp exists, and it turns "feels less
   smooth" into a number.

### 7.4 Where Uno is already ahead of Flutter

Flutter's Windows grid is `SnapToNextTick(now, 0, interval)` — a **quantization of a jittery input**.
Uno's `GetFrameTimestamp` **advances** by one period and then corrects. The difference matters at the
boundary: when Flutter's `now` drifts across a grid line, two consecutive frames can quantize to the
same tick (Δt = 0 → the animation freezes for one frame) or skip one (Δt = 2·period → a double-step).
The monotonicity clamp at `platform_configuration.cc:472-480` prevents going *backwards* but does
nothing about a duplicate. Uno's formulation cannot produce either from phase noise.

*(That failure mode is derived from reading `flutter_windows_engine.cc:671-678` +
`platform_configuration.cc:472-480`; I did not observe it running. **UNVERIFIED as an observed
Flutter defect**, stated only as a property of the algorithm.)*

---

## 8. Explicitly UNVERIFIED

- **HRESULT `0x88980090`** from `DwmGetCompositionTimingInfo` — not resolved to a documented symbol;
  observed only on Windows 11 Pro 10.0.29595 in a console process. Not reproduced on a second
  machine or a second build. It may be specific to this (very new) OS build, this display topology,
  or a console/session detail.
- **macOS/iOS epoch equality** between .NET `Stopwatch` (`CLOCK_UPTIME_RAW`) and
  `CACurrentMediaTime()`. Verified from the .NET side by source; the Apple side is documented
  behaviour, not source I read. The paired-read conversion makes it moot.
- **Linux/GTK embedder**: concluded from `grep -rln "vsync_callback" shell/platform/linux/`
  returning nothing. I did not read `fl_engine.cc` end to end or the renderer classes; a vsync path
  that never names "vsync" would have been missed.
- **Flutter Web** (`lib/web_ui`, `requestAnimationFrame`) — not read at all. Nothing here applies to
  Uno WASM.
- **The Windows duplicate/skipped-tick failure mode** in §7.4 — algorithmic analysis, not an
  observed defect.
- **Whether `AChoreographer_postVsyncCallback` (API 33 frame timelines) is used anywhere in
  Flutter** — the grep across `engine/src/flutter/**` for `vsyncId|postVsyncCallback|FrameTimeline|
  TargetTimeAndVsyncId` found nothing, but I did not check the DEPS-pulled third_party trees.
- **`Choreographer.doFrame(frameTimeNanos)` == `System.nanoTime()` domain** — this is Android
  platform documentation plus the corroborating evidence that Flutter's own Java path computes
  `System.nanoTime() - frameTimeNanos` as a positive "delay" (`VsyncWaiter.java:96`). I did not read
  AOSP source.
