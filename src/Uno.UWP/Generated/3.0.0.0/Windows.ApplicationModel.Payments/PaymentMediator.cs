#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentMediator 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentMediator() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentMediator", "PaymentMediator.PaymentMediator()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMediator.PaymentMediator()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> GetSupportedMethodIdsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> PaymentMediator.GetSupportedMethodIdsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentRequestSubmitResult> SubmitPaymentRequestAsync( global::Windows.ApplicationModel.Payments.PaymentRequest paymentRequest)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestSubmitResult> PaymentMediator.SubmitPaymentRequestAsync(PaymentRequest paymentRequest) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentRequestSubmitResult> SubmitPaymentRequestAsync( global::Windows.ApplicationModel.Payments.PaymentRequest paymentRequest,  global::Windows.ApplicationModel.Payments.PaymentRequestChangedHandler changeHandler)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestSubmitResult> PaymentMediator.SubmitPaymentRequestAsync(PaymentRequest paymentRequest, PaymentRequestChangedHandler changeHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentCanMakePaymentResult> CanMakePaymentAsync( global::Windows.ApplicationModel.Payments.PaymentRequest paymentRequest)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentCanMakePaymentResult> PaymentMediator.CanMakePaymentAsync(PaymentRequest paymentRequest) is not implemented in Uno.");
		}
		#endif
	}
}
