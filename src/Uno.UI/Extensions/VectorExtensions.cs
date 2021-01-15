using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;

namespace Uno.UI.Extensions
{
	public static class VectorExtensions
	{
		public static Point ToPoint(this Vector2 vector) => new Point(vector.X, vector.Y);
		public static Size ToSize(this Vector2 vector) => new Size(vector.X, vector.Y);
		public static Vector2 ToVector2(this Point point) => new Vector2((float)point.X, (float)point.Y);
		public static Vector2 ToVector2(this Size size) => new Vector2((float)size.Width, (float)size.Height);
	}
}
