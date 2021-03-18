#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false || false || false || false
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum CompositionBatchTypes 
	{
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.None
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.Animation
		// Skipping already declared field Windows.UI.Composition.CompositionBatchTypes.Effect
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		InfiniteAnimation,
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		AllAnimations,
		#endif
	}
	#endif
}
