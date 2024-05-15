using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System
{
	internal partial class Launcher
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Uno.UI.WindowManager.current.open")]
			internal static partial string Open(string url);
		}
	}
}
