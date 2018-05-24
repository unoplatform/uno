#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailQueryKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Important,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Flagged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unread,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Read,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unseen,
		#endif
	}
	#endif
}
