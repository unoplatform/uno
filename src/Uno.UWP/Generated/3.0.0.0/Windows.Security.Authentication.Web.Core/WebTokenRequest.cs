#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebTokenRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ClientId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebTokenRequest.ClientId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Core.WebTokenRequestPromptType PromptType
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebTokenRequestPromptType WebTokenRequest.PromptType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> WebTokenRequest.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Scope
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebTokenRequest.Scope is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccountProvider WebAccountProvider
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProvider WebTokenRequest.WebAccountProvider is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> AppProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> WebTokenRequest.AppProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CorrelationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebTokenRequest.CorrelationId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebTokenRequest", "string WebTokenRequest.CorrelationId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebTokenRequest( global::Windows.Security.Credentials.WebAccountProvider provider,  string scope,  string clientId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebTokenRequest", "WebTokenRequest.WebTokenRequest(WebAccountProvider provider, string scope, string clientId)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.WebTokenRequest(Windows.Security.Credentials.WebAccountProvider, string, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebTokenRequest( global::Windows.Security.Credentials.WebAccountProvider provider,  string scope,  string clientId,  global::Windows.Security.Authentication.Web.Core.WebTokenRequestPromptType promptType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebTokenRequest", "WebTokenRequest.WebTokenRequest(WebAccountProvider provider, string scope, string clientId, WebTokenRequestPromptType promptType)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.WebTokenRequest(Windows.Security.Credentials.WebAccountProvider, string, string, Windows.Security.Authentication.Web.Core.WebTokenRequestPromptType)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebTokenRequest( global::Windows.Security.Credentials.WebAccountProvider provider) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebTokenRequest", "WebTokenRequest.WebTokenRequest(WebAccountProvider provider)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.WebTokenRequest(Windows.Security.Credentials.WebAccountProvider)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebTokenRequest( global::Windows.Security.Credentials.WebAccountProvider provider,  string scope) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebTokenRequest", "WebTokenRequest.WebTokenRequest(WebAccountProvider provider, string scope)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.WebTokenRequest(Windows.Security.Credentials.WebAccountProvider, string)
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.WebAccountProvider.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.Scope.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.ClientId.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.PromptType.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.Properties.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.AppProperties.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.CorrelationId.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequest.CorrelationId.set
	}
}
