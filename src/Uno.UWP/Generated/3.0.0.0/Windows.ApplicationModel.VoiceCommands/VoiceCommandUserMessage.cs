#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.VoiceCommands
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VoiceCommandUserMessage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SpokenMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommandUserMessage.SpokenMessage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage", "string VoiceCommandUserMessage.SpokenMessage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommandUserMessage.DisplayMessage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage", "string VoiceCommandUserMessage.DisplayMessage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VoiceCommandUserMessage() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage", "VoiceCommandUserMessage.VoiceCommandUserMessage()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage.VoiceCommandUserMessage()
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage.DisplayMessage.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage.DisplayMessage.set
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage.SpokenMessage.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage.SpokenMessage.set
	}
}
