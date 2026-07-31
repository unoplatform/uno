# Flutter: input-to-frame pipeline, jank avoidance, pointer resampling, velocity tracking

Research note for the Uno scroll-smoothness effort. Every claim below is cited to a file and line
number in the checked-out sources. Anything I could not verify in source is marked **UNVERIFIED**.

**Sources read (all absolute paths):**

- Flutter framework: `D:/Work/flutter/packages/flutter/lib/src/...`
- Flutter engine (C++): `D:/Work/flutter/engine/src/flutter/...`
- Repo HEAD: `1add24630aef9b084a1c2c1031221b469b72b360` (`Fri Apr 24 2026`, "Roll Skia from 4c8bedd3c932 to 290a056fcd0e (#185518)")

---

## 0. Executive summary (the five mechanisms that matter)

1. **Frame timestamp is the *presentation* time, not "now".** The engine hands the framework the
   vsync *target* time, so every animation tick is evaluated for the moment the frame will be on
   screen. `platform_configuration.cc:469` literally says
   `// frameTime is not a delta; its the timestamp of the presentation.`
2. **Two-phase frame with a strict phase machine** (`transientCallbacks` → `midFrameMicrotasks` →
   `persistentCallbacks` → `postFrameCallbacks` → `idle`), enforced with asserts. Animation ticks
   are structurally guaranteed one-per-frame because a `Ticker` holds at most one transient callback
   id at a time (`ticker.dart:257,269,291`).
3. **Optional pointer resampling** re-samples raw touch samples onto a uniform timeline offset
   `-38 ms` from frame time, using linear interpolation between two bracketing raw samples. It is
   **off by default in the framework** (`binding.dart:610`).
4. **UI thread and raster thread are separate**, connected by a bounded `Pipeline` of depth 2
   (depth 1 when platform==raster thread, non-Metal). Layout/paint (UI thread) produces a layer
   tree; rasterization (raster thread) consumes it. `animator.cc:32,37-41`.
5. **Retained layers**: any layer whose `_needsAddToScene` is false is re-submitted with
   `SceneBuilder.addRetained(engineLayer)` rather than re-recorded (`layer.dart:707-709`).
   Repaint boundaries (`RenderRepaintBoundary`, `RenderViewport`, `_RenderSingleChildViewport`)
   scope `markNeedsPaint` so a scroll dirties one layer, not the whole tree.

A very important asymmetry for Uno: **sliver viewports re-run layout on every scroll frame**
(`RenderViewportBase.attach` subscribes `markNeedsLayout` to the offset), while
**`SingleChildScrollView` is paint-only** (`_hasScrolled` → `markNeedsPaint`). Section 4 proves both.

---

## 1. Pointer resampling

### 1.1 What it is

`PointerEventResampler` (`D:/Work/flutter/packages/flutter/lib/src/gestures/resampler.dart:36`)
queues raw pointer events for one pointer device and, on demand, emits synthetic events positioned
at an arbitrary `sampleTime` by linearly interpolating between the two raw samples that bracket that
time.

Doc comment, `resampler.dart:15-35`:

```
/// Class for pointer event resampling.
/// ...
/// This can be used to get smooth touch event processing at the cost
/// of adding some latency. Devices with low frequency sensors or when
/// the frequency is not a multiple of the display frequency
/// (e.g., 120Hz input and 90Hz display) benefit from this.
```

### 1.2 The interpolation core

`resampler.dart:130-149`:

```dart
Offset _positionAt(Duration sampleTime) {
  double x = _next?.position.dx ?? 0.0;
  double y = _next?.position.dy ?? 0.0;
  final Duration nextTimeStamp = _next?.timeStamp ?? Duration.zero;
  final Duration lastTimeStamp = _last?.timeStamp ?? Duration.zero;
  // Resample if `next` time stamp is past `sampleTime`.
  if (nextTimeStamp > sampleTime && nextTimeStamp > lastTimeStamp) {
    final double interval = (nextTimeStamp - lastTimeStamp).inMicroseconds.toDouble();
    final double scalar = (sampleTime - lastTimeStamp).inMicroseconds.toDouble() / interval;
    final double lastX = _last?.position.dx ?? 0.0;
    ...
    x = lastX + (x - lastX) * scalar;
    y = lastY + (y - lastY) * scalar;
  }
  return Offset(x, y);
}
```

Pure **linear interpolation** (no prediction, no extrapolation): when `sampleTime` is beyond the
newest queued sample, `scalar` is not applied and the newest position is used verbatim. Because the
sampling offset is negative, the normal case is genuine interpolation between two already-received
samples.

`_processPointerEvents(sampleTime)` (`resampler.dart:151-172`) walks the queue and maintains
`_last` (newest event at-or-before `sampleTime`) and `_next` (first event after `sampleTime`).

`sample()` (`resampler.dart:316-326`) is the entry point:

```dart
void sample(Duration sampleTime, Duration nextSampleTime, HandleEventCallback callback) {
  _processPointerEvents(sampleTime);
  _dequeueAndSampleNonHoverOrMovePointerEventsUntil(sampleTime, nextSampleTime, callback);
  if (_isTracked) {
    _samplePointerPosition(sampleTime, callback);
  }
}
```

Key details:

- **Move/hover events are never forwarded directly.** They are dropped from the queue and replaced
  by *synthesized* move/hover events generated only when the interpolated position actually changed
  (`resampler.dart:240-242, 277-298`). This is what removes jitter: the framework sees exactly one
  positional update per sample tick, spaced by a uniform time delta, regardless of how bursty the
  driver delivery was.
- **Up/removed events are allowed to be processed early**, before their nominal sample time, if they
  land before `nextSampleTime` (`resampler.dart:192-198`):

  ```
  // Update `endTime` to allow early processing of up and removed
  // events as this improves resampling of these events, which is
  // important for fling animations.
  ```
  This matters because the fling velocity is computed at pointer-up; deferring the up event by up
  to a frame would corrupt the velocity estimate.
- Non-move events (down/up/cancel/added/removed) are emitted with `timeStamp: sampleTime`,
  `delta: Offset.zero`, and `position` = the interpolated position (`resampler.dart:263-270`).
- `synthesized` is inherited from the source event (`resampler.dart:76, 112`), i.e. resampled move
  events are **not** flagged `synthesized`. That is deliberate: `DragGestureRecognizer.handleEvent`
  skips `event.synthesized` events for velocity tracking (`monodrag.dart:656`), so resampled moves
  **do** feed the velocity tracker. Resampling therefore smooths velocity estimation too.

### 1.3 The default sampling offset — the actual number

`D:/Work/flutter/packages/flutter/lib/src/gestures/binding.dart:218-232`:

```dart
// The default sampling offset.
//
// Sampling offset is relative to presentation time. If we produce frames
// 16.667 ms before presentation and input rate is ~60hz, worst case latency
// is 33.334 ms. This however assumes zero latency from the input driver.
// 4.666 ms margin is added for this.
const Duration _defaultSamplingOffset = Duration(milliseconds: -38);

// The sampling interval.
//
// Sampling interval is used to determine the approximate time for subsequent
// sampling. This is used to sample events when frame callbacks are not
// being received and decide if early processing of up and removed events
// is appropriate. 16667 us for 60hz sampling interval.
const Duration _samplingInterval = Duration(microseconds: 16667);
```

**Numbers:**

| Constant | Value | Where |
|---|---|---|
| default sampling offset | `-38 ms` (integer milliseconds) | `gestures/binding.dart:224` |
| sampling interval | `16667 µs` (60 Hz) | `gestures/binding.dart:232` |
| derivation | `16.667 + 16.667 + 4.666 = 38.0` | comment `binding.dart:220-223` |

