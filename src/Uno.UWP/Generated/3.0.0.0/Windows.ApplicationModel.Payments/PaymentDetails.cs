#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentItem Total
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentItem PaymentDetails.Total is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "PaymentItem PaymentDetails.Total");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Payments.PaymentShippingOption> ShippingOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PaymentShippingOption> PaymentDetails.ShippingOptions is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "IReadOnlyList<PaymentShippingOption> PaymentDetails.ShippingOptions");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Payments.PaymentDetailsModifier> Modifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PaymentDetailsModifier> PaymentDetails.Modifiers is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "IReadOnlyList<PaymentDetailsModifier> PaymentDetails.Modifiers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Payments.PaymentItem> DisplayItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PaymentItem> PaymentDetails.DisplayItems is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "IReadOnlyList<PaymentItem> PaymentDetails.DisplayItems");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetails( global::Windows.ApplicationModel.Payments.PaymentItem total) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "PaymentDetails.PaymentDetails(PaymentItem total)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.PaymentDetails(Windows.ApplicationModel.Payments.PaymentItem)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetails( global::Windows.ApplicationModel.Payments.PaymentItem total,  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Payments.PaymentItem> displayItems) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "PaymentDetails.PaymentDetails(PaymentItem total, IEnumerable<PaymentItem> displayItems)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.PaymentDetails(Windows.ApplicationModel.Payments.PaymentItem, System.Collections.Generic.IEnumerable<Windows.ApplicationModel.Payments.PaymentItem>)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetails() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetails", "PaymentDetails.PaymentDetails()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.PaymentDetails()
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.Total.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.Total.set
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.DisplayItems.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.DisplayItems.set
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.ShippingOptions.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.ShippingOptions.set
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.Modifiers.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetails.Modifiers.set
	}
}
