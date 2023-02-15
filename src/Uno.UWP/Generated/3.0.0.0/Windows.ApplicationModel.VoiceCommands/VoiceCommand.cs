#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.VoiceCommands
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VoiceCommand 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CommandName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommand.CommandName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20VoiceCommand.CommandName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Collections.Generic.IReadOnlyList<string>> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, IReadOnlyList<string>> VoiceCommand.Properties is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20IReadOnlyList%3Cstring%3E%3E%20VoiceCommand.Properties");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SpeechRecognition.SpeechRecognitionResult SpeechRecognitionResult
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpeechRecognitionResult VoiceCommand.SpeechRecognitionResult is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SpeechRecognitionResult%20VoiceCommand.SpeechRecognitionResult");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommand.CommandName.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommand.Properties.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommand.SpeechRecognitionResult.get
	}
}
