#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailMailboxActionKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MarkMessageAsSeen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MarkMessageRead,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChangeMessageFlagState,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SaveDraft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SendMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CreateResponseReplyMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CreateResponseReplyAllMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CreateResponseForwardMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveFolder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MarkFolderForSyncEnabled,
		#endif
	}
	#endif
}
