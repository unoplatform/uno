using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using Foundation;
#if __MACOS__
using AppKit;
using _NativeScrollView = AppKit.NSScrollView;
using _View = AppKit.NSView;
#else
using UIKit;
using _NativeScrollView = UIKit.UIScrollView;
using _View = UIKit.UIView;
#endif
using CoreGraphics;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	partial class NativeScrollContentPresenter : _NativeScrollView, DependencyObject
	{
		private CGSize _measuredSize;
		private bool _requiresMeasure;

		private ScrollBarVisibility _verticalScrollBarVisibility;
		private ScrollBarVisibility _horizotalScrollBarVisibility;

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return _verticalScrollBarVisibility; }
			set
			{
				_verticalScrollBarVisibility = value;

				ShowsVerticalScrollIndicator = value == ScrollBarVisibility.Auto || value == ScrollBarVisibility.Visible;
			}
		}
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return _horizotalScrollBarVisibility; }
			set
			{
				_horizotalScrollBarVisibility = value;

				ShowsHorizontalScrollIndicator = value == ScrollBarVisibility.Auto || value == ScrollBarVisibility.Visible;
			}
		}

#if __IOS__
		public override
#else
		public
#endif
			CGSize SizeThatFits(CGSize size)
		{
			if (_content != null)
			{
				double horizontalMargin = 0;
				double verticalMargin = 0;

				if (_content is IFrameworkElement frameworkElement)
				{
					horizontalMargin = frameworkElement.Margin.Left + frameworkElement.Margin.Right;
					verticalMargin = frameworkElement.Margin.Top + frameworkElement.Margin.Bottom;
				}

				size = AdjustSize(size);

				var availableSizeForChild = size;
				if (!(_content is IFrameworkElement))
				{
					// Apply margin if the content is native (otherwise it will apply it itself)
					availableSizeForChild.Width -= (nfloat)horizontalMargin;
					availableSizeForChild.Height -= (nfloat)verticalMargin;
				}

				_measuredSize = _content.SizeThatFits(availableSizeForChild);
				_measuredSize.Width += (nfloat)horizontalMargin;
				_measuredSize.Height += (nfloat)verticalMargin;

				// The dimensions are constrained to the size of the ScrollViewer, if available
				// otherwise to the size of the child.
				return new CGSize(
					Math.Min(nfloat.IsNaN(size.Width) ? nfloat.MaxValue : size.Width, _measuredSize.Width),
					Math.Min(nfloat.IsNaN(size.Height) ? nfloat.MaxValue : size.Height, _measuredSize.Height)
				);
			}
			else
			{
				return _measuredSize = CGSize.Empty;
			}
		}

#if __IOS__
		public override void LayoutSubviews()
#else
		public override void Layout()
