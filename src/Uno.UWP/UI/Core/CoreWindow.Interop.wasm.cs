using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Core;

internal partial class CoreWindow
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.WindowManager.current.setCursor")]
		internal static partial void SetCursor(string type);
	}
}
