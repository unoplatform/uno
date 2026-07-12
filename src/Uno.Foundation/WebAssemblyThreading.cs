using System.Runtime.InteropServices.JavaScript;

namespace Uno.Foundation;

public static partial class WebAssemblyThreading
{
	public static void Initialize()
	{
		WindowObject = NativeMethods.GetWindowObject();
		IsThreadingEnabled = NativeMethods.IsThreadingEnabled();
	}

	public static bool IsThreadingEnabled { get; private set; }

	public static JSObject WindowObject { get; private set; }

	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled")]
		internal static partial bool IsThreadingEnabled();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyThreading.getWindowObject")]
		internal static partial JSObject GetWindowObject();
	}
}
