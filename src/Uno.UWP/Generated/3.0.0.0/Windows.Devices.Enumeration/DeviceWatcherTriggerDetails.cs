#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceWatcherTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Enumeration.DeviceWatcherEvent> DeviceWatcherEvents
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DeviceWatcherEvent> DeviceWatcherTriggerDetails.DeviceWatcherEvents is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CDeviceWatcherEvent%3E%20DeviceWatcherTriggerDetails.DeviceWatcherEvents");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcherTriggerDetails.DeviceWatcherEvents.get
	}
}
