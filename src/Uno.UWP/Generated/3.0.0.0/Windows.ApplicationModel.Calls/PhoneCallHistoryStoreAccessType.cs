#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneCallHistoryStoreAccessType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppEntriesReadWrite,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllEntriesLimitedReadWrite,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllEntriesReadWrite,
		#endif
	}
	#endif
}
