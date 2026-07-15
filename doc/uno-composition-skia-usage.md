# SkiaSharp usage in the Uno Composition area

> Inventory of the SkiaSharp API surface consumed under `src/Uno.UI.Composition/Composition/**`
> (the composition layer). Extracted by static scan of the source on branch
> `feature/drawing-backend-abstraction`; counts are occurrence counts and are approximate. Companion to
> `doc/uno-drawing-backend-abstraction.md` — this is the *raw coupling* the abstraction is factoring out.
>
> Out of scope (composition-adjacent, separate files in `Uno.UI`): `SkiaRenderHelper`, `CompositionTarget`,
> `RenderTargetBitmap`, and text rasterization under `UI/Xaml/Documents` (`SKFont`/`SKTypeface`/`SKTextBlob`).

## Types referenced (by frequency)

| Type | # | Type | # | Type | # |
|------|---|------|---|------|---|
| SKImageFilter | 133 | SKColorFilter | 30 | SKPictureRecorder | 10 |
| SKPoint | 120 | SKPathBuilder | 29 | SKPathAddMode | 10 |
| SKRect | 92 | SKPathOp | 28 | SKPoint3 | 8 |
| SKPath | 83 | SKImage | 28 | SKPathVerb | 8 |
| SKBlendMode | 65 | SKShaderTileMode | 22 | SKImageInfo | 8 |
| SKMatrix | 44 | SKEncodedOrigin | 18 | SKSamplingOptions | 7 |
| SKCanvas | 40 | SKRoundRect | 16 | SKMatrix44 | 7 |
| SKPaint | 32 | SKShader | 13 | SKPicture | 6 |
| SKColor | 32 | SKBitmap | 13 | SKClipOperation | 6 |
| SKRuntimeEffect | 12 | SKStrokeJoin / SKStrokeCap | 10 / 10 | SKMaskFilter | 3 |

Also present (single-digit): `SKRuntimeEffectUniforms`, `SKRuntimeEffectChildren`, `SKAlphaType`, `SKSizeI`, `SKPathEffect`, `SKFilterMode`, `SKColorType`, `SKCodecResult`, `SKCanvasSaveLayerRec`, `SKSize`, `SKPathMeasure`, `SKPathFillType`, `SKManagedStream`, `SKPaintStyle`, `SKCubicResampler`, `SKCodec`, `SKSurface`, `SKRectI`, `SKPointI`, `SKPathDirection`, `SKCodecOptions`, `SKBlurStyle`.

---

## Instance methods by type

### SKCanvas
State: `Save`, `SaveLayer`, `Restore`, `RestoreToCount`, `SaveCount`
Transform: `SetMatrix`, `Concat`, `Scale`, `Translate`, `TotalMatrix`
Clip: `ClipRect`, `ClipPath`, `ClipRoundRect`, `DeviceClipBounds`, `IsClipRect`
Draw: `DrawRect`, `DrawPath`, `DrawLine`, `DrawCircle`, `DrawImage`, `DrawBitmap`, `DrawImageNinePatch`, `DrawBitmapNinePatch`, `DrawColor`, `DrawPicture`
Misc: `Clear`, `Flush`, `Handle`, `Context`

### SKPaint
`Color`, `Reset`, `IsAntialias`, `IsDither`, `Style`, `IsStroke`, `StrokeWidth`, `StrokeCap`, `StrokeJoin`, `StrokeMiter`, `Shader`, `ColorFilter`, `PathEffect`, `BlendMode`, `GetFillPath`, `Handle`

### SKPath / SKPathBuilder
Build: `MoveTo`, `LineTo`, `RLineTo`, `CubicTo`, `QuadTo`, `ArcTo`, `AddRect`, `AddRoundRect`, `AddOval`, `AddArc`, `AddPoly`, `AddPath`, `Close`
Edit/query: `Reset`, `Rewind`, `Detach` (builder → path), `Op` (boolean), `GetFillPath` (stroke→fill), `Contains`, `Bounds`, `TightBounds`, `CreateIterator`
`SKPathAddMode.Append` is the add mode used with `AddPath`.

### SKPathMeasure
`GetPositionAndTangent`, `NextContour`, `Length`, `IsClosed` (used for stroke cap/dash synthesis)

### SKMatrix / SKMatrix44
`PostConcat`, `PreConcat`, `Concat`, `Invert`, `TryInvert`, `MapRect`, `MapPoint`; constructed via `CreateScale`/`CreateTranslation`/`CreateRotation`/`CreateIdentity`/`Identity`

### SKRoundRect
`SetRectRadii` (+ the raw `UnoSkiaApi.sk_rrect_set_rect_radii`)

### SKImage / SKBitmap / SKSurface
`SKImage.FromBitmap`, `SKImage.FromPixels`, `SKImage.FromPixelCopy`; `SKBitmap.FromImage`, `GetPixels`, `ReadPixels`; `SKSurface.Create`, `.Snapshot()`; `SKColor.WithAlpha`

