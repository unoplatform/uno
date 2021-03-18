#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PeriodicForceEffect : global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PeriodicForceEffect.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "double PeriodicForceEffect.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackEffectState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ForceFeedbackEffectState PeriodicForceEffect.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.PeriodicForceEffectKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member PeriodicForceEffectKind PeriodicForceEffect.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PeriodicForceEffect( global::Windows.Gaming.Input.ForceFeedback.PeriodicForceEffectKind effectKind) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "PeriodicForceEffect.PeriodicForceEffect(PeriodicForceEffectKind effectKind)");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect.PeriodicForceEffect(Windows.Gaming.Input.ForceFeedback.PeriodicForceEffectKind)
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect.Gain.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect.Gain.set
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "void PeriodicForceEffect.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "void PeriodicForceEffect.Stop()");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParameters( global::System.Numerics.Vector3 vector,  float frequency,  float phase,  float bias,  global::System.TimeSpan duration)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "void PeriodicForceEffect.SetParameters(Vector3 vector, float frequency, float phase, float bias, TimeSpan duration)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParametersWithEnvelope( global::System.Numerics.Vector3 vector,  float frequency,  float phase,  float bias,  float attackGain,  float sustainGain,  float releaseGain,  global::System.TimeSpan startDelay,  global::System.TimeSpan attackDuration,  global::System.TimeSpan sustainDuration,  global::System.TimeSpan releaseDuration,  uint repeatCount)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.PeriodicForceEffect", "void PeriodicForceEffect.SetParametersWithEnvelope(Vector3 vector, float frequency, float phase, float bias, float attackGain, float sustainGain, float releaseGain, TimeSpan startDelay, TimeSpan attackDuration, TimeSpan sustainDuration, TimeSpan releaseDuration, uint repeatCount)");
		}
		#endif
		// Processing: Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	}
}
