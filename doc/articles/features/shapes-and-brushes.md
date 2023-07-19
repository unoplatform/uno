---
uid: Uno.Features.ShapesAndBrushes
---

# Shapes & Brushes

## Implemented Shapes

| Shape       | Android | iOS     | macOS   | Wasm (1) |                                                              |
| ----------- | ------- | ------- | ------- | -------- | ------------------------------------------------------------ |
| `Ellipse`   | Yes (2) | Yes (2) | Yes (2) | Yes (2)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.ellipse) |
| `Line`      | Yes     | Yes     | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.line) |
| `Path`      | Yes     | Yes     | Yes     | Yes (3)  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.path) |
| `Polygon`   | Yes     | Yes     | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.polygon) |
| `Polyline`  | Yes     | Yes     | Yes     | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.polyline) |
| `Rectangle` | Yes (4) | Yes (4) | Yes (4) | Yes      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.rectangle) |

Notes:

1. On _Wasm_, the stroke thickness is applied before the stretching
   scaling. It means the stroke thickness will follow the scaling.
   On all other platforms (and on UWP), the stroke thickness is applied
   after the stretching.

   To get a fully reliable shape on all platforms, you should only
   use the _fill_ part of the shape.

   All shapes on _Wasm_ platform (including `Rectangle`) are rendered as `SVG` elements.

2. Stretching modes `UniformToFill` and `Uniform` will behave like
   `Fill` for ellipses.

3. On _Wasm_, only the `Data=` text format is implemented.
   On other platforms, you can use either the _data_ or the geometries.

4. Rectangles on _Android_ and _iOS_ are rendered as a simple border.

## Implemented Shapes properties

| Shape                | Android | iOS  | macOS | Wasm    |                                                              |
| -------------------- | ------- | ---- | ----- | ------- | ------------------------------------------------------------ |
| `Fill` (1)           | Yes     | Yes  |       | Yes     | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.fill) |
| `GeometryTransform`  | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.geometrytransform) |
| `Stretch` (2)        | Yes     | Yes  |       | Yes     | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.stretch) |
| `Stroke` (1)         | Yes     | Yes  |       | Yes     | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.stroke) |
| `StrokeDashArray`    | No      | Yes  |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedasharray) |
| `StrokeDashCap`      | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedashcap) |
| `StrokeDashOffset`   | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokedashoffset) |
| `StrokeEndLineCap`   | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokeendlinecap) |
| `StrokeLineJoin`     | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokelinejoin) |
| `StrokeMiterLimit`   | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokemiterlimit) |
| `StrokeStartLineCap` | No      | No   |       | No      | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokestartlinecap) |
| `StrokeThickness`    | Yes     | Yes  |       | Yes (3) | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.shapes.shape.strokethickness) |

Notes:

1. See next section for _fill_ & _stroke_ brushes limitations.
2. Not working for `Ellipse`, always behaving like `Stretch.Fill`.
3. `StrokeThickness` on _Wasm_ will scale with _Stretch_.

## Implemented Brushes / Properties

| Brush                              | Android | iOS     | macOS | Wasm |                                                              |
| ---------------------------------- | ------- | ------- | ---- | ------------------------------------------------------------ | ---------------------------------- |
| `AcrylicBrush`                     | Yes (3) | Yes (3) | Yes (3) | Yes   | [Documentation](https://docs.microsoft.com/fr-ca/uwp/api/windows.ui.xaml.media.acrylicbrush) |
| `ImageBrush`                       | Yes (1) | Yes (1) |  | No   | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.ImageBrush) |
| `LinearGradientBrush` | Yes (2) | Yes (2) | Yes  | Yes | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.LinearGradientBrush) |
| `RadialGradientBrush` (WinUI 2.4+) | Yes | Yes  | Yes  | Yes |                                                              |
| `RevealBrush` (WinUI 2.0+) | No     | No     | No   | No  |                                                              |
| `SolidColorBrush`                  | Yes     | Yes     | Yes  | Yes  | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Media.SolidColorBrush) |
| `WebViewBrush`                     | No      | No      | No    | No   | [Documentation](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Controls.WebViewBrush) |
| `XamlCompositionBrushBase`         | No      | No      | No    | No   | [Documentation](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.xamlcompositionbrushbase) |

Notes:

1. `ImageBrush` on Android & iOS can only be used as a `Fill` / `Background` brush; it is not supported for `Stroke`  / `BorderBrush` properties and **the image needs to be a local asset**. They are not supported as text's `Froreground`.
2. On Android & iOS, gradient brushes (`LinearGradientBrush` & `RadialGradientBrush`) are only used as a `Fill` / `Background` brush.
3. On Android, iOS, and macOS `AcrylicBrush` has some limitations. Please see the following section for details.

## AcrylicBrush

Uno Platform supports the `Backdrop` version of `AcrylicBrush` (blurring in-app content behind element) on all targets except GTK and WPF. In addition, on macOS we support the `HostBackdrop` acrylic (blurring content behind the app window).

The brush currently has an important limitation on Android, iOS, and macOS: it can be used **only on elements which have no children**. Eg., if you wanted to have an acrylic effect in the background of a `Grid` with child content, then you would add a `Border` with no inner child behind the other content in the `Grid` and set the acrylic background on the `Border`, rather than set it directly on the `Grid`:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition />
    </Grid.RowDefinitions>
    
    <!-- This border represents the background, 
        it covers the entire parent area -->
    <Border Grid.RowSpan="2">
        <Border.Background>
            <AcrylicBrush 
                AlwaysUseFallback="False" 
                TintColor="Red" 
                TintOpacity="0.8" />
        </Border.Background>
    </Border>
    
    <TextBox Text="Some input" />
    <Button Grid.Row="1">My content</Button>
</Grid>

```

Because many WinUI styles use `AcrylicBrush` on elements which violate this condition, we made the brush use solid fallback color by default on targets other than WASM. To enable the brush, you need to explicitly set the `AlwaysUseFallback` property to `false`:

```xml
<AcrylicBrush x:Key="MyAcrylicBrush" AlwaysUseFallback="False" ... />
```

## Brushes Usages

Where you can use which brushes

| Usage                                        | SolidColorBrush | ImageBrush           | GradientBrush   |
| -------------------------------------------- | --------------- | -------------------- | --------------- |
| `Background` property (many controls/panels) | Yes             | Yes (except on Wasm) | Yes             |
| `BorderBrush` (`Border`, `Panel`)            | Yes             | No                   | Yes (Wasm only) |
| `Foreground` (`TextBlock`)                   | Yes             | No                   | Yes (Wasm only) |
| `Fill` (Shapes)                              | Yes             | Yes (except on Wasm) | Yes             |
| `Stroke` (Shapes)                            | Yes             | No                   | Yes (Wasm only) |
