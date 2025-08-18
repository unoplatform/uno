using System;
using HarfBuzzSharp;
using SkiaSharp;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.UI.Dispatching;

#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Inline
	{
		private FontDetails? _fontInfo;

		internal FontDetails FontInfo
		{
			get
			{
				if (_fontInfo is null)
				{
					var (details, task) = FontDetailsCache.GetFont(FontFamily?.Source, (float)FontSize, FontWeight, FontStretch, FontStyle);
					if (task.IsCompletedSuccessfully)
					{
						_fontInfo = task.Result;
					}
					else
					{
						task.ContinueWith(_ =>
						{
							NativeDispatcher.Main.Enqueue(OnFontLoaded);
						});
						_fontInfo = details;
					}
				}

				return _fontInfo;
			}
		}

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

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
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
