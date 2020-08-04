#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RcsEndUserMessage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Chat.RcsEndUserMessageAction> Actions
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<RcsEndUserMessageAction> RcsEndUserMessage.Actions is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPinRequired
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RcsEndUserMessage.IsPinRequired is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsEndUserMessage.Text is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsEndUserMessage.Title is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TransportId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsEndUserMessage.TransportId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessage.TransportId.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessage.Title.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessage.Text.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessage.IsPinRequired.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessage.Actions.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SendResponseAsync( global::Windows.ApplicationModel.Chat.RcsEndUserMessageAction action)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RcsEndUserMessage.SendResponseAsync(RcsEndUserMessageAction action) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SendResponseWithPinAsync( global::Windows.ApplicationModel.Chat.RcsEndUserMessageAction action,  string pin)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RcsEndUserMessage.SendResponseWithPinAsync(RcsEndUserMessageAction action, string pin) is not implemented in Uno.");
		}
		#endif
	}
}
