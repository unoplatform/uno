---
uid: Uno.XamarinForms.Migration
---

# Overview of migrating from Xamarin.Forms to Uno Platform

Despite its subtle differences to other XAML frameworks, the markup language flavor used by Xamarin.Forms makes migrating your team's existing investment a straightforward process. Uno Platform retains the same battle-tested XAML flavor used to develop rich experiences with [WinUI](https://learn.microsoft.com/windows/apps/get-started/uno-simple-photo-viewer). This is how your project has a multi-platform path forward to compatibility with a multitude of powerful controls and timely support for work done upstream.

## Our approach

Uno Platform was built to streamline the tedious process of developing rich, powerful .NET apps that serve users on multiple platforms. Pixel-perfect compatibility with WinUI is achieved by replicating [controls](xref:Uno.XamarinForms.Migration#Controls), panels, and other sought-after constructs. This appreciation for consistency in appearence and behavior across platforms allows your app to embody a precise brand identity. While Xamarin.Forms also allows for reuse across platforms, it differs by using platform renderers to draw native UI elements. These implementations preserve platform-specific behavior and performance, but also result in additional boilerplate code.

Most third-party controls that target Xamarin.Forms are not immediately compatible with Uno. Adjustments to your app will be needed to benefit from decades of Windows ecosystem investment in responsive design, customization, accessibility and more. This series will guide your team through the steps to migrate several key aspects of your app's presentation layer to Uno.

## Controls

Complex app designs are known to not only leverage built-in native controls, but also to incorporate a third-party view that is comprised of the same native building blocks. These third-party controls enable reuse of more intricate UI patterns—such as a calendar, chart, or data list—achieving coherency across multiple pages. Another less common scenario, but one that is still important to consider, is when a third-party control is custom-drawn. This is often done to achieve the app experience your team planned for despite limitations in the capabilities of native controls. For example, opting for a custom-drawn control is desirable when striving for greater visual fidelity. This control category harnesses a third-party graphics library to render drawings from scratch and uniquely respond to behaviors.

We provide the tips and tricks to help you smoothly migrate both types of controls from Xamarin.Forms to Uno Platform.

### Native controls

Migrating custom controls will generally involve several key steps, depending on the features it uses. Updates to XAML and C# codebehind are usually needed to match the equivalent types in Uno Platform. Certain names of properties, elements, and attributes may need to be adjusted to match WinUI. Notably, scenarios that leverage template overrides to alter a custom control's default appearance need only minimal changes which won't disrupt how you define its XAML tree.

### Custom-drawn controls

It is common to use **SkiaSharp**, a cross-platform .NET API for 2D graphics, to create custom-drawn controls that work on **Uno Platform**.

See the [tutorial]() which introduces **Microcharts**, an open-source charting library that uses SkiaSharp to draw various types of charts. It demonstrates how to add **Uno support** to Microcharts by creating a new project and linking the existing code.

#### Sample app

Check out the sample app repository [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MigratingAnimations).

## Animations

### Easing functions

The table below shows the mappings between Xamarin Forms to Uno:

| Xamarin Forms | Uno Platform |
|---|---|
| Bounce (BounceIn/BounceOut)  | BounceEase  |
| Cubic (CubicIn/CubicInOut/CubicOut) | CubicEase |
| Linear | (specify no EasingFunction) |
| Sin (SinIn/SinInOut/SinOut) | SineEase |
| Spring (SpringIn/SpringOut) | ElasticEase |

