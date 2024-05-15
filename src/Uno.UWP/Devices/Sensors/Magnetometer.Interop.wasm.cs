using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Sensors
{
	internal partial class Magnetometer
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Devices.Sensors.Magnetometer";

			[JSImport($"{JsType}.initialize")]
			internal static partial bool Initialize();

			[JSImport($"{JsType}.startReading")]
			internal static partial void StartReading();

			[JSImport($"{JsType}.stopReading")]
			internal static partial void StopReading();

		}
	}
}
