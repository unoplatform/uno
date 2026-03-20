using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Windows.UI
{
	[StructLayout(LayoutKind.Explicit)]
	public partial struct Color : IFormattable
	{
		/// <summary>
		/// Alias individual fields to avoid bitshifting and GetHashCode / compare costs
		/// </summary>
		[FieldOffset(0)]
		private uint _color;

		//
		// This memory layout assumes that the system uses little-endianness.
		//
		[FieldOffset(3)]
		private byte _a;
		[FieldOffset(2)]
		private byte _r;
		[FieldOffset(1)]
		private byte _g;
		[FieldOffset(0)]
		private byte _b;

		public byte A { get => _a; set => _a = value; }

		public byte B { get => _b; set => _b = value; }

		public byte G { get => _g; set => _g = value; }

		public byte R { get => _r; set => _r = value; }

		internal bool IsTransparent => _a == 0;

		public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);

		internal Color(byte a, byte r, byte g, byte b)
		{
			// Required for field initialization rules in C#
			_color = 0;

			_b = b;
			_g = g;
			_r = r;
			_a = a;
		}

		internal Color(uint color)
		{
			// Required for field initialization rules in C#
			_b = 0;
			_g = 0;
			_r = 0;
			_a = 0;

			_color = color;
		}

		public override bool Equals(object o) => o is Color color && Equals(color);

		public bool Equals(Color color) =>
			color._color == _color;

		public override int GetHashCode() => (int)_color;

		public override string ToString() => ToString(null, null);

		public static bool operator ==(Color color1, Color color2) => color1.Equals(color2);

		public static bool operator !=(Color color1, Color color2) => !color1.Equals(color2);

		/// <summary>
		/// Returns value indicating color's luminance.
		/// Values lower than 0.5 mean dark color, above 0.5 light color.
		/// </summary>
		internal double Luminance => (0.299 * _r + 0.587 * _g + 0.114 * _b) / 255;

		// Note: This method has an equivalent in Toolkit.ColorExtensions for usage with Windows
		internal Color WithOpacity(double opacity) => new((byte)(_a * opacity), _r, _g, _b);

		internal uint AsUInt32() => _color;

		/// <remarks>
		/// Both this color and the color on top are assumed to be alpha-premultiplied.
		/// </remarks>>
		internal Color AlphaBlend(Color colorOnTop)
		{
			// https://en.wikipedia.org/wiki/Alpha_compositing
			var thisAlpha = A / 255.0f;
			var thatAlpha = colorOnTop.A / 255.0f;
			var outputAlpha = thatAlpha + thisAlpha * (1 - thatAlpha);
			return new Color((byte)Math.Round(outputAlpha * 255f),
				(byte)(Math.Round(colorOnTop.R * thatAlpha + R * thisAlpha * (1 - thatAlpha)) / outputAlpha),
				(byte)(Math.Round(colorOnTop.G * thatAlpha + G * thisAlpha * (1 - thatAlpha)) / outputAlpha),
				(byte)(Math.Round(colorOnTop.B * thatAlpha + B * thisAlpha * (1 - thatAlpha)) / outputAlpha));
		}

		internal HslColor ToHsl()
		{
			double r = R / 255.0;
			double g = G / 255.0;
			double b = B / 255.0;

			double max = Math.Max(Math.Max(r, g), b);
			double min = Math.Min(Math.Min(r, g), b);

			double h = 0, s = 0, l = (max + min) / 2;
			double delta = max - min;

			if (Math.Abs(delta) > double.Epsilon)
			{
				s = delta / (l > 0.5 ? (2.0 - max - min) : (max + min));

				double deltaR = (((max - r) / 6) + (delta / 2)) / delta;
				double deltaG = (((max - g) / 6) + (delta / 2)) / delta;
				double deltaB = (((max - b) / 6) + (delta / 2)) / delta;

				if (Math.Abs(r - max) < double.Epsilon)
				{
					h = deltaB - deltaG;
				}
				else if (Math.Abs(g - max) < double.Epsilon)
				{
					h = (1.0 / 3.0) + deltaR - deltaB;
				}
				else
				{
					h = (2.0 / 3.0) + deltaG - deltaR;
				}

				if (h < 0)
				{
					h += 1;
				}

				if (h > 1)
				{
					h -= 1;
				}
			}

			return new(h, s, l);
		}

		string IFormattable.ToString(string format, IFormatProvider formatProvider) => ToString(format, formatProvider);

		private string ToString(string format, IFormatProvider formatProvider) => string.Format(formatProvider, "#{0:X2}{1:X2}{2:X2}{3:X2}", _a, _r, _g, _b);
	}
}
