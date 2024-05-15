// This class is a counterpart of VectorExtensions class in microsoft.windows.sdk.net.ref\WinRT.Runtime.dll
// Please do not remove it as third-parties may depend on it.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;

namespace System.Numerics;

public static class VectorExtensions
{
	/// <summary>
	/// Converts a <see cref="Vector2"/> to <see cref="Point"/>
	/// </summary>
	/// <param name="vector">The <see cref="Vector2"/> to convert</param>
	/// <returns>A <see cref="Point"/></returns>
	public static Point ToPoint(this Vector2 vector) => new Point(vector.X, vector.Y);

	/// <summary>
	/// Converts a <see cref="Vector2"/> to <see cref="Size"/>
	/// </summary>
	/// <param name="vector">The <see cref="Vector2"/> to convert</param>
	/// <returns>A <see cref="Size"/></returns>
	public static Size ToSize(this Vector2 vector) => new Size(vector.X, vector.Y);

	/// <summary>
	/// Converts a <see cref="Point"/> to <see cref="Vector2"/>
	/// </summary>
	/// <param name="point">The <see cref="Point"/> to Convert</param>
	/// <returns>A <see cref="Vector2"/></returns>
	public static Vector2 ToVector2(this Point point) => new Vector2((float)point.X, (float)point.Y);

	/// <summary>
	/// Converts a <see cref="Size"/> to <see cref="Vector2"/>
	/// </summary>
	/// <param name="point">The <see cref="Size"/> to Convert</param>
	/// <returns>A <see cref="Vector2"/></returns>
	public static Vector2 ToVector2(this Size size) => new Vector2((float)size.Width, (float)size.Height);
}