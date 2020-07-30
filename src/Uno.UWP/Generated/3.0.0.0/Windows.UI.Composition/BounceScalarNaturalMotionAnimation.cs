#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BounceScalarNaturalMotionAnimation : global::Windows.UI.Composition.ScalarNaturalMotionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Restitution
		{
			get
			{
				throw new global::System.NotImplementedException("The member float BounceScalarNaturalMotionAnimation.Restitution is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.BounceScalarNaturalMotionAnimation", "float BounceScalarNaturalMotionAnimation.Restitution");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Acceleration
		{
			get
			{
				throw new global::System.NotImplementedException("The member float BounceScalarNaturalMotionAnimation.Acceleration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.BounceScalarNaturalMotionAnimation", "float BounceScalarNaturalMotionAnimation.Acceleration");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.BounceScalarNaturalMotionAnimation.Acceleration.get
		// Forced skipping of method Windows.UI.Composition.BounceScalarNaturalMotionAnimation.Acceleration.set
		// Forced skipping of method Windows.UI.Composition.BounceScalarNaturalMotionAnimation.Restitution.get
		// Forced skipping of method Windows.UI.Composition.BounceScalarNaturalMotionAnimation.Restitution.set
	}
}
