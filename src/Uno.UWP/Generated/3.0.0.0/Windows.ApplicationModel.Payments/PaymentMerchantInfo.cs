#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentMerchantInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PackageFullName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentMerchantInfo.PackageFullName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri PaymentMerchantInfo.Uri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentMerchantInfo( global::System.Uri uri) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentMerchantInfo", "PaymentMerchantInfo.PaymentMerchantInfo(Uri uri)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMerchantInfo.PaymentMerchantInfo(System.Uri)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentMerchantInfo() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentMerchantInfo", "PaymentMerchantInfo.PaymentMerchantInfo()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMerchantInfo.PaymentMerchantInfo()
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMerchantInfo.PackageFullName.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMerchantInfo.Uri.get
	}
}
