#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceWatcherEvent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation DeviceInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation DeviceWatcherEvent.DeviceInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformationUpdate DeviceInformationUpdate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformationUpdate DeviceWatcherEvent.DeviceInformationUpdate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceWatcherEventKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceWatcherEventKind DeviceWatcherEvent.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcherEvent.Kind.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcherEvent.DeviceInformation.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcherEvent.DeviceInformationUpdate.get
	}
}
