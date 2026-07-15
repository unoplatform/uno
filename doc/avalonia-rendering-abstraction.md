# Avalonia Platform Rendering Abstraction Reference

> Reference extracted for the Uno pluggable-drawing-backend work. Signatures are transcribed
> verbatim from the Avalonia source under `src/Avalonia.Base` (checkout `bc3001bf77`, 2026-07-11).
> This documents *Avalonia's* abstraction as a design reference — it is not Uno API.

This document catalogs the interfaces a rendering backend implements in Avalonia, together with the supporting resource interfaces that cross the platform boundary. All source lives under `src/Avalonia.Base`.

## Layering overview

Avalonia isolates every platform-specific graphics API (Skia, Direct2D, etc.) behind a small set of interfaces so the rest of the framework speaks a single rendering vocabulary.

- **The immediate drawing boundary is `IDrawingContextImpl`** (`Avalonia.Platform`). It is a stateful, retained-mode-free surface with a current `Transform`, a stack of clips/opacity/layers, and `Draw*` primitives (bitmap, line, geometry, rectangle, region, ellipse, glyph run). Everything ultimately renders through this interface.
- **`IPlatformRenderInterface` is the factory.** It is the process-wide service (`[Unstable, PrivateApi]`) that manufactures the opaque platform implementations — geometries, bitmaps, glyph runs, regions — and creates a backend context (`IPlatformRenderInterfaceContext`) from a low-level graphics context. The backend context in turn creates `IRenderTarget`s from native surfaces and offscreen layers.
- **`IRenderTarget` is the creator of drawing contexts.** Given a scene description it hands back a fresh `IDrawingContextImpl` for one rendering session, and reports readiness/retention properties.
- **Two kinds of things cross the boundary.** *Declarative* paint descriptions — brushes (`IBrush` and friends) and pens (`IPen`, `IDashStyle`) — cross as immutable `Avalonia.Media` value types that the backend reads. *Resources* — geometry, bitmaps, glyph runs, regions, effects, typefaces — cross as **opaque `*Impl` handles** that only the backend understands; the framework holds them without inspecting their internals.

Attribute conventions used throughout: `[Unstable]` (API may change), `[PrivateApi]` (framework-internal, not a public contract), `[NotClientImplementable]` (consumers may hold instances but must not implement the interface).

---

## Drawing context

### IDrawingContextImpl

- File: `src/Avalonia.Base/Platform/IDrawingContextImpl.cs`
- Declaration: `[Unstable] public interface IDrawingContextImpl : IDisposable`

```csharp
Matrix Transform { get; set; }
void Clear(Color color);
void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect);
void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect);
void DrawLine(IPen? pen, Point p1, Point p2);
void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry);
void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default);
void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region);
void DrawEllipse(IBrush? brush, IPen? pen, Rect rect);
void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun);
IDrawingContextLayerImpl CreateLayer(PixelSize size);
void PushClip(Rect clip);
void PushClip(RoundedRect clip);
void PushClip(IPlatformRenderInterfaceRegion region);
void PopClip();
void PushLayer(Rect bounds);
void PopLayer();
void PushOpacity(double opacity, Rect? bounds);
void PopOpacity();
void PushOpacityMask(IBrush mask, Rect bounds);
void PopOpacityMask();
void PushGeometryClip(IGeometryImpl clip);
void PopGeometryClip();
void PushRenderOptions(RenderOptions renderOptions);
void PopRenderOptions();
void PushTextOptions(TextOptions textOptions);
void PopTextOptions();
object? GetFeature(Type t);
```

