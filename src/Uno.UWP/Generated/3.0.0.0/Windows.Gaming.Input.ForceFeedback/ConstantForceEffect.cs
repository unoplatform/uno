#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConstantForceEffect : global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ConstantForceEffect.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "double ConstantForceEffect.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackEffectState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ForceFeedbackEffectState ConstantForceEffect.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ConstantForceEffect() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "ConstantForceEffect.ConstantForceEffect()");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConstantForceEffect.ConstantForceEffect()
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConstantForceEffect.Gain.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConstantForceEffect.Gain.set
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConstantForceEffect.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "void ConstantForceEffect.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "void ConstantForceEffect.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParameters( global::System.Numerics.Vector3 vector,  global::System.TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "void ConstantForceEffect.SetParameters(Vector3 vector, TimeSpan duration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParametersWithEnvelope( global::System.Numerics.Vector3 vector,  float attackGain,  float sustainGain,  float releaseGain,  global::System.TimeSpan startDelay,  global::System.TimeSpan attackDuration,  global::System.TimeSpan sustainDuration,  global::System.TimeSpan releaseDuration,  uint repeatCount)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConstantForceEffect", "void ConstantForceEffect.SetParametersWithEnvelope(Vector3 vector, float attackGain, float sustainGain, float releaseGain, TimeSpan startDelay, TimeSpan attackDuration, TimeSpan sustainDuration, TimeSpan releaseDuration, uint repeatCount)");
		}
		#endif
		// Processing: Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	}
}
