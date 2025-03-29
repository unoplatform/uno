// See THIRD-PARTY-NOTICES.txt for licensing information
// Based on https://github.com/wieslawsoltes/Svg.Skia/commit/7e2c8dc047f93b79db48929f92c2aa129ca331f7
// SkiaSharp 3 support Imported https://github.com/follesoe/Svg.Skia/commit/3146adcec14a1c91d2a346403823c24fecbb0298

#pragma warning disable IDE0055 // Fix formatting

using System;
using System.IO;
using System.Linq;

namespace Svg.Skia.TypefaceProviders;

internal sealed class FontManagerTypefaceProvider : ITypefaceProvider
{
    public static readonly char[] s_fontFamilyTrim = { '\'' };

    public SkiaSharp.SKFontManager FontManager { get; set; }

    public FontManagerTypefaceProvider()
    {
        FontManager = SkiaSharp.SKFontManager.Default;
    }

    public SkiaSharp.SKTypeface CreateTypeface(Stream stream, int index = 0)
    {
        return FontManager.CreateTypeface(stream, index);
    }

    public SkiaSharp.SKTypeface CreateTypeface(SkiaSharp.SKStreamAsset stream, int index = 0)
    {
        return FontManager.CreateTypeface(stream, index);
    }

    public SkiaSharp.SKTypeface CreateTypeface(string path, int index = 0)
    {
        return FontManager.CreateTypeface(path, index);
    }

    public SkiaSharp.SKTypeface CreateTypeface(SkiaSharp.SKData data, int index = 0)
    {
        return FontManager.CreateTypeface(data, index);
    }

    public SkiaSharp.SKTypeface? FromFamilyName(string fontFamily, SkiaSharp.SKFontStyleWeight fontWeight, SkiaSharp.SKFontStyleWidth fontWidth, SkiaSharp.SKFontStyleSlant fontStyle)
    {
        var skTypeface = default(SkiaSharp.SKTypeface);
        var fontFamilyNames = fontFamily?.Split(',')?.Select(x => x.Trim().Trim(s_fontFamilyTrim))?.ToArray();
        if (fontFamilyNames is { } && fontFamilyNames.Length > 0)
        {
            var defaultName = SkiaSharp.SKTypeface.Default.FamilyName;
            var skFontManager = FontManager;
            var skFontStyle = new SkiaSharp.SKFontStyle(fontWeight, fontWidth, fontStyle);

            foreach (var fontFamilyName in fontFamilyNames)
            {
                var skFontStyleSet = skFontManager.GetFontStyles(fontFamilyName);
                if (skFontStyleSet.Count > 0)
                {
                    skTypeface = skFontManager.MatchFamily(fontFamilyName, skFontStyle);
                    if (skTypeface is { })
                    {
                        if (!defaultName.Equals(fontFamilyName, StringComparison.Ordinal)
                            && defaultName.Equals(skTypeface.FamilyName, StringComparison.Ordinal))
                        {
                            skTypeface.Dispose();
                            skTypeface = null;
                            continue;
                        }
                        break;
                    }
                }
            }
        }
        return skTypeface;
    }
}
