# Flutter Scroll Physics & Activity Model — Exact Math, Constants, and Threading

Research note for the Uno scroll-smoothness effort. **Every claim below is cited to a file
path + line number in the local Flutter checkout at `D:/Work/flutter`.** Where I could not
verify something in source it is explicitly marked `UNVERIFIED`.

Flutter version under study: the checkout at `D:/Work/flutter` (post-3.41 dev; `RenderViewportBase.cacheExtent`
is already deprecated in favour of `scrollCacheExtent`, see
`packages/flutter/lib/src/rendering/viewport.dart:550-575`).

All positions are **logical pixels**, all times are **seconds**, all velocities are
**logical pixels per second**, unless stated otherwise.

---

## 0. Executive summary — what actually makes it smooth

1. **The simulation is closed-form and stateless in time.** Every ballistic simulation
   (`FrictionSimulation`, `SpringSimulation`, `ClampingScrollSimulation`) is an analytic
   `x(t)`/`dx(t)` pair evaluated at the *frame timestamp*, not an incremental integrator
   stepped by `deltaTime`. Consequence: a dropped frame or a jittery frame interval
   produces the *correct* position, never accumulated drift, and never a velocity spike.
   (`packages/flutter/lib/src/physics/simulation.dart:36-60`,
   `packages/flutter/lib/src/animation/animation_controller.dart:941-955`.)
2. **One clock: the engine vsync callback.** `Ticker` registers a *transient* frame callback
   with `SchedulerBinding`; transient callbacks run at the very start of the frame, strictly
   *before* build/layout/paint (`packages/flutter/lib/src/scheduler/binding.dart:1226-1274`
   then `:1338-1376`). So the offset for frame N is computed from frame N's own vsync
   timestamp and consumed by the same frame's layout. No one-frame lag, no cross-thread
   handoff.
3. **Physics is required to be *ballistic* (memoryless).** `ScrollPhysics.createBallisticSimulation`
   may be re-invoked mid-fling every frame (e.g. when `maxScrollExtent` grows as a lazy list
   materialises). The doc comment explicitly requires that acceleration be a function of
   `(x, dx, params)` only, so restarting from the current velocity is visually a no-op
   (`packages/flutter/lib/src/widgets/scroll_physics.dart:386-406`).
4. **Overscroll is not a special case; it is a second simulation spliced in at a computed
   time.** `BouncingScrollSimulation` pre-computes `timeAtX(extent)` by Newton's method and
   hands the friction sim's instantaneous velocity to a spring at that instant, capped at
   5000 px/s — so the handoff is C1-ish and never snaps
   (`packages/flutter/lib/src/widgets/scroll_simulation.dart:52-76`).
5. **The whole thing degrades gracefully:** overscroll leftover from `setPixels` >
   `precisionErrorTolerance` (1e-10) terminates the activity into idle rather than fighting
   the boundary (`packages/flutter/lib/src/widgets/scroll_activity.dart:619-635`,
   `packages/flutter/lib/src/foundation/constants.dart:71`).

The thing Flutter *does not* do that a WinUI/Uno engineer might expect: it does **not** run
scrolling on a compositor thread, and it does **not** apply a pure paint-transform. Each
tick calls `setPixels` → `notifyListeners()` → `RenderViewportBase.markNeedsLayout` → a
real layout pass for the viewport and its slivers (see §5.4). It gets away with this because
sliver layout is O(visible children) and `RenderObject.layout` early-outs for children whose
constraints did not change (`packages/flutter/lib/src/rendering/object.dart:2847-2849`).

---

## 1. `BouncingScrollPhysics` — iOS

### 1.1 Where it is selected

`ScrollBehavior.getScrollPhysics` (`packages/flutter/lib/src/widgets/scroll_configuration.dart:243-257`):

```dart
static const ScrollPhysics _bouncingPhysics = BouncingScrollPhysics(
  parent: RangeMaintainingScrollPhysics(),
);                                                       // :227-229
static const ScrollPhysics _bouncingDesktopPhysics = BouncingScrollPhysics(
  decelerationRate: ScrollDecelerationRate.fast,
  parent: RangeMaintainingScrollPhysics(),
);                                                       // :230-233
static const ScrollPhysics _clampingPhysics = ClampingScrollPhysics(
  parent: RangeMaintainingScrollPhysics(),
);                                                       // :234-236
```

* iOS → `_bouncingPhysics`
* macOS → `_bouncingDesktopPhysics` (`ScrollDecelerationRate.fast`)
* android / fuchsia / **linux / windows** → `_clampingPhysics`

Note for Uno: **Flutter uses Android's clamping physics on Windows and Linux desktop.**
There is no separate "desktop" physics for Win32; the only desktop-specific variant is macOS'
`fast` deceleration rate.

### 1.2 The friction simulation and the 0.135 drag coefficient

`packages/flutter/lib/src/widgets/scroll_simulation.dart:50-57`:

```dart
// Taken from UIScrollView.decelerationRate (.normal = 0.998)
// 0.998^1000 = ~0.135
_frictionSimulation = FrictionSimulation(
  0.135,
  position,
  velocity,
  constantDeceleration: constantDeceleration,
);
```

So the drag coefficient **is** 0.135, and it is explicitly derived as UIKit's
`UIScrollView.decelerationRate.normal == 0.998` per *millisecond* raised to 1000 (i.e.
converted to a per-second base).

`FrictionSimulation` (`packages/flutter/lib/src/physics/friction_simulation.dart:35-165`):

```dart
FrictionSimulation(double drag, double position, double velocity, {
  super.tolerance, double constantDeceleration = 0,
}) : _drag = drag,
     _dragLog = math.log(drag),
     _x = position,
     _v = velocity,
     _constantDeceleration = constantDeceleration * velocity.sign {   // :40-50
  _finalTime = _newtonsMethod(
    initialGuess: 0, target: 0,
    f: dx,
    df: (double time) => (_v * math.pow(_drag, time) * _dragLog) - _constantDeceleration,
    iterations: 10,
  );                                                                   // :51-57
}

double x(double time) {
  if (time > _finalTime) return finalX;
  return _x + _v * math.pow(_drag, time) / _dragLog
            - _v / _dragLog
            - ((_constantDeceleration / 2) * time * time);             // :118-126
}

double dx(double time) {
  if (time > _finalTime) return 0;
  return _v * math.pow(_drag, time) - _constantDeceleration * time;    // :129-134
}

double get finalX {
  if (_constantDeceleration == 0) return _x - _v / _dragLog;
  return x(_finalTime);                                                // :137-142
}

bool isDone(double time) => dx(time).abs() < tolerance.velocity;       // :158-160
```

**Exact closed form (constantDeceleration = 0, the iOS-phone case):**

```
D      = 0.135
lnD    = ln(0.135) = -2.0024805
x(t)   = x0 + v0·D^t/lnD − v0/lnD          =  x0 + (v0/lnD)·(D^t − 1)
dx(t)  = v0·D^t
xFinal = x0 − v0/lnD                       =  x0 + 0.4993806·v0
```

So an iOS fling travels **≈ 0.4994 × v0 logical pixels** in total. At v0 = 5000 px/s that is
≈ 2497 px. Time to fall under the default velocity tolerance (20 px/s at dpr 1, see §4):
`t = ln(20/5000)/ln(0.135) ≈ 2.757 s`.

**`_finalTime` quirk worth knowing.** Newton's method on `v·D^t = 0` has no finite root; each
iteration adds exactly `−1/lnD`, so after 10 iterations
`_finalTime = −10/lnD = 10/2.00248 ≈ 4.9938 s` for D = 0.135. After that instant `x()` snaps
to `finalX` and `dx()` returns 0 exactly. In practice `isDone` fires far earlier (2.76 s
above), so this is a safety clamp, not a behaviour driver.
(`packages/flutter/lib/src/physics/friction_simulation.dart:15-27` for `_newtonsMethod`,
`:51-57` for the call.)

**Desktop (`ScrollDecelerationRate.fast`) adds a constant deceleration term of 1400 px/s²**
(`packages/flutter/lib/src/widgets/scroll_physics.dart:767-770`):

```dart
constantDeceleration: switch (decelerationRate) {
  ScrollDecelerationRate.fast => 1400,
  ScrollDecelerationRate.normal => 0,
},
```

with `_constantDeceleration = 1400 * velocity.sign` so it always opposes... actually it is
signed *with* the velocity and subtracted, giving `dx(t) = v0·D^t − 1400·sign(v0)·t`, i.e. an
extra linear brake. This makes `_finalTime` finite and genuinely reachable, which is why
`finalX` falls back to `x(_finalTime)` when `constantDeceleration != 0`
(`friction_simulation.dart:137-142`).

`FrictionSimulation.timeAtX` inverts `x(t)` numerically — 10 Newton iterations with
`f = x`, `df = dx` (`friction_simulation.dart:147-155`), returning `double.infinity` if the
target is unreachable.

### 1.3 Spring description (mass / stiffness / damping)

Base default (`packages/flutter/lib/src/widgets/scroll_physics.dart:411-418`):

```dart
static final SpringDescription _kDefaultSpring = SpringDescription.withDampingRatio(
  mass: 0.5, stiffness: 100.0, ratio: 1.1,
);
SpringDescription get spring => parent?.spring ?? _kDefaultSpring;
```

`BouncingScrollPhysics` override for the fast/desktop rate
(`packages/flutter/lib/src/widgets/scroll_physics.dart:812-820`):

```dart
SpringDescription get spring {
  switch (decelerationRate) {
    case ScrollDecelerationRate.fast:
      return SpringDescription.withDampingRatio(mass: 0.3, stiffness: 75.0, ratio: 1.3);
    case ScrollDecelerationRate.normal:
      return super.spring;
  }
}
```

`SpringDescription.withDampingRatio` converts ratio ζ to damping coefficient c
(`packages/flutter/lib/src/physics/spring_simulation.dart:40-44`):

```
c = ζ · 2 · sqrt(m · k)
```

Numerically:

| physics | m | k | ζ | c = ζ·2·√(mk) | c² − 4mk | type |
|---|---|---|---|---|---|---|
| default (iOS normal, and Android bounce-back) | 0.5 | 100.0 | 1.1 | **15.5563** | 242.0 − 200 = **+42.0** | overdamped |
| `fast` (macOS/desktop) | 0.3 | 75.0 | 1.3 | **12.3329** | 152.1 − 90 = **+62.1** | overdamped |

Both are **overdamped**, so the bounce-back never overshoots — no ringing, which is the
main visual "cheapness" tell in hand-rolled implementations.

Solution selection (`packages/flutter/lib/src/physics/spring_simulation.dart:285-301`):

```dart
return switch (spring.damping * spring.damping - 4 * spring.mass * spring.stiffness) {
  > 0.0 => _OverdampedSolution(...),
  < 0.0 => _UnderdampedSolution(...),
  _     => _CriticalSolution(...),
};
```

Closed forms — all three, verbatim math:

*Overdamped* (`:330-360`):
```
cmk = c² − 4mk
r1  = (−c − √cmk) / (2m)
r2  = (−c + √cmk) / (2m)
c2  = (v0 − r1·d) / (r2 − r1)      // d = start − end
c1  = d − c2
x(t)  = c1·e^{r1 t} + c2·e^{r2 t}          (relative to end)
dx(t) = c1·r1·e^{r1 t} + c2·r2·e^{r2 t}
```

*Critically damped* (`:303-328`):
```
r  = −c / (2m)
c1 = d
c2 = v0 − r·d
x(t)  = (c1 + c2·t)·e^{r t}
dx(t) = r·(c1 + c2·t)·e^{r t} + c2·e^{r t}
```

*Underdamped* (`:362-397`):
```
w  = √(4mk − c²) / (2m)
r  = −c / (2m)
c1 = d
c2 = (v0 − r·d) / w
x(t)  = e^{r t}·(c1·cos(w t) + c2·sin(w t))
dx(t) = e^{r t}·(c2·w·cos(w t) − c1·w·sin(w t)) + r·e^{r t}·(c2·sin(w t) + c1·cos(w t))
```

