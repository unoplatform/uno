#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
		private static VibrationDevice _instance = null;

		private VibrationDevice()
		{
		}

		public static VibrationDevice GetDefault() =>
			_instance ?? (_instance = new VibrationDevice());

		public void Vibrate(TimeSpan duration) =>
			WasmVibrate(duration);
		
		public void Cancel() =>
			WasmVibrate(TimeSpan.Zero);

		private void WasmVibrate(TimeSpan duration)
		{
			var command = $"Windows.Phone.Devices.Notification.VibrationDevice.vibrate(\"{(long)duration.TotalMilliseconds}\");";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
#endif
