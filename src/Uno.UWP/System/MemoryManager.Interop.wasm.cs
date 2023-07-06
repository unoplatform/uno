using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System
{
	internal partial class MemoryManager
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.System.MemoryManager";

			[JSImport($"{JsType}.getAppMemoryUsage")]
			[return: JSMarshalAs<JSType.Number>]
			internal static partial long GetAppMemoryUsage();
		}
	}
}
