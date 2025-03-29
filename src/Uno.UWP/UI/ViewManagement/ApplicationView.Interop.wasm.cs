using System;
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.ViewManagement;

internal partial class ApplicationView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.getWindowTitle")]
		internal static partial string GetWindowTitle();
	}
}
