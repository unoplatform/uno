#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioNodeEmitterDecayModel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitterDecayKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitterDecayKind AudioNodeEmitterDecayModel.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double MaxGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioNodeEmitterDecayModel.MaxGain is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double MinGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioNodeEmitterDecayModel.MinGain is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitterNaturalDecayModelProperties NaturalProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitterNaturalDecayModelProperties AudioNodeEmitterDecayModel.NaturalProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterDecayModel.Kind.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterDecayModel.MinGain.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterDecayModel.MaxGain.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterDecayModel.NaturalProperties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioNodeEmitterDecayModel CreateNatural( double minGain,  double maxGain,  double unityGainDistance,  double cutoffDistance)
		{
			throw new global::System.NotImplementedException("The member AudioNodeEmitterDecayModel AudioNodeEmitterDecayModel.CreateNatural(double minGain, double maxGain, double unityGainDistance, double cutoffDistance) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioNodeEmitterDecayModel CreateCustom( double minGain,  double maxGain)
		{
			throw new global::System.NotImplementedException("The member AudioNodeEmitterDecayModel AudioNodeEmitterDecayModel.CreateCustom(double minGain, double maxGain) is not implemented in Uno.");
		}
		#endif
	}
}
