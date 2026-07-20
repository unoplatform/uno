# Uno Pluggable Drawing-Backend Abstraction (work in progress)

> The abstraction built on branch `feature/drawing-backend-abstraction`. This is the current state of an
> in-progress effort to put Uno's Skia rendering behind interfaces so alternate 2D backends can be plugged
> in; the SkiaSharp backend is the default. Signatures are transcribed from source. See
> `doc/avalonia-rendering-abstraction.md` for the reference design this was informed by, and
> `doc/uno-composition-skia-usage.md` for the raw Skia coupling being factored out.

## Goal

Make the **core framework** render without a hard dependency on SkiaSharp: every type that crosses the
"how do I draw / decode / measure" boundary is a neutral interface or value type, and SkiaSharp is *one*
implementation behind them. A second, from-scratch implementation of each seam (a managed font backend, a
managed image decoder, a managed SVG engine) exists both to prove the seam is genuinely neutral and to move
the core toward being Skia-free.

## Design in one paragraph

We took a **Skia-shaped but backend-neutral** seam (rather than a fully-declarative one). Everything crossing
the boundary is split by lifetime/role:

- **Transient paint** (color, stroke width, blend, AA) crosses **by value**, passed **inline per draw verb**.
  There is no combined paint struct — each verb takes exactly the inputs it honors, and scenarios are
  distinct overloads (e.g. a solid-color fill vs. a shader fill are separate methods).
- **Expensive resources** (geometry, shaders, color/effect filters, images, image frames, fonts) cross as
  **opaque handles** manufactured by a factory (`IDrawingBackend`) or a decode call; the framework holds and
  caches them without inspecting their internals. The backend downcasts internally.
