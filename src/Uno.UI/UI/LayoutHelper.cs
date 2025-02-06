using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using static System.Double;

#if __IOS__ || __MACOS__
using ObjCRuntime;
#endif

namespace Uno.UI
{
	internal static partial class LayoutHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Deconstruct(this Rect rect, out double x, out double y, out double width, out double height)
		{
			x = rect.X;
			y = rect.Y;
			width = rect.Width;
			height = rect.Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Deconstruct(this Size size, out double width, out double height)
		{
			width = size.Width;
			height = size.Height;
		}

		internal static Size GetMinSize(this IFrameworkElement e) => new Size(e.MinWidth, e.MinHeight).NumberOrDefault(new Size(0, 0));

		internal static Size GetMaxSize(this IFrameworkElement e) => new Size(e.MaxWidth, e.MaxHeight).NumberOrDefault(new Size(PositiveInfinity, PositiveInfinity));

		internal static (Size min, Size max) GetMinMax(this IFrameworkElement e)
		{
			double minWidth;
			double maxWidth;
			double minHeight;
			double maxHeight;

			var isDefaultHeight = double.IsNaN(e.Height);
			var isDefaultWidth = double.IsNaN(e.Width);

			maxHeight = e.MaxHeight;
			minHeight = e.MinHeight;
			var userValue = e.Height;

			var height = isDefaultHeight ? double.PositiveInfinity : userValue;
			maxHeight = Math.Max(Math.Min(height, maxHeight), minHeight);

			height = (isDefaultHeight ? 0 : userValue);
			minHeight = Math.Max(Math.Min(maxHeight, height), minHeight);

			maxWidth = e.MaxWidth;
			minWidth = e.MinWidth;
			userValue = e.Width;

			var width = (isDefaultWidth ? double.PositiveInfinity : userValue);
			maxWidth = Math.Max(Math.Min(width, maxWidth), minWidth);

			width = (isDefaultWidth ? 0 : userValue);
			minWidth = Math.Max(Math.Min(maxWidth, width), minWidth);

			if (e is UIElement uiElement && uiElement.GetUseLayoutRounding())
			{
				// It is possible for max vars to be INF so be don't want to round those.

				minWidth = uiElement.LayoutRound(minWidth);

				if (double.IsFinite(maxWidth))
					maxWidth = uiElement.LayoutRound(maxWidth);

				minHeight = uiElement.LayoutRound(minHeight);

				if (double.IsFinite(maxHeight))
					maxHeight = uiElement.LayoutRound(maxHeight);
			}

			return (new Size(minWidth, minHeight), new Size(maxWidth, maxHeight));
		}

		/// <summary>
		/// Apply min/max and defined sized on control to an available size
		/// </summary>
		/// <returns>Available size after applying min/max</returns>
		internal static Size ApplySizeConstraints(this IFrameworkElement e, Size forSize)
		{
			var (min, max) = e.GetMinMax();
			return forSize
				.AtMost(max)
				.AtLeast(min); // UWP is applying "min" after "max", so if "min" > "max", "min" wins
		}

		/// <summary>
		/// Apply min/max and defined sized on control to an available size
		/// </summary>
		/// <returns>Available size after applying min/max</returns>
		internal static Size ApplySizeConstraints(this IFrameworkElement e, Size forSize, Size extraPadding)
		{
			var (min, max) = e.GetMinMax();
			return forSize
				.AtMost(max.Subtract(extraPadding))
				.AtLeast(min.Subtract(extraPadding)); // UWP is applying "min" after "max", so if "min" > "max", "min" wins
		}

		internal static Size GetMarginSize(this IFrameworkElement frameworkElement)
		{
			var margin = frameworkElement.Margin;
			if (margin == default)
			{
				return default;
			}
			var marginWidth = margin.Left + margin.Right;
			var marginHeight = margin.Top + margin.Bottom;
			return new Size(marginWidth, marginHeight);
		}

