#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandDeviceServiceInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid DeviceServiceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid MobileBroadbandDeviceServiceInformation.DeviceServiceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDataReadSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MobileBroadbandDeviceServiceInformation.IsDataReadSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsDataWriteSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MobileBroadbandDeviceServiceInformation.IsDataWriteSupported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceInformation.DeviceServiceId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceInformation.IsDataReadSupported.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceInformation.IsDataWriteSupported.get
	}
}