- **Per-visual/frame retained state** (Skia's `SKPicture`) crosses as an opaque `IRenderData` the backend
  owns; composition never inspects it. This is behind the **optional** `IRetainedRenderingSession` capability
  — a backend that doesn't advertise it is simply re-drawn every frame.
- **Value types are neutral** — `Windows.Foundation.Rect/Size`, `Windows.UI.Color`,
  `System.Numerics.Vector2/Matrix3x2/Matrix4x4` — no SkiaSharp type appears on any pluggable interface.
- **The render cycle stays in Uno**, not the backend. `CompositionTarget` owns scheduling/vsync/threading and
  the visual-tree walk; the backend is a passive two-phase participant (record, then present).

Layers, bottom to top: **resource factory** (`IDrawingBackend`) → **drawing session** (`IDrawingSession`,
consuming value paint + handles; optionally `IRetainedRenderingSession` for recording) → **frame lifecycle**
(`IRenderBackend`: record → `IRenderData` → present).

**Conventions.** Types are `internal`, namespace `Uno.UI.Composition.Drawing`, files under
`src/Uno.UI.Composition/Composition/Uno/Drawing/*.skia.cs` (Skia target), shaped for a later deliberate flip
to `public`. Backend-neutral geometry currency is `IGeometry`; `SKPath`/`SKImage`/`SKFont` etc. live only
inside the Skia implementation classes and are never exposed on an interface.

---

## Registration & factory

### `IDrawingBackend` — the resource factory
Manufactures the stateful handles that cross the boundary; also owns image decode/upload and offscreen
rendering. Current surface:

```csharp
IPathBuilder CreatePathBuilder();
IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder();
IGeometry CreateRectangleGeometry(Rect rect);

IImage RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);
bool TryDecodeImage(Stream stream, int? targetWidth, int? targetHeight, out IImageFrames? frames);
IImageFrames CreateImageFrame(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul);

IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode);
IColorFilter CreateColorMatrixColorFilter(float[] matrix);
IEffectFilter? CreateEffectFilter(IGraphicsEffect effect, Rect bounds, Func<string, CompositionBrush?> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput);
IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color);
```

### `DrawingBackend` (static)
Process-wide holder: `DrawingBackend.Current` resolves to `SkiaDrawingBackend` by default;
`DrawingBackend.Register(IDrawingBackend)` swaps it before the first frame.

---

## Drawing session — `IDrawingSession`

Immediate-mode, stateful canvas. Transform stack, clipping, layers, and per-scenario draw verbs:

```csharp
Matrix4x4 TotalMatrix { get; }
void SetMatrix(in Matrix4x4 m); void Concat(in Matrix4x4 m); void Translate(float dx, float dy); void Scale(float sx, float sy);
int Save(); int SaveCount { get; } void Restore(); void RestoreToCount(int count);
void SaveLayer(bool aa = false); void SaveLayer(IColorFilter f, bool aa = false); void SaveLayer(BlendMode b, bool aa = false); void SaveLayer(IEffectFilter f);
void ClipRect(in Rect r, ClipOperation op = Intersect, bool aa = false);
void ClipRoundRect(in RoundRectangle r, ClipOperation op = Intersect, bool aa = false);
void ClipPath(IGeometry g, ClipOperation op = Intersect, bool aa = false);
void Clear(Color color);
void DrawRect(in Rect r, Color color, bool aa = false);        // solid
void DrawRect(in Rect r, IShader shader, bool aa = false);      // shader (carries its own alpha)
void DrawPath(IGeometry g, Color color, bool aa = false);
void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool aa = false);
void StrokePath(IGeometry g, Color color, float strokeWidth, bool aa = false);
void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool aa = false);
void DrawImage(IImage img, float x, float y, ImageSampling s, float opacity = 1f, bool aa = false);
void DrawImage(IImage img, float x, float y, ImageSampling s, IColorFilter filter, bool aa = false);
void DrawImageNineSlice(IImage img, in Rect centerSlice, in Rect destination, bool centerHollow, bool aa = false);
void DrawEffectBackdrop(IEffectFilter filter, float opacity);
```

Intent-over-technique: the interface says *what* (a shadow, a gradient fill, a faded image); the backend
chooses *how* (mask filter, SDF, layer). Retained-mode concerns are deliberately **not** here.

### Retained rendering (optional) — `IRetainedRenderingSession`
```csharp
ICommandRecorder CreateRecording();   // nested session captured as IRenderData
void Replay(IRenderData data);
```
`ICommandRecorder : IDrawingSession, IRetainedRenderingSession` adds `IRenderData Finish()`.
`IRenderData : IDisposable` is the opaque recorded frame (Skia: an `SKPicture`).

---

## Frame lifecycle — `IRenderBackend`

Two-phase, driven by `CompositionTarget` (which keeps scheduling/vsync/threading):

```csharp
ICommandRecorder BeginFrame();               // phase 1 (UI thread): tree walks into this; Finish() -> IRenderData
IPresentSession BeginPresent(IRenderSurface target);   // phase 2 (vsync): compose a frame onto the target
```
`IPresentSession : IDrawingSession, IRetainedRenderingSession, IDisposable` — the present-time session lets
overlays (e.g. the FPS counter) compose *before* the frame is finalized; disposing it flushes/finalizes.
`IRenderSurface` is the opaque present target.

---

## Resource handles

- **`IGeometry : IDisposable`** — the geometry currency. `Rect Bounds { get; }`, `bool FillContains(...)`,
  transform/combine ops. Skia impl wraps `SKPath` (`SkiaGeometrySource2D`); the `SKPath` never escapes it.
- **Geometry builders** — split so imperative and whole-shape construction can't interleave:
  - `IGeometryBuilder` — common base: `GeometryFillRule FillRule { get; set; }`, `IGeometry Build()`.
  - `IPathBuilder : IGeometryBuilder` — pen verbs: `MoveTo`, `LineTo`, `CubicTo`, `QuadraticTo`, `ArcTo`, `Close`.
  - `IPrimitiveGeometryBuilder : IGeometryBuilder` — whole primitives (rects/ellipses/etc.).
- **`IShader`** — opaque gradient/shader handle (from the backend's `Create*GradientShader`).
- **`IColorFilter`** — blend-mode / color-matrix filter handle.
- **`IEffectFilter`** — realized `IGraphicsEffect` graph, or a drop-shadow filter.
- **`IImage`** — opaque bitmap handle: `int PixelWidth/PixelHeight`. Not itself disposable (its owner is).
- **`IImageFrames : IDisposable`** — decode/upload result: `IReadOnlyList<IImage> Frames`,
  `IReadOnlyList<int> DurationsMs` (one entry = still image; several = animation). Owns frame lifetime.

---

## Font seam — `IFont`

Render-time font handle (font *loading/shaping* stays backend-internal; this only turns a shaped run into
drawable output):

```csharp
IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY);
bool HasColorGlyphs { get; }
void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output);
```
Outline glyphs → one filled `IGeometry` (drawn via `DrawPath`); color glyphs (emoji: COLR/CBDT/sbix/SVG) →
positioned `IImage`s (`PositionedGlyphImage`, drawn via `DrawImage`). Obtained from `FontDetails.FontHandle`.

- **`SkiaFont`** — default (`SKFont.GetGlyphPath` + offscreen rasterization for color glyphs).
- **`ManagedFont`** — alternative, SkiaSharp-free: reads outlines straight from sfnt tables (TrueType `glyf`
  simple+composite, CFF/Type2), emits via `IPathBuilder`; COLRv0/CPAL color layers composited via
  `RenderOffscreen`. Toggle `UNO_MANAGED_FONT_BACKEND=1`; falls back to `SkiaFont` if a font can't be parsed.

---

## Image decode — `ManagedImageDecoder`

The decode/upload seam is `IDrawingBackend.TryDecodeImage` (encoded bytes → `IImageFrames`, incl. EXIF /
downscale / animation) and `CreateImageFrame` (raw BGRA → `IImageFrames`). The Skia backend uses `SKCodec`;
the neutral frame providers (`SingleFrameProvider`/`AnimatedImageFrameProvider`) hold `IImageFrames` and are
SkiaSharp-free.

**`ManagedImageDecoder`** (`.skia.cs` + `.Png/.Gif/.Bmp/.Jpeg/.Webp` partials) is the SkiaSharp-free decoder:
parses encoded bytes → BGRA-premultiplied pixel frames; the backend wraps them into `IImage`. Toggle
`UNO_MANAGED_IMAGE_DECODER=1`, tried before the Skia codec with automatic fallback.

| Format | Support |
|---|---|
| PNG | color types 0/2/3/4/6, depths 1–16, all filters, **+ Adam7 interlace** |
| GIF | LZW, animation (delays/disposal/transparency), interlace |
| BMP | uncompressed 8/24/32-bit |
| JPEG | baseline (Huffman/IDCT/YCbCr/restart), EXIF orientation, bilinear chroma |
| WebP | **lossless (VP8L)**: LZ77 + color cache + meta-Huffman + 4 transforms |
| _fallback → Skia codec_ | progressive JPEG, WebP lossy (VP8) + animated WebP, TIFF, ICO |

Validated pixel-perfect (maxDiff=0) vs Skia for PNG/GIF/BMP/VP8L; JPEG within avgDiff ~0.01.

---

## SVG seam — `ManagedSvg`

**`ManagedSvg`** (`.skia.cs` + `.Path.skia.cs`) is a SkiaSharp-free SVG parser+renderer: parses markup
(`XDocument`) and renders through the neutral abstraction (geometry via `IPathBuilder`, fills/strokes/clips
via `IDrawingSession`, gradients via the backend shaders), rasterizing via `RenderOffscreen`. It is the
**primary** SVG path in core `SvgImageSource` (drives `IsParsed`/`SourceSize`, renders to a composition
surface) — SVG no longer requires the `Svg.Skia` add-in. The add-in is optional: a rendering fallback and the
driver for the vector `SvgCanvas` (which stays Skia via `SKCanvasElement`, a separate control/package, by
design).

Covers the common icon subset (path incl. arcs→cubics, basic shapes, `use`, groups, transforms, viewBox,
solid + linear/radial gradient fills, strokes, opacity, fill-rule, inline style). Not yet: text,
clipPath/mask/filter/pattern, embedded images, CSS-class styling (fall back to the add-in when present).

---

## Value types & enums

Neutral structs on the verbs: `RoundRectangle`, `StrokeStyle`, `PositionedGlyphImage`. Enums:
`GeometryFillRule`, `GeometryCombineMode`, `StrokeCap`, `StrokeJoin`, `ClipOperation`, `BlendMode`,
`GradientTileMode` (Clamp/Repeat/Mirror), `ImageSampling`.

---

## Backends

- **Default: SkiaSharp** — `SkiaDrawingBackend`, `SkiaRenderBackend`, and the `Skia*` handle/session classes.
- **Managed/alternative seams** (SkiaSharp-free, opt-in via env toggle, prove the seams are neutral):
  `ManagedFont` (`UNO_MANAGED_FONT_BACKEND`), `ManagedImageDecoder` (`UNO_MANAGED_IMAGE_DECODER`), and
  `ManagedSvg` (primary; no toggle needed).

### Validation methodology
Each seam is proven by building the *same artifact two independent ways and comparing*: an alternative
backend renders/decodes with zero SkiaSharp, and its output is compared pixel-for-pixel against the Skia
backend (e.g. `Given_IFont_AlternativeBackend`, and pixel-parity of the managed decoders/SVG). Identical
output is the evidence the interface carries everything the backend needs.

---

## Status — what's neutral, and what's still Skia

**Neutral through the seams today:** all drawing verbs, geometry, shaders, color/effect filters, shadows,
glyph rendering (outline + color), image decode (PNG/GIF/BMP/JPEG/WebP-lossless), SVG, and the record/present
frame cycle. No SkiaSharp type appears on any of these interfaces.

**Still Skia (the remaining path to fully dropping SkiaSharp):**
1. **The rasterizer itself** — the default `IDrawingSession`/`RenderOffscreen` pixel work is SkiaSharp. A
   fully Skia-free runtime needs an alternative `IDrawingBackend` that rasterizes (the largest remaining piece).
2. **Image decode fallbacks** — progressive JPEG, WebP **lossy (VP8)** and animated WebP (both need the
   ~2000-line RFC 6386 VP8 intra decoder), TIFF, ICO still route to the Skia codec.
3. **`SKCanvasElement`/`SvgCanvas`** — intentionally Skia-only (an app-facing "draw with raw Skia" control in
   a separate package); out of scope for core neutrality.
4. **Transitional internals** — a few backend-internal spots still hand an `SKImage` to the composition
   surface (RTB render, the WASM browser-canvas decode path); these are inside/adjacent to the Skia backend,
   not on a pluggable interface.