**On "−38.3 ms" / `kDefaultSamplingOffset`:** neither exists in this tree. A repo-wide grep for
`kDefaultSamplingOffset` and for `_defaultSamplingOffset` returns exactly one definition,
`Duration(milliseconds: -38)` at `gestures/binding.dart:224`, plus test-local constants
(`test/gestures/gesture_binding_resample_event_test.dart:113` uses `-5 ms`;
`gesture_binding_resample_event_on_widget_test.dart:76` defines a local `kSamplingOffset = -5 ms`).
The public knob is the mutable field `GestureBinding.samplingOffset`
(`gestures/binding.dart:616`). **A `-38.3 ms` value is UNVERIFIED in this source tree.**

### 1.4 Is it on by default? **No.**

`gestures/binding.dart:599-610`:

```dart
/// Enable pointer event resampling for touch devices by setting
/// this to true.
///
/// Resampling results in smoother touch event processing at the
/// cost of some added latency. ...
bool resamplingEnabled = false;
```

Grep across `packages/` finds no production assignment of `resamplingEnabled = true` — only two
test files (`test/gestures/gesture_binding_resample_event_test.dart:116`,
`gesture_binding_resample_event_on_widget_test.dart:75,116`). So it is an opt-in that applications
must set themselves.

Also note it applies **only to `PointerDeviceKind.touch`** (`gestures/binding.dart:100`); every other
kind (mouse, trackpad, stylus) is dispatched immediately:

```dart
void addOrDispatch(PointerEvent event) {
  if (event.kind == PointerDeviceKind.touch) {
    ...resampler.addEvent(event);
  } else {
    _handlePointerEvent(event);   // immediate
  }
}
```

### 1.5 The sampling clock and how sample time is advanced

`_Resampler.sample()` (`gestures/binding.dart:120-194`) is the scheduler glue:

```dart
final int samplingIntervalUs = _samplingInterval.inMicroseconds;
final int elapsedIntervals = _frameTimeAge.elapsedMicroseconds ~/ samplingIntervalUs;
final int elapsedUs = elapsedIntervals * samplingIntervalUs;
final Duration frameTime = _frameTime + Duration(microseconds: elapsedUs);
final Duration sampleTime = frameTime + samplingOffset;          // line 148
final Duration nextSampleTime = sampleTime + _samplingInterval;  // line 152
```

and re-phases itself to real vsync in a **post-frame callback** (`gestures/binding.dart:174-193`):

```dart
// Add a post frame callback as this avoids producing unnecessary
// frames but ensures that sampling phase is adjusted to frame
// time when frames are produced.
scheduler.addPostFrameCallback((_) {
  _frameCallbackScheduled = false;
  // We use `currentSystemFrameTimeStamp` here as it's critical that
  // sample time is in the same clock as the event time stamps, and
  // never adjusted or scaled like `currentFrameTimeStamp`.
  _frameTime = scheduler.currentSystemFrameTimeStamp;
  _frameTimeAge.reset();
  _timer?.cancel();
  _timer = Timer.periodic(_samplingInterval, (_) => _onSampleTimeChanged());
  _onSampleTimeChanged();
}, debugLabel: 'Resampler.startTimer');
```

Two consequences worth copying:

- **Clock discipline**: sample time must live in the same clock domain as event timestamps
  (`currentSystemFrameTimeStamp`, i.e. the *raw* engine timestamp, not the `timeDilation`-scaled
  `currentFrameTimeStamp`). See `scheduler/binding.dart:1151-1153` for `currentSystemFrameTimeStamp`
  and `1116-1125` for `_adjustForEpoch` (which is what dilates `currentFrameTimeStamp`).
- **A periodic timer keeps sampling alive** when no frames are being produced
  (`gestures/binding.dart:131-132`), so a finger that stops moving still gets its queued events
  drained. When there are no active resamplers the timer is cancelled (`binding.dart:169-172`).

Debug hook: `debugPrintResamplingMargin` prints `_lastEventTime - _lastSampleTime`
(`gestures/binding.dart:206-215`) — the safety margin between the newest received event and the
sample time. Negative margin ⇒ resampler is extrapolating ⇒ the offset is too small.

### 1.6 Why this removes jitter — the mechanism, stated precisely

Raw touch sample streams have three defects:

1. delivery jitter (packets arrive in bursts on some frames, none on others),
2. rate mismatch (e.g. 120 Hz sensor into a 90 Hz display, or 60 Hz sensor into 120 Hz display),
3. non-uniform sample spacing from the driver.

Because the widget's applied scroll delta per frame is `position(t_n) - position(t_{n-1})`, any
non-uniformity in *which* samples land in *which* frame turns directly into non-uniform pixel
deltas — visible as stutter even at a locked 60 fps. Resampling replaces "whatever arrived" with
"position evaluated at `frameTime + offset`", where consecutive `frameTime`s are one vsync apart.
Uniform time base ⇒ uniform velocity ⇒ uniform pixel delta.

The price is latency: exactly `|samplingOffset|` ≈ 38 ms of added end-to-end input lag in the
worst case, less on average (the offset is measured from *presentation* time, so it partly consumes
latency that already existed).

### 1.7 The engine-side sibling: `SmoothPointerDataDispatcher`

Distinct from framework resampling and it **is** on by default on iOS.

`D:/Work/flutter/engine/src/flutter/shell/common/pointer_data_dispatcher.h:103-139`:

```
/// A dispatcher that may temporarily store and defer the last received
/// PointerDataPacket if multiple packets are received in one VSYNC. The
/// deferred packet will be sent in the next vsync in order to smooth out the
/// events. This filters out irregular input events delivery to provide a smooth
/// scroll on iPhone X/Xs.
...
/// If the input event is irregular, but with a random latency of no more than
/// one frame, this would guarantee that we'll miss at most 1 frame. Without
/// this, we could miss half of the frames.
```

Implementation (`pointer_data_dispatcher.cc:28-49`): if a dispatch is already in progress for this
frame, the new packet is cached in `pending_packet_` (overwriting any earlier pending packet — only
the newest is kept) and flushed on the next vsync via `ScheduleSecondaryVsyncCallback`.

Platform selection:
- iOS: `shell/platform/darwin/ios/platform_view_ios.mm:108-109` → `SmoothPointerDataDispatcher`.
- Everything else: `shell/common/platform_view.cc:123-124` → `DefaultPointerDataDispatcher`
  (pass-through).

`ScheduleSecondaryVsyncCallback` is a vsync callback that fires **even if no frame was requested**
(`shell/common/pointer_data_dispatcher.h:56-59`, implemented at `animator.cc:297-300` →
`vsync_waiter.cc:53-85`). That is the primitive Uno would need as well: "wake me at the next vsync
even if I have nothing to draw yet".

---

## 2. VelocityTracker

File: `D:/Work/flutter/packages/flutter/lib/src/gestures/velocity_tracker.dart`.

### 2.1 Default `VelocityTracker` — constants

`velocity_tracker.dart:142-145`:

```dart
static const int _assumePointerMoveStoppedMilliseconds = 40;
static const int _historySize = 20;
static const int _horizonMilliseconds = 100;
static const int _minSampleSize = 3;
```

