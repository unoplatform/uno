#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.LicenseManagement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LicenseRefreshOption 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RunningLicenses,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllLicenses,
		#endif
	}
	#endif
}
