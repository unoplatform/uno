#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandDeviceService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid DeviceServiceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid MobileBroadbandDeviceService.DeviceServiceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20MobileBroadbandDeviceService.DeviceServiceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<uint> SupportedCommands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<uint> MobileBroadbandDeviceService.SupportedCommands is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cuint%3E%20MobileBroadbandDeviceService.SupportedCommands");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceService.DeviceServiceId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceService.SupportedCommands.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession OpenDataSession()
		{
			throw new global::System.NotImplementedException("The member MobileBroadbandDeviceServiceDataSession MobileBroadbandDeviceService.OpenDataSession() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MobileBroadbandDeviceServiceDataSession%20MobileBroadbandDeviceService.OpenDataSession%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceCommandSession OpenCommandSession()
		{
			throw new global::System.NotImplementedException("The member MobileBroadbandDeviceServiceCommandSession MobileBroadbandDeviceService.OpenCommandSession() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MobileBroadbandDeviceServiceCommandSession%20MobileBroadbandDeviceService.OpenCommandSession%28%29");
		}
		#endif
	}
}
