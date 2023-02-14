using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;

#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Inline
	{
		// TODO: Investigate best value to use here. SKShaper uses a constant 512 scale, Avalonia uses default font scale. Not 100% sure how much difference it
		// makes here but it affects subpixel rendering accuracy. Performance does not seem to be affected by changing this value.
		private const int FontScale = 512;

		private static readonly Func<string?, FontWeight, FontStretch, FontStyle, FontDetails> _getFont =
			Funcs.CreateMemoized<string?, FontWeight, FontStretch, FontStyle, FontDetails>(
				(nm, wt, wh, sl) => GetFont(nm, wt, wh, sl));

		private FontDetails? _fontInfo;
		private SKPaint? _paint;

		internal record FontDetails(SKTypeface Typeface, Font Font, Face Face);

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

		internal FontDetails FontInfo => _fontInfo ??= _getFont(FontFamily?.Source, FontWeight, FontStretch, FontStyle);

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

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateFontInfo();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateFontInfo();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateFontInfo();
		}

		private void InvalidateFontInfo() => _fontInfo = null;

		private static FontDetails GetFont(
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

			SKTypeface? skTypeFace;

			SKTypeface GetDefaultTypeFace()
				=> SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant);

			if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
			{
				skTypeFace = GetDefaultTypeFace();
			}
			else if (XamlFilePathHelper.TryGetMsAppxAssetPath(name, out var path))
			{
				var filePath = global::System.IO.Path.Combine(
					Windows.ApplicationModel.Package.Current.InstalledLocation.Path
					, path.Replace('/', global::System.IO.Path.DirectorySeparatorChar));

				// SKTypeface.FromFile may return null if the file is not found (SkiaSharp is not yet nullable attributed)
				skTypeFace = SKTypeface.FromFile(filePath);
			}
			else
			{
				// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
				skTypeFace = SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
			}

			if (skTypeFace == null)
			{
				if (typeof(Inline).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(Inline).Log().LogWarning($"The font {name} could not be found, using system default");
				}

				skTypeFace = GetDefaultTypeFace();
			}

			Blob? GetTable(Face face, Tag tag)
			{
				var size = skTypeFace.GetTableSize(tag);

				if (size == 0)
				{
					return null;
				}

				var data = Marshal.AllocCoTaskMem(size);

				var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeCoTaskMem(data));

				var value = skTypeFace.TryGetTableData(tag, 0, size, data) ?
					new Blob(data, size, MemoryMode.Writeable, releaseDelegate) : null;

				return value;
			}

			var hbFace = new Face(GetTable);

			hbFace.UnitsPerEm = skTypeFace.UnitsPerEm;

			var hbFont = new Font(hbFace);
			hbFont.SetScale(FontScale, FontScale);
			hbFont.SetFunctionsOpenType();

			return new(skTypeFace, hbFont, hbFace);
		}

		/// <summary>
		/// Apply a workaround for https://github.com/mono/SkiaSharp/issues/2113
		/// </summary>
		public static void ApplyHarfbuzzWorkaround()
		{
			// HarfBuzzSharp.Font.SetFunctionsOpenType() needs to be initialized before GtkSharp is initialized
			// to avoid libHarfBuzzSharp's own static library to hook onto
			// the system implementation of libHarfBuzzSharp.
			var font = GetFont(null, FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);

			using HarfBuzzSharp.Buffer buffer = new();
			buffer.ContentType = ContentType.Unicode;
			buffer.GuessSegmentProperties();

			// Force a font loading by shaping a buffer.
			font.Font.Shape(buffer);
		}
	}
}
