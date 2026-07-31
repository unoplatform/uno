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

**Conventions.** The SPI is **`public`** (namespace `Uno.UI.Composition.Drawing`, files under
`src/Uno.UI.Composition/Composition/Uno/Drawing/*.skia.cs`, Skia target) — the deliberate flip is done, so a
foreign backend in a separate assembly implements it with no `[InternalsVisibleTo]`. The backend
implementations (`Skia*`, `Managed*`) stay `internal` — they are one implementation, not the contract.
Backend-neutral geometry currency is `IGeometry`; `SKPath`/`SKImage`/`SKFont` etc. live only inside the Skia
implementation classes and are never exposed on an interface.

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
`DrawingBackend.Register(IDrawingBackend)` swaps it before the first frame. **This default-and-swap
scheme is being replaced** — see "Backend registration model" below for the decided target
(matched-pair registration on the host builder, no module-init default).

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
  `RenderOffscreen`. Also parses **metrics** — `cmap` (Unicode fmt 4/12 → glyph index), `hmtx` (advances),
  `hhea`/`OS-2` (ascent/descent/line-gap) — validated pixel-for-glyph against SkiaSharp. Toggle
  `UNO_MANAGED_FONT_BACKEND=1`; falls back to `SkiaFont` if a font can't be parsed.

## Text layout & shaping — architecture decisions

The `IFont` seam above is the text boundary of the **render** abstraction: it takes *already-shaped* input
(glyph indices + positions) and produces neutral `IGeometry`/`IImage`. Everything upstream of it — **shaping**
(string → glyphs+positions) and the **text engine** (segmentation, bidi, script, line wrapping, alignment,
trimming, hit-testing, caret) — lives in `Uno.UI`'s text layer (`FontDetails`, `UnicodeText`, `ParsedText`,
`Run`, `Inline`) and is **not** part of the pluggable backend SPI. A render backend never sees it.

Decisions:

- **The shaper is NOT abstracted.** Shaping is upstream of the backend and font/script-dependent, not
  render-backend-dependent; in practice everyone uses HarfBuzz, and no backend author would override it. Keep
  it internal, non-overridable.
