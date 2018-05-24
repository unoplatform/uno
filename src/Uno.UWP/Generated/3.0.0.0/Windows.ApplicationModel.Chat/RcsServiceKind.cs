#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RcsServiceKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Chat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupChat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FileTransfer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Capability,
		#endif
	}
	#endif
}
