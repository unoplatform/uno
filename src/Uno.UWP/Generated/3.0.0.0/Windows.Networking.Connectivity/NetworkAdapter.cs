#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkAdapter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint IanaInterfaceType
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint NetworkAdapter.IanaInterfaceType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong InboundMaxBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong NetworkAdapter.InboundMaxBitsPerSecond is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid NetworkAdapterId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid NetworkAdapter.NetworkAdapterId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkItem NetworkItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkItem NetworkAdapter.NetworkItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong OutboundMaxBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong NetworkAdapter.OutboundMaxBitsPerSecond is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.NetworkAdapter.OutboundMaxBitsPerSecond.get
		// Forced skipping of method Windows.Networking.Connectivity.NetworkAdapter.InboundMaxBitsPerSecond.get
		// Forced skipping of method Windows.Networking.Connectivity.NetworkAdapter.IanaInterfaceType.get
		// Forced skipping of method Windows.Networking.Connectivity.NetworkAdapter.NetworkItem.get
		// Forced skipping of method Windows.Networking.Connectivity.NetworkAdapter.NetworkAdapterId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Connectivity.ConnectionProfile> GetConnectedProfileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConnectionProfile> NetworkAdapter.GetConnectedProfileAsync() is not implemented in Uno.");
		}
		#endif
	}
}
