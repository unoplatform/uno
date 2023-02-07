#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RcsEndUserMessageAvailableTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsEndUserMessageAvailableTriggerDetails.Text is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20RcsEndUserMessageAvailableTriggerDetails.Text");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsEndUserMessageAvailableTriggerDetails.Title is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20RcsEndUserMessageAvailableTriggerDetails.Title");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessageAvailableTriggerDetails.Title.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsEndUserMessageAvailableTriggerDetails.Text.get
	}
}
