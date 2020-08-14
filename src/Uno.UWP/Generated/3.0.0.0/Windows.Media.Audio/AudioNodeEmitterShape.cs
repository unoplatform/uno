#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioNodeEmitterShape 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitterConeProperties ConeProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitterConeProperties AudioNodeEmitterShape.ConeProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioNodeEmitterShapeKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioNodeEmitterShapeKind AudioNodeEmitterShape.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterShape.Kind.get
		// Forced skipping of method Windows.Media.Audio.AudioNodeEmitterShape.ConeProperties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioNodeEmitterShape CreateCone( double innerAngle,  double outerAngle,  double outerAngleGain)
		{
			throw new global::System.NotImplementedException("The member AudioNodeEmitterShape AudioNodeEmitterShape.CreateCone(double innerAngle, double outerAngle, double outerAngleGain) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Audio.AudioNodeEmitterShape CreateOmnidirectional()
		{
			throw new global::System.NotImplementedException("The member AudioNodeEmitterShape AudioNodeEmitterShape.CreateOmnidirectional() is not implemented in Uno.");
		}
		#endif
	}
}
