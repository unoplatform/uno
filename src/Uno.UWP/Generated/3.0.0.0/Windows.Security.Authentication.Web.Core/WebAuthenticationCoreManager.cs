#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAuthenticationCoreManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.FindAllAccountsResult> FindAllAccountsAsync( global::Windows.Security.Credentials.WebAccountProvider provider)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<FindAllAccountsResult> WebAuthenticationCoreManager.FindAllAccountsAsync(WebAccountProvider provider) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.FindAllAccountsResult> FindAllAccountsAsync( global::Windows.Security.Credentials.WebAccountProvider provider,  string clientId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<FindAllAccountsResult> WebAuthenticationCoreManager.FindAllAccountsAsync(WebAccountProvider provider, string clientId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindSystemAccountProviderAsync( string webAccountProviderId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindSystemAccountProviderAsync(string webAccountProviderId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindSystemAccountProviderAsync( string webAccountProviderId,  string authority)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindSystemAccountProviderAsync(string webAccountProviderId, string authority) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindSystemAccountProviderAsync( string webAccountProviderId,  string authority,  global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindSystemAccountProviderAsync(string webAccountProviderId, string authority, User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Authentication.Web.Core.WebAccountMonitor CreateWebAccountMonitor( global::System.Collections.Generic.IEnumerable<global::Windows.Security.Credentials.WebAccount> webAccounts)
		{
			throw new global::System.NotImplementedException("The member WebAccountMonitor WebAuthenticationCoreManager.CreateWebAccountMonitor(IEnumerable<WebAccount> webAccounts) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindAccountProviderAsync( string webAccountProviderId,  string authority,  global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindAccountProviderAsync(string webAccountProviderId, string authority, User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.WebTokenRequestResult> GetTokenSilentlyAsync( global::Windows.Security.Authentication.Web.Core.WebTokenRequest request)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebTokenRequestResult> WebAuthenticationCoreManager.GetTokenSilentlyAsync(WebTokenRequest request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.WebTokenRequestResult> GetTokenSilentlyAsync( global::Windows.Security.Authentication.Web.Core.WebTokenRequest request,  global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebTokenRequestResult> WebAuthenticationCoreManager.GetTokenSilentlyAsync(WebTokenRequest request, WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.WebTokenRequestResult> RequestTokenAsync( global::Windows.Security.Authentication.Web.Core.WebTokenRequest request)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebTokenRequestResult> WebAuthenticationCoreManager.RequestTokenAsync(WebTokenRequest request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Web.Core.WebTokenRequestResult> RequestTokenAsync( global::Windows.Security.Authentication.Web.Core.WebTokenRequest request,  global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebTokenRequestResult> WebAuthenticationCoreManager.RequestTokenAsync(WebTokenRequest request, WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> FindAccountAsync( global::Windows.Security.Credentials.WebAccountProvider provider,  string webAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAuthenticationCoreManager.FindAccountAsync(WebAccountProvider provider, string webAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindAccountProviderAsync( string webAccountProviderId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindAccountProviderAsync(string webAccountProviderId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccountProvider> FindAccountProviderAsync( string webAccountProviderId,  string authority)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccountProvider> WebAuthenticationCoreManager.FindAccountProviderAsync(string webAccountProviderId, string authority) is not implemented in Uno.");
		}
		#endif
	}
}
