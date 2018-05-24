#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatMessageKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Standard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FileTransferRequest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportCustom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JoinedConversation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftConversation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherParticipantJoinedConversation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherParticipantLeftConversation,
		#endif
	}
	#endif
}
