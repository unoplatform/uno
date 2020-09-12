#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemDiscoveryTypeFilter : global::Windows.System.RemoteSystems.IRemoteSystemFilter
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemDiscoveryType RemoteSystemDiscoveryType
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemDiscoveryType RemoteSystemDiscoveryTypeFilter.RemoteSystemDiscoveryType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemDiscoveryTypeFilter( global::Windows.System.RemoteSystems.RemoteSystemDiscoveryType discoveryType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemDiscoveryTypeFilter", "RemoteSystemDiscoveryTypeFilter.RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType discoveryType)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemDiscoveryTypeFilter.RemoteSystemDiscoveryTypeFilter(Windows.System.RemoteSystems.RemoteSystemDiscoveryType)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemDiscoveryTypeFilter.RemoteSystemDiscoveryType.get
		// Processing: Windows.System.RemoteSystems.IRemoteSystemFilter
	}
}
