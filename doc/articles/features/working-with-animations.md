# Animations

Uno Platform supports [storyboarded animations](https://docs.microsoft.com/en-us/windows/uwp/design/motion/storyboarded-animations). A number of animation types are supported, including `DoubleAnimation`, `ColorAnimation`, and key frame-based animations.

## General guidelines
As of 1.43.1: 
1. GPU-bound animations are supported for the following properties:
	* `Opacity`
	* `RenderTransform` of type `TranslateTransform`, `RotateTransform`, `ScaleTransform`, or `CompositeTransform`. Transforms cannot be part of a TransformGroup.
1. When animating a `Transform`, you can animate only one property at a time (i.e. `CompositeTransform.TranslateX` *or* `CompositeTransform.TranslateY`),
1. You cannot reuse a `Transform` or an `Animation` declared in resources on multiple controls (instead you have to put your animation in a `Template`)
1. By default on iOS, Android and WASM, controls are clipped by their parent. On iOS you can set the flag `Uno.UI.FeatureConfiguration.UIElement.UseLegacyClipping = false` to get the Windows behavior.
