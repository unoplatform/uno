#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioNodeEmitterConeProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double InnerAngle
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioNodeEmitterConeProperties.InnerAngle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OuterAngle
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioNodeEmitterConeProperties.OuterAngle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double OuterAngleGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double AudioNodeEmitterConeProperties.OuterAngleGain is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterConeProperties.InnerAngle.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterConeProperties.OuterAngle.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterConeProperties.OuterAngleGain.get
	}
}
