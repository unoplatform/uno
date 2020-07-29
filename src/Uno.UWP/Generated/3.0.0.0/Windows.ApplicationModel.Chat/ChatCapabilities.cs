#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsChatCapable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatCapabilities.IsChatCapable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsFileTransferCapable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatCapabilities.IsFileTransferCapable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsGeoLocationPushCapable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatCapabilities.IsGeoLocationPushCapable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsIntegratedMessagingCapable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatCapabilities.IsIntegratedMessagingCapable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOnline
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatCapabilities.IsOnline is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatCapabilities.IsOnline.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatCapabilities.IsChatCapable.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatCapabilities.IsFileTransferCapable.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatCapabilities.IsGeoLocationPushCapable.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatCapabilities.IsIntegratedMessagingCapable.get
	}
}