| Parameter | Value | Meaning |
|---|---|---|
| history buffer | 20 samples (circular) | `_samples` at `velocity_tracker.dart:159` |
| horizon | 100 ms | samples older than this are excluded (`:217`) |
| max gap between samples | 40 ms | a larger gap terminates the window (`:217`) |
| "pointer stopped" timeout | 40 ms since last `addPosition` | returns `Velocity.zero` outright (`:181-188`) |
| minimum samples for a fit | 3 | `:232` |
| polynomial degree | **2** (quadratic) | `LeastSquaresSolver(...).solve(2)` at `:234-235` |

### 2.2 Sample-window walk

`velocity_tracker.dart:207-230` iterates backwards from the newest sample:

```dart
final double age = (newestSample.time - sample.time).inMicroseconds.toDouble() / 1000;
final double delta = (sample.time - previousSample.time).inMicroseconds.abs().toDouble() / 1000;
previousSample = sample;
if (age > _horizonMilliseconds || delta > _assumePointerMoveStoppedMilliseconds) {
  break;
}
...
x.add(position.dx); y.add(position.dy); w.add(1.0); time.add(-age);
```

Note **uniform weights (`w = 1.0`)** and **time expressed in milliseconds, negative-going**
(newest sample at `t = 0`). Velocity is then the linear coefficient converted to px/s
(`velocity_tracker.dart:238-244`):

```dart
pixelsPerSecond: Offset(xFit.coefficients[1] * 1000, yFit.coefficients[1] * 1000),
confidence: xFit.confidence * yFit.confidence,
duration: newestSample.time - oldestSample.time,
offset: newestSample.point - oldestSample.point,
```

`coefficients[1]` is the first-derivative term of a degree-2 polynomial evaluated at `t = 0` — i.e.
instantaneous velocity at the newest sample, from a quadratic fit over the last ≤100 ms / ≤20
samples. `late final` on the two fits (`:233-235`) avoids computing the y-fit if the x-fit fails.

### 2.3 Least-squares solver

`D:/Work/flutter/packages/flutter/lib/src/gestures/lsq_solver.dart:106-203`. Gram-Schmidt QR
decomposition of the weighted Vandermonde matrix; `confidence` is R² computed at `:173-200`:

```dart
result.confidence = sumSquaredTotal <= precisionErrorTolerance
    ? 1.0
    : 1.0 - (sumSquaredError / sumSquaredTotal);
```

Returns `null` (no fit) if `degree > x.length` (`:107-110`) or if a basis vector norm falls below
`precisionErrorTolerance` (`:145-148`).

### 2.4 `IOSScrollViewFlingVelocityTracker` — the magic multipliers

`velocity_tracker.dart:295-398`. Does **not** do a regression at all. It keeps 20 raw samples and
takes a fixed weighted average of the last three pairwise finite differences.

`velocity_tracker.dart:366-369`:

```dart
final Offset estimatedVelocity =
    _previousVelocityAt(-2) * 0.6 +
    _previousVelocityAt(-1) * 0.35 +
    _previousVelocityAt(0) * 0.05;
```

`_previousVelocityAt(index)` (`:328-345`) is `(end.point - start.point) / dt` for the pair at
`(_index + index)` and `(_index + index - 1)` — so index `0` is the **newest** pair, `-1` the one
before it, `-2` two before.

**Weights: newest pair 0.05, previous 0.35, one before that 0.60.** The newest finger sample is
almost entirely discounted — this is exactly the "counter the accidental flick when lifting the
finger" heuristic. The comment at `:360-365` explains it approximates the velocity an iOS scroll
view reports at touch-release, not the pan recognizer's velocity.

`_sampleSize = 20` (`velocity_tracker.dart:303`) with the note:

```
/// The velocity estimation uses at most 4 `_PointAtTime` samples. The extra
/// samples are there to make the `VelocityEstimate.offset` sufficiently large
/// to be recognized as a fling. See `VerticalDragGestureRecognizer.isFlingGesture`.
```

The 40 ms "pointer stopped" short-circuit is shared with the base class (`:350-358`).

### 2.5 `MacOSScrollViewFlingVelocityTracker` — different multipliers

Subclasses the iOS tracker, overriding only the weights (`velocity_tracker.dart:436-439`):

```dart
final Offset estimatedVelocity =
    _previousVelocityAt(-2) * 0.15 +
    _previousVelocityAt(-1) * 0.65 +
    _previousVelocityAt(0) * 0.2;
```

**macOS weights: 0.20 / 0.65 / 0.15 (newest → oldest).** More weight on recent motion than iOS,
which fits a trackpad (no finger-lift artifact).

### 2.6 Which tracker is used where

`D:/Work/flutter/packages/flutter/lib/src/widgets/scroll_configuration.dart:213-225`:

```dart
GestureVelocityTrackerBuilder velocityTrackerBuilder(BuildContext context) {
  switch (getPlatform(context)) {
    case TargetPlatform.iOS:
      return (PointerEvent event) => IOSScrollViewFlingVelocityTracker(event.kind);
    case TargetPlatform.macOS:
      return (PointerEvent event) => MacOSScrollViewFlingVelocityTracker(event.kind);
    case TargetPlatform.android:
    case TargetPlatform.fuchsia:
    case TargetPlatform.linux:
    case TargetPlatform.windows:
      return (PointerEvent event) => VelocityTracker.withKind(event.kind);
  }
}
```

Wired into the recognizer at `widgets/scrollable.dart:802` / `:826`
(`..velocityTrackerBuilder = _configuration.velocityTrackerBuilder(context)`), and consumed by
`DragGestureRecognizer._addPointer` (`gestures/monodrag.dart:413`).

**Windows/Linux/Android use the quadratic least-squares tracker.** For Uno on desktop Skia this is
the reference implementation to mirror.

### 2.7 Fling gating constants

`gestures/constants.dart`:

| Constant | Value | Line |
|---|---|---|
| `kTouchSlop` | 18.0 logical px | `:65` |
| `kPanSlop` | 36.0 (`kTouchSlop * 2`) | `:76` |
| `kMinFlingVelocity` | 50.0 px/s | `:90` |
| `kMaxFlingVelocity` | 8000.0 px/s | `:95` |
| `kPrecisePointerHitSlop` (mouse) | 1.0 px | `:103` |
| `kPrecisePointerPanSlop` | 2.0 px | `:106` |

`VerticalDragGestureRecognizer.isFlingGesture` (`monodrag.dart:942-947`) requires **both**
velocity > `minFlingVelocity` **and** `estimate.offset.dy.abs() > minDistance` — velocity alone is
not enough; the gesture must also have covered ground. `considerFling` then clamps to
`maxFlingVelocity` (`monodrag.dart:954-955`).

Physics overrides these: `BouncingScrollPhysics.minFlingVelocity = kMinFlingVelocity * 2.0 = 100`
(`scroll_physics.dart:780`) and, for `ScrollDecelerationRate.fast`,
`maxFlingVelocity = kMaxFlingVelocity * 8.0 = 64000` (`scroll_physics.dart:807-810`).

### 2.8 Drag-side smoothing that also affects perceived smoothness

`ScrollDragController` (`widgets/scroll_activity.dart:256-467`) has three anti-jank heuristics:

| Constant | Value | Line | Purpose |
|---|---|---|---|
| `momentumRetainStationaryDurationThreshold` | 20 ms | `:302` | lose carried momentum if the finger is stationary this long |
| `momentumRetainVelocityThresholdFactor` | 0.5 | `:310` | new fling must be ≥50% of carried velocity to compound |
| `motionStoppedDurationThreshold` | 50 ms | `:315` | re-arm the start threshold after a pause |
| `_bigThresholdBreakDistance` | 24.0 px | `:319` | above this, treat threshold break as a deliberate fling |
| `dragStartDistanceMotionThreshold` (iOS) | 3.5 px | `scroll_physics.dart:804` | dead zone at motion start |

