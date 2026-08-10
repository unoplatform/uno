using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using HarfBuzzSharp;
using SkiaSharp;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Documents
{
	public abstract partial class Inline : TextElement
	{
		internal void InvalidateInlines(bool updateText)
		{
#if !IS_UNIT_TESTS
			switch (this.GetParent())
			{
				case Span span:
					span.InvalidateInlines(updateText);
					break;
				case TextBlock textBlock:
					textBlock.InvalidateInlines(updateText);
					break;
				default:
					break;
			}
#endif
		}

#nullable enable
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
#nullable disable

#if __NETSTD_REFERENCE__
		protected override void OnFontFamilyChanged() => base.OnFontFamilyChanged();

		protected override void OnFontStyleChanged() => base.OnFontStyleChanged();

		protected override void OnFontWeightChanged() => base.OnFontWeightChanged();

		protected override void OnFontSizeChanged() => base.OnFontSizeChanged();

		protected override void OnFontStretchChanged() => base.OnFontStretchChanged();
#endif
	}
}
