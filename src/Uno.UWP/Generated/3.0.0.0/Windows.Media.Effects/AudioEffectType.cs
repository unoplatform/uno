#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AudioEffectType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AcousticEchoCancellation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoiseSuppression,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutomaticGainControl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BeamForming,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConstantToneRemoval,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Equalizer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoudnessEqualizer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BassBoost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VirtualSurround,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VirtualHeadphones,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpeakerFill,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RoomCorrection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BassManagement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnvironmentalEffects,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpeakerProtection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpeakerCompensation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DynamicRangeCompression,
		#endif
	}
	#endif
}