#endif
		{
			try
			{
				if (Content != null)
				{
					if (_requiresMeasure)
					{
						_requiresMeasure = false;
						SizeThatFits(Frame.Size);
					}

					double horizontalMargin = 0;
					double verticalMargin = 0;

					var frameworkElement = _content as IFrameworkElement;

					if (frameworkElement != null)
					{
						horizontalMargin = frameworkElement.Margin.Left + frameworkElement.Margin.Right;
						verticalMargin = frameworkElement.Margin.Top + frameworkElement.Margin.Bottom;

						var adjustedMeasure = new CGSize(
							GetAdjustedArrangeWidth(frameworkElement, (nfloat)horizontalMargin),
							GetAdjustedArrangeHeight(frameworkElement, (nfloat)verticalMargin)
						);

						// Zoom works by applying a transform to the child view. If a view has a non-identity transform, its Frame shouldn't be set.
						if (ZoomScale == 1)
						{
							_content.Frame = new CGRect(
								GetAdjustedArrangeX(frameworkElement, adjustedMeasure, horizontalMargin),
								GetAdjustedArrangeY(frameworkElement, adjustedMeasure, verticalMargin),
								adjustedMeasure.Width,
								adjustedMeasure.Height
							);
						}
					}

#if __IOS__
					ContentSize = AdjustContentSize(_content.Frame.Size + new CGSize(horizontalMargin, verticalMargin));

					// This prevents unnecessary touch delays (which affects the pressed visual states of buttons) when user can't scroll.
					UpdateDelayedTouches();
#else
					var size = AdjustContentSize(_content.Frame.Size + new CGSize(horizontalMargin, verticalMargin));
					ContentView.Frame = new CGRect(new CGPoint(0, 0), size);
#endif
				}
			}
			catch (Exception e)
			{
				this.Log().Error(e.ToString());
			}
		}

		private CGSize AdjustContentSize(CGSize measuredSize)
		{
			//ScrollMode does not affect the measure pass, so we only take it into account when setting the ContentSize of the UIScrollView and not in the SizeThatFits
			var isHorizontalScrollDisabled = HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled;
			var isVerticalScrollDisabled = VerticalScrollBarVisibility == ScrollBarVisibility.Disabled;

			//UIScrollView will not scroll if its ContentSize is smaller than the available size
			//Therefore, force the ContentSize dimension to 0 when we do not want to scroll
			var adjustedWidth = isHorizontalScrollDisabled ? this.Frame.Width : measuredSize.Width;
			var adjustedHeight = isVerticalScrollDisabled ? this.Frame.Height : measuredSize.Height;

			return new CGSize(adjustedWidth, adjustedHeight);
		}

		private nfloat GetAdjustedArrangeX(IFrameworkElement child, CGSize adjustedMeasuredSize, double horizontalMargin)
		{
			var frameSize = Frame.Size;

			switch (child?.HorizontalAlignment ?? HorizontalAlignment.Stretch)
			{
				case HorizontalAlignment.Left:
					return (nfloat)child.Margin.Left;

				case HorizontalAlignment.Right:
					return (adjustedMeasuredSize.Width + horizontalMargin) <= frameSize.Width ?
						frameSize.Width - adjustedMeasuredSize.Width - (nfloat)child.Margin.Right :
						(nfloat)child.Margin.Left;

				case HorizontalAlignment.Stretch: //Treat Stretch the same as Center here even if the child is not the same Width as the container, adjustments have already been applied
				case HorizontalAlignment.Center:
					var layoutWidth = adjustedMeasuredSize.Width + horizontalMargin;
					if (layoutWidth <= frameSize.Width)
					{
						var marginToFrame = (frameSize.Width - layoutWidth) / 2;
						return (nfloat)(marginToFrame + child.Margin.Left);
					}
					else
					{
						return (nfloat)child.Margin.Left;

					}

				default:
					throw new NotSupportedException("Invalid HorizontalAlignment");
			}
		}

		private nfloat GetAdjustedArrangeY(IFrameworkElement child, CGSize adjustedMeasuredSize, double verticalMargin)
		{
			var frameSize = Frame.Size;

			switch (child?.VerticalAlignment ?? VerticalAlignment.Stretch)
			{
				case VerticalAlignment.Top:
					return (nfloat)child.Margin.Top;

				case VerticalAlignment.Bottom:
					return (adjustedMeasuredSize.Height + verticalMargin) <= frameSize.Height ?
						frameSize.Height - adjustedMeasuredSize.Height - (nfloat)child.Margin.Bottom :
						(nfloat)child.Margin.Top;

				case VerticalAlignment.Stretch: //Treat Stretch the same as Center even if the child is not the same Height as the container, adjustments have already been applied
				case VerticalAlignment.Center:
					var layoutHeight = adjustedMeasuredSize.Height + verticalMargin;
					if (layoutHeight <= frameSize.Height)
					{
						var marginToFrame = (frameSize.Height - layoutHeight) / 2;
						return (nfloat)(marginToFrame + child.Margin.Top);
					}
					else
					{
						return (nfloat)child.Margin.Top;
					}

				default:
					throw new NotSupportedException("Invalid VerticalAlignment");
			}
		}

		private nfloat GetAdjustedArrangeWidth(IFrameworkElement child, nfloat horizontalMargin)
		{
			var adjustedWidth = _measuredSize.Width - horizontalMargin;
			var viewPortWidth = Frame.Size.Width - horizontalMargin;

			// Apply Stretch
			if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				// Make it at least as big as the view port
				adjustedWidth = (nfloat)Math.Max(adjustedWidth, viewPortWidth);
			}

			// Apply ScrollMode
			if (HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				// Make it at most as big as the view port
				adjustedWidth = (nfloat)Math.Min(adjustedWidth, viewPortWidth);
			}

			var childDesiredWidth = GetDesiredValue(child.MinWidth, child.Width, child.MaxWidth);
			childDesiredWidth = double.IsNaN(childDesiredWidth) ? double.MaxValue : childDesiredWidth;  //Because Math.Min with a NaN will return NaN.
			adjustedWidth = (nfloat)Math.Min(adjustedWidth, childDesiredWidth); //Take the smallest between the child desired Width and the measured width

			return adjustedWidth;
		}

		/// <summary>
		/// Returns the biggest value it can based on if the different arguments are set or not (double.NaN)
		/// </summary>
		/// <param name="minimumValue"></param>
		/// <param name="desiredValue"></param>
		/// <param name="maximumValue"></param>
		/// <returns></returns>
		private double GetDesiredValue(double minimumValue, double desiredValue, double maximumValue)
		{
			//If nothing is defined return NaN as to indicate that no values was determine
			if (double.IsNaN(minimumValue) && double.IsNaN(desiredValue) && double.IsNaN(maximumValue))
			{
				return double.NaN;
			}

			//Make sure we have something to compare with, '>' or '<' does not work with NaN.
			var innerMinimumValue = double.IsNaN(minimumValue) ? double.MinValue : minimumValue;
			var innerDesiredValue = double.IsNaN(desiredValue) ? double.MinValue : desiredValue;
			var innerMaximumValue = double.IsNaN(maximumValue) ? double.MaxValue : maximumValue; //Set to Max of double

			// Case 10, NaN, 30 returns 30
			if (double.IsNaN(desiredValue) && //desiredValue was not set 
			   !double.IsNaN(maximumValue) && //maximumValue was set
			   innerMinimumValue < innerMaximumValue)
			{
				return innerMaximumValue;
			}

			// 20   10    30  = 20
			// 30   10    10  = 30
			// 20   NaN   NaN = 20
			// 50   NaN  30   = 50 
			if (innerMinimumValue > innerDesiredValue) // if minimumValue always wins over desiredValue and maximumValue
			{
				return innerMinimumValue;
			}

			// NaN  NaN   10  = 10 
			// NaN  50    20  = 30

			if (double.IsNaN(desiredValue) ||           // If desiredValue was not set
				innerDesiredValue > innerMaximumValue)  // or desireValue is bigger than maximumValue take maximumValue
			{
				return maximumValue;
			}

			// 10   20    30 = 20
			// NaN  20    NaN = 20
			// NaN  20    30 = 20
			return desiredValue;

		}

		private nfloat GetAdjustedArrangeHeight(IFrameworkElement child, nfloat verticalMargin)
		{
			var adjustedHeight = _measuredSize.Height - verticalMargin;
			var viewPortHeight = Frame.Size.Height - verticalMargin;

			// Apply Stretch
			if (child.VerticalAlignment == VerticalAlignment.Stretch)
			{
				// Make it at least as big as the view port
				adjustedHeight = (nfloat)Math.Max(adjustedHeight, viewPortHeight);
			}

			// Apply ScrollMode
			if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
			{
				// Make it at most as big as the view port
				adjustedHeight = (nfloat)Math.Min(adjustedHeight, viewPortHeight);
			}

			var childDesiredHeight = GetDesiredValue(child.MinHeight, child.Height, child.MaxHeight);
			childDesiredHeight = double.IsNaN(childDesiredHeight) ? double.MaxValue : childDesiredHeight;  //Because Math.Min with a NaN will return NaN.

			adjustedHeight = (nfloat)Math.Min(adjustedHeight, childDesiredHeight);

			return adjustedHeight;
		}

		private CGSize AdjustSize(CGSize size)
		{
			var width = GetMeasureValue(size.Width, HorizontalScrollBarVisibility);
			var height = GetMeasureValue(size.Height, VerticalScrollBarVisibility);

			return new CGSize(width, height);
		}

		private nfloat GetMeasureValue(nfloat value, ScrollBarVisibility scrollBarVisibility)
		{
			switch (scrollBarVisibility)
			{
				case ScrollBarVisibility.Auto:
				case ScrollBarVisibility.Hidden:
				case ScrollBarVisibility.Visible:
					return nfloat.NaN;

				default:
				case ScrollBarVisibility.Disabled:
					return value;
			}
		}
	}
}
