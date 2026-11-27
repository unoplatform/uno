using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System
{
	internal partial class Launcher
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.System.Launcher.open")]
			internal static partial string Open(string url);

			[JSImport("globalThis.Windows.System.Launcher.openFile")]
			internal static partial string OpenFile(byte[] content, string contentType, string fileName);
		}
	}
}
