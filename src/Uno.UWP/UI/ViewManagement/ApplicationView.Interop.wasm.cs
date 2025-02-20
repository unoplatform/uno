using System;
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.ViewManagement;

internal partial class ApplicationView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.WindowManager.current.getWindowTitle")]
		internal static partial string GetWindowTitle();
	}
}
