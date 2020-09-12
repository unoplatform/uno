#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkOperatorTetheringManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ClientCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint NetworkOperatorTetheringManager.ClientCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxClientCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint NetworkOperatorTetheringManager.MaxClientCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.TetheringOperationalState TetheringOperationalState
		{
			get
			{
				throw new global::System.NotImplementedException("The member TetheringOperationalState NetworkOperatorTetheringManager.TetheringOperationalState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager.MaxClientCount.get
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager.ClientCount.get
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager.TetheringOperationalState.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration GetCurrentAccessPointConfiguration()
		{
			throw new global::System.NotImplementedException("The member NetworkOperatorTetheringAccessPointConfiguration NetworkOperatorTetheringManager.GetCurrentAccessPointConfiguration() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConfigureAccessPointAsync( global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration configuration)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction NetworkOperatorTetheringManager.ConfigureAccessPointAsync(NetworkOperatorTetheringAccessPointConfiguration configuration) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringOperationResult> StartTetheringAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<NetworkOperatorTetheringOperationResult> NetworkOperatorTetheringManager.StartTetheringAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringOperationResult> StopTetheringAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<NetworkOperatorTetheringOperationResult> NetworkOperatorTetheringManager.StopTetheringAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringClient> GetTetheringClients()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<NetworkOperatorTetheringClient> NetworkOperatorTetheringManager.GetTetheringClients() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsNoConnectionsTimeoutEnabled()
		{
			throw new global::System.NotImplementedException("The member bool NetworkOperatorTetheringManager.IsNoConnectionsTimeoutEnabled() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void EnableNoConnectionsTimeout()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager", "void NetworkOperatorTetheringManager.EnableNoConnectionsTimeout()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction EnableNoConnectionsTimeoutAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction NetworkOperatorTetheringManager.EnableNoConnectionsTimeoutAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void DisableNoConnectionsTimeout()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager", "void NetworkOperatorTetheringManager.DisableNoConnectionsTimeout()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction DisableNoConnectionsTimeoutAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction NetworkOperatorTetheringManager.DisableNoConnectionsTimeoutAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager CreateFromConnectionProfile( global::Windows.Networking.Connectivity.ConnectionProfile profile,  global::Windows.Networking.Connectivity.NetworkAdapter adapter)
		{
			throw new global::System.NotImplementedException("The member NetworkOperatorTetheringManager NetworkOperatorTetheringManager.CreateFromConnectionProfile(ConnectionProfile profile, NetworkAdapter adapter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.TetheringCapability GetTetheringCapabilityFromConnectionProfile( global::Windows.Networking.Connectivity.ConnectionProfile profile)
		{
			throw new global::System.NotImplementedException("The member TetheringCapability NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(ConnectionProfile profile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager CreateFromConnectionProfile( global::Windows.Networking.Connectivity.ConnectionProfile profile)
		{
			throw new global::System.NotImplementedException("The member NetworkOperatorTetheringManager NetworkOperatorTetheringManager.CreateFromConnectionProfile(ConnectionProfile profile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.TetheringCapability GetTetheringCapability( string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member TetheringCapability NetworkOperatorTetheringManager.GetTetheringCapability(string networkAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager CreateFromNetworkAccountId( string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member NetworkOperatorTetheringManager NetworkOperatorTetheringManager.CreateFromNetworkAccountId(string networkAccountId) is not implemented in Uno.");
		}
		#endif
	}
}
