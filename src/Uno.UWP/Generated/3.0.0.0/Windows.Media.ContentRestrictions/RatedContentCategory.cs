#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ContentRestrictions
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RatedContentCategory 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		General,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Application,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Game,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Movie,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Television,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Music,
		#endif
	}
	#endif
}
