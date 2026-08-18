using System.Runtime.InteropServices.JavaScript;

namespace __Windows.__System.Display
{
	internal partial class DisplayRequest
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.System.Display.DisplayRequest";

			[JSImport($"{JsType}.activateScreenLock")]
			internal static partial void ActivateScreenLock();

			[JSImport($"{JsType}.deactivateScreenLock")]
			internal static partial void DeactivateScreenLock();
		}
	}
}
