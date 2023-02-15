#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Rfcomm
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RfcommDeviceServicesResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothError RfcommDeviceServicesResult.Error is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothError%20RfcommDeviceServicesResult.Error");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService> Services
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<RfcommDeviceService> RfcommDeviceServicesResult.Services is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CRfcommDeviceService%3E%20RfcommDeviceServicesResult.Services");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceServicesResult.Error.get
		// Forced skipping of method Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceServicesResult.Services.get
	}
}
