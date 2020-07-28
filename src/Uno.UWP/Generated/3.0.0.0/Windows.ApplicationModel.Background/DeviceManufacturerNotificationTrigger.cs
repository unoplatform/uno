#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceManufacturerNotificationTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool OneShot
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceManufacturerNotificationTrigger.OneShot is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TriggerQualifier
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DeviceManufacturerNotificationTrigger.TriggerQualifier is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DeviceManufacturerNotificationTrigger( string triggerQualifier,  bool oneShot) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.DeviceManufacturerNotificationTrigger", "DeviceManufacturerNotificationTrigger.DeviceManufacturerNotificationTrigger(string triggerQualifier, bool oneShot)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceManufacturerNotificationTrigger.DeviceManufacturerNotificationTrigger(string, bool)
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceManufacturerNotificationTrigger.TriggerQualifier.get
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceManufacturerNotificationTrigger.OneShot.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
