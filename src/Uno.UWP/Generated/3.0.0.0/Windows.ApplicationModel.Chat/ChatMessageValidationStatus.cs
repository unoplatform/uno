#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatMessageValidationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Valid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoRecipients,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidData,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageTooLarge,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooManyRecipients,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportInactive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooManyAttachments,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidRecipients,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidBody,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidOther,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ValidWithLargeMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VoiceRoamingRestriction,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataRoamingRestriction,
		#endif
	}
	#endif
}
