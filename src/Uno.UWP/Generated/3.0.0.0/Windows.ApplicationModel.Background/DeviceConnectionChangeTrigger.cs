#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceConnectionChangeTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool MaintainConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceConnectionChangeTrigger.MaintainConnection is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger", "bool DeviceConnectionChangeTrigger.MaintainConnection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanMaintainConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DeviceConnectionChangeTrigger.CanMaintainConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DeviceConnectionChangeTrigger.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger.DeviceId.get
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger.CanMaintainConnection.get
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger.MaintainConnection.get
		// Forced skipping of method Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger.MaintainConnection.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.DeviceConnectionChangeTrigger> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceConnectionChangeTrigger> DeviceConnectionChangeTrigger.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
