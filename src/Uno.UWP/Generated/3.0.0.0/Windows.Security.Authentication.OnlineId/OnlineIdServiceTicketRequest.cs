#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.OnlineId
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OnlineIdServiceTicketRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Policy
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OnlineIdServiceTicketRequest.Policy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Service
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OnlineIdServiceTicketRequest.Service is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public OnlineIdServiceTicketRequest( string service,  string policy) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest", "OnlineIdServiceTicketRequest.OnlineIdServiceTicketRequest(string service, string policy)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest.OnlineIdServiceTicketRequest(string, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public OnlineIdServiceTicketRequest( string service) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest", "OnlineIdServiceTicketRequest.OnlineIdServiceTicketRequest(string service)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest.OnlineIdServiceTicketRequest(string)
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest.Service.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest.Policy.get
	}
}