`SpringSimulation.x/dx` add `_endPosition` back (`:240-256`); `isDone` is
`nearZero(solution.x(t), tol.distance) && nearZero(solution.dx(t), tol.velocity)` (`:258-262`).

**`ScrollSpringSimulation`** is the subclass scrolling actually uses; its only difference is
that it *snaps exactly to the end value once done*, which removes the sub-pixel residue that
would otherwise leave the list one hair off the boundary
(`packages/flutter/lib/src/physics/spring_simulation.dart:271-281`):

```dart
class ScrollSpringSimulation extends SpringSimulation {
  @override
  double x(double time) => isDone(time) ? _endPosition : super.x(time);
}
```

### 1.4 Overscroll resistance during drag — `frictionFactor` and `_applyFriction`

`packages/flutter/lib/src/widgets/scroll_physics.dart:704-751`:

```dart
double frictionFactor(double overscrollFraction) {
  return math.pow(1 - overscrollFraction, 2) *
      switch (decelerationRate) {
        ScrollDecelerationRate.fast   => 0.26,
        ScrollDecelerationRate.normal => 0.52,
      };
}

@override
double applyPhysicsToUserOffset(ScrollMetrics position, double offset) {
  if (!position.outOfRange) return offset;

  final double overscrollPastStart = math.max(position.minScrollExtent - position.pixels, 0.0);
  final double overscrollPastEnd   = math.max(position.pixels - position.maxScrollExtent, 0.0);
  final double overscrollPast      = math.max(overscrollPastStart, overscrollPastEnd);
  final bool easing = (overscrollPastStart > 0.0 && offset < 0.0)
                   || (overscrollPastEnd   > 0.0 && offset > 0.0);

  final double friction = easing
      ? frictionFactor((overscrollPast - offset.abs()) / position.viewportDimension)
      : frictionFactor( overscrollPast                 / position.viewportDimension);
  final double direction = offset.sign;

  if (easing && decelerationRate == ScrollDecelerationRate.fast) {
    return direction * offset.abs();          // no resistance when releasing on desktop
  }
  return direction * _applyFriction(overscrollPast, offset.abs(), friction);
}

static double _applyFriction(double extentOutside, double absDelta, double gamma) {
  assert(absDelta > 0);
  var total = 0.0;
  if (extentOutside > 0) {
    final double deltaToLimit = extentOutside / gamma;
    if (absDelta < deltaToLimit) return absDelta * gamma;
    total += extentOutside;
    absDelta -= deltaToLimit;
  }
  return total + absDelta;
}
```

Reading of the math:

* `overscrollFraction = overscrollPast / viewportDimension`. Friction **starts at 0.52**
  (normal) / **0.26** (fast) at zero overscroll and **decays quadratically** to 0 when you have
  dragged a full viewport past the edge — i.e. resistance → infinite at 1 viewport of overscroll.
* Sign convention detail: `easing` means the drag is *reducing* overscroll; then the fraction
  is computed at the *post-drag* overscroll (`overscrollPast − |offset|`), giving *less*
  resistance on the way back. The comment says exactly this
  (`scroll_physics.dart:727-728`: "Apply less resistance when easing the overscroll vs tensioning").
* `_applyFriction` is piecewise: while still outside, each pixel of finger travel produces
  `gamma` pixels of content travel; once the remaining delta would carry you back inside the
  bounds, the excess is applied 1:1.
* On desktop (`fast`), easing back is applied with **zero** resistance (`:733-735`).

`BouncingScrollPhysics.applyBoundaryConditions` returns **0.0** unconditionally
(`scroll_physics.dart:753-754`) — nothing is clamped; overscroll is genuinely allowed to
accumulate in `pixels`.

### 1.5 `carriedMomentum` — the exact repeat-fling law

`packages/flutter/lib/src/widgets/scroll_physics.dart:782-799`:

```dart
// Methodology:
// 1- Use https://github.com/flutter/platform_tests/tree/master/scroll_overlay to test with
//    Flutter and platform scroll views superimposed.
// 3- If the scrollables stopped overlapping at any moment, adjust the desired
//    output value of this function at that input speed.
// 4- Feed new input/output set into a power curve fitter. Change function and repeat from 2.
// 5- Repeat from 2 with medium and slow flings.
@override
double carriedMomentum(double existingVelocity) {
  return existingVelocity.sign *
      math.min(0.000816 * math.pow(existingVelocity.abs(), 1.967).toDouble(), 40000.0);
}
```

**Exact formula:**

```
carried(v) = sign(v) · min( 0.000816 · |v|^1.967 , 40000 )
```

Base class returns `0.0` (`scroll_physics.dart:480-482`) — i.e. **non-iOS platforms carry no
momentum at all.**

Saturation point: `0.000816·|v|^1.967 = 40000` → `|v| ≈ (4.902e7)^(1/1.967) ≈ 7.2e3`… precisely,
`ln|v| = (ln 40000 − ln 0.000816)/1.967 = (10.5966 + 7.1112)/1.967 = 9.0027` → `|v| ≈ 8113 px/s`.
Since `maxFlingVelocity` for the normal rate is 8000 (§3), the cap is essentially never hit
on phone-rate physics but *is* relevant for the fast rate whose cap is 64000.

Sample values: v=1000 → carried ≈ 0.000816·1000^1.967 = 0.000816·e^{13.588} = 0.000816·797_000 ≈ 650 px/s.
v=3000 → 0.000816·e^{15.747} ≈ 0.000816·6.9e6 ≈ 5640 px/s. v=5000 → ≈ 15_400 px/s. So the
second fling in a series can more than triple the launch speed — that is the "iOS ratchets up
when you flick repeatedly" feel.

### 1.6 Where carried momentum is *consumed* — `ScrollDragController`

`packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart:263-276` creates the
drag with `carriedVelocity: physics.carriedMomentum(_heldPreviousVelocity)`; `_heldPreviousVelocity`
is captured in `hold()` from the *outgoing* activity's velocity (`:252-259`).

`ScrollDragController` constants (`packages/flutter/lib/src/widgets/scroll_activity.dart:299-319`):

```dart
static const Duration momentumRetainStationaryDurationThreshold = Duration(milliseconds: 20);
static const double  momentumRetainVelocityThresholdFactor      = 0.5;
static const Duration motionStoppedDurationThreshold            = Duration(milliseconds: 50);
static const double  _bigThresholdBreakDistance                 = 24.0;
```

Momentum is **lost** if the pointer sits still (offset == 0) for > 20 ms, or if the drag update
has no timestamp at all (`:334-342`):

```dart
void _maybeLoseMomentum(double offset, Duration? timestamp) {
  if (_retainMomentum && offset == 0.0 &&
      (timestamp == null ||
       timestamp - _lastNonStationaryTimestamp! > momentumRetainStationaryDurationThreshold)) {
    _retainMomentum = false;
  }
}
```

Momentum is **applied** only at drag end, and only if the new fling agrees in direction and is
not much weaker (`scroll_activity.dart:418-442`):

```dart
void end(DragEndDetails details) {
  double velocity = -details.primaryVelocity!;
  if (_reversed) velocity = -velocity;
  if (_retainMomentum) {
    final isFlingingInSameDirection = velocity.sign == carriedVelocity!.sign;
    final bool isVelocityNotSubstantiallyLessThanCarriedMomentum =
        velocity.abs() > carriedVelocity!.abs() * momentumRetainVelocityThresholdFactor;
    if (isFlingingInSameDirection && isVelocityNotSubstantiallyLessThanCarriedMomentum) {
      velocity += carriedVelocity!;      // simple addition
    }
  }
  delegate.goBallistic(velocity);
}
```

### 1.7 `dragStartDistanceMotionThreshold` (3.5 px) and the anti-jump ease-in

`BouncingScrollPhysics` (`scroll_physics.dart:801-804`):

```dart
// Eyeballed from observation to counter the effect of an unintended scroll
// from the natural motion of lifting the finger after a scroll.
@override
double get dragStartDistanceMotionThreshold => 3.5;
```

It is plumbed to `ScrollDragController.motionStartDistanceThreshold`
(`scroll_position_with_single_context.dart:270`) and consumed in
`_adjustForScrollStartThreshold` (`scroll_activity.dart:350-394`):

* If the pointer is stationary and `> 50 ms` (`motionStoppedDurationThreshold`) has elapsed
  since the last non-stationary sample, a **new** threshold is armed (`_offsetSinceLastStop = 0`).
* While armed, deltas accumulate but produce **zero** scroll until `|accumulated| > 3.5`.
* When the threshold breaks:
  * if this single delta is `> 24.0` px (`_bigThresholdBreakDistance`), it is passed through
    unchanged (deliberate fling);
  * otherwise the emitted offset is `min(threshold/3.0, |offset|) · sign(offset)` — i.e. capped
    at **1.1667 px** for the first frame, explicitly to "Ease into the motion when the threshold
    is initially broken to avoid a visible jump" (`scroll_activity.dart:379-388`).

This is the mechanism that kills the classic "content twitches 3 px when you put your finger
down to stop a fling" artifact. Android/`ClampingScrollPhysics` leaves
`dragStartDistanceMotionThreshold` null → `_offsetSinceLastStop == null` → offsets pass through
transparently (`scroll_activity.dart:366-369`).

### 1.8 `BouncingScrollSimulation` — composition of friction + spring

Full constructor (`packages/flutter/lib/src/widgets/scroll_simulation.dart:34-77`):

```dart
BouncingScrollSimulation({
  required double position, required double velocity,
  required this.leadingExtent, required this.trailingExtent, required this.spring,
  double constantDeceleration = 0, super.tolerance,
}) : assert(leadingExtent <= trailingExtent) {
  if (position < leadingExtent) {
    _springSimulation = _underscrollSimulation(position, velocity);
    _springTime = double.negativeInfinity;                 // spring active immediately
  } else if (position > trailingExtent) {
    _springSimulation = _overscrollSimulation(position, velocity);
    _springTime = double.negativeInfinity;
  } else {
    _frictionSimulation = FrictionSimulation(0.135, position, velocity,
                                             constantDeceleration: constantDeceleration);
    final double finalX = _frictionSimulation.finalX;
    if (velocity > 0.0 && finalX > trailingExtent) {
      _springTime = _frictionSimulation.timeAtX(trailingExtent);
      _springSimulation = _overscrollSimulation(
        trailingExtent,
        math.min(_frictionSimulation.dx(_springTime), maxSpringTransferVelocity),
      );
    } else if (velocity < 0.0 && finalX < leadingExtent) {
      _springTime = _frictionSimulation.timeAtX(leadingExtent);
      _springSimulation = _underscrollSimulation(
        leadingExtent,
        math.min(_frictionSimulation.dx(_springTime), maxSpringTransferVelocity),
      );
    } else {
      _springTime = double.infinity;                       // never springs
    }
  }
}

static const double maxSpringTransferVelocity = 5000.0;    // :81
```

Time multiplexing (`:107-126`):

```dart
Simulation _simulation(double time) {
  final Simulation simulation;
  if (time > _springTime) {
    _timeOffset = _springTime.isFinite ? _springTime : 0.0;
    simulation = _springSimulation;
  } else {
    _timeOffset = 0.0;
    simulation = _frictionSimulation;
  }
  return simulation..tolerance = tolerance;
}
double x(double time)     => _simulation(time).x(time - _timeOffset);
double dx(double time)    => _simulation(time).dx(time - _timeOffset);
bool isDone(double time)  => _simulation(time).isDone(time - _timeOffset);
```

Notes that matter for a port:
* `maxSpringTransferVelocity = 5000` is applied with `math.min`, **not** `clamp(abs)`. For an
  underscroll (negative velocity) `min(negative, 5000) == negative`, so the cap is effectively
  one-sided (only limits positive/trailing-edge transfers). This looks asymmetric; it is what
  the source says. Reproduce it literally if you want pixel-identical behaviour.
