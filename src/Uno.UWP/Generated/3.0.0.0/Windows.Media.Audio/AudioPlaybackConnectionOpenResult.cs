#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioPlaybackConnectionOpenResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception AudioPlaybackConnectionOpenResult.ExtendedError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20AudioPlaybackConnectionOpenResult.ExtendedError");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioPlaybackConnectionOpenResultStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioPlaybackConnectionOpenResultStatus AudioPlaybackConnectionOpenResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AudioPlaybackConnectionOpenResultStatus%20AudioPlaybackConnectionOpenResult.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioPlaybackConnectionOpenResult.Status.get
		// Forced skipping of method Windows.Media.Audio.AudioPlaybackConnectionOpenResult.ExtendedError.get
	}
}
