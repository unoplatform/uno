#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebSocketInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.Sockets.BandwidthStatistics BandwidthStatistics
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.HostName LocalAddress
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Protocol
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation.LocalAddress.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation.BandwidthStatistics.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation.Protocol.get
	}
}
