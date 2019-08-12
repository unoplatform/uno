#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandAccountUpdatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool HasDeviceInformationChanged
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MobileBroadbandAccountUpdatedEventArgs.HasDeviceInformationChanged is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool HasNetworkChanged
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MobileBroadbandAccountUpdatedEventArgs.HasNetworkChanged is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string NetworkAccountId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandAccountUpdatedEventArgs.NetworkAccountId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccountUpdatedEventArgs.NetworkAccountId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccountUpdatedEventArgs.HasDeviceInformationChanged.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandAccountUpdatedEventArgs.HasNetworkChanged.get
	}
}
