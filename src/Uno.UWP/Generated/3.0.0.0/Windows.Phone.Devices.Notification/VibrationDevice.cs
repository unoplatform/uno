#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Devices.Notification
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VibrationDevice 
	{
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  void Vibrate( global::System.TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Vibrate(TimeSpan duration)");
		}
		#endif
		#if false || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Devices.Notification.VibrationDevice", "void VibrationDevice.Cancel()");
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Phone.Devices.Notification.VibrationDevice GetDefault()
		{
			throw new global::System.NotImplementedException("The member VibrationDevice VibrationDevice.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