* `_timeOffset` is mutable state written inside `x()`/`dx()`, so this particular `Simulation`
  is *not* purely functional — it must be queried with monotonically non-decreasing times, as
  `Simulation`'s own doc requires (`packages/flutter/lib/src/physics/simulation.dart:23-31`).

### 1.9 Entry point

`packages/flutter/lib/src/widgets/scroll_physics.dart:756-774`:

```dart
Simulation? createBallisticSimulation(ScrollMetrics position, double velocity) {
  final Tolerance tolerance = toleranceFor(position);
  if (velocity.abs() >= tolerance.velocity || position.outOfRange) {
    return BouncingScrollSimulation(
      spring: spring, position: position.pixels, velocity: velocity,
      leadingExtent: position.minScrollExtent, trailingExtent: position.maxScrollExtent,
      tolerance: tolerance,
      constantDeceleration: switch (decelerationRate) {
        ScrollDecelerationRate.fast => 1400,
        ScrollDecelerationRate.normal => 0,
      },
    );
  }
  return null;     // → IdleScrollActivity
}
```

---

## 2. `ClampingScrollPhysics` / `ClampingScrollSimulation` — Android

### 2.1 Provenance and the ballistic re-derivation

Header comment (`packages/flutter/lib/src/widgets/scroll_simulation.dart:134-163`):

> For any value of [velocity], this travels the same total distance as the Android scroll physics.
>
> This scroll physics has been adjusted relative to Android's in order to make it ballistic,
> meaning that the deceleration at any moment is a function only of the current velocity [dx]
> and does not depend on how long ago the simulation was started. …
> Compared to this scroll physics, Android's moves faster at the very beginning, then slower,
> and it ends at the same place but a little later.
>
> This class is based on OverScroller.java from Android:
> https://android.googlesource.com/platform/frameworks/base/+/android-13.0.0_r24/core/java/android/widget/OverScroller.java#738
> and in particular class SplineOverScroller (at the end of the file), starting at method "fling".
> (A very similar algorithm is in Scroller.java in the same directory, but OverScroller is what's
> used by RecyclerView.)
>
> In the Android implementation, times are in milliseconds, positions are in physical pixels,
> but velocity is in physical pixels per whole second.

**There are no SPLINE lookup tables in Flutter's port.** Android's `SplineOverScroller` uses
`SPLINE_POSITION[]`/`SPLINE_TIME[]` 101-entry tables to shape the fling curve; Flutter replaces
the whole thing with the analytic power law in §2.3. That is the single biggest simplification
and it is deliberate (to satisfy the ballistic/memoryless invariant of
`ScrollPhysics.createBallisticSimulation`, `scroll_physics.dart:386-406`).

### 2.2 Constants

`packages/flutter/lib/src/widgets/scroll_simulation.dart:164-215`:

```dart
ClampingScrollSimulation({
  required this.position, required this.velocity,
  this.friction = 0.015, super.tolerance,
}) {
  _duration = _flingDuration();
  _distance = _flingDistance();
}

// See DECELERATION_RATE.
static final double _kDecelerationRate = math.log(0.78) / math.log(0.9);

// See INFLEXION.
static const double _kInflexion = 0.35;

// See mPhysicalCoeff.  This has a value of 0.84 times Earth gravity,
// expressed in units of logical pixels per second^2.
static const double _physicalCoeff =
    9.80665   // g, in meters per second^2
  * 39.37     // 1 meter / 1 inch
  * 160.0     // 1 inch / 1 logical pixel
  * 0.84;     // "look and feel tuning"
```

Numeric values (computed from the source expressions):

| symbol | expression | value |
|---|---|---|
| `_kDecelerationRate` | `ln(0.78)/ln(0.9)` | **2.3582017** |
| `_kDecelerationRate − 1` | | 1.3582017 |
| `1/(_kDecelerationRate − 1)` | | 0.7362675 |
| `_kDecelerationRate · _kInflexion` | | 0.8253706 |
| `_kInflexion` | `INFLEXION` | **0.35** |
| `_physicalCoeff` | `9.80665 · 39.37 · 160 · 0.84` | **51890.2017** px/s² |
| `friction` | `mFlingFriction` | **0.015** |
| `referenceVelocity` | `friction · _physicalCoeff / _kInflexion` | **2223.8658** px/s |

Note the unit trick: `39.37 inches/metre × 160 dp/inch` — Flutter treats a logical pixel as
1/160 inch (the Android *mdpi* definition), and folds `ppi` out entirely. Android's real
`mPhysicalCoeff` is `g · 39.37 · ppi · 0.84`; Flutter substitutes 160 so the result is in
*logical* px. **When porting to Uno you must decide whether your "logical pixel" is 1/160 in
(Android dp) or 1/96 in (WinUI DIP).** If DIPs, the correct substitution is `96.0` in place of
`160.0`, giving `_physicalCoeff = 9.80665·39.37·96·0.84 = 31134.12` px/s² — otherwise Android
flings will travel 1.667× too far in DIP space. This is an inference from the units, not stated
in the Flutter source: marked as **derived, not quoted**.

### 2.3 `flingDuration` and `flingDistance` derivation

`packages/flutter/lib/src/widgets/scroll_simulation.dart:217-248`:

```dart
// See getSplineFlingDuration().
double _flingDuration() {
  // See getSplineDeceleration().  That function's value is
  // math.log(velocity.abs() / referenceVelocity).
  final double referenceVelocity = friction * _physicalCoeff / _kInflexion;

  // This is the value getSplineFlingDuration() would return, but in seconds.
  final androidDuration =
      math.pow(velocity.abs() / referenceVelocity, 1 / (_kDecelerationRate - 1.0)) as double;

  // We finish a bit sooner than Android, in order to travel the same total distance.
  return _kDecelerationRate * _kInflexion * androidDuration;
}

// See getSplineFlingDistance().  This returns the same value but with the
// sign of [velocity], and in logical pixels.
double _flingDistance() {
  final double distance = velocity * _duration / _kDecelerationRate;
  assert(() {
    // This is the more complicated calculation that getSplineFlingDistance()
    // actually performs, which boils down to the much simpler formula above.
    final double referenceVelocity = friction * _physicalCoeff / _kInflexion;
    final double logVelocity = math.log(velocity.abs() / referenceVelocity);
    final double distanceAgain = friction * _physicalCoeff *
        math.exp(logVelocity * _kDecelerationRate / (_kDecelerationRate - 1.0));
    return (distance.abs() - distanceAgain).abs() < tolerance.distance;
  }());
  return distance;
}
```

**Exact formulas:**

```
vRef        = friction · physicalCoeff / INFLEXION                     (= 2223.8658 for defaults)
androidT    = (|v0| / vRef) ^ (1 / (DR − 1))                           (= ^0.7362675)
T           = DR · INFLEXION · androidT   = 0.8253706 · androidT       [seconds]
S           = v0 · T / DR                                              [signed logical px]
```

and equivalently (the assert's cross-check, i.e. Android's own formula):

```
|S| = friction · physicalCoeff · exp( ln(|v0|/vRef) · DR/(DR−1) )
    = friction · physicalCoeff · (|v0|/vRef) ^ 1.7362675
```

### 2.4 The fling curve

`packages/flutter/lib/src/widgets/scroll_simulation.dart:250-265`:

```dart
double x(double time) {
  final double t = clampDouble(time / _duration, 0.0, 1.0);
  return position + _distance * (1.0 - math.pow(1.0 - t, _kDecelerationRate));
}

double dx(double time) {
  final double t = clampDouble(time / _duration, 0.0, 1.0);
  return velocity * math.pow(1.0 - t, _kDecelerationRate - 1.0);
}

bool isDone(double time) => time >= _duration;
```

So with `u = 1 − clamp(t/T, 0, 1)`:

```
x(t)  = x0 + S·(1 − u^2.3582017)
dx(t) = v0 · u^1.3582017
```

Self-consistency check: `dx(0) = v0` ✔, `x(T) = x0 + S` ✔, and
`dS/dt|₀ = S·DR/T = (v0·T/DR)·DR/T = v0` ✔. And it is genuinely ballistic:
`dx = v0·u^{DR−1}` and `d(dx)/dt = −v0·(DR−1)/T·u^{DR−2}`; substituting `u = (dx/v0)^{1/(DR−1)}`
gives acceleration purely as a function of `dx` (since `v0/T` is itself a power of `v0`).
Concretely `a(dx) = −(DR−1)/(DR·INFLEXION) · vRef^{-1/(DR-1)+1}... ` — the point is only that the
`t` dependence cancels; **derived, not quoted**.

Worked example, v0 = 5000 px/s: `androidT = (5000/2223.8658)^0.7362675 = 1.8158 s`,
`T = 1.4988 s`, `S = 5000·1.4988/2.3582 = 3177.8 px`.
Compare iOS at the same launch speed: 2496.9 px over ~2.76 s. Android goes ~27 % further and
stops ~45 % sooner — this is the recognisable difference between the two platforms' feel.

### 2.5 Boundary conditions (hard clamp) and the bounce-back-into-range spring

`packages/flutter/lib/src/widgets/scroll_physics.dart:849-892`:

```dart
double applyBoundaryConditions(ScrollMetrics position, double value) {
  // assert: must never be called when value == position.pixels   (:851-874)
  if (value < position.pixels && position.pixels <= position.minScrollExtent) {
    return value - position.pixels;            // Underscroll — reject the whole delta.
  }
  if (position.maxScrollExtent <= position.pixels && position.pixels < value) {
    return value - position.pixels;            // Overscroll — reject the whole delta.
  }
  if (value < position.minScrollExtent && position.minScrollExtent < position.pixels) {
    return value - position.minScrollExtent;   // Hit top edge — reject only the excess.
  }
  if (position.pixels < position.maxScrollExtent && position.maxScrollExtent < value) {
    return value - position.maxScrollExtent;   // Hit bottom edge — reject only the excess.
  }
  return 0.0;
}
```

and the simulation factory (`:894-928`):

```dart
Simulation? createBallisticSimulation(ScrollMetrics position, double velocity) {
  final Tolerance tolerance = toleranceFor(position);
  if (position.outOfRange) {
    double? end;
    if (position.pixels > position.maxScrollExtent) end = position.maxScrollExtent;
    if (position.pixels < position.minScrollExtent) end = position.minScrollExtent;
    return ScrollSpringSimulation(spring, position.pixels, end!,
                                  math.min(0.0, velocity), tolerance: tolerance);
  }
  if (velocity.abs() < tolerance.velocity) return null;
  if (velocity > 0.0 && position.pixels >= position.maxScrollExtent) return null;
  if (velocity < 0.0 && position.pixels <= position.minScrollExtent) return null;
  return ClampingScrollSimulation(position: position.pixels, velocity: velocity,
                                  tolerance: tolerance);
}
```

Two things worth flagging:

* Even on Android, an out-of-range position (which can only happen via a dimension change,
  `jumpTo`, or a `correctPixels` race) is resolved by the **same default overdamped spring**
  (m 0.5 / k 100 / ζ 1.1), not by a hard snap. So even the "clamping" platform has a spring path.
* `math.min(0.0, velocity)` looks like a latent bug (it forces the initial spring velocity to be
  ≤ 0 regardless of which edge you are outside). It is what the code says; reproduce or fix
  deliberately.

Android's visual overscroll is not in the physics at all — it is `GlowingOverscrollIndicator`
or `StretchingOverscrollIndicator`, chosen by `ScrollBehavior.buildOverscrollIndicator`
(`scroll_configuration.dart:178-195`). Stretch constants live in
`packages/flutter/lib/src/widgets/overscroll_indicator.dart:869-928`
(`_stretchIntensity = 0.016`, `_exponentialScalar = e/0.33`, `_flingVelocityFriction = 1/6000`,
`kNaturalFrequency = 24.657`, `kDampingRatio = 0.98`, `kTimeCorrectionFactor = 0.8`).

---

## 3. `minFlingVelocity` / `maxFlingVelocity` / `minFlingDistance` / `dragStartDistanceMotionThreshold`

### 3.1 Base values

`packages/flutter/lib/src/gestures/constants.dart`:

