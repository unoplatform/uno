using System.Runtime.InteropServices.JavaScript;

namespace __Microsoft.UI.Xaml.Media
{
	internal partial class FontFamilyLoader
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Microsoft.UI.Xaml.Media.FontFamily.forceFontUsage")]
			internal static partial void ForceFontUsage(string cssFontName);

			[JSImport("globalThis.Microsoft.UI.Xaml.Media.FontFamily.loadFont")]
			internal static partial void LoadFont(string cssFontName, string externalSource);
		}
	}
}
