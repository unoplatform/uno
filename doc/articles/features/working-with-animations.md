---
uid: Uno.Features.Animations
---

# Animations

Uno Platform supports [storyboarded animations](https://learn.microsoft.com/windows/apps/design/motion/storyboarded-animations). A number of animation types are supported, including `DoubleAnimation`, `ColorAnimation`, and key frame-based animations.

## General guidelines

1. GPU-bound animations are supported for the following properties:
    * `Opacity`
    * `RenderTransform` of type `TranslateTransform`, `RotateTransform`, `ScaleTransform`, or `CompositeTransform`. Transforms cannot be part of a TransformGroup.
1. When animating a `Transform`, you can animate only one property at a time (i.e. `CompositeTransform.TranslateX` *or* `CompositeTransform.TranslateY`),
1. You cannot reuse a `Transform` or an `Animation` declared in resources on multiple controls (instead you have to put your animation in a `Template`)
