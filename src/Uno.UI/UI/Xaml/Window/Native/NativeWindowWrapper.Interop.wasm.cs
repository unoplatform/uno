using System;
using System.Collections.Generic;
using System.Linq;
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
	}
}
