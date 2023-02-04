#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEConnectionPhy 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEConnectionPhyInfo ReceiveInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEConnectionPhyInfo BluetoothLEConnectionPhy.ReceiveInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothLEConnectionPhyInfo%20BluetoothLEConnectionPhy.ReceiveInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEConnectionPhyInfo TransmitInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEConnectionPhyInfo BluetoothLEConnectionPhy.TransmitInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothLEConnectionPhyInfo%20BluetoothLEConnectionPhy.TransmitInfo");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionPhy.TransmitInfo.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionPhy.ReceiveInfo.get
	}
}
