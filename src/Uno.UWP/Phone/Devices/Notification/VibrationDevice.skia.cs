using System;

namespace Windows.Phone.Devices.Notification
{
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__")]
	public partial class VibrationDevice
	{
		private VibrationDevice()
		{
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__")]
		public static VibrationDevice GetDefault()
		{
			throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException("Windows.Phone.Devices.Notification.VibrationDevice", "VibrationDevice VibrationDevice.GetDefault()");
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__")]
		public void Vibrate(TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Vibrate(TimeSpan duration)");
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__")]
		public void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Cancel()");
		}
	}
}
