#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatConversationThreadingKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Participants,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ContactId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConversationId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
