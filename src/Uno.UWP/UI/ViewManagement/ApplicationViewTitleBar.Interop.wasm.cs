using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.ViewManagement
{
	internal partial class ApplicationViewTitleBar
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationViewTitleBar.setBackgroundColor")]
			internal static partial void SetBackgroundColor(string? color);
		}
	}
}
