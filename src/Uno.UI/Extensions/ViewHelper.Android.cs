using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Android.App;
using Windows.UI.Xaml;
using SizeF = System.Drawing.SizeF;
using System.Runtime.CompilerServices;
using Android.Views;
using System.Threading;
using System.Numerics;
using Android.Graphics;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;

namespace Uno.UI
{
	public static class ViewHelper
	{
		/// <summary>
		/// Android reserves ids over (0x00FFFFFF) for resource files.
		/// Dynamically created views will start at id 0.
		/// Our best chance of having a unique id is to start from the 
		/// highest id and decrement it as requested.
		/// </summary>
		private static int _uniqueViewId = 0x00FFFFFF;

		/// <summary>
		/// Private field coming from MeasureSpec
		/// </summary>
		private static int MODE_SHIFT = 30;

		/// <summary>
		/// Private field coming from MeasureSpec
		/// </summary>
		private static int MODE_MASK = 0x3 << MODE_SHIFT;

		private static readonly double _cachedDensity;
		private static double _cachedScaledDensity;
		private static readonly double _cachedScaledXDpi;

		/// <summary>
		/// The scale to be applied to all layout conversions from/to DIP (device-independent pixels) to physical pixels.
		/// </summary>
		public static double Scale
		{
			get
			{
				return _cachedDensity;
			}
		}

		/// <summary>
		/// The scale to be applied, when text is displayed, to all layout conversions from/to DIP (device-independent pixels) to physical pixels.
		/// </summary>
		public static double FontScale
		{
			get
			{
				return _cachedScaledDensity;
			}
		}

		/// <summary>
		/// The maximum logical pixel value which can be converted to physical pixels without overflow.
		/// </summary>
		private static double MaxLogicalValue { get; }

		/// <summary>
		/// The minimum logical pixel value which can be converted to physical pixels without underflow.
		/// </summary>
		private static double MinLogicalValue { get; }

		public static string Architecture { get; }

