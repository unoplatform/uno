using System.Drawing;
using Uno.UITest;

namespace SamplesApp.UITests
{
	public static class RectExtensions
	{
		public static float GetRight(this IAppRect rect)
		{
			return rect.X + rect.Width;
		}

		public static float GetBottom(this IAppRect rect)
		{
			return rect.Y + rect.Height;
		}

		public static Rectangle ToRectangle(this IAppRect rect)
		{
			return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}

		/// <summary>
		/// Get point within <paramref name="rect"/> from normalized relative coordinates.
		/// </summary>
		/// <param name="pointNormalizedRelative">The point of interest, relative to the <paramref name="rect"/>'s origin and normalized (0 - 1).</param>
		/// <returns>The point of interest in (unscaled) screen coordinates.</returns>
		public static PointF GetPointInRect(this IAppRect rect, PointF pointNormalizedRelative)
			=> new PointF(rect.X + pointNormalizedRelative.X * rect.Width, rect.Y + pointNormalizedRelative.Y * rect.Height);
	}
}
