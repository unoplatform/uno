#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public static partial class WebAuthenticationBroker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void AuthenticateAndContinue( global::System.Uri requestUri)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.WebAuthenticationBroker", "void WebAuthenticationBroker.AuthenticateAndContinue(Uri requestUri)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void AuthenticateAndContinue( global::System.Uri requestUri,  global::System.Uri callbackUri)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.WebAuthenticationBroker", "void WebAuthenticationBroker.AuthenticateAndContinue(Uri requestUri, Uri callbackUri)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void AuthenticateAndContinue( global::System.Uri requestUri,  global::System.Uri callbackUri,  global::Windows.Foundation.Collections.ValueSet continuationData,  global::Windows.Security.Authentication.Web.WebAuthenticationOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.WebAuthenticationBroker", "void WebAuthenticationBroker.AuthenticateAndContinue(Uri requestUri, Uri callbackUri, ValueSet continuationData, WebAuthenticationOptions options)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.WebAuthenticationResult> AuthenticateSilentlyAsync( global::System.Uri requestUri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAuthenticationResult> WebAuthenticationBroker.AuthenticateSilentlyAsync(Uri requestUri) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CWebAuthenticationResult%3E%20WebAuthenticationBroker.AuthenticateSilentlyAsync%28Uri%20requestUri%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.WebAuthenticationResult> AuthenticateSilentlyAsync( global::System.Uri requestUri,  global::Windows.Security.Authentication.Web.WebAuthenticationOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAuthenticationResult> WebAuthenticationBroker.AuthenticateSilentlyAsync(Uri requestUri, WebAuthenticationOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CWebAuthenticationResult%3E%20WebAuthenticationBroker.AuthenticateSilentlyAsync%28Uri%20requestUri%2C%20WebAuthenticationOptions%20options%29");
		}
		#endif
		// Skipping already declared method Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(Windows.Security.Authentication.Web.WebAuthenticationOptions, System.Uri, System.Uri)
		// Skipping already declared method Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(Windows.Security.Authentication.Web.WebAuthenticationOptions, System.Uri)
		// Skipping already declared method Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri()
	}
}
