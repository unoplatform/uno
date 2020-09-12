#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConditionForceEffect : global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ConditionForceEffectKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ConditionForceEffectKind ConditionForceEffect.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Gain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ConditionForceEffect.Gain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConditionForceEffect", "double ConditionForceEffect.Gain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackEffectState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member ForceFeedbackEffectState ConditionForceEffect.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ConditionForceEffect( global::Windows.Gaming.Input.ForceFeedback.ConditionForceEffectKind effectKind) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConditionForceEffect", "ConditionForceEffect.ConditionForceEffect(ConditionForceEffectKind effectKind)");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConditionForceEffect.ConditionForceEffect(Windows.Gaming.Input.ForceFeedback.ConditionForceEffectKind)
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConditionForceEffect.Gain.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConditionForceEffect.Gain.set
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConditionForceEffect.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConditionForceEffect", "void ConditionForceEffect.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConditionForceEffect", "void ConditionForceEffect.Stop()");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ConditionForceEffect.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetParameters( global::System.Numerics.Vector3 direction,  float positiveCoefficient,  float negativeCoefficient,  float maxPositiveMagnitude,  float maxNegativeMagnitude,  float deadZone,  float bias)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ConditionForceEffect", "void ConditionForceEffect.SetParameters(Vector3 direction, float positiveCoefficient, float negativeCoefficient, float maxPositiveMagnitude, float maxNegativeMagnitude, float deadZone, float bias)");
		}
		#endif
		// Processing: Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect
	}
}
