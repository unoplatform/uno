using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI
{
	/// <summary>
	/// Mapping of a <see cref="Color"/> which permits negative values, which is convenient for calculations during animation.
	/// </summary>
	internal struct ColorOffset
	{
		public int A { get; set; }

		public int B { get; set; }

		public int G { get; set; }

		public int R { get; set; }

		private ColorOffset(int a, int r, int g, int b)
		{
			A = a;
			R = r;
			G = g;
			B = b;
		}

		public static readonly ColorOffset Zero = default;

		public static ColorOffset FromArgb(int a, int r, int g, int b) => new ColorOffset(a, r, g, b);

		public static explicit operator Color(ColorOffset colorOffset) => Color.FromArgb((byte)colorOffset.A, (byte)colorOffset.R, (byte)colorOffset.G, (byte)colorOffset.B);

		public static explicit operator ColorOffset(Color color) => FromArgb(color.A, color.R, color.G, color.B);

		public static ColorOffset operator -(ColorOffset minuend, ColorOffset subtrahend)
			=> FromArgb(minuend.A - subtrahend.A, minuend.R - subtrahend.R, minuend.G - subtrahend.G, minuend.B - subtrahend.B);

		public static ColorOffset operator +(ColorOffset first, ColorOffset second)
			=> FromArgb(first.A + second.A, first.R + second.R, first.G + second.G, first.B + second.B);

		public static ColorOffset operator *(float multiplier, ColorOffset color)
			=> FromArgb(multiplier * color.A, multiplier * color.R, multiplier * color.G, multiplier * color.B);

		private static ColorOffset FromArgb(float a, float r, float g, float b)
			=> FromArgb((int)a, (int)r, (int)g, (int)b);
	}
}
