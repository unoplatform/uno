#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkOperatorTetheringAccessPointConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Ssid
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NetworkOperatorTetheringAccessPointConfiguration.Ssid is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration", "string NetworkOperatorTetheringAccessPointConfiguration.Ssid");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Passphrase
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NetworkOperatorTetheringAccessPointConfiguration.Passphrase is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration", "string NetworkOperatorTetheringAccessPointConfiguration.Passphrase");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public NetworkOperatorTetheringAccessPointConfiguration() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration", "NetworkOperatorTetheringAccessPointConfiguration.NetworkOperatorTetheringAccessPointConfiguration()");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration.NetworkOperatorTetheringAccessPointConfiguration()
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration.Ssid.get
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration.Ssid.set
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration.Passphrase.get
		// Forced skipping of method Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration.Passphrase.set
	}
}
