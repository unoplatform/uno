# Uno Pluggable Drawing-Backend Abstraction (work in progress)

> The abstraction built on branch `feature/drawing-backend-abstraction` so far. This is the current
> state of an in-progress effort to put Uno's Skia rendering behind interfaces so alternate 2D
> backends can be plugged in; the SkiaSharp backend is the default implementation. Signatures are
> transcribed from source. See `doc/avalonia-rendering-abstraction.md` for the reference design this
> was informed by.

## Design in one paragraph

We took the **"refined Track 1"** shape: a Skia-shaped but backend-neutral seam rather than Avalonia's
fully-declarative one. Everything crossing the boundary is split by lifetime/role:

- **Transient paint** (color, stroke, blend, AA) crosses **by value** inline on the draw verbs — `PaintParams`.
- **Expensive resources** (geometry, shaders, color/mask filters, images, effects) cross as **opaque handles** manufactured by a factory (`IDrawingBackend`), because they're queried/cached outside any draw call.
- **Per-visual/frame retained state** (Skia's `SKPicture`) crosses as an **opaque `IRenderData`** the backend owns; composition never inspects it. This is behind the **optional** `IRetainedRenderingSession` capability — a backend that doesn't advertise it is simply re-drawn every frame.
- **Value types are neutral** (`Windows.Foundation.Rect`, `Windows.UI.Color`, `System.Numerics.Vector2`/`Matrix3x2`/`Matrix4x4`) — no SkiaSharp type appears on any interface (with the transitional exceptions noted under *Status*).
- **The render cycle stays in Uno**, not the backend. `CompositionTarget` owns scheduling/vsync/threading and the visual-tree walk (traversal + caching + picture-collapsing) stays single-and-unified in `Visual.skia.cs`; the backend is a **passive two-phase participant** (`IRenderBackend`: record in phase 1, present in phase 2).

Layers, bottom to top: **resource factory** (`IDrawingBackend`) → **drawing session** (`IDrawingSession`, consuming `PaintParams` + handles; optionally `IRetainedRenderingSession` for recording) → **frame lifecycle** (`IRenderBackend`, record→`IRenderData`→present).

**Conventions.** All types are currently `internal` and live in namespace `Uno.UI.Composition.Drawing`
(files under `src/Uno.UI.Composition/Composition/Uno/Drawing/*.skia.cs`, compiled into the Skia target),
except `SkiaRenderBackend` and the `CompositionTarget` integration which live in `Uno.UI`. They are shaped
for a later, deliberate flip to `public` so third parties can implement a backend (see *Status*); that flip
hasn't happened yet.

---

## Registration & factory

### IDrawingBackend

- File: `.../Uno/Drawing/IDrawingBackend.skia.cs`
- **Provides:** the resource factory — manufactures the stateful handles that cross the boundary (path builders → geometry, gradient/color shaders, color/mask/effect filters, drop-shadow filters). Transient paint is passed inline on the session verbs, not made here.
- **Used by (public API):** the gradient brushes (`LinearGradientBrush`/`RadialGradientBrush`) for shaders; `ImageBrush`/`Image`/`BitmapIcon` and `AcrylicBrush` for color/effect filters; `ThemeShadow`/elevation for the blur mask filter; the `Geometry` types, `Shape` controls, `Border`, and the clip types for path/rectangle geometry.

```csharp
IPathBuilder CreatePathBuilder();
IGeometry CreateRectangleGeometry(Rect rect);
IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IShader CreateRadialGradientShader(Vector2 center, float radius, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IShader CreateTwoPointConicalGradientShader(Vector2 start, float startRadius, Vector2 end, float endRadius, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix);
IShader CreateColorShader(Color color);
IShader ComposeShaders(IShader outer, IShader inner);
IColorFilter? CreateOpacityColorFilter(float opacity);
IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode);
IColorFilter CreateColorMatrixColorFilter(float[] matrix);
IMaskFilter CreateBlurMaskFilter(float sigma);
IEffectFilter? CreateEffectFilter(IGraphicsEffect effect, Rect bounds, Func<string, CompositionBrush?> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput);
IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color);
```

| Member | Description |
|--------|-------------|
| `CreatePathBuilder()` / `CreateRectangleGeometry(Rect)` | Build an `IGeometry` — imperatively, or a ready-made rectangle. |
| `CreateLinearGradientShader` / `CreateRadialGradientShader` / `CreateTwoPointConicalGradientShader` / `CreateColorShader` / `ComposeShaders` | Gradient, solid-color, and composed `IShader`s in the current coordinate space. |
| `CreateOpacityColorFilter` / `CreateBlendModeColorFilter` / `CreateColorMatrixColorFilter` | Opacity modulation, blend-mode/monochrome tint, and 4×5 color-matrix `IColorFilter`s. |
| `CreateBlurMaskFilter(float)` | Gaussian blur `IMaskFilter` (analytic shadow). |
| `CreateEffectFilter(...)` | Realizes a neutral `IGraphicsEffect` graph into an opaque `IEffectFilter` (mirrors the public effect-brush graph; `sourceResolver` maps source-parameter names to input brushes). |
| `CreateDropShadowFilter(...)` | An offset + blur + color drop-shadow `IEffectFilter` (non-analytic shadow fallback). |

### DrawingBackend (static)

- File: `.../Uno/Drawing/DrawingBackend.skia.cs`
- Declaration: `internal static class DrawingBackend`
- Holds the process-wide `IDrawingBackend`, defaulting to the SkiaSharp backend.

```csharp
public static IDrawingBackend Current { get; }          // resolves to new SkiaDrawingBackend() if unset
public static void Register(IDrawingBackend backend);   // replace before first frame
```

---

## Drawing session

### IDrawingSession

- File: `.../Uno/Drawing/IDrawingSession.skia.cs`
- **Provides:** the immediate-mode, stateful drawing surface — the canvas verbs (transform, clip, draw, layer, effect backdrop). This is the *entire* obligation to implement a backend; retained recording is a **separate optional capability** (`IRetainedRenderingSession`), not part of this contract.
- **Used by (public API):** the visual-tree paint walk behind every on-screen element; every `CompositionBrush` via `TryPaint`; `Shape` controls via `CompositionSpriteShape`; `UIElement.Clip`/`Border` clipping via `CompositionClip.ApplyClip`; `TextBlock`/`Run` selection & caret fills.

```csharp
Matrix4x4 TotalMatrix { get; }
void SetMatrix(in Matrix4x4 matrix);
void Concat(in Matrix4x4 matrix);
void Translate(float dx, float dy);
void Scale(float sx, float sy);
int Save();
int SaveCount { get; }
void Restore();
void RestoreToCount(int count);
void SaveLayer(Rect? bounds = null, PaintParams? paint = null);
void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);
void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);
void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);
void Clear(Color color);
void DrawRect(in Rect rect, in PaintParams paint);
void DrawPath(IGeometry geometry, in PaintParams paint);
void DrawLine(Vector2 p0, Vector2 p1, in PaintParams paint);
void DrawCircle(Vector2 center, float radius, in PaintParams paint);
void DrawImage(IImage image, float x, float y, ImageSampling sampling, in PaintParams paint);
void SaveLayer(IEffectFilter filter);
void DrawEffectBackdrop(IEffectFilter filter, float opacity);
```

| Member | Description |
|--------|-------------|
| `TotalMatrix` | The current total transform, origin → current coordinate space. |
| `SetMatrix` / `Concat` / `Translate` / `Scale` | Transform manipulation. |
| `Save` / `SaveCount` / `Restore` / `RestoreToCount` | State stack; `Save` returns the count to restore to. |
| `SaveLayer(Rect?, PaintParams?)` | Begins an offscreen layer, optionally bounded, with an optional compositing paint applied on restore. |
| `SaveLayer(IEffectFilter)` | Begins a layer whose content is transformed by an effect filter on restore (e.g. a drop shadow derived from the drawn content). |
| `ClipRect` / `ClipRoundRect` / `ClipPath` | Clip by rect, round-rect, or geometry handle, with intersect/difference op. |
| `Clear(Color)` | Clears to a color. |
| `DrawRect` / `DrawPath` / `DrawLine` / `DrawCircle` | Primitive fills/strokes; paint passed inline. |
| `DrawImage(IImage, float, float, ImageSampling, PaintParams)` | Draws an image at (x, y) with a sampling mode. |
| `DrawEffectBackdrop(IEffectFilter, float)` | Applies an effect to the current surface content as an effect-brush backdrop (used by `CompositionEffectBrush`). |

### IRetainedRenderingSession

- File: `.../Uno/Drawing/IRetainedRenderingSession.skia.cs`
- **Provides:** the *optional* retained-recording capability a session may advertise — capture a run of draw calls as an opaque `IRenderData` and replay it cheaply. Kept off the core `IDrawingSession` so a third-party backend only has to provide the drawing verbs; a backend whose session doesn't implement this is simply re-drawn every frame (correct, just uncached).
- **Used by (public API):** the composition render walk's caching — per-visual (`_content`) and collapsed-subtree (`_childrenContent`) reuse — behind every on-screen element. Not a public API directly; it's what makes cached rendering possible.

```csharp
IRecordingSession CreateRecording(Rect cullBounds);
void Replay(IRenderData data);
```

### IRecordingSession

- File: `.../Uno/Drawing/IRecordingSession.skia.cs`
- **Provides:** a drawing session that captures into an `IRenderData`; `EndRecording()` finishes it. Extends `IDrawingSession` + `IRetainedRenderingSession` so recordings can nest.
- **Used by (public API):** the frame loop (`CompositionTarget`, which records the whole frame) and the caching walk above.

```csharp
IRenderData EndRecording();
```

---

## Frame lifecycle

### IRenderBackend

- File: `.../Uno/Drawing/IRenderBackend.skia.cs`
- **Provides:** the two-phase frame lifecycle — a **passive participant** in Uno's backend-agnostic render cycle. Scheduling/vsync/threading **stay in `CompositionTarget`** and the visual-tree walk stays in `Visual.skia.cs`; the backend only (1) hands back the recording session for phase 1 and (2) presents in phase 2.
- **Used by (public API):** `CompositionTarget` — i.e. all on-screen rendering. Resolved via `CompositionTarget.RenderBackend`.

```csharp
IRecordingSession BeginFrame();
void Present(IRenderData frame, SKCanvas target, Action<SKCanvas>? postPresent);
```

| Member | Description |
|--------|-------------|
| `BeginFrame()` | **Phase 1 (UI thread):** returns the session the render cycle walks the visual tree into; the cycle then calls `EndRecording()` to obtain the frame. |
| `Present(frame, target, postPresent)` | **Phase 2 (vsync):** presents a previously recorded frame onto the target. |

The cycle drives it as: `var s = backend.BeginFrame(); root.RenderRootVisual(s); var frame = s.EndRecording();` on the UI thread (via `CompositionTarget.Render` → `SkiaRenderHelper.RecordFrame`), then `backend.Present(frame, target, …)` on the next vsync (via `CompositionTarget.Draw`).

> The present target is still a SkiaSharp `SKCanvas` — the host swapchain surface hasn't been neutralized;
> that (and the per-platform host contract) is follow-up. The native-element clip path is computed by the
> agnostic cycle (`SkiaRenderHelper`), not the backend.

---

## Resource handles

Opaque, backend-created. Composition holds them without inspecting internals.

### IGeometry

- File: `.../Uno/Drawing/IGeometry.skia.cs`
- **Provides:** the crux resource — a geometry compute/query surface (bounds, hit-test, boolean combine, fill/stroke outline), not draw-time data (hit-testing runs with no canvas).
- **Used by (public API):** `CompositionPath`/`IGeometrySource2D` and the `Geometry` types; the `Shape` controls (`Path`/`Rectangle`/`Ellipse`/`Line`/`Polygon`/`Polyline`) via `CompositionSpriteShape`; `RectangleClip`/`InsetClip`/`CompositionGeometricClip`; `Border` corner clipping; `UIElement` hit-testing; the analytic drop-shadow silhouette.

```csharp
Rect Bounds { get; }
Rect TightBounds { get; }
bool FillContains(Vector2 point);
IGeometry Transform(Matrix3x2 matrix);
IGeometry Combine(IGeometry other, GeometryCombineMode mode);
IGeometry GetFilledGeometry(float trimStart, float trimEnd);
IGeometry GetStrokeFillGeometry(in StrokeStyle style);
```

| Member | Description |
|--------|-------------|
| `Bounds` / `TightBounds` | Loose (control-point) and tight (on-curve) bounds. |
| `FillContains(Vector2)` | Whether the filled interior contains the point. |
| `Transform(Matrix3x2)` | A new geometry with the matrix baked in. |
| `Combine(IGeometry, GeometryCombineMode)` | Boolean combination of two geometries. |
| `GetFilledGeometry(float, float)` | The fill region, optionally trimmed to [start, stop]. |
| `GetStrokeFillGeometry(in StrokeStyle)` | The fill region produced by stroking with WinUI semantics (caps, miter-clip, dash caps); result must be disposed. |

### IPathBuilder

- File: `.../Uno/Drawing/IPathBuilder.skia.cs`
- **Provides:** imperative path construction that produces an `IGeometry` (`Build()` resets it for reuse).
- **Used by (public API):** the `Geometry` types (`CompositionPathGeometry`, rounded-rectangle/rectangle/ellipse geometries) behind `Path`/`Rectangle`/`Ellipse`; `RectangleClip`; `Border` border/background outlines.

```csharp
void MoveTo(Vector2 point);
void LineTo(Vector2 point);
void CubicTo(Vector2 control1, Vector2 control2, Vector2 end);
void QuadraticTo(Vector2 control, Vector2 end);
void AddRectangle(Rect rect);
void AddRoundedRectangle(Rect rect, float radiusX, float radiusY);
void AddEllipse(Vector2 center, float radiusX, float radiusY);
void Close();
IGeometry Build();
```

- Imperative path construction; `Build()` produces the `IGeometry` and resets the builder for reuse.

### IShader

- File: `.../Uno/Drawing/IShader.skia.cs` (marker interface)
- **Provides:** an opaque shader handle (gradients, composed shaders). Referenced by `PaintParams.Shader`; built via `IDrawingBackend` and cached by the producing brush across frames.
- **Used by (public API):** `LinearGradientBrush`/`RadialGradientBrush` (via `CompositionLinearGradientBrush`/`CompositionRadialGradientBrush`), and the effect graph.

### IColorFilter

- File: `.../Uno/Drawing/IColorFilter.skia.cs` (marker interface)
- **Provides:** an opaque color-filter handle (opacity modulation, blend-mode/monochrome tint, 4×5 color matrix). Referenced by `PaintParams.ColorFilter`; built via `IDrawingBackend`.
- **Used by (public API):** `ImageBrush`/`Image` (opacity), `Image.MonochromeColor`/`BitmapIcon` (tint), `AlphaMaskSurface` (color matrix), and the effect graph.

### IMaskFilter

- File: `.../Uno/Drawing/IMaskFilter.skia.cs` (marker interface)
- **Provides:** an opaque mask-filter handle (Gaussian blur). Referenced by `PaintParams.MaskFilter`; built via `IDrawingBackend.CreateBlurMaskFilter`.
- **Used by (public API):** the analytic drop shadow — `ThemeShadow`/elevation (`Translation.Z`).

### IImage

- File: `.../Uno/Drawing/IImage.skia.cs`
- **Provides:** an opaque decoded-bitmap handle; lifetime owned by whatever produced it (not itself disposable).
- **Used by (public API):** `ImageBrush`/`Image`/`BitmapIcon` and the nine-grid brush, via `CompositionSurfaceBrush`.

```csharp
int PixelWidth { get; }
int PixelHeight { get; }
```

### IEffectFilter

- File: `.../Uno/Drawing/IEffectFilter.skia.cs` (marker interface)
- **Provides:** an opaque realized effect — either an `IGraphicsEffect` graph translated by the backend (mirroring the public effect-brush graph, *not* SkSL) or a drop shadow. Applied via `IDrawingSession.SaveLayer(IEffectFilter)` / `DrawEffectBackdrop`.
- **Used by (public API):** `CompositionEffectBrush` (`Compositor.CreateEffectFactory`/`CreateEffectBrush`, e.g. `AcrylicBrush`) and the non-analytic drop-shadow fallback.

### IRenderData

- File: `.../Uno/Drawing/IRenderData.skia.cs`
- **Provides:** opaque, backend-defined retained state produced by a recording and replayed via `IRetainedRenderingSession.Replay`. **Not** necessarily a display list — Skia stores an `SKPicture`; another backend may store a texture, a command buffer, or any per-content metadata. `IDisposable` gives deterministic release.
- **Used by (public API):** per-visual/subtree caching (`Visual`) and the `CompositionTarget` frame — behind every on-screen element, when the backend advertises `IRetainedRenderingSession`.

---

## Inline value types

### PaintParams

- File: `.../Uno/Drawing/PaintParams.skia.cs`
- Declaration: `internal readonly struct PaintParams`
- Transient paint passed by value on the draw verbs. Only simple, universally-supported properties; expensive resources are the optional `IShader`/`IColorFilter`/`IMaskFilter` handles.

```csharp
public PaintParams(Color color);           // sets Color; Opacity defaults to 1
public Color Color { get; init; }
public float Opacity { get; init; }
public PaintStyle Style { get; init; }
public float StrokeWidth { get; init; }
public StrokeCap StrokeCap { get; init; }
public StrokeJoin StrokeJoin { get; init; }
public float StrokeMiter { get; init; }
public bool IsAntialias { get; init; }
public BlendMode BlendMode { get; init; }
public IShader? Shader { get; init; }
public IColorFilter? ColorFilter { get; init; }
public IMaskFilter? MaskFilter { get; init; }
```

### StrokeStyle

- File: `.../Uno/Drawing/StrokeStyle.skia.cs`
- Declaration: `internal readonly struct StrokeStyle`
- How a geometry is stroked, passed to `IGeometry.GetStrokeFillGeometry`. Uses WinUI composition stroke enums (framework types, not backend types) so the contract is "give me the WinUI-correct stroke fill region."

```csharp
public float Thickness { get; init; }
public CompositionStrokeCap StartCap { get; init; }
public CompositionStrokeCap EndCap { get; init; }
public CompositionStrokeCap DashCap { get; init; }
public CompositionStrokeLineJoin LineJoin { get; init; }
public float MiterLimit { get; init; }
public float[]? DashArray { get; init; }   // in multiples of Thickness, or null for a solid stroke
public float DashOffset { get; init; }
public float TrimStart { get; init; }
public float TrimEnd { get; init; }
```

### RoundRectangle

- File: `.../Uno/Drawing/RoundRectangle.skia.cs`
- Declaration: `internal readonly struct RoundRectangle`
- A rectangle with per-corner radii (x, y pairs), in Skia's order: top-left, top-right, bottom-right, bottom-left.

```csharp
public Rect Rect { get; init; }
public Vector2 TopLeft { get; init; }
public Vector2 TopRight { get; init; }
public Vector2 BottomRight { get; init; }
public Vector2 BottomLeft { get; init; }
```

---

## Enums

- File: `.../Uno/Drawing/DrawingEnums.skia.cs` (and `GeometryCombineMode.skia.cs`)

```csharp
enum PaintStyle { Fill, Stroke }
enum StrokeCap { Butt, Round, Square }
enum StrokeJoin { Miter, Round, Bevel }
enum BlendMode { SrcOver, Src, Plus, Modulate, Multiply, DstIn, DstOut, SrcIn }   // SrcOver default
enum ImageSampling { NearestNeighbor, Linear }
enum ClipOperation { Intersect, Difference }
enum GradientTileMode { Clamp, Repeat, Mirror }
enum GeometryCombineMode { Union, Intersect, Difference, Xor }
```

> Note the two stroke-cap/join vocabularies: the `PaintStyle`/`StrokeCap`/`StrokeJoin` enums above are for
> *direct stroked draws* on the session; rich shape stroking (`StrokeStyle`) uses the WinUI
> `CompositionStrokeCap`/`CompositionStrokeLineJoin` enums (which include `Triangle`/`MiterOrBevel`).

---

## Default backend — SkiaSharp implementation

All under `.../Uno/Drawing/*.skia.cs` unless noted. Each wraps the corresponding SkiaSharp type; the raw
`UnoSkiaApi` P/Invoke fast paths (`sk_canvas_draw_picture`, `sk_picture_recorder_end_recording`,
`sk_canvas_set_matrix`, `sk_refcnt_safe_unref`) stay internal to these classes.

| Abstraction | Skia implementation | Wraps / notes |
|-------------|--------------------|---------------|
| `IDrawingBackend` | `SkiaDrawingBackend` | gradients → `SKShader.Create*Gradient`; opacity filter → cached `Modulate` `SKColorFilter`; blend/color-matrix filters → `SKColorFilter`; blur → `SKMaskFilter`; effects → `SkiaEffectFactory` |
| `IPathBuilder` | `SkiaPathBuilder` | `SKPathBuilder`; `Build()` → `SkiaGeometrySource2D(builder.Detach())` |
| `IGeometry` | `SkiaGeometrySource2D` (`.../Composition/SkiaGeometrySource2D.skia.cs` + `.Stroke.skia.cs`) | `SKPath`; explicit `IGeometry` impl on the existing type; stroke parity synthesis (caps/miter/dash) lives in the `.Stroke` partial |
| `IShader` | `SkiaShader` | `SKShader` |
| `IColorFilter` | `SkiaColorFilter` | `SKColorFilter` |
| `IMaskFilter` | `SkiaMaskFilter` | `SKMaskFilter` (Gaussian blur) |
| `IImage` | `SkiaImage` | `SKImage` |
| `IEffectFilter` | `SkiaEffectFilter` | `SKImageFilter`; the `IGraphicsEffect`-graph → `SKImageFilter` translation (incl. any SkSL `SKRuntimeEffect`) lives in `SkiaEffectFactory` |
| `IRenderData` | `SkiaRenderData` | `SKPicture` handle; `Dispose` → `sk_refcnt_safe_unref` |
| `IDrawingSession` (+ `IRetainedRenderingSession`) | `SkiaDrawingSession` | `SKCanvas`; `PaintParams` → thread-pooled `SKPaint`; pooled `SKPictureRecorder`s; `SetMatrix` preserves the full 4×4 via `SKMatrix44`; advertises retained recording |
| `IRecordingSession` | `SkiaRecordingSession` | derives `SkiaDrawingSession` over an `SKPictureRecorder` canvas; `EndRecording` → `SkiaRenderData` |
| `IRenderBackend` | `SkiaRenderBackend` (`src/Uno.UI/Helpers/SkiaRenderBackend.skia.cs`) | the established `SkiaRenderHelper` `SKPicture` two-phase |

---

## How composition consumes it (integration points)

- **`Visual.PaintingSession`** (`.../Composition/Uno/Visual.PaintingSession.skia.cs`) — the per-visual draw context; carries only `IDrawingSession Session` (the raw-canvas accessor has been removed).
- **`Visual.RenderRootVisual(IDrawingSession)`** — the agnostic entry to the unified walk. Per-visual retained state is `IRenderData? _content` / `_childrenContent`; `PaintStep`/`RenderChildrenStep` feature-detect `IRetainedRenderingSession` (via `AsRetained`) and, when present, record via `CreateRecording`/`EndRecording` and replay via `Replay` — otherwise they render immediately (uncached). Caching + picture-collapsing are unchanged on the retained path.
- **`CompositionBrush.TryPaint(IDrawingSession, float opacity, Rect bounds)`** — the **sole** brush paint entry point; `Paint(SKCanvas, …)` has been removed. Implemented by every brush: color, gradients, surface/image, mask, nine-grid, brush-wrapper, effect, and the legacy acrylic. (Nine-grid and the legacy `SkiaAcrylicBrush` still reach the Skia canvas through a contained `((SkiaDrawingSession)session).Canvas` downcast — nine-patch and the legacy acrylic filter have no neutral verb yet.)
- **`CompositionClip.ApplyClip(Visual, IDrawingSession)`** — dispatches neutral `Rect` / `RoundRectangle` / `IGeometry` clips to the session; used by `UIElement.Clip` and `BorderVisual`.
- **`CompositionSpriteShape`** — produces its fill/stroke outline via `IGeometry.GetFilledGeometry`/`GetStrokeFillGeometry`, clips and fills through the session (no direct `SKPaint`).
- **`CompositionSurfaceBrush.TryPaint`** — draws the image branch via `session.DrawImage` + `IImage`/`IColorFilter`; the recursive visual-surface branch (`ISkiaSurface`) renders through the session too.
- **`CompositionEffectBrush`** — realizes its `IGraphicsEffect` graph through `IDrawingBackend.CreateEffectFilter` and applies it via `session.DrawEffectBackdrop`; the drop shadow uses `session.SaveLayer(IEffectFilter)`.
- **`CompositionTarget.RenderBackend`** (`src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs`) — the `IRenderBackend` the frame loop delegates record/present to; frames are stored as `IRenderData`.
- **Text** (`TextBlock`/`UnicodeText`/`ParsedText`) — selection/caret/highlight fills paint via `IDrawingSession`; glyph rasterization (`SKTextBlob`) reaches the canvas through a contained `SkiaDrawingSession` downcast, as does the public `SKCanvasVisual` escape hatch.

---

## Status — what a backend implements today, and what's still Skia

**Required to provide a backend today:** `IDrawingBackend` (+ `IPathBuilder`), `IGeometry`
(incl. `GetStrokeFillGeometry` with WinUI stroke parity), `IShader`, `IColorFilter`, `IMaskFilter`,
`IImage`, `IEffectFilter`, `IDrawingSession`, and `IRenderBackend`; then register via
`DrawingBackend.Register` / `CompositionTarget.RenderBackend`.

**Optional** (for cached rendering): `IRetainedRenderingSession` (+ `IRecordingSession` / `IRenderData`).
Skip it and the composition walk re-draws every frame — correct, just without per-visual/subtree
picture caching.

**What's done since the early drafts:** clips, geometry math and hit-test bookkeeping, the analytic
**and** non-analytic shadows, **effects** (`CompositionEffectBrush` realized through
`IDrawingBackend.CreateEffectFilter`, mirroring the public `IGraphicsEffect` graph — SkSL stays a
Skia-backend detail), **all brushes** (color, both gradients, surface/image, mask, nine-grid, effect,
acrylic), the **image handle** (`IFrameProvider`/`SkiaCompositionSurface.Image` cross as `IImage`, so
`CompositionSurfaceBrush` is Skia-free), and retained recording (now the optional capability above) are
all off the core interfaces. `PaintingSession.Canvas` and `CompositionBrush.Paint(SKCanvas)` have been
removed.

**Intentionally Skia, out of scope (not a leak):**
- **Image decoding** — `FrameProviderFactory` (`SKCodec`, EXIF orientation, resize, animation) and the
  frame providers / `SkiaCompositionSurface` pixel-and-stream handling. Decoding is an image-source
  concern, not a rendering one; it stays Skia internally and only its output crosses the rendering
  boundary as `IImage`. `SkiaImage`/`SkiaCompositionSurface` are the backend's concrete image/surface.

**Still Skia-coupled / not yet abstracted:**
- **Geometry construction**: the public `CompositionPath`/`IGeometrySource2D`/`Geometry` types and `BorderVisual`'s border/background outlines still *build* `SKPath`/`SKRoundRect` (`SkiaGeometrySource2D` is the concrete `IGeometry`); `CompositionSpriteShape`'s geometry-transform is an `SKMatrix`.
- **Contained backend downcasts**: nine-grid (`DrawImageNinePatch`), the legacy `SkiaAcrylicBrush` filter chain (the last direct `SKImageFilter`), and **text** glyph rasterization (`SKTextBlob`) reach the Skia canvas via an explicit `((SkiaDrawingSession)…).Canvas` cast; the public `SKCanvasVisual` is an intentional raw-canvas escape hatch.
- **Present target**: `IRenderBackend.Present` still takes an `SKCanvas` (host swapchain not neutralized), and the final native-element clip path is unwrapped to `SKPath` in `SkiaRenderHelper` (the clip *math* is now `IGeometry`).
- **The public flip** (internal → public) and, ultimately, **extracting the Skia backend into its own assembly** so `Uno.UI.Composition` core carries no `SkiaSharp` reference — the structural finish line.
