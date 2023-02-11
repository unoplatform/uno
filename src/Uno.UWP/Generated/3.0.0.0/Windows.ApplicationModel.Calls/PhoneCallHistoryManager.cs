#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public static partial class PhoneCallHistoryManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Calls.PhoneCallHistoryManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member PhoneCallHistoryManagerForUser PhoneCallHistoryManager.GetForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneCallHistoryManagerForUser%20PhoneCallHistoryManager.GetForUser%28User%20user%29");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Calls.PhoneCallHistoryStore> RequestStoreAsync( global::Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PhoneCallHistoryStore> PhoneCallHistoryManager.RequestStoreAsync(PhoneCallHistoryStoreAccessType accessType) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPhoneCallHistoryStore%3E%20PhoneCallHistoryManager.RequestStoreAsync%28PhoneCallHistoryStoreAccessType%20accessType%29");
		}
		#endif
	}
}