| Member | Description |
|--------|-------------|
| `Transform` | Gets or sets the current transform of the drawing context. |
| `Clear(Color)` | Clears the render target to the specified color. |
| `DrawBitmap(IBitmapImpl, double, Rect, Rect)` | Draws a bitmap image with an opacity, from a source rect into a destination rect. |
| `DrawBitmap(IBitmapImpl, IBrush, Rect, Rect)` | Draws a bitmap image using an opacity mask (mask brush + mask dest rect) into a destination rect. |
| `DrawLine(IPen?, Point, Point)` | Draws a line between two points with the stroke pen. |
| `DrawGeometry(IBrush?, IPen?, IGeometryImpl)` | Draws a geometry with a fill brush and stroke pen. |
| `DrawRectangle(IBrush?, IPen?, RoundedRect, BoxShadows)` | Draws a (rounded) rectangle with brush/pen; either may be null; optional box-shadow parameters. |
| `DrawRegion(IBrush?, IPen?, IPlatformRenderInterfaceRegion)` | Draws the specified region with brush/pen; either may be null. |
| `DrawEllipse(IBrush?, IPen?, Rect)` | Draws an ellipse with brush/pen; either may be null. |
| `DrawGlyphRun(IBrush?, IGlyphRunImpl)` | Draws a glyph run using the foreground brush. |
| `CreateLayer(PixelSize)` | Creates a render layer (`IDrawingContextLayerImpl`) tuned for the current render target. |
| `PushClip(Rect)` / `PushClip(RoundedRect)` / `PushClip(IPlatformRenderInterfaceRegion)` | Pushes a rectangular, rounded-rectangular, or region clip. |
| `PopClip()` | Pops the latest pushed clip. |
| `PushLayer(Rect)` | Forces rendering onto an intermediate surface within the bounds. |
| `PopLayer()` | Pops the latest pushed intermediate surface layer. |
| `PushOpacity(double, Rect?)` | Pushes an opacity value, applied within the optional bounds. |
| `PopOpacity()` | Pops the latest pushed opacity value. |
| `PushOpacityMask(IBrush, Rect)` | Pushes an opacity mask over the given bounds. |
| `PopOpacityMask()` | Pops the latest pushed opacity mask. |
| `PushGeometryClip(IGeometryImpl)` | Pushes a geometry clip. |
| `PopGeometryClip()` | Pops the latest pushed geometry clip. |
| `PushRenderOptions(RenderOptions)` / `PopRenderOptions()` | Pushes/pops render options. |
| `PushTextOptions(TextOptions)` / `PopTextOptions()` | Pushes/pops text options. |
| `GetFeature(Type)` | Attempts to get an optional feature from the drawing context implementation. |

### IDrawingContextImplWithEffects

- File: `src/Avalonia.Base/Platform/IDrawingContextImpl.cs`
- Declaration: `[PrivateApi] public interface IDrawingContextImplWithEffects : IDrawingContextImpl`

```csharp
void PushEffect(Rect? clipRect, IEffect effect);
void PopEffect();
```

- `PushEffect(Rect?, IEffect)` — Pushes an effect (optionally clipped to `clipRect`) onto the context.
- `PopEffect()` — Pops the latest pushed effect.

### IDrawingContextLayerImpl

- File: `src/Avalonia.Base/Platform/IDrawingContextImpl.cs`
- Declaration: `public interface IDrawingContextLayerImpl : IBitmapImpl`

```csharp
void Blit(IDrawingContextImpl context);
bool CanBlit { get; }
bool IsCorrupted { get; }
IDrawingContextImpl CreateDrawingContext();
```

| Member | Description |
|--------|-------------|
| `Blit(IDrawingContextImpl)` | Does an optimized blit with the `Src` blend mode. |
| `CanBlit` | True if the layer supports optimized blit. |
| `IsCorrupted` | True if the render target is no longer usable and needs recreation. |
| `CreateDrawingContext()` | Creates a drawing context matching the properties of the context this layer was created from. |

### IDrawingContextLayerWithRenderContextAffinityImpl

- File: `src/Avalonia.Base/Platform/IDrawingContextImpl.cs`
- Declaration: `public interface IDrawingContextLayerWithRenderContextAffinityImpl : IDrawingContextLayerImpl`

```csharp
bool HasRenderContextAffinity { get; }
IBitmapImpl CreateNonAffinedSnapshot();
```

- `HasRenderContextAffinity` — Whether the layer is bound to a specific render context.
- `CreateNonAffinedSnapshot()` — Produces a context-independent bitmap snapshot.

### DrawingContextImplExtensions (static)

- File: `src/Avalonia.Base/Platform/IDrawingContextImpl.cs`
- Declaration: `public static class DrawingContextImplExtensions`

```csharp
public static T? GetFeature<T>(this IDrawingContextImpl context) where T : class;
```

- `GetFeature<T>(IDrawingContextImpl)` — Strongly-typed wrapper over `IDrawingContextImpl.GetFeature(Type)`.

### IDrawingContextWithAcrylicLikeSupport

- File: `src/Avalonia.Base/Platform/IDrawingContextWithAcrylicLikeSupport.cs`
- Declaration: `[PrivateApi] public interface IDrawingContextWithAcrylicLikeSupport`

```csharp
void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect);
```

- `DrawRectangle(IExperimentalAcrylicMaterial, RoundedRect)` — Draws a rounded rectangle filled with an acrylic-like material.

---

## Factory & context

### IPlatformRenderInterface

- File: `src/Avalonia.Base/Platform/IPlatformRenderInterface.cs`
- Declaration: `[Unstable, PrivateApi] public interface IPlatformRenderInterface`