```dart
const double kTouchSlop        = 18.0;    // :65  logical pixels
const double kPanSlop          = kTouchSlop * 2.0;   // :76
const double kMinFlingVelocity = 50.0;    // :90  logical pixels / second
const double kMaxFlingVelocity = 8000.0;  // :95  logical pixels / second
const double kPrecisePointerHitSlop = 1.0;// :103 (mouse/trackpad)
const double kPrecisePointerPanSlop = 2.0;// :106
```

`ScrollPhysics` defaults (`packages/flutter/lib/src/widgets/scroll_physics.dart`):

```dart
double get minFlingDistance => parent?.minFlingDistance ?? kTouchSlop;      // :457  → 18.0
double get minFlingVelocity => parent?.minFlingVelocity ?? kMinFlingVelocity;//:469  → 50.0
double get maxFlingVelocity => parent?.maxFlingVelocity ?? kMaxFlingVelocity;//:472  → 8000.0
double? get dragStartDistanceMotionThreshold => parent?.dragStartDistanceMotionThreshold; // :488 → null
```

`BouncingScrollPhysics` overrides:

```dart
// The ballistic simulation here decelerates more slowly than the one for
// ClampingScrollPhysics so we require a more deliberate input gesture
// to trigger a fling.
double get minFlingVelocity => kMinFlingVelocity * 2.0;                    // :776-780  → 100.0
double get dragStartDistanceMotionThreshold => 3.5;                        // :801-804
double get maxFlingVelocity => switch (decelerationRate) {                 // :806-810
  ScrollDecelerationRate.fast   => kMaxFlingVelocity * 8.0,                //   → 64000.0
  ScrollDecelerationRate.normal => super.maxFlingVelocity,                 //   → 8000.0
};
```

Summary table:

| platform physics | minFlingDistance | minFlingVelocity | maxFlingVelocity | dragStartDistanceMotionThreshold |
|---|---|---|---|---|
| `ClampingScrollPhysics` (Android/Win/Linux) | 18.0 | 50.0 | 8000.0 | null (no threshold) |
| `BouncingScrollPhysics` normal (iOS) | 18.0 | **100.0** | 8000.0 | **3.5** |
| `BouncingScrollPhysics` fast (macOS) | 18.0 | **100.0** | **64000.0** | **3.5** |

### 3.2 Where they are wired

`ScrollableState.setCanDrag` copies them onto the drag recognizer every time drag-ability
changes (`packages/flutter/lib/src/widgets/scrollable.dart:788-834`, mirrored for the
two-dimensional case at `:2395-2410`):

```dart
instance
  ..onDown = _handleDragDown
  ..onStart = _handleDragStart
  ..onUpdate = _handleDragUpdate
  ..onEnd = _handleDragEnd
  ..onCancel = _handleDragCancel
  ..minFlingDistance = _physics?.minFlingDistance
  ..minFlingVelocity = _physics?.minFlingVelocity
  ..maxFlingVelocity = _physics?.maxFlingVelocity
  ..velocityTrackerBuilder = _configuration.velocityTrackerBuilder(context)
  ..dragStartBehavior = widget.dragStartBehavior
  ..multitouchDragStrategy = _configuration.getMultitouchDragStrategy(context)
  ..gestureSettings = _mediaQueryGestureSettings
  ..supportedDevices = _configuration.dragDevices;
```

Consumption in the recognizer (`packages/flutter/lib/src/gestures/monodrag.dart:941-962`
for vertical; `:1005-1026` horizontal; `:1062-1077` pan):

```dart
bool isFlingGesture(VelocityEstimate estimate, PointerDeviceKind kind) {
  final double minVelocity = minFlingVelocity ?? kMinFlingVelocity;
  final double minDistance = minFlingDistance ?? computeHitSlop(kind, gestureSettings);
  return estimate.pixelsPerSecond.dy.abs() > minVelocity
      && estimate.offset.dy.abs()          > minDistance;
}

DragEndDetails? considerFling(VelocityEstimate estimate, PointerDeviceKind kind) {
  if (!isFlingGesture(estimate, kind)) return null;          // → velocity 0 → goBallistic(0)
  final double maxVelocity = maxFlingVelocity ?? kMaxFlingVelocity;
  final double dy = clampDouble(estimate.pixelsPerSecond.dy, -maxVelocity, maxVelocity);
  return DragEndDetails(velocity: Velocity(pixelsPerSecond: Offset(0, dy)), primaryVelocity: dy, ...);
}
```

So: **both** a velocity *and* a distance gate must be passed for the gesture to count as a
fling; otherwise the drag ends with zero velocity and `goBallistic(0.0)` immediately settles
(iOS: possibly a bounce-back spring; Android: idle).

`dragStartDistanceMotionThreshold` is consumed only through `ScrollDragController` (§1.7).

### 3.3 Velocity estimation — the other half of "feel"

The default tracker is a **least-squares quadratic fit over ≤20 samples in a 100 ms horizon**
(`packages/flutter/lib/src/gestures/velocity_tracker.dart:142-256`):

```dart
static const int _assumePointerMoveStoppedMilliseconds = 40;   // :142
static const int _historySize                          = 20;   // :143
static const int _horizonMilliseconds                  = 100;  // :144
static const int _minSampleSize                        = 3;    // :145
```

* If > 40 ms have elapsed since the last sample, velocity is declared **exactly zero**
  (`:180-188`) — this is what prevents a "stale" fling when the finger rests before lifting.
* Samples older than 100 ms, or separated by a > 40 ms gap, break the accumulation loop
  (`:213-219`).
* Requires ≥3 samples, then `LeastSquaresSolver(time, x, w).solve(2)` (degree-2 polynomial);
  velocity = `coefficients[1] * 1000` (ms→s) (`:232-245`).

iOS/macOS use a **weighted average of the last three inter-sample velocities** instead
(`velocity_tracker.dart:295-398` / `:400-460`), which is much closer to UIKit:

```dart
// IOSScrollViewFlingVelocityTracker.getVelocityEstimate  :366-369
final Offset estimatedVelocity =
    _previousVelocityAt(-2) * 0.6  +
    _previousVelocityAt(-1) * 0.35 +
    _previousVelocityAt(0)  * 0.05;

// MacOSScrollViewFlingVelocityTracker.getVelocityEstimate  :436-439
final Offset estimatedVelocity =
    _previousVelocityAt(-2) * 0.15 +
    _previousVelocityAt(-1) * 0.65 +
    _previousVelocityAt(0)  * 0.2;
```

`_previousVelocityAt(i)` is the finite difference between two adjacent samples
(`:328-345`); `_sampleSize = 20` (`:303`) only so that `VelocityEstimate.offset` spans enough
history to clear `minFlingDistance`. Selection is in
`ScrollBehavior.velocityTrackerBuilder` (`scroll_configuration.dart:213-225`): iOS →
`IOSScrollViewFlingVelocityTracker`, macOS → `MacOSScrollViewFlingVelocityTracker`,
everything else → plain `VelocityTracker`.

**This is a major, cheap smoothness lever:** the iOS weighting deliberately *discounts the most
recent sample to 5 %*, because the last sample before lift-off is contaminated by the finger
rolling/decelerating. A naive "last two points" estimator produces exactly the "fling died
even though I flicked hard" complaint.

---

## 4. `Tolerance` — and how a simulation is declared done

`packages/flutter/lib/src/physics/tolerance.dart:9-49`:

```dart
class Tolerance {
  const Tolerance({
    this.distance = _epsilonDefault,
    this.time     = _epsilonDefault,
    this.velocity = _epsilonDefault,
  });
  static const double _epsilonDefault = 1e-3;
  static const Tolerance defaultTolerance = Tolerance();
  final double distance;   // same units as x
  final double time;       // same units as t
  final double velocity;   // same units as dx
}
```

Scrolling overrides two of the three, scaled by device pixel ratio
(`packages/flutter/lib/src/widgets/scroll_physics.dart:438-445`):

```dart
Tolerance toleranceFor(ScrollMetrics metrics) {
  return parent?.toleranceFor(metrics) ?? Tolerance(
    velocity: 1.0 / (0.050 * metrics.devicePixelRatio), // logical pixels per second
    distance: 1.0 / metrics.devicePixelRatio,           // logical pixels
  );
}
```

`time` is **not** overridden and stays at 1e-3 s.

| devicePixelRatio | velocity tolerance | distance tolerance |
|---|---|---|
| 1.0 | 20.0 px/s | 1.0 px |
| 1.5 | 13.33 px/s | 0.667 px |
| 2.0 | 10.0 px/s | 0.5 px |
| 3.0 | 6.67 px/s | 0.333 px |

Interpretation: the velocity tolerance is "less than one *physical* pixel of movement per
50 ms" — i.e. sub-perceptible on that display. `devicePixelRatio` comes from
`ScrollPosition.devicePixelRatio → context.devicePixelRatio`
(`scroll_position.dart:347-348`), which `ScrollableState` reads from
`MediaQuery.maybeDevicePixelRatioOf(context) ?? View.of(context).devicePixelRatio`
(`scrollable.dart:598-600`, `:670-672`).

**`isDone` per simulation:**

| simulation | `isDone(t)` | citation |
|---|---|---|
| `FrictionSimulation` | `dx(t).abs() < tolerance.velocity` | `friction_simulation.dart:158-160` |
| `BoundedFrictionSimulation` | `super.isDone(t) \|\| \|x(t)−minX\| < tol.distance \|\| \|x(t)−maxX\| < tol.distance` | `friction_simulation.dart:192-196` |
| `SpringSimulation` | `nearZero(sol.x(t), tol.distance) && nearZero(sol.dx(t), tol.velocity)` | `spring_simulation.dart:258-262` |
| `ClampingScrollSimulation` | `time >= _duration` (pure time cut-off) | `scroll_simulation.dart:262-265` |
| `BouncingScrollSimulation` | delegates to whichever sub-simulation is active, with `_timeOffset` subtracted | `scroll_simulation.dart:125-126` |
| `ClampedSimulation` | delegates unchanged (clamping does **not** affect doneness) | `clamped_simulation.dart:64-65` |

`nearEqual`/`nearZero` (`packages/flutter/lib/src/physics/utils.dart:10-21`):

```dart
bool nearEqual(double? a, double? b, double epsilon) {
  if (a == null || b == null) return a == b;
  return (a > (b - epsilon)) && (a < (b + epsilon)) || a == b;
}
bool nearZero(double a, double epsilon) => nearEqual(a, 0.0, epsilon);
```

The check is consumed once per frame in `AnimationController._tick`
(`packages/flutter/lib/src/animation/animation_controller.dart:941-955`) — see §5.3.

---

## 5. The `ScrollActivity` state machine

### 5.1 The delegate contract

`packages/flutter/lib/src/widgets/scroll_activity.dart:35-59`:

```dart
abstract class ScrollActivityDelegate {
  AxisDirection get axisDirection;
  double setPixels(double pixels);        // returns overscroll
  void applyUserOffset(double delta);
  void goIdle();
  void goBallistic(double velocity);
}
```

`ScrollPositionWithSingleContext` implements it
(`packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart:46`).

### 5.2 The activities

| activity | `isScrolling` | `shouldIgnorePointer` | `velocity` | source |
|---|---|---|---|---|
| `IdleScrollActivity` | false | false | 0.0 | `scroll_activity.dart:181-198` |
| `HoldScrollActivity` | false | false | 0.0 | `:221-248` |
| `DragScrollActivity` | true | `_controller?._kind != PointerDeviceKind.trackpad` | 0.0 | `:476-558` (esp. `:538`, `:545-546`) |
| `BallisticScrollActivity` | true | ctor arg (`position.shouldIgnorePointer` at start) | `_controller.velocity` | `:584-678` |
| `DrivenScrollActivity` | true | **true** always | `_controller.velocity` | `:695-810` |

Transitions (all via `ScrollPosition.beginActivity`, `scroll_position.dart:1011-1036`):

