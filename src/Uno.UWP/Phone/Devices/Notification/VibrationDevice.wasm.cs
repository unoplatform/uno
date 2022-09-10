#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
		private const string JsType = "Windows.Phone.Devices.Notification.VibrationDevice";
		private static VibrationDevice _instance;
		private static bool _initializationAttempted;

		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault()
		{
			if (!_initializationAttempted && _instance == null)
			{
				var command = $"{JsType}.initialize()";
				var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
				if (bool.Parse(initialized) == true)
				{
					_instance = new VibrationDevice();
				}
				_initializationAttempted = true;
			}
			return _instance;
		}

		public void Vibrate(TimeSpan duration) =>
			WasmVibrate(duration);

		public void Cancel() =>
			WasmVibrate(TimeSpan.Zero);

		private void WasmVibrate(TimeSpan duration)
		{
			var command = $"{JsType}.vibrate(\"{(long)duration.TotalMilliseconds}\");";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
#endif
