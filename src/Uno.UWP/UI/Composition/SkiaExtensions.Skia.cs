using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.UI;

namespace Windows.UI.Composition
{
	public static class SkiaExtensions
	{
		public static SKRect ToSKRect(this Rect rect)
			=> new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);

		public static Size ToSize(this SKSize size)
			=> new Size(size.Width, size.Height);

		public static SKColor ToSKColor(this Color color)
			=> new SKColor(red: color.R, green: color.G, blue: color.B, alpha: color.A);

		public static SKMatrix44 ToSKMatrix44(this Matrix4x4 m)
		{
			var ret = new SKMatrix44();

			ret[0,0] = m.M11;
			ret[1,0] = m.M12;
			ret[2,0] = m.M13;
			ret[3,0] = m.M14;

			ret[0,1] = m.M21;
			ret[1,1] = m.M22;
			ret[2,1] = m.M23;
			ret[3,1] = m.M24;

			ret[0,2] = m.M31;
			ret[1,2] = m.M32;
			ret[2,2] = m.M33;
			ret[3,2] = m.M34;

			ret[0,3] = m.M41;
			ret[1,3] = m.M42;
			ret[2,3] = m.M43;
			ret[3,3] = m.M44;

			return ret;
		}
	}
}
