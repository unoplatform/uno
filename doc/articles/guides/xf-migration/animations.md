---
uid: Uno.XamarinFormsMigration.Animations
---

# Migrating Animations from Xamarin.Forms to Uno Platform

This guide explores how to migrate animations from Xamarin.Forms to Uno Platform. While both frameworks support rich animation capabilities, they use different APIs and approaches. This article will help you understand the differences and successfully migrate your animations.

## Animation Approaches

### Xamarin.Forms Animations

Xamarin.Forms provides a simple, code-based animation API through extension methods on the `View` class:

- `RotateTo`
- `ScaleTo`, `ScaleXTo`, `ScaleYTo`
- `TranslateTo`
- `FadeTo`

These methods are called directly on view instances and allow you to animate properties over time with optional easing functions.

### Uno Platform Animations

Uno Platform, following the WinUI model, uses XAML-based animations through the `Storyboard` class. Animations are typically defined in XAML and controlled from code. This approach provides more flexibility and better separation of animation definitions from business logic.

## Animation Types

### Transform Animations

Transforms allow you to rotate, scale, translate, and skew visual elements without affecting their layout position.

#### Rotation

**Xamarin.Forms:**

```csharp
await myElement.RotateTo(360, 1000);
```

**Uno Platform:**

```xml
<Storyboard x:Name="RotateStoryboard">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(RotateTransform.Angle)"
                     To="360"
                     Duration="0:0:1"/>
</Storyboard>
```

To use transforms in Uno Platform, you must first apply the transform to the element:

```xml
<Border x:Name="myElement">
    <Border.RenderTransform>
        <RotateTransform/>
    </Border.RenderTransform>
</Border>
```

#### Scaling

**Xamarin.Forms:**

```csharp
await myElement.ScaleTo(2.0, 1000);
await myElement.ScaleXTo(2.0, 1000);
await myElement.ScaleYTo(2.0, 1000);
```

**Uno Platform:**

```xml
<Storyboard x:Name="ScaleStoryboard">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                     To="2.0"
                     Duration="0:0:1"/>
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                     To="2.0"
                     Duration="0:0:1"/>
</Storyboard>
```

With the transform applied:

```xml
<Border x:Name="myElement">
    <Border.RenderTransform>
        <ScaleTransform/>
    </Border.RenderTransform>
</Border>
```

#### Translation

**Xamarin.Forms:**

```csharp
await myElement.TranslateTo(100, 100, 1000);
```

**Uno Platform:**

```xml
<Storyboard x:Name="TranslateStoryboard">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.X)"
                     To="100"
                     Duration="0:0:1"/>
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.Y)"
                     To="100"
                     Duration="0:0:1"/>
</Storyboard>
```

With the transform applied:

```xml
<Border x:Name="myElement">
    <Border.RenderTransform>
        <TranslateTransform/>
    </Border.RenderTransform>
</Border>
```

### Opacity Animations

**Xamarin.Forms:**

```csharp
await myElement.FadeTo(0.5, 1000);
```

**Uno Platform:**

```xml
<Storyboard x:Name="FadeStoryboard">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="Opacity"
                     To="0.5"
                     Duration="0:0:1"/>
</Storyboard>
```

## Xamarin.Forms to Storyboard Mappings

| Xamarin.Forms Method | WinUI/Uno Storyboard Target |
|---------------------|----------------------------|
| `RotateTo` | `(UIElement.RenderTransform).(RotateTransform.Angle)` |
| `ScaleTo` | `(UIElement.RenderTransform).(ScaleTransform.ScaleX)` and `(UIElement.RenderTransform).(ScaleTransform.ScaleY)` |
| `ScaleXTo` | `(UIElement.RenderTransform).(ScaleTransform.ScaleX)` |
| `ScaleYTo` | `(UIElement.RenderTransform).(ScaleTransform.ScaleY)` |
| `TranslateTo` | `(UIElement.RenderTransform).(TranslateTransform.X)` and `(UIElement.RenderTransform).(TranslateTransform.Y)` |
| `FadeTo` | `Opacity` |

