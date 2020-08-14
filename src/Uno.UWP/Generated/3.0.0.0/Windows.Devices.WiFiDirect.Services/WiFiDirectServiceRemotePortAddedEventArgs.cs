#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectServiceRemotePortAddedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair> EndpointPairs
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<EndpointPair> WiFiDirectServiceRemotePortAddedEventArgs.EndpointPairs is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceIPProtocol Protocol
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceIPProtocol WiFiDirectServiceRemotePortAddedEventArgs.Protocol is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceRemotePortAddedEventArgs.EndpointPairs.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceRemotePortAddedEventArgs.Protocol.get
	}
}
