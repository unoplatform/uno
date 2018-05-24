#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactListSyncStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Idle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Syncing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UpToDate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticationError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PolicyError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManualAccountRemovalRequired,
		#endif
	}
	#endif
}
