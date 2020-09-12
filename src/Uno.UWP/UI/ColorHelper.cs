using System;
using System.ComponentModel;
using Windows.UI;

// Do not remove or change for the WinUI conversion tool (space is required).
using Color = global::Windows .UI.Color;

namespace Windows.UI
{
    public static partial class ColorHelper
    {
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Color FromARGB(byte a, byte r, byte g, byte b) 
			=> Color.FromArgb(a, r, g, b);

		public static Color FromArgb(byte a, byte r, byte g, byte b)
			=> Color.FromArgb(a, r, g, b);
	}
}