```csharp
IGeometryImpl CreateEllipseGeometry(Rect rect);
IGeometryImpl CreateLineGeometry(Point p1, Point p2);
IGeometryImpl CreateRectangleGeometry(Rect rect);
IStreamGeometryImpl CreateStreamGeometry();
IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<IGeometryImpl> children);
IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, IGeometryImpl g1, IGeometryImpl g2);
IGeometryImpl BuildGlyphRunGeometry(GlyphRun glyphRun);
IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi);
IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat);
IBitmapImpl LoadBitmap(string fileName);
IBitmapImpl LoadBitmap(Stream stream);
IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
IWriteableBitmapImpl LoadWriteableBitmap(string fileName);
IWriteableBitmapImpl LoadWriteableBitmap(Stream stream);
IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality);
IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride);
IGlyphRunImpl CreateGlyphRun(GlyphTypeface glyphTypeface, double fontRenderingEmSize, IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin);
IPlatformRenderInterfaceContext CreateBackendContext(IPlatformGraphicsContext? graphicsApiContext);
bool SupportsIndividualRoundRects { get; }
public AlphaFormat DefaultAlphaFormat { get; }
public PixelFormat DefaultPixelFormat { get; }
bool IsSupportedBitmapPixelFormat(PixelFormat format);
bool SupportsRegions { get; }
IPlatformRenderInterfaceRegion CreateRegion();
```

| Member | Description |
|--------|-------------|
| `CreateEllipseGeometry(Rect)` | Creates an ellipse geometry implementation. |
| `CreateLineGeometry(Point, Point)` | Creates a line geometry implementation. |
| `CreateRectangleGeometry(Rect)` | Creates a rectangle geometry implementation. |
| `CreateStreamGeometry()` | Creates a stream geometry implementation. |
| `CreateGeometryGroup(FillRule, IReadOnlyList<IGeometryImpl>)` | Creates a geometry group from child geometries and a fill rule. |
| `CreateCombinedGeometry(GeometryCombineMode, IGeometryImpl, IGeometryImpl)` | Creates a combined geometry from two geometries. |
| `BuildGlyphRunGeometry(GlyphRun)` | Creates the combined geometry of all glyphs in a glyph run. |
| `CreateRenderTargetBitmap(PixelSize, Vector)` | Creates a render-target bitmap implementation. |
| `CreateWriteableBitmap(PixelSize, Vector, PixelFormat, AlphaFormat)` | Creates a writeable bitmap implementation. |
| `LoadBitmap(string)` / `LoadBitmap(Stream)` | Loads a bitmap from a file path or stream. |
| `LoadWriteableBitmapToWidth(Stream, int, BitmapInterpolationMode)` | Loads a writeable bitmap scaled to a width, preserving aspect ratio. |
| `LoadWriteableBitmapToHeight(Stream, int, BitmapInterpolationMode)` | Loads a writeable bitmap scaled to a height, preserving aspect ratio. |
| `LoadWriteableBitmap(string)` / `LoadWriteableBitmap(Stream)` | Loads a writeable bitmap from a file path or stream. |
| `LoadBitmapToWidth(Stream, int, BitmapInterpolationMode)` | Loads a bitmap scaled to a width, preserving aspect ratio. |
| `LoadBitmapToHeight(Stream, int, BitmapInterpolationMode)` | Loads a bitmap scaled to a height, preserving aspect ratio. |
| `ResizeBitmap(IBitmapImpl, PixelSize, BitmapInterpolationMode)` | Resizes an existing bitmap to a destination size. |
| `LoadBitmap(PixelFormat, AlphaFormat, IntPtr, PixelSize, Vector, int)` | Loads a bitmap from raw pixels in memory (pointer + stride). |
| `CreateGlyphRun(GlyphTypeface, double, IReadOnlyList<GlyphInfo>, Point)` | Creates a platform glyph-run implementation. |
| `CreateBackendContext(IPlatformGraphicsContext?)` | Creates a backend-specific context from a low-level graphics context. |
| `SupportsIndividualRoundRects` | Whether the platform directly supports rounded-corner rectangles. |
| `DefaultAlphaFormat` | Default `AlphaFormat` on this platform. |
| `DefaultPixelFormat` | Default `PixelFormat` on this platform. |
| `IsSupportedBitmapPixelFormat(PixelFormat)` | Whether the given pixel format is supported for bitmaps. |
| `SupportsRegions` | Whether region operations are supported. |
| `CreateRegion()` | Creates a platform region implementation. |

### IPlatformRenderInterfaceContext

- File: `src/Avalonia.Base/Platform/IPlatformRenderInterface.cs`
- Declaration: `[Unstable, PrivateApi] public interface IPlatformRenderInterfaceContext : IOptionalFeatureProvider, IDisposable`

```csharp
IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);
IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling, bool enableTextAntialiasing);
bool IsLost { get; }
IReadOnlyDictionary<Type, object> PublicFeatures { get; }
public PixelSize? MaxOffscreenRenderTargetPixelSize { get; }
bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) => true;
```

