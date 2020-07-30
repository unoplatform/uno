#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RampForceEffect : global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double RampForceEffect.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "double RampForceEffect.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackEffectState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ForceFeedbackEffectState RampForceEffect.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RampForceEffect() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "RampForceEffect.RampForceEffect()");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.RampForceEffect.RampForceEffect()
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.RampForceEffect.Gain.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.RampForceEffect.Gain.set
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.RampForceEffect.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "void RampForceEffect.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "void RampForceEffect.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParameters( global::System.Numerics.Vector3 startVector,  global::System.Numerics.Vector3 endVector,  global::System.TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "void RampForceEffect.SetParameters(Vector3 startVector, Vector3 endVector, TimeSpan duration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParametersWithEnvelope( global::System.Numerics.Vector3 startVector,  global::System.Numerics.Vector3 endVector,  float attackGain,  float sustainGain,  float releaseGain,  global::System.TimeSpan startDelay,  global::System.TimeSpan attackDuration,  global::System.TimeSpan sustainDuration,  global::System.TimeSpan releaseDuration,  uint repeatCount)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.RampForceEffect", "void RampForceEffect.SetParametersWithEnvelope(Vector3 startVector, Vector3 endVector, float attackGain, float sustainGain, float releaseGain, TimeSpan startDelay, TimeSpan attackDuration, TimeSpan sustainDuration, TimeSpan releaseDuration, uint repeatCount)");
		}
		#endif
		// Processing: Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	}
}
