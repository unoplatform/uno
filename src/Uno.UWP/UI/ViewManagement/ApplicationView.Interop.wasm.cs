using System;
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.ViewManagement;

internal partial class ApplicationView
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.WindowManager.current.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setFullScreenMode")]
		internal static partial bool SetFullScreenMode(bool turnOn);

		[JSImport("globalThis.Uno.UI.WindowManager.current.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);
	}
}
