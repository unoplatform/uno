#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ServerStreamWebSocketInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.BandwidthStatistics BandwidthStatistics
		{
			get
			{
				throw new global::System.NotImplementedException("The member BandwidthStatistics ServerStreamWebSocketInformation.BandwidthStatistics is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName LocalAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName ServerStreamWebSocketInformation.LocalAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Protocol
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ServerStreamWebSocketInformation.Protocol is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.ServerStreamWebSocketInformation.BandwidthStatistics.get
		// Forced skipping of method Windows.Networking.Sockets.ServerStreamWebSocketInformation.Protocol.get
		// Forced skipping of method Windows.Networking.Sockets.ServerStreamWebSocketInformation.LocalAddress.get
	}
}
