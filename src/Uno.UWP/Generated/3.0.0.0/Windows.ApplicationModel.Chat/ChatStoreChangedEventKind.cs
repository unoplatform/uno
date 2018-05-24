#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatStoreChangedEventKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotificationsMissed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StoreModified,
		#endif
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
		ConversationModified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConversationDeleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConversationTransportDeleted,
		#endif
	}
	#endif
}
