using HarfBuzzSharp;
using SkiaSharp;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents.TextFormatting;

#nullable enable

namespace Windows.UI.Xaml.Documents
{
	partial class Inline
	{
		private FontDetails? _fontInfo;
		private SKPaint? _paint;

		internal SKPaint Paint
		{
			get
			{
				var paint = _paint ??= new SKPaint()
				{
					TextEncoding = SKTextEncoding.Utf16,
					IsStroke = false,
					IsAntialias = true,
				};

				return paint;
			}
		}

		internal FontDetails FontInfo => _fontInfo ??= FontDetailsCache.GetFont(FontFamily?.Source, (float)FontSize, FontWeight, FontStyle);

		internal float LineHeight => FontInfo.LineHeight;

		internal float AboveBaselineHeight => -FontInfo.SKFontMetrics.Ascent;

		internal float BelowBaselineHeight => FontInfo.SKFontMetrics.Descent;

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

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateFontInfo();
		}

		private void InvalidateFontInfo() => _fontInfo = null;
	}
}
