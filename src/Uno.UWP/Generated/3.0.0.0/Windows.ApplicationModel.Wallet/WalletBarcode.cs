#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WalletBarcode 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Wallet.WalletBarcodeSymbology Symbology
		{
			get
			{
				throw new global::System.NotImplementedException("The member WalletBarcodeSymbology WalletBarcode.Symbology is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WalletBarcode.Value is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WalletBarcode( global::Windows.ApplicationModel.Wallet.WalletBarcodeSymbology symbology,  string value) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.WalletBarcode", "WalletBarcode.WalletBarcode(WalletBarcodeSymbology symbology, string value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletBarcode.WalletBarcode(Windows.ApplicationModel.Wallet.WalletBarcodeSymbology, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WalletBarcode( global::Windows.Storage.Streams.IRandomAccessStreamReference streamToBarcodeImage) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.WalletBarcode", "WalletBarcode.WalletBarcode(IRandomAccessStreamReference streamToBarcodeImage)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletBarcode.WalletBarcode(Windows.Storage.Streams.IRandomAccessStreamReference)
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletBarcode.Symbology.get
		// Forced skipping of method Windows.ApplicationModel.Wallet.WalletBarcode.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamReference> GetImageAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamReference> WalletBarcode.GetImageAsync() is not implemented in Uno.");
		}
		#endif
	}
}
