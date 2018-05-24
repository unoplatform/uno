#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserActivityState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		New,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Published,
		#endif
	}
	#endif
}
