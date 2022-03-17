#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DelegatedInkTrailVisual : global::Windows.UI.Composition.Visual
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint AddTrailPoints( global::Windows.UI.Composition.InkTrailPoint[] inkPoints)
		{
			throw new global::System.NotImplementedException("The member uint DelegatedInkTrailVisual.AddTrailPoints(InkTrailPoint[] inkPoints) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint AddTrailPointsWithPrediction( global::Windows.UI.Composition.InkTrailPoint[] inkPoints,  global::Windows.UI.Composition.InkTrailPoint[] predictedInkPoints)
		{
			throw new global::System.NotImplementedException("The member uint DelegatedInkTrailVisual.AddTrailPointsWithPrediction(InkTrailPoint[] inkPoints, InkTrailPoint[] predictedInkPoints) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveTrailPoints( uint generationId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.DelegatedInkTrailVisual", "void DelegatedInkTrailVisual.RemoveTrailPoints(uint generationId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartNewTrail( global::Windows.UI.Color color)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.DelegatedInkTrailVisual", "void DelegatedInkTrailVisual.StartNewTrail(Color color)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Composition.DelegatedInkTrailVisual Create( global::Windows.UI.Composition.Compositor compositor)
		{
			throw new global::System.NotImplementedException("The member DelegatedInkTrailVisual DelegatedInkTrailVisual.Create(Compositor compositor) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Composition.DelegatedInkTrailVisual CreateForSwapChain( global::Windows.UI.Composition.Compositor compositor,  global::Windows.UI.Composition.ICompositionSurface swapChain)
		{
			throw new global::System.NotImplementedException("The member DelegatedInkTrailVisual DelegatedInkTrailVisual.CreateForSwapChain(Compositor compositor, ICompositionSurface swapChain) is not implemented in Uno.");
		}
		#endif
	}
}
