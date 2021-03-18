#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentRequestChangedArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentRequestChangeKind ChangeKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentRequestChangeKind PaymentRequestChangedArgs.ChangeKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentShippingOption SelectedShippingOption
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentShippingOption PaymentRequestChangedArgs.SelectedShippingOption is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentAddress ShippingAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentAddress PaymentRequestChangedArgs.ShippingAddress is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedArgs.ChangeKind.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedArgs.ShippingAddress.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentRequestChangedArgs.SelectedShippingOption.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Acknowledge( global::Windows.ApplicationModel.Payments.PaymentRequestChangedResult changeResult)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentRequestChangedArgs", "void PaymentRequestChangedArgs.Acknowledge(PaymentRequestChangedResult changeResult)");
		}
		#endif
	}
}
