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
		internal void InvalidateInlines(bool updateText) => InvalidateInlines(updateText, inherited: false);

		internal void InvalidateInlines(bool updateText, bool inherited)
		{
#if !IS_UNIT_TESTS
			switch (this.GetParent())
			{
				case Span span:
					span.InvalidateInlines(updateText, inherited);
					break;
				case TextBlock textBlock:
					textBlock.InvalidateInlines(updateText);
					break;
				case Block block:
					block.InvalidateInlines(contentChanged: !inherited);
					break;
				default:
					break;
			}
#endif
		}

		// CUIElement::MarkInheritedPropertyDirty walks only GetChildren(), so an inherited formatting change
		// on the owning RichTextBlock never reaches CTextElement::MarkDirty -> CRichTextBlock::OnContentChanged;
		// the owner just runs its own InvalidateContent. A locally set value does go through MarkDirty, and
		// must keep clearing the selection and the cached focusable children.
		private protected void InvalidateInlinesForFormatChange(DependencyProperty property)
			=> InvalidateInlines(
				updateText: false,
				inherited: this.GetCurrentHighestValuePrecedence(property) == DependencyPropertyValuePrecedences.Inheritance);

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
#nullable disable

	}
}
