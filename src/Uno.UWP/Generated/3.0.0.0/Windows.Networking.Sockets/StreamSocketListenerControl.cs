#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamSocketListenerControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketQualityOfService QualityOfService
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketQualityOfService StreamSocketListenerControl.QualityOfService is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListenerControl", "SocketQualityOfService StreamSocketListenerControl.QualityOfService");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte OutboundUnicastHopLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte StreamSocketListenerControl.OutboundUnicastHopLimit is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListenerControl", "byte StreamSocketListenerControl.OutboundUnicastHopLimit");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint OutboundBufferSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StreamSocketListenerControl.OutboundBufferSizeInBytes is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListenerControl", "uint StreamSocketListenerControl.OutboundBufferSizeInBytes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool NoDelay
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StreamSocketListenerControl.NoDelay is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListenerControl", "bool StreamSocketListenerControl.NoDelay");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool KeepAlive
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StreamSocketListenerControl.KeepAlive is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListenerControl", "bool StreamSocketListenerControl.KeepAlive");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.QualityOfService.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.QualityOfService.set
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.NoDelay.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.NoDelay.set
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.KeepAlive.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.KeepAlive.set
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.OutboundBufferSizeInBytes.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.OutboundBufferSizeInBytes.set
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.OutboundUnicastHopLimit.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerControl.OutboundUnicastHopLimit.set
	}
}
