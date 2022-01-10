#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpringVector2NaturalMotionAnimation : global::Windows.UI.Composition.Vector2NaturalMotionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Period
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SpringVector2NaturalMotionAnimation.Period is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpringVector2NaturalMotionAnimation", "TimeSpan SpringVector2NaturalMotionAnimation.Period");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float DampingRatio
		{
			get
			{
				throw new global::System.NotImplementedException("The member float SpringVector2NaturalMotionAnimation.DampingRatio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpringVector2NaturalMotionAnimation", "float SpringVector2NaturalMotionAnimation.DampingRatio");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.SpringVector2NaturalMotionAnimation.DampingRatio.get
		// Forced skipping of method Windows.UI.Composition.SpringVector2NaturalMotionAnimation.DampingRatio.set
		// Forced skipping of method Windows.UI.Composition.SpringVector2NaturalMotionAnimation.Period.get
		// Forced skipping of method Windows.UI.Composition.SpringVector2NaturalMotionAnimation.Period.set
	}
}
