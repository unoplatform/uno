#nullable enable

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Skia;

public static partial class BrowserInputHelper
{
	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInputHelper.setBrowserZoomEnabled")]
		public static partial void SetBrowserZoomEnabled(bool enabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInputHelper.lockKeys")]
		public static partial Task LockKeys(string[] keyCodes);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.BrowserInputHelper.unlockKeys")]
		public static partial void UnlockKeys();
	}
}
