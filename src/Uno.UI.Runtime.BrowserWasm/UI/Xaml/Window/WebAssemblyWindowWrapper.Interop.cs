#nullable enable

using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Uno.UI.Runtime;

internal partial class WebAssemblyWindowWrapper
{
	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.WebAssemblyWindowWrapper.initialize")]
		public static partial Task Initialize([JSMarshalAs<JSType.Any>] object owner);

		[JSImport("globalThis.Uno.UI.Runtime.WebAssemblyWindowWrapper.getContainerId")]
		public static partial string GetContainerId([JSMarshalAs<JSType.Any>] object owner);

		[JSImport("globalThis.Uno.UI.Runtime.WebAssemblyWindowWrapper.getCanvasId")]
		public static partial string GetCanvasId([JSMarshalAs<JSType.Any>] object owner);

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setFullScreenMode")]
		internal static partial bool SetFullScreenMode(bool turnOn);

		[JSImport("globalThis.Uno.UI.Runtime.WebAssemblyWindowWrapper.resizeWindow")]
		internal static partial void ResizeWindow(int width, int height);

		[JSImport("globalThis.Uno.UI.Runtime.WebAssemblyWindowWrapper.moveWindow")]
		internal static partial void MoveWindow(int x, int y);
	}
}
