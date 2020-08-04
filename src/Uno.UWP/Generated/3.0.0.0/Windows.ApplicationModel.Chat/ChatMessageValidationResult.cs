#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatMessageValidationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? MaxPartCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ChatMessageValidationResult.MaxPartCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? PartCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ChatMessageValidationResult.PartCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? RemainingCharacterCountInPart
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ChatMessageValidationResult.RemainingCharacterCountInPart is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatMessageValidationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatMessageValidationStatus ChatMessageValidationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageValidationResult.MaxPartCount.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageValidationResult.PartCount.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageValidationResult.RemainingCharacterCountInPart.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatMessageValidationResult.Status.get
	}
}
