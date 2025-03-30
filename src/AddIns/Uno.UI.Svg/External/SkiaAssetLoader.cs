// See THIRD-PARTY-NOTICES.txt for licensing information
// Based on https://github.com/wieslawsoltes/Svg.Skia/commit/7e2c8dc047f93b79db48929f92c2aa129ca331f7
// SkiaSharp 3 support Imported https://github.com/follesoe/Svg.Skia/commit/3146adcec14a1c91d2a346403823c24fecbb0298

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable IDE0055 // Fix formatting

using System.Collections.Generic;
using Model = global::Svg.Model;

namespace Uno.Svg.Skia;

internal class SkiaAssetLoader : Model.IAssetLoader
{
    private readonly SkiaModel _skiaModel;

    public SkiaAssetLoader(SkiaModel skiaModel)
    {
        _skiaModel = skiaModel;
    }

    public ShimSkiaSharp.SKImage LoadImage(System.IO.Stream stream)
    {
        var data = ShimSkiaSharp.SKImage.FromStream(stream);
        using var image = SkiaSharp.SKImage.FromEncodedData(data);
        return new ShimSkiaSharp.SKImage {Data = data, Width = image.Width, Height = image.Height};
    }

    public List<Model.TypefaceSpan> FindTypefaces(string? text, ShimSkiaSharp.SKPaint paintPreferredTypeface)
    {
        var ret = new List<Model.TypefaceSpan>();

        if (text is null || string.IsNullOrEmpty(text))
        {
            return ret;
        }

        System.Func<int, SkiaSharp.SKTypeface?> matchCharacter;

        if (paintPreferredTypeface.Typeface is { } preferredTypeface)
        {
            var weight = _skiaModel.ToSKFontStyleWeight(preferredTypeface.FontWeight);
            var width = _skiaModel.ToSKFontStyleWidth(preferredTypeface.FontWidth);
            var slant = _skiaModel.ToSKFontStyleSlant(preferredTypeface.FontSlant);

            matchCharacter = codepoint => SkiaSharp.SKFontManager.Default.MatchCharacter(
                preferredTypeface.FamilyName,
                weight,
                width,
                slant,
                null,
                codepoint);
        }
        else
        {
            matchCharacter = codepoint => SkiaSharp.SKFontManager.Default.MatchCharacter(codepoint);
        }

        using var runningPaint = _skiaModel.ToSKPaint(paintPreferredTypeface);
        if (runningPaint is null)
        {
            return ret;
        }

        var currentTypefaceStartIndex = 0;
        var i = 0;

        void YieldCurrentTypefaceText()
        {
            var currentTypefaceText = text.Substring(currentTypefaceStartIndex, i - currentTypefaceStartIndex);

			ret.Add(new(currentTypefaceText, runningPaint.MeasureText(currentTypefaceText),
                runningPaint.Typeface is null
                    ? null
                    : ShimSkiaSharp.SKTypeface.FromFamilyName(
                        runningPaint.Typeface.FamilyName,
                        // SkiaSharp provides int properties here. Let's just assume our
                        // ShimSkiaSharp defines the same values as SkiaSharp and convert directly
                        (ShimSkiaSharp.SKFontStyleWeight)runningPaint.Typeface.FontWeight,
                        (ShimSkiaSharp.SKFontStyleWidth)runningPaint.Typeface.FontWidth,
                        (ShimSkiaSharp.SKFontStyleSlant)runningPaint.Typeface.FontSlant)
            ));
		}

        for (; i < text.Length; i++)
        {
            var typeface = matchCharacter(char.ConvertToUtf32(text, i));

            if (i == 0)
            {
                runningPaint.Typeface = typeface;
            }
            else if (runningPaint.Typeface is null
                     && typeface is { } || runningPaint.Typeface is { }
                     && typeface is null || runningPaint.Typeface is { } l
                     && typeface is { } r
                     && (l.FamilyName, l.FontWeight, l.FontWidth, l.FontSlant) != (r.FamilyName, r.FontWeight, r.FontWidth, r.FontSlant))
            {
                YieldCurrentTypefaceText();

                currentTypefaceStartIndex = i;
                runningPaint.Typeface = typeface;
            }

            if (char.IsHighSurrogate(text[i]))
            {
                i++;
            }
        }

        YieldCurrentTypefaceText();

        return ret;
    }
}
