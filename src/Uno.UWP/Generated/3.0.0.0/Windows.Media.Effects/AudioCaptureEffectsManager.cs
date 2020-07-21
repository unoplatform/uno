#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioCaptureEffectsManager 
	{
		// Forced skipping of method Windows.Media.Effects.AudioCaptureEffectsManager.AudioCaptureEffectsChanged.add
		// Forced skipping of method Windows.Media.Effects.AudioCaptureEffectsManager.AudioCaptureEffectsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Effects.AudioEffect> GetAudioCaptureEffects()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<AudioEffect> AudioCaptureEffectsManager.GetAudioCaptureEffects() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Effects.AudioCaptureEffectsManager, object> AudioCaptureEffectsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.AudioCaptureEffectsManager", "event TypedEventHandler<AudioCaptureEffectsManager, object> AudioCaptureEffectsManager.AudioCaptureEffectsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Effects.AudioCaptureEffectsManager", "event TypedEventHandler<AudioCaptureEffectsManager, object> AudioCaptureEffectsManager.AudioCaptureEffectsChanged");
			}
		}
		#endif
	}
}
