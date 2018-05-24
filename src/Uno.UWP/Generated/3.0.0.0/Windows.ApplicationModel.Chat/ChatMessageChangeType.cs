#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatMessageChangeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageCreated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageModified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageDeleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChangeTrackingLost,
		#endif
	}
	#endif
}
