#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAuthenticationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResponseData
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebAuthenticationResult.ResponseData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ResponseErrorDetail
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WebAuthenticationResult.ResponseErrorDetail is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.WebAuthenticationStatus ResponseStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAuthenticationStatus WebAuthenticationResult.ResponseStatus is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.WebAuthenticationResult.ResponseData.get
		// Forced skipping of method Windows.Security.Authentication.Web.WebAuthenticationResult.ResponseStatus.get
		// Forced skipping of method Windows.Security.Authentication.Web.WebAuthenticationResult.ResponseErrorDetail.get
	}
}
