---
uid: Uno.Contributing.InteractionTracker
---
# InteractionTracker internals

This document tries to detail and clarify the implementation of InteractionTracker.

The interaction tracker has four states:

1. Idle
2. Interacting
3. Inertia
4. CustomAnimation

Currently, custom animation is not yet implemented. The transitioning between states is well-explained in [InteractionTracker Class | Microsoft Learn](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.interactions.interactiontracker).

This document is going to focus on the Inertia state. The core calculations for this state are in `AxisHelper` nested class (in `InteractionTrackerInertiaHandler.AxisHelper.cs` file).

First, there are two things we need to early calculate:

1. The position distance we will move
1. The total inertia time (i.e, the time we stay in inertia state before transitioning to idle, which also is the time it takes for the position to be constant)

> [!NOTE]
> All mentions of decay rates below are `1 - PositionInertiaDecayRate`, unless `PositionInertiaDecayRate` is explicitly written.

## Position distance to move

This is about the "natural" distance, i.e, not taking into account MinPosition/MaxPosition constraints.

The core important equation of this is:

```csharp
float val = MathF.Pow(DecayRate, time);
return ((val - 1.0f) * InitialVelocity) / MathF.Log(DecayRate);
```

This equation represents the position of an object undergoing exponential decay over time.

Normally, the exponential decay is represented by `DecayRate ^ t`. This equation, however, is more about a "rate of change" in position. So, to get the position, we integrate that.

The integration of `DecayRate ^ t` is `(DecayRate ^ t) / ln(DecayRate)`.

However, we want the distance to be zero at time t = zero. Note that at time t = 0 the numerator is `DecayRate ^ 0` which is `1`. So, we subtract one.

The formula is now `x(t) = ((DecayRate ^ t) - 1) / ln(DecayRate)`. One last important thing that affects the distance is the initial velocity.

For a given decay rate and a given time, the effect of initial velocity is linear. So, we multiply the initial velocity. That is the final formula.

Now, let's visualize this by looking at a graph (assuming initial velocity = 60):

![image](https://github.com/unoplatform/uno/assets/31348972/2e76c554-2299-4ad1-8917-144d01978915)

You can see the position distance at time zero is always zero, and at the beginning it's exponentially growing until it settles down to its final value.

## Total inertia time (`TimeToMinimumVelocity`)

The core important equation of this is:

```csharp
return (MathF.Log(minimumVelocity) - MathF.Log(initialVelocity)) / MathF.Log(decayRate);
```

Note that as we have exponential decay, the final velocity is calculated as `v_f = v_i * r^t`, where:

- `v_f`: final velocity
- `v_i`: initial velocity
- `r`: decay rate
- `t`: time

To get the time:

1. `v_f / v_i = r^t`
1. `ln(v_f / v_i) = ln(r^t)`
1. `ln(v_f) - ln(v_i) = t ln(r)`
1. `t = (ln(v_f) - ln(v_i)) / ln(r)`

Currently, we fix the final velocity `v_f` to 30 px/sec, and call that `minimumVelocity`.

Note that the expression above will produce negative value if initialVelocity < minimumVelocity. So, in `GetTimeToMinimumVelocity`, if initialVelocity <= minimumVelocity, we return zero. The following graph visualizes the expression:

![image](https://github.com/unoplatform/uno/assets/31348972/958b0e92-d268-4229-810d-b38609eff4e0)

The red curve corresponds to decay rate 0.7, and the green one corresponds to decay rate 0.9. The x-axis is the initial velocity, and the y-axis is `TimeToMinimumVelocity`.

Note that in both cases, the intersection with x-axis is the minimum velocity. The higher the decay rate, the more `TimeToMinimumVelocity`. Again, decay rate mentioned here is `1 - PositionInertiaDecayRate`.
So, actually as `PositionInertiaDecayRate` gets larger, the time gets smaller.

## Generalized position calculation

Earlier, we concluded that `x(t) = InitialVelocity * ((DecayRate ^ t) - 1) / ln(DecayRate)`. This works well if "overpanning" isn't taken into account.
The overpanning happens when, in interacting state, the user active input goes beyond the restrictions of MinPosition and MaxPosition.

Two cases where this can happen:

1. In Interacting state, the active input caused Position to be out of the specified range. That is, when we switch from Interacting to Idle, the position is already out of the specified range.
2. Interacting state is left with high velocity that causes the calculation in Inertia state to calculate natural distance out of the MinPosition/MaxPosition range.

In this case, once we have a value outside of the range while in inertia, we want to bring it back. For that, we use a damping animation.

In WinUI, underdamped animation is used, and potentially critically-damped animation is also used for low velocities.

The current Uno implementation is more simple, using only critically-damped animation.

So, once we get a value outside of the range in inertia, we set `_dampingStateTimeInSeconds` to the current elapsed time, and set `_dampingStatePosition` to the current position. These values will be used in future `GetPosition` calls.

Basically, we want the animation to settle in the *remaining* time, i.e, `TimeToMinimumVelocity - _dampingStateTimeInSeconds.Value`.

The following graph shows the critical damping equation with time being the x-axis. As you see, it starts from value of 0 until it settles to 1, and the settling time is approximately `5.8335 / w`.

The settling time above is the 2% criterion, defined as "the time required for the response curve to reach and stay within 2% of the final value". The position calculation is then done by:

```csharp
value * (FinalModifiedValue - _dampingStatePosition!.Value) + _dampingStatePosition.Value
```

where `value` is the result of the critically damped equation.

Note that at the beginning when `value` is zero, we get `_dampingStatePosition`, then as time goes on and we reach 1, we get `FinalModifiedValue`, where `FinalModifiedValue` is the natural position clamped between `MinPosition` and `MaxPosition`.
