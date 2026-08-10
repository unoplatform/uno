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
			throw new global::System.NotImplementedException("The member VibrationDevice VibrationDevice.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=VibrationDevice%20VibrationDevice.GetDefault%28%29");
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