## Composite Transforms

If you need to apply multiple transforms to a single element (e.g., rotate and scale), use `CompositeTransform` instead of individual transforms:

```xml
<Border x:Name="myElement">
    <Border.RenderTransform>
        <CompositeTransform/>
    </Border.RenderTransform>
</Border>

<Storyboard x:Name="CompositeStoryboard">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(CompositeTransform.Rotation)"
                     To="360"
                     Duration="0:0:1"/>
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(CompositeTransform.ScaleX)"
                     To="2.0"
                     Duration="0:0:1"/>
</Storyboard>
```

## Easing Functions

Easing functions change how animations progress over time, enabling elements to speed up and slow down for more natural movements.

### Xamarin.Forms Easing

Xamarin.Forms uses the `Easing` class with built-in easing functions:

```csharp
await myElement.ScaleTo(2.0, 1000, Easing.BounceOut);
```

### Uno Platform Easing

WinUI uses `EasingFunctionBase`-derived classes:

```xml
<DoubleAnimation Storyboard.TargetName="myElement"
                 Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                 To="2.0"
                 Duration="0:0:1">
    <DoubleAnimation.EasingFunction>
        <BounceEase EasingMode="EaseOut"/>
    </DoubleAnimation.EasingFunction>
</DoubleAnimation>
```

### Easing Function Mappings

| Xamarin.Forms | WinUI/Uno Platform |
|---------------|-------------------|
| `Bounce` (`BounceIn`/`BounceOut`) | `BounceEase` |
| `Cubic` (`CubicIn`/`CubicInOut`/`CubicOut`) | `CubicEase` |
| `Linear` | (specify no `EasingFunction`) |
| `Sin` (`SinIn`/`SinInOut`/`SinOut`) | `SineEase` |
| `Spring` (`SpringIn`/`SpringOut`) | `ElasticEase` |

### Easing Modes

Easing functions in WinUI have an `EasingMode` property that controls the direction of the easing:

- `EaseIn`: Acceleration at the start
- `EaseOut`: Deceleration at the end
- `EaseInOut`: Acceleration at start and deceleration at end

```xml
<BounceEase EasingMode="EaseOut"/>
<CubicEase EasingMode="EaseInOut"/>
<SineEase EasingMode="EaseIn"/>
```

### Additional WinUI Easing Functions

WinUI provides additional easing functions not available in Xamarin.Forms:

- **QuadraticEase**: Quadratic acceleration/deceleration
- **QuarticEase**: Quartic acceleration/deceleration
- **QuinticEase**: Quintic acceleration/deceleration
- **PowerEase**: Acceleration/deceleration using any power (provides more flexibility)
- **BackEase**: Reverses direction slightly before starting (or overshoots on ending)
- **CircleEase**: Acceleration/deceleration using a circular function
- **ExponentialEase**: Acceleration/deceleration using an exponential function

Example using `PowerEase`:

```xml
<PowerEase Power="3" EasingMode="EaseInOut"/>
```

Example using `BackEase`:

```xml
<BackEase Amplitude="0.5" EasingMode="EaseOut"/>
```

## Custom Easing

Xamarin.Forms supports custom easing functions where you define your own progression from 0.0 to 1.0:

```csharp
var customEasing = new Easing(t => Math.Sin(t * Math.PI * 2));
await myElement.ScaleTo(2.0, 1000, customEasing);
```

WinUI doesn't currently support custom easing functions directly. You can work around this limitation by:

1. Using the closest available easing function
2. Using keyframe animation to specify required values along the timeline

### Keyframe Animations

Keyframe animations allow you to specify exact values at specific points in time:

