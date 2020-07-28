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
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerPhoneNumber is not implemented in Uno.");
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
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerName is not implemented in Uno.");
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
				throw new global::System.NotImplementedException("The member string PaymentTransaction.PayerEmail is not implemented in Uno.");
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
				throw new global::System.NotImplementedException("The member PaymentRequest PaymentTransaction.PaymentRequest is not implemented in Uno.");
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
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestChangedResult> PaymentTransaction.UpdateShippingAddressAsync(PaymentAddress shippingAddress) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.PaymentRequestChangedResult> UpdateSelectedShippingOptionAsync( global::Windows.ApplicationModel.Payments.PaymentShippingOption selectedShippingOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentRequestChangedResult> PaymentTransaction.UpdateSelectedShippingOptionAsync(PaymentShippingOption selectedShippingOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Payments.Provider.PaymentTransactionAcceptResult> AcceptAsync( global::Windows.ApplicationModel.Payments.PaymentToken paymentToken)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentTransactionAcceptResult> PaymentTransaction.AcceptAsync(PaymentToken paymentToken) is not implemented in Uno.");
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
			throw new global::System.NotImplementedException("The member IAsyncOperation<PaymentTransaction> PaymentTransaction.FromIdAsync(string id) is not implemented in Uno.");
		}
		#endif
	}
}
