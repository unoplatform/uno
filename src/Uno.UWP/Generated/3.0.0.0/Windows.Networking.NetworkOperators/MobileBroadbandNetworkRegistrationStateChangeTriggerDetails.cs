#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandNetworkRegistrationStateChangeTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.NetworkOperators.MobileBroadbandNetworkRegistrationStateChange> NetworkRegistrationStateChanges
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MobileBroadbandNetworkRegistrationStateChange> MobileBroadbandNetworkRegistrationStateChangeTriggerDetails.NetworkRegistrationStateChanges is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CMobileBroadbandNetworkRegistrationStateChange%3E%20MobileBroadbandNetworkRegistrationStateChangeTriggerDetails.NetworkRegistrationStateChanges");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandNetworkRegistrationStateChangeTriggerDetails.NetworkRegistrationStateChanges.get
	}
}
