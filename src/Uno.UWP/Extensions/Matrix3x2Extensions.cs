using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

namespace Uno.Extensions
{
	internal static class Matrix3x2Extensions
	{
		/// <summary>
		/// Creates a transformed <see cref="Point"/> using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="point">The point to transform</param>
		/// <param name="matrix">The matrix to use to transform the <paramref name="point"/></param>
		/// <returns>A new rectangle</returns>
		public static Point Transform(this Matrix3x2 matrix, Point point)
			=> matrix.Transform(point.X, point.Y);

		/// <summary>
		/// Creates a transformed point using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="x">The x coordinate of the point to transform</param>
		/// <param name="y">The y coordinate of the point to transform</param>
		/// <param name="matrix">The matrix to use to transform the point</param>
		/// <returns>A new rectangle</returns>
		public static Point Transform(this Matrix3x2 matrix, double x, double y)
		{
			if (matrix.IsIdentity)
			{
				return new Point(x, y);
			}

			var transformed = Vector2.Transform(new Vector2((float)x, (float)y), matrix);
			return new Point(transformed.X, transformed.Y);
		}

		/// <summary>
		/// Creates a transformed point using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="x">The x coordinate of the point to transform</param>
		/// <param name="y">The y coordinate of the point to transform</param>
		/// <param name="matrix">The matrix to use to transform the point</param>
		/// <returns>A new rectangle</returns>
		public static Point Transform(this Matrix3x2 matrix, double x, double y, double originX, double originY)
		{
			if (matrix.IsIdentity)
			{
				return new Point(x, y);
			}

			return new Point(
				(x * matrix.M11 * originX) + (y * matrix.M21 * originY) + matrix.M31,
				(x * matrix.M12 * originX) + (y * matrix.M22 * originY) + matrix.M32);
		}

		/// <summary>
		/// Creates a transformed bounds <see cref="Rect"/> using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="rect">The rectangle to transform</param>
		/// <param name="matrix">The matrix to use to transform the <paramref name="rect"/></param>
		/// <returns>A new rectangle</returns>
		public static Rect Transform(this Matrix3x2 matrix, Rect rect)
		{
			var leftTop = matrix.Transform(rect.Left, rect.Top);
			var leftBottom = matrix.Transform(rect.Left, rect.Bottom);
			var rightTop = matrix.Transform(rect.Right, rect.Top);
			var rightBottom = matrix.Transform(rect.Right, rect.Bottom);

			var point1 = Min(leftTop, leftBottom, rightTop, rightBottom);
			var point2 = Max(leftTop, leftBottom, rightTop, rightBottom);

			return new Rect(point1, point2);
		}

		/// <summary>
		/// Creates a transformed bounds <see cref="Rect"/> using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="matrix">The matrix to use to transform the <paramref name="rect"/></param>
		/// <param name="rect">The rectangle to transform</param>
		/// <param name="origin">The relative origin to use to apply transform.</param>
		/// <returns>A new rectangle</returns>
		public static Rect Transform(this Matrix3x2 matrix, Rect rect, Point origin)
			=> Transform(matrix.CenterOn(origin, rect.Size), rect);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix3x2 CenterOn(this Matrix3x2 matrix, Point relativeOrigin, Size size)
		{
			if (matrix.IsIdentity || relativeOrigin == default)
			{
				return matrix;
			}

			var origin = new Vector2((float)(relativeOrigin.X * size.Width), (float)(relativeOrigin.Y * size.Height));

			return Matrix3x2.CreateTranslation(origin * -1) * matrix * Matrix3x2.CreateTranslation(origin);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix3x2 CenterOn(this Matrix3x2 matrix, Point absoluteOrigin)
		{
			if (matrix.IsIdentity || absoluteOrigin == default)
			{
				return matrix;
			}

			var origin = new Vector2((float)absoluteOrigin.X, (float)absoluteOrigin.Y);

			return Matrix3x2.CreateTranslation(origin * -1) * matrix * Matrix3x2.CreateTranslation(origin);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix3x2 Inverse(this Matrix3x2 matrix)
		{
			if (matrix.IsIdentity)
			{
				return matrix;
			}

			Matrix3x2.Invert(matrix, out var result);
			return result;
		}

		private static Point Min(Point point1, Point point2, Point point3, Point point4)
		{
			var x = point1.X;
			x = Math.Min(point2.X, x);
			x = Math.Min(point3.X, x);
			x = Math.Min(point4.X, x);

			var y = point1.Y;
			y = Math.Min(point2.Y, y);
			y = Math.Min(point3.Y, y);
			y = Math.Min(point4.Y, y);

			return new Point(x, y);
		}

		private static Point Max(Point point1, Point point2, Point point3, Point point4)
		{
			var x = point1.X;
			x = Math.Max(point2.X, x);
			x = Math.Max(point3.X, x);
			x = Math.Max(point4.X, x);

			var y = point1.Y;
			y = Math.Max(point2.Y, y);
			y = Math.Max(point3.Y, y);
			y = Math.Max(point4.Y, y);

			return new Point(x, y);
		}
	}
}
