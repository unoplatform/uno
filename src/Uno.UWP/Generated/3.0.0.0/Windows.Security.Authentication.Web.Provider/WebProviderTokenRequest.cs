#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebProviderTokenRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ApplicationCallbackUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebProviderTokenRequest.ApplicationCallbackUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Core.WebTokenRequest ClientRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebTokenRequest WebProviderTokenRequest.ClientRequest is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountSelectionOptions WebAccountSelectionOptions
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountSelectionOptions WebProviderTokenRequest.WebAccountSelectionOptions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Credentials.WebAccount> WebAccounts
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<WebAccount> WebProviderTokenRequest.WebAccounts is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ApplicationPackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebProviderTokenRequest.ApplicationPackageFamilyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ApplicationProcessName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebProviderTokenRequest.ApplicationProcessName is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.ClientRequest.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.WebAccounts.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.WebAccountSelectionOptions.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.ApplicationCallbackUri.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Cryptography.Core.CryptographicKey> GetApplicationTokenBindingKeyAsync( global::Windows.Security.Authentication.Web.TokenBindingKeyType keyType,  global::System.Uri target)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CryptographicKey> WebProviderTokenRequest.GetApplicationTokenBindingKeyAsync(TokenBindingKeyType keyType, Uri target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> GetApplicationTokenBindingKeyIdAsync( global::Windows.Security.Authentication.Web.TokenBindingKeyType keyType,  global::System.Uri target)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> WebProviderTokenRequest.GetApplicationTokenBindingKeyIdAsync(TokenBindingKeyType keyType, Uri target) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.ApplicationPackageFamilyName.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest.ApplicationProcessName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> CheckApplicationForCapabilityAsync( string capabilityName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> WebProviderTokenRequest.CheckApplicationForCapabilityAsync(string capabilityName) is not implemented in Uno.");
		}
		#endif
	}
}
