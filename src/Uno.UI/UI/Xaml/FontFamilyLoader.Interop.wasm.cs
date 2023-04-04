#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Xaml.Media
{
	internal partial class FontFamilyLoader
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.Xaml.Media.FontFamily.loadFont")]
			internal static partial void LoadFont(string cssFontName, string externalSource);
		}
	}
}
#endif
