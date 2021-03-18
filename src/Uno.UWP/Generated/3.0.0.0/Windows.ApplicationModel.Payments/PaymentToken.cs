#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentToken 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string JsonDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentToken.JsonDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PaymentMethodId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentToken.PaymentMethodId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentToken( string paymentMethodId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentToken", "PaymentToken.PaymentToken(string paymentMethodId)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentToken.PaymentToken(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentToken( string paymentMethodId,  string jsonDetails) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentToken", "PaymentToken.PaymentToken(string paymentMethodId, string jsonDetails)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentToken.PaymentToken(string, string)
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentToken.PaymentMethodId.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentToken.JsonDetails.get
	}
}
