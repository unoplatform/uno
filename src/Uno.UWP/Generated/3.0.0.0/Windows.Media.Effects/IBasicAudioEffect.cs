#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBasicAudioEffect : global::Windows.Media.IMediaExtension
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.MediaProperties.AudioEncodingProperties> SupportedEncodingProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool UseInputFrameForOutput
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.IBasicAudioEffect.UseInputFrameForOutput.get
		// Forced skipping of method Windows.Media.Effects.IBasicAudioEffect.SupportedEncodingProperties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetEncodingProperties( global::Windows.Media.MediaProperties.AudioEncodingProperties encodingProperties);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void ProcessFrame( global::Windows.Media.Effects.ProcessAudioFrameContext context);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Close( global::Windows.Media.Effects.MediaEffectClosedReason reason);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void DiscardQueuedFrames();
		#endif
	}
}
