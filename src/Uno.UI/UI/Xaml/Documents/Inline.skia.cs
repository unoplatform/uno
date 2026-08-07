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
					var scaledSize = Uno.UI.Xaml.Core.TextScaleHelper.GetScaledFontSize(FontSize, Uno.UI.Xaml.Core.CoreServices.Instance.FontScale, IsTextScaleFactorEnabled && !Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor);
					var (details, task) = FontDetailsCache.GetFont(FontFamily?.Source, (float)scaledSize, FontWeight, FontStretch, FontStyle);
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
			InvalidateInlinesForFormatChange(FontFamilyProperty);
			InvalidateFontInfo();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlinesForFormatChange(FontStyleProperty);
			InvalidateFontInfo();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlinesForFormatChange(FontStretchProperty);
			InvalidateFontInfo();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlinesForFormatChange(FontWeightProperty);
			InvalidateFontInfo();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlinesForFormatChange(FontSizeProperty);
			InvalidateFontInfo();
		}

		private void InvalidateFontInfo() => _fontInfo = null;

		private protected override void OnIsTextScaleFactorEnabledChanged()
		{
			base.OnIsTextScaleFactorEnabledChanged();
			InvalidateFontInfo();
			InvalidateInlinesForFormatChange(IsTextScaleFactorEnabledProperty);
		}

		/// <summary>
		/// Invalidates the cached font info due to a text scale factor change.
		/// Called from CoreServices.RecursiveInvalidateTextScale().
		/// </summary>
		internal virtual void InvalidateTextScaleFontInfo() => _fontInfo = null;
	}
}
