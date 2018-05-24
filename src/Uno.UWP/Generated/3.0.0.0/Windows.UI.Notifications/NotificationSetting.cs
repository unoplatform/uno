#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NotificationSetting 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledForApplication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledForUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByGroupPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByManifest,
		#endif
	}
	#endif
}
