#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Xaml
{
	internal partial class Application
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.Xaml.Application.observeSystemTheme")]
			internal static partial void ObserveSystemTheme();

			[JSImport("globalThis.Windows.UI.Xaml.Application.observeVisibility")]
			internal static partial void ObserveVisibility();
		}
	}
}
#endif
