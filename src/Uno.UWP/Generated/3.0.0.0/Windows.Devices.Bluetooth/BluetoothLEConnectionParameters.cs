#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEConnectionParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort ConnectionInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEConnectionParameters.ConnectionInterval is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ushort%20BluetoothLEConnectionParameters.ConnectionInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort ConnectionLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEConnectionParameters.ConnectionLatency is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ushort%20BluetoothLEConnectionParameters.ConnectionLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort LinkTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEConnectionParameters.LinkTimeout is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ushort%20BluetoothLEConnectionParameters.LinkTimeout");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionParameters.LinkTimeout.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionParameters.ConnectionLatency.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEConnectionParameters.ConnectionInterval.get
	}
}
