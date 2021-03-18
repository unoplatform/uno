#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RfcommConnectionTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName RemoteHostName
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName RfcommConnectionTrigger.RemoteHostName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.RfcommConnectionTrigger", "HostName RfcommConnectionTrigger.RemoteHostName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketProtectionLevel ProtectionLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketProtectionLevel RfcommConnectionTrigger.ProtectionLevel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.RfcommConnectionTrigger", "SocketProtectionLevel RfcommConnectionTrigger.ProtectionLevel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowMultipleConnections
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RfcommConnectionTrigger.AllowMultipleConnections is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.RfcommConnectionTrigger", "bool RfcommConnectionTrigger.AllowMultipleConnections");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Background.RfcommInboundConnectionInformation InboundConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member RfcommInboundConnectionInformation RfcommConnectionTrigger.InboundConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Background.RfcommOutboundConnectionInformation OutboundConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member RfcommOutboundConnectionInformation RfcommConnectionTrigger.OutboundConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RfcommConnectionTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.RfcommConnectionTrigger", "RfcommConnectionTrigger.RfcommConnectionTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.RfcommConnectionTrigger()
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.InboundConnection.get
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.OutboundConnection.get
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.AllowMultipleConnections.get
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.AllowMultipleConnections.set
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.ProtectionLevel.get
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.ProtectionLevel.set
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.RemoteHostName.get
		// Forced skipping of method Windows.ApplicationModel.Background.RfcommConnectionTrigger.RemoteHostName.set
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
