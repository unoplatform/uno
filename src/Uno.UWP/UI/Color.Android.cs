using System;

namespace Windows.UI
{
	public partial struct Color : IFormattable
	{
		public static implicit operator Android.Graphics.Color(Color color) => Android.Graphics.Color.Argb(color.A, color.R, color.G, color.B);

		public static implicit operator Color(Android.Graphics.Color color) => FromArgb(color.A, color.R, color.G, color.B);

		internal static Color FromAndroidInt(int color) => FromArgb(
			(byte)((color >> 24) & 0xFF),
			(byte)((color >> 16) & 0xFF),
			(byte)((color >> 8) & 0xFF),
			(byte)(color & 0xFF));
	}
}
