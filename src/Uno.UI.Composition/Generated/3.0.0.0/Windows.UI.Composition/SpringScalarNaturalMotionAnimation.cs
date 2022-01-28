#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpringScalarNaturalMotionAnimation : global::Windows.UI.Composition.ScalarNaturalMotionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Period
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SpringScalarNaturalMotionAnimation.Period is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpringScalarNaturalMotionAnimation", "TimeSpan SpringScalarNaturalMotionAnimation.Period");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float DampingRatio
		{
			get
			{
				throw new global::System.NotImplementedException("The member float SpringScalarNaturalMotionAnimation.DampingRatio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpringScalarNaturalMotionAnimation", "float SpringScalarNaturalMotionAnimation.DampingRatio");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.SpringScalarNaturalMotionAnimation.DampingRatio.get
		// Forced skipping of method Windows.UI.Composition.SpringScalarNaturalMotionAnimation.DampingRatio.set
		// Forced skipping of method Windows.UI.Composition.SpringScalarNaturalMotionAnimation.Period.get
		// Forced skipping of method Windows.UI.Composition.SpringScalarNaturalMotionAnimation.Period.set
	}
}
