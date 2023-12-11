---
uid: Uno.XamarinFormsMigration.Animations
---

# Animations

## Easing functions

The table below shows the mappings between Xamarin Forms to Uno:

| Function | Xamarin Forms | Uno Platform |
|---|---|---|
| Bounce |  `BounceIn`<br>`BounceOut`  | `BounceEase`  |
| Cubic | `CubicIn`<br>`CubicOut`<br>`CubicInOut` | `CubicEase` |
| Linear | `Linear` | _Don't specify a value for `DoubleAnimation.EasingFunction`_ |
| Sine |  `SinIn`<br>`SinOut`<br>`SinInOut` | `SineEase` |
| Spring Oscillation | `SpringIn`<br>`SpringOut` | `ElasticEase` |

## See also
* [Sample project: Animations](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MigratingAnimations)
* [Xamarin.Forms Animations](https://learn.microsoft.com/xamarin/xamarin-forms/user-interface/animation/)