---

## Static factories & constructors

### Geometry / paint effects
- `SKShader.CreateLinearGradient`, `CreateRadialGradient`, `CreateTwoPointConicalGradient`, `CreateColor`, `CreateCompose`
- `SKPathEffect.CreateTrim`, `CreateDash`, `CreateSum`
- `SKMaskFilter.CreateBlur`
- `SKColorFilter.CreateColorMatrix`, `CreateBlendMode`, `CreateLumaColor`

### Image-filter graph (the effect brush DAG)
`SKImageFilter.` — `CreateColorFilter` (×22), `CreateBlendMode`, `CreateOffset`, `CreateBlur`, `CreateMerge`, `CreateMatrix`, `CreateTile`, `CreateShader`, `CreatePicture`, `CreateArithmetic`, `CreateDropShadow`, `CreateMatrixConvolution`, `CreateCompose`, `Create{Spot,Point,Distant}Lit{Specular,Diffuse}`, plus `CropRect`.

### Runtime shaders (SkSL)
`SKRuntimeEffect.CreateShader` (×6) with `SKRuntimeEffectUniforms` / `SKRuntimeEffectChildren` — acrylic + effect brushes.

### Images / codecs / surfaces
`SKImage.From*`, `SKBitmap.FromImage`, `SKSurface.Create`, `SKCodec.Create`, `SKManagedStream`, `SKImageInfo.PlatformColorType`, `SKCubicResampler.CatmullRom`, `SKSamplingOptions.Default`.

### Retained mode
`SKPictureRecorder` (`.BeginRecording`) + raw P/Invoke (below); `SKPicture`.

---

## Enum values used

| Enum | Values seen |
|------|-------------|
| `SKBlendMode` | SrcOver, Src, SrcIn, SrcOut, SrcATop, DstIn, DstOut, DstOver, DstATop, Plus, Modulate, Multiply, Screen, Overlay, Darken, Lighten, ColorDodge, ColorBurn, HardLight, SoftLight, Difference, Exclusion, Hue, Saturation, Color, Luminosity, Xor |
| `SKPathOp` | Intersect, Difference, Union, Xor |
| `SKShaderTileMode` | Clamp, Repeat, Mirror |
| `SKClipOperation` | Intersect, Difference |
| `SKStrokeCap` | Butt, Round, Square |
| `SKStrokeJoin` | Miter, Round, Bevel |
| `SKPaintStyle` | Fill, Stroke |
| `SKPathFillType` | EvenOdd |
| `SKPathDirection` | Clockwise |
| `SKPathVerb` | Move, Line, Quad, Conic, Cubic, Close, Done |
| `SKPathAddMode` | Append |
| `SKColorType` | Bgra8888 (+ `PlatformColorType`) |
| `SKAlphaType` | Premul |
| `SKFilterMode` | Linear, Nearest |
| `SKBlurStyle` | Normal |
| `SKEncodedOrigin` | all 8 orientations |
| `SKCodecResult` | Success, IncompleteInput |
| `SKColors` | Transparent, White, Black (+ `SKColor.Empty`) |

---

## Raw P/Invoke (`UnoSkiaApi`, bypassing managed wrappers on hot paths)

| Function | Use |
|----------|-----|
| `sk_canvas_draw_picture` | Replay a recorded `SKPicture` |
| `sk_picture_recorder_end_recording` | Finish recording → picture handle |
| `sk_refcnt_safe_unref` | Release a picture/native refcounted object |
| `sk_canvas_set_matrix` | Set the canvas matrix from an `SKMatrix44*` (avoids a copy) |
| `sk_rrect_set_rect_radii` | Set per-corner radii on an `SKRoundRect` |
| `Initialize` | One-time P/Invoke setup |

---

## Where the coupling concentrates

- **Effects** — `SKImageFilter` (133 refs, ~20 distinct factory nodes) + `SKRuntimeEffect` (SkSL) in `CompositionEffectBrush`/`SkiaAcrylicBrush`. The Win2D/D2D effect graph mapped onto Skia. Hardest to make backend-neutral.
- **Geometry as computation** — `SKPath.Op` (×22 boolean), `SKPaint.GetFillPath` (stroke→fill), `SKPathMeasure`, `SKPath.CreateIterator`: used to build stroke fills and clips, not just to draw. (This is what `IGeometry.GetStrokeFillGeometry`/`Combine` now abstracts.)
- **Retained mode** — `SKPictureRecorder`/`SKPicture` + the raw P/Invoke, per-visual. (Now behind `IRenderData`.)
- **Gradients / shaders** — `SKShader.Create*` in the gradient brushes. (Linear now via `IDrawingBackend.CreateLinearGradientShader`; radial still direct.)
- **Images** — `SKImage`/`SKBitmap`/`SKSurface` in surface/nine-grid brushes and image sources.
