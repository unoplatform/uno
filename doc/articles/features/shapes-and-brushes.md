# Shapes & Brushes

## Implemented Shapes

| Shape       | Android  | iOS     | Wasm (1) |    |
| ----------- | -------- | ------- | -------- | -- |
| `Ellipse`   | Yes (2)  | Yes (2) | Yes (2)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.ellipse) |
| `Line`      | Yes      | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.line) |
| `Path`      | Yes      | Yes     | Yes (3)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.path) |
| `Polygon`   | Yes      | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.polygon) |
| `Polyline`  | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.polyline) |
| `Rectangle` | Yes (4)  | Yes (4) | Yes (4)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.rectangle) |

Notes:

1. On _Wasm_, the stroke thickness is applied before the stretching
   scaling. It means the stroke thickness will follow the scaling.
   On all other platforms (and on UWP), the stroke thickness is applied
   after the stretching.

   To get a fully reliable shape on all platforms, you should only
   use the _fill_ part of the shape.
2. Stretching modes `UniformToFill` and `Uniform` will behave like
   `Fill` for ellipses.
3. On _Wasm_, only the `Data=` text format is implemented.
   On other platforms, you can use either the _data_ or the geometries.
4. Rectangles on _Android_ and _iOS_ are rendered as a simple border.
   On _Wasm_, it's a rectangle in a _SVG_ surface.

## Implemented Shapes properties

| Shape                | Android  | iOS     | Wasm     |    |
| -------------------- | -------- | ------- | -------- | -- |
| `Fill` (1)           | Yes      | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.fill) |
| `GeometryTransform`  | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.geometrytransform) |
| `Stretch` (2)        | Yes      | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.stretch) |
| `Stroke` (1)         | Yes      | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.stroke) |
| `StrokeDashArray`    | No       | Yes     | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedasharray) |
| `StrokeDashCap`      | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedashcap) |
| `StrokeDashOffset`   | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedashoffset) |
| `StrokeEndLineCap`   | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokeendlinecap) |
| `StrokeLineJoin`     | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokelinejoin) |
| `StrokeMiterLimit`   | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokemiterlimit) |
| `StrokeStartLineCap` | No       | No      | No       | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokestartlinecap) |
| `StrokeThickness`    | Yes      | Yes     | Yes (3)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokethickness) |

Notes:

1. See next section for _fill_ & _stroke_ brushes limitations.
2. Not working for `Ellipse`, always behaving like `Stretch.Fill`.
3. `StrokeThickness` on _Wasm_ will scale with _Stretch_.

## Implemented Brushes

| Brush                      | Android | iOS     | Wasm    |    |
| -------------------------- | ------- | ------- | ------- | -- |
| `AcrylicBrush`             | No      | No      | No      | [Documentation](https://docs.microsoft.com/fr-ca/uwp/api/windows.ui.xaml.media.acrylicbrush) |
| `ImageBrush`               | Yes     | Yes (1) | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.ImageBrush) |
| `LinearGradientBrush`      | Yes     | Yes (2) | Yes (2) | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.LinearGradientBrush) |
| `SolidColorBrush`          | Yes     | Yes     | Yes     | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.SolidColorBrush) |
| `WebViewBrush`             | No      | No      | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Controls.WebViewBrush) |
| `XamlCompositionBrushBase` | No      | No      | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.xamlcompositionbrushbase) |

Notes:

1. `ImageBrush` on iOS can only be used as a `Fill` brush; it is not supported for `Stroke` and the image needs to be a local asset.
2. `LinearGradientBrush` on _Wasm_ is not implemented on _shapes_, but it works on other _FrameworkElement_, like a `Border` or a `Panel`...
