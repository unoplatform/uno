---
uid: Uno.XamarinFormsMigration.CustomDrawnControls
---

# Migrating Custom-Drawn Controls from Xamarin.Forms to Uno Platform

This guide explores how to migrate custom-drawn controls from Xamarin.Forms to Uno Platform. Custom-drawn controls use low-level graphics APIs to render custom visuals, and the good news is that the most common approach – using SkiaSharp – works seamlessly with Uno Platform.

## Custom-Drawn Controls in Xamarin.Forms

While there are many ways to create custom-drawn controls, the main implementation you'll encounter in multi-platform Xamarin.Forms controls is **SkiaSharp**. SkiaSharp is a cross-platform .NET API based on the [Skia](https://skia.org/) 2D graphics library. Skia was created by Google but is available on all mainstream platforms, allowing you to create drawing code that works across devices regardless of their specific implementation.

### SKCanvasView in Xamarin.Forms

When building a custom control for Xamarin.Forms, you use the `SKCanvasView`. This provides a view that contains the `OnPaintSurface` method, which you implement in your derived class to draw to the screen. The method arguments provide access to the `SKSurface`, which represents the raw surface you can draw to.

The `SKCanvasView` handles resizing and calls your `OnPaintSurface` method to redraw as required. Your code considers the size of the surface, as you should expect this to vary between devices or at runtime due to screen rotation or moveable UI elements.

## SkiaSharp and Uno Platform

