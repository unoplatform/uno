using System.Runtime.InteropServices.JavaScript;

namespace __Microsoft.UI.Xaml;

internal partial class Application
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Microsoft.UI.Xaml.Application.observeVisibility")]
		internal static partial void ObserveVisibility();
	}
}
