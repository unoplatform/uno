#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction InvalidateAppCacheForAllAccountsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.InvalidateAppCacheForAllAccountsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction InvalidateAppCacheForAccountAsync( global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.InvalidateAppCacheForAccountAsync(WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.WebAccount>> FindAllProviderWebAccountsForUserAsync( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<WebAccount>> WebAccountManager.FindAllProviderWebAccountsForUserAsync(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountForUserAsync( global::Windows.System.User user,  string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountForUserAsync(User user, string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountForUserAsync( global::Windows.System.User user,  string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props,  global::Windows.Security.Authentication.Web.Provider.WebAccountScope scope)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountForUserAsync(User user, string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props, WebAccountScope scope) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountForUserAsync( global::Windows.System.User user,  string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props,  global::Windows.Security.Authentication.Web.Provider.WebAccountScope scope,  string perUserWebAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountForUserAsync(User user, string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props, WebAccountScope scope, string perUserWebAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountAsync( string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props,  global::Windows.Security.Authentication.Web.Provider.WebAccountScope scope,  string perUserWebAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountAsync(string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props, WebAccountScope scope, string perUserWebAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetPerAppToPerUserAccountAsync( global::Windows.Security.Credentials.WebAccount perAppAccount,  string perUserWebAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.SetPerAppToPerUserAccountAsync(WebAccount perAppAccount, string perUserWebAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> GetPerUserFromPerAppAccountAsync( global::Windows.Security.Credentials.WebAccount perAppAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.GetPerUserFromPerAppAccountAsync(WebAccount perAppAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ClearPerUserFromPerAppAccountAsync( global::Windows.Security.Credentials.WebAccount perAppAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.ClearPerUserFromPerAppAccountAsync(WebAccount perAppAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountAsync( string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props,  global::Windows.Security.Authentication.Web.Provider.WebAccountScope scope)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountAsync(string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props, WebAccountScope scope) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetScopeAsync( global::Windows.Security.Credentials.WebAccount webAccount,  global::Windows.Security.Authentication.Web.Provider.WebAccountScope scope)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.SetScopeAsync(WebAccount webAccount, WebAccountScope scope) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Authentication.Web.Provider.WebAccountScope GetScope( global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member WebAccountScope WebAccountManager.GetScope(WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction PullCookiesAsync( string uriString,  string callerPFN)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.PullCookiesAsync(string uriString, string callerPFN) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction UpdateWebAccountPropertiesAsync( global::Windows.Security.Credentials.WebAccount webAccount,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> additionalProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.UpdateWebAccountPropertiesAsync(WebAccount webAccount, string webAccountUserName, IReadOnlyDictionary<string, string> additionalProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Credentials.WebAccount> AddWebAccountAsync( string webAccountId,  string webAccountUserName,  global::System.Collections.Generic.IReadOnlyDictionary<string, string> props)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WebAccount> WebAccountManager.AddWebAccountAsync(string webAccountId, string webAccountUserName, IReadOnlyDictionary<string, string> props) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction DeleteWebAccountAsync( global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.DeleteWebAccountAsync(WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.WebAccount>> FindAllProviderWebAccountsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<WebAccount>> WebAccountManager.FindAllProviderWebAccountsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction PushCookiesAsync( global::System.Uri uri,  global::System.Collections.Generic.IReadOnlyList<global::Windows.Web.Http.HttpCookie> cookies)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.PushCookiesAsync(Uri uri, IReadOnlyList<HttpCookie> cookies) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetViewAsync( global::Windows.Security.Credentials.WebAccount webAccount,  global::Windows.Security.Authentication.Web.Provider.WebAccountClientView view)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.SetViewAsync(WebAccount webAccount, WebAccountClientView view) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ClearViewAsync( global::Windows.Security.Credentials.WebAccount webAccount,  global::System.Uri applicationCallbackUri)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.ClearViewAsync(WebAccount webAccount, Uri applicationCallbackUri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Authentication.Web.Provider.WebAccountClientView>> GetViewsAsync( global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<WebAccountClientView>> WebAccountManager.GetViewsAsync(WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetWebAccountPictureAsync( global::Windows.Security.Credentials.WebAccount webAccount,  global::Windows.Storage.Streams.IRandomAccessStream webAccountPicture)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.SetWebAccountPictureAsync(WebAccount webAccount, IRandomAccessStream webAccountPicture) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction ClearWebAccountPictureAsync( global::Windows.Security.Credentials.WebAccount webAccount)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebAccountManager.ClearWebAccountPictureAsync(WebAccount webAccount) is not implemented in Uno.");
		}
		#endif
	}
}
