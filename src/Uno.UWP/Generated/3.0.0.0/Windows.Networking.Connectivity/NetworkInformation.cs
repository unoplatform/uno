#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if false || false || NET461 || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.ConnectionProfile>> FindConnectionProfilesAsync( global::Windows.Networking.Connectivity.ConnectionProfileFilter pProfileFilter)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ConnectionProfile>> NetworkInformation.FindConnectionProfilesAsync(ConnectionProfileFilter pProfileFilter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.ConnectionProfile> GetConnectionProfiles()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ConnectionProfile> NetworkInformation.GetConnectionProfiles() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static global::Windows.Networking.Connectivity.ConnectionProfile GetInternetConnectionProfile()
		{
			throw new global::System.NotImplementedException("The member ConnectionProfile NetworkInformation.GetInternetConnectionProfile() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.LanIdentifier> GetLanIdentifiers()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<LanIdentifier> NetworkInformation.GetLanIdentifiers() is not implemented in Uno.");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> GetHostNames()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<HostName> NetworkInformation.GetHostNames() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Connectivity.ProxyConfiguration> GetProxyConfigurationAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ProxyConfiguration> NetworkInformation.GetProxyConfigurationAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair> GetSortedEndpointPairs( global::System.Collections.Generic.IEnumerable<global::Windows.Networking.EndpointPair> destinationList,  global::Windows.Networking.HostNameSortOptions sortOptions)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<EndpointPair> NetworkInformation.GetSortedEndpointPairs(IEnumerable<EndpointPair> destinationList, HostNameSortOptions sortOptions) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged.add
		// Forced skipping of method Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged.remove
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static event global::Windows.Networking.Connectivity.NetworkStatusChangedEventHandler NetworkStatusChanged
		{
			[global::Uno.NotImplemented("NET461")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Connectivity.NetworkInformation", "event NetworkStatusChangedEventHandler NetworkInformation.NetworkStatusChanged");
			}
			[global::Uno.NotImplemented("NET461")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Connectivity.NetworkInformation", "event NetworkStatusChangedEventHandler NetworkInformation.NetworkStatusChanged");
			}
		}
		#endif
	}
}
