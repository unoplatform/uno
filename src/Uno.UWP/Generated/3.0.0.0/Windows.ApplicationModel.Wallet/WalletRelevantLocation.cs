#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WalletRelevantLocation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.BasicGeoposition Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition WalletRelevantLocation.Position is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.WalletRelevantLocation", "BasicGeoposition WalletRelevantLocation.Position");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WalletRelevantLocation.DisplayMessage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.WalletRelevantLocation", "string WalletRelevantLocation.DisplayMessage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WalletRelevantLocation() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.WalletRelevantLocation", "WalletRelevantLocation.WalletRelevantLocation()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletRelevantLocation.WalletRelevantLocation()
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletRelevantLocation.Position.get
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletRelevantLocation.Position.set
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletRelevantLocation.DisplayMessage.get
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletRelevantLocation.DisplayMessage.set
	}
}