```
                            ┌──────────────────────────── goBallistic(v) ◄── DrivenScrollActivity._end
                            │                                              (animateTo finished)
   pointer down             ▼
Idle ──hold()──► Hold ──drag()──► Drag ──end(v)──► goBallistic(v) ──► Ballistic ──┐
  ▲                │                │                                   │        │
  │                │cancel()        │cancel() → goBallistic(0)           │        │
  │                └────────────────┴──► goBallistic(0)                  │        │
  │                                                                      │        │
  └── goIdle() ◄── applyMoveTo() returned false (overscroll leftover) ◄──┘        │
  └── goIdle() ◄── createBallisticSimulation() returned null ◄────────────────────┘
  └── Ballistic._end() → goBallistic(0.0)  (simulation completed → maybe spring, else idle)
Idle ──applyNewDimensions()──► goBallistic(0.0)      // scroll_activity.dart:185-188
Ballistic ──applyNewDimensions()/resetActivity()──► goBallistic(velocity)  // :609-617
animateTo()/ScrollAction ──► DrivenScrollActivity    // scroll_position_with_single_context.dart:176-194
```

Key handlers:

```dart
// ScrollPositionWithSingleContext
void goIdle() { beginActivity(IdleScrollActivity(this)); }                       // :134-137

void goBallistic(double velocity) {
  final Simulation? simulation = physics.createBallisticSimulation(this, velocity);
  if (simulation != null) {
    beginActivity(BallisticScrollActivity(this, simulation, context.vsync, shouldIgnorePointer));
  } else {
    goIdle();
  }
}                                                                                 // :148-157

void applyUserOffset(double delta) {
  updateUserScrollDirection(delta > 0.0 ? ScrollDirection.forward : ScrollDirection.reverse);
  setPixels(pixels - physics.applyPhysicsToUserOffset(this, delta));
}                                                                                 // :128-132
```

Note `pixels - applyPhysicsToUserOffset(...)`: drag deltas are *subtracted*, because the finger
moving down means the scroll offset decreases; `ScrollDragController.update` already negated
once for the reversed-axis case (`scroll_activity.dart:412-415`, `:418-426`).

`beginActivity` also drives the "is scrolling" side effects
(`scroll_position.dart:1011-1036`): disposes the old activity, fires
`didEndScroll()` (which persists the offset via `saveOffset`/`saveScrollOffset`,
`:1050-1059`), updates `context.setIgnorePointer`, sets `isScrollingNotifier`, and fires
`didStartScroll()`.

### 5.3 Who ticks it, and on which thread

**Chain: `BallisticScrollActivity` → `AnimationController.unbounded` → `Ticker` →
`SchedulerBinding.scheduleFrameCallback` (transient) → engine vsync. All on the single Dart
UI isolate/thread. There is no compositor-thread scrolling in the framework.**

```dart
// scroll_activity.dart:590-605
BallisticScrollActivity(super.delegate, Simulation simulation, TickerProvider vsync,
                        this.shouldIgnorePointer) {
  _controller = AnimationController.unbounded(
      debugLabel: kDebugMode ? objectRuntimeType(this, 'BallisticScrollActivity') : null,
      vsync: vsync,
    )
    ..addListener(_tick)
    ..animateWith(simulation).whenComplete(_end);
}
```

`vsync` is `ScrollableState` itself (`scrollable.dart:560-562` `with TickerProviderStateMixin`,
`:595-596` `TickerProvider get vsync => this;`). That means the ticker is automatically
**muted when the subtree's `TickerMode` is disabled** (off-screen route, etc.) — free power
saving, and the animation resumes from the correct clock because `Ticker._startTime` is
preserved (`ticker.dart:106-129`, `:271-284`).

