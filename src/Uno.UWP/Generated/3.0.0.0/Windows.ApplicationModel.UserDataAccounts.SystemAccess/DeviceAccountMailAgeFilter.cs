#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.SystemAccess
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceAccountMailAgeFilter 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last1Day,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last3Days,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last7Days,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last14Days,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last30Days,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last90Days,
		#endif
	}
	#endif
}
