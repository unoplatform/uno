#if IS_UNIT_TESTS || UNO_REFERENCE_API
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Uno.UI
{
	public static class ViewHelper
	{
		private static double _cachedDensity;
		private static double _maxLogicalValue;
		private static double _minLogicalValue;

		static ViewHelper()
		{
			_cachedDensity = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
			_maxLogicalValue = (int.MaxValue - 1) / _cachedDensity;
			_minLogicalValue = (int.MinValue + 1) / _cachedDensity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size PhysicalToLogicalPixels(this Size size)
			=> new Size(PhysicalToLogicalPixels(size.Width), PhysicalToLogicalPixels(size.Height));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size LogicalToPhysicalPixels(this Size size)
			=> new Size(LogicalToPhysicalPixels(size.Width), LogicalToPhysicalPixels(size.Height));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect LogicalToPhysicalPixels(this Rect rect)
			=> new Rect(new Point(LogicalToPhysicalPixels(rect.Left), LogicalToPhysicalPixels(rect.Top)), LogicalToPhysicalPixels(rect.Size));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect PhysicalToLogicalPixels(this Rect rect)
			=> new Rect(new Point(PhysicalToLogicalPixels(rect.Left), PhysicalToLogicalPixels(rect.Top)), PhysicalToLogicalPixels(rect.Size));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point PhysicalToLogicalPixels(this Point point)
			=> new Point(PhysicalToLogicalPixels(point.X), PhysicalToLogicalPixels(point.Y));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point LogicalToPhysicalPixels(this Point point)
			=> new Point(LogicalToPhysicalPixels(point.X), LogicalToPhysicalPixels(point.Y));

		internal static double PhysicalToLogicalPixels(double value)
		{
			// TODO: Platform check here is very unfortunate. Try to refactor this into Uno.UWP
			if (Uno.UI.Helpers.DeviceTargetHelper.IsMobile())
			{
				return value / _cachedDensity;
			}

			return value;
		}

		internal static double LogicalToPhysicalPixels(double value)
		{
			// TODO: Platform check here is very unfortunate. Try to refactor this into Uno.UWP
			if (Uno.UI.Helpers.DeviceTargetHelper.IsMobile()) // TODO:MZ: Verify logic for Apple UIKit
			{
				if (double.IsNaN(value))
				{
					return 0;
				}
				if (double.IsPositiveInfinity(value) || value > _maxLogicalValue)
				{
					return int.MaxValue;
				}
				if (double.IsNegativeInfinity(value) || value < _minLogicalValue)
				{
					return int.MinValue;
				}

				// The rounding is explained here : http://developer.android.com/guide/practices/screens_support.html#dips-pels
				// 
				//  "This figure is the factor by which you should multiply the dp units on order to get the actual 
				//   pixel count for the current screen. (Then add 0.5f to round the figure up to the nearest whole number,
				//   when converting to an integer.)"
				if (value < 0)
				{
					return (int)Math.Ceiling(value * _cachedDensity);
				}

				return (int)((value * _cachedDensity) + .5f);
			}

			return value;
		}
	}
}
#endif
