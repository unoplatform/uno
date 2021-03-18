#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebUIWebAccountProviderActivatedEventArgs : global::Windows.ApplicationModel.Activation.IWebAccountProviderActivatedEventArgs,global::Windows.ApplicationModel.Activation.IActivatedEventArgs,global::Windows.UI.WebUI.IActivatedEventArgsDeferral,global::Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ActivationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivationKind WebUIWebAccountProviderActivatedEventArgs.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.ApplicationExecutionState PreviousExecutionState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ApplicationExecutionState WebUIWebAccountProviderActivatedEventArgs.PreviousExecutionState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Activation.SplashScreen SplashScreen
		{
			get
			{
				throw new global::System.NotImplementedException("The member SplashScreen WebUIWebAccountProviderActivatedEventArgs.SplashScreen is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User WebUIWebAccountProviderActivatedEventArgs.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation Operation
		{
			get
			{
				throw new global::System.NotImplementedException("The member IWebAccountProviderOperation WebUIWebAccountProviderActivatedEventArgs.Operation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WebUI.ActivatedOperation ActivatedOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivatedOperation WebUIWebAccountProviderActivatedEventArgs.ActivatedOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.Operation.get
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.Kind.get
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.PreviousExecutionState.get
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.SplashScreen.get
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.ActivatedOperation.get
		// Forced skipping of method Windows.UI.WebUI.WebUIWebAccountProviderActivatedEventArgs.User.get
		// Processing: Windows.ApplicationModel.Activation.IWebAccountProviderActivatedEventArgs
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgs
		// Processing: Windows.UI.WebUI.IActivatedEventArgsDeferral
		// Processing: Windows.ApplicationModel.Activation.IActivatedEventArgsWithUser
	}
}
