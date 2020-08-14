#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.OnlineId
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserAuthenticationOperation : global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.OnlineId.UserIdentity>,global::Windows.Foundation.IAsyncInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ErrorCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception UserAuthenticationOperation.ErrorCode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UserAuthenticationOperation.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncStatus UserAuthenticationOperation.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.AsyncOperationCompletedHandler<global::Windows.Security.Authentication.OnlineId.UserIdentity> Completed
		{
			get
			{
				throw new global::System.NotImplementedException("The member AsyncOperationCompletedHandler<UserIdentity> UserAuthenticationOperation.Completed is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.UserAuthenticationOperation", "AsyncOperationCompletedHandler<UserIdentity> UserAuthenticationOperation.Completed");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.UserAuthenticationOperation.Completed.set
		// Forced skipping of method Windows.Security.Authentication.OnlineId.UserAuthenticationOperation.Completed.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.OnlineId.UserIdentity GetResults()
		{
			throw new global::System.NotImplementedException("The member UserIdentity UserAuthenticationOperation.GetResults() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.OnlineId.UserAuthenticationOperation.Id.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.UserAuthenticationOperation.Status.get
		// Forced skipping of method Windows.Security.Authentication.OnlineId.UserAuthenticationOperation.ErrorCode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Cancel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.UserAuthenticationOperation", "void UserAuthenticationOperation.Cancel()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.OnlineId.UserAuthenticationOperation", "void UserAuthenticationOperation.Close()");
		}
		#endif
		// Processing: Windows.Foundation.IAsyncOperation<Windows.Security.Authentication.OnlineId.UserIdentity>
		// Processing: Windows.Foundation.IAsyncInfo
	}
}
