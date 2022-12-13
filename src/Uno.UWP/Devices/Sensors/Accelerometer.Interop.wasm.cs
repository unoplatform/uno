#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Sensors
{
	internal static partial class Accelerometer
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Devices.Sensors.Accelerometer.initialize")]
			internal static partial bool Initialize();

			[JSImport("globalThis.Windows.Devices.Sensors.Accelerometer.startReading")]
			internal static partial void StartReading();

			[JSImport("globalThis.Windows.Devices.Sensors.Accelerometer.stopReading")]
			internal static partial void StopReading();
		}
	}
}
#endif
