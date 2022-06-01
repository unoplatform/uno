using System;
using System.Collections.Generic;
using System.Text;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;

#nullable enable

namespace Windows.UI.Xaml.Documents
{
	partial class Inline
	{
		// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
		// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
		private const int FontScale = 512;

		private static readonly Func<string?, FontWeight, FontStretch, FontStyle, (SKTypeface, Font)> _getFont =
			Funcs.CreateMemoized<string?, FontWeight, FontStretch, FontStyle, (SKTypeface, Font)>(
				(nm, wt, wh, sl) => GetFont(nm, wt, wh, sl));

		private (SKTypeface Typeface, Font Font)? _fontInfo;
		private SKPaint? _paint;

		internal SKPaint Paint
		{
			get
			{
				var paint = _paint ??= new SKPaint
				{
					TextEncoding = SKTextEncoding.Utf16,
					IsStroke = false,
					IsAntialias = true,
					LcdRenderText = true,
					SubpixelText = true,
				};

				paint.Typeface = FontInfo.Typeface;
				paint.TextSize = (float)FontSize;

				return paint;
			}
		}

		internal (SKTypeface Typeface, Font Font) FontInfo => _fontInfo ??= _getFont(FontFamily?.Source, FontWeight, FontStretch, FontStyle);

		internal float LineHeight
		{
			get
			{
				var metrics = Paint.FontMetrics;
				return metrics.Descent - metrics.Ascent;
			}
		}

		internal float AboveBaselineHeight => -Paint.FontMetrics.Ascent;

		internal float BelowBaselineHeight => Paint.FontMetrics.Descent;

		private static (SKTypeface Typeface, Font Font) GetFont(
			string? name,
			FontWeight weight,
			FontStretch stretch,
			FontStyle style)
		{
			var skWeight = weight.ToSkiaWeight();
			// TODO: FontStretch not supported by Uno yet
			// var skWidth = FontStretch.ToSkiaWidth();
			var skWidth = SKFontStyleWidth.Normal;
			var skSlant = style.ToSkiaSlant();

			SKTypeface skTypeFace;

			if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
			{
				skTypeFace = SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant);
			}
			else if (name.StartsWith(XamlFilePathHelper.AppXIdentifier))
			{
				var path = new Uri(name).PathAndQuery;

				var filePath = global::System.IO.Path.Combine(Windows.Application­Model.Package.Current.Installed­Location.Path, path.TrimStart('/')
					.Replace('/', global::System.IO.Path.DirectorySeparatorChar));

				skTypeFace = SKTypeface.FromFile(filePath);
			}
			else
			{
				skTypeFace = SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);

				// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
				if (skTypeFace == null)
				{
					if (typeof(Run).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(Run).Log().LogWarning($"The font {name} could not be found, using system default");
					}

					skTypeFace = SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant);
				}
			}

			using (var hbBlob = skTypeFace.OpenStream(out int index).ToHarfBuzzBlob())
			using (var hbFace = new Face(hbBlob, index))
			{
				hbFace.Index = index;
				hbFace.UnitsPerEm = skTypeFace.UnitsPerEm;

				var hbFont = new Font(hbFace);
				hbFont.SetScale(FontScale, FontScale);
				hbFont.SetFunctionsOpenType();

				return (skTypeFace, hbFont);
			}
		}
	}
}
