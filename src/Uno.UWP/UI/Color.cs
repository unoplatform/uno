using System;

namespace Windows.UI
{
	public partial struct Color : IFormattable
	{
		public byte A { get; set; }

		public byte B { get; set; }

		public byte G { get; set; }

		public byte R { get; set; }

		internal bool IsTransparent => A == 0;

		public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);

		private Color(byte a, byte r, byte g, byte b)
		{
			A = a;
			R = r;
			G = g;
			B = b;
		}

		public override bool Equals(object o) => o is Color color && Equals(color);

		public bool Equals(Color color) => 
			color.A == A 
			&& color.R == R 
			&& color.G == G 
			&& color.B == B;

		public override int GetHashCode() => (A << 8) ^ (R << 6) ^ (G << 4) ^ B;

		public override string ToString() => $"[#{A:X2}{R:X2}{G:X2}{B:X2}]";

		public string ToString(string format, IFormatProvider provider) => ToString();

		public static bool operator ==(Color color1, Color color2) => color1.Equals(color2);

		public static bool operator !=(Color color1, Color color2) => !color1.Equals(color2);

		internal Color WithOpacity(double opacity) => new Color((byte)(A * opacity), R, G, B);
	}
}
