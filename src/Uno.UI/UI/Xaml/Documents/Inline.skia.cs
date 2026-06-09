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
				// Invalidate cached font details when the TextFormatting generation counter
				// advances (e.g., a parent's FontFamily/FontSize changed).
				// MUX ref: WinUI stores font properties in the TextFormatting storage group,
				// so any read of a font property goes through EnsureTextFormatting.
				// In Uno/Skia, we mirror that by invalidating the cache on staleness.
				if (_fontInfo is not null && (_textFormatting?.IsOld ?? false))
				{
					InvalidateFontInfo();
				}

				if (_fontInfo is null)
				{
					// Ensure TextFormatting is up to date to resolve inherited font values.
					// This is needed because TextBlock/Control font DPs no longer use the
					// DP Inherits flag — inheritance is handled via the TextFormatting system.
					EnsureTextFormatting(null, forGetValue: true);

					// Use effective values: for unset properties, use the inherited value
					// from TextFormatting; for locally set properties, use the DP value.
					var fontFamily = IsPropertyDefault(FontFamilyProperty)
						? (_textFormatting?.FontFamily ?? FontFamily)
						: FontFamily;
					var fontSize = IsPropertyDefault(FontSizeProperty)
						? (_textFormatting?.FontSize ?? FontSize)
						: FontSize;
					var fontWeight = IsPropertyDefault(FontWeightProperty)
						? (_textFormatting?.FontWeight ?? FontWeight)
						: FontWeight;
					var fontStretch = IsPropertyDefault(FontStretchProperty)
						? (_textFormatting?.FontStretch ?? FontStretch)
						: FontStretch;
					var fontStyle = IsPropertyDefault(FontStyleProperty)
						? (_textFormatting?.FontStyle ?? FontStyle)
						: FontStyle;

					// Apply the text scale factor to the resolved (effective) font size.
					var scaledSize = Uno.UI.Xaml.Core.TextScaleHelper.GetScaledFontSize(fontSize, Uno.UI.Xaml.Core.CoreServices.Instance.FontScale, IsTextScaleFactorEnabled && !Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor);
					var (details, task) = FontDetailsCache.GetFont(fontFamily?.Source, (float)scaledSize, fontWeight, fontStretch, fontStyle);
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
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlines(false);
			InvalidateFontInfo();
		}

		private void InvalidateFontInfo() => _fontInfo = null;

		private protected override void OnIsTextScaleFactorEnabledChanged()
		{
			base.OnIsTextScaleFactorEnabledChanged();
			InvalidateFontInfo();
			InvalidateInlines(false);
		}

		/// <summary>
		/// Invalidates the cached font info due to a text scale factor change.
		/// Called from CoreServices.RecursiveInvalidateTextScale().
		/// </summary>
		internal virtual void InvalidateTextScaleFontInfo() => _fontInfo = null;
	}
}
