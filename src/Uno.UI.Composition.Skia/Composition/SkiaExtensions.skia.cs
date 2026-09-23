#nullable enable

using SkiaSharp;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public static class SkiaExtensions
	{

		public static SKRect ToSKRect(this Rect rect)
			=> new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

		public static Rect ToRect(this SKRect rect)
			=> new Rect(x: (float)rect.Left, y: (float)rect.Top, width: (float)rect.Width, height: (float)rect.Height);

		public static Size ToSize(this SKSize size)
			=> new Size(size.Width, size.Height);

		public static SKColor ToSKColor(this Color color)
			=> new SKColor(red: color.R, green: color.G, blue: color.B, alpha: color.A);

		public static SKMatrix ToSKMatrix(this Matrix3x2 m)
		{
			var ret = SKMatrix.Identity;

			ret.ScaleX = m.M11;
			ret.SkewY = m.M12;
			ret.SkewX = m.M21;
			ret.ScaleY = m.M22;
			ret.TransX = m.M31;
			ret.TransY = m.M32;

			return ret;
		}

		public static Matrix3x2 ToMatrix3x2(this SKMatrix m)
			=> new Matrix3x2(m.ScaleX, m.SkewY, m.SkewX, m.ScaleY, m.TransX, m.TransY);

		public static SKMatrix ToSKMatrix(this Matrix4x4 m)
			=> new(
				m.M11, m.M21, m.M41,
				m.M12, m.M22, m.M42,
				m.M14, m.M24, m.M44);

		public static Matrix4x4 ToMatrix4x4(this SKMatrix m)
		{
			var vals = m.Values;
			return new(
				vals[0], vals[3], 0, vals[6],
				vals[1], vals[4], 0, vals[7],
				/* */ 0, /* */ 0, 1, /* */ 0,
				vals[2], vals[5], 0, vals[8]);
		}
	}
}
