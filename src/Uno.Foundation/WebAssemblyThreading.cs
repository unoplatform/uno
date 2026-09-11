using System.Runtime.InteropServices.JavaScript;

namespace Uno.Foundation;

public static partial class WebAssemblyThreading
{
	public static void Initialize()
	{
		var window = NativeMethods.GetWindowObject();

		IsThreadingEnabled = NativeMethods.IsThreadingEnabled();
		WindowObject = window;
	}

	public static bool IsThreadingEnabled { get; private set; }

	/// <summary>
	/// Passing the Window object as an argument to a JSImport routes the call to the Main browser thread.
	/// This is not normally necessary on the deputy thread as it has no JSProxyContext.
	/// Use this for JSImports calls inside JSWebWorker.
	/// See: JSProxyContext.SealJSImportCapturing().
	/// </summary>
	public static JSObject WindowObject { get; private set; }

	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled")]
		internal static partial bool IsThreadingEnabled();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyThreading.getWindowObject")]
		internal static partial JSObject GetWindowObject();
	}
}