| Member | Description |
|--------|-------------|
| `CreateRenderTarget(IEnumerable<IPlatformRenderSurface>)` | Creates an `IRenderTarget` from a list of native output surfaces. |
| `CreateOffscreenRenderTarget(PixelSize, Vector, bool)` | Creates an offscreen render target (layer) at a pixel size + scaling, optionally with text antialiasing. |
| `IsLost` | Indicates the context is no longer usable (thread-safe). |
| `PublicFeatures` | Features consumable while the context isn't active (e.g. from the UI thread). |
| `MaxOffscreenRenderTargetPixelSize` | Maximum offscreen render-target size, or null if unlimited. |
| `IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface>)` | Whether a render target can be created for the surfaces and the preferred surface is ready (default `true`). |

---

## Render targets

### IRenderTarget

- File: `src/Avalonia.Base/Platform/IRenderTarget.cs`
- Declaration: `[PrivateApi] public interface IRenderTarget : IDisposable`

```csharp
RenderTargetProperties Properties { get; }
IDrawingContextImpl CreateDrawingContext(RenderTargetSceneInfo sceneInfo, out RenderTargetDrawingContextProperties properties);
PlatformRenderTargetState PlatformRenderTargetState => PlatformRenderTargetState.Ready;

public record struct RenderTargetSceneInfo(PixelSize Size, double Scaling, Size LogicalSize, CompositionTransparencyLevel TransparencyLevel)
{
    public RenderTargetSceneInfo(PixelSize size, double scaling, CompositionTransparencyLevel transparencyLevel);
}
```

| Member | Description |
|--------|-------------|
| `Properties` | Gets the properties of the render target. |
| `CreateDrawingContext(RenderTargetSceneInfo, out RenderTargetDrawingContextProperties)` | Creates an `IDrawingContextImpl` for one rendering session; `sceneInfo` describes the scene (may affect framebuffer size), and `properties` is returned. |
| `PlatformRenderTargetState` (default `Ready`) | Gets the current readiness state of the render target. |
| `RenderTargetSceneInfo` (nested `record struct`) | Scene description: `PixelSize Size`, `double Scaling`, `Size LogicalSize`, `CompositionTransparencyLevel TransparencyLevel`; secondary ctor derives `LogicalSize` from `size.ToSize(scaling)`. |

### RenderTargetProperties

- File: `src/Avalonia.Base/Platform/RenderTargetProperties.cs`
- Declaration: `[PrivateApi] public struct RenderTargetProperties`

```csharp
public bool RetainsPreviousFrameContents { get; init; }
public bool IsSuitableForDirectRendering { get; init; }
```

- `RetainsPreviousFrameContents` — Render-target contents are preserved between `CreateDrawingContext` calls (retained CPU framebuffers, sequential swapchains).
- `IsSuitableForDirectRendering` — The target can be used without `CreateLayer` (not always true, e.g. GL framebuffers without a stencil attachment).

### RenderTargetDrawingContextProperties

- File: `src/Avalonia.Base/Platform/RenderTargetProperties.cs`
- Declaration: `[PrivateApi] public struct RenderTargetDrawingContextProperties`

```csharp
public bool PreviousFrameIsRetained { get; init; }
```

- `PreviousFrameIsRetained` — The drawing context targets a surface that preserved its contents since the previous frame.

### PlatformRenderTargetState

- File: `src/Avalonia.Base/Platform/RenderTargetProperties.cs`
- Declaration: `[PrivateApi] public readonly struct PlatformRenderTargetState`

```csharp
public bool IsReady { get; init; }
public bool WillWakeUpRenderLoopWhenReady { get; init; }
public bool IsCorrupted { get; init; }
public static PlatformRenderTargetState Ready => new() { IsReady = true };
public static PlatformRenderTargetState NotReadyTryLater => default;
public static PlatformRenderTargetState Corrupted => new() { IsCorrupted = true, IsReady = true };
public static PlatformRenderTargetState Disposed => new() { IsCorrupted = true };
public static PlatformRenderTargetState NotReadyWillWakeupRenderLoop => new() { WillWakeUpRenderLoopWhenReady = true };
```

| Member | Description |
|--------|-------------|
| `IsReady` | Whether the render target is currently ready to render to. |
| `WillWakeUpRenderLoopWhenReady` | Not ready now, but will wake the render loop when ready (compositor should stop polling). |
| `IsCorrupted` | The render target is no longer usable and needs recreation. |
| `Ready` / `NotReadyTryLater` / `Corrupted` / `Disposed` / `NotReadyWillWakeupRenderLoop` | Predefined readiness states. |

---

## Bitmaps

### IBitmapImpl

- File: `src/Avalonia.Base/Platform/IBitmapImpl.cs`
- Declaration: `[PrivateApi] public interface IBitmapImpl : IDisposable`

```csharp
Vector Dpi { get; }
PixelSize PixelSize { get; }
int Version { get; }
void Save(Stream stream, BitmapEncoderOptions options);
```

