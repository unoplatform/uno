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

		partial void InvalidateTextBlockPartial()
		{

		}

		protected override Size MeasureOverride(Size availableSize)
		{
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

		partial void OnTextDecorationsChangedPartial()
		{
			this.SetTextDecorations(TextDecorations);
		}

		partial void OnTextWrappingChangedPartial()
		{
			this.SetTextWrapping(TextWrapping);
		}

		partial void OnPaddingChangedPartial()
		{
			this.SetTextPadding(Padding);
		}

		internal override bool IsViewHit()
		{
			return Text != null || base.IsViewHit();
		}
	}
}
