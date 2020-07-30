#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if false || false || NET461 || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConnectionProfile 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkAdapter NetworkAdapter
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkAdapter ConnectionProfile.NetworkAdapter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkSecuritySettings NetworkSecuritySettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkSecuritySettings ConnectionProfile.NetworkSecuritySettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProfileName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ConnectionProfile.ProfileName is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false || false || false
		[global::Uno.NotImplemented("NET461", "__WASM__")]
		public  bool IsWlanConnectionProfile
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionProfile.IsWlanConnectionProfile is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false || false || false
		[global::Uno.NotImplemented("NET461", "__WASM__")]
		public  bool IsWwanConnectionProfile
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionProfile.IsWwanConnectionProfile is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid? ServiceProviderGuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid? ConnectionProfile.ServiceProviderGuid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.WlanConnectionProfileDetails WlanConnectionProfileDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member WlanConnectionProfileDetails ConnectionProfile.WlanConnectionProfileDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.WwanConnectionProfileDetails WwanConnectionProfileDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member WwanConnectionProfileDetails ConnectionProfile.WwanConnectionProfileDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanDelete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionProfile.CanDelete is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.ProfileName.get
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.Networking.Connectivity.NetworkConnectivityLevel GetNetworkConnectivityLevel()
		{
			throw new global::System.NotImplementedException("The member NetworkConnectivityLevel ConnectionProfile.GetNetworkConnectivityLevel() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> GetNetworkNames()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> ConnectionProfile.GetNetworkNames() is not implemented in Uno.");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.ConnectionCost GetConnectionCost()
		{
			throw new global::System.NotImplementedException("The member ConnectionCost ConnectionProfile.GetConnectionCost() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.DataPlanStatus GetDataPlanStatus()
		{
			throw new global::System.NotImplementedException("The member DataPlanStatus ConnectionProfile.GetDataPlanStatus() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.NetworkAdapter.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.DataUsage GetLocalUsage( global::System.DateTimeOffset StartTime,  global::System.DateTimeOffset EndTime)
		{
			throw new global::System.NotImplementedException("The member DataUsage ConnectionProfile.GetLocalUsage(DateTimeOffset StartTime, DateTimeOffset EndTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.DataUsage GetLocalUsage( global::System.DateTimeOffset StartTime,  global::System.DateTimeOffset EndTime,  global::Windows.Networking.Connectivity.RoamingStates States)
		{
			throw new global::System.NotImplementedException("The member DataUsage ConnectionProfile.GetLocalUsage(DateTimeOffset StartTime, DateTimeOffset EndTime, RoamingStates States) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.NetworkSecuritySettings.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.IsWwanConnectionProfile.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.IsWlanConnectionProfile.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.WwanConnectionProfileDetails.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.WlanConnectionProfileDetails.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.ServiceProviderGuid.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? GetSignalBars()
		{
			throw new global::System.NotImplementedException("The member byte? ConnectionProfile.GetSignalBars() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.DomainConnectivityLevel GetDomainConnectivityLevel()
		{
			throw new global::System.NotImplementedException("The member DomainConnectivityLevel ConnectionProfile.GetDomainConnectivityLevel() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.NetworkUsage>> GetNetworkUsageAsync( global::System.DateTimeOffset startTime,  global::System.DateTimeOffset endTime,  global::Windows.Networking.Connectivity.DataUsageGranularity granularity,  global::Windows.Networking.Connectivity.NetworkUsageStates states)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<NetworkUsage>> ConnectionProfile.GetNetworkUsageAsync(DateTimeOffset startTime, DateTimeOffset endTime, DataUsageGranularity granularity, NetworkUsageStates states) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.ConnectivityInterval>> GetConnectivityIntervalsAsync( global::System.DateTimeOffset startTime,  global::System.DateTimeOffset endTime,  global::Windows.Networking.Connectivity.NetworkUsageStates states)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ConnectivityInterval>> ConnectionProfile.GetConnectivityIntervalsAsync(DateTimeOffset startTime, DateTimeOffset endTime, NetworkUsageStates states) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.AttributedNetworkUsage>> GetAttributedNetworkUsageAsync( global::System.DateTimeOffset startTime,  global::System.DateTimeOffset endTime,  global::Windows.Networking.Connectivity.NetworkUsageStates states)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<AttributedNetworkUsage>> ConnectionProfile.GetAttributedNetworkUsageAsync(DateTimeOffset startTime, DateTimeOffset endTime, NetworkUsageStates states) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.ProviderNetworkUsage>> GetProviderNetworkUsageAsync( global::System.DateTimeOffset startTime,  global::System.DateTimeOffset endTime,  global::Windows.Networking.Connectivity.NetworkUsageStates states)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ProviderNetworkUsage>> ConnectionProfile.GetProviderNetworkUsageAsync(DateTimeOffset startTime, DateTimeOffset endTime, NetworkUsageStates states) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionProfile.CanDelete.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Connectivity.ConnectionProfileDeleteStatus> TryDeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConnectionProfileDeleteStatus> ConnectionProfile.TryDeleteAsync() is not implemented in Uno.");
		}
		#endif
	}
}
