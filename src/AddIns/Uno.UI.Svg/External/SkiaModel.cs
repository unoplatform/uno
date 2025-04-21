// See THIRD-PARTY-NOTICES.txt for licensing information
// Based on https://github.com/wieslawsoltes/Svg.Skia/commit/7e2c8dc047f93b79db48929f92c2aa129ca331f7
// SkiaSharp 3 support Imported https://github.com/follesoe/Svg.Skia/commit/3146adcec14a1c91d2a346403823c24fecbb0298

#pragma warning disable IDE0055 // Fix formatting

using System;
using System.Collections.Generic;
using ShimSkiaSharp;

namespace Uno.Svg.Skia;

internal class SkiaModel
{
    public SKSvgSettings Settings { get; }

    public SkiaModel(SKSvgSettings settings)
    {
        Settings = settings;
    }

    public SkiaSharp.SKPoint ToSKPoint(SKPoint point)
    {
        return new(point.X, point.Y);
    }

    public SkiaSharp.SKPoint3 ToSKPoint3(SKPoint3 point3)
    {
        return new(point3.X, point3.Y, point3.Z);
    }

    public SkiaSharp.SKPoint[] ToSKPoints(IList<SKPoint> points)
    {
        var skPoints = new SkiaSharp.SKPoint[points.Count];

        for (var i = 0; i < points.Count; i++)
        {
            skPoints[i] = ToSKPoint(points[i]);
        }

        return skPoints;
    }

    public SkiaSharp.SKPointI ToSKPointI(SKPointI pointI)
    {
        return new(pointI.X, pointI.Y);
    }

    public SkiaSharp.SKSize ToSKSize(SKSize size)
    {
        return new(size.Width, size.Height);
    }

    public SkiaSharp.SKSizeI ToSKSizeI(SKSizeI sizeI)
    {
        return new(sizeI.Width, sizeI.Height);
    }