```xml
<Storyboard x:Name="KeyframeStoryboard">
    <DoubleAnimationUsingKeyFrames Storyboard.TargetName="myElement"
                                    Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)">
        <LinearDoubleKeyFrame KeyTime="0:0:0" Value="1.0"/>
        <LinearDoubleKeyFrame KeyTime="0:0:0.25" Value="1.5"/>
        <LinearDoubleKeyFrame KeyTime="0:0:0.5" Value="0.8"/>
        <LinearDoubleKeyFrame KeyTime="0:0:0.75" Value="1.8"/>
        <LinearDoubleKeyFrame KeyTime="0:0:1" Value="2.0"/>
    </DoubleAnimationUsingKeyFrames>
</Storyboard>
```

Types of keyframes:

- **LinearDoubleKeyFrame**: Linear interpolation between values
- **DiscreteDoubleKeyFrame**: Jumps to the value (no interpolation)
- **EasingDoubleKeyFrame**: Applies an easing function between keyframes
- **SplineDoubleKeyFrame**: Uses a cubic Bezier curve for interpolation

## Controlling Animations from Code

### Starting Animations

**Xamarin.Forms:**

```csharp
await myElement.RotateTo(360, 1000);
```

**Uno Platform:**

```csharp
RotateStoryboard.Begin();
```

### Stopping Animations

**Uno Platform:**

```csharp
RotateStoryboard.Stop();
```

### Pausing and Resuming

**Uno Platform:**

```csharp
RotateStoryboard.Pause();
RotateStoryboard.Resume();
```

### Handling Completion

**Xamarin.Forms:**

```csharp
await myElement.RotateTo(360, 1000);
// Code here runs after animation completes
```

**Uno Platform:**

```csharp
RotateStoryboard.Completed += (s, e) =>
{
    // Code here runs after animation completes
};
RotateStoryboard.Begin();
```

## Animation Repeat Behavior

### Repeat Forever

**Uno Platform:**

```xml
<Storyboard x:Name="RepeatStoryboard" RepeatBehavior="Forever">
    <DoubleAnimation Storyboard.TargetName="myElement"
                     Storyboard.TargetProperty="(UIElement.RenderTransform).(RotateTransform.Angle)"
                     From="0"
                     To="360"
                     Duration="0:0:2"/>
</Storyboard>
```

### Repeat Count

**Uno Platform:**

```xml
<Storyboard x:Name="RepeatStoryboard" RepeatBehavior="3x">
    <!-- Animation repeats 3 times -->
</Storyboard>
```

### Auto-Reverse

**Uno Platform:**

```xml
<Storyboard x:Name="ReverseStoryboard" AutoReverse="True">
    <!-- Animation plays forward, then backward -->
</Storyboard>
```

## Migration Strategy

When migrating animations from Xamarin.Forms to Uno Platform:

1. **Identify all animation calls** in your Xamarin.Forms code (look for `RotateTo`, `ScaleTo`, `TranslateTo`, `FadeTo`, etc.)
2. **Create corresponding Storyboards** in XAML for each animation
3. **Add necessary RenderTransforms** to elements that will be animated
4. **Map easing functions** from Xamarin.Forms to WinUI equivalents
5. **Replace animation method calls** with `Storyboard.Begin()` calls
6. **Handle completion** using the `Completed` event instead of `await`
7. **Test animations** on all target platforms to ensure consistency

## Summary

While Xamarin.Forms uses code-based animations and Uno Platform uses XAML-based Storyboards, the concepts are similar:

- Both support transform-based animations (rotation, scaling, translation)
- Both support opacity animations
- Both support easing functions (with similar built-in options)
- Uno Platform provides more control through XAML definitions
- Uno Platform supports additional easing functions and keyframe animations

The main difference is the declarative nature of Uno Platform animations, which provides better separation between animation definitions and business logic.

## Next Steps

- Continue with [Migrating Custom Controls](xref:Uno.XamarinFormsMigration.CustomControls)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)
