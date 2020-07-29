#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBasicVideoEffect : global::Windows.Media.IMediaExtension
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsReadOnly
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.MediaProperties.VideoEncodingProperties> SupportedEncodingProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Effects.MediaMemoryTypes SupportedMemoryTypes
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool TimeIndependent
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.IBasicVideoEffect.IsReadOnly.get
		// Forced skipping of method Windows.Media.Effects.IBasicVideoEffect.SupportedMemoryTypes.get
		// Forced skipping of method Windows.Media.Effects.IBasicVideoEffect.TimeIndependent.get
		// Forced skipping of method Windows.Media.Effects.IBasicVideoEffect.SupportedEncodingProperties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetEncodingProperties( global::Windows.Media.MediaProperties.VideoEncodingProperties encodingProperties,  global::Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice device);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void ProcessFrame( global::Windows.Media.Effects.ProcessVideoFrameContext context);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Close( global::Windows.Media.Effects.MediaEffectClosedReason reason);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void DiscardQueuedFrames();
		#endif
	}
}
