#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Interactions
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InteractionTrackerInertiaNaturalMotion : global::Windows.UI.Composition.Interactions.InteractionTrackerInertiaModifier
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.ScalarNaturalMotionAnimation NaturalMotion
		{
			get
			{
				throw new global::System.NotImplementedException("The member ScalarNaturalMotionAnimation InteractionTrackerInertiaNaturalMotion.NaturalMotion is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion", "ScalarNaturalMotionAnimation InteractionTrackerInertiaNaturalMotion.NaturalMotion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.ExpressionAnimation Condition
		{
			get
			{
				throw new global::System.NotImplementedException("The member ExpressionAnimation InteractionTrackerInertiaNaturalMotion.Condition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion", "ExpressionAnimation InteractionTrackerInertiaNaturalMotion.Condition");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion.Condition.get
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion.Condition.set
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion.NaturalMotion.get
		// Forced skipping of method Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion.NaturalMotion.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Composition.Interactions.InteractionTrackerInertiaNaturalMotion Create( global::Windows.UI.Composition.Compositor compositor)
		{
			throw new global::System.NotImplementedException("The member InteractionTrackerInertiaNaturalMotion InteractionTrackerInertiaNaturalMotion.Create(Compositor compositor) is not implemented in Uno.");
		}
		#endif
	}
}
