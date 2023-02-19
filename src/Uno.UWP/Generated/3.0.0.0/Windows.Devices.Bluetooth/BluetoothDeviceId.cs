#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothDeviceId 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string BluetoothDeviceId.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20BluetoothDeviceId.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsClassicDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothDeviceId.IsClassicDevice is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BluetoothDeviceId.IsClassicDevice");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsLowEnergyDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothDeviceId.IsLowEnergyDevice is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BluetoothDeviceId.IsLowEnergyDevice");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothDeviceId.Id.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothDeviceId.IsClassicDevice.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothDeviceId.IsLowEnergyDevice.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Bluetooth.BluetoothDeviceId FromId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member BluetoothDeviceId BluetoothDeviceId.FromId(string deviceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothDeviceId%20BluetoothDeviceId.FromId%28string%20deviceId%29");
		}
		#endif
	}
}
