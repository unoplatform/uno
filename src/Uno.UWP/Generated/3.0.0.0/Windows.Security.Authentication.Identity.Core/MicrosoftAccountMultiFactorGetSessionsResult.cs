#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MicrosoftAccountMultiFactorGetSessionsResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse ServiceResponse
		{
			get
			{
				throw new global::System.NotImplementedException("The member MicrosoftAccountMultiFactorServiceResponse MicrosoftAccountMultiFactorGetSessionsResult.ServiceResponse is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorSessionInfo> Sessions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MicrosoftAccountMultiFactorSessionInfo> MicrosoftAccountMultiFactorGetSessionsResult.Sessions is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorGetSessionsResult.Sessions.get
		// Forced skipping of method Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorGetSessionsResult.ServiceResponse.get
	}
}