The threshold-break easing at `scroll_activity.dart:379-388` is the anti-jump trick:

```dart
// Ease into the motion when the threshold is initially broken
// to avoid a visible jump.
return math.min(motionStartDistanceThreshold! / 3.0, offset.abs()) * offset.sign;
```

`carriedMomentum` for iOS (`scroll_physics.dart:796-799`):
`sign * min(0.000816 * |v|^1.967, 40000.0)`.

---

## 3. Frame pipeline ordering — vsync to raster

### 3.1 Engine side (C++)

Threads (`engine/src/flutter/common/task_runners.h:15-35`): **platform**, **UI**, **IO**, **raster**.

Sequence for one frame:

1. Something calls `Animator::RequestFrame` (`animator.cc:239-271`). It is idempotent within a
   frame via `pending_frame_semaphore_.TryWait()` (`:250-254`). It then **posts** `AwaitVSync` to
   the UI task runner rather than calling it inline:

   ```
   // The AwaitVSync is going to call us back at the next VSync. However, we want
   // to be reasonably certain that the UI thread is not in the middle of a
   // particularly expensive callout. We post the AwaitVSync to run right after
   // an idle.                                                    (animator.cc:256-261)
   ```
2. `VsyncWaiter::FireCallback(frame_start_time, frame_target_time, pause_secondary_tasks)`
   (`vsync_waiter.cc:87-152`) posts a task to the UI task runner carrying both timestamps and
   records them into a `FrameTimingsRecorder` (`:137-141`). If `pause_secondary_tasks` is set it
   *pauses the Dart event loop's secondary source* for the duration of the frame
   (`vsync_waiter.cc:114-116, 154-160`) — i.e. non-frame microtask/timer work is deliberately held
   off so it cannot steal UI-thread time mid-frame.
3. `Animator::AwaitVSync`'s callback (`animator.cc:273-289`):
   - if `CanReuseLastLayerTrees()` (nothing was invalidated), `DrawLastLayerTrees` — a re-present
     with **no Dart work at all** (`animator.cc:218-237`);
   - else `BeginFrame(recorder)` then `EndFrame()`.
4. `Animator::BeginFrame` (`animator.cc:61-119`):
   - `RecordBuildStart(now)` (`:72`);
   - acquires a `producer_continuation_` from the layer-tree pipeline; **if the pipeline is full
     (raster thread is behind) it bails out with `TRACE_EVENT0("flutter","PipelineFull")` and
     re-requests a frame** (`:99-108`). This is the backpressure that prevents the UI thread from
     running ahead and burning battery/queueing latency.
   - `dart_frame_deadline_ = frame_target_time` (`:114-116`);
   - `delegate_.OnAnimatorBeginFrame(frame_target_time, frame_number)` (`:118`) — **note it passes
     the target (presentation) time.**
5. `PlatformConfiguration::BeginFrame` (`platform_configuration.cc:454-492`) converts and invokes
   Dart:

   ```cpp
   // frameTime is not a delta; its the timestamp of the presentation.
   int64_t microseconds = frameTime.ToEpochDelta().ToMicroseconds();
   if (last_microseconds_ > microseconds) {
     // Do not allow time traveling frametimes
     microseconds = last_microseconds_;          // monotonicity clamp, :472-480
   }
   ...
   tonic::DartInvoke(begin_frame_.Get(), {microseconds, frame_number});   // :483-487
   UIDartState::Current()->FlushMicrotasksNow();                          // :489
   tonic::DartInvokeVoid(draw_frame_.Get());                              // :491
   ```

   This is precisely the three-part shape: `onBeginFrame` → **flush microtasks** → `onDrawFrame`.
   (`hooks.dart:401,412` are the Dart entry points.)
6. `Animator::EndFrame` (`animator.cc:121-186`): `RecordBuildEnd`, then
   `producer_continuation_.Complete(FrameItem{layer trees, timings})` and, if this item is the head
   of the pipeline, `delegate_.OnAnimatorDraw(layer_tree_pipeline_)` which kicks the raster thread.
   `Rasterizer::Draw` asserts it runs on the raster task runner (`rasterizer.cc:253-255`,
   `:513-515`).
7. Idle heuristic: 51 ms after a frame with none scheduled, notify the VM it can GC
   (`animator.cc:17-21`):

   ```cpp
   // Wait 51 milliseconds (which is 1 more milliseconds than 3 frames at 60hz)
   // before notifying the engine that we are idle.
   constexpr fml::TimeDelta kNotifyIdleTaskWaitTime = fml::TimeDelta::FromMilliseconds(51);
   ```

**Pipeline depth** (`animator.cc:31-42`):

```cpp
#if SHELL_ENABLE_METAL
  layer_tree_pipeline_(std::make_shared<FramePipeline>(2)),
#else
  layer_tree_pipeline_(std::make_shared<FramePipeline>(
      task_runners.GetPlatformTaskRunner() == task_runners.GetRasterTaskRunner() ? 1 : 2)),
#endif
```

Depth 2 = one frame being rasterized while the next is being built. Depth 1 when platform and raster
share a thread.

### 3.2 Framework side (Dart) — the phase machine

`SchedulerPhase` (`scheduler/binding.dart:160-199`):

| Phase | Enum | What runs |
|---|---|---|
| 0 | `idle` | tasks, microtasks, timers, **input event handlers** |
| 1 | `transientCallbacks` | `scheduleFrameCallback` callbacks → all `Ticker`s / `AnimationController`s |
| 2 | `midFrameMicrotasks` | microtasks scheduled by transient callbacks |
| 3 | `persistentCallbacks` | `addPersistentFrameCallback` → build/layout/paint/composite |
| 4 | `postFrameCallbacks` | `addPostFrameCallback` → cleanup, next-frame scheduling |

`handleBeginFrame` (`scheduler/binding.dart:1226-1274`):

```dart
_currentFrameTimeStamp = _adjustForEpoch(rawTimeStamp ?? _lastRawTimeStamp);   // :1229
if (rawTimeStamp != null) { _lastRawTimeStamp = rawTimeStamp; }                // :1230-1232
assert(schedulerPhase == SchedulerPhase.idle);                                  // :1253
_hasScheduledFrame = false;                                                     // :1254
try {
  _schedulerPhase = SchedulerPhase.transientCallbacks;                          // :1258
  final Map<int, _FrameCallbackEntry> callbacks = _transientCallbacks;
  _transientCallbacks = <int, _FrameCallbackEntry>{};                           // :1260  (swap-out!)
  callbacks.forEach((int id, _FrameCallbackEntry e) {
    if (!_removedIds.contains(id)) { _invokeFrameCallback(e.callback, _currentFrameTimeStamp!, e.debugStack); }
  });
  _removedIds.clear();
} finally {
  _schedulerPhase = SchedulerPhase.midFrameMicrotasks;                          // :1272
}
```

The **swap-out at line 1260** is the once-per-frame guarantee at the scheduler level: a callback
re-registered from inside a transient callback lands in the *new* map and therefore runs next frame,
never twice in this frame.

`handleDrawFrame` (`scheduler/binding.dart:1338-1376`):

