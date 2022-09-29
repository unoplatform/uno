#nullable disable

#if __ANDROID__
using System;

namespace Windows.UI
{
	public partial struct Color : IFormattable
	{
		public static implicit operator Android.Graphics.Color(Color color) => Android.Graphics.Color.Argb(color.A, color.R, color.G, color.B);

		public static implicit operator Color(Android.Graphics.Color color) => FromArgb(color.A, color.R, color.G, color.B);
	}
}
#endif
