#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Phone.Devices.Notification.VibrationDevice.NativeMethods;
#endif

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Phone.Devices.Notification.VibrationDevice";
#endif

		private static VibrationDevice _instance;
		private static bool _initializationAttempted;

		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault()
		{
			if (!_initializationAttempted && _instance == null)
			{
#if NET7_0_OR_GREATER
				_instance = NativeMethods.Initialize() ? new() : null;
#else
				var command = $"{JsType}.initialize()";
				var initialized = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
				if (bool.Parse(initialized) == true)
				{
					_instance = new VibrationDevice();
				}
#endif

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
#if NET7_0_OR_GREATER
			NativeMethods.Vibrate($"{(long)duration.TotalMilliseconds}");
#else
			var command = $"{JsType}.vibrate(\"{(long)duration.TotalMilliseconds}\");";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif
		}
	}
}
#endif
