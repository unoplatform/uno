#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGameControllerProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Gaming.Input.Custom.GameControllerVersionInfo FirmwareVersionInfo
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		ushort HardwareProductId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		ushort HardwareVendorId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Gaming.Input.Custom.GameControllerVersionInfo HardwareVersionInfo
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsConnected
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Custom.IGameControllerProvider.FirmwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.IGameControllerProvider.HardwareProductId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.IGameControllerProvider.HardwareVendorId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.IGameControllerProvider.HardwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.IGameControllerProvider.IsConnected.get
	}
}
