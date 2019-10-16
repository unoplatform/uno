using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;

namespace Uno.Extensions
{
	public static class Vector2Extensions
	{
		/// <summary>
		/// Converts a <see cref="Vector2"/> to a <see cref="Point"/>.
		/// </summary>
		/// <param name="v">The <see cref="Vector2"/> to convert</param>
		/// <returns>A <see cref="Point"/></returns>
		public static Point ToPoint(this Vector2 v) => new Point(v.X, v.Y);

		/// <summary>
		/// Converts a <see cref="Point"/> to a <see cref="Vector2"/>.
		/// </summary>
		/// <param name="p">The <see cref="Point"/> to convert</param>
		/// <returns>A <see cref="Vector2"/></returns>
		public static Vector2 ToVector(this Point p) => new Vector2((float)p.X, (float)p.Y);
	}
}
