#if !__ANDROID__ && !__APPLE_UIKIT__ && !__WASM__
using System;

namespace Windows.Phone.Devices.Notification
{
	/// <summary>
	/// Stub for platforms where VibrationDevice is not natively supported.
	/// The generated stub was removed because the type is no longer part of the WinUI API surface.
	/// </summary>
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public partial class VibrationDevice
	{
		private VibrationDevice()
		{
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static VibrationDevice GetDefault()
		{
			throw new global::System.NotImplementedException("The member VibrationDevice VibrationDevice.GetDefault() is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=VibrationDevice%20VibrationDevice.GetDefault%28%29");
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public void Vibrate(TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Vibrate(TimeSpan duration)");
		}

		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
		public void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Cancel()");
		}
	}
}
#endif