| Member | Description |
|--------|-------------|
| `Dpi` | Gets the dots-per-inch of the image. |
| `PixelSize` | Gets the size of the bitmap in device pixels. |
| `Version` | Version of the pixel data. |
| `Save(Stream, BitmapEncoderOptions)` | Saves the bitmap to a stream using the given encoder options. |

### IReadableBitmapImpl

- File: `src/Avalonia.Base/Platform/IReadableBitmapImpl.cs`
- Declaration: `[PrivateApi] public interface IReadableBitmapImpl : IBitmapImpl`

```csharp
PixelFormat? Format { get; }
AlphaFormat? AlphaFormat { get; }
ILockedFramebuffer Lock();
```

| Member | Description |
|--------|-------------|
| `Format` | The bitmap's pixel format, if known. |
| `AlphaFormat` | The bitmap's alpha format, if known. |
| `Lock()` | Locks the bitmap and returns an `ILockedFramebuffer` for CPU access. |

### IWriteableBitmapImpl

- File: `src/Avalonia.Base/Platform/IWriteableBitmapImpl.cs`
- Declaration: `[PrivateApi] public interface IWriteableBitmapImpl : IBitmapImpl, IReadableBitmapImpl`

No members of its own — a marker combining readable + writeable bitmap semantics.

### IRenderTargetBitmapImpl

- File: `src/Avalonia.Base/Platform/IRenderTargetBitmapImpl.cs`
- Declaration: `[Unstable] public interface IRenderTargetBitmapImpl : IReadableBitmapImpl`

```csharp
IDrawingContextImpl CreateDrawingContext();
```

- `CreateDrawingContext()` — Creates a drawing context that renders into this bitmap.

### ILockedFramebuffer

- File: `src/Avalonia.Base/Platform/ILockedFramebuffer.cs`
- Declaration: `public interface ILockedFramebuffer : IDisposable`

```csharp
IntPtr Address { get; }
PixelSize Size { get; }
int RowBytes { get; }
Vector Dpi { get; }
PixelFormat Format { get; }
AlphaFormat AlphaFormat { get; }
```

| Member | Description |
|--------|-------------|
| `Address` | Address of the first pixel. |
| `Size` | Framebuffer size in device pixels. |
| `RowBytes` | Number of bytes per row (stride). |
| `Dpi` | DPI of the underlying screen. |
| `Format` | Pixel format. |
| `AlphaFormat` | Alpha format. |

---

## Geometry

### IGeometryImpl

- File: `src/Avalonia.Base/Platform/IGeometryImpl.cs`
- Declaration: `[Unstable] public interface IGeometryImpl : IRenderDataGeometry` (base `IRenderDataGeometry` in `Avalonia.Rendering.Composition.Drawing`; `IGeometryImpl` provides the explicit default `IGeometryImpl IRenderDataGeometry.GeometryImpl => this;`)

```csharp
IGeometryImpl IRenderDataGeometry.GeometryImpl => this;
Rect Bounds { get; }
double ContourLength { get; }
Rect GetRenderBounds(IPen? pen);
IGeometryImpl GetWidenedGeometry(IPen pen);
bool FillContains(Point point);
IGeometryImpl? Intersect(IGeometryImpl geometry);
bool StrokeContains(IPen? pen, Point point);
ITransformedGeometryImpl WithTransform(Matrix transform);
bool TryGetPointAtDistance(double distance, out Point point);
bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent);
bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, [NotNullWhen(true)] out IGeometryImpl? segmentGeometry);
```

| Member | Description |
|--------|-------------|
| `GeometryImpl` (from base) | Returns itself as the underlying platform geometry. |
| `Bounds` | The geometry's bounding rectangle. |
| `ContourLength` | Total length of all contours placed in a straight line. |
| `GetRenderBounds(IPen?)` | Bounding rectangle when stroked with the given pen (pen may be null). |
| `GetWidenedGeometry(IPen)` | The geometry describing the stroke outline produced by the pen. |
| `FillContains(Point)` | Whether the geometry's fill contains the point. |
| `Intersect(IGeometryImpl)` | Intersection with another geometry, or null on failure. |
| `StrokeContains(IPen?, Point)` | Whether the geometry's stroke contains the point. |
| `WithTransform(Matrix)` | Clones the geometry with a transform (stroke thickness unchanged). |
| `TryGetPointAtDistance(double, out Point)` | Gets the point at a contour distance. |
| `TryGetPointAndTangentAtDistance(double, out Point, out Point)` | Gets the point and tangent at a contour distance. |
| `TryGetSegment(double, double, bool, out IGeometryImpl?)` | Snips a sub-path between two contour distances; `startOnBeginFigure` starts the result with a BeginFigure. |

### ITransformedGeometryImpl

- File: `src/Avalonia.Base/Platform/ITransformedGeometryImpl.cs`
- Declaration: `[Unstable] public interface ITransformedGeometryImpl : IGeometryImpl`

