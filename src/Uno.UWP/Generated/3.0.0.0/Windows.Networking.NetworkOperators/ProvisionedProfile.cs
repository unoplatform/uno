#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProvisionedProfile 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void UpdateCost( global::Windows.Networking.Connectivity.NetworkCostType value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.ProvisionedProfile", "void ProvisionedProfile.UpdateCost(NetworkCostType value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void UpdateUsage( global::Windows.Networking.NetworkOperators.ProfileUsage value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.ProvisionedProfile", "void ProvisionedProfile.UpdateUsage(ProfileUsage value)");
		}
		#endif
	}
}
