#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceServicingTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DeviceServicingTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.DeviceServicingTrigger", "DeviceServicingTrigger.DeviceServicingTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceServicingTrigger.DeviceServicingTrigger()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.DeviceTriggerResult> RequestAsync( string deviceId,  global::System.TimeSpan expectedDuration)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceTriggerResult> DeviceServicingTrigger.RequestAsync(string deviceId, TimeSpan expectedDuration) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDeviceTriggerResult%3E%20DeviceServicingTrigger.RequestAsync%28string%20deviceId%2C%20TimeSpan%20expectedDuration%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.DeviceTriggerResult> RequestAsync( string deviceId,  global::System.TimeSpan expectedDuration,  string arguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceTriggerResult> DeviceServicingTrigger.RequestAsync(string deviceId, TimeSpan expectedDuration, string arguments) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDeviceTriggerResult%3E%20DeviceServicingTrigger.RequestAsync%28string%20deviceId%2C%20TimeSpan%20expectedDuration%2C%20string%20arguments%29");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