```csharp
IGeometryImpl SourceGeometry { get; }
Matrix Transform { get; }
```

| Member | Description |
|--------|-------------|
| `SourceGeometry` | The source geometry the transform is applied to. |
| `Transform` | The applied transform (does not transform stroke thickness). |

### IStreamGeometryImpl

- File: `src/Avalonia.Base/Platform/IStreamGeometryImpl.cs`
- Declaration: `[Unstable] public interface IStreamGeometryImpl : IGeometryImpl`

```csharp
IStreamGeometryImpl Clone();
IStreamGeometryContextImpl Open();
```

| Member | Description |
|--------|-------------|
| `Clone()` | Clones the geometry. |
| `Open()` | Opens the geometry to define it via an `IStreamGeometryContextImpl`. |

### IGeometryContext

- File: `src/Avalonia.Base/Platform/IGeometryContext.cs`
- Declaration: `public interface IGeometryContext : IDisposable`

```csharp
void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true);
void BeginFigure(Point startPoint, bool isFilled = true);
void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true);
void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true);
void LineTo(Point point, bool isStroked = true);
void EndFigure(bool isClosed);
void SetFillRule(FillRule fillRule);
```

| Member | Description |
|--------|-------------|
| `ArcTo(Point, Size, double, bool, SweepDirection, bool)` | Draws an arc to a point; `size` is the oval radii, `rotationAngle` in radians, plus large-arc/sweep-direction flags. |
| `BeginFigure(Point, bool)` | Begins a new figure at a start point, optionally filled. |
| `CubicBezierTo(Point, Point, Point, bool)` | Draws a cubic Bézier curve with two control points and an end point. |
| `QuadraticBezierTo(Point, Point, bool)` | Draws a quadratic Bézier curve with one control point and an end point. |
| `LineTo(Point, bool)` | Draws a line to a point. |
| `EndFigure(bool)` | Ends the current figure, optionally closing it. |
| `SetFillRule(FillRule)` | Sets the winding rule (default EvenOdd); call before any `BeginFigure`. |

### IStreamGeometryContextImpl

- File: `src/Avalonia.Base/Platform/IStreamGeometryContextImpl.cs`
- Declaration: `[Unstable] public interface IStreamGeometryContextImpl : IGeometryContext`

No members of its own — the platform-specific specialization of `IGeometryContext` returned by `IStreamGeometryImpl.Open()`.

---

## Text & glyphs

### IGlyphRunImpl

- File: `src/Avalonia.Base/Platform/IGlyphRunImpl.cs`
- Declaration: `[Unstable] public interface IGlyphRunImpl : IDisposable`

```csharp
double FontRenderingEmSize { get; }
Point BaselineOrigin { get; }
Rect Bounds { get; }
IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit);
```

| Member | Description |
|--------|-------------|
| `FontRenderingEmSize` | The em size used for rendering the glyph run. |
| `BaselineOrigin` | The baseline origin of the glyph run. |
| `Bounds` | The conservative bounding box of the glyph run. |
| `GetIntersections(float, float)` | Gets intersections within the specified lower/upper limit. |

### IFontManagerImpl

- File: `src/Avalonia.Base/Platform/IFontManagerImpl.cs`
- Declaration: `[Unstable] public interface IFontManagerImpl`

```csharp
string GetDefaultFontFamilyName();
string[] GetInstalledFontFamilyNames(bool checkForUpdates = false);
bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, string? familyName, CultureInfo? culture, [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface);
bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface);
bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface);
bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces);
```

| Member | Description |
|--------|-------------|
| `GetDefaultFontFamilyName()` | Gets the system's default font family name. |
| `GetInstalledFontFamilyNames(bool)` | Gets all installed font families; `checkForUpdates` refreshes the collection. |
| `TryMatchCharacter(int, FontStyle, FontWeight, FontStretch, string?, CultureInfo?, out IPlatformTypeface?)` | Matches a codepoint to a typeface supporting the given font properties. |
| `TryCreateGlyphTypeface(string, FontStyle, FontWeight, FontStretch, out IPlatformTypeface?)` | Gets a glyph typeface for the given family and style parameters. |
| `TryCreateGlyphTypeface(Stream, FontSimulations, out IPlatformTypeface?)` | Creates a glyph typeface from a font-data stream, with optional style simulations. |
| `TryGetFamilyTypefaces(string, out IReadOnlyList<Typeface>?)` | Gets the list of typefaces for a family name. |

### ITextShaperImpl

- File: `src/Avalonia.Base/Platform/ITextShaperImpl.cs`
- Declaration: `[NotClientImplementable] public interface ITextShaperImpl`

```csharp
ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options);
ITextShaperTypeface CreateTypeface(GlyphTypeface glyphTypeface);
```

