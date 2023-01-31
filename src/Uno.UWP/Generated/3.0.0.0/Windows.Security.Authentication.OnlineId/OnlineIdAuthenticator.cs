#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.OnlineId
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OnlineIdAuthenticator 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ApplicationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid OnlineIdAuthenticator.ApplicationId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20OnlineIdAuthenticator.ApplicationId");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator", "Guid OnlineIdAuthenticator.ApplicationId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AuthenticatedSafeCustomerId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string OnlineIdAuthenticator.AuthenticatedSafeCustomerId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20OnlineIdAuthenticator.AuthenticatedSafeCustomerId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanSignOut
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool OnlineIdAuthenticator.CanSignOut is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20OnlineIdAuthenticator.CanSignOut");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public OnlineIdAuthenticator() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator", "OnlineIdAuthenticator.OnlineIdAuthenticator()");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator.OnlineIdAuthenticator()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.UserAuthenticationOperation AuthenticateUserAsync( global::Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest request)
		{
			throw new global::System.NotImplementedException("The member UserAuthenticationOperation OnlineIdAuthenticator.AuthenticateUserAsync(OnlineIdServiceTicketRequest request) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserAuthenticationOperation%20OnlineIdAuthenticator.AuthenticateUserAsync%28OnlineIdServiceTicketRequest%20request%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.UserAuthenticationOperation AuthenticateUserAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest> requests,  global::Windows.Security.Authentication.OnlineId.CredentialPromptType credentialPromptType)
		{
			throw new global::System.NotImplementedException("The member UserAuthenticationOperation OnlineIdAuthenticator.AuthenticateUserAsync(IEnumerable<OnlineIdServiceTicketRequest> requests, CredentialPromptType credentialPromptType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserAuthenticationOperation%20OnlineIdAuthenticator.AuthenticateUserAsync%28IEnumerable%3COnlineIdServiceTicketRequest%3E%20requests%2C%20CredentialPromptType%20credentialPromptType%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.SignOutUserOperation SignOutUserAsync()
		{
			throw new global::System.NotImplementedException("The member SignOutUserOperation OnlineIdAuthenticator.SignOutUserAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SignOutUserOperation%20OnlineIdAuthenticator.SignOutUserAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator.ApplicationId.set
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator.ApplicationId.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator.CanSignOut.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator.AuthenticatedSafeCustomerId.get
	}
}
