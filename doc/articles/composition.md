---
uid: Uno.Development.Composition
---

# Composition API

Composition Visuals make up the visual tree structure which all other features of the composition API use and build on.
The API allows developers to define and create one or many visual objects each representing a single node in a visual tree.

To get more info, you can refer to [Microsoft's documentation](https://learn.microsoft.com/windows/uwp/composition/composition-visual-tree).

Uno Platform currently supports a small number of the APIs in the [`Windows.UI.Composition` namespace](https://learn.microsoft.com/uwp/api/windows.ui.composition?view=winrt).

The rest of this article details Uno-specific considerations regarding Composition API.

**On Android, most composition features are functional only for Android 10 (API 29) and above.**

## Compositor Thread [Android]

On Android, the composition refers to the [`Draw`](https://developer.android.com/reference/android/view/View#draw(android.graphics.Canvas)) and [`OnDraw`](https://developer.android.com/reference/android/view/View#onDraw(android.graphics.Canvas)) methods.
To get more info about custom drawing on Android, you can refer to the [Android's documentation](https://developer.android.com/training/custom-views/custom-drawing).

By default, those methods are invoked on the UI Thread.

With Uno, you can request to run those methods on a dedicated thread by setting in your Android application's constructor:

```csharp
Uno.CompositionConfiguration.Configuration = Uno.CompositionConfiguration.Options.Enabled;
```

This thread will also be used for [independent animations](https://learn.microsoft.com/windows/uwp/design/motion/storyboarded-animations#dependent-and-independent-animations).

**When overriding the `[On]Draw` methods, it is very important not to access any state that can be edited from the UI Thread, including any `DependencyProperty`.**
**Instead, you should capture the state of your control into a `RenderNode` during the `ArrangeOverride` and render it on the provided `Canvas`.**

_There are a few known issues associated with the used of the compositor thread, [make sure to read the section below](#known-issues)._

## Brush Anti-aliasing [Skia Backends]

On Skia Desktop targets (X11, Framebuffer, macOS, and Windows), anti-aliasing is disabled by default for brushes, You can request it to be anti-aliased by setting this in your application's constructor:

```csharp
#if HAS_UNO
    Uno.CompositionConfiguration.Configuration |= Uno.CompositionConfiguration.Options.UseBrushAntialiasing;
#endif
```

Or alternatively, if you want to enable all available Composition capabilities:

```csharp
#if HAS_UNO
    Uno.CompositionConfiguration.Configuration = Uno.CompositionConfiguration.Options.Enabled;
#endif
```

## Implemented APIs [Skia Backends]

### Windows.UI.Composition

- [CompositionAnimation](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionanimation)
- [CompositionBackdropBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionbackdropbrush)
- [CompositionBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionbrush)
- [CompositionCapabilities](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositioncapabilities)
- [CompositionClip](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionclip)
- [CompositionColorBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositioncolorbrush)
- [CompositionEffectBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositioneffectbrush)
- [CompositionEffectFactory](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositioneffectfactory)
- [CompositionEffectSourceParameter](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositioneffectsourceparameter)
- [CompositionEllipseGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionellipsegeometry)
- [CompositionGeometricClip](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositiongeometricclip)
- [CompositionGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositiongeometry)
- [CompositionGradientBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositiongradientbrush)
- [CompositionLinearGradientBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionlineargradientbrush)
- [CompositionLineGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionlinegeometry)
- [CompositionMaskBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionmaskbrush)
- [CompositionNineGridBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionninegridbrush)
- [CompositionObject](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionobject)
- [CompositionPath](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionpath)
- [CompositionPathGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionpathgeometry)
- [CompositionPropertySet](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionpropertyset)
- [CompositionRadialGradientBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionradialgradientbrush)
- [CompositionRectangleGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionrectanglegeometry)
- [CompositionRoundedRectangleGeometry](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionroundedrectanglegeometry)
- [CompositionShape](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionshape)
- [CompositionShapeCollection](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionshapecollection)
- [CompositionSpriteShape](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionspriteshape)
- [CompositionSurfaceBrush](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionsurfacebrush)
- [CompositionViewBox](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionviewbox)
- [CompositionVisualSurface](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositionvisualsurface)
- [Compositor](https://learn.microsoft.com/uwp/api/windows.ui.composition.compositor)
- [ContainerVisual](https://learn.microsoft.com/uwp/api/windows.ui.composition.containervisual)
- [ExpressionAnimation](https://learn.microsoft.com/uwp/api/windows.ui.composition.expressionanimation)
- [InsetClip](https://learn.microsoft.com/uwp/api/windows.ui.composition.insetclip)
- [IVisualElement](https://learn.microsoft.com/uwp/api/windows.ui.composition.ivisualelement)
- [IVisualElement2](https://learn.microsoft.com/uwp/api/windows.ui.composition.ivisualelement2)
- [RectangleClip](https://learn.microsoft.com/uwp/api/windows.ui.composition.rectangleclip)
- [RedirectVisual](https://learn.microsoft.com/uwp/api/windows.ui.composition.redirectvisual)
- [ShapeVisual](https://learn.microsoft.com/uwp/api/windows.ui.composition.shapevisual)
- [SpriteVisual](https://learn.microsoft.com/uwp/api/windows.ui.composition.spritevisual)
- [Visual](https://learn.microsoft.com/uwp/api/windows.ui.composition.visual)
- [VisualCollection](https://learn.microsoft.com/uwp/api/windows.ui.composition.visualcollection)

### Windows.UI.Composition.Interactions

- [InteractionTracker](https://learn.microsoft.com/uwp/api/windows.ui.composition.interactions.interactiontracker)
- [VisualInteractionSource](https://learn.microsoft.com/uwp/api/windows.ui.composition.interactions.visualinteractionsource)

### Windows.Graphics.Effects

- [IGraphicsEffectSource](https://learn.microsoft.com/uwp/api/windows.graphics.effects.igraphicseffectsource)

### Windows.Graphics.Effects.Interop

- [IGraphicsEffectD2D1Interop](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.effects.interop/nn-windows-graphics-effects-interop-igraphicseffectd2d1interop)

### Microsoft.Graphics.Canvas.Effects (Win2D/Composition Effects)

- [AlphaMaskEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_AlphaMaskEffect.htm)
- [ArithmeticCompositeEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_ArithmeticCompositeEffect.htm)
- [BlendEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_BlendEffect.htm)
- [BorderEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_BorderEffect.htm)
- [ColorMatrixEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_ColorMatrixEffect.htm)
- [ColorSourceEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_ColorSourceEffect.htm)
- [CompositeEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_CompositeEffect.htm)
- [ContrastEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_ContrastEffect.htm)
- [CrossFadeEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_CrossFadeEffect.htm)
- [DistantDiffuseEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_DistantDiffuseEffect.htm)
- [DistantSpecularEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_DistantSpecularEffect.htm)
- [ExposureEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_ExposureEffect.htm)
- [GammaTransferEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_GammaTransferEffect.htm)
- [GaussianBlurEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_GaussianBlurEffect.htm)
- [GrayscaleEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_GrayscaleEffect.htm)
- [HueRotationEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_HueRotationEffect.htm)
- [InvertEffectEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_InvertEffect.htm)
- [LinearTransferEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_LinearTransferEffect.htm)
- [LuminanceToAlphaEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_LuminanceToAlphaEffect.htm)
- [Matrix5x4](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_Matrix5x4.htm)
- [OpacityEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_OpacityEffect.htm)
- [PointDiffuseEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_PointDiffuseEffect.htm)
- [PointSpecularEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_PointSpecularEffect.htm)
- [SaturationEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_SaturationEffect.htm)
- [SepiaEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_SepiaEffect.htm)
- [SpotDiffuseEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_SpotDiffuseEffect.htm)
- [SpotSpecularEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_SpotSpecularEffect.htm)
- [TemperatureAndTintEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_TemperatureAndTintEffect.htm)
- [TintEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_TintEffect.htm)
- [Transform2DEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_Transform2DEffect.htm)

Note that while Uno Platform implements these effects and [their Win2D wrappers](https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Composition/Win2D/Microsoft/Graphics/Canvas/Effects), the Win2D wrappers are still internal and not exposed to users, but the effects can still be used by temporary implementing the [IGraphicsEffectD2D1Interop](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.effects.interop/nn-windows-graphics-effects-interop-igraphicseffectd2d1interop) interface manually until the Win2D wrappers become public, like for example the [GaussianBlurEffect](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Effects_GaussianBlurEffect.htm) can be implemented like this:

```csharp
#nullable enable

using System;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

internal class GaussianBlurEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
{
    private string _name = "GaussianBlurEffect";
    private Guid _id = new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public IGraphicsEffectSource? Source { get; set; }

    public float BlurAmount { get; set; } = 3.0f;

    public Guid GetEffectId() => _id;

    public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
    {
        switch (name)
        {
            case nameof(BlurAmount):
                {
                    index = 0;
                    mapping = GraphicsEffectPropertyMapping.Direct;
                    break;
                }
            default:
                {
                    index = 0xFF;
                    mapping = (GraphicsEffectPropertyMapping)0xFF;
                    break;
                }
        }
    }

    public object? GetProperty(uint index)
    {
        switch (index)
        {
            case 0:
                return BlurAmount;
            default:
                return null;
        }
    }

    public uint GetPropertyCount() => 1;
    public IGraphicsEffectSource? GetSource(uint index) => Source;
    public uint GetSourceCount() => 1;
}
```

The GUID used in the example above is the Effect CLSID of the [Direct2D Gaussian Blur Effect](https://learn.microsoft.com/windows/win32/direct2d/gaussian-blur). For a list of built-in Direct2D effects and their corresponding CLSIDs, see [Direct2D Built-in Effects](https://learn.microsoft.com/windows/win32/direct2d/built-in-effects).

## Known issues

- [Android] When using the compositor thread, the native ripple effect of Android (used in native buttons) does not work.

- [Skia Backends] Some Composition effects don't render properly (or at all) on software rendering (CPU), to check if Uno is running on the software rendering (CPU) or the hardware rendering (GPU), you can call `CompositionCapabilities.GetForCurrentView().AreEffectsFast()`.