		internal static Point GetAlignmentOffset(this IFrameworkElement e, Size clientSize, Size renderSize)
		{
			double offsetX = 0;
			double offsetY = 0;
			switch (e.HorizontalAlignment)
			{
				case HorizontalAlignment.Stretch when renderSize.Width > clientSize.Width:
				case HorizontalAlignment.Left:
					offsetX = 0;
					break;
				case HorizontalAlignment.Stretch:
				case HorizontalAlignment.Center:
					offsetX = (clientSize.Width - renderSize.Width) / 2.0;
					break;
				case HorizontalAlignment.Right:
					offsetX = clientSize.Width - renderSize.Width;
					break;
			}

			switch (e.VerticalAlignment)
			{
				case VerticalAlignment.Stretch when renderSize.Height > clientSize.Height:
				case VerticalAlignment.Top:
					offsetY = 0;
					break;
				case VerticalAlignment.Stretch:
				case VerticalAlignment.Center:
					offsetY = (clientSize.Height - renderSize.Height) / 2.0;
					break;
				case VerticalAlignment.Bottom:
					offsetY = clientSize.Height - renderSize.Height;
					break;
			}

			return new Point(offsetX, offsetY);
		}

		internal static Size Min(Size val1, Size val2)
		{
			return new Size(
				Math.Min(val1.Width, val2.Width),
				Math.Min(val1.Height, val2.Height)
			);
		}

		internal static Size Max(Size val1, Size val2)
		{
			return new Size(
				Math.Max(val1.Width, val2.Width),
				Math.Max(val1.Height, val2.Height)
			);
		}

		internal static Size Add(this Size left, Size right)
		{
			if (right == default)
			{
				return left;
			}

			return new Size(
				left.Width + right.Width,
				left.Height + right.Height
			);
		}

		internal static Size Add(this Size left, Thickness right)
		{
			if (right == default)
			{
				return left;
			}

			return new Size(
				left.Width + right.Left + right.Right,
				left.Height + right.Top + right.Bottom
			);
		}

		internal static Size Subtract(this Size size, double width, double height)
		{
			if (width == default && height == default)
			{
				return size;
			}

			return new Size(
				size.Width - width,
				size.Height - height
			);
		}

		internal static Size Subtract(this Size left, Size right)
		{
			if (right == default)
			{
				return left;
			}

			return new Size(
				left.Width - right.Width,
				left.Height - right.Height
			);
		}

		internal static Size Subtract(this Size left, Thickness right)
		{
			if (right == Thickness.Empty)
			{
				return left;
			}

			return new Size(
				left.Width - right.Left - right.Right,
				left.Height - right.Top - right.Bottom
			);
		}

		/// <summary>
		/// a.k.a. Scale
		/// </summary>
		internal static Size Multiply(this Size left, double right)
		{
			return new Size(left.Width * right, left.Height * right);
		}

		internal static Size Divide(this Size left, double right)
		{
			return new Size(left.Width / right, left.Height / right);
		}

		internal static Rect InflateBy(this Rect left, Thickness right)
		{
			var newWidth = right.Left + left.Width + right.Right;
			var newHeight = right.Top + left.Height + right.Bottom;

			// The origin is always following the left/top
			var newX = left.X - right.Left;
			var newY = left.Y - right.Top;

			return new Rect(newX, newY, Math.Max(newWidth, 0d), Math.Max(newHeight, 0d));
		}

		internal static Rect DeflateBy(this Rect left, Thickness right) => left.InflateBy(right.GetInverse());

		internal static double NumberOrDefault(this double value, double defaultValue)
		{
			return IsNaN(value)
				? defaultValue
				: value;
		}

		internal static Size NumberOrDefault(this Size value, Size defaultValue)
		{
			return new Size(
				value.Width.NumberOrDefault(defaultValue.Width),
				value.Height.NumberOrDefault(defaultValue.Height)
			);
		}

		internal static double FiniteOrDefault(this double value, double defaultValue)
		{
#if XAMARIN
			return IsFinite(value)
				? value
				: defaultValue;
#else
			return IsInfinity(value) || IsNaN(value)
				? defaultValue
				: value;
#endif
		}

