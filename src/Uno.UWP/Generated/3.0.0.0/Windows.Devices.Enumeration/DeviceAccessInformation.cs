#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceAccessInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceAccessStatus CurrentStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceAccessStatus DeviceAccessInformation.CurrentStatus is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceAccessInformation.AccessChanged.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceAccessInformation.AccessChanged.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceAccessInformation.CurrentStatus.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceAccessInformation CreateFromId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member DeviceAccessInformation DeviceAccessInformation.CreateFromId(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceAccessInformation CreateFromDeviceClassId( global::System.Guid deviceClassId)
		{
			throw new global::System.NotImplementedException("The member DeviceAccessInformation DeviceAccessInformation.CreateFromDeviceClassId(Guid deviceClassId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceAccessInformation CreateFromDeviceClass( global::Windows.Devices.Enumeration.DeviceClass deviceClass)
		{
			throw new global::System.NotImplementedException("The member DeviceAccessInformation DeviceAccessInformation.CreateFromDeviceClass(DeviceClass deviceClass) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Enumeration.DeviceAccessInformation, global::Windows.Devices.Enumeration.DeviceAccessChangedEventArgs> AccessChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceAccessInformation", "event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> DeviceAccessInformation.AccessChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceAccessInformation", "event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> DeviceAccessInformation.AccessChanged");
			}
		}
		#endif
	}
}
