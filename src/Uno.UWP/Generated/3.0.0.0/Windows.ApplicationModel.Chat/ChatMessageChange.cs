#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatMessageChange 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatMessageChangeType ChangeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatMessageChangeType ChatMessageChange.ChangeType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ChatMessageChangeType%20ChatMessageChange.ChangeType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatMessage Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatMessage ChatMessageChange.Message is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ChatMessage%20ChatMessageChange.Message");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageChange.ChangeType.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageChange.Message.get
	}
}
