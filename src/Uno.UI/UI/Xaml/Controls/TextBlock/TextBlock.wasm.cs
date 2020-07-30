using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation;
using System.Linq;
using Uno.UI.UI.Xaml.Documents;
using Microsoft.Extensions.Logging;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement
	{
		private const int MaxMeasureCache = 50;

		private static TextBlockMeasureCache _cache = new TextBlockMeasureCache();
		private bool _fontStyleChanged;
		private bool _fontWeightChanged;
		private bool _textChanged;
		private bool _fontFamilyChanged;
		private bool _fontSizeChanged;
		private bool _maxLinesChanged;
		private bool _textTrimmingChanged;
		private bool _textAlignmentChanged;
		private bool _lineHeightChanged;
		private bool _characterSpacingChanged;
		private bool _textDecorationsChanged;
		private bool _textWrappingChanged;
		private bool _paddingChangedChanged;

		public TextBlock() : base("p")
		{
			OnFontStyleChangedPartial();
			OnFontWeightChangedPartial();
			OnTextChangedPartial();
			OnFontFamilyChangedPartial();
			OnFontSizeChangedPartial();
			OnCharacterSpacingChangedPartial();
			OnLineHeightChangedPartial();
			OnTextAlignmentChangedPartial();
			OnTextWrappingChangedPartial();
			OnIsTextSelectionEnabledChangedPartial();
		}

		private void ConditionalUpdate(ref bool condition, Action action)
		{
			if (condition)
			{
				condition = false;
				action();
			}
		}

		private void SynchronizeHtmlParagraphAttributes()
		{
			ConditionalUpdate(ref _fontStyleChanged, () => this.SetFontStyle(FontStyle));
			ConditionalUpdate(ref _fontWeightChanged, () => this.SetFontWeight(FontWeight));
			ConditionalUpdate(ref _fontFamilyChanged, () => this.SetFontFamily(FontFamily));
			ConditionalUpdate(ref _fontSizeChanged, () => this.SetFontSize(FontSize));
			ConditionalUpdate(ref _maxLinesChanged, () => this.SetMaxLines(MaxLines));
			ConditionalUpdate(ref _textAlignmentChanged, () => this.SetTextAlignment(TextAlignment));
			ConditionalUpdate(ref _lineHeightChanged, () => this.SetLineHeight(LineHeight));
			ConditionalUpdate(ref _characterSpacingChanged, () => this.SetCharacterSpacing(CharacterSpacing));
			ConditionalUpdate(ref _textDecorationsChanged, () => this.SetTextDecorations(TextDecorations));
			ConditionalUpdate(ref _paddingChangedChanged, () => this.SetTextPadding(Padding));

			if(_textTrimmingChanged || _textWrappingChanged)
			{
				_textTrimmingChanged = _textWrappingChanged = false;
				this.SetTextWrappingAndTrimming(textTrimming: TextTrimming, textWrapping: TextWrapping);
			}

			if (_textChanged)
			{
				_textChanged = false;

				if (UseInlinesFastPath)
				{
					this.SetText(Text);
				}
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			SynchronizeHtmlParagraphAttributes();

			if (UseInlinesFastPath)
			{
				if (_cache.FindMeasuredSize(this, availableSize) is Size desiredSize)
				{
					UnoMetrics.TextBlock.MeasureCacheHits++;
					return desiredSize;
				}
				else
				{
					UnoMetrics.TextBlock.MeasureCacheMisses++;
					desiredSize = MeasureView(availableSize);

					_cache.CacheMeasure(this, availableSize, desiredSize);

					return desiredSize;
				}
			}
			else
			{
				var desizedSize = MeasureView(availableSize);

				return desizedSize;
			}
		}

		private int GetCharacterIndexAtPoint(Point point) => throw new NotSupportedException();

		partial void OnFontStyleChangedPartial() => _fontStyleChanged = true;

		partial void OnFontWeightChangedPartial() => _fontWeightChanged = true;

		partial void OnIsTextSelectionEnabledChangedPartial()
		{
			if (IsTextSelectionEnabled)
			{
				SetCssClasses("selectionEnabled");
			}
			else
			{
				UnsetCssClasses("selectionEnabled");
			}
		}

		partial void OnTextChangedPartial()
		{
			_textChanged = true;

			UpdateHitTest();
		}

		partial void ClearTextPartial() => SetHtmlContent("");

		partial void OnFontFamilyChangedPartial() => _fontFamilyChanged = true;

		partial void OnFontSizeChangedPartial() => _fontSizeChanged = true;

		partial void OnMaxLinesChangedPartial() => _maxLinesChanged = true;

		partial void OnTextTrimmingChangedPartial() => _textTrimmingChanged = true;

		partial void OnForegroundChangedPartial() => this.SetForeground(Foreground);

		partial void OnTextAlignmentChangedPartial() => _textAlignmentChanged = true;

		partial void OnLineHeightChangedPartial() => _lineHeightChanged = true;

		partial void OnCharacterSpacingChangedPartial() => _characterSpacingChanged = true;

		partial void OnTextDecorationsChangedPartial() => _textDecorationsChanged = true;

		partial void OnTextWrappingChangedPartial() => _textWrappingChanged = true;

		partial void OnPaddingChangedPartial() => _paddingChangedChanged = true;

		internal override bool IsViewHit() => Text != null || base.IsViewHit();
	}
}
