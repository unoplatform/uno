using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace __Uno.UI.Xaml.Controls;

internal partial class NativeWindowWrapper
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.WindowManager.current.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setFullScreenMode")]
		internal static partial bool SetFullScreenMode(bool turnOn);

		[JSImport("globalThis.Uno.UI.WindowManager.current.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);

		[JSImport("globalThis.Uno.UI.WindowManager.current.resizeWindow")]
		internal static partial void ResizeWindow(int width, int height);

		[JSImport("globalThis.Uno.UI.WindowManager.current.moveWindow")]
		internal static partial void MoveWindow(int x, int y);
	}
}
