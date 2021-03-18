#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MicrosoftAccountMultiFactorAuthenticationManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorAuthenticationManager Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member MicrosoftAccountMultiFactorAuthenticationManager MicrosoftAccountMultiFactorAuthenticationManager.Current is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorOneTimeCodedInfo> GetOneTimePassCodeAsync( string userAccountId,  uint codeLength)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorOneTimeCodedInfo> MicrosoftAccountMultiFactorAuthenticationManager.GetOneTimePassCodeAsync(string userAccountId, uint codeLength) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> AddDeviceAsync( string userAccountId,  string authenticationToken,  string wnsChannelId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.AddDeviceAsync(string userAccountId, string authenticationToken, string wnsChannelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> RemoveDeviceAsync( string userAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.RemoveDeviceAsync(string userAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> UpdateWnsChannelAsync( string userAccountId,  string channelUri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.UpdateWnsChannelAsync(string userAccountId, string channelUri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorGetSessionsResult> GetSessionsAsync( global::System.Collections.Generic.IEnumerable<string> userAccountIdList)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorGetSessionsResult> MicrosoftAccountMultiFactorAuthenticationManager.GetSessionsAsync(IEnumerable<string> userAccountIdList) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorUnregisteredAccountsAndSessionInfo> GetSessionsAndUnregisteredAccountsAsync( global::System.Collections.Generic.IEnumerable<string> userAccountIdList)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorUnregisteredAccountsAndSessionInfo> MicrosoftAccountMultiFactorAuthenticationManager.GetSessionsAndUnregisteredAccountsAsync(IEnumerable<string> userAccountIdList) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> ApproveSessionAsync( global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorSessionAuthenticationStatus sessionAuthentictionStatus,  global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorSessionInfo authenticationSessionInfo)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.ApproveSessionAsync(MicrosoftAccountMultiFactorSessionAuthenticationStatus sessionAuthentictionStatus, MicrosoftAccountMultiFactorSessionInfo authenticationSessionInfo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> ApproveSessionAsync( global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorSessionAuthenticationStatus sessionAuthentictionStatus,  string userAccountId,  string sessionId,  global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorAuthenticationType sessionAuthenticationType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.ApproveSessionAsync(MicrosoftAccountMultiFactorSessionAuthenticationStatus sessionAuthentictionStatus, string userAccountId, string sessionId, MicrosoftAccountMultiFactorAuthenticationType sessionAuthenticationType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> DenySessionAsync( global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorSessionInfo authenticationSessionInfo)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.DenySessionAsync(MicrosoftAccountMultiFactorSessionInfo authenticationSessionInfo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorServiceResponse> DenySessionAsync( string userAccountId,  string sessionId,  global::Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorAuthenticationType sessionAuthenticationType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MicrosoftAccountMultiFactorServiceResponse> MicrosoftAccountMultiFactorAuthenticationManager.DenySessionAsync(string userAccountId, string sessionId, MicrosoftAccountMultiFactorAuthenticationType sessionAuthenticationType) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Identity.Core.MicrosoftAccountMultiFactorAuthenticationManager.Current.get
	}
}
