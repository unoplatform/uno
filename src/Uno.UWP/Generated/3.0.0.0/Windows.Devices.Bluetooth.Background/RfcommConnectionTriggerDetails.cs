#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RfcommConnectionTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Incoming
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RfcommConnectionTriggerDetails.Incoming is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothDevice RemoteDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothDevice RfcommConnectionTriggerDetails.RemoteDevice is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocket Socket
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocket RfcommConnectionTriggerDetails.Socket is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommConnectionTriggerDetails.Socket.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommConnectionTriggerDetails.Incoming.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.RfcommConnectionTriggerDetails.RemoteDevice.get
	}
}