- **The text engine is NOT abstracted (for now).** `UnicodeText` (segmentation/bidi/wrap/arrange/hit-test) is
  mostly pure, backend-agnostic logic. A pluggable text-engine seam *could* be advantageous for perf/features
  — e.g. [PretextSharp](https://github.com/wieslawsoltes/PretextSharp), a managed-C#/MIT line-layout engine
  with a graphics-agnostic contract that delegates shaping to platform backends, is a real candidate — but the
  engine's contract is large and deeply XAML-integrated (`TextBlock`/`TextBox`/selection/caret), so it's
  **kept to the side as not worth it now**. Revisit only if text layout is measured to be a bottleneck, and
  then gate on a prototype (e.g. PretextSharp against Uno's workload) before designing a seam.

### Goal: make the text logic Skia-less

The priority for the text layer is to remove **SkiaSharp** from it (HarfBuzz + ICU are native but *not* Skia —
the goal is Skia-less, not zero-native). "Text logic" = the layout/measure/draw code (`FontDetails`,
`UnicodeText`, `ParsedText`, `Run`, `Inline`) — it must talk only to `IFont` + neutral types. A *backend*
font impl (`SkiaFont`) may still use SkiaSharp internally; `ManagedFont` is the Skia-free impl. What Skia
provides today falls into six jobs, and each has a home:

| Text concern | Skia today | Home / Skia-less path |
|---|---|---|
| glyph outlines / color glyphs | `SkiaFont.GetGlyphPath` | ✅ `IFont` (`ManagedFont` alt, toggle) |
| **metrics** (ascent/descent/lineGap, underline & strikeout pos/thickness) | `SKFontMetrics`/`SKFont` | ✅ **`IFont`** — the handle exposes metrics (SkiaSharp sign convention; `ManagedFont` parses `post`/`OS-2`) |
| **glyph coverage** ("does *this* font have the glyph") | `SKFont.ContainsGlyph` | ✅ **`IFont`** — `GetGlyphIndex`/`ContainsGlyph` |
| **shaping table access** (HarfBuzz `Face` source) | `SKTypeface.GetTableData` | ✅ **`IFont`** — `GetFontTable`/`UnitsPerEm`; `SkiaFont` serves from its *variable-instanced* `SKTypeface` (shaping byte-identical to before), `ManagedFont` from its bytes. HarfBuzz stays (not Skia). |
| value types (`SKPoint`/`SKRect`/`SKColor`/`SKPath`) | pervasive in layout/draw | ✅ existing equivalents — `Vector2`, `Windows.Foundation.Rect` (via `Point`), `Windows.UI.Color`, `IGeometry` (spell-check squiggle via `IPathBuilder`). Also collapsed the vestigial `SKPaint` color-carrier to `Color`. |
| **font resolution + fallback** (family/weight/style → face; *which* font covers a codepoint) | `SKTypeface.FromFamilyName`, `SKFontManager.MatchCharacter` | the genuinely new part → **`IFontManager` seam** (below) **(not yet done)** |

So the font *handle* (`IFont`) absorbs metrics + coverage + table access — none of it a separate abstraction,
and none throwaway (it's the target regardless of when resolution is replaced). `FontDetails` now holds an
`IFont` (+ the HarfBuzz `Font` built from `IFont`'s tables) instead of `SKFont`/`SKFontMetrics`; it keeps the
resolved `SKTypeface` only as the *interim resolution handle* (used by Run's fallback matching) until the
`IFontManager` seam replaces it.

**Status:** the `IFont` absorption is **done and validated** — the text layout/measure/draw code (`FontDetails`,
`Inline`, `UnicodeText`, `ParsedText`, `Run`) reads metrics/coverage/tables only through `IFont`; `Given_TextBlock`
runtime tests pass 103/103 on both the default Skia path *and* the fully Skia-less managed-font path
(`UNO_MANAGED_FONT_BACKEND=1`), which now exercises ManagedFont metrics + coverage + HarfBuzz-from-ManagedFont-tables
end-to-end. The **value-type sweep is also done** — `UnicodeText`/`ParsedText` layout & draw code uses
`Vector2`/`Rect`/`Color`/`IGeometry` (validated: TextBlock 103/103 on both paths, TextBox unchanged at its
pre-existing headless baseline). **Still Skia in the text layer:** only font resolution/fallback — the
`IFontManager` seam — plus the backend-internal `SKFont` that `FontDetails` builds to feed `SkiaFont` (which
moves once resolution is behind the seam).

#### Font resolution & fallback — approaches

Everything except resolution reduces to *font bytes → `ManagedFont` (metrics/outlines) + HarfBuzz face (from
those same bytes)*. The open question is only **how to turn a family name + style into bytes**, and how to
find a fallback font for a codepoint the chosen family can't render. Today this is 100% Skia
(`FontDetailsCache`): `SKTypeface.FromFamilyName(name, weight, width, slant)`, `.ttc`/`.otc` face selection by
family/PostScript name, variable-font axis positioning (`wght`/`wdth`/`ital`/`slnt`), and default-font
fallback (`SKTypeface.FromFamilyName(null)`). Application/URI fonts and per-codepoint fallback already route
through neutral-ish seams (`AppDataUriEvaluator` byte load; `IFontFallbackService` via `ApiExtensibility`); it
is **system-family resolution + variable-font positioning** that is Skia-only.

Three ways to remove that dependency:

- **A) Managed font manager (pure C#).** Enumerate the OS font directories, parse each face's `name`/`OS-2`/
  `fvar` tables, and match family + weight/width/slant ourselves (reusing `ManagedFont`'s sfnt parser); glyph
  fallback = scan candidate faces' `cmap`s for coverage.
  *Pro:* zero native, one implementation everywhere, fully introspectable.
  *Con:* re-implements OS font matching — locale-aware family aliases, synthetic bold/oblique, the platform
  fallback *chain* (CJK/emoji/symbol ordering) — which is exactly the fiddly, locale-sensitive part users
  notice when it's wrong. Variable-font instancing (applying an axis position to `glyf`/`gvar`) is also
  non-trivial to add to `ManagedFont`.

- **B) Platform font APIs behind a seam (recommended target).** Define an `IFontManager` extensibility point
  (mirroring `IFontFallbackService`): `family + style → font bytes/handle`, plus the existing
  codepoint→fallback. Provide it via `ApiExtensibility` with per-runtime implementations in the
  `Uno.UI.Runtime.Skia.*` projects — **DirectWrite** (Win32), **CoreText** (macOS/iOS), **fontconfig**
  (Linux), **Android font APIs** (Android).
  *Pro:* correct, locale-aware matching + fallback chains for free; the text *logic* (metrics/shaping/render)
  becomes Skia-less regardless of resolver; matches the rest of this design (platform specifics live in
  `Runtime.Skia.*`, core stays neutral).
  *Con:* several native implementations to write and maintain; variable-font positioning still needs handling
  (either the platform API instances it, or we do it in `ManagedFont`).

- **C) Skia as byte-source only (interim).** Keep `SKTypeface.FromFamilyName` purely to *locate* a face, then
  `OpenStream` → bytes → `ManagedFont` + HarfBuzz-from-bytes for everything downstream.
  *Pro:* smallest step; unblocks the managed-text toggle *now* with correct matching; no new native code.
  *Con:* SkiaSharp still linked as the resolver — text logic is Skia-less but the assembly isn't
  Skia-*free*. Only a stepping stone.

**Chosen direction: B is the target, reached via C as the interim.** Introduce the `IFontManager` seam and
route `FontDetailsCache` through it. Ship a **Skia-backed implementation first** (option C behavior — Skia
resolves, managed code does metrics/shaping/render), which makes the *logic* Skia-less immediately and is
drop-in replaceable. Then add native `IFontManager` implementations per runtime (option B) to make the
core assembly Skia-*free*. Option A stays a fallback for platforms without a good native font API, not the
primary path. Variable-font positioning is handled at the resolver seam (platform instancing where available;
otherwise a follow-up in `ManagedFont`).

**Status — the seam exists.** `IFontManager` (`CreateFont` from bytes / `MatchFamily` / `MatchCharacter` /
`GetDefaultFont`, all returning `IFont`) hangs off `IDrawingBackend.FontManager`. `SkiaFontManager` holds all
the Skia resolution (`SKTypeface.FromData`/`FromFamilyName`, `.ttc` face selection, variable-font axes,
`SKFontManager.MatchCharacter`); `FontDetailsCache`/`Run`/`UnicodeText` now resolve through it with `IFont`
currency and no direct Skia. `ManagedFontManager` (option A) is the **SkiaSharp-free** resolver: it indexes the
OS font directories by parsing each face's `name`/`OS-2`/`head` tables, matches family + weight/width/italic
with a nearest-score, produces `ManagedFont` handles, and does codepoint fallback by scanning indexed `cmap`s.
With it selected, the entire font path — resolution, metrics, coverage, shaping-tables, outlines — is
Skia-free (only the final geometry rasterization is the drawing backend's job). Validated: `Given_TextBlock`
103/103 on the default Skia resolver *and* with the managed resolver (DejaVu resolved via managed lookup on Linux).

**The resolver is pluggable by interface, not a managed-specific flag.** `DrawingBackendOptions.FontManager` is
an `IFontManager?` — `null` means the backend's default (Skia for `SkiaDrawingBackend`), and a host assigns any
implementation to override (`ManagedFontManager`, or a platform-specific one). This matters because
`ManagedFontManager` — which discovers fonts by **enumerating filesystem font directories** — is the right
resolver only where that model holds:

| Target | Filesystem fonts? | Managed resolver | What to use |
|---|---|---|---|
| Windows / macOS-desktop / Linux | yes (`C:\Windows\Fonts`, `/usr/share/fonts`, …) | ✅ works | `ManagedFontManager` or default Skia |
| Android (Skia) | yes (`/system/fonts`) | ✅ works | `ManagedFontManager` or default Skia |
| **iOS** (Skia) | **no** — the app sandbox blocks reading `/System/Library/Fonts` | ❌ finds nothing | default Skia (SkiaSharp→CoreText), or a future CoreText `IFontManager` |
| **WebAssembly** (Skia) | **no** — the browser sandbox has no OS font directory | ❌ finds nothing | default Skia (bundled fonts), or a bundled/`@font-face` `IFontManager` |

So on iOS and WASM the *managed filesystem* resolver can't see fonts; those platforms keep the default Skia
resolver (SkiaSharp routes to CoreText on iOS and to bundled fonts on WASM) until a platform-native
`IFontManager` (CoreText via `CTFontManager`; a bundled/browser resolver for WASM) is written and plugged in —
exactly what the pluggable seam is for. **Backend selection moved off environment variables:**
`DrawingBackendOptions` (`FontManager` + `UseManagedGeometry`/`UseManagedImageDecoder`), set by the host at
init, replaces the former `UNO_MANAGED_*` toggles (the SamplesApp host bridges those env vars to the options as
a dev/test affordance).

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
| JPEG | **baseline + progressive** (Huffman/IDCT/YCbCr/restart, coefficient-buffer model), EXIF orientation, bilinear chroma |
| WebP | **lossless (VP8L)**: LZ77 + color cache + meta-Huffman + 4 transforms |
| _fallback → Skia codec_ | WebP lossy (VP8) + animated WebP, TIFF, ICO |

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

## Backend registration & graphics negotiation (implemented; X11 GL/GLES/software neutral)

This is the model the whole pluggable story hangs on. It has five moving parts — registration,
negotiation, the host's contribution, the framework's contribution, and the per-frame contract —
plus a clear answer to "who drives."

> **Implementation status.** The negotiation is live under the vision naming (`GraphicsRegistry`,
> `IGraphicsProvider`, `Graphics(IDrawingFactory, IRenderer)`, `IGraphicsContext`,
> `GraphicsContextKind`). `SkiaBackend.Register` registers the one built-in pair (`SkiaGraphicsProvider`,
> default kind order GL → GLES → software). On X11 the render-selection chain names **no** GPU-library
> type: `X11SoftwareGraphicsRenderer` installs `X11GraphicsContextFactory` and calls
> `GraphicsRegistry.Initialize`, which negotiates the context; the Skia backend wraps the acquired
> neutral target (`ISoftwareRenderTarget` → CPU surface, `IGLRenderTarget` → `GRContext`-GL/GLES). A
> host with one window type steers the outcome through `Initialize`'s optional neutral
> **kind-preference override** (e.g. `[Software]`, `[OpenGLES, Software]`) — expressed in
> `GraphicsContextKind` only, never a backend type. **Verified** headless (Xvfb): a render test passes
> on both the default GLX (`OpenGL context`) and forced-software (`Software context`) paths, and the
> WebGPU backend agrees pixel-for-pixel with Skia on the shared render seam. **Remaining kinds** —
> Vulkan (fails headless on lavapipe) and WebGPU-on-window — await a real-GPU matrix before their X11
> branches migrate and the `UseOpenGL*/UseVulkanOnX11`/`PreferGLESOverGLOnX11` knobs retire.

### The two seams stay separate

`IDrawingBackend` (resource/content **factory** — geometries, shaders, filters, images; the
`Uno.UI.Composition` layer, frame-independent) and `IRenderBackend` (**frame/present** pipeline,
host-adjacent, per-surface) live at different layers and lifetimes (a shader is built once and
reused across frames before any window exists; a present is per-tick). They are **not** collapsed
into one interface — that would force one construction moment and drag the host-adjacent present
concern into the content layer. A backend is registered as a **matched pair** (see `IGraphicsBackend`)
so the two can never come from different families.

### Handle model — why the matched pair is mandatory

`IShader` / `IColorFilter` / `IEffectFilter` are **opaque** and consumed *only* by the paired
renderer, which downcasts them to its own concrete type. A Skia factory paired with a foreign
renderer would fault that cast (a `WebGpuShader` cast can never receive a `SkiaShader`). So the fix
for gradients/effects is **not** to make `IShader` introspectable — it is to guarantee the factory
and renderer are the same family. (`IGeometry` / `IImage` are the opposite: core itself introspects
them — `Bounds`, `FillContains`, `Combine`, pixels — so they carry neutral members + readbacks and a
*shared managed* implementation is legitimate. Rule of thumb: consumed only by the paired renderer →
opaque + cast-back; introspected by core → neutral abstraction.)

### Registration — user-side, uniform, no default

There is **no default backend**. The core references no backend package; `Uno…Skia` is one
optionally-referenced package among peers, so an app that registers only WebGPU never links Skia and
the linker drops it. The app registers its choice with **one process-static call, identical on every
platform** (including Android/iOS, which have no fluent host builder), placed in the shared app
bootstrap that runs everywhere:

```csharp
// ordered by preference: try WebGPU, fall back to Skia
GraphicsBackend.Register(new IGraphicsBackend[] { new WebGpuGraphicsBackend(), new SkiaGraphicsBackend() });
```

No `[ModuleInitializer]` self-registration: cross-assembly initializer order is undefined and a
plugin assembly may not even be loaded, so it can't serve third parties — and it would bake a
backend into core. Unregistered access throws with a message naming the fix. Standalone consumers
(unit tests, offscreen harnesses) are their own composition root and register too. Where a fluent
host builder exists, a `.UseSkiaRendering()` extension may be **thin sugar over the same static
call** — never a separate mechanism, so the mechanism stays uniform.

Two orthogonal choices, kept separate: the **windowing host** (`UseX11()` / Android `Activity`) is
*where pixels go*; the **graphics backend** (`GraphicsBackend.Register`) is *how pixels are
produced*. `DrawingBackend.Current` and the render backend remain process-global → one backend per
process; per-window backends would require making those per-host (a separate change).

### Negotiation — lazy, ordered, create-until-success

Two ordered preference lists: the **user's backend list** (above) and each **backend's context-kind
list** (owned by the backend — the user never needs to know Skia prefers Vulkan over GL). Nothing is
created speculatively; contexts are created on demand until one succeeds:

```
foreach backend in userBackendList:                         // user's order
    foreach kind in backend.PreferredContexts:              // backend's order
        if (ContextFactory(kind, host.NativeWindow, backend.Requirements) is { } ctx)  // concrete, Uno-owned
            return backend.CreateRenderBackend(ctx)          // first success wins; stop
        // kind unavailable / requirements unmet → factory returned null (fully disposed) → next kind
    // no kind worked → next backend
throw "no registered backend could initialize on this host (attempted: …)";
```

`TryCreate` does a cheap availability probe before the real init (as Uno already does —
`X11VulkanSurfaceFactory.IsVulkanAvailable()`), must fully clean up on failure (a null return means
"as if never attempted"), and treating a later `CreateRenderBackend` failure like a context failure
keeps the walk robust.

### Who provides what

- **Host provides `INativeWindow` only** — the tagged native handle (X11 `Display`+`Window`, Win32
  `HWND`, Android `ANativeWindow`, `CAMetalLayer`) + size + resize events. This is the *only* thing
  that is both platform-specific and GPU-agnostic. **The host references no GPU API and no backend.**
- **Framework owns context + surface creation**, concretely. The context kinds are a **closed,
  Uno-owned set** (`gl/gles/vulkan/metal/wgpu/software`) — third parties plug in *backends*, not new
  kinds — so there is **no per-kind provider plugin interface**. Instead a single concrete
  `GraphicsContextFactory` (a `switch` over the known kinds, living in the Uno graphics layer that
  references the API libs) creates each context, owns its swapchain/surface, and owns the
  **blit-with-dirty-rects** and present. Core reaches it through one internal seam
  (`GraphicsBackend.ContextFactory`) so core stays free of GPU-API libraries. Uno already has most of
  the creation code (`VulkanContext`, the EGL/GL renderers, the `*VulkanSurfaceFactory` set) — the
  refactor splits each renderer's "acquire context/surface" half (→ the factory) from its "wrap as
  `SKSurface`" half (→ the Skia backend). The **`Software`** context is special: it's a neutral CPU
  **framebuffer** (pointer + width/height/stride) that lives in core with no lib, and each backend
  *wraps* it (Skia via `SKSurface.Create(info, ptr, stride)`, a managed rasterizer via the raw bytes).
- **Backend consumes** a ready context, builds its pipelines + its own scratch/stencil from it, and
  fills a render target. It writes **no** graphics-init and **no** windowing code.

### Contexts, not surfaces — and capabilities

The negotiation currency is the **context** (the GPU-API connection/device: `IWebGpuContext` =
Instance/Adapter/Device/Queue; GL context; `VkDevice`+queue; `MTLDevice`) — that's where the API
family and the hard platform init live, and it's shareable + offscreen-capable. Well-known kinds:
`OpenGL, OpenGLES, Vulkan, Metal, WebGpu, Software`. WebGPU is a first-class provided context, not a
special case.

A backend declares **capabilities/requirements** (min stencil bits, depth, MSAA sample count,
preferred color format, limits). These are **support guarantees on the created context** — the
provider selects a device that supports them or `TryCreate` fails (feeding the negotiation fallback).
Requirements split by role: color-format/sample-count **configure the framework-created color
target**; stencil/depth-format/limits are **device-support the backend relies on when it allocates
its *own* attachments**. The backend still allocates its own depth/stencil/scratch (technique- and
size-specific, never presented) from the context — capabilities only guarantee the device *can*.

### The render target is a view — surface vs texture stays internal

In every API the render target is a **color attachment view of a uniform type regardless of origin**
(WebGPU `TextureView`, Vulkan `VkImageView`, Metal `MTLTexture`, GL framebuffer) — a swapchain image
and an offscreen texture produce the *same* type. So the backend is handed a **render-target view**
and never learns whether it's backed by the window swapchain (direct, no blit) or a retained
offscreen texture (then we blit dirty rects). That decision is ours, per frame.

No "previous contents retained" flag is needed: the **dirty region is expressed as a clip in the
recorded frame** (backend-independent), so the backend just replays the clipped frame; the invariant
that pixels *outside* the dirty region already hold the previous frame is **ours to uphold** by our
target management (a constraint on our strategy, not the backend contract). The render-target view
is **kind-matched** (opaque to core; a `WebGpu` provider mints the one a WebGPU backend downcasts).

### Who drives the pipeline

- **The render loop / frame cadence — us (push).** `CompositionTarget`'s scheduler owns
  vsync/invalidation/threading and *pushes* frames; the backend never runs its own loop or blocks on
  vsync.
- **The frame lifecycle — us.** Acquire the color target, ensure render-thread + context-current,
  blit (dirty rects), present, and the sync points between the backend's render and our blit.
- **The scene's GPU commands — the backend.** Pipelines/PSOs, its render pass (our color attachment +
  its own depth/stencil, its load ops), draw calls, and **submitting** that work (or issuing
  immediate GL calls). The backend submits its own scene commands — the one model that unifies
  immediate-mode GL with command-buffer APIs, since it doesn't force a shared command-buffer across
  the seam. We sequence `acquire → backend.Render() → our blit → present` single-threaded on the
  shared queue; where a barrier/flush is needed before we sample the offscreen texture, we insert it.
- **Hard contract: thread + context affinity.** The backend does all GPU work (render *and* resource
  creation) only inside the calls we make, on the render thread we invoke it on, with the context
  already current. It may not render on arbitrary threads.

### Interface sketch

```csharp
public enum GraphicsContextKind { OpenGL, OpenGLES, Vulkan, Metal, WebGpu, Software }

public readonly struct GraphicsRequirements
{
    public int MinStencilBits { get; init; }   // e.g. 8 for even-odd/nonzero fills
    public bool NeedsDepth { get; init; }
    public int SampleCount { get; init; }       // MSAA
    public ColorFormat PreferredColor { get; init; }
}

public interface INativeWindow                  // host-provided; GPU-agnostic
{
    NativeWindowKind Kind { get; }              // X11 / Win32 / Android / Metal / …
    nint Handle { get; } nint Display { get; }
    PixelSize Size { get; }
    event EventHandler Resized;
}

// concrete, Uno-owned creation over the closed kind set — NOT a per-kind plugin interface.
// Set once by the Uno graphics layer; core reaches it via GraphicsBackend.ContextFactory.
public delegate IGraphicsContext? GraphicsContextFactory(
    GraphicsContextKind kind, INativeWindow window, GraphicsRequirements requirements);

public interface IGraphicsContext : IDisposable   // thin device/init handle; NOT a resource factory
{
    GraphicsContextKind Kind { get; }
    bool IsLost { get; }
}

public interface IRenderTarget : IDisposable { PixelSize Size { get; } }   // kind-matched color view

public interface IGraphicsBackend               // the registerable unit (matched pair)
{
    IReadOnlyList<GraphicsContextKind> PreferredContexts { get; }   // ordered
    GraphicsRequirements Requirements { get; }
    IDrawingBackend Drawing { get; }
    IRenderBackend CreateRenderBackend(IGraphicsContext context);
}

public interface IRenderBackend
{
    ICommandRecorder BeginFrame();                     // record the tree → IRenderData (UI thread)
    IPresentSession BeginPresent(IRenderTarget target); // render session onto the color target (phase 2)
    // The session form (rather than a bare Render(target, frame)) is retained because the framework
    // composes present-time overlays into it — the FPS counter, whose timing is only known at present —
    // and the per-frame Clear, before/after replaying the recorded frame. IPresentSession : IDrawingSession,
    // IRetainedRenderingSession, IDisposable.
}

public static class GraphicsBackend
{
    public static void Register(IReadOnlyList<IGraphicsBackend> backendsInPreferenceOrder);
    // no default; unregistered access throws
}
```

### ⚠️ Breaking change — removal of the Skia-assuming rendering options

The existing per-host "rendering backend" / "surface type" options **select which GPU API *Skia*
renders through** (Skia-on-Vulkan, Skia-on-OpenGL, Skia software). They conflate the *graphics
backend* choice with a *Skia-internal surface/GPU-API* choice, which is incoherent once the graphics
backend itself is pluggable. They are **removed**:

| Removed API | Assembly |
|-------------|----------|
| `X11RenderingBackend` enum (`Default`/`Vulkan`/`OpenGL`/`OpenGLES`/`Software`) + `X11HostBuilder.RenderingBackend(…)` | `Uno.UI.Runtime.Skia.X11` |
| `Win32RenderingBackend` enum (`Default`/`Vulkan`/`OpenGL`/`Software`) + `Win32HostBuilder.RenderingBackend(…)` | `Uno.UI.Runtime.Skia.Win32` |
| `RenderSurfaceType` enum (`Auto`/`Metal`/`Software`; `Software`/`OpenGL`) | `Uno.UI.Runtime.Skia.MacOS`, `Uno.UI.Runtime.Skia.Win32` |
| `FeatureConfiguration.Rendering.UseOpenGLOnX11`, `FeatureConfiguration.Rendering.UseVulkanOnX11` | `Uno.UI` |

**Rationale.** The host builder now selects a *graphics backend* (Skia / WebGPU / third party); the
GPU API and surface type are that backend's own internal concern — a WebGPU backend targets
Vulkan/Metal/D3D via `wgpu`; a Skia backend picks its `GRContext` surface. A host-level,
cross-backend "Vulkan/OpenGL/Software" knob no longer has a coherent meaning.

**Migration.** Selecting a renderer becomes "register a backend on the host builder" rather than
"select a GPU API". Any GPU-API preference a specific backend still supports becomes that backend's
own configuration, not a shared host-builder enum. Removing these public enums/methods/flags will be
flagged by the `Uno.PackageDiff` CI gate (expected; this doc is the reference for why).

---

## Status — what's neutral, and what's still Skia

**Neutral through the seams today:** all drawing verbs, geometry, shaders, color/effect filters, shadows,
glyph rendering (outline + color), image decode (PNG/GIF/BMP/JPEG/WebP-lossless), SVG, `RenderTargetBitmap`,
the native-element clip path (crosses `CompositionTarget`↔host as `IGeometry`, converted to `SKPath` only
inside each Skia host), and the record/present frame cycle. No SkiaSharp type appears on any of these interfaces.

**Assembly layout:** `Uno.UI.Composition` is fully SkiaSharp-free; the Skia backend lives in a separate
`Uno.UI.Composition.Skia` assembly (`Uno.UI → Uno.UI.Composition.Skia → Uno.UI.Composition`). `Uno.UI` itself
no longer references SkiaSharp except the two items below — the Vulkan GPU subsystem was relocated wholesale
from `Uno.UI` into the Skia backend assembly.

**Still Skia (the remaining path to fully dropping SkiaSharp):**
1. **The rasterizer itself** — the default `IDrawingSession`/`RenderOffscreen` pixel work is SkiaSharp. A
   fully Skia-free runtime needs an alternative `IDrawingBackend` that rasterizes (the largest remaining piece).
2. **Image decode fallbacks** — WebP **lossy (VP8)** and animated WebP (both need the ~2000-line RFC 6386
   VP8 intra decoder), TIFF, ICO still route to the Skia codec.
3. **`SKCanvasElement`/`SvgCanvas`** — intentionally Skia-only (an app-facing "draw with raw Skia" control in
   a separate package); out of scope for core neutrality. This is the only remaining SkiaSharp use in `Uno.UI`
   besides the frame-rate-counter overlay's `SKFont` text shaping (a diagnostic, not a render-path dependency).
4. **Transitional internals** — a few backend-internal spots still hand an `SKImage` to the composition
   surface (the WASM browser-canvas decode path); these are inside/adjacent to the Skia backend, not on a
   pluggable interface.