`AnimationController.unbounded` sets `lowerBound = -infinity`, `upperBound = +infinity`, and
crucially `animationBehavior = AnimationBehavior.preserve`
(`animation_controller.dart:277-291`), so the accessibility "disable animations" flag does
**not** shrink fling durations (`:645-653`, and see the doc at `:56-58`:
"the `AnimationController` which controls the physics simulation for a scrollable list will
have `AnimationBehavior.preserve`, so that when a user attempts to scroll it does not jump to
the end/beginning too quickly").

`_startSimulation` (`animation_controller.dart:861-872`):

```dart
TickerFuture _startSimulation(Simulation simulation) {
  assert(!isAnimating);
  _simulation = simulation;
  _lastElapsedDuration = Duration.zero;
  _value = clampDouble(simulation.x(0.0), lowerBound, upperBound);
  final TickerFuture result = _ticker!.start();
  ...
}
```

The per-frame tick (`animation_controller.dart:941-955`):

```dart
void _tick(Duration elapsed) {
  _lastElapsedDuration = elapsed;
  final double elapsedInSeconds = elapsed.inMicroseconds.toDouble() / Duration.microsecondsPerSecond;
  assert(elapsedInSeconds >= 0.0);
  _value = clampDouble(_simulation!.x(elapsedInSeconds), lowerBound, upperBound);
  if (_simulation!.isDone(elapsedInSeconds)) {
    _status = ...;
    stop(canceled: false);          // resolves the TickerFuture → _end() → goBallistic(0.0)
  }
  notifyListeners();                // → BallisticScrollActivity._tick
  _checkStatusChanged();
}
```

`velocity` is likewise read straight off the simulation, never differenced
(`animation_controller.dart:401-408`):

```dart
double get velocity {
  if (!isAnimating) return 0.0;
  return _simulation!.dx(lastElapsedDuration!.inMicroseconds.toDouble()
                         / Duration.microsecondsPerSecond);
}
```

`Ticker` (`packages/flutter/lib/src/scheduler/ticker.dart`):

```dart
void _tick(Duration timeStamp) {
  assert(isTicking);
  assert(scheduled);
  _animationId = null;
  _startTime ??= timeStamp;
  _onTick(timeStamp - _startTime!);          // elapsed since first tick
  if (shouldScheduleTick) scheduleTick(rescheduling: true);
}                                                             // :271-284

void scheduleTick({bool rescheduling = false}) {
  if (forceFrames) SchedulerBinding.instance.scheduleForcedFrame();
  else             SchedulerBinding.instance.scheduleFrame();
  _animationId = SchedulerBinding.instance.scheduleFrameCallback(
    _tick, rescheduling: rescheduling, scheduleNewFrame: false);
}                                                             // :290-303
```

Two smoothness-relevant details:

* `_startTime` is taken from **`SchedulerBinding.currentFrameTimeStamp`** when `start()` is
  called inside a frame (`ticker.dart:202-205`), otherwise from the first tick's timestamp.
  Elapsed time is therefore anchored to vsync timestamps, not `DateTime.now()`.
* The ticker re-schedules itself *from within* the tick, so there is exactly one frame callback
  outstanding at any moment; no timer coalescing, no drift.

Frame phase ordering (`packages/flutter/lib/src/scheduler/binding.dart`):

```dart
void handleBeginFrame(Duration? rawTimeStamp) {          // :1226
  _currentFrameTimeStamp = _adjustForEpoch(rawTimeStamp ?? _lastRawTimeStamp);
  ...
  _schedulerPhase = SchedulerPhase.transientCallbacks;   // :1258  ← Tickers run here
  callbacks.forEach(... _invokeFrameCallback(...));
  _schedulerPhase = SchedulerPhase.midFrameMicrotasks;   // :1272
}

void handleDrawFrame() {                                  // :1338
  _schedulerPhase = SchedulerPhase.persistentCallbacks;   // :1343  ← build / layout / paint
  for (final callback in _persistentCallbacks) _invokeFrameCallback(callback, ...);
  _schedulerPhase = SchedulerPhase.postFrameCallbacks;    // :1349
  ...
  _schedulerPhase = SchedulerPhase.idle;                  // :1365
}
```

`ScrollPosition.setPixels` asserts it is **not** called during `persistentCallbacks`
(`scroll_position.dart:367-371`):

```dart
assert(
  SchedulerBinding.instance.schedulerPhase != SchedulerPhase.persistentCallbacks,
  "A scrollable's position should not change during the build, layout, and paint phases, "
  "otherwise the rendering will be confused.",
);
```

That assert *is* the architectural rule: **the scroll offset for frame N must be finalised
before frame N's layout begins.** No mid-layout mutation, therefore no torn frame, no
double-layout, no "position changed after we measured" jitter.

### 5.4 How the per-frame offset is applied — layout, not just paint offset

`BallisticScrollActivity._tick` / `DrivenScrollActivity._tick`
(`scroll_activity.dart:619-635` and `:750-766`):

```dart
void _tick() {
  if (!applyMoveTo(_controller.value)) {
    delegate.goIdle();
  }
}

@protected
bool applyMoveTo(double value) {
  return delegate.setPixels(value).abs() < precisionErrorTolerance;
}
```

`precisionErrorTolerance = 1e-10` (`packages/flutter/lib/src/foundation/constants.dart:71`).
So the very first frame in which the simulation asks for a position the physics refuses to
grant (Android hitting an edge) terminates the fling into `Idle`.

`ScrollPosition.setPixels` (`scroll_position.dart:366-401`):

```dart
double setPixels(double newPixels) {
  assert(hasPixels);
  assert(SchedulerBinding.instance.schedulerPhase != SchedulerPhase.persistentCallbacks, ...);
  if (newPixels != pixels) {
    final double overscroll = applyBoundaryConditions(newPixels);
    // debug assert: |overscroll| must not exceed |delta|      (:374-385)
    final double oldPixels = pixels;
    _pixels = newPixels - overscroll;
    if (_pixels != oldPixels) {
      if (outOfRange) context.setIgnorePointer(false);
      notifyListeners();                                   // ← the only "apply" step
      didUpdateScrollPositionBy(pixels - oldPixels);        // ScrollUpdateNotification
    }
    if (overscroll.abs() > precisionErrorTolerance) {
      didOverscrollBy(overscroll);                         // OverscrollNotification
      return overscroll;
    }
  }
  return 0.0;
}
```

`ScrollPosition` is a `ViewportOffset extends ChangeNotifier`
(`scroll_position.dart:189`, `packages/flutter/lib/src/rendering/viewport_offset.dart:100`).
The only listener that matters is installed by the viewport
(`packages/flutter/lib/src/rendering/viewport.dart:685-695`):

```dart
@override
void attach(PipelineOwner owner) {
  super.attach(owner);
  _offset.addListener(markNeedsLayout);
}

@override
void detach() {
  _offset.removeListener(markNeedsLayout);
  super.detach();
}
```

(and again on the `offset` setter, `:530-545`.)

**So: yes, every scroll frame triggers a real layout pass of the viewport.** Not a paint
transform, not a compositor offset. `RenderViewport.performLayout`
(`viewport.dart:1692-1765`) re-runs `_attemptLayout(mainAxisExtent, crossAxisExtent,
offset.pixels + centerOffsetAdjustment)`, which lays out slivers with a new
`SliverConstraints.scrollOffset`.

Why this is nevertheless smooth:

1. Sliver layout is O(children intersecting the viewport + cache extent), because
   `RenderSliverMultiBoxAdaptor` only materialises what the `SliverConstraints` demand.
2. `RenderObject.layout` short-circuits for any child whose constraints are unchanged and which
   is not dirty (`packages/flutter/lib/src/rendering/object.dart:2847-2849`):
   ```dart
   _isRelayoutBoundary = !parentUsesSize || sizedByParent || constraints.isTight || parent == null;
   if (!_needsLayout && constraints == _constraints) {
     ... return;     // no work
   }
   ```
   During a pure scroll, the *box* children receive identical `BoxConstraints` frame to frame,
   so their `performLayout` is never re-entered. Only the slivers themselves re-run, adjusting
   `layoutOffset`/`paintOffset`.
3. The actual pixel translation is a paint-time offset:
   `context.paintChild(child, offset + paintOffsetOf(child))`
   (`viewport.dart:995-1000`, `paintOffsetOf` at `:1875-1878`, computed in
   `updateChildLayoutOffset`/`computeAbsolutePaintOffset` at `:1866-1872`).

Practical takeaway for Uno: Flutter's model is "recompute realized-item set + paint offsets
every frame, on the UI thread, before layout" — it accepts a layout pass but keeps it bounded.
It does *not* try to decouple scroll from layout the way DirectManipulation/DComp does.

### 5.5 Mouse wheel and keyboard are **not** simulated

`ScrollPositionWithSingleContext.pointerScroll`
(`scroll_position_with_single_context.dart:209-236`):

```dart
void pointerScroll(double delta) {
  if (delta == 0.0) { goBallistic(0.0); return; }
  final double targetPixels =
      math.min(math.max(pixels + delta, minScrollExtent), maxScrollExtent);
  if (targetPixels != pixels) {
    goIdle();
    updateUserScrollDirection(-delta > 0.0 ? ScrollDirection.forward : ScrollDirection.reverse);
    final double oldPixels = pixels;
    isScrollingNotifier.value = true;
    forcePixels(targetPixels);       // instant, clamped, notifies listeners
    didStartScroll();
    didUpdateScrollPositionBy(pixels - oldPixels);
    didEndScroll();
    goBallistic(0.0);
  }
}
```

**Flutter applies wheel deltas instantly and clamped — no animation, no inertia, no easing.**
Smoothness for wheel input therefore comes entirely from the OS/engine delivering
high-frequency deltas (trackpads, precision wheels), not from the framework. `PointerScrollInertiaCancelEvent`
maps to `position.pointerScroll(0)` → `goBallistic(0.0)` (`scrollable.dart:965-968`).

Hit-testing note: `_receivedPointerSignal` registers with `GestureBinding.instance.pointerSignalResolver`
only if the delta would actually move the position (`scrollable.dart:953-969`), and
`_handlePointerScroll` calls `scrollEvent.respond(allowPlatformDefault: false)` so the browser/
platform does not also scroll (`:971-982`).

Keyboard: `ScrollAction.invoke` uses a **100 ms `Curves.easeInOut` `moveTo`**
(`packages/flutter/lib/src/widgets/scrollable_helpers.dart:503-507`), with increments of
50.0 px for a "line" and `0.8 * viewportDimension` for a "page" (`:435-438`).

### 5.6 Ignore-pointer during activities

`ScrollPosition.shouldIgnorePointer` (`scroll_position.dart:291`):

```dart
bool get shouldIgnorePointer => !outOfRange && (activity?.shouldIgnorePointer ?? true);
```

pushed to `ScrollableState.setIgnorePointer` (`scrollable.dart:843-855`) which flips
`RenderIgnorePointer.ignoring` directly on the render object — no rebuild. Also
`setPixels` proactively re-enables hit-testing the moment the position leaves range
(`scroll_position.dart:389-391`), so you can grab an overscrolled iOS list mid-bounce.

---

## 6. `setPixels` / `applyContentDimensions` / `correctPixels` — jank avoidance

### 6.1 The layout-time correction loop

`RenderViewport.performLayout` (`viewport.dart:1721-1740`):

```dart
double correction;
var count = 0;
do {
  correction = _attemptLayout(mainAxisExtent, crossAxisExtent,
                              offset.pixels + centerOffsetAdjustment);
  if (correction != 0.0) {
    offset.correctBy(correction);            // silent: no notifyListeners
  } else {
    if (offset.applyContentDimensions(
          math.min(0.0, _minScrollExtent + mainAxisExtent * anchor),
          math.max(0.0, _maxScrollExtent - mainAxisExtent * (1.0 - anchor)))) {
      break;
    }
  }
  count += 1;
} while (count < maxLayoutCycles);            // _maxLayoutCyclesPerChild * childCount
```

`ViewportOffset.correctBy` on `ScrollPosition` (`scroll_position.dart:457-465`):

```dart
@override
void correctBy(double correction) {
  assert(hasPixels, ...);
  _pixels = _pixels! + correction;
  _didChangeViewportDimensionOrReceiveCorrection = true;
}
```

and `correctPixels` (`:437-440`):

```dart
// ignore: use_setters_to_change_properties, (API is intended to discourage setting value)
void correctPixels(double value) {
  _pixels = value;
}
```

**The entire point of `correctPixels`/`correctBy` is that they mutate `_pixels` *without*
`notifyListeners()`.** The doc is explicit (`scroll_position.dart:403-436`):

> This is used to adjust the position while doing layout. In particular, this is typically
> called as a response to `applyViewportDimension` or `applyContentDimensions` (in both cases,
> if this method is called, those methods should then return false to indicate that the position
> has been adjusted). … It will not immediately cause the rendering to change, since it does not
> notify the widgets or render objects that might be listening to this object.

Because the correction happens *inside* `performLayout`'s do/while, the corrected offset is
consumed by the immediately-following `_attemptLayout` in the same frame. The user never sees
an intermediate position. If it notified, you would get: layout → notify → markNeedsLayout →
next frame layout again → visible one-frame jump. That is the jank this design eliminates.

### 6.2 `applyContentDimensions`

`scroll_position.dart:641-682`:

```dart
@override
bool applyContentDimensions(double minScrollExtent, double maxScrollExtent) {
  assert(haveDimensions == (_lastMetrics != null));
  if (!nearEqual(_minScrollExtent, minScrollExtent, Tolerance.defaultTolerance.distance) ||
      !nearEqual(_maxScrollExtent, maxScrollExtent, Tolerance.defaultTolerance.distance) ||
      _didChangeViewportDimensionOrReceiveCorrection ||
      _lastAxis != axis) {
    _minScrollExtent = minScrollExtent;
    _maxScrollExtent = maxScrollExtent;
    _lastAxis = axis;
    final ScrollMetrics? currentMetrics = haveDimensions ? copyWith() : null;
    _didChangeViewportDimensionOrReceiveCorrection = false;
    _pendingDimensions = true;
    if (haveDimensions && !correctForNewDimensions(_lastMetrics!, currentMetrics!)) {
      return false;                      // ← ask the viewport to re-layout
    }
    _haveDimensions = true;
  }
  if (_pendingDimensions) {
    applyNewDimensions();                // → activity.applyNewDimensions() → goBallistic(v)
    _pendingDimensions = false;
  }
  if (_isMetricsChanged()) {
    // It is too late to send useful notifications, because the potential
    // listeners have, by definition, already been built this frame. To make
    // sure the notification is sent at all, we delay it until after the frame
    // is complete.
    if (!_haveScheduledUpdateNotification) {
      scheduleMicrotask(didUpdateScrollMetrics);
      _haveScheduledUpdateNotification = true;
    }
    _lastMetrics = copyWith();
  }
  return true;
}
```

Anti-jank mechanisms visible here:

1. **Extent changes below `1e-3` px are ignored** (`nearEqual(..., Tolerance.defaultTolerance.distance)`)
   — sub-pixel churn from float accumulation does not restart the layout loop or the ballistic
   activity.
2. `applyNewDimensions()` is fired **at most once** per dimension change (`_pendingDimensions`
   latch), even though `applyContentDimensions` may be called several times in the correction
   loop.
3. `ScrollMetricsNotification` is deferred to a **microtask after the frame**, explicitly
   because listeners have already built. Dispatching mid-layout would either be dropped or force
   a re-build (`:670-680`, and the assert in `didUpdateScrollMetrics` at `:1081-1084` that the
   phase is not `persistentCallbacks`).

`correctForNewDimensions` (`scroll_position.dart:697-710`):

```dart
@protected
bool correctForNewDimensions(ScrollMetrics oldPosition, ScrollMetrics newPosition) {
  final double newPixels = physics.adjustPositionForNewDimensions(
    oldPosition: oldPosition, newPosition: newPosition,
    isScrolling: activity!.isScrolling, velocity: activity!.velocity,
  );
  if (newPixels != pixels) {
    correctPixels(newPixels);
    return false;
  }
  return true;
}
```

### 6.3 `RangeMaintainingScrollPhysics` — the "content changed under me" guard

Always in the chain on every platform (`scroll_configuration.dart:227-236`). Full logic at
`packages/flutter/lib/src/widgets/scroll_physics.dart:576-650`. Decision table:

```dart
var maintainOverscroll = true;
var enforceBoundary    = true;
if (velocity != 0.0) {                    // :585-590
  // Don't try to adjust an animating position, the jumping around would be distracting.
  maintainOverscroll = false;
  enforceBoundary    = false;
}
if (min/max extents unchanged)            maintainOverscroll = false;   // :591-595
if (oldPosition.pixels != newPosition.pixels) {                          // :596-613
  maintainOverscroll = false;
  if (all four extents finite)            enforceBoundary = false;
}
if (old position was out of range)        enforceBoundary = false;      // :614-619
if (maintainOverscroll) {                                                // :620-637
  if (was underscrolled && min extent grew)  return newMin - (oldMin - oldPixels);
  if (was overscrolled  && max extent shrank) return newMax + (oldPixels - oldMax);
}
double result = super.adjustPositionForNewDimensions(...);
if (enforceBoundary) result = clampDouble(result, newMin, newMax);       // :645-648
return result;
```

The single most important line for smoothness: **`if (velocity != 0.0)` disables both
adjustments.** A fling in progress is never corrected by a dimension change, so lazily-loaded
list content growing mid-fling cannot yank the position.

### 6.4 `forcePixels` and `_impliedVelocity`

`scroll_position.dart:489-498`:

```dart
@protected
void forcePixels(double value) {
  assert(hasPixels);
  _impliedVelocity = value - pixels;
  _pixels = value;
  notifyListeners();
  SchedulerBinding.instance.addPostFrameCallback((Duration timeStamp) {
    _impliedVelocity = 0;
  }, debugLabel: 'ScrollPosition.resetVelocity');
}
```

`_impliedVelocity` exists purely so that `recommendDeferredLoading` sees a large "velocity"
for a `jumpTo` that skips thousands of pixels (`scroll_position.dart:246-261`, `:1103-1110`),
letting image decodes be skipped for content that will never be seen. That is a real
smoothness lever: `ScrollPhysics.recommendDeferredLoading` defaults to
`velocity.abs() > View.of(context).physicalSize.longestSide`
(`scroll_physics.dart:266-272`) — i.e. "if you'd traverse more than a screen-diagonal per
second, don't decode".

---

## 7. What happens when a fling would leave the range

### 7.1 iOS / `BouncingScrollPhysics` — pre-computed friction→spring handoff

Already given in §1.8. Restated as an algorithm:

```
if position < leadingExtent          → spring(position → leadingExtent, v0), active from t = −∞
else if position > trailingExtent    → spring(position → trailingExtent, v0), active from t = −∞
else
  friction = Friction(0.135, position, v0, constDecel)
  if v0 > 0 and friction.finalX > trailingExtent:
      tS = friction.timeAtX(trailingExtent)                    // Newton, 10 iters
      spring = ScrollSpring(spring, trailingExtent, trailingExtent, min(friction.dx(tS), 5000))
      // NB: start == end == trailingExtent; the *velocity* carries it out and back
  elif v0 < 0 and friction.finalX < leadingExtent:
      tS = friction.timeAtX(leadingExtent)
      spring = ScrollSpring(spring, leadingExtent, leadingExtent, min(friction.dx(tS), 5000))
  else
      tS = +∞
for t <= tS: use friction at t
for t >  tS: use spring at (t − tS)
```

The spring's *start* and *end* are both the extent (`_overscrollSimulation(x, dx)` is
`ScrollSpringSimulation(spring, x, trailingExtent, dx)` with `x == trailingExtent`,
`scroll_simulation.dart:99-105` + `:61-64`), so `distance = start − end = 0` and all the motion
comes from the injected velocity. With the overdamped default solution and `d = 0`:
`c2 = v0/(r2−r1)`, `c1 = −c2`, giving `x(t) = c2·(e^{r2 t} − e^{r1 t})` — a single hump that
rises and decays with no overshoot past zero. Exactly the iOS rubber-band.

During overscroll the drag path is separately damped by `applyPhysicsToUserOffset` (§1.4), and
`applyBoundaryConditions` returns 0 so nothing is clipped
(`scroll_physics.dart:753-754`).

