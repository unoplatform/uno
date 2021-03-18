#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatSyncConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatRestoreHistorySpan RestoreHistorySpan
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatRestoreHistorySpan ChatSyncConfiguration.RestoreHistorySpan is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatSyncConfiguration", "ChatRestoreHistorySpan ChatSyncConfiguration.RestoreHistorySpan");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSyncEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatSyncConfiguration.IsSyncEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatSyncConfiguration", "bool ChatSyncConfiguration.IsSyncEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatSyncConfiguration.IsSyncEnabled.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatSyncConfiguration.IsSyncEnabled.set
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatSyncConfiguration.RestoreHistorySpan.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatSyncConfiguration.RestoreHistorySpan.set
	}
}
