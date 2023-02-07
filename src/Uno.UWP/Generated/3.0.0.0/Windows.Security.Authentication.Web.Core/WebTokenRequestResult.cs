#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebTokenRequestResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Authentication.Web.Core.WebTokenResponse> ResponseData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<WebTokenResponse> WebTokenRequestResult.ResponseData is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CWebTokenResponse%3E%20WebTokenRequestResult.ResponseData");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Core.WebProviderError ResponseError
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebProviderError WebTokenRequestResult.ResponseError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WebProviderError%20WebTokenRequestResult.ResponseError");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Core.WebTokenRequestStatus ResponseStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebTokenRequestStatus WebTokenRequestResult.ResponseStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WebTokenRequestStatus%20WebTokenRequestResult.ResponseStatus");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequestResult.ResponseData.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequestResult.ResponseStatus.get
		// Forced skipping of method Windows.Security.Authentication.Web.Core.WebTokenRequestResult.ResponseError.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction InvalidateCacheAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WebTokenRequestResult.InvalidateCacheAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20WebTokenRequestResult.InvalidateCacheAsync%28%29");
		}
		#endif
	}
}
