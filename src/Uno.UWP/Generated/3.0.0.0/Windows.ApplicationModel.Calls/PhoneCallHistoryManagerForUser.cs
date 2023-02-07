#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneCallHistoryManagerForUser 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User PhoneCallHistoryManagerForUser.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20PhoneCallHistoryManagerForUser.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Calls.PhoneCallHistoryStore> RequestStoreAsync( global::Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PhoneCallHistoryStore> PhoneCallHistoryManagerForUser.RequestStoreAsync(PhoneCallHistoryStoreAccessType accessType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPhoneCallHistoryStore%3E%20PhoneCallHistoryManagerForUser.RequestStoreAsync%28PhoneCallHistoryStoreAccessType%20accessType%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallHistoryManagerForUser.User.get
	}
}
