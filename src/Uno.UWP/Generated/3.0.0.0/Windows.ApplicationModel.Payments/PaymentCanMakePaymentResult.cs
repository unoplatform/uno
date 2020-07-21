#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentCanMakePaymentResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentCanMakePaymentResultStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentCanMakePaymentResultStatus PaymentCanMakePaymentResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentCanMakePaymentResult( global::Windows.ApplicationModel.Payments.PaymentCanMakePaymentResultStatus value) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentCanMakePaymentResult", "PaymentCanMakePaymentResult.PaymentCanMakePaymentResult(PaymentCanMakePaymentResultStatus value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentCanMakePaymentResult.PaymentCanMakePaymentResult(Windows.ApplicationModel.Payments.PaymentCanMakePaymentResultStatus)
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentCanMakePaymentResult.Status.get
	}
}