| Member | Description |
|--------|-------------|
| `ShapeText(ReadOnlyMemory<char>, TextShaperOptions)` | Shapes a text region and returns a shaped glyph buffer. |
| `CreateTypeface(GlyphTypeface)` | Creates a text-shaper typeface based on the given glyph typeface. |

### IPlatformTypeface

- File: `src/Avalonia.Base/Media/IPlatformTypeface.cs`
- Declaration: `[NotClientImplementable] public interface IPlatformTypeface : IFontMemory` (base `IFontMemory : IDisposable`, `[NotClientImplementable]`)

```csharp
string FamilyName { get; }
FontWeight Weight { get; }
FontStyle Style { get; }
FontStretch Stretch { get; }
FontSimulations FontSimulations { get; }
bool TryGetStream([NotNullWhen(true)] out Stream? stream);
// from IFontMemory:
bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table);
```

| Member | Description |
|--------|-------------|
| `FamilyName` | Font family name (may be an alias/fallback used at creation). |
| `Weight` | Designed weight of the font. |
| `Style` | Font style. |
| `Stretch` | Font stretch. |
| `FontSimulations` | Algorithmic style simulations applied. |
| `TryGetStream(out Stream?)` | Returns the font file stream if obtainable. |
| `TryGetTable(OpenTypeTag, out ReadOnlyMemory<byte>)` (from `IFontMemory`) | Retrieves the memory block for an OpenType table tag. |

### ITextShaperTypeface

- File: `src/Avalonia.Base/Media/ITextShaperTypeface.cs`
- Declaration: `[NotClientImplementable] public interface ITextShaperTypeface : IDisposable`

No members of its own — an opaque, disposable shaper-typeface handle produced by `ITextShaperImpl.CreateTypeface`.

---

## Regions

### IPlatformRenderInterfaceRegion

- File: `src/Avalonia.Base/Platform/IPlatformRenderInterfaceRegion.cs`
- Declaration: `[Unstable, PrivateApi] public interface IPlatformRenderInterfaceRegion : IDisposable`

```csharp
void AddRect(LtrbPixelRect rect);
void Reset();
bool IsEmpty { get; }
LtrbPixelRect Bounds { get; }
IList<LtrbPixelRect> Rects { get; }
bool Intersects(LtrbRect rect);
bool Contains(Point pt);
```

| Member | Description |
|--------|-------------|
| `AddRect(LtrbPixelRect)` | Adds a rectangle to the region. |
| `Reset()` | Clears the region. |
| `IsEmpty` | Whether the region contains nothing. |
| `Bounds` | Bounding rectangle of the region. |
| `Rects` | The list of rectangles composing the region. |
| `Intersects(LtrbRect)` | Whether the region intersects the given rectangle. |
| `Contains(Point)` | Whether the region contains the point. |

---

## Brushes & pens

Brushes and pens are the declarative paint descriptions the framework passes into `IDrawingContextImpl`. They live in `Avalonia.Media`, are almost all `[NotClientImplementable]`, and have immutable variants (`IImmutable*`) safe to hand across threads.

### IBrush

- File: `src/Avalonia.Base/Media/IBrush.cs`
- Declaration: `[TypeConverter(typeof(BrushConverter))] [NotClientImplementable] public interface IBrush`

```csharp
double Opacity { get; }
ITransform? Transform { get; }
RelativePoint TransformOrigin { get; }
```

| Member | Description |
|--------|-------------|
| `Opacity` | The opacity of the brush. |
| `Transform` | The transform of the brush. |
| `TransformOrigin` | The origin of the brush's `Transform`. |

### IImmutableBrush

- File: `src/Avalonia.Base/Media/IImmutableBrush.cs`
- Declaration: `public interface IImmutableBrush : IBrush`

No members of its own — marks an immutable brush safe for use across threading contexts.

### ISolidColorBrush / IImmutableSolidColorBrush

- File: `src/Avalonia.Base/Media/ISolidColorBrush.cs`
- Declarations:
  - `[NotClientImplementable] public interface ISolidColorBrush : IBrush`
  - `[NotClientImplementable] public interface IImmutableSolidColorBrush : ISolidColorBrush, IImmutableBrush`

```csharp
// ISolidColorBrush
Color Color { get; }
// IImmutableSolidColorBrush : ISolidColorBrush, IImmutableBrush  (no additional members)
```

- `Color` — The color of the brush.
- `IImmutableSolidColorBrush` — Immutable solid-color brush (no additional members).

### IGradientBrush

- File: `src/Avalonia.Base/Media/IGradientBrush.cs`
- Declaration: `[NotClientImplementable] public interface IGradientBrush : IBrush`

```csharp
IReadOnlyList<IGradientStop> GradientStops { get; }
GradientSpreadMethod SpreadMethod { get; }
```

| Member | Description |
|--------|-------------|
| `GradientStops` | The brush's gradient stops. |
| `SpreadMethod` | How a gradient that doesn't fill the destination bounds is spread. |

