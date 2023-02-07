#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentTransaction 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PayerPhoneNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerPhoneNumber is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PaymentTransaction.PayerPhoneNumber");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.Provider.PaymentTransaction", "string PaymentTransaction.PayerPhoneNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PayerName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PaymentTransaction.PayerName");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.Provider.PaymentTransaction", "string PaymentTransaction.PayerName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PayerEmail
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerEmail is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PaymentTransaction.PayerEmail");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.Provider.PaymentTransaction", "string PaymentTransaction.PayerEmail");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentRequest PaymentRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentRequest PaymentTransaction.PaymentRequest is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PaymentRequest%20PaymentTransaction.PaymentRequest");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PaymentRequest.get
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerEmail.get
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerEmail.set
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerName.get
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerName.set
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerPhoneNumber.get
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentTransaction.PayerPhoneNumber.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentRequestChangedResult> UpdateShippingAddressAsync( global::Windows.ApplicationModel.Payments.PaymentAddress shippingAddress)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestChangedResult> PaymentTransaction.UpdateShippingAddressAsync(PaymentAddress shippingAddress) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPaymentRequestChangedResult%3E%20PaymentTransaction.UpdateShippingAddressAsync%28PaymentAddress%20shippingAddress%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentRequestChangedResult> UpdateSelectedShippingOptionAsync( global::Windows.ApplicationModel.Payments.PaymentShippingOption selectedShippingOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestChangedResult> PaymentTransaction.UpdateSelectedShippingOptionAsync(PaymentShippingOption selectedShippingOption) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPaymentRequestChangedResult%3E%20PaymentTransaction.UpdateSelectedShippingOptionAsync%28PaymentShippingOption%20selectedShippingOption%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.Provider.PaymentTransactionAcceptResult> AcceptAsync( global::Windows.ApplicationModel.Payments.PaymentToken paymentToken)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentTransactionAcceptResult> PaymentTransaction.AcceptAsync(PaymentToken paymentToken) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPaymentTransactionAcceptResult%3E%20PaymentTransaction.AcceptAsync%28PaymentToken%20paymentToken%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reject()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.Provider.PaymentTransaction", "void PaymentTransaction.Reject()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.Provider.PaymentTransaction> FromIdAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentTransaction> PaymentTransaction.FromIdAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPaymentTransaction%3E%20PaymentTransaction.FromIdAsync%28string%20id%29");
		}
		#endif
	}
}
