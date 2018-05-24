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
	}
}
