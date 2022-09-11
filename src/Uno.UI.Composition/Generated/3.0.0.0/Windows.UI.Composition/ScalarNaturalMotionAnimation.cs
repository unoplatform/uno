#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScalarNaturalMotionAnimation : global::Windows.UI.Composition.NaturalMotionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float InitialVelocity
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ScalarNaturalMotionAnimation.InitialVelocity is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.ScalarNaturalMotionAnimation", "float ScalarNaturalMotionAnimation.InitialVelocity");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float? InitialValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member float? ScalarNaturalMotionAnimation.InitialValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.ScalarNaturalMotionAnimation", "float? ScalarNaturalMotionAnimation.InitialValue");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float? FinalValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member float? ScalarNaturalMotionAnimation.FinalValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.ScalarNaturalMotionAnimation", "float? ScalarNaturalMotionAnimation.FinalValue");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.FinalValue.get
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.FinalValue.set
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.InitialValue.get
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.InitialValue.set
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.InitialVelocity.get
		// Forced skipping of method Windows.UI.Composition.ScalarNaturalMotionAnimation.InitialVelocity.set
	}
}
