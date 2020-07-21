#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ApplicationSettings
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderCommand 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.ApplicationSettings.WebAccountProviderCommandInvokedHandler Invoked
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProviderCommandInvokedHandler WebAccountProviderCommand.Invoked is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccountProvider WebAccountProvider
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProvider WebAccountProviderCommand.WebAccountProvider is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebAccountProviderCommand( global::Windows.Security.Credentials.WebAccountProvider webAccountProvider,  global::Windows.UI.ApplicationSettings.WebAccountProviderCommandInvokedHandler invoked) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ApplicationSettings.WebAccountProviderCommand", "WebAccountProviderCommand.WebAccountProviderCommand(WebAccountProvider webAccountProvider, WebAccountProviderCommandInvokedHandler invoked)");
		}
		#endif
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountProviderCommand.WebAccountProviderCommand(Windows.Security.Credentials.WebAccountProvider, Windows.UI.ApplicationSettings.WebAccountProviderCommandInvokedHandler)
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountProviderCommand.WebAccountProvider.get
		// Forced skipping of method Windows.UI.ApplicationSettings.WebAccountProviderCommand.Invoked.get
	}
}
