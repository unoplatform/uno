using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System
{
	internal partial class Launcher
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.System.Launcher.open")]
			internal static partial string Open(string url);
		}
	}
}
