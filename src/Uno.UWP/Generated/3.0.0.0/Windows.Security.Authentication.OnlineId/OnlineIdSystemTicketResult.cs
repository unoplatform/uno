#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.OnlineId
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OnlineIdSystemTicketResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception OnlineIdSystemTicketResult.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.OnlineIdSystemIdentity Identity
		{
			get
			{
				throw new global::System.NotImplementedException("The member OnlineIdSystemIdentity OnlineIdSystemTicketResult.Identity is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.OnlineIdSystemTicketStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member OnlineIdSystemTicketStatus OnlineIdSystemTicketResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdSystemTicketResult.Identity.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdSystemTicketResult.Status.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdSystemTicketResult.ExtendedError.get
	}
}