		internal static Point FiniteOrDefault(this Point value, Point defaultValue)
		{
			return new Point(
				value.X.FiniteOrDefault(defaultValue.X),
				value.Y.FiniteOrDefault(defaultValue.Y));
		}

		internal static Size FiniteOrDefault(this Size value, Size defaultValue)
		{
			return new Size(
				value.Width.FiniteOrDefault(defaultValue.Width),
				value.Height.FiniteOrDefault(defaultValue.Height)
			);
		}

		internal static Rect FiniteOrDefault(this Rect value, Rect defaultValue)
		{
			return new Rect(
				value.X.FiniteOrDefault(defaultValue.X),
				value.Y.FiniteOrDefault(defaultValue.Y),
				value.Width.FiniteOrDefault(defaultValue.Width),
				value.Height.FiniteOrDefault(defaultValue.Height));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double AtMost(this double value, double most) => Math.Min(value, most);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size AtMost(this Size value, Size most)
		{
			return new Size(
				value.Width.AtMost(most.Width),
				value.Height.AtMost(most.Height)
			);
		}

		internal static Rect AtMost(this Rect value, Size most) => new Rect(value.Location, value.Size.AtMost(most));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double AtLeast(this double value, double least) => Math.Max(value, least);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size AtLeast(this Size value, Size least)
		{
			return new Size(
				value.Width.AtLeast(least.Width),
				value.Height.AtLeast(least.Height)
			);
		}

		internal static Rect AtLeast(this Rect value, Size least) => new Rect(value.Location, value.Size.AtLeast(least));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size AtLeastZero(this Size value)
		{
			return new Size(
				value.Width.AtLeast(0d),
				value.Height.AtLeast(0d)
			);
		}

		/// <summary>
		/// Return overlapped zone, if any
		/// </summary>
		/// <returns>null means no overlap</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect? IntersectWith(this Rect rect1, Rect rect2)
		{
			if (rect1.Equals(rect2))
			{
				return rect1;
			}

			if (rect1.IsInfinite)
			{
				return rect2;
			}
			else if (rect2.IsInfinite)
			{
				return rect1;
			}

			var left = Math.Max(rect1.Left, rect2.Left);
			var right = Math.Min(rect1.Right, rect2.Right);
			var top = Math.Max(rect1.Top, rect2.Top);
			var bottom = Math.Min(rect1.Bottom, rect2.Bottom);

			if (right >= left && bottom >= top)
			{
				return new Rect(left, top, right - left, bottom - top);
			}
			else
			{
				return null;
			}
		}

		internal static Rect UnionWith(this Rect rect1, Rect rect2)
		{
			rect1.Union(rect2);
			return rect1;
		}

		/// <summary>
		/// Test if a Rect "fits" totally in another one.
		/// </summary>
		internal static bool IsEnclosedBy(this Rect enclosee, Rect encloser)
		{
			if (enclosee.Equals(encloser))
			{
				return true;
			}

			return enclosee.Left >= encloser.Left
				&& enclosee.Right <= encloser.Right
				&& enclosee.Top >= encloser.Top
				&& enclosee.Bottom <= encloser.Bottom;
		}

		internal static double AspectRatio(this Rect rect) => rect.Size.AspectRatio();

		internal static Rect GetBoundsRectRelativeTo(this FrameworkElement element, FrameworkElement relativeTo)
		{
			var elementToTarget = element.TransformToVisual(relativeTo);
			// Use ActualWidth/ActualHeight which may differ from RenderSize in some cases (notably, TextBlock)
			var elementRect = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
			var elementRectRelToTarget = elementToTarget.TransformBounds(elementRect);

			return elementRectRelToTarget;
		}

		internal static Rect GetAbsoluteBoundsRect(this FrameworkElement element)
		{
			var root = (element.XamlRoot?.VisualTree.RootElement ?? Window.CurrentSafe?.RootElement) as FrameworkElement;
			return GetBoundsRectRelativeTo(element, root);
		}
	}
}