		static ViewHelper()
		{
			using (Android.Util.DisplayMetrics displayMetrics = Android.App.Application.Context.Resources.DisplayMetrics)
			{
				AdjustScaledDensity(displayMetrics);

				// WARNING: The Density value is not completely based on the DPI of the device.
				// On two 8" devices, the Density may not be consistent.
				_cachedDensity = displayMetrics.Density;
				_cachedScaledDensity = displayMetrics.ScaledDensity;
				_cachedScaledXDpi = displayMetrics.Xdpi;

				MaxLogicalValue = (int.MaxValue - 1) / _cachedDensity;
				MinLogicalValue = (int.MinValue + 1) / _cachedDensity;
			}

			if (typeof(ViewHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(ViewHelper).Log().DebugFormat("Display Scale is {0}", Scale);
			}

			Architecture = Java.Lang.JavaSystem.GetProperty("os.arch");
		}

		public static void RefreshFontScale()
		{
			using Android.Util.DisplayMetrics displayMetrics = Android.App.Application.Context.Resources.DisplayMetrics;

			AdjustScaledDensity(displayMetrics);

			_cachedScaledDensity = displayMetrics.ScaledDensity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size PhysicalToLogicalPixels(this Size size)
		{
			return new Size(size.Width / Scale, size.Height / Scale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Size LogicalToPhysicalPixels(this Size size)
		{
			return new Size(LogicalToPhysicalPixels(size.Width), LogicalToPhysicalPixels(size.Height));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 LogicalToPhysicalPixels(this Vector2 vector)
		{
			return new Vector2(LogicalToPhysicalPixels(vector.X), LogicalToPhysicalPixels(vector.Y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point PhysicalToLogicalPixels(this Point point)
		{
			return new Point(point.X / Scale, point.Y / Scale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point LogicalToPhysicalPixels(this Point point)
		{
			return new Point(
				x: LogicalToPhysicalPixels(point.X),
				y: LogicalToPhysicalPixels(point.Y)
			);
		}

		public static bool IsLoaded(this View view)
		{
			var bindableView = view as Controls.BindableView;

			if (bindableView != null)
			{
				return bindableView.IsNativeLoaded;
			}
			else
			{
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.JellyBeanMr2)
				{
					return view.WindowId != null;
				}
				else
				{
					return view.WindowToken != null;
				}
			}
		}

		/// <summary>
		/// Gets the physical representation of the provided logical pixels, for text (which is affected by Text size preferences)
		/// </summary>
		/// <param name="value">The logical value</param>
		/// <returns>The pixels value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LogicalToPhysicalFontPixels(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				// We need to keep the NaN and Infinity as the value may be used
				// for layout constraints.
				return value;
			}

			// The rounding is explained here : http://developer.android.com/guide/practices/screens_support.html#dips-pels
			// 
			//  "This figure is the factor by which you should multiply the dp units on order to get the actual 
			//   pixel count for the current screen. (Then add 0.5f to round the figure up to the nearest whole number,
			//   when converting to an integer.)"

			return (int)((value * FontScale) + .5f);
		}

		/// <summary>
		/// Gets the physical representation of the provided logical pixels, 
		/// for the <see cref="View.PivotX"/> and <see cref="View.PivotY"/> of a View (cf. Remarks)
		/// </summary>
		/// <remarks>Compared to <see cref="LogicalToPhysicalPixels"/>, this won't apply any rounding and returns a float.</remarks>
		/// <param name="value">The logical value</param>
		/// <returns>The pixels value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LogicalToPhysicalPivotPixels(double value)
		{
			if (double.IsNaN(value))
			{
				return 0;
			}

			if (double.IsPositiveInfinity(value))
			{
				return int.MaxValue;
			}

			if (double.IsNegativeInfinity(value))
			{
				return int.MinValue;
			}

			return (float)(value * Scale);
		}


		/// <summary>
		/// Gets the physical representation of the provided logical pixels.
		/// </summary>
		/// <param name="value">The logical value</param>
		/// <returns>The pixels value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LogicalToPhysicalPixels(double value)
		{
			if (double.IsNaN(value))
			{
				return 0;
			}

			if (double.IsPositiveInfinity(value) || value > MaxLogicalValue)
			{
				return int.MaxValue;
			}

			if (double.IsNegativeInfinity(value) || value < MinLogicalValue)
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
				return (int)Math.Ceiling(value * Scale);
			}

			return (int)((value * Scale) + .5f);
		}

		/// <summary>
		/// Gets the logical representation of the provided physical pixels.
		/// </summary>
		/// <param name="value">The pixels value</param>
		/// <returns>The logical value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double PhysicalToLogicalPixels(double value)
		{
			return value / Scale;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Thickness LogicalToPhysicalPixels(this Thickness size)
		{
			return new Thickness(
				top: LogicalToPhysicalPixels(size.Top),
				left: LogicalToPhysicalPixels(size.Left),
				right: LogicalToPhysicalPixels(size.Right),
				bottom: LogicalToPhysicalPixels(size.Bottom)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Thickness PhysicalToLogicalPixels(this Thickness size)
		{
			return new Thickness(
				top: PhysicalToLogicalPixels(size.Top),
				left: PhysicalToLogicalPixels(size.Left),
				right: PhysicalToLogicalPixels(size.Right),
				bottom: PhysicalToLogicalPixels(size.Bottom)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect PhysicalToLogicalPixels(this Rect size)
		{
			return new Rect(
				size.X / Scale,
				size.Y / Scale,
				size.Width / Scale,
				size.Height / Scale
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF PhysicalToLogicalPixels(this RectF size)
		{
			var fScale = (float)Scale;

			return new RectF(
				size.Left / fScale,
				size.Top / fScale,
				size.Right / fScale,
				size.Bottom / fScale
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect LogicalToPhysicalPixels(this Rect size)
		{
			var physicalBottom = LogicalToPhysicalPixels(size.Bottom);
			var physicalRight = LogicalToPhysicalPixels(size.Right);

			var physicalX = LogicalToPhysicalPixels(size.X);
			var physicalY = LogicalToPhysicalPixels(size.Y);

			// We convert bottom and right to physical pixels and then determine physical width and height from them, rather than the other way around, to ensure that adjacent views touch (otherwise there can be a +/-1-pixel gap due to rounding error, in the case of non-integer logical dimensions).
			return new Rect(
				physicalX,
				physicalY,
				physicalRight - physicalX,
				physicalBottom - physicalY
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF LogicalToPhysicalPixels(this RectF size)
		{
			return new RectF(
				LogicalToPhysicalPixels(size.Left),
				LogicalToPhysicalPixels(size.Top),
				LogicalToPhysicalPixels(size.Right),
				LogicalToPhysicalPixels(size.Bottom)
			);
		}

		/// <summary>
		/// A C# re-implementation of the MeasureSpec.makeMeasureSpec method, to avoid the cost of interop.
		/// </summary>
		/// <param name="size">The size to use to create the MeasureSpec</param>
		/// <param name="mode">The mode of the MeasureSpec</param>
		/// <returns>A measure spec.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MakeMeasureSpec(int size, MeasureSpecMode mode)
		{
			return (size & ~MODE_MASK) | ((int)mode & MODE_MASK);
		}

		/// <summary>
		/// A C# re-implementation of the MeasureSpec.getSize method, to avoid the cost of interop.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MeasureSpecGetSize(int measureSpec)
		{
			return (measureSpec & ~MODE_MASK);
		}

		/// <summary>
		/// A C# re-implementation of the MeasureSpec.getMode method, to avoid the cost of interop.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MeasureSpecMode MeasureSpecGetMode(int measureSpec)
		{
			return (MeasureSpecMode)(measureSpec & MODE_MASK);
		}

		/// <summary>
		/// Converts logical size to measureSpec, using MeasureSpecMode based on the size's value.
		/// </summary>
		/// <param name="value">logical size</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SpecFromLogicalSize(double value)
		{
			return MakeMeasureSpec(
				double.IsPositiveInfinity(value) ? 0 : LogicalToPhysicalPixels(value),
				double.IsPositiveInfinity(value) ? MeasureSpecMode.Unspecified : MeasureSpecMode.AtMost
			);
		}

		/// <summary>
		/// Converts physical size to measureSpec, using MeasureSpecMode based on the size's value.
		/// </summary>
		/// <param name="value">physical size</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SpecFromPhysicalSize(int value)
		{
			return MakeMeasureSpec(value, MeasureSpecMode.AtMost);
		}

		/// <summary>
		/// Gets the Logical size from MeasureSpecs
		/// </summary>
		/// <param name="widthMeasureSpec"></param>
		/// <param name="heightMeasureSpec"></param>
		/// <returns></returns>
		public static Size LogicalSizeFromSpec(int widthMeasureSpec, int heightMeasureSpec)
		{
			double width = MeasureSpecGetSize(widthMeasureSpec);
			double height = MeasureSpecGetSize(heightMeasureSpec);
			MeasureSpecMode widthMode = MeasureSpecGetMode(widthMeasureSpec);
			MeasureSpecMode heightMode = MeasureSpecGetMode(heightMeasureSpec);

			return new Size(
				widthMode != MeasureSpecMode.Unspecified ? width : double.PositiveInfinity,
				heightMode != MeasureSpecMode.Unspecified ? height : double.PositiveInfinity
			)
			.PhysicalToLogicalPixels();
		}

		/// <summary>
		/// Get the size to use for measurement from a MeasureSpec, in physical pixels.
		/// </summary>
		public static int PhysicalSizeFromSpec(int dimensionMeasureSpec)
		{
			var dimension = MeasureSpecGetSize(dimensionMeasureSpec);
			var mode = MeasureSpecGetMode(dimensionMeasureSpec);
			return mode != MeasureSpecMode.Unspecified ? dimension : int.MaxValue;
		}

		/// <summary>
		/// Generates a probably unique view id.
		/// </summary>
		public static int GenerateUniqueViewId()
		{
			return Interlocked.Decrement(ref _uniqueViewId);
		}

		/// <summary>
		/// Converts an unpacked complex data value holding a dimension to its final floating point value. The two parameters unit and value are as in TYPE_DIMENSION.
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="value"></param>
		/// <param name="metrics"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method is an extract of com.google.android/android/5.1.1_r1/android/util/TypedValue.java, for performance.
		/// See http://grepcode.com/file/repository.grepcode.com/java/ext/com.google.android/android/5.1.1_r1/android/util/TypedValue.java#339
		/// </remarks>
		public static double ApplyDimension(Android.Util.ComplexUnitType unit, double value)
		{
			switch (unit)
			{
				case Android.Util.ComplexUnitType.Px:
					return value;

				case Android.Util.ComplexUnitType.Dip:
					return value * _cachedDensity;

				case Android.Util.ComplexUnitType.Sp:
					return value * _cachedScaledDensity;

				case Android.Util.ComplexUnitType.Pt:
					return value * _cachedScaledXDpi * (1.0d / 72);

				case Android.Util.ComplexUnitType.In:
					return value * _cachedScaledXDpi;

				case Android.Util.ComplexUnitType.Mm:
					return value * _cachedScaledXDpi * (1.0d / 25.4f);
			}

			return 0;
		}

		private static void AdjustScaledDensity(Android.Util.DisplayMetrics displayMetrics)
		{
			if (FeatureConfiguration.Font.IgnoreTextScaleFactor)
			{
				// To disable text scaling, we put the Density value in ScaledDensity so that the ratio between them is 1.
				// This ensures it's disabled for everything using ScaledDensity (e.g. TextBlock, TextBox, AppBarButton, etc.)
				// https://developer.xamarin.com/api/property/Android.Util.DisplayMetrics.ScaledDensity/
				displayMetrics.ScaledDensity = displayMetrics.Density;
			}
			else if (FeatureConfiguration.Font.MaximumTextScaleFactor is float scaleFactor)
			{
				displayMetrics.ScaledDensity = Math.Min(displayMetrics.ScaledDensity, scaleFactor);
			}
		}
	}
}
