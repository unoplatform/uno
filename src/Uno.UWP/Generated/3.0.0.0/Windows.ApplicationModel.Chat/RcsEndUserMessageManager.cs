#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RcsEndUserMessageManager 
	{
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessageManager.MessageAvailableChanged.add
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessageManager.MessageAvailableChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Chat.RcsEndUserMessageManager, global::Windows.ApplicationModel.Chat.RcsEndUserMessageAvailableEventArgs> MessageAvailableChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.RcsEndUserMessageManager", "event TypedEventHandler<RcsEndUserMessageManager, RcsEndUserMessageAvailableEventArgs> RcsEndUserMessageManager.MessageAvailableChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.RcsEndUserMessageManager", "event TypedEventHandler<RcsEndUserMessageManager, RcsEndUserMessageAvailableEventArgs> RcsEndUserMessageManager.MessageAvailableChanged");
			}
		}
		#endif
	}
}
