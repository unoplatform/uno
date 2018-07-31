using System;
using System.Linq;
using Uno.UI.Views.Controls;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Uno.Disposables;
using System.Collections.Generic;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Uno.UI.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
using CoreText;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreText;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock : UILabel, IFrameworkElement, IFontScalable
	{
		/// <summary>
		/// Indicates the kind of target on which an NSAttributedString will be applied.
		/// </summary>
		/// <remarks>
		/// The same NSAttributedString will produce different results
		/// when applied to different targets (e.g., LineBreakMode, BaselineOffset).
		/// </remarks>
		private enum NSAttributedStringTarget
		{
			UILabel,
			NSTextStorage,
		}

		private bool _measureInvalidated;
		private CGSize? _previousAvailableSize;
		private CGSize _previousDesiredSize;
		private NSLayoutManager _layoutManager;
		private CGRect _drawRect;

		private UIFont _currentFont;
		private Color? _currentColor;

		private void InitializePartial()
		{
			InitializeBinder();

			ClipsToBounds = true;
			UserInteractionEnabled = true; // Required for Hyperlinks
		}

		public override void DrawText(CGRect rect)
		{
			_drawRect = GetDrawRect(rect);
			base.DrawText(_drawRect);

			if (FeatureConfiguration.TextBlock.ShowHyperlinkLayouts)
			{
				UpdateHyperlinkLayout();
				_layoutManager?.DrawGlyphs(new NSRange(0, Text.Length), _drawRect.Location);
			}
		}

		private CGRect GetDrawRect(CGRect rect)
		{
			// Reduce available size by Padding
			rect.Width -= (nfloat)(Padding.Left + Padding.Right);
			rect.Height -= (nfloat)(Padding.Top + Padding.Bottom);

			// On Windows, text is vertically top-aligned by default.
			// On iOS, text is vertically center-aligned by default.
			// http://stackoverflow.com/questions/1054558/vertically-align-text-within-a-uilabel
			// This ensures the text is drawn to be vertically top-aligned:
			rect = TextRectForBounds(rect, Lines);
			rect.Y = 0;

			// Offset drawing location by Padding
			rect.X += (nfloat)Padding.Left;
			rect.Y += (nfloat)Padding.Top;

			return rect;
		}

		/// <summary>
		/// Invalidates the last cached measure
		/// </summary>
		partial void SetNeedsLayoutPartial()
		{
			_measureInvalidated = true;
		}

		#region Layout

		public override CGSize SizeThatFits(CGSize size)
		{
			var hasSameDesiredSize =
				!_measureInvalidated
				&& _previousAvailableSize != null
				&& _previousDesiredSize.Width == size.Width
				&& _previousDesiredSize.Height == size.Height;

			var isSingleLineNarrower =
				!_measureInvalidated
				&& _previousAvailableSize != null
				&& _previousDesiredSize.Width <= size.Width
				&& _previousDesiredSize.Height == size.Height;

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

				// available size considering min/max width/height
				// necessary, because the height of a wrapping TextBlock depends on its width
				size = IFrameworkElementHelper.SizeThatFits(this, size);

				// available size considering padding
				size.Width -= (nfloat)horizontalPadding;
				size.Height -= (nfloat)verticalPadding;

				var result = base.SizeThatFits(size);

				if (result.Height == 0) // this can happen when Text is null or empty
				{
					// This measures the height correctly, even if the Text is null or empty
					// This matches Windows where empty TextBlocks still have a height (especially useful when measuring ListView items with no DataContext)
					result = (Text ?? NSString.Empty).StringSize(this.Font, size);
				}

				if (AdjustsFontSizeToFitWidth && !nfloat.IsNaN(size.Width))
				{
					result.Width = size.Width;
				}

				result.Width += (nfloat)horizontalPadding;
				result.Height += (nfloat)verticalPadding;

				result = IFrameworkElementHelper.SizeThatFits(this, result);
				return _previousDesiredSize = new CGSize(Math.Ceiling(result.Width), Math.Ceiling(result.Height));
			}
		}

		#endregion

		#region Update

		private void UpdateTypography()
		{
			UpdateWrapAndTrim();
			UpdateFont();
			UpdateTextColor();
			UpdateTextAlignment();
			UpdateText();
		}

		private void UpdateTextColor()
		{
			var color = (Foreground as SolidColorBrush)?.ColorWithOpacity;

			if (_currentColor != color)
			{
				// Local cache is used to avoid the interop if the color has not changed.
				_currentColor = color;
				TextColor = color;
			}
		}

		private void UpdateTextAlignment()
		{
			base.TextAlignment = TextAlignment.ToNativeTextAlignment();
		}

		private void UpdateWrapAndTrim()
		{
			Lines = GetLines();
			LineBreakMode = GetLineBreakMode();
		}

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
			else if (TextWrapping == TextWrapping.NoWrap)
			{
				return UILineBreakMode.Clip;
			}
			else
			{
				return UILineBreakMode.WordWrap;
			}
		}

		private void UpdateFont()
		{
			var newFont = UIFontHelper.TryGetFont((float)FontSize, FontWeight, FontStyle, FontFamily);
			if (newFont != null && !ReferenceEquals(_currentFont, newFont))
			{
				// Local cache is used to avoid the interop if the font has not changed.
				_currentFont = newFont;
				base.Font = newFont;
			}
		}

		private void UpdateText()
		{
			var attributedText = GetAttributedText(NSAttributedStringTarget.UILabel);
			if (attributedText != null)
			{
				AttributedText = attributedText;
			}
			else
			{
				base.Text = Text;
			}
		}

		private NSAttributedString GetAttributedText(NSAttributedStringTarget target)
		{
			if (target == NSAttributedStringTarget.UILabel && UseInlinesFastPath && LineHeight == 0 && CharacterSpacing == 0)
			{
				// Fast path in case everything can be set directly on UILabel without using NSAttributedString.
				return null;
			}
			else
			{
				var mutableAttributedString = new NSMutableAttributedString(Text);
				mutableAttributedString.BeginEditing();

				mutableAttributedString.AddAttributes(GetAttributes(target), new NSRange(0, mutableAttributedString.Length));

				// Apply Inlines
				foreach (var inline in GetEffectiveInlines())
				{
					mutableAttributedString.AddAttributes(inline.inline.GetAttributes(), new NSRange(inline.start, inline.end - inline.start));
				}

				mutableAttributedString.EndEditing();
				return mutableAttributedString;
			}
		}

		private UIStringAttributes GetAttributes(NSAttributedStringTarget target)
		{
			var attributes = new UIStringAttributes();

			// We only apply these values to NSTextStorage, as they're already applied to UILabel.
			// See: UpdateFont, UpdateTextColor, etc.
			if (target == NSAttributedStringTarget.NSTextStorage)
			{
				attributes.Font = _currentFont;
				attributes.ForegroundColor = _currentColor;
			}

			if (target == NSAttributedStringTarget.NSTextStorage || LineHeight != 0)
			{
				var paragraphStyle = new NSMutableParagraphStyle()
				{
					MinimumLineHeight = (nfloat)LineHeight,
					Alignment = TextAlignment.ToNativeTextAlignment(),
					LineBreakMode = GetLineBreakMode(),
				};

				// For unknown reasons, the LineBreakMode must be set to WordWrap
				// when applied to a NSTextStorage for text to wrap (but not when applied to a UILabel).
				if (target == NSAttributedStringTarget.NSTextStorage)
				{
					paragraphStyle.LineBreakMode = UILineBreakMode.WordWrap;
				}

				if (LineStackingStrategy != LineStackingStrategy.MaxHeight)
				{
					paragraphStyle.MaximumLineHeight = (nfloat)LineHeight;
				}
				attributes.ParagraphStyle = paragraphStyle;

				if (Font != null)
				{
					// iOS puts text at the bottom of the line box, whereas Windows puts it at the top. 
					// Empirically this offset gives similar positioning to Windows. 
					// Note: Descender is typically a negative value.
					var verticalOffset = LineHeight - Font.LineHeight + Font.Descender;

					// For unknown reasons, the verticalOffset must be divided by 2 
					// when applied to a UILabel (but not when applied to a NSTextStorage).
					if (target == NSAttributedStringTarget.UILabel)
					{
						verticalOffset /= 2;
					}

					// Because we're trying to move the text up (toward the top of the line box), 
					// we only set BaselineOffset to a positive value. 
					// A negative value indicates that the the text is already bottom-aligned.
					attributes.BaselineOffset = Math.Max(0, (float)verticalOffset);
				}
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

		partial void HitCheckOverridePartial(ref bool hitCheck)
		{
			hitCheck = true;
		}

		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			var point = (touches.AnyObject as UITouch).LocationInView(this);
			var args = new PointerRoutedEventArgs(point);
			OnPointerPressed(args);
			if (!args.Handled)
			{
				// Give parent a chance to handle the event.
				base.TouchesBegan(touches, evt);
			}
		}

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			var point = (touches.AnyObject as UITouch).LocationInView(this);
			var args = new PointerRoutedEventArgs(point);
			OnPointerReleased(args);
			if (!args.Handled)
			{
				// Give parent a chance to handle the event.
				base.TouchesEnded(touches, evt);
			}
		}

		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			var point = (touches.AnyObject as UITouch).LocationInView(this);
			var args = new PointerRoutedEventArgs(point);
			OnPointerCanceled(args);
			if (!args.Handled)
			{
				// Give parent a chance to handle the event.
				base.TouchesCancelled(touches, evt);
			}
		}

		private void UpdateHyperlinkLayout()
		{
			// Configure textContainer
			var textContainer = new NSTextContainer();
			textContainer.LineFragmentPadding = 0;
			textContainer.LineBreakMode = LineBreakMode;
			textContainer.MaximumNumberOfLines = (nuint)Lines;
			textContainer.Size = _drawRect.Size;

			// Configure layoutManager
			_layoutManager = new NSLayoutManager();
			_layoutManager.AddTextContainer(textContainer);

			// Configure textStorage
			var textStorage = new NSTextStorage();
			textStorage.SetString(GetAttributedText(NSAttributedStringTarget.NSTextStorage));
			textStorage.AddLayoutManager(_layoutManager);

			if (FeatureConfiguration.TextBlock.ShowHyperlinkLayouts)
			{
				textStorage.AddAttributes(
					new UIStringAttributes()
					{
						ForegroundColor = UIColor.Red
					},
					new NSRange(0, textStorage.Length)
				);
			}
		}

		private int GetCharacterIndexAtPoint(Point point)
		{
			if (!_drawRect.Contains(point))
			{
				return -1;
			}

			UpdateHyperlinkLayout();

			// Find the tapped character's index
			var partialFraction = (nfloat)0;
			var pointInTextContainer = new CGPoint(point.X - _drawRect.X, point.Y - _drawRect.Y);
			var characterIndex = (int)_layoutManager.CharacterIndexForPoint(pointInTextContainer, _layoutManager.TextContainers.FirstOrDefault(), ref partialFraction);

			return characterIndex;
		}

		#endregion
	}
}