### ITileBrush

- File: `src/Avalonia.Base/Media/ITileBrush.cs`
- Declaration: `[NotClientImplementable] public interface ITileBrush : IBrush`

```csharp
AlignmentX AlignmentX { get; }
AlignmentY AlignmentY { get; }
RelativeRect DestinationRect { get; }
RelativeRect SourceRect { get; }
Stretch Stretch { get; }
TileMode TileMode { get; }
```

| Member | Description |
|--------|-------------|
| `AlignmentX` | Horizontal alignment of a tile in the destination. |
| `AlignmentY` | Vertical alignment of a tile in the destination. |
| `DestinationRect` | The destination rectangle in which to paint a tile. |
| `SourceRect` | The source rectangle that will be displayed. |
| `Stretch` | How the source rect is stretched to fill the destination. |
| `TileMode` | The brush's tile mode. |

### IImageBrush / IImageBrushSource

- File: `src/Avalonia.Base/Media/IImageBrush.cs`
- Declarations:
  - `[NotClientImplementable] public interface IImageBrush : ITileBrush`
  - `[NotClientImplementable] public interface IImageBrushSource`

```csharp
// IImageBrush
IImageBrushSource? Source { get; }
// IImageBrushSource
internal IRef<IBitmapImpl>? Bitmap { get; }
```

| Member | Description |
|--------|-------------|
| `IImageBrush.Source` | The image to draw. |
| `IImageBrushSource.Bitmap` (internal) | The underlying ref-counted bitmap implementation. |

### ISceneBrush / ISceneBrushContent

- File: `src/Avalonia.Base/Media/ISceneBrush.cs`
- Declarations:
  - `[NotClientImplementable] public interface ISceneBrush : ITileBrush`
  - `[NotClientImplementable] public interface ISceneBrushContent : IImmutableBrush, IDisposable`

```csharp
// ISceneBrush
ISceneBrushContent? CreateContent();
// ISceneBrushContent
ITileBrush Brush { get; }
Rect Rect { get; }
void Render(IDrawingContextImpl context, Matrix? transform);
internal bool UseScalableRasterization { get; }
```

| Member | Description |
|--------|-------------|
| `ISceneBrush.CreateContent()` | Snapshots the scene into renderable content. |
| `ISceneBrushContent.Brush` | The originating tile brush. |
| `ISceneBrushContent.Rect` | The content's rectangle. |
| `ISceneBrushContent.Render(IDrawingContextImpl, Matrix?)` | Renders the content into a drawing context with an optional transform. |
| `ISceneBrushContent.UseScalableRasterization` (internal) | Whether scalable rasterization should be used. |

### IPen

- File: `src/Avalonia.Base/Media/IPen.cs`
- Declaration: `[NotClientImplementable] public interface IPen`

```csharp
IBrush? Brush { get; }
IDashStyle? DashStyle { get; }
PenLineCap LineCap { get; }
PenLineJoin LineJoin { get; }
double MiterLimit { get; }
double Thickness { get; }
```

| Member | Description |
|--------|-------------|
| `Brush` | The brush used to draw the stroke. |
| `DashStyle` | The dash pattern style. |
| `LineCap` | The shape used on both ends of a line. |
| `LineJoin` | How consecutive line/curve segments are joined. |
| `MiterLimit` | The limit of thickness on a mitered corner. |
| `Thickness` | The stroke thickness. |

### IDashStyle

- File: `src/Avalonia.Base/Media/IDashStyle.cs`
- Declaration: `[NotClientImplementable] public interface IDashStyle`

```csharp
IReadOnlyList<double>? Dashes { get; }
double Offset { get; }
```

| Member | Description |
|--------|-------------|
| `Dashes` | The lengths of alternating dashes and gaps. |
| `Offset` | How far into the dash sequence the stroke starts. |

---

## Effects

Effects cross the boundary as opaque `Avalonia.Media` handles; the drawing-context specialization `IDrawingContextImplWithEffects` (see Drawing context) consumes them.

### IEffect

- File: `src/Avalonia.Base/Media/Effects/IEffect.cs`
- Declaration: `[TypeConverter(typeof(EffectConverter))] [NotClientImplementable] public interface IEffect`

No members — the marker base for all effects (e.g. blur, drop shadow).

### IMutableEffect

- File: `src/Avalonia.Base/Media/Effects/IEffect.cs`
- Declaration: `public interface IMutableEffect : IEffect`

```csharp
internal IImmutableEffect ToImmutable();
```

- `ToImmutable()` (internal) — Creates an immutable clone of the effect.

### IImmutableEffect

- File: `src/Avalonia.Base/Media/Effects/IEffect.cs`
- Declaration: `public interface IImmutableEffect : IEffect, IEquatable<IEffect>`

No members of its own — an immutable, equatable effect suitable for thread-safe sharing and comparison.