```dart
assert(_schedulerPhase == SchedulerPhase.midFrameMicrotasks);         // :1339
_schedulerPhase = SchedulerPhase.persistentCallbacks;                 // :1343
for (final callback in List<FrameCallback>.of(_persistentCallbacks)) {
  _invokeFrameCallback(callback, _currentFrameTimeStamp!);            // :1345
}
_schedulerPhase = SchedulerPhase.postFrameCallbacks;                  // :1349
final localPostFrameCallbacks = List<FrameCallback>.of(_postFrameCallbacks);
_postFrameCallbacks.clear();                                          // :1351
... invoke ... 
finally { _schedulerPhase = SchedulerPhase.idle; _currentFrameTimeStamp = null; }  // :1365,1374
```

Post-frame callbacks are also drained by copy-then-clear (`:1350-1351`), so one registered during
the post-frame phase runs *next* frame.

The single persistent callback that matters is registered in
`rendering/binding.dart:61` → `_handlePersistentFrameCallback` (`rendering/binding.dart:508-511`):

```dart
void _handlePersistentFrameCallback(Duration timeStamp) {
  drawFrame();
  _scheduleMouseTrackerUpdate();
}
```

`WidgetsBinding.drawFrame` (`widgets/binding.dart:1536-1573`) does
`buildOwner!.buildScope(rootElement!)` then `super.drawFrame()`; `RendererBinding.drawFrame`
(`rendering/binding.dart:642-653`):

```dart
void drawFrame() {
  rootPipelineOwner.flushLayout();
  rootPipelineOwner.flushCompositingBits();
  rootPipelineOwner.flushPaint();
  if (sendFramesToEngine) {
    for (final RenderView renderView in renderViews) {
      renderView.compositeFrame();   // this sends the bits to the GPU
    }
    rootPipelineOwner.flushSemantics();   // this sends the semantics to the OS.
    _firstFrameSent = true;
  }
}
```

**Full ordering, vsync → raster:**

```
vsync (platform) 
  → VsyncWaiter::FireCallback  [posts to UI thread, carries frame_start + frame_target]
  → Animator::BeginFrame       [acquire pipeline slot or bail "PipelineFull"]
  → PlatformConfiguration::BeginFrame  → Dart onBeginFrame
      → SchedulerBinding.handleBeginFrame
          phase = transientCallbacks   : all Tickers / AnimationControllers tick
      phase = midFrameMicrotasks
  → FlushMicrotasksNow (C++)
  → Dart onDrawFrame
      → SchedulerBinding.handleDrawFrame
          phase = persistentCallbacks  : buildScope → flushLayout → flushCompositingBits
                                          → flushPaint → compositeFrame (SceneBuilder → engine)
                                          → flushSemantics
          phase = postFrameCallbacks   : resampler re-phasing, mouse tracker, cleanup
          phase = idle
  → Animator::EndFrame  → pipeline.Complete → OnAnimatorDraw
  → [RASTER THREAD] Rasterizer::Draw → DoDraw → DrawToSurfaces → GPU submit / present
```

### 3.3 The one-tick-per-frame guarantee for animations

`Ticker` (`scheduler/ticker.dart`):

```dart
int? _animationId;                                                   // :253
bool get scheduled => _animationId != null;                          // :257
bool get shouldScheduleTick => !muted && isActive && !scheduled;     // :269

void scheduleTick({bool rescheduling = false}) {
  assert(!scheduled);
  assert(shouldScheduleTick);                                        // :291-292
  if (forceFrames) { SchedulerBinding.instance.scheduleForcedFrame(); }
  else            { SchedulerBinding.instance.scheduleFrame(); }     // :293-297
  _animationId = SchedulerBinding.instance.scheduleFrameCallback(
    _tick, rescheduling: rescheduling, scheduleNewFrame: false);     // :298-302
}

void _tick(Duration timeStamp) {
  assert(isTicking);
  assert(scheduled);
  _animationId = null;                                               // :271-274
  _startTime ??= timeStamp;
  _onTick(timeStamp - _startTime!);                                  // :276-277
  if (shouldScheduleTick) { scheduleTick(rescheduling: true); }       // :281-283
}
```

The invariant chain:

- at most one outstanding `_animationId` per ticker (`shouldScheduleTick` requires `!scheduled`);
- `_animationId` is cleared *before* the user callback runs, and re-armed *after*;
- the scheduler's map swap-out (`scheduler/binding.dart:1260`) means the re-armed callback cannot
  fire in the same `handleBeginFrame`.

Therefore: **exactly one `_onTick` per frame per active ticker, and the elapsed time passed to it is
derived from the presentation timestamp** — `timeStamp - _startTime`, both being
`currentFrameTimeStamp` values. Animation position is a pure function of presentation time, so a
dropped frame produces a *jump*, never a *slowdown*. That is the property that makes Flutter
animations look correct even under load.

`scheduleFrame` itself is idempotent (`scheduler/binding.dart:946-959`,
`if (_hasScheduledFrame || !framesEnabled) return;`) and `_hasScheduledFrame` is cleared at
`handleBeginFrame` line 1254.

`ensureVisualUpdate` (`scheduler/binding.dart:906-917`) is the smart re-entrancy guard — it only
schedules a *new* frame if the current phase is `idle` or `postFrameCallbacks`; during
transient/mid-microtask/persistent phases the in-flight frame will pick the change up.

Scroll ballistic animation is exactly this path:
`BallisticScrollActivity` builds an `AnimationController.unbounded(vsync: context.vsync)` and
`animateWith(simulation)` (`widgets/scroll_activity.dart:596-604`), with `_tick` calling
`applyMoveTo(_controller.value)` → `delegate.setPixels(value)` (`:619-635`).

---

## 4. What does a `ViewportOffset` change invalidate? Layout or paint?

**Answer: it depends on the viewport implementation, and the two mainstream ones differ.**

### 4.1 Sliver viewports → `markNeedsLayout` (layout every scroll frame)

`D:/Work/flutter/packages/flutter/lib/src/rendering/viewport.dart:685-695`:

```dart
@override
void attach(PipelineOwner owner) {
  super.attach(owner);
  _offset.addListener(markNeedsLayout);      // :688
}

@override
void detach() {
  _offset.removeListener(markNeedsLayout);   // :693
  super.detach();
}
```

and the setter (`viewport.dart:530-545`):

```dart
set offset(ViewportOffset value) {
  if (value == _offset) { return; }
  if (attached) { _offset.removeListener(markNeedsLayout); }
  _offset = value;
  if (attached) { _offset.addListener(markNeedsLayout); }
  // We need to go through layout even if the new offset has the same pixels
  // value as the old offset so that we will apply our viewport and content
  // dimensions.
  markNeedsLayout();
}
```

`ViewportOffset` is a `ChangeNotifier` (`rendering/viewport_offset.dart:100`) and
`ScrollPosition.setPixels` calls `notifyListeners()` when pixels actually change
(`widgets/scroll_position.dart:392`). So: **every scroll delta on a `CustomScrollView` / `ListView`
/ `GridView` dirties layout.**

`RenderViewport.performLayout` (`viewport.dart:1693-1765`) then re-runs the sliver layout loop:

```dart
final int maxLayoutCycles = _maxLayoutCyclesPerChild * childCount;   // :1719, per-child factor = 10 (:1685)
do {
  correction = _attemptLayout(mainAxisExtent, crossAxisExtent, offset.pixels + centerOffsetAdjustment);
  if (correction != 0.0) { offset.correctBy(correction); }
  else if (offset.applyContentDimensions(...)) { break; }
  count += 1;
} while (count < maxLayoutCycles);
```

