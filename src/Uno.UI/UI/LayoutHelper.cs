using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using static System.Double;
using static System.Math;

namespace Uno.UI
{
	internal static class LayoutHelper
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

		[Pure]
		internal static Size GetMinSize(this IFrameworkElement e) => new Size(e.MinWidth, e.MinHeight).NumberOrDefault(new Size(0, 0));

		[Pure]
		internal static Size GetMaxSize(this IFrameworkElement e) => new Size(e.MaxWidth, e.MaxHeight).NumberOrDefault(new Size(PositiveInfinity, PositiveInfinity));

		[Pure]
		internal static (Size min, Size max) GetMinMax(this IFrameworkElement e)
		{
			var size = new Size(e.Width, e.Height);
			var minSize = e.GetMinSize();
			var maxSize = e.GetMaxSize();

			minSize = size
				.NumberOrDefault(new Size(0, 0))
				.AtMost(maxSize)
				.AtLeast(minSize); // UWP is applying "min" after "max", so if "min" > "max", "min" wins

			maxSize = size
				.NumberOrDefault(new Size(PositiveInfinity, PositiveInfinity))
				.AtMost(maxSize)
				.AtLeast(minSize); // UWP is applying "min" after "max", so if "min" > "max", "min" wins

			return (minSize, maxSize);
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

		[Pure]
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

		[Pure]
		internal static (Point offset, bool overflow) GetAlignmentOffset(this IFrameworkElement e, Size clientSize, Size renderSize)
		{
			// Start with Bottom-Right alignment, multiply by 0/0.5/1 for Top-Left/Center/Bottom-Right alignment
			var offset = new Point(
				clientSize.Width - renderSize.Width,
				clientSize.Height - renderSize.Height
			);

			var overflow = false;

			switch (e.HorizontalAlignment)
			{
				case HorizontalAlignment.Stretch when renderSize.Width > clientSize.Width:
					offset.X = 0;
					overflow = true;
					break;
				case HorizontalAlignment.Left:
					offset.X = 0;
					break;
				case HorizontalAlignment.Stretch:
				case HorizontalAlignment.Center:
					offset.X *= 0.5;
					break;
				case HorizontalAlignment.Right:
					offset.X *= 1;
					break;
			}

			switch (e.VerticalAlignment)
			{
				case VerticalAlignment.Stretch when renderSize.Height > clientSize.Height:
					offset.Y = 0;
					overflow = true;
					break;
				case VerticalAlignment.Top:
					offset.Y = 0;
					break;
				case VerticalAlignment.Stretch:
				case VerticalAlignment.Center:
					offset.Y *= 0.5;
					break;
				case VerticalAlignment.Bottom:
					offset.Y *= 1;
					break;
			}

			return (offset, overflow);
		}

		[Pure]
		internal static Size Min(Size val1, Size val2)
		{
			return new Size(
				Math.Min(val1.Width, val2.Width),
				Math.Min(val1.Height, val2.Height)
			);
		}

		[Pure]
		internal static Size Max(Size val1, Size val2)
		{
			return new Size(
				Math.Max(val1.Width, val2.Width),
				Math.Max(val1.Height, val2.Height)
			);
		}

		[Pure]
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

		[Pure]
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

		[Pure]
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

		[Pure]
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

		[Pure]
		internal static Rect InflateBy(this Rect left, Thickness right)
		{
			var newWidth = right.Left + left.Width + right.Right;
			var newHeight = right.Top + left.Height + right.Bottom;

			// The origin is always following the left/top
			var newX = left.X - right.Left;
			var newY = left.Y - right.Top;

			return new Rect(newX, newY, Math.Max(newWidth, 0d), Math.Max(newHeight, 0d));
		}

		[Pure]
		internal static Rect DeflateBy(this Rect left, Thickness right) => left.InflateBy(right.GetInverse());

		[Pure]
		internal static double NumberOrDefault(this double value, double defaultValue)
		{
			return IsNaN(value)
				? defaultValue
				: value;
		}

		[Pure]
		internal static double FiniteOrDefault(this double value, double defaultValue)
		{
			return IsFinite(value)
				? value
				: defaultValue;
		}

		[Pure]
		internal static Size NumberOrDefault(this Size value, Size defaultValue)
		{
			return new Size(
				value.Width.NumberOrDefault(defaultValue.Width),
				value.Height.NumberOrDefault(defaultValue.Height)
			);
		}

		[Pure]
		internal static Size FiniteOrDefault(this Size value, Size defaultValue)
		{
			return new Size(
				value.Width.FiniteOrDefault(defaultValue.Width),
				value.Height.FiniteOrDefault(defaultValue.Height)
			);
		}

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double AtMost(this double value, double most) => Math.Min(value, most);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size AtMost(this Size value, Size most)
		{
			return new Size(
				value.Width.AtMost(most.Width),
				value.Height.AtMost(most.Height)
			);
		}

		[Pure]
		internal static Rect AtMost(this Rect value, Size most) => new Rect(value.Location, value.Size.AtMost(most));

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double AtLeast(this double value, double least) => Math.Max(value, least);

		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Size AtLeast(this Size value, Size least)
		{
			return new Size(
				value.Width.AtLeast(least.Width),
				value.Height.AtLeast(least.Height)
			);
		}

		[Pure]
		internal static Rect AtLeast(this Rect value, Size least) => new Rect(value.Location, value.Size.AtLeast(least));

		[Pure]
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
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect? IntersectWith(this Rect rect1, Rect rect2)
		{
			if(rect1.Equals(rect2))
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

		[Pure]
		internal static Rect UnionWith(this Rect rect1, Rect rect2)
		{
			rect1.Union(rect2);
			return rect1;
		}

		/// <summary>
		/// Test if a Rect "fits" totally in another one.
		/// </summary>
		[Pure]
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

		[Pure]
		internal static double AspectRatio(this Rect rect) => rect.Size.AspectRatio();

		[Pure]
		internal static double AspectRatio(this Size size)
		{
			var w = size.Width;
			var h = size.Height;

			switch (w)
			{
				case NegativeInfinity:
					return -1;
				case PositiveInfinity:
					return 1;
				case NaN:
					return 1;
				case 0.0d:
					return 1;
			}

			switch (h)
			{
				case NegativeInfinity:
					return -1;
				case PositiveInfinity:
					return 1;
				case NaN:
					return 1;
				case 0.0d:
					return 1; // special case
				case 1.0d:
					return w;
			}

			return w / h;
		}

#if __IOS__ || __MACOS__
		[Pure]
		internal static double AspectRatio(this CoreGraphics.CGSize size)
		{
			var w = size.Width;
			var h = size.Height;

			if (w == nfloat.NegativeInfinity)
			{
				return -1;
			}
			else if (w == nfloat.PositiveInfinity)
			{
				return 1;
			}
			else if (w == nfloat.NaN)
			{
				return 1;
			}
			else if (w == 0.0d)
			{
				return 1;
			}

			if (h == nfloat.NegativeInfinity)
			{
				return -1;
			}
			else if (h == nfloat.PositiveInfinity)
			{
				return 1;
			}
			else if (h == nfloat.NaN)
			{
				return 1;
			}
			else if (h == 0.0d)
			{
				return 1; // special case
			}
			else if (h == 1.0d)
			{
				return w;
			}

			return w / h;
		}
#endif

		[Pure]
		internal static Rect GetBoundsRectRelativeTo(this UIElement element, UIElement relativeTo)
		{
			var elementToTarget = element.TransformToVisual(relativeTo);
			var elementRect = new Rect(default, element.RenderSize);
			var elementRectRelToTarget = elementToTarget.TransformBounds(elementRect);

			return elementRectRelToTarget;
		}

		[Pure]
		internal static Rect GetAbsoluteBoundsRect(this UIElement element)
		{
			var root = Window.Current.Content;
			return GetBoundsRectRelativeTo(element, root);
		}
	}
}
