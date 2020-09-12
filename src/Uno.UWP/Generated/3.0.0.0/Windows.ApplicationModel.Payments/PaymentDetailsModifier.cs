#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentDetailsModifier 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Payments.PaymentItem> AdditionalDisplayItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PaymentItem> PaymentDetailsModifier.AdditionalDisplayItems is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string JsonData
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentDetailsModifier.JsonData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> SupportedMethodIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> PaymentDetailsModifier.SupportedMethodIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Payments.PaymentItem Total
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentItem PaymentDetailsModifier.Total is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetailsModifier( global::System.Collections.Generic.IEnumerable<string> supportedMethodIds,  global::Windows.ApplicationModel.Payments.PaymentItem total) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetailsModifier", "PaymentDetailsModifier.PaymentDetailsModifier(IEnumerable<string> supportedMethodIds, PaymentItem total)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.PaymentDetailsModifier(System.Collections.Generic.IEnumerable<string>, Windows.ApplicationModel.Payments.PaymentItem)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetailsModifier( global::System.Collections.Generic.IEnumerable<string> supportedMethodIds,  global::Windows.ApplicationModel.Payments.PaymentItem total,  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Payments.PaymentItem> additionalDisplayItems) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetailsModifier", "PaymentDetailsModifier.PaymentDetailsModifier(IEnumerable<string> supportedMethodIds, PaymentItem total, IEnumerable<PaymentItem> additionalDisplayItems)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.PaymentDetailsModifier(System.Collections.Generic.IEnumerable<string>, Windows.ApplicationModel.Payments.PaymentItem, System.Collections.Generic.IEnumerable<Windows.ApplicationModel.Payments.PaymentItem>)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentDetailsModifier( global::System.Collections.Generic.IEnumerable<string> supportedMethodIds,  global::Windows.ApplicationModel.Payments.PaymentItem total,  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Payments.PaymentItem> additionalDisplayItems,  string jsonData) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentDetailsModifier", "PaymentDetailsModifier.PaymentDetailsModifier(IEnumerable<string> supportedMethodIds, PaymentItem total, IEnumerable<PaymentItem> additionalDisplayItems, string jsonData)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.PaymentDetailsModifier(System.Collections.Generic.IEnumerable<string>, Windows.ApplicationModel.Payments.PaymentItem, System.Collections.Generic.IEnumerable<Windows.ApplicationModel.Payments.PaymentItem>, string)
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.JsonData.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.SupportedMethodIds.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.Total.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentDetailsModifier.AdditionalDisplayItems.get
	}
}
