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
				throw new global::System.NotImplementedException("The member uint NetworkAdapter.IanaInterfaceType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20NetworkAdapter.IanaInterfaceType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong InboundMaxBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong NetworkAdapter.InboundMaxBitsPerSecond is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20NetworkAdapter.InboundMaxBitsPerSecond");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid NetworkAdapterId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid NetworkAdapter.NetworkAdapterId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20NetworkAdapter.NetworkAdapterId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkItem NetworkItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkItem NetworkAdapter.NetworkItem is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=NetworkItem%20NetworkAdapter.NetworkItem");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong OutboundMaxBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong NetworkAdapter.OutboundMaxBitsPerSecond is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20NetworkAdapter.OutboundMaxBitsPerSecond");
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
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConnectionProfile> NetworkAdapter.GetConnectedProfileAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CConnectionProfile%3E%20NetworkAdapter.GetConnectedProfileAsync%28%29");
		}
		#endif
	}
}
