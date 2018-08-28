using System;
using System.Linq;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Uno.UI.Controls;
using Windows.UI.Xaml.Input;
using Foundation;
using UIKit;
using CoreGraphics;
using Windows.UI.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock : FrameworkElement, IFontScalable
	{

		private bool _measureInvalidated;
		private Size? _previousAvailableSize;
		private Size _previousDesiredSize;
		private NSAttributedString _attributedString;
		private NSTextContainer _textContainer;
		private NSLayoutManager _layoutManager;
		private NSTextStorage _textStorage;
		private CGRect _drawRect;

		/// <summary>
		/// Determines whether the TextBlock should use a NSLayoutManager for layouting and rendering.
		/// </summary>
		/// <remarks>
		/// By default, TextBlock uses NSAttributedString for layouting and rendering.
		/// However, NSAttributedString doesn't support the following scenarios:
		/// - Finding the location of glyphs for hyperlink hit-testing
		/// - Trimming the last line of wrapping text
		/// - Limiting the number of lines in wrapping text
		/// To enable these scenarios, we must use a (more expensive) NSLayoutManager for layouting and rendering.
		/// </remarks>
		private bool UseLayoutManager =>
			HasHyperlink ||
			CanWrap && CanTrim ||
			CanWrap && MaxLines != 0;

		private bool CanWrap => TextWrapping != TextWrapping.NoWrap && MaxLines != 1;

		private bool CanTrim => TextTrimming != TextTrimming.None;

		private void InitializePartial()
		{
			ClipsToBounds = true;
			UserInteractionEnabled = true; // Required for Hyperlinks
			ContentMode = UIViewContentMode.Redraw;
			BackgroundColor = UIColor.Clear;
		}

		public override void Draw(CGRect rect)
		{
			_drawRect = GetDrawRect(rect);
			if (UseLayoutManager)
			{
				_layoutManager?.DrawGlyphs(new NSRange(0, (nint)_layoutManager.NumberOfGlyphs), _drawRect.Location);
			}
			else
			{
				_attributedString?.DrawString(_drawRect, NSStringDrawingOptions.UsesLineFragmentOrigin, null);
			}
		}

		private CGRect GetDrawRect(CGRect rect)
		{
			// Reduce available size by Padding
			rect.Width -= (nfloat)(Padding.Left + Padding.Right);
			rect.Height -= (nfloat)(Padding.Top + Padding.Bottom);

			// Offset drawing location by Padding
			rect.X += (nfloat)Padding.Left;
			rect.Y += (nfloat)Padding.Top;

			return rect;
		}

		/// <summary>
		/// Invalidates the last cached measure
		/// </summary>
		protected internal override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();
			SetNeedsDisplay();
			_measureInvalidated = true;
		}

		#region Layout

		protected override Size MeasureOverride(Size size)
		{
			// Size used to compare with the previous one. We don't want to use this one for the calculation.
			var ceiledNewSize = new CGSize(Math.Ceiling(size.Width), Math.Ceiling(size.Height));

			var hasSameDesiredSize =
				!_measureInvalidated
				&& _previousAvailableSize != null
				&& _previousDesiredSize.Width == ceiledNewSize.Width
				&& _previousDesiredSize.Height == ceiledNewSize.Height;

			var isSingleLineNarrower =
				!_measureInvalidated
				&& _previousAvailableSize != null
				&& _previousDesiredSize.Width <= ceiledNewSize.Width
				&& _previousDesiredSize.Height == ceiledNewSize.Height;

			if (hasSameDesiredSize || isSingleLineNarrower)
			{
				return _previousDesiredSize;
			}
			else
			{
				_previousAvailableSize = size;
				_measureInvalidated = false;

				UpdateTypography();
				
				var horizontalPadding = Padding.Left + Padding.Right;
				var verticalPadding = Padding.Top + Padding.Bottom;

				// available size considering padding
				size.Width -= horizontalPadding;
				size.Height -= verticalPadding;

				var result = LayoutTypography(size);

				if (result.Height == 0) // this can happen when Text is null or empty
				{
					// This measures the height correctly, even if the Text is null or empty
					// This matches Windows where empty TextBlocks still have a height (especially useful when measuring ListView items with no DataContext)
					var font = UIFontHelper.TryGetFont((float)FontSize, FontWeight, FontStyle, FontFamily);
					result = (Text ?? NSString.Empty).StringSize(font, size);
				}

				result.Width += horizontalPadding;
				result.Height += verticalPadding;

				return _previousDesiredSize = new CGSize(Math.Ceiling(result.Width), Math.Ceiling(result.Height));
			}
		}

		protected override Size ArrangeOverride(Size size)
		{
			var horizontalPadding = Padding.Left + Padding.Right;
			var verticalPadding = Padding.Top + Padding.Bottom;

			// final size considering padding
			size.Width -= horizontalPadding;
			size.Height -= verticalPadding;

			var result = LayoutTypography(size);

			result.Width += horizontalPadding;
			result.Height += verticalPadding;

			return new CGSize(Math.Ceiling(result.Width), Math.Ceiling(result.Height));
		}

		#endregion

		#region Update

		private int GetLines()
		{
			return TextWrapping == TextWrapping.NoWrap
				? 1
				: MaxLines;
		}

		private UILineBreakMode GetLineBreakMode()
		{
			if (TextTrimming == TextTrimming.CharacterEllipsis || TextTrimming == TextTrimming.WordEllipsis)
			{
				return UILineBreakMode.TailTruncation;
			}
			else if (TextWrapping == TextWrapping.NoWrap || MaxLines == 1)
			{
				return UILineBreakMode.Clip;
			}
			else
			{
				return UILineBreakMode.WordWrap;
			}
		}

		private NSAttributedString GetAttributedText()
		{
			var mutableAttributedString = new NSMutableAttributedString(Text);
			mutableAttributedString.BeginEditing();

			mutableAttributedString.AddAttributes(GetAttributes(), new NSRange(0, mutableAttributedString.Length));

			// Apply Inlines
			foreach (var inline in GetEffectiveInlines())
			{
				mutableAttributedString.AddAttributes(inline.inline.GetAttributes(), new NSRange(inline.start, inline.end - inline.start));
			}

			mutableAttributedString.EndEditing();
			return mutableAttributedString;
		}

		private UIStringAttributes GetAttributes()
		{
			var attributes = new UIStringAttributes();

			var font = UIFontHelper.TryGetFont((float)FontSize, FontWeight, FontStyle, FontFamily);

			attributes.Font = font;
			attributes.ForegroundColor = (Foreground as SolidColorBrush)?.ColorWithOpacity;

			if (TextDecorations != TextDecorations.None)
			{
				attributes.UnderlineStyle = (TextDecorations & TextDecorations.Underline) == TextDecorations.Underline
					? NSUnderlineStyle.Single
					: NSUnderlineStyle.None;

				attributes.StrikethroughStyle = (TextDecorations & TextDecorations.Strikethrough) == TextDecorations.Strikethrough
					? NSUnderlineStyle.Single
					: NSUnderlineStyle.None;
			}

			var paragraphStyle = new NSMutableParagraphStyle()
			{
				MinimumLineHeight = (nfloat)LineHeight,
				Alignment = TextAlignment.ToNativeTextAlignment(),
				LineBreakMode = GetLineBreakMode(),
			};

			// For unknown reasons, the LineBreakMode must be set to WordWrap
			// when applied to a NSTextStorage for text to wrap.
			if (UseLayoutManager)
			{
				paragraphStyle.LineBreakMode = UILineBreakMode.WordWrap;
			}

			if (LineStackingStrategy != LineStackingStrategy.MaxHeight)
			{
				paragraphStyle.MaximumLineHeight = (nfloat)LineHeight;
			}
			attributes.ParagraphStyle = paragraphStyle;

			if (LineHeight != 0 && font != null)
			{
				// iOS puts text at the bottom of the line box, whereas Windows puts it at the top. 
				// Empirically this offset gives similar positioning to Windows. 
				// Note: Descender is typically a negative value.
				var verticalOffset = LineHeight - font.LineHeight + font.Descender;

				// Because we're trying to move the text up (toward the top of the line box), 
				// we only set BaselineOffset to a positive value. 
				// A negative value indicates that the the text is already bottom-aligned.
				attributes.BaselineOffset = Math.Max(0, (float)verticalOffset);
			}

			if (CharacterSpacing != 0)
			{
				//CharacterSpacing is in 1/1000 of an em, iOS KerningAdjustment is in points. 1 em = 12 points
				attributes.KerningAdjustment = (CharacterSpacing / 1000f) * 12;
			}

			return attributes;
		}

		#endregion

		#region IFontScalable

		public void RefreshFont()
		{
			this.InvalidateMeasure();
		}

		#endregion

		#region Hyperlinks

		internal override bool IsViewHit()
		{
			return true;
		}

		#endregion

		private void UpdateTypography()
		{
			_attributedString = GetAttributedText();

			if (UseLayoutManager)
			{
				// Configure textContainer
				_textContainer = new NSTextContainer();
				_textContainer.LineFragmentPadding = 0;
				_textContainer.LineBreakMode = GetLineBreakMode();
				_textContainer.MaximumNumberOfLines = (nuint)GetLines();
				
				// Configure layoutManager
				_layoutManager = new NSLayoutManager();
				_layoutManager.AddTextContainer(_textContainer);

				// Configure textStorage
				_textStorage = new NSTextStorage();
				_textStorage.AddLayoutManager(_layoutManager);
				_textStorage.SetString(_attributedString);
			}
		}

		private Size LayoutTypography(Size size)
		{
			if (UseLayoutManager)
			{
				_textContainer.Size = size;
				return _layoutManager.GetUsedRectForTextContainer(_textContainer).Size;
			}
			else
			{
				return _attributedString.GetBoundingRect(size, NSStringDrawingOptions.UsesLineFragmentOrigin, null).Size;
			}
		}

		private int GetCharacterIndexAtPoint(Point point)
		{
			if (!_drawRect.Contains(point))
			{
				return -1;
			}

			// Find the tapped character's index
			var partialFraction = (nfloat)0;
			var pointInTextContainer = new CGPoint(point.X - _drawRect.X, point.Y - _drawRect.Y);
			var characterIndex = (int)_layoutManager.CharacterIndexForPoint(pointInTextContainer, _layoutManager.TextContainers.FirstOrDefault(), ref partialFraction);

			return characterIndex;
		}
	}
}
