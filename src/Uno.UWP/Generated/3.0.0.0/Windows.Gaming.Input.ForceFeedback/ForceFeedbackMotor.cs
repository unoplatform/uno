#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.ForceFeedback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ForceFeedbackMotor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double MasterGain
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ForceFeedbackMotor.MasterGain is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor", "double ForceFeedbackMotor.MasterGain");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AreEffectsPaused
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ForceFeedbackMotor.AreEffectsPaused is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ForceFeedbackMotor.IsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackEffectAxes SupportedAxes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ForceFeedbackEffectAxes ForceFeedbackMotor.SupportedAxes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor.AreEffectsPaused.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor.MasterGain.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor.MasterGain.set
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor.IsEnabled.get
		// Forced skipping of method Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor.SupportedAxes.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Gaming.Input.ForceFeedback.ForceFeedbackLoadEffectResult> LoadEffectAsync( global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect effect)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ForceFeedbackLoadEffectResult> ForceFeedbackMotor.LoadEffectAsync(IForceFeedbackEffect effect) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PauseAllEffects()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor", "void ForceFeedbackMotor.PauseAllEffects()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ResumeAllEffects()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor", "void ForceFeedbackMotor.ResumeAllEffects()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopAllEffects()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor", "void ForceFeedbackMotor.StopAllEffects()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisableAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ForceFeedbackMotor.TryDisableAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryEnableAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ForceFeedbackMotor.TryEnableAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryResetAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ForceFeedbackMotor.TryResetAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryUnloadEffectAsync( global::Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect effect)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ForceFeedbackMotor.TryUnloadEffectAsync(IForceFeedbackEffect effect) is not implemented in Uno.");
		}
		#endif
	}
}