The great news is that the SkiaSharp project already has support for Uno Platform through the [**SkiaSharp.Views.Uno**](https://www.nuget.org/packages/SkiaSharp.Views.Uno) NuGet package.

### SKXamlCanvas Control

The `SKXamlCanvas` control is the equivalent to the `SKCanvasView` from Xamarin.Forms, but it inherits from the WinUI `Canvas` class. This control:

- Handles lifecycle and size changes
- Contains an overridable `OnPaintSurface` method where you can draw to the surface
- Works consistently across all Uno Platform targets

Because of this, you probably don't need to change any of the SkiaSharp drawing code from an existing control – you simply:

1. Create a library with a class derived from `SKXamlCanvas`
2. Hook up the drawing logic from your Xamarin.Forms control

## Migration Example: Microcharts

[Microcharts](https://github.com/microcharts-dotnet/Microcharts/) is a simple open-source charting library written in C# that utilizes SkiaSharp to draw a wide range of chart types. It had implementations for Xamarin.Forms, .NET MAUI, UWP, and others but initially had no Uno support.

The great thing about having UWP support is that the code was already written for the Windows version of `SKCanvasView` and could be used with minimal changes in an Uno library.

### Creating the Uno Library

To add Uno support, a new project was created: `Microcharts.Uno.WinUI`. This is created as a .NET Standard library, so it has no platform dependencies itself. The required steps:

1. Add a reference to `SkiaSharp.Views.Uno.WinUI`
2. Add a reference to `Uno.WinUI`
3. Link to the existing UWP `ChartView.cs` file

Because the `SkiaSharp.Views` libraries depend on the base `SkiaSharp` library, you don't need to explicitly add it separately.

### Code Changes for WinUI 3.0 Support

The only necessary changes in the code were to support WinUI 3.0 (and hence the `Uno.WinUI` implementation) by changing the using statements. This is handled using an `#if` directive so that the single file can be retained in multiple projects:

```csharp
#if WINUI
    using Microsoft.UI.Xaml;
    using SkiaSharp.Views.Windows;
#else
    using Windows.UI.Xaml;
    using SkiaSharp.Views.UWP;
#endif
```

### The OnPaintCanvas Method

As described above, the drawing is carried out by the `OnPaintCanvas` method:

```csharp
private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
{
    if (this.chart != null)
    {
        this.chart.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
    }
    else
    {
        e.Surface.Canvas.Clear(SKColors.Transparent);
    }
}
```

The `chart` object is an instance of `Microcharts.Chart` and is the same across all implementations of Microcharts. It contains the `Draw` method, which takes the Skia canvas and size and does all the drawing based on the properties set on it and the data source.

This means that on all platforms, you can access the `Chart` property via XAML or code and have the same API regardless of platform.

## Creating Your Own Custom-Drawn Control

### Step 1: Add SkiaSharp Package

Add the `SkiaSharp.Views.Uno.WinUI` NuGet package to your project:

```xml
<PackageReference Include="SkiaSharp.Views.Uno.WinUI" Version="2.88.0" />
```

### Step 2: Create a Custom Control Class

Create a class that inherits from `SKXamlCanvas`:

```csharp
using SkiaSharp;
using SkiaSharp.Views.Windows;

public class MyCustomControl : SKXamlCanvas
{
    public MyCustomControl()
    {
        PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        // Clear the canvas
        canvas.Clear(SKColors.White);

        // Your custom drawing code here
        using (var paint = new SKPaint())
        {
            paint.Color = SKColors.Blue;
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;

            // Example: Draw a circle in the center
            var centerX = info.Width / 2f;
            var centerY = info.Height / 2f;
            var radius = Math.Min(info.Width, info.Height) / 4f;

            canvas.DrawCircle(centerX, centerY, radius, paint);
        }
    }
}
```

### Step 3: Use the Control in XAML

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:MyApp">

    <Grid>
        <local:MyCustomControl Width="400" Height="400"/>
    </Grid>
</Page>
```

## Adding Bindable Properties

To make your custom control more flexible, add dependency properties that can be data-bound:

```csharp
public class MyCustomControl : SKXamlCanvas
{
    public static readonly DependencyProperty FillColorProperty =
        DependencyProperty.Register(
            nameof(FillColor),
            typeof(Color),
            typeof(MyCustomControl),
            new PropertyMetadata(Colors.Blue, OnPropertyChanged));

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Invalidate the control to trigger a redraw
        if (d is MyCustomControl control)
        {
            control.Invalidate();
        }
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        canvas.Clear(SKColors.White);

        using (var paint = new SKPaint())
        {
            // Convert WinUI Color to SKColor
            var color = FillColor;
            paint.Color = new SKColor(color.R, color.G, color.B, color.A);
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;

            var centerX = info.Width / 2f;
            var centerY = info.Height / 2f;
            var radius = Math.Min(info.Width, info.Height) / 4f;

            canvas.DrawCircle(centerX, centerY, radius, paint);
        }
    }
}
```

Now you can bind or set the color from XAML:

```xml
<local:MyCustomControl Width="400" 
                       Height="400" 
                       FillColor="Red"/>
```

## Performance Considerations

### Invalidation

Call `Invalidate()` on your control when you need to trigger a redraw. This is typically done in property change handlers.

### Caching

For complex drawings that don't change often, consider caching rendered content:

```csharp
private SKBitmap cachedBitmap;

private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
{
    if (cachedBitmap == null || needsRedraw)
    {
        // Render to cached bitmap
        cachedBitmap = new SKBitmap(info.Width, info.Height);
        using (var canvas = new SKCanvas(cachedBitmap))
        {
            // Draw your complex graphics here
        }
        needsRedraw = false;
    }

    // Draw the cached bitmap to the surface
    e.Surface.Canvas.DrawBitmap(cachedBitmap, 0, 0);
}
```

### Hardware Acceleration

SkiaSharp automatically uses hardware acceleration when available. Ensure your drawing code is efficient:

- Reuse `SKPaint` objects when possible
- Dispose of resources properly
- Avoid creating objects inside the paint loop

## Common SkiaSharp Drawing Operations

### Drawing Shapes

```csharp
// Rectangle
canvas.DrawRect(x, y, width, height, paint);

// Circle
canvas.DrawCircle(centerX, centerY, radius, paint);

// Oval
canvas.DrawOval(new SKRect(left, top, right, bottom), paint);

// Line
canvas.DrawLine(x1, y1, x2, y2, paint);

// Path
using (var path = new SKPath())
{
    path.MoveTo(x1, y1);
    path.LineTo(x2, y2);
    path.LineTo(x3, y3);
    path.Close();
    canvas.DrawPath(path, paint);
}
```

### Drawing Text

```csharp
using (var paint = new SKPaint())
{
    paint.Color = SKColors.Black;
    paint.TextSize = 24;
    paint.IsAntialias = true;
    paint.TextAlign = SKTextAlign.Center;

    canvas.DrawText("Hello, Uno!", x, y, paint);
}
```

### Drawing Images

```csharp
using (var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.Assets.image.png"))
using (var bitmap = SKBitmap.Decode(stream))
{
    canvas.DrawBitmap(bitmap, x, y);
}
```

### Applying Effects

```csharp
using (var paint = new SKPaint())
{
    paint.Color = SKColors.Blue;
    
    // Shadow
    paint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, SKColors.Black);
    
    // Blur
    paint.ImageFilter = SKImageFilter.CreateBlur(5, 5);
    
    canvas.DrawCircle(centerX, centerY, radius, paint);
}
```

## Using Microcharts in an Uno App

The Microcharts.Samples.Uno project demonstrates how to use Microcharts in an Uno Platform app. The project structure:

- **Shared project**: Contains XAML and code-behind for the app
- **Platform projects**: Windows, Mobile, Skia.GTK, WebAssembly
- **Samples project**: Contains example data used by all sample projects

This shows how easy it is to share or port code between different .NET platforms, as all the logic is platform-agnostic.

## Migration Checklist

When migrating custom-drawn controls from Xamarin.Forms to Uno Platform:

- [ ] Identify all controls that use `SKCanvasView`
- [ ] Add `SkiaSharp.Views.Uno.WinUI` NuGet package to your Uno project
- [ ] Change control base class from `SKCanvasView` to `SKXamlCanvas`
- [ ] Update using statements to include `SkiaSharp.Views.Windows`
- [ ] Rename `OnPaintSurface` event handler if needed
- [ ] Convert `BindableProperty` to `DependencyProperty` for any custom properties
- [ ] Update XAML namespaces from Xamarin.Forms to WinUI
- [ ] Test on all target platforms
- [ ] Verify performance is acceptable

## Summary

Migrating custom-drawn controls from Xamarin.Forms to Uno Platform is straightforward when using SkiaSharp:

- **Minimal code changes**: Most SkiaSharp drawing code can be reused as-is
- **Same API**: The drawing API remains consistent across platforms
- **SKXamlCanvas**: Drop-in replacement for `SKCanvasView`
- **Platform support**: Works on all Uno Platform targets
- **Shared code**: Drawing logic can be completely platform-agnostic

The key difference is changing from `SKCanvasView` to `SKXamlCanvas` and updating namespaces – the actual drawing code remains the same.

## Next Steps

- Continue with [Migrating Custom Controls](xref:Uno.XamarinFormsMigration.CustomControls)
- Return to [Overview](xref:Uno.XamarinFormsMigration.Overview)

## Sample Code

The complete Microcharts code, including the Uno libraries and sample app, is available in the [Microcharts GitHub repository](https://github.com/microcharts-dotnet/Microcharts/). The Microcharts.Uno package is also available on NuGet for easy integration into your own app.
