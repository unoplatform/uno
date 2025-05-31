---
uid: Uno.Features.SVG
---

# Using SVG images

Uno Platform supports using vector SVG graphics inside of your cross-platform applications, using the `SvgImageSource` class. SVG Images can also be used through [`UnoImage` with Resizetizer](xref:Uno.Resizetizer.GettingStarted).

![Uno SVG sample](../Assets/features/svg/heliocentric.png)

## How to use SVG

## [**Single Project**](#tab/singleproject)

To use an SVG, for iOS and Android, you'll need to add the `Svg` [Uno Feature](xref:Uno.Features.Uno.Sdk#uno-platform-features) as follows:

```xml
<UnoFeatures>
    ...
    Svg;
    ...
</UnoFeatures>
```

On Desktop Skia and WebAssembly, the feature is enabled by default.

To include an SVG file, you will need to place it in a folder named `Svg` (e.g. `Assets\Svg\MyFile.svg`). This is required in order to avoid [Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted) transform the file into a set of scaled PNGs files.

Now, you can display the SVG image in an `Image` by referencing it from the `Source` property. For example:

```xml
<Image Source="ms-appx:///Assets/Svg/test.svg" Stretch="UniformToFill" Width="100" Height="100" />
```

## [**Legacy Project**](#tab/legacyproject)

To use SVG, install the following NuGet packages into the iOS, macOS, Android, and Skia projects:

* `Uno.WinUI.Svg`
* `SkiaSharp.Views.Uno.WinUI`
* `Svg.Skia` (required only for the Skia project)

> [!NOTE]
> If the `Uno.[UI|WinUI].Svg` package is not installed, you will get a warning when an `.svg` image is loaded.
> If the `Svg.Skia` package is not installed, you will get an error when an `.svg` image is loaded for the Skia project.
> [!IMPORTANT]
> The `Uno.[UI|WinUI].Svg` package is not needed for WebAssembly, and must only be installed on the Mobile and Skia heads. It must not be in any other class libraries of your solution.

Add the SVG Image to the app project and make sure that the build action is set to Content.

Now, you can display the SVG image in an `Image` by referencing it from the `Source` property. For example:

```xml
<Image Source="ms-appx:///Assets/test.svg" Stretch="UniformToFill" Width="100" Height="100" />
```

---

You can also explicitly use `SvgImageSource`:

```xml
<Image>
  <Image.Source>
    <SvgImageSource UriSource="https://example.com/test.svg" />
  </Image.Source>
</Image>
```

## Supported features by platform

SVG is supported on all Uno Platform targets.

* On Android, iOS, macOS, and Skia, Uno Platform uses SkiaSharp to render the SVG graphics.
* On WebAssembly, the browser renders the SVG images directly.
* On Windows, the OS is responsible for SVG rendering (and complex SVG files may not render properly).

## When to use SVG

Because SVG requires to be parsed initially before rendering and its vector-based form needs to re-render each time the size of the image changes, it may not be suitable for all scenarios. For ideal performance, we recommend using SVG for in-app vector graphics and icons but prefer bitmap image formats in other cases. In case you run into performance issues, test switching from SVG to a bitmap image format to see if it alleviates the problem. You may also consider using SVG rasterization (see below).

If you need to keep your Android or iOS app package size as small as possible, it is also preferable to avoid using SVG, as the package depends on SkiaSharp.

## SVG rasterization support

For improved performance, you can use the `RasterizePixelHeight` and `RasterizePixelWidth` properties of `SvgImageSource` to rasterize the SVG image when first loaded. When rasterized, the image will always scale this rasterized version of the SVG image instead of rendering the vector-based graphics.

For example:

```xml
<Image Stretch="UniformToFill" Width="100" Height="100">
  <Image.Source>
    <SvgImageSource 
      UriSource="ms-appx:///Assets/couch.svg" 
      RasterizePixelHeight="10" 
      RasterizePixelWidth="10" />
  </Image.Source>
<Image>
```

Will render as:

![Scaled up](../Assets/features/svg/rasterized.png)

> [!NOTE]
> This feature is not available on WebAssembly where the rendering is handled directly by the browser.