    public SkiaSharp.SKRect ToSKRect(SKRect rect)
    {
        return new(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public SkiaSharp.SKMatrix ToSKMatrix(SKMatrix matrix)
    {
        return new(
            matrix.ScaleX,
            matrix.SkewX,
            matrix.TransX,
            matrix.SkewY,
            matrix.ScaleY,
            matrix.TransY,
            matrix.Persp0,
            matrix.Persp1,
            matrix.Persp2);
    }

    public SkiaSharp.SKImage ToSKImage(SKImage image)
    {
        return SkiaSharp.SKImage.FromEncodedData(image.Data);
    }

    public SkiaSharp.SKPaintStyle ToSKPaintStyle(SKPaintStyle paintStyle)
    {
        return paintStyle switch
        {
            SKPaintStyle.Fill => SkiaSharp.SKPaintStyle.Fill,
            SKPaintStyle.Stroke => SkiaSharp.SKPaintStyle.Stroke,
            SKPaintStyle.StrokeAndFill => SkiaSharp.SKPaintStyle.StrokeAndFill,
            _ => SkiaSharp.SKPaintStyle.Fill
        };
    }

    public SkiaSharp.SKStrokeCap ToSKStrokeCap(SKStrokeCap strokeCap)
    {
        return strokeCap switch
        {
            SKStrokeCap.Butt => SkiaSharp.SKStrokeCap.Butt,
            SKStrokeCap.Round => SkiaSharp.SKStrokeCap.Round,
            SKStrokeCap.Square => SkiaSharp.SKStrokeCap.Square,
            _ => SkiaSharp.SKStrokeCap.Butt
        };
    }

    public SkiaSharp.SKStrokeJoin ToSKStrokeJoin(SKStrokeJoin strokeJoin)
    {
        return strokeJoin switch
        {
            SKStrokeJoin.Miter => SkiaSharp.SKStrokeJoin.Miter,
            SKStrokeJoin.Round => SkiaSharp.SKStrokeJoin.Round,
            SKStrokeJoin.Bevel => SkiaSharp.SKStrokeJoin.Bevel,
            _ => SkiaSharp.SKStrokeJoin.Miter
        };
    }

    public SkiaSharp.SKTextAlign ToSKTextAlign(SKTextAlign textAlign)
    {
        return textAlign switch
        {
            SKTextAlign.Left => SkiaSharp.SKTextAlign.Left,
            SKTextAlign.Center => SkiaSharp.SKTextAlign.Center,
            SKTextAlign.Right => SkiaSharp.SKTextAlign.Right,
            _ => SkiaSharp.SKTextAlign.Left
        };
    }

    public SkiaSharp.SKTextEncoding ToSKTextEncoding(SKTextEncoding textEncoding)
    {
        return textEncoding switch
        {
            SKTextEncoding.Utf8 => SkiaSharp.SKTextEncoding.Utf8,
            SKTextEncoding.Utf16 => SkiaSharp.SKTextEncoding.Utf16,
            SKTextEncoding.Utf32 => SkiaSharp.SKTextEncoding.Utf32,
            SKTextEncoding.GlyphId => SkiaSharp.SKTextEncoding.GlyphId,
            _ => SkiaSharp.SKTextEncoding.Utf8
        };
    }

    public SkiaSharp.SKFontStyleWeight ToSKFontStyleWeight(SKFontStyleWeight fontStyleWeight)
    {
        return fontStyleWeight switch
        {
            SKFontStyleWeight.Invisible => SkiaSharp.SKFontStyleWeight.Invisible,
            SKFontStyleWeight.Thin => SkiaSharp.SKFontStyleWeight.Thin,
            SKFontStyleWeight.ExtraLight => SkiaSharp.SKFontStyleWeight.ExtraLight,
            SKFontStyleWeight.Light => SkiaSharp.SKFontStyleWeight.Light,
            SKFontStyleWeight.Normal => SkiaSharp.SKFontStyleWeight.Normal,
            SKFontStyleWeight.Medium => SkiaSharp.SKFontStyleWeight.Medium,
            SKFontStyleWeight.SemiBold => SkiaSharp.SKFontStyleWeight.SemiBold,
            SKFontStyleWeight.Bold => SkiaSharp.SKFontStyleWeight.Bold,
            SKFontStyleWeight.ExtraBold => SkiaSharp.SKFontStyleWeight.ExtraBold,
            SKFontStyleWeight.Black => SkiaSharp.SKFontStyleWeight.Black,
            SKFontStyleWeight.ExtraBlack => SkiaSharp.SKFontStyleWeight.ExtraBlack,
            _ => SkiaSharp.SKFontStyleWeight.Invisible
        };
    }

    public SkiaSharp.SKFontStyleWidth ToSKFontStyleWidth(SKFontStyleWidth fontStyleWidth)
    {
        return fontStyleWidth switch
        {
            SKFontStyleWidth.UltraCondensed => SkiaSharp.SKFontStyleWidth.UltraCondensed,
            SKFontStyleWidth.ExtraCondensed => SkiaSharp.SKFontStyleWidth.ExtraCondensed,
            SKFontStyleWidth.Condensed => SkiaSharp.SKFontStyleWidth.Condensed,
            SKFontStyleWidth.SemiCondensed => SkiaSharp.SKFontStyleWidth.SemiCondensed,
            SKFontStyleWidth.Normal => SkiaSharp.SKFontStyleWidth.Normal,
            SKFontStyleWidth.SemiExpanded => SkiaSharp.SKFontStyleWidth.SemiExpanded,
            SKFontStyleWidth.Expanded => SkiaSharp.SKFontStyleWidth.Expanded,
            SKFontStyleWidth.ExtraExpanded => SkiaSharp.SKFontStyleWidth.ExtraExpanded,
            SKFontStyleWidth.UltraExpanded => SkiaSharp.SKFontStyleWidth.UltraExpanded,
            _ => SkiaSharp.SKFontStyleWidth.UltraCondensed
        };
    }

    public SkiaSharp.SKFontStyleSlant ToSKFontStyleSlant(SKFontStyleSlant fontStyleSlant)
    {
        return fontStyleSlant switch
        {
            SKFontStyleSlant.Upright => SkiaSharp.SKFontStyleSlant.Upright,
            SKFontStyleSlant.Italic => SkiaSharp.SKFontStyleSlant.Italic,
            SKFontStyleSlant.Oblique => SkiaSharp.SKFontStyleSlant.Oblique,
            _ => SkiaSharp.SKFontStyleSlant.Upright
        };
    }

    public SkiaSharp.SKTypeface? ToSKTypeface(SKTypeface? typeface)
    {
        if (typeface is null || typeface.FamilyName is null) return SkiaSharp.SKTypeface.Default;

        var fontFamily = typeface.FamilyName;
        var fontWeight = ToSKFontStyleWeight(typeface.FontWeight);
        var fontWidth = ToSKFontStyleWidth(typeface.FontWidth);
        var fontStyle = ToSKFontStyleSlant(typeface.FontSlant);

        if (Settings.TypefaceProviders is { } && Settings.TypefaceProviders.Count > 0)
        {
            foreach (var typefaceProviders in Settings.TypefaceProviders)
            {
                var skTypeface = typefaceProviders.FromFamilyName(fontFamily, fontWeight, fontWidth, fontStyle);
                if (skTypeface is { })
                {
                    return skTypeface;
                }
            }
        }

        return SkiaSharp.SKTypeface.FromFamilyName(fontFamily, fontWeight, fontWidth, fontStyle);
    }

    public SkiaSharp.SKColor ToSKColor(SKColor color)
    {
        return new(color.Red, color.Green, color.Blue, color.Alpha);
    }

    public SkiaSharp.SKColor[] ToSKColors(SKColor[] colors)
    {
        var skColors = new SkiaSharp.SKColor[colors.Length];

        for (var i = 0; i < colors.Length; i++)
        {
            skColors[i] = ToSKColor(colors[i]);
        }

        return skColors;
    }

    public SkiaSharp.SKColorF ToSKColor(SKColorF color)
    {
        return new(color.Red, color.Green, color.Blue, color.Alpha);
    }

    public SkiaSharp.SKColorF[] ToSKColors(SKColorF[] colors)
    {
        var skColors = new SkiaSharp.SKColorF[colors.Length];

        for (var i = 0; i < colors.Length; i++)
        {
            skColors[i] = ToSKColor(colors[i]);
        }

        return skColors;
    }

    public SkiaSharp.SKShaderTileMode ToSKShaderTileMode(SKShaderTileMode shaderTileMode)
    {
        return shaderTileMode switch
        {
            SKShaderTileMode.Clamp => SkiaSharp.SKShaderTileMode.Clamp,
            SKShaderTileMode.Repeat => SkiaSharp.SKShaderTileMode.Repeat,
            SKShaderTileMode.Mirror => SkiaSharp.SKShaderTileMode.Mirror,
            SKShaderTileMode.Decal => SkiaSharp.SKShaderTileMode.Decal,
            _ => SkiaSharp.SKShaderTileMode.Clamp
        };
    }

    public SkiaSharp.SKShader? ToSKShader(SKShader? shader)
    {
        switch (shader)
        {
            case ColorShader colorShader:
            {
                return SkiaSharp.SKShader.CreateColor(
                    ToSKColor(colorShader.Color),
                    colorShader.ColorSpace == SKColorSpace.Srgb 
                        ? Settings.Srgb 
                        : Settings.SrgbLinear);
            }
            case LinearGradientShader linearGradientShader:
            {
                if (linearGradientShader.Colors is null || linearGradientShader.ColorPos is null)
                {
                    return null;
                }

                if (linearGradientShader.LocalMatrix is { })
                {
                    return SkiaSharp.SKShader.CreateLinearGradient(
                        ToSKPoint(linearGradientShader.Start),
                        ToSKPoint(linearGradientShader.End),
                        ToSKColors(linearGradientShader.Colors),
                        linearGradientShader.ColorSpace == SKColorSpace.Srgb
                            ? Settings.Srgb
                            : Settings.SrgbLinear,
                        linearGradientShader.ColorPos,
                        ToSKShaderTileMode(linearGradientShader.Mode),
                        ToSKMatrix(linearGradientShader.LocalMatrix.Value));
                }

                return SkiaSharp.SKShader.CreateLinearGradient(
                    ToSKPoint(linearGradientShader.Start),
                    ToSKPoint(linearGradientShader.End),
                    ToSKColors(linearGradientShader.Colors),
                    linearGradientShader.ColorSpace == SKColorSpace.Srgb 
                        ? Settings.Srgb 
                        : Settings.SrgbLinear,
                    linearGradientShader.ColorPos,
                    ToSKShaderTileMode(linearGradientShader.Mode));
            }
            case RadialGradientShader radialGradientShader:
            {
                if (radialGradientShader.Colors is null || radialGradientShader.ColorPos is null)
                {
                    return null;
                }

                if (radialGradientShader.LocalMatrix is { })
                {
                    return SkiaSharp.SKShader.CreateRadialGradient(
                        ToSKPoint(radialGradientShader.Center),
                        radialGradientShader.Radius,
                        ToSKColors(radialGradientShader.Colors),
                        radialGradientShader.ColorSpace == SKColorSpace.Srgb
                            ? Settings.Srgb
                            : Settings.SrgbLinear,
                        radialGradientShader.ColorPos,
                        ToSKShaderTileMode(radialGradientShader.Mode),
                        ToSKMatrix(radialGradientShader.LocalMatrix.Value));
                }

                return SkiaSharp.SKShader.CreateRadialGradient(
                    ToSKPoint(radialGradientShader.Center),
                    radialGradientShader.Radius,
                    ToSKColors(radialGradientShader.Colors),
                    radialGradientShader.ColorSpace == SKColorSpace.Srgb 
                        ? Settings.Srgb 
                        : Settings.SrgbLinear,
                    radialGradientShader.ColorPos,
                    ToSKShaderTileMode(radialGradientShader.Mode));
            }
            case TwoPointConicalGradientShader twoPointConicalGradientShader:
            {
                if (twoPointConicalGradientShader.Colors is null || twoPointConicalGradientShader.ColorPos is null)
                {
                    return null;
                }

                if (twoPointConicalGradientShader.LocalMatrix is { })
                {
                    return SkiaSharp.SKShader.CreateTwoPointConicalGradient(
                        ToSKPoint(twoPointConicalGradientShader.Start),
                        twoPointConicalGradientShader.StartRadius,
                        ToSKPoint(twoPointConicalGradientShader.End),
                        twoPointConicalGradientShader.EndRadius,
                        ToSKColors(twoPointConicalGradientShader.Colors),
                        twoPointConicalGradientShader.ColorSpace == SKColorSpace.Srgb
                            ? Settings.Srgb
                            : Settings.SrgbLinear,
                        twoPointConicalGradientShader.ColorPos,
                        ToSKShaderTileMode(twoPointConicalGradientShader.Mode),
                        ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value));
                }

                return SkiaSharp.SKShader.CreateTwoPointConicalGradient(
                    ToSKPoint(twoPointConicalGradientShader.Start),
                    twoPointConicalGradientShader.StartRadius,
                    ToSKPoint(twoPointConicalGradientShader.End),
                    twoPointConicalGradientShader.EndRadius,
                    ToSKColors(twoPointConicalGradientShader.Colors),
                    twoPointConicalGradientShader.ColorSpace == SKColorSpace.Srgb 
                        ? Settings.Srgb 
                        : Settings.SrgbLinear,
                    twoPointConicalGradientShader.ColorPos,
                    ToSKShaderTileMode(twoPointConicalGradientShader.Mode));
            }
            case PictureShader pictureShader:
            {
                if (pictureShader.Src is null)
                {
                    return null;
                }

                return SkiaSharp.SKShader.CreatePicture(
                    ToSKPicture(pictureShader.Src),
                    SkiaSharp.SKShaderTileMode.Repeat,
                    SkiaSharp.SKShaderTileMode.Repeat,
                    ToSKMatrix(pictureShader.LocalMatrix),
                    ToSKRect(pictureShader.Tile));
            }
            case PerlinNoiseFractalNoiseShader perlinNoiseFractalNoiseShader:
            {
                return SkiaSharp.SKShader.CreatePerlinNoiseFractalNoise(
                    perlinNoiseFractalNoiseShader.BaseFrequencyX,
                    perlinNoiseFractalNoiseShader.BaseFrequencyY,
                    perlinNoiseFractalNoiseShader.NumOctaves,
                    perlinNoiseFractalNoiseShader.Seed,
                    ToSKPointI(perlinNoiseFractalNoiseShader.TileSize));
            }
            case PerlinNoiseTurbulenceShader perlinNoiseTurbulenceShader:
            {
                return SkiaSharp.SKShader.CreatePerlinNoiseTurbulence(
                    perlinNoiseTurbulenceShader.BaseFrequencyX,
                    perlinNoiseTurbulenceShader.BaseFrequencyY,
                    perlinNoiseTurbulenceShader.NumOctaves,
                    perlinNoiseTurbulenceShader.Seed,
                    ToSKPointI(perlinNoiseTurbulenceShader.TileSize));
            }
            default:
                return null;
        }
    }

    public SkiaSharp.SKColorFilter? ToSKColorFilter(SKColorFilter? colorFilter)
    {
        switch (colorFilter)
        {
            case BlendModeColorFilter blendModeColorFilter:
            {
                return SkiaSharp.SKColorFilter.CreateBlendMode(
                    ToSKColor(blendModeColorFilter.Color),
                    ToSKBlendMode(blendModeColorFilter.Mode));
            }
            case ColorMatrixColorFilter colorMatrixColorFilter:
            {
                if (colorMatrixColorFilter.Matrix is null)
                {
                    return null;
                }
                return SkiaSharp.SKColorFilter.CreateColorMatrix(colorMatrixColorFilter.Matrix);
            }
            case LumaColorColorFilter _:
            {
                return SkiaSharp.SKColorFilter.CreateLumaColor();
            }
            case TableColorFilter tableColorFilter:
            {
                if (tableColorFilter.TableA is null
                    || tableColorFilter.TableR is null
                    || tableColorFilter.TableG is null
                    || tableColorFilter.TableB is null)
                    return null;
                {
                    return SkiaSharp.SKColorFilter.CreateTable(
                        tableColorFilter.TableA,
                        tableColorFilter.TableR,
                        tableColorFilter.TableG,
                        tableColorFilter.TableB);
                }
            }
            default:
            {
                return null;
            }
        }
    }

    public SkiaSharp.SKColorChannel ToSKColorChannel(SKColorChannel colorChannel)
    {
        return colorChannel switch
        {
            SKColorChannel.R => SkiaSharp.SKColorChannel.R,
            SKColorChannel.G => SkiaSharp.SKColorChannel.G,
            SKColorChannel.B => SkiaSharp.SKColorChannel.B,
            SKColorChannel.A => SkiaSharp.SKColorChannel.A,
            _ => SkiaSharp.SKColorChannel.R
        };
    }

    public SkiaSharp.SKImageFilter? ToSKImageFilter(SKImageFilter? imageFilter)
    {
        switch (imageFilter)
        {
            case ArithmeticImageFilter arithmeticImageFilter:
            {
                if (arithmeticImageFilter.Background is null)
                {
                    return null;
                }

                return arithmeticImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateArithmetic(
                        arithmeticImageFilter.K1,
                        arithmeticImageFilter.K2,
                        arithmeticImageFilter.K3,
                        arithmeticImageFilter.K4,
                        arithmeticImageFilter.EforcePMColor,
                        ToSKImageFilter(arithmeticImageFilter.Background),
                        ToSKImageFilter(arithmeticImageFilter.Foreground),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateArithmetic(
                        arithmeticImageFilter.K1,
                        arithmeticImageFilter.K2,
                        arithmeticImageFilter.K3,
                        arithmeticImageFilter.K4,
                        arithmeticImageFilter.EforcePMColor,
                        ToSKImageFilter(arithmeticImageFilter.Background),
                        ToSKImageFilter(arithmeticImageFilter.Foreground));
            }
            case BlendModeImageFilter blendModeImageFilter:
            {
                if (blendModeImageFilter.Background is null)
                {
                    return null;
                }

                return blendModeImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateBlendMode(
                        ToSKBlendMode(blendModeImageFilter.Mode),
                        ToSKImageFilter(blendModeImageFilter.Background),
                        ToSKImageFilter(blendModeImageFilter.Foreground),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateBlendMode(
                        ToSKBlendMode(blendModeImageFilter.Mode),
                        ToSKImageFilter(blendModeImageFilter.Background),
                        ToSKImageFilter(blendModeImageFilter.Foreground));
            }
            case BlurImageFilter blurImageFilter:
            {
                return blurImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateBlur(
                        blurImageFilter.SigmaX,
                        blurImageFilter.SigmaY,
                        ToSKImageFilter(blurImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateBlur(
                        blurImageFilter.SigmaX,
                        blurImageFilter.SigmaY,
                        ToSKImageFilter(blurImageFilter.Input));
            }
            case ColorFilterImageFilter colorFilterImageFilter:
            {
                if (colorFilterImageFilter.ColorFilter is null)
                {
                    return null;
                }

                return colorFilterImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateColorFilter(
                        ToSKColorFilter(colorFilterImageFilter.ColorFilter)!,
                        ToSKImageFilter(colorFilterImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateColorFilter(
                        ToSKColorFilter(colorFilterImageFilter.ColorFilter)!,
                        ToSKImageFilter(colorFilterImageFilter.Input));
            }
            case DilateImageFilter dilateImageFilter:
            {
                return dilateImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateDilate(
                        dilateImageFilter.RadiusX,
                        dilateImageFilter.RadiusY,
                        ToSKImageFilter(dilateImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateDilate(
                        dilateImageFilter.RadiusX,
                        dilateImageFilter.RadiusY,
                        ToSKImageFilter(dilateImageFilter.Input));
            }
            case DisplacementMapEffectImageFilter displacementMapEffectImageFilter:
            {
                if (displacementMapEffectImageFilter.Displacement is null)
                {
                    return null;
                }

                return displacementMapEffectImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateDisplacementMapEffect(
                        ToSKColorChannel(displacementMapEffectImageFilter.XChannelSelector),
                        ToSKColorChannel(displacementMapEffectImageFilter.YChannelSelector),
                        displacementMapEffectImageFilter.Scale,
                        ToSKImageFilter(displacementMapEffectImageFilter.Displacement)!,
                        ToSKImageFilter(displacementMapEffectImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateDisplacementMapEffect(
                        ToSKColorChannel(displacementMapEffectImageFilter.XChannelSelector),
                        ToSKColorChannel(displacementMapEffectImageFilter.YChannelSelector),
                        displacementMapEffectImageFilter.Scale,
                        ToSKImageFilter(displacementMapEffectImageFilter.Displacement)!,
                        ToSKImageFilter(displacementMapEffectImageFilter.Input));
            }
            case DistantLitDiffuseImageFilter distantLitDiffuseImageFilter:
            {
                return distantLitDiffuseImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateDistantLitDiffuse(
                        ToSKPoint3(distantLitDiffuseImageFilter.Direction),
                        ToSKColor(distantLitDiffuseImageFilter.LightColor),
                        distantLitDiffuseImageFilter.SurfaceScale,
                        distantLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(distantLitDiffuseImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateDistantLitDiffuse(
                        ToSKPoint3(distantLitDiffuseImageFilter.Direction),
                        ToSKColor(distantLitDiffuseImageFilter.LightColor),
                        distantLitDiffuseImageFilter.SurfaceScale,
                        distantLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(distantLitDiffuseImageFilter.Input));
            }
            case DistantLitSpecularImageFilter distantLitSpecularImageFilter:
            {
                return distantLitSpecularImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateDistantLitSpecular(
                        ToSKPoint3(distantLitSpecularImageFilter.Direction),
                        ToSKColor(distantLitSpecularImageFilter.LightColor),
                        distantLitSpecularImageFilter.SurfaceScale,
                        distantLitSpecularImageFilter.Ks,
                        distantLitSpecularImageFilter.Shininess,
                        ToSKImageFilter(distantLitSpecularImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateDistantLitSpecular(
                        ToSKPoint3(distantLitSpecularImageFilter.Direction),
                        ToSKColor(distantLitSpecularImageFilter.LightColor),
                        distantLitSpecularImageFilter.SurfaceScale,
                        distantLitSpecularImageFilter.Ks,
                        distantLitSpecularImageFilter.Shininess,
                        ToSKImageFilter(distantLitSpecularImageFilter.Input));
            }
            case ErodeImageFilter erodeImageFilter:
            {
                return erodeImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateErode(
                        erodeImageFilter.RadiusX,
                        erodeImageFilter.RadiusY,
                        ToSKImageFilter(erodeImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateErode(
                        erodeImageFilter.RadiusX,
                        erodeImageFilter.RadiusY,
                        ToSKImageFilter(erodeImageFilter.Input));
            }
            case ImageImageFilter imageImageFilter:
            {
                if (imageImageFilter.Image is null)
                {
                    return null;
                }

                var sampling = new SkiaSharp.SKSamplingOptions(
                    SkiaSharp.SKFilterMode.Linear,
                    SkiaSharp.SKMipmapMode.Linear);

                return SkiaSharp.SKImageFilter.CreateImage(
                    ToSKImage(imageImageFilter.Image),
                    ToSKRect(imageImageFilter.Src),
                    ToSKRect(imageImageFilter.Dst),
                    sampling);
            }
            case MatrixConvolutionImageFilter matrixConvolutionImageFilter:
            {
                if (matrixConvolutionImageFilter.Kernel is null)
                {
                    return null;
                }

                return matrixConvolutionImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateMatrixConvolution(
                        ToSKSizeI(matrixConvolutionImageFilter.KernelSize),
                        matrixConvolutionImageFilter.Kernel,
                        matrixConvolutionImageFilter.Gain,
                        matrixConvolutionImageFilter.Bias,
                        ToSKPointI(matrixConvolutionImageFilter.KernelOffset),
                        ToSKShaderTileMode(matrixConvolutionImageFilter.TileMode),
                        matrixConvolutionImageFilter.ConvolveAlpha,
                        ToSKImageFilter(matrixConvolutionImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateMatrixConvolution(
                        ToSKSizeI(matrixConvolutionImageFilter.KernelSize),
                        matrixConvolutionImageFilter.Kernel,
                        matrixConvolutionImageFilter.Gain,
                        matrixConvolutionImageFilter.Bias,
                        ToSKPointI(matrixConvolutionImageFilter.KernelOffset),
                        ToSKShaderTileMode(matrixConvolutionImageFilter.TileMode),
                        matrixConvolutionImageFilter.ConvolveAlpha,
                        ToSKImageFilter(matrixConvolutionImageFilter.Input));
            }
            case MergeImageFilter mergeImageFilter:
            {
                if (mergeImageFilter.Filters is null)
                {
                    return null;
                }

                return mergeImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateMerge(
                        ToSKImageFilters(mergeImageFilter.Filters),
                        ToSKRect(clip	))
                    : SkiaSharp.SKImageFilter.CreateMerge(
                        ToSKImageFilters(mergeImageFilter.Filters));
            }
            case OffsetImageFilter offsetImageFilter:
            {
                return offsetImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateOffset(
                        offsetImageFilter.Dx,
                        offsetImageFilter.Dy,
                        ToSKImageFilter(offsetImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateOffset(
                        offsetImageFilter.Dx,
                        offsetImageFilter.Dy,
                        ToSKImageFilter(offsetImageFilter.Input));
            }
            case PaintImageFilter paintImageFilter:
            {
                if (paintImageFilter.Paint is null)
                {
                    return null;
                }

                var shader = paintImageFilter.Paint.Shader ?? SKShader.CreateColor(paintImageFilter.Paint.Color!.Value, SKColorSpace.Srgb);

                return paintImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateShader(ToSKShader(shader), dither: false, cropRect: ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateShader(ToSKShader(shader), dither: false);
            }
            case ShaderImageFilter shaderImageFilter:
            {
                if (shaderImageFilter.Shader is null)
                {
                    return null;
                }

                return shaderImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateShader(
                        ToSKShader(shaderImageFilter.Shader),
                        shaderImageFilter.Dither,
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateShader(
                        ToSKShader(shaderImageFilter.Shader),
                        shaderImageFilter.Dither);
            }
            case PictureImageFilter pictureImageFilter:
            {
                if (pictureImageFilter.Picture is null)
                {
                    return null;
                }

                return SkiaSharp.SKImageFilter.CreatePicture(
                    ToSKPicture(pictureImageFilter.Picture)!,
                    ToSKRect(pictureImageFilter.Picture.CullRect));
            }
            case PointLitDiffuseImageFilter pointLitDiffuseImageFilter:
            {
                return pointLitDiffuseImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreatePointLitDiffuse(
                        ToSKPoint3(pointLitDiffuseImageFilter.Location),
                        ToSKColor(pointLitDiffuseImageFilter.LightColor),
                        pointLitDiffuseImageFilter.SurfaceScale,
                        pointLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(pointLitDiffuseImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreatePointLitDiffuse(
                        ToSKPoint3(pointLitDiffuseImageFilter.Location),
                        ToSKColor(pointLitDiffuseImageFilter.LightColor),
                        pointLitDiffuseImageFilter.SurfaceScale,
                        pointLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(pointLitDiffuseImageFilter.Input));
            }
            case PointLitSpecularImageFilter pointLitSpecularImageFilter:
            {
                return pointLitSpecularImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreatePointLitSpecular(
                        ToSKPoint3(pointLitSpecularImageFilter.Location),
                        ToSKColor(pointLitSpecularImageFilter.LightColor),
                        pointLitSpecularImageFilter.SurfaceScale,
                        pointLitSpecularImageFilter.Ks,
                        pointLitSpecularImageFilter.Shininess,
                        ToSKImageFilter(pointLitSpecularImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreatePointLitSpecular(
                        ToSKPoint3(pointLitSpecularImageFilter.Location),
                        ToSKColor(pointLitSpecularImageFilter.LightColor),
                        pointLitSpecularImageFilter.SurfaceScale,
                        pointLitSpecularImageFilter.Ks,
                        pointLitSpecularImageFilter.Shininess,
                        ToSKImageFilter(pointLitSpecularImageFilter.Input));
            }
            case SpotLitDiffuseImageFilter spotLitDiffuseImageFilter:
            {
                return spotLitDiffuseImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateSpotLitDiffuse(
                        ToSKPoint3(spotLitDiffuseImageFilter.Location),
                        ToSKPoint3(spotLitDiffuseImageFilter.Target),
                        spotLitDiffuseImageFilter.SpecularExponent,
                        spotLitDiffuseImageFilter.CutoffAngle,
                        ToSKColor(spotLitDiffuseImageFilter.LightColor),
                        spotLitDiffuseImageFilter.SurfaceScale,
                        spotLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(spotLitDiffuseImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateSpotLitDiffuse(
                        ToSKPoint3(spotLitDiffuseImageFilter.Location),
                        ToSKPoint3(spotLitDiffuseImageFilter.Target),
                        spotLitDiffuseImageFilter.SpecularExponent,
                        spotLitDiffuseImageFilter.CutoffAngle,
                        ToSKColor(spotLitDiffuseImageFilter.LightColor),
                        spotLitDiffuseImageFilter.SurfaceScale,
                        spotLitDiffuseImageFilter.Kd,
                        ToSKImageFilter(spotLitDiffuseImageFilter.Input));
            }
            case SpotLitSpecularImageFilter spotLitSpecularImageFilter:
            {
                return spotLitSpecularImageFilter.Clip is { } clip
                    ? SkiaSharp.SKImageFilter.CreateSpotLitSpecular(
                        ToSKPoint3(spotLitSpecularImageFilter.Location),
                        ToSKPoint3(spotLitSpecularImageFilter.Target),
                        spotLitSpecularImageFilter.SpecularExponent,
                        spotLitSpecularImageFilter.CutoffAngle,
                        ToSKColor(spotLitSpecularImageFilter.LightColor),
                        spotLitSpecularImageFilter.SurfaceScale,
                        spotLitSpecularImageFilter.Ks,
                        spotLitSpecularImageFilter.SpecularExponent,
                        ToSKImageFilter(spotLitSpecularImageFilter.Input),
                        ToSKRect(clip))
                    : SkiaSharp.SKImageFilter.CreateSpotLitSpecular(
                        ToSKPoint3(spotLitSpecularImageFilter.Location),
                        ToSKPoint3(spotLitSpecularImageFilter.Target),
                        spotLitSpecularImageFilter.SpecularExponent,
                        spotLitSpecularImageFilter.CutoffAngle,
                        ToSKColor(spotLitSpecularImageFilter.LightColor),
                        spotLitSpecularImageFilter.SurfaceScale,
                        spotLitSpecularImageFilter.Ks,
                        spotLitSpecularImageFilter.SpecularExponent,
                        ToSKImageFilter(spotLitSpecularImageFilter.Input));
            }
            case TileImageFilter tileImageFilter:
            {
                return SkiaSharp.SKImageFilter.CreateTile(
                    ToSKRect(tileImageFilter.Src),
                    ToSKRect(tileImageFilter.Dst),
                    ToSKImageFilter(tileImageFilter.Input));
            }
            default:
            {
                return null;
            }
        }
    }

    public SkiaSharp.SKImageFilter[]? ToSKImageFilters(SKImageFilter[]? imageFilters)
    {
        if (imageFilters is null)
        {
            return null;
        }

        var skImageFilters = new SkiaSharp.SKImageFilter[imageFilters.Length];

        for (var i = 0; i < imageFilters.Length; i++)
        {
            var imageFilter = imageFilters[i];
            var skImageFilter = ToSKImageFilter(imageFilter);
            if (skImageFilter is { })
            {
                skImageFilters[i] = skImageFilter;
            }
        }

        return skImageFilters;
    }

    public SkiaSharp.SKPathEffect? ToSKPathEffect(SKPathEffect? pathEffect)
    {
        switch (pathEffect)
        {
            case DashPathEffect dashPathEffect:
            {
                return SkiaSharp.SKPathEffect.CreateDash(
                    dashPathEffect.Intervals,
                    dashPathEffect.Phase);
            }
            default:
            {
                return null;
            }
        }
    }

    public SkiaSharp.SKBlendMode ToSKBlendMode(SKBlendMode blendMode)
    {
        return blendMode switch
        {
            SKBlendMode.Clear => SkiaSharp.SKBlendMode.Clear,
            SKBlendMode.Src => SkiaSharp.SKBlendMode.Src,
            SKBlendMode.Dst => SkiaSharp.SKBlendMode.Dst,
            SKBlendMode.SrcOver => SkiaSharp.SKBlendMode.SrcOver,
            SKBlendMode.DstOver => SkiaSharp.SKBlendMode.DstOver,
            SKBlendMode.SrcIn => SkiaSharp.SKBlendMode.SrcIn,
            SKBlendMode.DstIn => SkiaSharp.SKBlendMode.DstIn,
            SKBlendMode.SrcOut => SkiaSharp.SKBlendMode.SrcOut,
            SKBlendMode.DstOut => SkiaSharp.SKBlendMode.DstOut,
            SKBlendMode.SrcATop => SkiaSharp.SKBlendMode.SrcATop,
            SKBlendMode.DstATop => SkiaSharp.SKBlendMode.DstATop,
            SKBlendMode.Xor => SkiaSharp.SKBlendMode.Xor,
            SKBlendMode.Plus => SkiaSharp.SKBlendMode.Plus,
            SKBlendMode.Modulate => SkiaSharp.SKBlendMode.Modulate,
            SKBlendMode.Screen => SkiaSharp.SKBlendMode.Screen,
            SKBlendMode.Overlay => SkiaSharp.SKBlendMode.Overlay,
            SKBlendMode.Darken => SkiaSharp.SKBlendMode.Darken,
            SKBlendMode.Lighten => SkiaSharp.SKBlendMode.Lighten,
            SKBlendMode.ColorDodge => SkiaSharp.SKBlendMode.ColorDodge,
            SKBlendMode.ColorBurn => SkiaSharp.SKBlendMode.ColorBurn,
            SKBlendMode.HardLight => SkiaSharp.SKBlendMode.HardLight,
            SKBlendMode.SoftLight => SkiaSharp.SKBlendMode.SoftLight,
            SKBlendMode.Difference => SkiaSharp.SKBlendMode.Difference,
            SKBlendMode.Exclusion => SkiaSharp.SKBlendMode.Exclusion,
            SKBlendMode.Multiply => SkiaSharp.SKBlendMode.Multiply,
            SKBlendMode.Hue => SkiaSharp.SKBlendMode.Hue,
            SKBlendMode.Saturation => SkiaSharp.SKBlendMode.Saturation,
            SKBlendMode.Color => SkiaSharp.SKBlendMode.Color,
            SKBlendMode.Luminosity => SkiaSharp.SKBlendMode.Luminosity,
            _ => SkiaSharp.SKBlendMode.Clear
        };
    }

#pragma warning disable CS0618 // Type or member is obsolete
	public SkiaSharp.SKFilterQuality ToSKFilterQuality(SKFilterQuality filterQuality)
	{
        return filterQuality switch
        {
            SKFilterQuality.None => SkiaSharp.SKFilterQuality.None,
            SKFilterQuality.Low => SkiaSharp.SKFilterQuality.Low,
            SKFilterQuality.Medium => SkiaSharp.SKFilterQuality.Medium,
            SKFilterQuality.High => SkiaSharp.SKFilterQuality.High,
            _ => SkiaSharp.SKFilterQuality.None
        };
    }
#pragma warning restore format

    public SkiaSharp.SKPaint? ToSKPaint(SKPaint? paint)
    {
        if (paint is null)
        {
            return null;
        }

        var style = ToSKPaintStyle(paint.Style);
        var strokeCap = ToSKStrokeCap(paint.StrokeCap);
        var strokeJoin = ToSKStrokeJoin(paint.StrokeJoin);
        var textAlign = ToSKTextAlign(paint.TextAlign);
        var typeface = ToSKTypeface(paint.Typeface);
        var textEncoding = ToSKTextEncoding(paint.TextEncoding);
        var color = paint.Color is null 
            ? SkiaSharp.SKColor.Empty : 
            ToSKColor(paint.Color.Value);
        var shader = ToSKShader(paint.Shader);
        var colorFilter = ToSKColorFilter(paint.ColorFilter);
        var imageFilter = ToSKImageFilter(paint.ImageFilter);
        var pathEffect = ToSKPathEffect(paint.PathEffect);
        var blendMode = ToSKBlendMode(paint.BlendMode);
        var filterQuality = ToSKFilterQuality(paint.FilterQuality);

        return new SkiaSharp.SKPaint
        {
            Style = style,
            IsAntialias = paint.IsAntialias,
            StrokeWidth = paint.StrokeWidth,
            StrokeCap = strokeCap,
            StrokeJoin = strokeJoin,
            StrokeMiter = paint.StrokeMiter,
            TextSize = paint.TextSize,
            TextAlign = textAlign,
            Typeface = typeface,
            LcdRenderText = paint.LcdRenderText,
            SubpixelText = paint.SubpixelText,
            TextEncoding = textEncoding,
            Color = color,
            Shader = shader,
            ColorFilter = colorFilter,
            ImageFilter = imageFilter,
            PathEffect = pathEffect,
            BlendMode = blendMode,
            FilterQuality = filterQuality
        };
    }

    public SkiaSharp.SKClipOperation ToSKClipOperation(SKClipOperation clipOperation)
    {
        return clipOperation switch
        {
            SKClipOperation.Difference => SkiaSharp.SKClipOperation.Difference,
            SKClipOperation.Intersect => SkiaSharp.SKClipOperation.Intersect,
            _ => SkiaSharp.SKClipOperation.Difference
        };
    }

    public SkiaSharp.SKPathFillType ToSKPathFillType(SKPathFillType pathFillType)
    {
        return pathFillType switch
        {
            SKPathFillType.Winding => SkiaSharp.SKPathFillType.Winding,
            SKPathFillType.EvenOdd => SkiaSharp.SKPathFillType.EvenOdd,
            _ => SkiaSharp.SKPathFillType.Winding
        };
    }

    public SkiaSharp.SKPathArcSize ToSKPathArcSize(SKPathArcSize pathArcSize)
    {
        return pathArcSize switch
        {
            SKPathArcSize.Small => SkiaSharp.SKPathArcSize.Small,
            SKPathArcSize.Large => SkiaSharp.SKPathArcSize.Large,
            _ => SkiaSharp.SKPathArcSize.Small
        };
    }

    public SkiaSharp.SKPathDirection ToSKPathDirection(SKPathDirection pathDirection)
    {
        return pathDirection switch
        {
            SKPathDirection.Clockwise => SkiaSharp.SKPathDirection.Clockwise,
            SKPathDirection.CounterClockwise => SkiaSharp.SKPathDirection.CounterClockwise,
            _ => SkiaSharp.SKPathDirection.Clockwise
        };
    }

    public void ToSKPath(PathCommand pathCommand, SkiaSharp.SKPath skPath)
    {
        switch (pathCommand)
        {
            case MoveToPathCommand moveToPathCommand:
            {
                var x = moveToPathCommand.X;
                var y = moveToPathCommand.Y;
                skPath.MoveTo(x, y);
                break;
            }
            case LineToPathCommand lineToPathCommand:
            {
                var x = lineToPathCommand.X;
                var y = lineToPathCommand.Y;
                skPath.LineTo(x, y);
                break;
            }
            case ArcToPathCommand arcToPathCommand:
            {
                var rx = arcToPathCommand.Rx;
                var ry = arcToPathCommand.Ry;
                var xAxisRotate = arcToPathCommand.XAxisRotate;
                var largeArc = ToSKPathArcSize(arcToPathCommand.LargeArc);
                var sweep = ToSKPathDirection(arcToPathCommand.Sweep);
                var x = arcToPathCommand.X;
                var y = arcToPathCommand.Y;
                skPath.ArcTo(rx, ry, xAxisRotate, largeArc, sweep, x, y);
                break;
            }
            case QuadToPathCommand quadToPathCommand:
            {
                var x0 = quadToPathCommand.X0;
                var y0 = quadToPathCommand.Y0;
                var x1 = quadToPathCommand.X1;
                var y1 = quadToPathCommand.Y1;
                skPath.QuadTo(x0, y0, x1, y1);
                break;
            }
            case CubicToPathCommand cubicToPathCommand:
            {
                var x0 = cubicToPathCommand.X0;
                var y0 = cubicToPathCommand.Y0;
                var x1 = cubicToPathCommand.X1;
                var y1 = cubicToPathCommand.Y1;
                var x2 = cubicToPathCommand.X2;
                var y2 = cubicToPathCommand.Y2;
                skPath.CubicTo(x0, y0, x1, y1, x2, y2);
                break;
            }
            case ClosePathCommand _:
            {
                skPath.Close();
                break;
            }
            case AddRectPathCommand addRectPathCommand:
            {
                var rect = ToSKRect(addRectPathCommand.Rect);
                skPath.AddRect(rect);
                break;
            }
            case AddRoundRectPathCommand addRoundRectPathCommand:
            {
                var rect = ToSKRect(addRoundRectPathCommand.Rect);
                var rx = addRoundRectPathCommand.Rx;
                var ry = addRoundRectPathCommand.Ry;
                skPath.AddRoundRect(rect, rx, ry);
                break;
            }
            case AddOvalPathCommand addOvalPathCommand:
            {
                var rect = ToSKRect(addOvalPathCommand.Rect);
                skPath.AddOval(rect);
                break;
            }
            case AddCirclePathCommand addCirclePathCommand:
            {
                var x = addCirclePathCommand.X;
                var y = addCirclePathCommand.Y;
                var radius = addCirclePathCommand.Radius;
                skPath.AddCircle(x, y, radius);
                break;
            }
            case AddPolyPathCommand addPolyPathCommand:
            {
                if (addPolyPathCommand.Points is { })
                {
                    var points = ToSKPoints(addPolyPathCommand.Points);
                    var close = addPolyPathCommand.Close;
                    skPath.AddPoly(points, close);
                }
                break;
            }
        }
    }

    public SkiaSharp.SKPath ToSKPath(SKPath path)
    {
        var skPath = new SkiaSharp.SKPath
        {
            FillType = ToSKPathFillType(path.FillType)
        };

        if (path.Commands is null)
        {
            return skPath;
        }

        foreach (var pathCommand in path.Commands)
        {
            ToSKPath(pathCommand, skPath);
        }

        return skPath;
    }

    public SkiaSharp.SKPath? ToSKPath(ClipPath? clipPath)
    {
        if (clipPath?.Clips is null)
        {
            return null;
        }

        var skPathResult = default(SkiaSharp.SKPath);

        foreach (var clip in clipPath.Clips)
        {
            if (clip.Path is null)
            {
                return null;
            }

            var skPath = ToSKPath(clip.Path);
            var skPathClip = ToSKPath(clip.Clip);
            if (skPathClip is { }) skPath = skPath.Op(skPathClip, SkiaSharp.SKPathOp.Intersect);

            if (clip.Transform is { })
            {
                var skMatrix = ToSKMatrix(clip.Transform.Value);
                skPath.Transform(skMatrix);
            }

            if (skPathResult is null)
            {
                skPathResult = skPath;
            }
            else
            {
                var result = skPathResult.Op(skPath, SkiaSharp.SKPathOp.Union);
                skPathResult = result;
            }
        }

        if (skPathResult is { })
        {
            if (clipPath.Clip?.Clips is { })
            {
                var skPathClip = ToSKPath(clipPath.Clip);
                if (skPathClip is { }) skPathResult = skPathResult.Op(skPathClip, SkiaSharp.SKPathOp.Intersect);
            }

            if (clipPath.Transform is { })
            {
                var skMatrix = ToSKMatrix(clipPath.Transform.Value);
                skPathResult.Transform(skMatrix);
            }
        }

        return skPathResult;
    }

    public SkiaSharp.SKPicture? ToSKPicture(SKPicture? picture)
    {
        if (picture is null)
        {
            return null;
        }

        var skRect = ToSKRect(picture.CullRect);
        using var skPictureRecorder = new SkiaSharp.SKPictureRecorder();
        using var skCanvas = skPictureRecorder.BeginRecording(skRect);

        Draw(picture, skCanvas);

        return skPictureRecorder.EndRecording();
    }

    public void Draw(CanvasCommand canvasCommand, SkiaSharp.SKCanvas skCanvas)
    {
        switch (canvasCommand)
        {
            case ClipPathCanvasCommand clipPathCanvasCommand:
            {
                if (clipPathCanvasCommand.ClipPath is { })
                {
                    var path = ToSKPath(clipPathCanvasCommand.ClipPath);
                    var operation = ToSKClipOperation(clipPathCanvasCommand.Operation);
                    var antialias = clipPathCanvasCommand.Antialias;
                    skCanvas.ClipPath(path, operation, antialias);
                }
                break;
            }
            case ClipRectCanvasCommand clipRectCanvasCommand:
            {
                var rect = ToSKRect(clipRectCanvasCommand.Rect);
                var operation = ToSKClipOperation(clipRectCanvasCommand.Operation);
                var antialias = clipRectCanvasCommand.Antialias;
                skCanvas.ClipRect(rect, operation, antialias);
                break;
            }
            case SaveCanvasCommand _:
            {
                skCanvas.Save();
                break;
            }
            case RestoreCanvasCommand _:
            {
                skCanvas.Restore();
                break;
            }
            case SetMatrixCanvasCommand setMatrixCanvasCommand:
            {
                var matrix = ToSKMatrix(setMatrixCanvasCommand.TotalMatrix);
                skCanvas.SetMatrix(matrix);
                break;
            }
            case SaveLayerCanvasCommand saveLayerCanvasCommand:
            {
                if (saveLayerCanvasCommand.Paint is { })
                {
                    var paint = ToSKPaint(saveLayerCanvasCommand.Paint);
                    skCanvas.SaveLayer(paint);
                }
                else
                {
                    skCanvas.SaveLayer();
                }
                break;
            }
            case DrawImageCanvasCommand drawImageCanvasCommand:
            {
                if (drawImageCanvasCommand.Image is { })
                {
                    var image = ToSKImage(drawImageCanvasCommand.Image);
                    var source = ToSKRect(drawImageCanvasCommand.Source);
                    var dest = ToSKRect(drawImageCanvasCommand.Dest);
                    var paint = ToSKPaint(drawImageCanvasCommand.Paint);
                    skCanvas.DrawImage(image, source, dest, paint);
                }
                break;
            }
            case DrawPathCanvasCommand drawPathCanvasCommand:
            {
                if (drawPathCanvasCommand.Path is { } && drawPathCanvasCommand.Paint is { })
                {
                    var path = ToSKPath(drawPathCanvasCommand.Path);
                    var paint = ToSKPaint(drawPathCanvasCommand.Paint);
                    skCanvas.DrawPath(path, paint);
                }
                break;
            }
            case DrawTextBlobCanvasCommand drawPositionedTextCanvasCommand:
            {
                if (drawPositionedTextCanvasCommand.TextBlob?.Points is { } && drawPositionedTextCanvasCommand.Paint is { })
                {
                    var text = drawPositionedTextCanvasCommand.TextBlob.Text;
                    var points = ToSKPoints(drawPositionedTextCanvasCommand.TextBlob.Points);
                    var paint = ToSKPaint(drawPositionedTextCanvasCommand.Paint);
                    var font = paint?.ToFont();
                    var textBlob = SkiaSharp.SKTextBlob.CreatePositioned(text!, font!, points);
                    skCanvas.DrawText(textBlob, 0, 0, paint);
                }
                break;
            }
            case DrawTextCanvasCommand drawTextCanvasCommand:
            {
                if (drawTextCanvasCommand.Paint is { })
                {
                    var text = drawTextCanvasCommand.Text;
                    var x = drawTextCanvasCommand.X;
                    var y = drawTextCanvasCommand.Y;
                    var paint = ToSKPaint(drawTextCanvasCommand.Paint);
                    skCanvas.DrawText(text, x, y, paint);
                }
                break;
            }
            case DrawTextOnPathCanvasCommand drawTextOnPathCanvasCommand:
            {
                if (drawTextOnPathCanvasCommand.Path is { } && drawTextOnPathCanvasCommand.Paint is { })
                {
                    var text = drawTextOnPathCanvasCommand.Text;
                    var path = ToSKPath(drawTextOnPathCanvasCommand.Path);
                    var hOffset = drawTextOnPathCanvasCommand.HOffset;
                    var vOffset = drawTextOnPathCanvasCommand.VOffset;
                    var paint = ToSKPaint(drawTextOnPathCanvasCommand.Paint);
                    skCanvas.DrawTextOnPath(text, path, hOffset, vOffset, paint);
                }
                break;
            }
        }
    }

    public void Draw(SKPicture picture, SkiaSharp.SKCanvas skCanvas)
    {
        if (picture.Commands is null)
        {
            return;
        }

        foreach (var canvasCommand in picture.Commands)
        {
            Draw(canvasCommand, skCanvas);
        }
    }
}
