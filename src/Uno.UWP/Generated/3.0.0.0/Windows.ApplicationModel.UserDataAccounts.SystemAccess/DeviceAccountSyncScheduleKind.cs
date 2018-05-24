#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.SystemAccess
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceAccountSyncScheduleKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manual,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Every15Minutes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Every30Minutes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Every60Minutes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Every2Hours,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Daily,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AsItemsArrive,
		#endif
	}
	#endif
}
