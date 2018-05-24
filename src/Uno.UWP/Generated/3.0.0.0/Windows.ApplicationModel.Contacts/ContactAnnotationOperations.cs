#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ContactAnnotationOperations 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ContactProfile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Message,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioCall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoCall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SocialFeeds,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Share,
		#endif
	}
	#endif
}
