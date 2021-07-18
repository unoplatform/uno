#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatMessageManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatSyncManager> RequestSyncManagerAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatSyncManager> ChatMessageManager.RequestSyncManagerAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> RegisterTransportAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> ChatMessageManager.RegisterTransportAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatMessageTransport> GetTransportAsync( string transportId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatMessageTransport> ChatMessageManager.GetTransportAsync(string transportId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Chat.ChatMessageTransport>> GetTransportsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ChatMessageTransport>> ChatMessageManager.GetTransportsAsync() is not implemented in Uno.");
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Chat.ChatMessageStore> RequestStoreAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ChatMessageStore> ChatMessageManager.RequestStoreAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Windows.Foundation.IAsyncAction ShowComposeSmsMessageAsync( global::Windows.ApplicationModel.Chat.ChatMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ChatMessageManager.ShowComposeSmsMessageAsync(ChatMessage message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowSmsSettings()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatMessageManager", "void ChatMessageManager.ShowSmsSettings()");
		}
		#endif
	}
}
