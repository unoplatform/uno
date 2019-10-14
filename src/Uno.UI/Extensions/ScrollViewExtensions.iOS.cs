using System;
using System.Drawing;

#if XAMARIN_IOS_UNIFIED
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
#endif

namespace Uno.UI.Extensions
{
	public static class ScrollViewExtensions
	{
		public enum ScrollingMode
		{
			/// <summary>
			/// Do not animate the scrolling.
			/// </summary>
			NotAnimated,

			/// <summary>
			/// Use standard animation
			/// </summary>
			Animated,

			/// <summary>
			/// Should use animation, but mainly force the target view port (bypasses platform validation). (Will animate on iOS 8 -if th keyboard is closed- but not on iOS 7, but works in all cases)
			/// </summary>
			Forced
		}

		/// <summary>
		/// Make a view visible into a scroll view
		/// </summary>
		/// <param name="scrollView">The scroll view to scroll to get the item visible.</param>
		/// <param name="view">View to make visible.</param>
		/// <param name="mode">Specifies a mode to use to place the view into the view port.</param>
		/// <param name="padding">
		/// Add an extra margin to the view from the edge of the view port when mode is 
		/// <seealso cref="BringIntoViewMode.TopLeftOfViewPort"/> or <seealso cref="BringIntoViewMode.BottomRightOfViewPort"/>.
		/// </param>
		/// <param name="animationMode">Specifies animation mode to use to animate the scrolling (or not).</param>
		public static void BringIntoView(
			this UIScrollView scrollView,
			UIView view,
			BringIntoViewMode mode = BringIntoViewMode.ClosestEdge,
			int padding = 0,
			ScrollingMode animationMode = ScrollingMode.Forced)
		{
			var boundsOfViewInScrollViewCoordinatesSystem = scrollView.ConvertRectFromView(view.Bounds, view);
			var bounds = scrollView.Bounds;
			var inset = scrollView.ContentInset;
			var viewPort = new CGRect
				(
					bounds.X + inset.Left,
					bounds.Y + inset.Top,
					bounds.Width - inset.Left - inset.Right,
					bounds.Height - inset.Top - inset.Bottom
				);

			nfloat x, y;
			switch (mode)
			{
				case BringIntoViewMode.ClosestEdge:
					scrollView.ScrollRectToVisible(boundsOfViewInScrollViewCoordinatesSystem, animationMode != ScrollingMode.NotAnimated);
					return;

				case BringIntoViewMode.TopLeftOfViewPort:
					x = boundsOfViewInScrollViewCoordinatesSystem.Left - padding;
					y = boundsOfViewInScrollViewCoordinatesSystem.Top - padding;
					break;

				case BringIntoViewMode.CenterOfViewPort:
					x = boundsOfViewInScrollViewCoordinatesSystem.Left - (viewPort.Width/2) + (boundsOfViewInScrollViewCoordinatesSystem.Width/2);
					y = boundsOfViewInScrollViewCoordinatesSystem.Top - (viewPort.Height/2) + (boundsOfViewInScrollViewCoordinatesSystem.Height/2);
					break;

				case BringIntoViewMode.BottomRightOfViewPort:
					x = boundsOfViewInScrollViewCoordinatesSystem.Right - viewPort.Width + padding;
					y = boundsOfViewInScrollViewCoordinatesSystem.Bottom - viewPort.Height + padding;
					break;

				default:
					throw new NotSupportedException("Unknown scroll into view behavior");
			}

			var maxX = scrollView.ContentSize.Width - viewPort.Width;
			var maxY = scrollView.ContentSize.Height - viewPort.Height;

			x = (nfloat)Math.Max(0, (nfloat)Math.Min(maxX, x));
			y = (nfloat)Math.Max(0, (nfloat)Math.Min(maxY, y));

			switch (animationMode)
			{
				case ScrollingMode.NotAnimated:
					scrollView.SetContentOffset(new CGPoint(x, y), false);
					break;


				case ScrollingMode.Animated:
					scrollView.SetContentOffset(new CGPoint(x, y), true);
					break;

				case ScrollingMode.Forced:
					scrollView.ContentOffset = new CGPoint(x, y);
					break;

				default:
					throw new NotSupportedException("Unknown animation mode");
			}
		}

		/// <summary>
		/// Reset the scrolling state to a valid location, i.e. ensure that the offset is not upper than the ContentSize.
		/// </summary>
		/// <param name="scrollView"></param>
		/// <param name="animationMode">Specifies animation mode to use to animate the scrolling (or not).</param>
		public static void ClearCustomScrollOffset(this UIScrollView scrollView, ScrollingMode animationMode = ScrollingMode.Forced)
		{
			var viewPort = scrollView.Bounds;
			var x = scrollView.ContentOffset.X;
			var y = scrollView.ContentOffset.Y;

			var maxX = scrollView.ContentSize.Width - viewPort.Width;
			var maxY = scrollView.ContentSize.Height - viewPort.Height;

			x = (nfloat)Math.Max(0, (nfloat)Math.Min(maxX, x));
			y = (nfloat)Math.Max(0, (nfloat)Math.Min(maxY, y));

			switch (animationMode)
			{
				case ScrollingMode.NotAnimated:
					scrollView.SetContentOffset(new CGPoint(x, y), false);
					break;

				case ScrollingMode.Animated:
					scrollView.SetContentOffset(new CGPoint(x, y), true);
					break;

				case ScrollingMode.Forced:
					scrollView.ContentOffset = new CGPoint(x, y);
					break;

				default:
					throw new NotSupportedException("Unknown animation mode");
			}
		}
	}
}
