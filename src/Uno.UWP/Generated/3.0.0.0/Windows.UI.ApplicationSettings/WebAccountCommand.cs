#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ApplicationSettings
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountCommand 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ApplicationSettings.SupportedWebAccountActions Actions
		{
			get
			{
				throw new global::System.NotImplementedException("The member SupportedWebAccountActions WebAccountCommand.Actions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ApplicationSettings.WebAccountCommandInvokedHandler Invoked
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountCommandInvokedHandler WebAccountCommand.Invoked is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccount WebAccount
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccount WebAccountCommand.WebAccount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebAccountCommand( global::Windows.Security.Credentials.WebAccount webAccount,  global::Windows.UI.ApplicationSettings.WebAccountCommandInvokedHandler invoked,  global::Windows.UI.ApplicationSettings.SupportedWebAccountActions actions) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.WebAccountCommand", "WebAccountCommand.WebAccountCommand(WebAccount webAccount, WebAccountCommandInvokedHandler invoked, SupportedWebAccountActions actions)");
		}
		#endif
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountCommand.WebAccountCommand(Windows.Security.Credentials.WebAccount, Windows.UI.ApplicationSettings.WebAccountCommandInvokedHandler, Windows.UI.ApplicationSettings.SupportedWebAccountActions)
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountCommand.WebAccount.get
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountCommand.Invoked.get
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountCommand.Actions.get
	}
}
