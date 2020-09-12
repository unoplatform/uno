#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AttributedNetworkUsage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AttributionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AttributedNetworkUsage.AttributionId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AttributionName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AttributedNetworkUsage.AttributionName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamReference AttributionThumbnail
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStreamReference AttributedNetworkUsage.AttributionThumbnail is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong BytesReceived
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AttributedNetworkUsage.BytesReceived is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong BytesSent
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AttributedNetworkUsage.BytesSent is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.AttributedNetworkUsage.BytesSent.get
		// Forced skipping of method Windows.Networking.Connectivity.AttributedNetworkUsage.BytesReceived.get
		// Forced skipping of method Windows.Networking.Connectivity.AttributedNetworkUsage.AttributionId.get
		// Forced skipping of method Windows.Networking.Connectivity.AttributedNetworkUsage.AttributionName.get
		// Forced skipping of method Windows.Networking.Connectivity.AttributedNetworkUsage.AttributionThumbnail.get
	}
}
