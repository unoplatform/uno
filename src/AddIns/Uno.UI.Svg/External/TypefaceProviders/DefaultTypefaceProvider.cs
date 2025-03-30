// See THIRD-PARTY-NOTICES.txt for licensing information
// Based on https://github.com/wieslawsoltes/Svg.Skia/commit/7e2c8dc047f93b79db48929f92c2aa129ca331f7
// SkiaSharp 3 support Imported https://github.com/follesoe/Svg.Skia/commit/3146adcec14a1c91d2a346403823c24fecbb0298

#pragma warning disable IDE0055 // Fix formatting

using System;
using System.Linq;

namespace Svg.Skia.TypefaceProviders;

internal sealed class DefaultTypefaceProvider : ITypefaceProvider
{
    public static readonly char[] s_fontFamilyTrim = { '\'' };

    public SkiaSharp.SKTypeface? FromFamilyName(string fontFamily, SkiaSharp.SKFontStyleWeight fontWeight, SkiaSharp.SKFontStyleWidth fontWidth, SkiaSharp.SKFontStyleSlant fontStyle)
    {
        var skTypeface = default(SkiaSharp.SKTypeface);
        var fontFamilyNames = fontFamily?.Split(',')?.Select(x => x.Trim().Trim(s_fontFamilyTrim))?.ToArray();
        if (fontFamilyNames is { } && fontFamilyNames.Length > 0)
        {
            var defaultName = SkiaSharp.SKTypeface.Default.FamilyName;

            foreach (var fontFamilyName in fontFamilyNames)
            {
                skTypeface = SkiaSharp.SKTypeface.FromFamilyName(fontFamilyName, fontWeight, fontWidth, fontStyle);
                if (skTypeface is { })
                {
                    if (!skTypeface.FamilyName.Equals(fontFamilyName, StringComparison.Ordinal)
                        && defaultName.Equals(skTypeface.FamilyName, StringComparison.Ordinal))
                    {
                        skTypeface.Dispose();
                        continue;
                    }
                    break;
                }
            }
        }
        return skTypeface;
    }
}