### 7.2 Android / `ClampingScrollPhysics` — the fling is *terminated*, not sprung

The `ClampingScrollSimulation` itself is unbounded — nothing in it knows about extents. The
boundary is enforced by the setPixels round-trip:

1. `BallisticScrollActivity._tick` → `applyMoveTo(value)` → `delegate.setPixels(value)`.
2. `setPixels` → `applyBoundaryConditions` → `ClampingScrollPhysics.applyBoundaryConditions`
   returns the excess (`scroll_physics.dart:849-892`).
3. `_pixels = newPixels − overscroll`, so the position lands exactly on the extent.
4. `setPixels` returns the overscroll; `applyMoveTo` sees `|overscroll| >= 1e-10` → returns
   false → `delegate.goIdle()` (`scroll_activity.dart:619-635`).
5. The `OverscrollNotification` emitted by `didOverscrollBy` (`scroll_position.dart:1064-1067`)
   is what `GlowingOverscrollIndicator`/`StretchingOverscrollIndicator` consume to draw the glow
   or stretch (`scroll_configuration.dart:178-195`).

Additionally `createBallisticSimulation` refuses to even start a fling that is already pinned
against the edge in that direction (`scroll_physics.dart:917-922`), which prevents a 1-frame
activity churn on every edge-directed flick.

If the position somehow *is* out of range (dimension change, `jumpTo`), Android springs back
with the default overdamped spring and `math.min(0.0, velocity)`
(`scroll_physics.dart:897-913`).

---

## 8. Self-contained C#-ready transcription

Straight ports of the verified formulas. Units: logical pixels, seconds. `Math.Pow`,
`Math.Log`, `Math.Exp` throughout. These are **not** copied from any C# source; they are
transcriptions of the Dart cited above.

### 8.1 Shared scaffolding

```csharp
public readonly record struct Tolerance(double Distance, double Time, double Velocity)
{
    public static readonly Tolerance Default = new(1e-3, 1e-3, 1e-3);

    // ScrollPhysics.toleranceFor — scroll_physics.dart:439-445
    public static Tolerance ForScroll(double devicePixelRatio) => new(
        Distance: 1.0 / devicePixelRatio,
        Time:     1e-3,
        Velocity: 1.0 / (0.050 * devicePixelRatio));
}

public interface ISimulation
{
    double X(double time);
    double Dx(double time);
    bool IsDone(double time);
    Tolerance Tolerance { get; set; }
}

internal static class Numerics
{
    // friction_simulation.dart:15-27
    public static double NewtonsMethod(double initialGuess, double target,
                                       Func<double, double> f, Func<double, double> df,
                                       int iterations)
    {
        var guess = initialGuess;
        for (var i = 0; i < iterations; i++)
        {
            guess -= (f(guess) - target) / df(guess);
        }
        return guess;
    }

    public static bool NearEqual(double a, double b, double epsilon)
        => (a > b - epsilon && a < b + epsilon) || a == b;

    public static bool NearZero(double a, double epsilon) => NearEqual(a, 0.0, epsilon);

    public static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);
}
```

### 8.2 iOS: `FrictionSimulation` + `SpringSimulation` + `BouncingScrollSimulation`

```csharp
// friction_simulation.dart:35-165
public sealed class FrictionSimulation : ISimulation
{
    private readonly double _drag, _dragLog, _x, _v, _constantDeceleration;
    private readonly double _finalTime;

    public Tolerance Tolerance { get; set; } = Tolerance.Default;

    public FrictionSimulation(double drag, double position, double velocity,
                              double constantDeceleration = 0.0,
                              Tolerance? tolerance = null)
    {
        _drag = drag;
        _dragLog = Math.Log(drag);
        _x = position;
        _v = velocity;
        _constantDeceleration = constantDeceleration * Math.Sign(velocity);
        if (tolerance.HasValue) Tolerance = tolerance.Value;

        // NOTE: the Dart ctor runs Newton BEFORE _finalTime is assigned, i.e. Dx/X see
        // _finalTime == +infinity during the solve. Reproduce that by solving against the
        // unclamped forms.
        _finalTime = Numerics.NewtonsMethod(
            initialGuess: 0.0,
            target: 0.0,
            f:  t => _v * Math.Pow(_drag, t) - _constantDeceleration * t,
            df: t => _v * Math.Pow(_drag, t) * _dragLog - _constantDeceleration,
            iterations: 10);
    }

    public double X(double time)
    {
        if (time > _finalTime) return FinalX;
        return _x
             + _v * Math.Pow(_drag, time) / _dragLog
             - _v / _dragLog
             - (_constantDeceleration / 2.0) * time * time;
    }

    public double Dx(double time)
    {
        if (time > _finalTime) return 0.0;
        return _v * Math.Pow(_drag, time) - _constantDeceleration * time;
    }

    public double FinalX => _constantDeceleration == 0.0
        ? _x - _v / _dragLog
        : XUnclamped(_finalTime);

    private double XUnclamped(double time)
        => _x + _v * Math.Pow(_drag, time) / _dragLog - _v / _dragLog
             - (_constantDeceleration / 2.0) * time * time;

    // friction_simulation.dart:147-155
    public double TimeAtX(double x)
    {
        if (x == _x) return 0.0;
        if (_v == 0.0 || (_v > 0 ? (x < _x || x > FinalX) : (x > _x || x < FinalX)))
            return double.PositiveInfinity;
        return Numerics.NewtonsMethod(0.0, x, X, Dx, 10);
    }

    public bool IsDone(double time) => Math.Abs(Dx(time)) < Tolerance.Velocity;
}
```

```csharp
// spring_simulation.dart:23-158, 285-397
public readonly record struct SpringDescription(double Mass, double Stiffness, double Damping)
{
    public static SpringDescription WithDampingRatio(double mass, double stiffness, double ratio = 1.0)
        => new(mass, stiffness, ratio * 2.0 * Math.Sqrt(mass * stiffness));

    // scroll_physics.dart:411-415  — iOS normal + Android bounce-back default
    public static readonly SpringDescription ScrollDefault =
        WithDampingRatio(mass: 0.5, stiffness: 100.0, ratio: 1.1);   // damping ≈ 15.5563

    // scroll_physics.dart:812-820  — macOS / ScrollDecelerationRate.fast
    public static readonly SpringDescription ScrollFast =
        WithDampingRatio(mass: 0.3, stiffness: 75.0, ratio: 1.3);    // damping ≈ 12.3329
}

public class SpringSimulation : ISimulation
{
    private readonly double _endPosition;
    private readonly Func<double, double> _solX, _solDx;

    public Tolerance Tolerance { get; set; } = Tolerance.Default;

    public SpringSimulation(SpringDescription s, double start, double end, double velocity,
                            Tolerance? tolerance = null)
    {
        _endPosition = end;
        if (tolerance.HasValue) Tolerance = tolerance.Value;

        double d = start - end;                       // initial displacement
        double v = velocity;
        double cmk = s.Damping * s.Damping - 4.0 * s.Mass * s.Stiffness;

        if (cmk > 0.0)                                 // overdamped
        {
            double r1 = (-s.Damping - Math.Sqrt(cmk)) / (2.0 * s.Mass);
            double r2 = (-s.Damping + Math.Sqrt(cmk)) / (2.0 * s.Mass);
            double c2 = (v - r1 * d) / (r2 - r1);
            double c1 = d - c2;
            _solX  = t => c1 * Math.Exp(r1 * t) + c2 * Math.Exp(r2 * t);
            _solDx = t => c1 * r1 * Math.Exp(r1 * t) + c2 * r2 * Math.Exp(r2 * t);
        }
        else if (cmk < 0.0)                            // underdamped
        {
            double w = Math.Sqrt(4.0 * s.Mass * s.Stiffness - s.Damping * s.Damping) / (2.0 * s.Mass);
            double r = -(s.Damping / 2.0 / s.Mass);
            double c1 = d;
            double c2 = (v - r * d) / w;
            _solX  = t => Math.Exp(r * t) * (c1 * Math.Cos(w * t) + c2 * Math.Sin(w * t));
            _solDx = t =>
            {
                double p = Math.Exp(r * t), cos = Math.Cos(w * t), sin = Math.Sin(w * t);
                return p * (c2 * w * cos - c1 * w * sin) + r * p * (c2 * sin + c1 * cos);
            };
        }
        else                                            // critically damped
        {
            double r = -s.Damping / (2.0 * s.Mass);
            double c1 = d;
            double c2 = v - r * d;
            _solX  = t => (c1 + c2 * t) * Math.Exp(r * t);
            _solDx = t =>
            {
                double p = Math.Exp(r * t);
                return r * (c1 + c2 * t) * p + c2 * p;
            };
        }
    }

    protected double EndPosition => _endPosition;

    public virtual double X(double time) => _endPosition + _solX(time);
    public virtual double Dx(double time) => _solDx(time);

    public bool IsDone(double time)
        => Numerics.NearZero(_solX(time), Tolerance.Distance)
        && Numerics.NearZero(_solDx(time), Tolerance.Velocity);
}

// spring_simulation.dart:271-281 — snaps exactly to the end value once done
public sealed class ScrollSpringSimulation : SpringSimulation
{
    public ScrollSpringSimulation(SpringDescription s, double start, double end, double velocity,
                                  Tolerance? tolerance = null)
        : base(s, start, end, velocity, tolerance) { }

    public override double X(double time) => IsDone(time) ? EndPosition : base.X(time);
}
```

```csharp
// scroll_simulation.dart:18-132
public sealed class BouncingScrollSimulation : ISimulation
{
    public const double MaxSpringTransferVelocity = 5000.0;
    private const double IosDrag = 0.135;             // UIScrollView.decelerationRate .normal^1000

    private readonly double _leading, _trailing;
    private readonly FrictionSimulation? _friction;
    private readonly ISimulation _spring;
    private readonly double _springTime;
    private double _timeOffset;                        // MUTABLE — query with non-decreasing t
    private Tolerance _tolerance = Tolerance.Default;

    public Tolerance Tolerance
    {
        get => _tolerance;
        set { _tolerance = value; if (_friction is not null) _friction.Tolerance = value; _spring.Tolerance = value; }
    }

    public BouncingScrollSimulation(double position, double velocity,
                                    double leadingExtent, double trailingExtent,
                                    SpringDescription spring,
                                    double constantDeceleration = 0.0,
                                    Tolerance? tolerance = null)
    {
        _leading = leadingExtent;
        _trailing = trailingExtent;
        if (tolerance.HasValue) _tolerance = tolerance.Value;

        if (position < leadingExtent)
        {
            _spring = new ScrollSpringSimulation(spring, position, leadingExtent, velocity, _tolerance);
            _springTime = double.NegativeInfinity;
        }
        else if (position > trailingExtent)
        {
            _spring = new ScrollSpringSimulation(spring, position, trailingExtent, velocity, _tolerance);
            _springTime = double.NegativeInfinity;
        }
        else
        {
            _friction = new FrictionSimulation(IosDrag, position, velocity, constantDeceleration, _tolerance);
            double finalX = _friction.FinalX;
            if (velocity > 0.0 && finalX > trailingExtent)
            {
                _springTime = _friction.TimeAtX(trailingExtent);
                _spring = new ScrollSpringSimulation(spring, trailingExtent, trailingExtent,
                    Math.Min(_friction.Dx(_springTime), MaxSpringTransferVelocity), _tolerance);
            }
            else if (velocity < 0.0 && finalX < leadingExtent)
            {
                _springTime = _friction.TimeAtX(leadingExtent);
                _spring = new ScrollSpringSimulation(spring, leadingExtent, leadingExtent,
                    Math.Min(_friction.Dx(_springTime), MaxSpringTransferVelocity), _tolerance);
            }
            else
            {
                _springTime = double.PositiveInfinity;
                _spring = new ScrollSpringSimulation(spring, position, position, 0.0, _tolerance);
            }
        }
    }

    private ISimulation Select(double time)
    {
        if (time > _springTime)
        {
            _timeOffset = double.IsFinite(_springTime) ? _springTime : 0.0;
            return _spring;
        }
        _timeOffset = 0.0;
        return _friction!;
    }

    public double X(double time)  => Select(time).X(time - _timeOffset);
    public double Dx(double time) => Select(time).Dx(time - _timeOffset);
    public bool IsDone(double time) => Select(time).IsDone(time - _timeOffset);
}
```

