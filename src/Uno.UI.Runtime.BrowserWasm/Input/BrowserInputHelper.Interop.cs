#nullable enable

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Uno.UI.Runtime;

public static partial class BrowserInputHelper
{
	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.BrowserInputHelper.setBrowserZoomEnabled")]
		public static partial void SetBrowserZoomEnabled(bool enabled);

		[JSImport("globalThis.Uno.UI.Runtime.BrowserInputHelper.isKeyboardLockSupported")]
		public static partial bool IsKeyboardLockSupported();

		[JSImport("globalThis.Uno.UI.Runtime.BrowserInputHelper.lockKeys")]
		public static partial Task LockKeys(string[] keyCodes);

		[JSImport("globalThis.Uno.UI.Runtime.BrowserInputHelper.unlockKeys")]
		public static partial void UnlockKeys();
	}
}
