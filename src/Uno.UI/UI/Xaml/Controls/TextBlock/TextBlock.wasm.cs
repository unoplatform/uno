using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation;
using System.Linq;

using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement
	{
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

		private bool _shouldUpdateIsTextTrimmed;

		public TextBlock() : base("p")
		{
			SetDefaultForeground(ForegroundProperty);

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
			InitializeDefaultValues();

		}

		/// <summary>
		/// Set default properties to vertical top.
		/// In wasm, this behavior is closer to the default textblock property than stretch.
		/// </summary>
		private void InitializeDefaultValues()
		{
			this.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top, DependencyPropertyValuePrecedences.DefaultValue);
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

			if (_textTrimmingChanged || _textWrappingChanged)
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
				if (TextBlockMeasureCache.Instance.FindMeasuredSize(this, availableSize) is Size desiredSize)
				{
					UnoMetrics.TextBlock.MeasureCacheHits++;
					return desiredSize;
				}
				else
				{
					UnoMetrics.TextBlock.MeasureCacheMisses++;
					desiredSize = MeasureView(availableSize);

					TextBlockMeasureCache.Instance.CacheMeasure(this, availableSize, desiredSize);

					return desiredSize;
				}
			}
			else
			{
				var desizedSize = MeasureView(availableSize);

				return desizedSize;
			}
		}

		/// <summary>
		/// When the control is constrained, it should only take it's desired size or
		/// it will show all of it's content.
		/// </summary>
		/// <param name="finalSize"></param>
		/// <returns></returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size arrangeSize;
			if (IsLayoutConstrainedByMaxLines)
			{
				arrangeSize = DesiredSize;

				if (HorizontalAlignment == HorizontalAlignment.Stretch)
				{
					arrangeSize.Width = finalSize.Width;
				}
			}
			else
			{
				arrangeSize = finalSize;
			}

			if (Foreground is GradientBrush or RadialGradientBrush)
			{
				// Make sure to always re-set the foreground when the size is changed.
				this.SetForeground(Foreground);
			}

			return base.ArrangeOverride(arrangeSize);
		}

		internal override void AfterArrange()
		{
			base.AfterArrange();

			if (_shouldUpdateIsTextTrimmed)
			{
				UpdateIsTextTrimmed();
			}
		}

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

		partial void UpdateIsTextTrimmed()
		{
			IsTextTrimmed =
				IsTextTrimmable &&
				WindowManagerInterop.GetIsOverflowing(HtmlId);
		}
	}
}
