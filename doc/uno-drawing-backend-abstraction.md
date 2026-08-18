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
  **opaque handles** manufactured by a factory (`IDrawingFactory`) or a decode call; the framework holds and
  caches them without inspecting their internals. The backend downcasts internally.
- **Per-visual/frame retained state** (Skia's `SKPicture`) crosses as an opaque `IRenderRecord` the backend
  owns; composition never inspects it. Recording is a first-class part of the factory (`CreateRecording()`), not
  an optional capability — a backend with no native display-list gets a neutral command-list recorder for free
  (see "Retained rendering" below), so composition always has a cached path.
- **Value types are neutral** — `Windows.Foundation.Rect/Size`, `Windows.UI.Color`,
  `System.Numerics.Vector2/Matrix3x2/Matrix4x4` — no SkiaSharp type appears on any pluggable interface.
- **The render cycle stays in Uno**, not the backend. `CompositionTarget` owns scheduling/vsync/threading and
  the visual-tree walk; the backend is a passive two-phase participant (record, then present).

Layers, bottom to top: **resource + frame factory** (`IDrawingFactory`, and its typed
`IDrawingFactory<TTarget>` present half) → **drawing session** (`IDrawingSession`, consuming value paint +
handles) → **records** (`ICommandRecorder.Finish()` → an opaque `IRenderRecord` replayed into a present
session). The earlier separate `IRenderBackend` and the optional `IRetainedRenderingSession` were **merged into
`IDrawingFactory`** — there is one factory interface for resources, recording, and (typed per render-target)
present.

**Conventions.** The SPI is **`public`** (namespace `Uno.UI.Composition.Drawing`, in the dedicated
`Uno.UI.Composition.Drawing` assembly) — the deliberate flip is done, so a foreign backend in a separate
assembly implements it with **no `[InternalsVisibleTo]`**. This is now enforced: **no assembly grants its
internals to `Uno.UI.Composition.Skia` or `Uno.UI.Composition.WebGpu`** — each backend stands entirely on the
public seam (the last such IVTs were removed, and the WebGPU renderer engine was relocated into its own
assembly so nothing reaches into it). The backend implementations (`Skia*`, `Managed*`, `WebGpu*`) stay
`internal` — they are one implementation, not the contract. Backend-neutral geometry currency is `IGeometry`;
`SKPath`/`SKImage`/`SKFont` etc. live only inside the Skia implementation classes and are never exposed on an
interface.

---

## Registration & factory

### `IDrawingFactory` — the resource + frame factory
Manufactures the stateful handles that cross the boundary (textures, shaders, filters, the drop-shadow /
effect-tree filter), owns offscreen rendering + CPU snapshot, and starts recordings. The former separate
`IRenderBackend` was merged in: `CreateRecording()` is here, and the present half is the typed
`IDrawingFactory<TTarget>` (below). Current surface:

```csharp
ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);
Task<IImage> SnapshotAsync(ITexture texture);
ITexture CreateTexture(IImage image);
ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul);   // raw pixels-in-hand sibling

IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode);
IColorFilter CreateColorMatrixColorFilter(float[] matrix);
IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds);   // a neutral, pre-resolved effect tree — see below
IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color);

ICommandRecorder CreateRecording();                               // record a (sub)tree → IRenderRecord

// present half, typed to the render-target kind the backend composes onto:
public interface IDrawingFactory<in TTarget> : IDrawingFactory where TTarget : IRenderTarget
{
    IPresentSession BeginPresent(TTarget target);
}
```

**Geometry, image decode and fonts are *separate* neutral seams, not on the factory** — because core itself
introspects them (geometry bounds/contains/combine; decoded pixels; glyph metrics/coverage), they are neutral
abstractions with a *shared managed* implementation, and each is registered independently:

- `IGeometryFactory` — path/primitive builders + `IGeometry` (its own registration; a managed geometry engine
  or the Skia one).
- `IImageDecoder` — `TryDecodeImage` / `CreateImageFrame` → `ImageFrames`.
- `IFontProvider` / `IFont` — font resolution + render-time glyph output (see "Font seam").

`CreateEffectFilter` takes a **neutral `EffectNode` tree** (composition resolves the `IGraphicsEffect` graph,
sources and flags into a backend-independent node tree first), so the backend never sees `IGraphicsEffect` /
`CompositionBrush`.

### Holder & registration
`DrawingFactory.Current` (an **internal** process-wide holder) is installed **once, by graphics negotiation**
(`GraphicsRegistry`), at the winning backend — there is no public default-and-swap and no `[ModuleInitializer]`
default.

The **default-backend reflection lives entirely in the host builder** (`UnoPlatformHostBuilder.Build()`), not
scattered across the seam holders. After the app's explicit registrations are applied, `Build()` reflectively
lights up any seam the app left empty from the SkiaSharp backend (or the SVG/Lottie add-ins) — by
assembly-qualified name, so the framework keeps no compile-time dependency on SkiaSharp — then **throws right
there** if a *required* seam (graphics backend, font, image decoder, geometry) is still unsatisfied and no Skia
is present, instead of NRE-ing deep in the first frame. A deliberately SkiaSharp-free app registers each seam
explicitly and never trips the throw. Seam holders (`FontProvider`/`ImageEncoderDecoder`/`GeometryFactory`/
`SvgRenderer`/`LottieRenderer`) are now plain registries — they carry no lazy reflection. See "Backend
registration & graphics negotiation" below.

---

## Drawing session — `IDrawingSession`

Immediate-mode, stateful canvas. Transform stack, clipping, layers, and per-scenario draw verbs:

```csharp
Matrix4x4 TotalMatrix { get; }
object? NativeSurface { get; }   // see "Zero-copy native surface" below — [EditorBrowsable(Never)]
IDrawingFactory Factory { get; } // the backend that owns THIS session — mint session-native textures through it
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
void DrawImage(ITexture img, float x, float y, ImageSampling s, float opacity = 1f, bool aa = false);
void DrawImage(ITexture img, float x, float y, ImageSampling s, IColorFilter filter, bool aa = false);
void DrawImageNineSlice(ITexture img, in Rect centerSlice, in Rect destination, bool centerHollow, bool aa = false);
void DrawEffectBackdrop(IEffectFilter filter, float opacity);
```

Intent-over-technique: the interface says *what* (a shadow, a gradient fill, a faded image); the backend
chooses *how* (mask filter, SDF, layer). Retained-mode concerns are deliberately **not** here.

### Zero-copy native surface — `IDrawingSession.NativeSurface`

`NativeSurface` is the backend's live, directly-drawable native surface for this session — a SkiaSharp
`SKCanvas` on the Skia backend, or `null` when the backend records neutral commands and exposes none (WebGPU
records into a command list — it returns `null`). It is **type-erased (`object?`)** so the seam names no
graphics library, and marked `[EditorBrowsable(Never)]` so it doesn't clutter the advertised API. A consumer
that wants to draw with a specific graphics API type-checks it (`session.NativeSurface is SKCanvas c`) and, on a
match, draws **zero-copy straight into the frame**; on `null`/a different type it does something else. This is
what `SKCanvasElement` uses: a small visual living in the `Uno.WinUI.Graphics2DSK` add-in draws directly into
the frame `SKCanvas` when the active backend exposes one, and otherwise falls back to a self-contained
Skia-on-GL island (`GLCanvasElement`, own `GRContext` + framebuffer, read back and composited). No backend name
appears in that add-in — only the `SKCanvas` type-check.

### The session's own factory — `IDrawingSession.Factory`

A consumer that rasterizes to its own surface (the SVG add-in, the Skottie Lottie renderer) needs a texture the
*current* session will actually draw — and a texture is only drawable on the backend that minted it (WebGPU's
`DrawImage` silently no-ops on a foreign `ITexture`; only its own `WebGpuTexture` binds). `session.Factory`
hands back the session's own backing factory (never a process-global), and `CreateTexture(w, h, bgraPremul)`
mints a session-native texture from raw pixels with no `IImage` detour. So the neutral fallback for a
non-`SKCanvas` backend is: rasterize to an offscreen → read the pixels → `session.Factory.CreateTexture(...)` →
`session.DrawImage(...)`. An add-in never touches `DrawingFactory.Current` (it's internal) and never implements
`ITexture` itself. Framework composition code, which legitimately owns the composition root, still uses the
ambient `DrawingFactory.Current` for its sessionless texture/resource creation.

### Retained rendering — always available
```csharp
ICommandRecorder CreateRecording();   // on IDrawingFactory: start a recording; Finish() → IRenderRecord
```
`ICommandRecorder : IDrawingSession` adds `IRenderRecord Finish()`. `IRenderRecord : IDisposable` is the opaque
recorded frame (Skia: an `SKPicture`) and exposes `void Replay(IDrawingSession into)`.

Recording is **not** an optional capability interface any more — `CreateRecording()` is on `IDrawingFactory`.
A backend with an efficient *native* display list (SKPicture, a GPU command buffer) returns its own recorder;
a backend without one gets the framework's **command-list fallback** (`CommandListRetainedSession`) that records
the neutral `IDrawingSession` verbs and replays them on top of any session. So retention is *always* available
and composition has no uncached branch. The fallback is a self-contained snapshot: it copies (owns) the
geometry handed to each deferred draw — matching a native display list, since composition disposes transient
geometries right after the draw — and frees them when the recording is disposed.

---

## Frame lifecycle — `IDrawingFactory` (record) + `IDrawingFactory<TTarget>` (present)

Two-phase, driven by `CompositionTarget` (which keeps scheduling/vsync/threading). The former `IRenderBackend`
is merged into the factory:

```csharp
ICommandRecorder CreateRecording();          // phase 1 (UI thread): tree walks into this; Finish() -> IRenderRecord
IPresentSession BeginPresent(TTarget target);   // phase 2 (vsync): on IDrawingFactory<TTarget>, compose onto the target
```
`IPresentSession : IDrawingSession, IDisposable` — the present-time session lets overlays (e.g. the FPS
counter) compose *before* the frame is finalized; disposing it flushes/finalizes. The recorded frame is
replayed into it via `IRenderRecord.Replay(present)` (native replay, or the command-list fallback).

`IRenderTarget` (`Width`/`Height`/`GraphicsColorFormat`) is the opaque present target — a **color-attachment
view** the backend downcasts to its kind-matched face: `IGLRenderTarget`, `IVulkanRenderTarget`,
`IMetalRenderTarget`, `IWebGpuRenderTarget` (a wgpu `ColorView` handle), `ISoftwareRenderTarget` (CPU
`Pixels`+`RowBytes`). The backend's present half is typed `IDrawingFactory<TTarget>` so `BeginPresent` receives
exactly the target face for the negotiated kind — no neutral-context downcast in the backend.

---

## Resource handles

- **`IGeometry : IDisposable`** — the geometry currency. `Rect Bounds { get; }`, `bool FillContains(...)`,
  transform/combine ops. Skia impl wraps `SKPath` (`SkiaGeometrySource2D`); the `SKPath` never escapes it.
- **Geometry builders** (on the separate `IGeometryFactory` seam) — split so imperative and whole-shape
  construction can't interleave:
  - `IGeometryBuilder` — common base: `GeometryFillRule FillRule { get; set; }`, `IGeometry Build()`.
  - `IPathBuilder : IGeometryBuilder` — pen verbs: `MoveTo`, `LineTo`, `CubicTo`, `QuadraticTo`, `ArcTo`, `Close`.
  - `IPrimitiveGeometryBuilder : IGeometryBuilder` — whole primitives (rects/ellipses/etc.).
- **`IShader`** — opaque gradient/shader handle (from the backend's `Create*GradientShader`).
- **`IColorFilter`** — blend-mode / color-matrix filter handle.
- **`IEffectFilter`** — a realized neutral `EffectNode` tree, or a drop-shadow filter.
- **`IImage`** — neutral **CPU** pixels: `int PixelWidth/PixelHeight` + `CopyPixels` (BGRA8888 premul). The
  decode/snapshot form. Not itself disposable (its owner is).
- **`ITexture : IDisposable`** — backend-resident **GPU** form (wgpu texture / `SKImage`). The currency the
  draw verbs consume (`DrawImage`) and that `RenderOffscreen` returns, so an offscreen result is sampled directly
  with no CPU round-trip. Caller-owned, disposed deterministically.
- **`ImageFrames : IDisposable`** — decode/upload result, now a **concrete** neutral class (was an interface):
  `IReadOnlyList<IImage> Frames`, `IReadOnlyList<int> DurationsMs` (one entry = still image; several =
  animation). Owns frame lifetime. On the `IImageDecoder` seam.

**Produce vs. read-back.** `RenderOffscreen` yields an `ITexture` (stays on the backend — nine-slice, color
glyphs and rendered SVG sample it straight). Pulling CPU pixels out (`RenderTargetBitmap`, snapshots) is the one
inherently-async step — a GPU→CPU map can't block the browser's single JS thread — so it is isolated to
`SnapshotAsync(ITexture) : Task<IImage>`. Skia returns a completed task; WASM WebGPU maps via JS `mapAsync`;
desktop WebGPU blocks a poll. Everything else in the seam (record/draw/present/geometry) stays synchronous.

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
positioned backend textures (`PositionedGlyphImage.Image` is an `ITexture`, drawn via `DrawImage` and
disposed by the caller). Obtained from `FontDetails.FontHandle`.

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

- **Shaping IS abstracted — it lives on `IFont` (`Shape`).** (Per the vision `uno-drawing-backend-design.md`
  §E; an earlier draft here said the opposite — that was wrong.) Shaping is font/engine-dependent yet
  render-independent, so it's a **font capability**: `IFont.Shape(text, direction)` returns a neutral
  `GlyphRun` (glyphs + pixel offsets/advances + clusters). The shaper (HarfBuzz today; CoreText/DirectWrite
  possible) is an **implementation detail of the handle** — `SkiaFont`/`ManagedFont` each build their own
  HarfBuzz face internally, so **no raw sfnt tables leak onto the seam** (`GetFontTable`/`UnitsPerEm` were
  removed from `IFont`). The text engine no longer references `HarfBuzzSharp`.
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
| **shaping** (string → glyphs+positions) | HarfBuzz on `SKTypeface` tables, driven by the text engine | ✅ **`IFont.Shape`** — returns a neutral `GlyphRun`. `SkiaFont`/`ManagedFont` each own a private HarfBuzz face (from the `SKTypeface`'s tables / from the font bytes); the shaper never leaks — `GetFontTable`/`UnitsPerEm` are **off** the seam. HarfBuzz stays (not Skia). |
| value types (`SKPoint`/`SKRect`/`SKColor`/`SKPath`) | pervasive in layout/draw | ✅ existing equivalents — `Vector2`, `Windows.Foundation.Rect` (via `Point`), `Windows.UI.Color`, `IGeometry` (spell-check squiggle via `IPathBuilder`). Also collapsed the vestigial `SKPaint` color-carrier to `Color`. |
| **font resolution + fallback** (family/weight/style → face; *which* font covers a codepoint) | `SKTypeface.FromFamilyName`, `SKFontManager.MatchCharacter` | the genuinely new part → **`IFontManager` seam** (below) **(not yet done)** |

So the font *handle* (`IFont`) absorbs shaping + metrics + coverage — none of it a separate abstraction, and
none throwaway (it's the target regardless of when resolution is replaced). `FontDetails` now holds just an
`IFont` + size (no `SKFont`/`SKFontMetrics`, no HarfBuzz `Font`); the text engine calls `FontHandle.Shape(...)`
and consumes the neutral `GlyphRun` (pixel-space), so `UnicodeText`/`Run`/`Inline`/`FontDetails` no longer
reference `HarfBuzzSharp` at all.

**Status:** the `IFont` absorption is **done and validated** — the text layout/measure/draw code (`FontDetails`,
`Inline`, `UnicodeText`, `ParsedText`, `Run`) reads shaping/metrics/coverage only through `IFont`, and the
engine no longer imports `HarfBuzzSharp`. Shaping moved onto `IFont.Shape` (each backend owns its HarfBuzz face;
`GetFontTable`/`UnitsPerEm` are off the seam). `Given_TextBlock` runtime tests pass **103/103** on both the
default Skia path *and* the fully Skia-less managed-font path (`ManagedFont.Shape` end-to-end); `Given_TextBox`
is unchanged at its pre-existing headless baseline (19 failures before and after this change — 18 pointer +
1 tab-AA — none introduced here). The **value-type sweep is also done** — `UnicodeText`/`ParsedText` layout &
draw code uses `Vector2`/`Rect`/`Color`/`IGeometry`. **Still Skia in the text layer:** only font
resolution/fallback — the `IFontProvider` seam has a Skia-backed default (`SkiaFontProvider`); native providers
(CoreText/DirectWrite/fontconfig) are the remaining step to a Skia-*free* core on every platform.

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
`GetDefaultFont`, all returning `IFont`) hangs off `IDrawingFactory.FontManager`. `SkiaFontManager` holds all
the Skia resolution (`SKTypeface.FromData`/`FromFamilyName`, `.ttc` face selection, variable-font axes,
`SKFontManager.MatchCharacter`); `FontDetailsCache`/`Run`/`UnicodeText` now resolve through it with `IFont`
currency and no direct Skia. `ManagedFontManager` (option A) is the **SkiaSharp-free** resolver: it indexes the
OS font directories by parsing each face's `name`/`OS-2`/`head` tables, matches family + weight/width/italic
with a nearest-score, produces `ManagedFont` handles, and does codepoint fallback by scanning indexed `cmap`s.
With it selected, the entire font path — resolution, metrics, coverage, shaping-tables, outlines — is
Skia-free (only the final geometry rasterization is the drawing backend's job). Validated: `Given_TextBlock`
103/103 on the default Skia resolver *and* with the managed resolver (DejaVu resolved via managed lookup on Linux).

**The resolver is pluggable by interface, not a managed-specific flag.** `FontProvider.Current` is an
`IFontProvider` — an app assigns any implementation (`ManagedFontProvider`, or a platform-specific one), and a
backend's `Register()` fills in its own default (Skia's resolver) only if the app registered nothing. This matters
because `ManagedFontProvider` — which discovers fonts by **enumerating filesystem font directories** — is the right
resolver only where that model holds:

| Target | Filesystem fonts? | Managed resolver | What to use |
|---|---|---|---|
| Windows / macOS-desktop / Linux | yes (`C:\Windows\Fonts`, `/usr/share/fonts`, …) | ✅ works | `ManagedFontManager` or default Skia |
| Android (Skia) | yes (`/system/fonts`) | ✅ works | `ManagedFontManager` or default Skia |
| **iOS** (Skia) | **no** — the app sandbox blocks reading `/System/Library/Fonts` | ❌ finds nothing | default Skia (SkiaSharp→CoreText), or a future CoreText `IFontManager` |
| **WebAssembly** (Skia) | **no** — the browser sandbox has no OS font directory | ❌ finds nothing | default Skia (bundled fonts), or a bundled/`@font-face` `IFontManager` |

So on iOS and WASM the *managed filesystem* resolver can't see fonts; those platforms keep the default Skia
resolver (SkiaSharp routes to CoreText on iOS and to bundled fonts on WASM) until a platform-native
`IFontProvider` (CoreText via `CTFontManager`; a bundled/browser resolver for WASM) is written and plugged in —
exactly what the pluggable seam is for. **Backend selection moved off environment variables to registration:**
`ImageDecoder.Current` and `FontProvider.Current` are backend-independent seams that accept **any** implementor;
geometry lives on the drawing backend (the renderer must understand it), so a managed-geometry path implementor is
a registered `IDrawingFactory` (`SkiaManagedGeometryDrawingFactory` = managed geometry + Skia pixels). There is no
`DrawingBackendOptions` and no skia-vs-managed flag. An app registers what it wants before the backend's
`Register()`, which installs its own defaults only where nothing was registered (the SamplesApp host bridges the
former `UNO_MANAGED_*` env vars to these registrations as a dev/test affordance).

---

## Image decode — `ManagedImageDecoder`

The decode/upload seam is `IDrawingFactory.TryDecodeImage` (encoded bytes → `ImageFrames`, incl. EXIF /
downscale / animation) and `CreateImageFrame` (raw BGRA → `ImageFrames`). The Skia backend uses `SKCodec`;
the neutral frame providers (`SingleFrameProvider`/`AnimatedImageFrameProvider`) hold `ImageFrames` and are
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

## Lottie seam — `ILottieRenderer`

Lottie (Bodymovin JSON) plugs in through a backend-neutral seam, mirroring `ISvgRenderer`:

```csharp
public interface ILottieRenderer { ILottieAnimation? Load(string animationJson); }
public interface ILottieAnimation : IDisposable
{
    Vector2 Size { get; }
    TimeSpan Duration { get; }
    void Render(IDrawingSession session, float progress, Rect area);   // seek + draw one frame
}
```

- **Renderer** — the `Uno.UI.Lottie` add-in's `SkottieLottieRenderer` wraps `SkiaSharp.Skottie`. Its `Render`
  fast-paths on `NativeSurface is SKCanvas` (draws straight into the frame), else rasterizes to an offscreen and
  goes through `session.Factory.CreateTexture(...)` → `DrawImage` — so Lottie plays on WebGPU too (the old
  `SKCanvasElement` path only had a GL-island fallback). Resolved reflectively by the host builder when the
  add-in is referenced, with a `.LottieRenderer(...)` override; `LottieRenderer.Current` is null when none is
  registered, and the `AnimatedVisualPlayer` then shows its fallback content (no silent drop).
- **Visual** — `LottieVisualSource`'s `IAnimatedVisual` is a plain `ContainerVisual` that overrides `Paint` to
  call `ILottieAnimation.Render(session, progress, area)`, reading the player-driven `Progress` scalar
  (`AddContext` repaints it each tick). The source is now SkiaSharp-free; only the Skottie renderer references
  SkiaSharp. A managed (SkiaSharp-free) Lottie engine could later register against the same seam.

---

## Value types & enums

Neutral structs on the verbs: `RoundRectangle`, `StrokeStyle`, `PositionedGlyphImage`. Enums:
`GeometryFillRule`, `GeometryCombineMode`, `StrokeCap`, `StrokeJoin`, `ClipOperation`, `BlendMode`,
`GradientTileMode` (Clamp/Repeat/Mirror), `ImageSampling`.

---

## Backends

- **Default: SkiaSharp** — `SkiaDrawingFactory`, `SkiaDrawingFactory`, and the `Skia*` handle/session classes.
- **Managed/alternative seams** (SkiaSharp-free, opt-in via env toggle, prove the seams are neutral):
  `ManagedFont` (`UNO_MANAGED_FONT_BACKEND`), `ManagedImageDecoder` (`UNO_MANAGED_IMAGE_DECODER`), and
  `ManagedSvg` (primary; no toggle needed).

### Validation methodology
Each seam is proven by building the *same artifact two independent ways and comparing*: an alternative
backend renders/decodes with zero SkiaSharp, and its output is compared pixel-for-pixel against the Skia
backend (e.g. `Given_IFont_AlternativeBackend`, and pixel-parity of the managed decoders/SVG). Identical
output is the evidence the interface carries everything the backend needs.

### WebGPU backend — per-target status & how to enable

`Uno.UI.Composition.WebGpu` is the second full render backend behind the neutral seam (`WebGpuRenderer` /
`WebGpuCommandRecorder` / `WebGpuPresentSession` + the device-bound `WebGpuDrawingFactory`). It binds the
**modern `webgpu.h` ABI** through a generated, Silk-free interop layer (`Native/WebGpuInterop.cs`, shared by
all targets — Dawn and wgpu-native agree on the ABI); the native library is provisioned at build (desktop
`wgpu-native` via `wgpu-native.targets`, browser `emdawnwebgpu` via `wgpu-wasm.targets`).

**On-window presentation** splits into two shapes, both opt-in via `UNO_WEBGPU=1|true|swapchain` (native hosts
also expose a builder enum):

- **Native swapchain** (X11, Win32, macOS, …) — one shared `WebGpuSwapChainContext` owns the device + surface +
  acquire/configure/present, parameterized by a platform surface factory (`CreateXlibSurface` /
  `CreateHwndSurface` / `CreateMetalSurface`). MSAA-resolves into the swapchain texture and presents with
  `wgpuSurfacePresent`. A new native host is just its surface factory + its host's present hook.
- **Browser** (`WebGpuBrowserGraphicsContext`) is separate: emdawnwebgpu presents the canvas *implicitly* on
  return to the event loop (no `wgpuSurfacePresent`), and SwiftShader composits the canvas only from a render
  pass into it — so it resolves into an offscreen texture and **blits** that into the canvas' current texture.

| Host | Wiring | Runtime state |
|------|--------|---------------|
| **X11** | `X11SoftwareGraphicsContext` factory → `WebGpuSwapChainContext(CreateXlibSurface)` | ✅ validated headless (Xvfb + lavapipe): render tests pass, "WebGpu context via WebGpuRenderer" |
| **WebAssembly (browser)** | `BrowserRenderer` opt-in → `WebGpuBrowserGraphicsContext` (async device init, canvas-selector surface, blit present) | renders with **zero Dawn validation errors** headless; visual pixels need a real-GPU browser (headless SwiftShader doesn't capture WebGPU-canvas output in screenshots — reproduced with pure-JS WebGPU) |
| **Win32** | `Win32RenderingBackend.WebGpu` → `WebGpuSwapChainContext(CreateHwndSurface)` + `Win32WebGpuRenderer` bridges the render thread | build-validated (net10.0 on Linux); GPU present needs a real Windows GPU — same swapchain path as validated X11 |
| **Android (Skia)** | `UnoSKWebGpuView` (SurfaceView) → `WebGpuSwapChainContext(CreateAndroidSurface)` from the Surface's ANativeWindow, opt-in `UNO_WEBGPU` | build/runtime needs the mobile CI restore context + a device (the base Android host doesn't build standalone in a plain Linux container — a pre-existing TFM-graph limitation, not this change); the wgpu-native android `.so` must be packaged per-ABI as `AndroidNativeLibrary` |
| **macOS** | `CreateMetalSurface` ready, but the native ObjC helper (`UnoNativeMac`) owns the `CAMetalLayer`/present; WebGPU needs it to expose the layer and cede present — a native change + a Mac |
| **FrameBuffer** | surfaceless — `wgpu-native` has no KMS/DRM surface source, so WebGPU would render **offscreen + read back** and present via the host's existing framebuffer path (a GPU→CPU copy per frame); not yet wired |

### Offscreen VRAM lifecycle (WebGPU)

Every layer/mask/path-clip-coverage/shadow/backdrop renders into a transient offscreen (`WebGpuRenderSurface`)
rented from a per-device pool. Each such surface has three parts with **different lifetimes** within a frame:

- **MSAA colour** and **depth/stencil** are *write-only inside that surface's own render pass* — the MSAA colour
  resolves into the single-sample view and the depth is discarded; neither is sampled afterwards. They are
  returned to the pool the instant the pass ends (`WebGpuTexturePool.Return`) so the next same-size pass reuses
  them. One MSAA+depth pair per size serves a whole frame regardless of how many offscreens it has.
- **The single-sample resolve view** is sampled later (as the layer/coverage/backdrop texture in the main pass),
  so it stays `InUse` until the frame's `BeginFrame`.

The pool evicts entries unused for 16 frames (so a window resize doesn't strand a whole generation of full-window
textures) and `WebGpuDevice.Dispose` releases it. This is what keeps a layer/clip/acrylic-heavy frame from
allocating one full MSAA+depth+resolve triple per offscreen (which exhausted VRAM on real GPUs). The swapchain
`WebGpuRenderSurface` owns only its MSAA+depth; the acquired swapchain image is borrowed and released by
`WebGpuSwapChainContext.Present`, so its `Dispose` must not double-free it on resize.

### Parity gaps vs the original X11 WebGPU branch (`ramez/webgpu-experiment`)

A line-by-line audit against the pre-seam branch drove a series of fixes. **Fixed:** the VRAM/crash items above;
the native swapchain now blits an offscreen resolve into the acquired image (a direct MSAA-resolve into the
swapchain didn't composite); opaque acrylic short-circuits to a tint fill; `SaveLayer(IColorFilter)` colour
matrices apply to solid fills; radial gradients are exact under rotation/skew; a blur is only routed to the
acrylic path when it actually samples the backdrop; MSAA/depth are `StoreOp.Discard`ed after resolve; the main
pass dedups redundant scissor changes.

**Still-open** (backend-internal quality; none affect the neutral-seam contract). These are larger reworks that
want real-GPU visual validation:

- **Acrylic (translucent):** still re-renders the whole command prefix into a full-window surface per backdrop
  (O(n²)); the original blurred only the element's padded region sampled from the already-rendered target — the
  fix needs the resolve-then-sample rearchitecture below. Also missing: procedural noise/grain and the rounded-corner mask.
- **Bounds-sized offscreens:** layer/coverage offscreens are full-window; sizing them to the element/clip AABB
  needs `Ndc`/`SetScissor` parameterized by a target origin+size (the same rearchitecture as translucent acrylic).
- **Nested clips:** only the innermost rounded-rect / path clip survives; the original intersected a full clip
  stack (needs a shader-side clip stack — touches the ubiquitous clip path, so validate carefully).
- **Gradient** stops are capped at 16 (the original baked a 256-entry LUT). **No analytic rounded-rect/border**
  primitive. **No glyph atlas** (color glyphs upload a texture per glyph per frame, and only render through a
  WebGPU-native font texture — a `SkiaImageTexture` color glyph is a no-op in `DrawImage`).
- **Perf:** the replay path is now zero-allocation for a static frame (reused scratch + pooled op list); the
  on-window present can pipeline via `UNO_WEBGPU_PIPELINE=1` (opt-in — non-blocking poll; default still drains
  each frame, pending a real-GPU tearing check). Still open: per-frame full vertex/uniform re-upload (no
  persistent dirty-range slabs), rect-only draw-call coalescing, no per-visual transform-restamp (a moved cached
  visual — e.g. scrolling — rebuilds its geometry), and no DPI-aware 1× MSAA (needs a no-resolve pipeline variant).

---

## Backend registration & graphics negotiation (implemented; all Skia hosts neutral)

This is the model the whole pluggable story hangs on. It has five moving parts — registration,
negotiation, the host's contribution, the framework's contribution, and the per-frame contract —
plus a clear answer to "who drives."

> **Implementation status (seam v2 — the host owns per-kind window+context creation).** Every Skia
> host — X11, Win32, macOS, WebAssembly, Linux.FrameBuffer, AppleUIKit, Android — is backend-agnostic.
> The host's *entire* contribution is one delegate:
> `GraphicsRegistry.ContextFactory = (GraphicsContextKind) => Task<IGraphicsContext?>`, which creates a
> **window+context for the negotiated kind** (a fresh window when the kind needs one — e.g. an X11 GLX
> visual; or the host's existing kind-agnostic window reused — e.g. a Win32 HWND), or returns `null` to
> **decline** (unavailable, or the host's config opted out). The host switches purely on `kind` and
> names no backend and no `UNO_WEBGPU`. **The backend owns the kind order** (`IGraphicsProvider.PreferredContexts`,
> walked as-is); preference is expressed two neutral ways only — the app orders the provider's kinds via
> its constructor (`new SkiaGraphicsProvider(GraphicsContextKind.Software)` forces software), and a host
> *declines* kinds per its own config (Win32 `UseOpenGLOnWin32`, X11 `PreferGLESOverGLOnX11`, LinuxFB
> `UseDRM`, WASM `forceSoftwareRendering`). Removed with the redesign: `INativeWindow`, `NativeWindowKind`,
> `GraphicsRequirements`, the WebGPU self-registration, `Initialize`'s `(window, preferredKinds)` params.
> `DrawingFactory` is now `internal`.
>
> **WebGPU** is split into a lightweight, host-referenced **`Uno.UI.Composition.WebGpu.Init`** (the
> renderer-agnostic on-window `WebGpuContext.Create{Win32,X11,Metal,Android,WasmAsync}` + swapchain/device +
> raw bindings; carries no desktop native asset) and the app-referenced renderer `Uno.UI.Composition.WebGpu`
> (the pipelines + `wgpu-native.targets`). A host references only `Init` and calls `WebGpuContext.Create*`
> directly (no reflection); the expensive desktop `wgpu-native` ships only when an app pulls the renderer,
> while the small WASM emdawnwebgpu bridge (~a few hundred KB in the 12 MB runtime `.wasm`) is always linked
> via `Init`'s targets so the WASM host's direct call always resolves.
>
> **Verified** here: X11 runtime-proven headless on lavapipe for **all four kinds** (`OpenGL`, `OpenGLES`,
> `Software`, `WebGpu`), each negotiating and rendering. Win32/macOS/WASM/LinuxFB compile clean on Linux;
> Android compiles (net10.0-android); the WASM app head links emdawnwebgpu clean without reflection. Per-platform
> runtime (Win32/macOS/iOS/Android/WASM/DRM) is for on-device validation; AppleUIKit is unbuilt here only for lack
> of the iOS workload. Vulkan on X11 still awaits a real-GPU matrix before its branch migrates.

### One factory with a typed present half (the two seams merged)

The earlier design kept `IDrawingFactory` (resources) and `IRenderBackend` (present) as a separately-registered
matched pair. They are now **one interface**: `IDrawingFactory` carries the resources + `CreateRecording()`, and
the present half is the **typed** `IDrawingFactory<TTarget>` where `TTarget : IRenderTarget`. A backend
implements `IDrawingFactory<TTarget>` **once per render-target kind it can present**
(`IDrawingFactory<IVulkanRenderTarget>`, `IDrawingFactory<IWebGpuRenderTarget>`, …). Because there is one
object, the resources and the present session are always the same family by construction — no pairing to keep
in sync.

### Handle model — why factory and renderer are one family

`IShader` / `IColorFilter` / `IEffectFilter` are **opaque** and consumed *only* by the same backend that
minted them, which downcasts them to its own concrete type. A `WebGpuShader` cast can never receive a
`SkiaShader`, so the guarantee is that the thing producing the handles is the same object presenting them —
delivered by the single `IDrawingFactory` above. (`IGeometry` / `IImage` are the opposite: core itself
introspects them — `Bounds`, `FillContains`, `Combine`, pixels — so they carry neutral members + readbacks and a
*shared managed* implementation is legitimate. Rule of thumb: consumed only by the backend → opaque + cast-back;
introspected by core → neutral abstraction.)

### Registration — user-side, uniform, no default

There is **no default backend**. The core references no backend package; `Uno…Skia` is one
optionally-referenced package among peers, so an app that registers only WebGPU never links Skia and
the linker drops it. The app registers an **ordered list of `IGraphicsProvider`** with one process-static call,
identical on every platform, in the shared app bootstrap:

```csharp
// ordered by preference: try WebGPU, fall back to Skia
GraphicsRegistry.Register(new IGraphicsProvider[] { new WebGpuGraphicsProvider(), new SkiaGraphicsProvider() });
```

Separately, the **host** sets exactly one delegate — `GraphicsRegistry.ContextFactory =
(GraphicsContextKind) => Task<ISwapChain?>` — which builds the window+context for a kind or returns `null` to
decline. No `[ModuleInitializer]` self-registration (cross-assembly init order is undefined and it would bake a
backend into core); unregistered access throws with a message naming the fix. If the app registers nothing,
negotiation lights up the implicit Skia provider when that assembly is present. `DrawingFactory.Current` and the
present session are process-global → one backend per process.

### Negotiation — lazy, ordered, create-until-success

Two ordered lists — the app's **provider list** and each provider's **`PreferredContexts`** kind list (the app
never needs to know Skia prefers Vulkan over GL). Nothing is created speculatively; the host makes a
window+context per kind on demand until one binds:

```
foreach provider in registeredProviders:            // app's order
    foreach kind in provider.PreferredContexts:      // backend's order
        ISwapChain? ctx = await ContextFactory(kind) // host builds window+context, or null to decline
        if ctx is null: continue                     // kind unavailable / host opted out → next kind
        // narrow ctx to the device face this kind implies (IVulkanDeviceContext / IWebGpuDeviceContext / …)
        // and hand it to the matching typed IGraphicsProvider<TContext>.CreateGraphics(ctx):
        IDrawingFactory? factory = CreateGraphics(kind, provider, ctx)
        if factory is null: ctx.Dispose(); continue  // backend has no provider for this kind
        // bind-time capability gate: backend must also implement IDrawingFactory<TTarget> for this kind,
        // else it could win a kind it can't present (crash on the first frame):
        if !CanPresent(kind, factory): dispose(factory, ctx); continue
        DrawingFactory.Register(factory); return { provider, ctx, factory }   // first success wins; stop
throw "no graphics backend could initialize on this host (attempted: …)";
```

The kind→device-face narrowing and the `CanPresent` gate live Uno-side (keyed on the closed `GraphicsContextKind`
set), so the backend reads its device details through a typed context without ever downcasting a neutral one.

The host's context factory does a cheap availability probe before the real init (as Uno already does —
`X11VulkanSurfaceFactory.IsVulkanAvailable()`) and must fully clean up on failure (a `null` return means "as if
never attempted"). Treating a failed `CreateGraphics` narrowing or a failed `CanPresent` gate the same way — dispose
the context and try the next kind — keeps the walk robust.

### Who provides what

- **Host provides one delegate** — `GraphicsRegistry.ContextFactory = (GraphicsContextKind) =>
  Task<ISwapChain?>`. Given a kind it builds the window+context (a fresh window when the kind needs one —
  an X11 GLX visual — or its existing kind-agnostic window reused — a Win32 HWND), owns the swapchain +
  acquire/present, and returns it, or `null` to **decline** (unavailable, or its config opted out). The host
  switches purely on `kind`; it names no backend and no `UNO_WEBGPU`. This replaced the earlier `INativeWindow`
  hand-off (removed, with `NativeWindowKind`/`GraphicsRequirements`). For the GPU kinds the host delegates the
  hard init to shared Uno helpers — `WebGpuContext.Create{X11,Win32,Metal,Android,WasmAsync}` and the
  `*VulkanSurfaceFactory` set — so per-host code stays tiny. The **`Software`** target is a neutral CPU
  framebuffer (`ISoftwareRenderTarget`: `Pixels`+`RowBytes`), wrapped by each backend (Skia via
  `SKSurface.Create(info, ptr, stride)`; a managed rasterizer via the raw bytes).
- **Backend consumes** the narrowed device face, builds its pipelines + its own scratch/stencil from it, and
  fills the kind-matched render target. It writes **no** graphics-init and **no** windowing code.

### Contexts, not surfaces — and capabilities

The negotiation currency is the **context** — the GPU-API connection/device, exposed to the backend through a
typed device face (`IWebGpuDeviceContext` = Instance/Adapter/Device/Queue + color format/sample count;
`IVulkanDeviceContext`; `IGLDeviceContext`; `IMetalDeviceContext`) — that's where the API family and the hard
platform init live. WebGPU is a first-class provided context, not a special case; its handles are opaque `nint`s
so the same face serves **Dawn or wgpu-native** interchangeably.

There is **no `GraphicsRequirements` struct** any more. A backend's support requirement is expressed structurally:
it only implements `IGraphicsProvider<TContext>` for the device faces it understands and `IDrawingFactory<TTarget>`
for the targets it can present, and the negotiator's bind-time **`CanPresent` gate** skips a kind the backend
can't present (so it can never win one it would crash on). Color format/sample count travel on the device face /
render target; the backend still allocates its own depth/stencil/scratch (technique- and size-specific, never
presented) from the context.

### The render target is a view — surface vs texture stays internal

In every API the render target is a **color attachment view of a uniform type regardless of origin**
(WebGPU `TextureView`, Vulkan `VkImageView`, Metal `MTLTexture`, GL framebuffer) — a swapchain image
and an offscreen texture produce the *same* type. So the backend is handed a **render-target view**
and never learns whether it's backed by the window swapchain or a retained offscreen. The dirty
region is expressed as a **clip in the recorded frame** (backend-independent), so the backend just
replays the clipped frame; it is **damage-agnostic**.

Retention — the invariant that pixels *outside* the dirty region still hold the previous frame — is
entirely host-side, declared by the internal **`ISwapChain.PreservesContents`** (the framework reads
it in `CompositionTarget.Draw` to choose partial repaint vs. a full clear). **The backend never sees
it** and always renders straight into the target it is handed. It's `true` when the host hands back a
stable target across frames: the **software** buffer is reused (CPU framebuffer / DIB), and the
**Vulkan** path renders into a stable render image and blits it. **GL and Metal** present an undefined
back buffer (SwapBuffers leaves it undefined; a Metal drawable is fresh each frame) and have no
host-side retention yet, so they report `false` (full repaint each frame); giving those hosts a
persistent FBO / texture to restore partial repaint is a follow-up. The render-target view is
**kind-matched** (opaque to core; a `WebGpu` provider mints the one a WebGPU backend downcasts).

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
public enum GraphicsColorFormat { Bgra8888, Rgba8888 }

// The host's ONLY contribution: build a window+context for a kind, or null to decline. Async because one
// kind (WASM WebGpu) imports its device from JS; native hosts return an already-completed task. (internal)
internal delegate Task<ISwapChain?> GraphicsContextFactory(GraphicsContextKind kind);

public interface IGraphicsContext : IDisposable { GraphicsContextKind Kind { get; } }   // thin device/init handle

// Device faces the negotiator narrows a context to, so the backend reads device details without casting
// a neutral context. (Instance/Device/Queue etc. are opaque nint handles — Dawn or wgpu-native, GL, Vulkan, Metal.)
public interface IGLDeviceContext     : IGraphicsContext { GLFlavor Flavor { get; } Func<string,nint> GetProcAddress { get; } }
public interface IVulkanDeviceContext : IGraphicsContext { nint Instance { get; } nint PhysicalDevice { get; } nint Device { get; } nint Queue { get; } /* + queue family, extensions, GetProcAddress */ }
public interface IMetalDeviceContext  : IGraphicsContext { /* device/queue/layer handles */ }
public interface IWebGpuDeviceContext : IGraphicsContext { nint Instance { get; } nint Adapter { get; } nint Device { get; } nint Queue { get; } uint ColorFormat { get; } uint SampleCount { get; } }

internal interface ISwapChain : IGraphicsContext          // what the host factory returns; Uno-internal
{
    IRenderTarget AcquireRenderTarget(int width, int height); // caches by size; CompositionTarget.Draw acquires each frame
    void Present();
    bool PreservesContents => false;                          // host-only retention signal; the backend never sees it
}

public interface IRenderTarget : IDisposable { int Width { get; } int Height { get; } GraphicsColorFormat ColorFormat { get; } }
// kind-matched color-view faces the backend downcasts to:
public interface IGLRenderTarget       : IRenderTarget { /* framebuffer id */ }
public interface IVulkanRenderTarget   : IRenderTarget { /* image + view */ }
public interface IMetalRenderTarget    : IRenderTarget { /* drawable/texture */ }
public interface IWebGpuRenderTarget   : IRenderTarget { nint ColorView { get; } }
public interface ISoftwareRenderTarget : IRenderTarget { nint Pixels { get; } int RowBytes { get; } }

public interface IGraphicsProvider { IReadOnlyList<GraphicsContextKind> PreferredContexts { get; } }   // the registerable unit
public interface IGraphicsProvider<TContext> : IGraphicsProvider where TContext : IGraphicsContext
{
    IDrawingFactory CreateGraphics(TContext context);   // implemented once per device-face kind the backend serves
}

public interface IDrawingFactory { /* resources + CreateRecording() — see "Registration & factory" */ }
public interface IDrawingFactory<in TTarget> : IDrawingFactory where TTarget : IRenderTarget
{
    IPresentSession BeginPresent(TTarget target);       // implemented once per render-target kind the backend presents
}

internal static class GraphicsRegistry                  // negotiation hub (internal; hosts/bootstrap reach it)
{
    public static GraphicsContextFactory? ContextFactory { get; set; }                 // host sets this
    internal static void Register(IReadOnlyList<IGraphicsProvider> providersInPreferenceOrder);   // app/bootstrap
    public static GraphicsInitialization Initialize();                                 // sync; wraps InitializeAsync
    public static Task<GraphicsInitialization> InitializeAsync();
    // no default; unregistered access throws. Implicit Skia provider lit up when the app registered nothing.
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

**Assembly layout:** the public seam lives in its own SkiaSharp-free `Uno.UI.Composition.Drawing` assembly;
`Uno.UI.Composition` is also SkiaSharp-free; the Skia backend lives in a separate `Uno.UI.Composition.Skia`
assembly, and the WebGPU backend across `Uno.UI.Composition.WebGpu(.Init)`. `Uno.UI` itself no longer references
SkiaSharp except the items below — the Vulkan GPU subsystem was relocated wholesale from `Uno.UI` into the Skia
backend assembly. **No assembly grants its internals (`[InternalsVisibleTo]`) to `Uno.UI.Composition.Skia` or
`Uno.UI.Composition.WebGpu`** — both backends stand entirely on the public seam (the WebGPU renderer engine was
moved into its own assembly precisely so nothing reaches into it), so a third-party backend has the same access
the built-in ones do: the public seam and nothing more.

**WebGPU backend native/ABI:** the experimental WebGPU backend is bound to the **modern `webgpu.h` ABI**
(no Silk.NET) and is split across two assemblies. The renderer-agnostic **init half** — the interop layer
(`Uno.UI.Composition.WebGpu.Init/Native/WebGpuInterop.cs`, generated by `gen_webgpu.py` from the vendored,
pinned `wgpu-native` headers), the `DllImportResolver` (`WebGpuLoader.cs`), and the on-window
`WebGpuContext.Create*` / swapchain / browser contexts — lives in **`Uno.UI.Composition.WebGpu.Init`**, which
a *host* references directly (no native asset of its own). The **renderer** (`WebGpuBackend`, pipelines) plus
the desktop native fetch (`wgpu-native.targets`, resolved through the init half's loader) stay in
`Uno.UI.Composition.WebGpu`, referenced only by an app that opts into WebGPU. Async device/adapter/buffer-map
use modern `CallbackInfo` + `wgpuInstanceProcessEvents`; strings cross as `WGPUStringView`. Runtime-proven
headless on X11 + lavapipe (on-window swapchain via `UNO_WEBGPU=swapchain`). **WASM is live** (emdawnwebgpu,
Dawn's `webgpu.h` emscripten port via `Uno.UI.Composition.WebGpu.Init/wgpu-wasm.targets`, always linked so the
WASM host's direct `WebGpuContext` reference resolves; the browser device is imported from `navigator.gpu`).

**Still Skia (the remaining path to fully dropping SkiaSharp):**
1. **The rasterizer itself** — the default `IDrawingSession`/`RenderOffscreen` pixel work is SkiaSharp. A
   fully Skia-free runtime needs an alternative `IDrawingFactory` that rasterizes (the largest remaining piece).
2. **Image decode fallbacks** — WebP **lossy (VP8)** and animated WebP (both need the ~2000-line RFC 6386
   VP8 intra decoder), TIFF, ICO still route to the Skia codec.
3. **`SKCanvasElement`/`SvgCanvas`** — app-facing "draw with raw Skia" controls in a separate package, so they
   are SkiaSharp *by contract* (the user draws on an `SKCanvas`); out of scope for core neutrality. But their
   placement is now backend-agnostic: `SKCanvasElement` draws **zero-copy** into the frame when the active
   backend exposes an `SKCanvas` via `IDrawingSession.NativeSurface`, and otherwise falls back to a Skia-on-GL
   island — so it works on any backend and names none. (The frame-rate-counter overlay's `SKFont` text shaping
   is the other diagnostic-only SkiaSharp use in `Uno.UI`.)
4. **Transitional internals** — a few backend-internal spots still hand an `SKImage` to the composition
   surface (the WASM browser-canvas decode path); these are inside/adjacent to the Skia backend, not on a
   pluggable interface.
