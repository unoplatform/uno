#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Payments.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaymentAppManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Payments.Provider.PaymentAppManager Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member PaymentAppManager PaymentAppManager.Current is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PaymentAppManager%20PaymentAppManager.Current");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RegisterAsync( global::System.Collections.Generic.IEnumerable<string> supportedPaymentMethodIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PaymentAppManager.RegisterAsync(IEnumerable<string> supportedPaymentMethodIds) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20PaymentAppManager.RegisterAsync%28IEnumerable%3Cstring%3E%20supportedPaymentMethodIds%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction UnregisterAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PaymentAppManager.UnregisterAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20PaymentAppManager.UnregisterAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Payments.Provider.PaymentAppManager.Current.get
	}
}
