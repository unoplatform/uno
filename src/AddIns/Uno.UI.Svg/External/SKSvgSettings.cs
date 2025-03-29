// See THIRD-PARTY-NOTICES.txt for licensing information
// Based on https://github.com/wieslawsoltes/Svg.Skia/commit/7e2c8dc047f93b79db48929f92c2aa129ca331f7
// SkiaSharp 3 support Imported https://github.com/follesoe/Svg.Skia/commit/3146adcec14a1c91d2a346403823c24fecbb0298

#pragma warning disable IDE0055 // Fix formatting

using System.Collections.Generic;
using Svg.Skia.TypefaceProviders;

namespace Uno.Svg.Skia;

internal class SKSvgSettings
{
    public SkiaSharp.SKAlphaType AlphaType { get; set; }

    public SkiaSharp.SKColorType ColorType { get; set; }

    public SkiaSharp.SKColorSpace SrgbLinear { get; set; }

    public SkiaSharp.SKColorSpace Srgb { get; set; }

    public IList<ITypefaceProvider>? TypefaceProviders  { get; set; }

    public SKSvgSettings()
    {
        AlphaType = SkiaSharp.SKAlphaType.Unpremul;

        ColorType = SkiaSharp.SKImageInfo.PlatformColorType;

        SrgbLinear = SkiaSharp.SKColorSpace.CreateRgb(SkiaSharp.SKColorSpaceTransferFn.Linear, SkiaSharp.SKColorSpaceXyz.Srgb); // SkiaSharp.SKColorSpace.CreateSrgbLinear();

        Srgb = SkiaSharp.SKColorSpace.CreateRgb(SkiaSharp.SKColorSpaceTransferFn.Srgb, SkiaSharp.SKColorSpaceXyz.Srgb); // SkiaSharp.SKColorSpace.CreateSrgb();

        TypefaceProviders = new List<ITypefaceProvider>
        {
            new FontManagerTypefaceProvider(),
            new DefaultTypefaceProvider()
        };
    }
}
