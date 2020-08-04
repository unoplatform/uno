#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.VoiceCommands
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VoiceCommandResponse 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage RepeatMessage
		{
			get
			{
				throw new global::System.NotImplementedException("The member VoiceCommandUserMessage VoiceCommandResponse.RepeatMessage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse", "VoiceCommandUserMessage VoiceCommandResponse.RepeatMessage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member VoiceCommandUserMessage VoiceCommandResponse.Message is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse", "VoiceCommandUserMessage VoiceCommandResponse.Message");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppLaunchArgument
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommandResponse.AppLaunchArgument is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse", "string VoiceCommandResponse.AppLaunchArgument");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandContentTile> VoiceCommandContentTiles
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<VoiceCommandContentTile> VoiceCommandResponse.VoiceCommandContentTiles is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static uint MaxSupportedVoiceCommandContentTiles
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint VoiceCommandResponse.MaxSupportedVoiceCommandContentTiles is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.Message.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.Message.set
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.RepeatMessage.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.RepeatMessage.set
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.AppLaunchArgument.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.AppLaunchArgument.set
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.VoiceCommandContentTiles.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse.MaxSupportedVoiceCommandContentTiles.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse CreateResponse( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage userMessage)
		{
			throw new global::System.NotImplementedException("The member VoiceCommandResponse VoiceCommandResponse.CreateResponse(VoiceCommandUserMessage userMessage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse CreateResponse( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage message,  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandContentTile> contentTiles)
		{
			throw new global::System.NotImplementedException("The member VoiceCommandResponse VoiceCommandResponse.CreateResponse(VoiceCommandUserMessage message, IEnumerable<VoiceCommandContentTile> contentTiles) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse CreateResponseForPrompt( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage message,  global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage repeatMessage)
		{
			throw new global::System.NotImplementedException("The member VoiceCommandResponse VoiceCommandResponse.CreateResponseForPrompt(VoiceCommandUserMessage message, VoiceCommandUserMessage repeatMessage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse CreateResponseForPrompt( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage message,  global::Windows.ApplicationModel.VoiceCommands.VoiceCommandUserMessage repeatMessage,  global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandContentTile> contentTiles)
		{
			throw new global::System.NotImplementedException("The member VoiceCommandResponse VoiceCommandResponse.CreateResponseForPrompt(VoiceCommandUserMessage message, VoiceCommandUserMessage repeatMessage, IEnumerable<VoiceCommandContentTile> contentTiles) is not implemented in Uno.");
		}
		#endif
	}
}
