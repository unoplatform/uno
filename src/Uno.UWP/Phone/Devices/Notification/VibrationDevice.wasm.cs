using System;
using System.Collections.Generic;
using System.Text;

using NativeMethods = __Windows.Phone.Devices.Notification.VibrationDevice.NativeMethods;

namespace Windows.Phone.Devices.Notification
{
	public partial class VibrationDevice
	{
		private static VibrationDevice? _instance;
		private static bool _initializationAttempted;

		private VibrationDevice()
		{
		}

		public static VibrationDevice? GetDefault()
		{
			if (!_initializationAttempted && _instance == null)
			{
				_instance = NativeMethods.Initialize() ? new() : null;

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
			NativeMethods.Vibrate($"{(long)duration.TotalMilliseconds}");
		}
	}
}
