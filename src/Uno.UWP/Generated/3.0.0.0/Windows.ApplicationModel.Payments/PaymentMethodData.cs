#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentMethodData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string JsonData
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PaymentMethodData.JsonData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> SupportedMethodIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> PaymentMethodData.SupportedMethodIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentMethodData( global::System.Collections.Generic.IEnumerable<string> supportedMethodIds) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentMethodData", "PaymentMethodData.PaymentMethodData(IEnumerable<string> supportedMethodIds)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMethodData.PaymentMethodData(System.Collections.Generic.IEnumerable<string>)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaymentMethodData( global::System.Collections.Generic.IEnumerable<string> supportedMethodIds,  string jsonData) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Payments.PaymentMethodData", "PaymentMethodData.PaymentMethodData(IEnumerable<string> supportedMethodIds, string jsonData)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMethodData.PaymentMethodData(System.Collections.Generic.IEnumerable<string>, string)
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMethodData.SupportedMethodIds.get
		// Forced skipping of method Windows.ApplicationModel.Payments.PaymentMethodData.JsonData.get
	}
}
