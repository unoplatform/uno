# Geometry Visual Parity Notes

These showcase pages exist so a maintainer can spot-check Uno Skia's `Microsoft.UI.Xaml.Media.Geometry` rendering against native WinUI by opening the same XAML in both apps' SamplesApp and visually diffing.

## Pages

| Sample | What it covers |
|--------|----------------|
| `Geometry_AllSegments_Showcase` | Every `PathSegment` subtype (Line, Bezier, QuadraticBezier, Arc small/large CW/CCW, elliptical, rotated, PolyLine, PolyBezier, PolyQuadratic). |
| `Geometry_SimpleTypes_Showcase` | `RectangleGeometry`, `EllipseGeometry`, `LineGeometry`, `GeometryGroup` with EvenOdd/Nonzero, plus per-geometry `Transform` variants. |
| `Geometry_PathMarkup_Showcase` | SVG mini-language commands: `M`/`L`/`H`/`V`/`C`/`S`/`Q`/`T`/`A`/`Z` and `F0`/`F1` fill rule, plus implicit-repeat. |
| `Geometry_Stroke_Showcase` | Stroke variations: dash arrays, line caps (Flat/Round/Square), line joins (Miter/Bevel/Round), miter limit, dash offset. |
| `Geometry_Transforms_Showcase` | `Geometry.Transform` with TranslateTransform, ScaleTransform, RotateTransform, SkewTransform, CompositeTransform. |
| `Geometry_GeometryGroup_FillRule_Showcase` | Self-intersecting and nested paths under EvenOdd vs Nonzero fill rules side-by-side. |
| `Geometry_StreamGeometry_Showcase` | Same shape built via mini-language string vs explicit `PathGeometry`/segment hierarchy. |

## Verification protocol

1. Build and run `SamplesApp.Skia.Generic` (Skia Desktop on Windows).
2. Build and run the WinUI/WinAppSDK target of the same SamplesApp (or open the same XAML in a small reference WinUI app).
3. Navigate to each `Geometry_*_Showcase` sample in both apps.
4. Take screenshots of both at the same window size.
5. Overlay or eyeball-diff the screenshots.

## Acceptable divergences

These differences are expected from D2D vs Skia rasterizers and are NOT bugs:

- Sub-pixel anti-aliasing on diagonal stroke edges.
- Slightly different sub-pixel stroke positioning (half-pixel boundary handling).
- Minor color blending differences along glow/anti-aliased edges.

## Bug-level divergences

These are bugs that need investigation:

- Wrong shape (segment in wrong location, missing curve, missing arc).
- Wrong fill (solid where there should be hole, or vice-versa under EvenOdd/Nonzero).
- Wrong stroke topology (incorrect joint shape, wrong cap, missing dash).
- Missing or extra figures.
- Crashes / exceptions on load.

When a divergence is found, capture screenshots of both renders, file an issue with the showcase sample name, the failing cell, and the expected-vs-actual screenshots.
