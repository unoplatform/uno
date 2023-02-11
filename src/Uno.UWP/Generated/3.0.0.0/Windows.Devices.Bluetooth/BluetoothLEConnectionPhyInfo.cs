#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEConnectionPhyInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCodedPhy
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothLEConnectionPhyInfo.IsCodedPhy is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BluetoothLEConnectionPhyInfo.IsCodedPhy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUncoded1MPhy
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothLEConnectionPhyInfo.IsUncoded1MPhy is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BluetoothLEConnectionPhyInfo.IsUncoded1MPhy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUncoded2MPhy
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BluetoothLEConnectionPhyInfo.IsUncoded2MPhy is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BluetoothLEConnectionPhyInfo.IsUncoded2MPhy");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionPhyInfo.IsUncoded1MPhy.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionPhyInfo.IsUncoded2MPhy.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionPhyInfo.IsCodedPhy.get
	}
}
