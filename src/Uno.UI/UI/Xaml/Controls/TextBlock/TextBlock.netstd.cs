using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation;
using System.Linq;
using Uno.UI.UI.Xaml.Documents;

namespace Windows.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement
	{
		public TextBlock() : base("p")
		{
			OnFontStyleChangedPartial();
			OnFontWeightChangedPartial();
			OnTextChangedPartial();
			OnFontFamilyChangedPartial();
			OnCharacterSpacingChangedPartial();
			OnLineHeightChangedPartial();
			OnTextAlignmentChangedPartial();
			OnTextWrappingChangedPartial();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = MeasureView(availableSize);
			size.Width += 1.0; // Necessary because MeasureView seems to return rounded values.
			return size;
		}

		private int GetCharacterIndexAtPoint(Point point)
		{
			throw new NotSupportedException();
		}

		partial void OnFontStyleChangedPartial()
		{
			this.SetFontStyle(FontStyle);
		}

		partial void OnFontWeightChangedPartial()
		{
			this.SetFontWeight(FontWeight);
		}

		partial void OnTextChangedPartial()
		{
			if (UseInlinesFastPath)
			{
				this.SetText(Text);
			}

			UpdateHitTest();
		}

		partial void ClearTextPartial()
		{
			SetHtmlContent(""); // Remove any child element
		}

		partial void OnFontFamilyChangedPartial()
		{
			this.SetFontFamily(FontFamily);
		}

		partial void OnFontSizeChangedPartial()
		{
			this.SetFontSize(FontSize);
		}

		partial void OnMaxLinesChangedPartial()
		{
			this.SetMaxLines(MaxLines);
		}

		partial void OnTextTrimmingChangedPartial()
		{
			this.SetTextTrimming(TextTrimming);
		}

		partial void OnForegroundChangedPartial()
		{
			this.SetForeground(Foreground);
		}

		partial void OnTextAlignmentChangedPartial()
		{
			this.SetTextAlignment(TextAlignment);
		}

		partial void OnLineHeightChangedPartial()
		{
			this.SetLineHeight(LineHeight);
		}

		partial void OnCharacterSpacingChangedPartial()
		{
			this.SetCharacterSpacing(CharacterSpacing);
		}

		partial void OnTextWrappingChangedPartial()
		{
			this.SetTextWrapping(TextWrapping);
		}

		internal override bool IsViewHit()
		{
			return Text != null || base.IsViewHit();
		}
	}
}
