using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using Uno.UI.Extensions;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Windows.Graphics.Display;

using Foundation;
using CoreGraphics;
using ObjCRuntime;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
using Microsoft.UI.Xaml;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#endif

namespace Uno.UI
{
	public static class ViewHelper
	{
		/// <summary>
		/// This is used to correct some errors when using Floor and Ceiling in LogicalToPhysicalPixels for CGRect.
		/// </summary>
		public static double RectangleRoundingEpsilon { get; set; } = 0.05d;

		[Uno.NotImplemented]
		public static string Architecture => null;

		static ViewHelper()
		{
			if (typeof(ViewHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(ViewHelper).Log().DebugFormat("Display scale is {0}", DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
			}
		}

		public static nfloat OnePixel
		{
			get
			{
				return (nfloat)(1.0d / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CGSize PhysicalToLogicalPixels(this CGSize size)
		{
			return size;
			// UISize are automatically scaled to the device's DPI, we don't need to adjust.
			// return new SizeF(size.Width / MainScreenScale, size.Height / MainScreenScale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Windows.Foundation.Size PhysicalToLogicalPixels(this Windows.Foundation.Size size)
		{
			return size;
			// UISize are automatically scaled to the device's DPI, we don't need to adjust.
			// return new SizeF(size.Width / MainScreenScale, size.Height / MainScreenScale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CGSize LogicalToPhysicalPixels(this Windows.Foundation.Size size)
		{
			var ret = new CGSize((nfloat)size.Width, (nfloat)size.Height);
			return ret;
			// UISize are automatically scaled to the device's DPI, we don't need to adjust.
			// return new SizeF(size.Width * MainScreenScale, size.Height * MainScreenScale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CGSize LogicalToPhysicalPixels(this CGSize size)
		{
			return size;
			// UISize are automatically scaled to the device's DPI, we don't need to adjust.
			// return new SizeF(size.Width * MainScreenScale, size.Height * MainScreenScale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CGRect PhysicalToLogicalPixels(this CGRect size)
		{
			return size;
			// UISize are automatically scaled to the device's DPI, we don't need to adjust.
			//return new RectangleF(
			//	size.X / MainScreenScale,
			//	size.Y / MainScreenScale,
			//	size.Width / MainScreenScale,
			//	size.Height / MainScreenScale
			//);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Windows.Foundation.Point PhysicalToLogicalPixels(this Windows.Foundation.Point point)
		{
			return point;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Windows.Foundation.Point LogicalToPhysicalPixels(this Windows.Foundation.Point point)
		{
			return point;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static CGRect LogicalToPhysicalPixels(this CGRect size)
		{
			// This returns the `GCRect` that encompasses the given `CGRect`
			// in _real_ physical pixels.
			//
			// For a 1x display this would mean integral values, which is
			// similar to what `CGRectIntegral` provides. However this needs
			// and additional epsilon to properly rounds values.
			// https://developer.apple.com/documentation/coregraphics/1456348-cgrectintegral?language=objc
			//
			// For a retina (2x) display this could be half pixels and so on...

			var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
			double x1, y1, x2, y2;
			double epsilon = RectangleRoundingEpsilon;
			if (scale == 1.0d)
			{
				x1 = FloorWithEpsilon(size.X, epsilon);
				y1 = FloorWithEpsilon(size.Y, epsilon);
				x2 = CeilingWithEpsilon(size.X + size.Width, epsilon);
				y2 = CeilingWithEpsilon(size.Y + size.Height, epsilon);
			}
			else
			{
				var scaledEpsilon = epsilon * scale;
				x1 = FloorWithEpsilon(size.X * scale, scaledEpsilon) / scale;
				y1 = FloorWithEpsilon(size.Y * scale, scaledEpsilon) / scale;
				x2 = CeilingWithEpsilon((size.X + size.Width) * scale, scaledEpsilon) / scale;
				y2 = CeilingWithEpsilon((size.Y + size.Height) * scale, scaledEpsilon) / scale;
			}
			return new CGRect(x1, y1, x2 - x1, y2 - y1);
		}

		/// <summary>
		/// if the value would be 0.01, result would be 0 instead of 1
		/// </summary>
		private static double CeilingWithEpsilon(double value, double epsilon)
		{
			var truncate = Math.Truncate(value);
			var decimals = value - truncate;
			if (decimals < epsilon)
			{
				// note: since we process, always positive, pixels we can avoid
				// a call to `Floor` and use the `Truncate` result directly.
				return truncate;
			}
			else
			{
				return Math.Ceiling(value);
			}
		}

		/// <summary>
		/// if the value would be 0.99, result would be 1 instead of 0
		/// </summary>
		private static double FloorWithEpsilon(double value, double epsilon)
		{
			var truncate = Math.Truncate(value);
			var decimals = value - truncate;
			if (1 - decimals < epsilon)
			{
				return Math.Ceiling(value);
			}
			else
			{
				// note: since we process, always positive, pixels we can avoid
				// a call to `Floor` and use the `Truncate` result directly.
				return truncate;
			}
		}

		public static nfloat GetConvertedPixel(float thickness)
		{
			if (thickness > 0 && thickness <= 1 && (DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel > 1.0f))
			{
				return OnePixel;
			}
			return thickness;
		}

		public static nfloat StackSubViews(IEnumerable<_View> views)
		{
			nfloat lastBottom = 0f;
			foreach (var view in views)
			{

				if (view.Hidden)
				{
					continue;
				}

				view.Frame = view.Frame.SetY(lastBottom);
				lastBottom = view.Frame.Bottom;
			}

			return lastBottom;
		}

		public static nfloat StackSubViews(_View thisView, float topPadding, float spaceBetweenElements)
		{
			nfloat lastBottom = topPadding;

			foreach (var view in thisView.Subviews)
			{

				if (view.Hidden)
				{
					continue;
				}
				view.Frame = view.Frame.SetY(lastBottom);

				lastBottom = view.Frame.Bottom + spaceBetweenElements;
			}

			return lastBottom;
		}
	}
}
