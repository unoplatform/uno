#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandAccount 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceInformation CurrentDeviceInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandDeviceInformation MobileBroadbandAccount.CurrentDeviceInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandNetwork CurrentNetwork
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandNetwork MobileBroadbandAccount.CurrentNetwork is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string NetworkAccountId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandAccount.NetworkAccountId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ServiceProviderGuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid MobileBroadbandAccount.ServiceProviderGuid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ServiceProviderName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandAccount.ServiceProviderName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri AccountExperienceUrl
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri MobileBroadbandAccount.AccountExperienceUrl is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<string> AvailableNetworkAccountIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> MobileBroadbandAccount.AvailableNetworkAccountIds is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.NetworkAccountId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.ServiceProviderGuid.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.ServiceProviderName.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.CurrentNetwork.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.CurrentDeviceInformation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Connectivity.ConnectionProfile> GetConnectionProfiles()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ConnectionProfile> MobileBroadbandAccount.GetConnectionProfiles() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.AccountExperienceUrl.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccount.AvailableNetworkAccountIds.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.NetworkOperators.MobileBroadbandAccount CreateFromNetworkAccountId( string networkAccountId)
		{
			throw new global::System.NotImplementedException("The member MobileBroadbandAccount MobileBroadbandAccount.CreateFromNetworkAccountId(string networkAccountId) is not implemented in Uno.");
		}
		#endif
	}
}
