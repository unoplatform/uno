#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview.InstallControl
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppInstallType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Install,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Update,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Repair,
		#endif
	}
	#endif
}
