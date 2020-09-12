#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAudioInputNode2 : global::Windows.Media.Audio.IAudioNode,global::System.IDisposable,global::Windows.Media.Audio.IAudioInputNode
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Audio.AudioNodeEmitter Emitter
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.IAudioInputNode2.Emitter.get
	}
}
