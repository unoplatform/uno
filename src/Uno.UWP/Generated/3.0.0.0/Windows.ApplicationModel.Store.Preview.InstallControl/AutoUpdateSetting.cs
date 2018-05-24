#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview.InstallControl
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutoUpdateSetting 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnabledByPolicy,
		#endif
	}
	#endif
}
