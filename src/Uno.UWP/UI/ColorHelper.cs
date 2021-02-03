using System;
using System.ComponentModel;
using Windows.UI;

// Do not remove or change for the WinUI conversion tool (space is required).
using Color = global::Windows .UI.Color;

namespace Windows.UI
{
    public static partial class ColorHelper
    {
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string ToDisplayName(Color color)
		{
			throw new global::System.NotImplementedException("The member string ColorHelper.ToDisplayName(Color color) is not implemented in Uno.");
		}
		#endif

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Color FromARGB(byte a, byte r, byte g, byte b) 
			=> Color.FromArgb(a, r, g, b);

		public static Color FromArgb(byte a, byte r, byte g, byte b)
			=> Color.FromArgb(a, r, g, b);
	}
}