Because `SliverConstraints` carries `scrollOffset` (`rendering/sliver.dart:331`),
`precedingScrollExtent` (`:356`), `remainingPaintExtent` (`:381`), `cacheOrigin` (`:420`) and
`remainingCacheExtent` (`:439`), and `RenderObject.layout` short-circuits only when
`!_needsLayout && constraints == _constraints` (`rendering/object.dart:2848`), **each sliver's
`performLayout` really does re-run every scroll frame.** What is *not* re-run is the layout of the
box children inside the sliver: they receive unchanged `BoxConstraints` and so hit the
short-circuit at `object.dart:2848` (they are relayout boundaries when constraints are tight, per
`object.dart:2847`: `_isRelayoutBoundary = !parentUsesSize || sizedByParent || constraints.isTight || parent == null`).

Damage control that makes this affordable:

- `RenderViewportBase.isRepaintBoundary => true` (`viewport.dart:752`) — the viewport owns its own
  `OffsetLayer`, so `markNeedsPaint` never escapes upward.
- Cache extent 250 logical px on each side (`viewport.dart:289`:
  `static const double defaultCacheExtent = 250.0;`), so slivers keep a small ring of
  already-laid-out children beyond the visible window.
- Children are wrapped in `RepaintBoundary` by default
  (`widgets/sliver.dart:217, 295, 365, 494, 545`: `bool addRepaintBoundaries = true`), so each list
  item is its own retained engine layer.

### 4.2 `SingleChildScrollView` → `markNeedsPaint` only

`D:/Work/flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart`:

```dart
void _hasScrolled() {
  markNeedsPaint();                 // :403
  markNeedsSemanticsUpdate();       // :404
}

@override
void attach(PipelineOwner owner) {
  super.attach(owner);
  _offset.addListener(_hasScrolled);  // :419
}

@override
bool get isRepaintBoundary => true;   // :429
```

(The offset *setter* still calls `markNeedsLayout` at `:386`, because swapping the offset object can
change dimensions; but a *pixels change* on the same object is paint-only.)

**Proof of the contrast, side by side:**

| Scroll container | Listener attached to `ViewportOffset` | Effect of a pixels change |
|---|---|---|
| `RenderViewportBase` (ListView/GridView/CustomScrollView) | `markNeedsLayout` (`viewport.dart:688`) | full sliver layout pass, then paint |
| `_RenderSingleChildViewport` | `_hasScrolled` → `markNeedsPaint` (`single_child_scroll_view.dart:403,419`) | repaint of one repaint-boundary layer |

This is the single most transferable finding for Uno: **paint-only scrolling is achievable and is
what Flutter does for the non-virtualized case; the layout-per-frame cost is accepted only where
virtualization requires it, and is bounded by relayout boundaries + repaint boundaries + a 250 px
cache ring.**

### 4.3 Frame-phase discipline around offset mutation

`ScrollPosition.setPixels` (`widgets/scroll_position.dart:366-401`):

```dart
assert(
  SchedulerBinding.instance.schedulerPhase != SchedulerPhase.persistentCallbacks,
  "A scrollable's position should not change during the build, layout, and paint phases, "
  "otherwise the rendering will be confused.",
);
```

and layout-time corrections go through `correctBy` (`scroll_position.dart:458-465`) /
`correctPixels` (`:438-440`), which mutate `_pixels` **without** `notifyListeners()` — precisely so
that a correction issued from inside `performLayout` cannot re-dirty layout mid-pass. `correctBy`'s
contract is documented at `viewport_offset.dart:188-200`.

`forcePixels` (`scroll_position.dart:490-498`) notifies and additionally records `_impliedVelocity`
for one frame, cleared in a post-frame callback.

---

## 5. Mouse wheel and trackpad

### 5.1 Two entirely different paths

| Input | Event type | Path | Result |
|---|---|---|---|
| Mouse wheel | `PointerScrollEvent` (a `PointerSignalEvent`) | `Listener.onPointerSignal` → `PointerSignalResolver` → `ScrollPosition.pointerScroll` | **immediate jump, no animation** |
| Trackpad scroll (native gesture) | `PointerPanZoomStart/Update/End` | `DragGestureRecognizer.addAllowedPointerPanZoom` → normal drag → ballistic fling | animated, physics-driven |
| Trackpad scroll (web / some platforms) | `PointerScrollEvent` with `kind == trackpad` | same as wheel | immediate jump |

### 5.2 Pointer signal routing

`PointerSignalEvent` is a discrete signal that does not change pointer state
(`gestures/events.dart:1780-1804`). Dispatch: `GestureBinding._handlePointerEventImmediately`
hit-tests fresh for `PointerSignalEvent` (`gestures/binding.dart:409-421`) and
`GestureBinding.handleEvent` resolves it (`gestures/binding.dart:543-545`):

```dart
} else if (event is PointerSignalEvent) {
  pointerSignalResolver.resolve(event);
}
```

The resolver exists so nested scrollables don't all scroll: the innermost that registers interest
wins (`gestures/pointer_signal_resolver.dart`).

`Scrollable`'s handler (`widgets/scrollable.dart:953-982`):

```dart
void _receivedPointerSignal(PointerSignalEvent event) {
  if (event is PointerScrollEvent && _position != null) {
    if (_physics != null && !_physics!.shouldAcceptUserOffset(position)) { return; }
    final double delta = _pointerSignalEventDelta(event);
    final double targetScrollOffset = _targetScrollOffsetForPointerScroll(delta);
    // Only express interest in the event if it would actually result in a scroll.
    if (delta != 0.0 && targetScrollOffset != position.pixels) {
      GestureBinding.instance.pointerSignalResolver.register(event, _handlePointerScroll);
      return;
    }
  } else if (event is PointerScrollInertiaCancelEvent) {
    position.pointerScroll(0);
    // Don't use the pointer signal resolver, all hit-tested scrollables should stop.
  }
}

void _handlePointerScroll(PointerEvent event) {
  ...
  if (delta != 0.0 && targetScrollOffset != position.pixels) {
    position.pointerScroll(delta);
    // Tell engine this scrollable handled the event.
    scrollEvent.respond(allowPlatformDefault: false);
  }
}
```

`_pointerSignalEventDelta` (`scrollable.dart:932-951`) applies axis flipping only for
`PointerDeviceKind.mouse` when a modifier from `ScrollBehavior.pointerAxisModifiers` is held —
explicitly *not* for trackpads (comment at `:936-941`).

### 5.3 Is wheel scrolling animated? **No.**

`ScrollPositionWithSingleContext.pointerScroll`
(`widgets/scroll_position_with_single_context.dart:210-236`):

```dart
void pointerScroll(double delta) {
  if (delta == 0.0) { goBallistic(0.0); return; }
  final double targetPixels = math.min(math.max(pixels + delta, minScrollExtent), maxScrollExtent);
  if (targetPixels != pixels) {
    goIdle();
    updateUserScrollDirection(-delta > 0.0 ? ScrollDirection.forward : ScrollDirection.reverse);
    final double oldPixels = pixels;
    isScrollingNotifier.value = true;
    forcePixels(targetPixels);          // instantaneous
    didStartScroll();
    didUpdateScrollPositionBy(pixels - oldPixels);
    didEndScroll();
    goBallistic(0.0);
  }
}
```

There is **no `Curve`, no `Duration`, no `AnimationController`** anywhere on this path. Each wheel
notch produces one instantaneous `forcePixels` jump, one notify, and then `goBallistic(0.0)` which
produces `null` simulation (velocity 0, in range) and therefore `goIdle()`
(`scroll_position_with_single_context.dart:149-157`). Smoothness on a wheel therefore comes entirely
from the platform emitting many small `scrollDelta`s (as macOS/trackpad and modern Windows
high-precision wheels do), not from framework easing.