Drag-time overscroll resistance (`scroll_physics.dart:704-751`):

```csharp
public enum ScrollDecelerationRate { Normal, Fast }

public static double FrictionFactor(double overscrollFraction, ScrollDecelerationRate rate)
    => Math.Pow(1.0 - overscrollFraction, 2) * (rate == ScrollDecelerationRate.Fast ? 0.26 : 0.52);

private static double ApplyFriction(double extentOutside, double absDelta, double gamma)
{
    double total = 0.0;
    if (extentOutside > 0.0)
    {
        double deltaToLimit = extentOutside / gamma;
        if (absDelta < deltaToLimit) return absDelta * gamma;
        total += extentOutside;
        absDelta -= deltaToLimit;
    }
    return total + absDelta;
}

public static double ApplyPhysicsToUserOffset(
    double pixels, double minExtent, double maxExtent, double viewportDimension,
    double offset, ScrollDecelerationRate rate)
{
    bool outOfRange = pixels < minExtent || pixels > maxExtent;
    if (!outOfRange) return offset;

    double pastStart = Math.Max(minExtent - pixels, 0.0);
    double pastEnd   = Math.Max(pixels - maxExtent, 0.0);
    double past      = Math.Max(pastStart, pastEnd);
    bool easing = (pastStart > 0.0 && offset < 0.0) || (pastEnd > 0.0 && offset > 0.0);

    double friction = easing
        ? FrictionFactor((past - Math.Abs(offset)) / viewportDimension, rate)
        : FrictionFactor(past / viewportDimension, rate);
    double direction = Math.Sign(offset);

    if (easing && rate == ScrollDecelerationRate.Fast) return direction * Math.Abs(offset);
    return direction * ApplyFriction(past, Math.Abs(offset), friction);
}

// scroll_physics.dart:795-799
public static double CarriedMomentum(double existingVelocity)
    => Math.Sign(existingVelocity)
     * Math.Min(0.000816 * Math.Pow(Math.Abs(existingVelocity), 1.967), 40000.0);
```

### 8.3 Android: `ClampingScrollSimulation`

```csharp
// scroll_simulation.dart:164-266
public sealed class ClampingScrollSimulation : ISimulation
{
    // DECELERATION_RATE = ln(0.78)/ln(0.9) ≈ 2.3582017
    private static readonly double DecelerationRate = Math.Log(0.78) / Math.Log(0.9);

    // INFLEXION
    private const double Inflexion = 0.35;

    // mPhysicalCoeff = g(m/s²) · 39.37(in/m) · 160(logicalPx/in) · 0.84(tuning)
    //   = 51890.2017 logical px/s²  (Android dp; substitute 96.0 for WinUI DIPs → 31134.12)
    private const double PhysicalCoeff = 9.80665 * 39.37 * 160.0 * 0.84;

    private readonly double _position, _velocity, _friction, _duration, _distance;

    public Tolerance Tolerance { get; set; } = Tolerance.Default;

    public ClampingScrollSimulation(double position, double velocity,
                                    double friction = 0.015, Tolerance? tolerance = null)
    {
        _position = position;
        _velocity = velocity;
        _friction = friction;
        if (tolerance.HasValue) Tolerance = tolerance.Value;

        _duration = FlingDuration();
        _distance = _velocity * _duration / DecelerationRate;
    }

    private double FlingDuration()
    {
        double referenceVelocity = _friction * PhysicalCoeff / Inflexion;      // ≈ 2223.8658
        double androidDuration = Math.Pow(Math.Abs(_velocity) / referenceVelocity,
                                          1.0 / (DecelerationRate - 1.0));      // ^0.7362675
        return DecelerationRate * Inflexion * androidDuration;                  // ×0.8253706
    }

    public double Duration => _duration;
    public double Distance => _distance;

    public double X(double time)
    {
        double t = Numerics.Clamp(time / _duration, 0.0, 1.0);
        return _position + _distance * (1.0 - Math.Pow(1.0 - t, DecelerationRate));
    }

    public double Dx(double time)
    {
        double t = Numerics.Clamp(time / _duration, 0.0, 1.0);
        return _velocity * Math.Pow(1.0 - t, DecelerationRate - 1.0);
    }

    public bool IsDone(double time) => time >= _duration;
}
```

Boundary conditions (`scroll_physics.dart:849-892`):

```csharp
public static double ClampingApplyBoundaryConditions(
    double pixels, double minExtent, double maxExtent, double value)
{
    if (value < pixels && pixels <= minExtent) return value - pixels;      // underscroll
    if (maxExtent <= pixels && pixels < value) return value - pixels;      // overscroll
    if (value < minExtent && minExtent < pixels) return value - minExtent; // hit top edge
    if (pixels < maxExtent && maxExtent < value) return value - maxExtent; // hit bottom edge
    return 0.0;
}
```

Simulation factories (`scroll_physics.dart:756-774` and `:894-928`):

```csharp
public static ISimulation? CreateBouncingBallistic(
    double pixels, double minExtent, double maxExtent, double velocity,
    double devicePixelRatio, ScrollDecelerationRate rate)
{
    var tol = Tolerance.ForScroll(devicePixelRatio);
    bool outOfRange = pixels < minExtent || pixels > maxExtent;
    if (Math.Abs(velocity) >= tol.Velocity || outOfRange)
    {
        return new BouncingScrollSimulation(
            position: pixels, velocity: velocity,
            leadingExtent: minExtent, trailingExtent: maxExtent,
            spring: rate == ScrollDecelerationRate.Fast
                ? SpringDescription.ScrollFast : SpringDescription.ScrollDefault,
            constantDeceleration: rate == ScrollDecelerationRate.Fast ? 1400.0 : 0.0,
            tolerance: tol);
    }
    return null;
}

public static ISimulation? CreateClampingBallistic(
    double pixels, double minExtent, double maxExtent, double velocity, double devicePixelRatio)
{
    var tol = Tolerance.ForScroll(devicePixelRatio);
    if (pixels > maxExtent || pixels < minExtent)
    {
        double end = pixels > maxExtent ? maxExtent : minExtent;
        return new ScrollSpringSimulation(SpringDescription.ScrollDefault, pixels, end,
                                          Math.Min(0.0, velocity), tol);   // sic: Math.Min
    }
    if (Math.Abs(velocity) < tol.Velocity) return null;
    if (velocity > 0.0 && pixels >= maxExtent) return null;
    if (velocity < 0.0 && pixels <= minExtent) return null;
    return new ClampingScrollSimulation(pixels, velocity, tolerance: tol);
}
```

### 8.4 The tick loop, transcribed

```csharp
// BallisticScrollActivity + AnimationController._tick + Ticker._tick, collapsed.
// Call OnFrame from your compositor/UI vsync callback, BEFORE layout for that frame.
public sealed class BallisticScroll
{
    private readonly ISimulation _sim;
    private TimeSpan? _startTimestamp;
    public bool Finished { get; private set; }
    public double Velocity { get; private set; }

    public BallisticScroll(ISimulation sim) => _sim = sim;

    /// <param name="frameTimestamp">The vsync timestamp of the frame being produced.</param>
    /// <param name="setPixels">Returns the *unapplied* overscroll, like ScrollPosition.setPixels.</param>
    public void OnFrame(TimeSpan frameTimestamp, Func<double, double> setPixels)
    {
        _startTimestamp ??= frameTimestamp;
        double t = (frameTimestamp - _startTimestamp.Value).TotalSeconds;

        double value = _sim.X(t);
        Velocity = _sim.Dx(t);

        if (_sim.IsDone(t)) Finished = true;                 // controller stops, then _end()

        double overscroll = setPixels(value);
        if (Math.Abs(overscroll) >= 1e-10) Finished = true;  // precisionErrorTolerance → goIdle()
    }
}
```

Ordering rules to preserve (all from §5.3/§5.4):

* `OnFrame` must run in the *animation* phase, strictly before the layout/measure pass of the
  same frame. Never mutate the scroll offset during measure/arrange.
* Elapsed time must be `frameTimestamp − firstFrameTimestamp`, both from the same vsync clock,
  never `Stopwatch`/wall clock and never accumulated `dt`.
* On completion, call the equivalent of `goBallistic(0.0)` — that is what lets iOS transition
  from a completed friction phase into a bounce-back spring, and what settles Android at the
  edge (`scroll_activity.dart:637-643`).

---

## 9. Direct implications for Uno / WinUI-style engines

These are my inferences, flagged as such; the *facts* they rest on are cited above.

1. **Adopt the "closed-form simulation evaluated at vsync timestamp" model.** The single
   biggest robustness win in Flutter's design is that `x(t)` never integrates. Any
   `position += velocity * dt; velocity *= friction^dt` loop will drift and will spike after a
   dropped frame. (Facts: §0.1, §5.3.)
2. **Enforce the "no offset mutation during layout" invariant with an assert.** Flutter's
   assert at `scroll_position.dart:367-371` is what keeps the pipeline single-pass.
3. **Separate `SetPixels` (notifies, boundary-checked) from `CorrectPixels` (silent, layout-time).**
   Without the silent variant, every content-dimension correction costs a visible frame. (§6.1.)
4. **Terminate ballistic activity on the first frame the boundary rejects motion**
   (`|overscroll| ≥ 1e-10`) rather than letting the simulation grind against the edge. (§7.2.)
5. **Do not correct a position that has non-zero velocity.** `RangeMaintainingScrollPhysics`'s
   `if (velocity != 0.0) { maintainOverscroll = false; enforceBoundary = false; }` is the
   single cheapest fix for "list jumps when items load during a fling". (§6.3.)
6. **Velocity estimation matters as much as the fling curve.** The 40 ms staleness cut-off, the
   100 ms horizon, and the iOS `0.6/0.35/0.05` weighting are all things you can adopt
   independently of the rest. (§3.3.)
7. **Mind the logical-pixel definition when porting `_physicalCoeff`.** Flutter bakes 160 dp/in;
   WinUI DIPs are 96/in. (§2.2 — derived.)
8. **Wheel input is instant and clamped in Flutter.** If Uno wants smooth-wheel, that is a
   deliberate divergence, not a Flutter-parity feature. (§5.5.)

---

## 10. Explicitly UNVERIFIED / out of scope

* **Android's actual `SplineOverScroller` tables** — I did not read `OverScroller.java`; only
  Flutter's comment describing it (`scroll_simulation.dart:153-163`). The claim "Flutter has no
  spline tables" is verified (there are none in the file); the claim about what Android does is
  quoted from Flutter's comment, not independently checked.
* **UIKit's real `UIScrollView` deceleration** — only Flutter's comment
  (`scroll_simulation.dart:50-51`) is evidence; not verified against Apple sources.
* **Engine-side (C++) frame scheduling, raster thread behaviour, `Animator`/`VsyncWaiter`** — not
  read. Everything in §5.3 is framework-side Dart only. Whether the engine does frame pacing
  beyond delivering vsync is UNVERIFIED here.
* **`NestedScrollView` / `_NestedScrollCoordinator`** — referenced by a comment in
  `scroll_position_with_single_context.dart:211-213` but not read; its physics coordination is
  UNVERIFIED.
* **`TwoDimensionalScrollable` physics** — only its recognizer wiring at `scrollable.dart:2375-2417`
  and `:2510-2536` was read.
* **Whether `math.min(_frictionSimulation.dx(...), maxSpringTransferVelocity)` and
  `math.min(0.0, velocity)` are intentional or bugs** — I report the code; I have not checked
  the Flutter issue tracker.
* **Numeric values in tables** (e.g. `_physicalCoeff = 51890.2017`, `carried(5000) ≈ 15400`)
  are my arithmetic on the cited symbolic expressions, not values printed by Flutter.
