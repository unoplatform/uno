#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NewWebUIViewCreatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.IActivatedEventArgs ActivatedEventArgs
		{
			get
			{
				throw new global::System.NotImplementedException("The member IActivatedEventArgs NewWebUIViewCreatedEventArgs.ActivatedEventArgs is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasPendingNavigate
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NewWebUIViewCreatedEventArgs.HasPendingNavigate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WebUI.WebUIView WebUIView
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebUIView NewWebUIViewCreatedEventArgs.WebUIView is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.NewWebUIViewCreatedEventArgs.WebUIView.get
		// Forced skipping of method Windows.UI.WebUI.NewWebUIViewCreatedEventArgs.ActivatedEventArgs.get
		// Forced skipping of method Windows.UI.WebUI.NewWebUIViewCreatedEventArgs.HasPendingNavigate.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral NewWebUIViewCreatedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
