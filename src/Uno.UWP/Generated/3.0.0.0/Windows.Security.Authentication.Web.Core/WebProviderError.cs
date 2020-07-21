#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebProviderError 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WebProviderError.ErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ErrorMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebProviderError.ErrorMessage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> WebProviderError.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebProviderError( uint errorCode,  string errorMessage) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Core.WebProviderError", "WebProviderError.WebProviderError(uint errorCode, string errorMessage)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebProviderError.WebProviderError(uint, string)
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebProviderError.ErrorCode.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebProviderError.ErrorMessage.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebProviderError.Properties.get
	}
}