**Contrast with WinUI/Avalonia, which animate wheel deltas.** If Uno wants Flutter-like behavior, it
would be "no easing"; if it wants WinUI-like behavior, easing must be added on top — Flutter offers
no constant to copy here.

`PointerScrollInertiaCancelEvent` (`gestures/events.dart:1989-2010`, doc: "Touching the trackpad
immediately after a scroll") is handled by calling `position.pointerScroll(0)`, which routes to
`goBallistic(0.0)` → `goIdle()` → stop. Notably it deliberately **bypasses** the signal resolver so
that *every* hit-tested scrollable stops (`scrollable.dart:967`).

### 5.4 Trackpad pan/zoom → real drag

`DragGestureRecognizer.addAllowedPointerPanZoom` (`gestures/monodrag.dart:441-448`):

```dart
@override
void addAllowedPointerPanZoom(PointerPanZoomStartEvent event) {
  super.addAllowedPointerPanZoom(event);
  startTrackingPointer(event.pointer, event.transform);
  if (_state == _DragState.ready) { _initialButtons = kPrimaryButton; }
  _addPointer(event);
}
```

and `handleEvent` treats `PointerPanZoomUpdateEvent` exactly like a move, reading `event.panDelta` /
`event.localPanDelta` and feeding `event.pan` into the velocity tracker
(`monodrag.dart:656-666, 672-686`). So a native trackpad scroll gets the full drag → velocity →
ballistic-fling treatment.

`PointerDeviceKind.trackpad` is in the default drag device set
(`widgets/scroll_configuration.dart:29-33, 120`), and pan/zoom drags do **not** set ignore-pointer
(`widgets/scroll_activity.dart:538`:
`bool get shouldIgnorePointer => _controller?._kind != PointerDeviceKind.trackpad;`).

Hit-slop for trackpad is the touch slop (18 px) not the precise-pointer slop
(`gestures/events.dart:2552-2563`).

`kDefaultTrackpadScrollToScaleFactor` and `trackpadScrollCausesScale` exist on
`ScaleGestureRecognizer` (`gestures/scale.dart:354-355, 403-421`) but are `false` by default and are
about pinch-zoom, not scrolling.

### 5.5 Physics constants reachable from these paths (for reference)

`ClampingScrollSimulation` (Android), `widgets/scroll_simulation.dart:164-259`:

| Constant | Value | Line |
|---|---|---|
| `friction` | 0.015 | `:169` |
| `_kDecelerationRate` | `log(0.78)/log(0.9) ≈ 2.3582` | `:201` |
| `_kInflexion` | 0.35 | `:204` |
| `_physicalCoeff` | `9.80665 * 39.37 * 160.0 * 0.84` | `:208-215` |
| position curve | `position + distance * (1 - (1-t)^rate)` | `:251-254` |

`BouncingScrollSimulation` (iOS), `widgets/scroll_simulation.dart:50-56`:

```dart
// Taken from UIScrollView.decelerationRate (.normal = 0.998)
// 0.998^1000 = ~0.135
_frictionSimulation = FrictionSimulation(0.135, ...);
```

Springs: default `SpringDescription.withDampingRatio(mass: 0.5, stiffness: 100.0, ratio: 1.1)`
(`scroll_physics.dart:411-415`); fast-deceleration variant
`(mass: 0.3, stiffness: 75.0, ratio: 1.3)` (`:816`).

Ballistic stop tolerance (`scroll_physics.dart:439-445`):

```dart
Tolerance(
  velocity: 1.0 / (0.050 * metrics.devicePixelRatio),  // logical px/s
  distance: 1.0 / metrics.devicePixelRatio,            // logical px
);
```

i.e. stop when the simulation would move less than one physical pixel in 50 ms — a DPI-aware
termination condition, which prevents both premature stop (visible snap) and endless sub-pixel
ticking (wasted frames).

Overscroll friction factor: `pow(1 - overscrollFraction, 2) * {fast: 0.26, normal: 0.52}`
(`scroll_physics.dart:704-710`); `constantDeceleration` 1400 for fast, 0 for normal (`:767-770`).

---

## 6. Keeping the UI thread free during scroll

### 6.1 Thread split

Four task runners (`engine/src/flutter/common/task_runners.h:29-35`): platform, UI, IO, raster.

- The UI thread runs Dart: animate → build → layout → paint → `SceneBuilder` → `render()`.
  It produces a `LayerTree` (a display-list description), **not** pixels.
- The raster thread runs `Rasterizer::Draw` / `DoDraw` / `DrawToSurfaces`, asserted to be on the
  raster task runner (`rasterizer.cc:253-255, 513-515`), and does the actual GPU submission.
- The bounded pipeline of depth 2 (`animator.cc:32`) lets frame N rasterize while frame N+1 builds,
  and applies backpressure via `PipelineFull` (`animator.cc:101-108`) when the raster thread falls
  behind — the UI thread then simply re-requests a frame instead of queueing unbounded work.
- The IO thread exists for texture upload / image decode so those never block either.

### 6.2 Dart event-loop pause during a frame

`VsyncWaiter::FireCallback` optionally calls `PauseDartEventLoopTasks()` before running the frame
callback and `ResumeDartEventLoopTasks()` after (`vsync_waiter.cc:112-146, 154-167`), which
pauses the *secondary* task source on the UI task queue. Effect: unrelated timers/messages cannot
interleave into the middle of a frame's build/layout/paint on the UI thread.

### 6.3 Layer reuse (retained rendering)

`rendering/layer.dart:696-716`:

```dart
void _addToSceneWithRetainedRendering(ui.SceneBuilder builder) {
  if (!_needsAddToScene && _engineLayer != null) {
    builder.addRetained(_engineLayer!);
    return;
  }
  addToScene(builder);
  _needsAddToScene = false;
}
```

Dirty propagation rules (`layer.dart:358-372`):

```
// - If [alwaysNeedsAddToScene] is true, then [_needsAddToScene] is also true.
// - If [_needsAddToScene] is true and [parent] is not null, then
//   `parent._needsAddToScene` is true.
```

Made consistent by `updateSubtreeNeedsAddToScene` before compositing
(`layer.dart:495-497, 1160-1166`). Individual layer types keep their engine layer across frames via
`oldLayer: _engineLayer as ui.OffsetEngineLayer?` (`layer.dart:1507`, `:1670`, etc.), so the engine
can reuse GPU-side state.

### 6.4 Repaint boundaries scope the damage

`RenderObject.markNeedsPaint` (`rendering/object.dart:3326-3367`):

```dart
if (_needsPaint) { return; }
_needsPaint = true;
if (isRepaintBoundary && _wasRepaintBoundary) {
  // If we always have our own layer, then we can just repaint
  // ourselves without involving any other nodes.
  owner!._nodesNeedingPaint.add(this);
  owner!.requestVisualUpdate();
} else if (parent != null) {
  parent!.markNeedsPaint();        // walk up until a boundary
}
```

Plus `markNeedsCompositedLayerUpdate` (`object.dart:3386-3406`) which can update a composited
layer's *properties* (e.g. an offset or opacity) without repainting children — the closest analogue
to "move the layer, don't re-record it".

Symmetrically, `markNeedsLayout` (`object.dart:2660-2679`) stops at a relayout boundary:

```dart
if (owner case final PipelineOwner owner? when (_isRelayoutBoundary ?? false)) {
  owner._nodesNeedingLayout.add(this);
  owner.requestVisualUpdate();
} else if (parent != null) {
  markParentNeedsLayout();
}
```

with `_isRelayoutBoundary = !parentUsesSize || sizedByParent || constraints.isTight || parent == null`
(`object.dart:2847`) and the layout short-circuit
`if (!_needsLayout && constraints == _constraints) { ...return; }` (`object.dart:2848`).

Boundaries relevant to scrolling:

| Object | `isRepaintBoundary` | Cite |
|---|---|---|
| `RenderViewportBase` | `true` | `rendering/viewport.dart:752` |
| `_RenderSingleChildViewport` | `true` | `widgets/single_child_scroll_view.dart:429` |
| Every `ListView`/`GridView` item (default) | `true` via wrapper `RepaintBoundary` | `widgets/sliver.dart:217,295,365,494,545` (`addRepaintBoundaries = true`) |

### 6.5 Skipping the whole Dart frame when nothing changed

`Animator::CanReuseLastLayerTrees()` (`animator.cc:218-220`) returns `!regenerate_layer_trees_`;
if true, `DrawLastLayerTrees` re-presents the previous layer tree without entering Dart at all
(`animator.cc:222-237`, `rasterizer.cc:219-235`). Cost is "very cheap" per the comment at
`animator.cc:225-226`.

### 6.6 Warm-up frame and event locking at startup

`scheduleWarmUpFrame` (`scheduler/binding.dart:1037-1080`) runs a full build/layout/paint outside
vsync so the first real frame is cheap, and **locks event dispatch** while it runs:

```dart
// Lock events so touch events etc don't insert themselves until the
// scheduled frame has finished.
lockEvents(() async { await endOfFrame; ... });
```

`GestureBinding.unlocked()` then flushes the queued pointer events
(`gestures/binding.dart:294-298, 347-353`).

### 6.7 Frame timing telemetry

`FrameTiming` via `addTimingsCallback` (`scheduler/binding.dart:321-328`) exposes
`buildDuration`, `rasterDuration`, `vsyncOverhead`, `totalSpan`
(`_profileFramePostEvent`, `scheduler/binding.dart:1378-1387`). The doc explicitly names the failure
mode Uno should measure for (`scheduler/binding.dart:284-294`):

```
/// It is possible for no frames to be missed but for the
/// latency to be more than one frame in the case where the Flutter
/// engine is pipelining the graphics updates... In those cases,
/// animations will be smooth but touch input will feel more sluggish.
```

---

## 7. Latency arithmetic (how many frames from finger to photon)

With resampling **off** (the default):

1. Pointer packet arrives on the platform thread, is posted to the UI thread.
2. `GestureBinding._handlePointerDataPacket` → `handlePointerEvent` →
   `_handlePointerEventImmediately` → hit test → `DragGestureRecognizer.handleEvent` →
   `ScrollDragController.update` → `applyUserOffset` → `setPixels` → `notifyListeners` →
   `markNeedsLayout`/`markNeedsPaint` → `PipelineOwner.requestVisualUpdate` →
   `RendererBinding.ensureVisualUpdate` (`rendering/binding.dart:842-844`) →
   `SchedulerBinding.scheduleFrame` (only if phase is `idle` or `postFrameCallbacks`).
3. Next vsync builds and rasterizes; presentation happens one or two vsyncs later depending on
   pipeline depth.

With resampling **on**, step 2 is deferred: events are enqueued and dispatched from
`_handleSampleTimeChanged`, which is driven either by the periodic 16.667 ms timer or by the
post-frame callback (`gestures/binding.dart:180-192`). Since the post-frame callback runs in
`SchedulerPhase.postFrameCallbacks`, `ensureVisualUpdate` *does* schedule the next frame
(`scheduler/binding.dart:908-911`) — so the sampled delta lands in frame N+1, and the position it
represents is 38 ms behind the frame's presentation time.

---

## 8. Concrete takeaways for a Uno/Skia implementation

Ordered by expected impact.

1. **Feed animations the presentation timestamp, not `DateTime.Now`.** Flutter's entire jank story
   rests on `frameTime = presentation time` (`platform_configuration.cc:469`) plus a monotonicity
   clamp (`:472-480`). Any tick that samples the wall clock at callback time will jitter by exactly
   the amount of work that ran before it.
2. **Make scroll offset changes paint-only wherever the content is not virtualized.**
   `single_child_scroll_view.dart:403,419,429` is the pattern: repaint boundary + offset listener →
   `markNeedsPaint`. Reserve layout-per-frame for virtualizing panels, and bound it with relayout
   boundaries and a cache ring (Flutter: 250 px, `viewport.dart:289`).
3. **Adopt an explicit frame-phase enum with asserts.** `SchedulerPhase`
   (`scheduler/binding.dart:160-199`) plus `setPixels`'s
   `assert(phase != persistentCallbacks)` (`scroll_position.dart:368-371`) plus the layout-time
   `correctBy` escape hatch (`viewport_offset.dart:188-200`) is a complete, battle-tested protocol
   for "who may mutate scroll offset when".
4. **Guarantee one animation tick per frame structurally**, using the callback-map swap
   (`scheduler/binding.dart:1260`) + single-outstanding-id ticker
   (`ticker.dart:253-302`), rather than by convention.
5. **Use a quadratic least-squares velocity fit with a 100 ms horizon, 20-sample ring, ≥3-sample
   minimum, and a 40 ms "pointer stopped" short-circuit** on Windows/Linux/Android
   (`velocity_tracker.dart:142-145, 181-244`). On macOS/iOS-flavoured targets, the weighted
   three-difference form with the documented weights (0.05/0.35/0.60 iOS, 0.20/0.65/0.15 macOS) is
   the parity target.
6. **Consider a resampler as an opt-in.** The full algorithm is ~200 lines
   (`resampler.dart`) and needs only: a per-device queue, an interpolation function, a
   `frameTime + offset` clock in the same domain as event timestamps, a periodic tick for when no
   frames are produced, and the early-release rule for up/removed events. The default offset is
   `-38 ms`; the interval is `16667 µs`.
7. **Add engine-level backpressure**: a bounded producer/consumer between "build the scene" and
   "raster the scene", so a slow GPU frame throttles the UI thread instead of queueing
   (`animator.cc:99-108`, `PipelineFull`).
8. **Retained layers with an explicit `_needsAddToScene` invariant** and `addRetained`
   (`layer.dart:696-716`) — the cheapest possible re-present when only one subtree changed.
9. **DPI-aware ballistic termination**: stop when the sim would move <1 physical pixel per 50 ms
   (`scroll_physics.dart:439-445`) rather than a fixed logical-pixel epsilon.
10. **Wheel: Flutter does not animate.** If Uno's target is WinUI parity, Flutter is not the model
    here; the citations above (`scroll_position_with_single_context.dart:210-236`) exist to prove
    that this is a deliberate design point rather than an omission.

---

## 9. Explicitly UNVERIFIED items

- **`-38.3 ms` sampling offset / a symbol named `kDefaultSamplingOffset`.** Not present anywhere in
  `D:/Work/flutter` at this revision. The only constant is
  `const Duration _defaultSamplingOffset = Duration(milliseconds: -38);`
  (`packages/flutter/lib/src/gestures/binding.dart:224`). It may have existed historically or in a
  different embedder; I did not verify git history.
- Whether any Flutter *embedder* or app template flips `resamplingEnabled = true` at startup — grep
  over `packages/` found only tests. I did not grep the whole engine/embedder tree for a Dart-side
  assignment.
- Web (CanvasKit/skwasm) frame scheduling: I did not read `lib/web_ui`, so nothing above should be
  assumed to hold for Flutter Web.
- Impeller's own raster-thread scheduling specifics beyond `Rasterizer::Draw` thread assertions.
