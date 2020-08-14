#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CreateMediaSourceAudioInputNodeResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.MediaSourceAudioInputNode Node
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSourceAudioInputNode CreateMediaSourceAudioInputNodeResult.Node is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.MediaSourceAudioInputNodeCreationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSourceAudioInputNodeCreationStatus CreateMediaSourceAudioInputNodeResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception CreateMediaSourceAudioInputNodeResult.ExtendedError is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.CreateMediaSourceAudioInputNodeResult.Status.get
		// Forced skipping of method Windows.Media.Audio.CreateMediaSourceAudioInputNodeResult.Node.get
		// Forced skipping of method Windows.Media.Audio.CreateMediaSourceAudioInputNodeResult.ExtendedError.get
	}
}
