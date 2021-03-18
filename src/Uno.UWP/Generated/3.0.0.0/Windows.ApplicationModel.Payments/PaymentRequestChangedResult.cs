#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentRequestChangedResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentDetails UpdatedPaymentDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentDetails PaymentRequestChangedResult.UpdatedPaymentDetails is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedResult", "PaymentDetails PaymentRequestChangedResult.UpdatedPaymentDetails");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentRequestChangedResult.Message is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedResult", "string PaymentRequestChangedResult.Message");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ChangeAcceptedByMerchant
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PaymentRequestChangedResult.ChangeAcceptedByMerchant is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedResult", "bool PaymentRequestChangedResult.ChangeAcceptedByMerchant");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentRequestChangedResult( bool changeAcceptedByMerchant) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedResult", "PaymentRequestChangedResult.PaymentRequestChangedResult(bool changeAcceptedByMerchant)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.PaymentRequestChangedResult(bool)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentRequestChangedResult( bool changeAcceptedByMerchant,  global::Windows.ApplicationModel.Payments.PaymentDetails updatedPaymentDetails) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedResult", "PaymentRequestChangedResult.PaymentRequestChangedResult(bool changeAcceptedByMerchant, PaymentDetails updatedPaymentDetails)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.PaymentRequestChangedResult(bool, Windows.ApplicationModel.Payments.PaymentDetails)
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.ChangeAcceptedByMerchant.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.ChangeAcceptedByMerchant.set
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.Message.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.Message.set
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.UpdatedPaymentDetails.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedResult.UpdatedPaymentDetails.set
	}
}
