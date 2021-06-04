#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false || false || false || false
	[global::System.FlagsAttribute]
	public   enum CompositionBatchTypes : uint
	{
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.None
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.Animation
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.Effect
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		InfiniteAnimation = 4,
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		AllAnimations = 5,
		#endif
	}
	#endif
}
