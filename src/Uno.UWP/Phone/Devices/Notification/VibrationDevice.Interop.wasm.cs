using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Phone.Devices.Notification
{
	internal partial class VibrationDevice
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Phone.Devices.Notification.VibrationDevice";

			[JSImport($"{JsType}.initialize")]
			internal static partial bool Initialize();

			[JSImport($"{JsType}.vibrate")]
			internal static partial void Vibrate(string duration);
		}
	}
}
