using System;
using System.Diagnostics.Contracts;
using Windows.Foundation;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Provides helper methods to evaluate or set Rect values.
	/// </summary>
	/// <remarks>
	/// Most of the methods on this class are present directly as methods on the Rect class.
	/// </remarks>
	public partial class RectHelper
	{
		public static Rect Empty { get; } = new Rect(0, 0, 0, 0); // Value is different than Rect.Empty!
		public static Rect FromCoordinatesAndDimensions(float x, float y, float width, float height) => new Rect(x, y, width, height);
		public static Rect FromPoints(Point point1, Point point2) => new Rect(point1, point2);
		public static Rect FromLocationAndSize(Point location, Size size) => new Rect(location, size);

		public static bool GetIsEmpty(Rect target) => target.Equals(Empty);
		public static float GetBottom(Rect target) => (float)target.Bottom;
		public static float GetLeft(Rect target) => (float)target.Left;
		public static float GetRight(Rect target) => (float)target.Right;
		public static float GetTop(Rect target) => (float)target.Top;

		/// <summary>
		/// Returns whether a given Point is within the bounds of a given Rect, for a shared coordinate reference.
		/// </summary>
		public static bool Contains(Rect target, Point point) => target.Contains(point);

		/// <summary>
		/// Provides comparison of the values of two Rect values.
		/// </summary>
		public static bool Equals(Rect target, Rect value) => target.Equals(value);

		/// <summary>
		/// Returns the areas of two specified Rect values that intersect, as a new Rect.
		/// </summary>
		/// <remarks>
		/// Contrary to Rect.Intersect(), original target won't be modified.
		/// </remarks>
		public static Rect Intersect(Rect target, Rect rect)
		{
			var left = Math.Max(target.Left, rect.Left);
			var right = Math.Min(target.Right, rect.Right);
			if (left > right)
			{
				return Empty; // no intersection on the X axis
			}

			var top = Math.Max(target.Top, rect.Top);
			var bottom = Math.Min(target.Bottom, rect.Bottom);
			if (top > bottom)
			{
				return Empty; // no intersection on the Y axis
			}

			return new Rect(left, top, right - left, bottom - top);
		}

		/// <summary>
		/// Creates a rectangle that is exactly large enough to contain the a specified rectangle and a specified point.
		/// </summary>
		/// <remarks>
		/// Contrary to Rect.Union(), original target won't be modified.
		/// </remarks>
		public static Rect Union(Rect target, Point point)
		{
			var left = Math.Min(target.Left, point.X);
			var right = Math.Max(target.Right, point.X);
			var top = Math.Min(target.Top, point.Y);
			var bottom = Math.Max(target.Bottom, point.Y);

			return new Rect(left, top, right - left, bottom - top);
		}

		/// <summary>
		/// Creates a rectangle that is exactly large enough to contain the two specified rectangles.
		/// </summary>
		/// <remarks>
		/// Contrary to Rect.Union(), original target won't be modified.
		/// </remarks>
		public static Rect Union(Rect target, Rect rect)
		{
			var left = Math.Min(target.Left, rect.Left);
			var right = Math.Max(target.Right, rect.Right);
			var top = Math.Min(target.Top, rect.Top);
			var bottom = Math.Max(target.Bottom, rect.Bottom);

			return new Rect(left, top, right - left, bottom - top);
		}
	}
}
