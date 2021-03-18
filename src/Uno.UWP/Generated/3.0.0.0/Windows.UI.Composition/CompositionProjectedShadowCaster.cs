#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionProjectedShadowCaster : global::Windows.UI.Composition.CompositionObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.Visual CastingVisual
		{
			get
			{
				throw new global::System.NotImplementedException("The member Visual CompositionProjectedShadowCaster.CastingVisual is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionProjectedShadowCaster", "Visual CompositionProjectedShadowCaster.CastingVisual");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionBrush Brush
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionBrush CompositionProjectedShadowCaster.Brush is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionProjectedShadowCaster", "CompositionBrush CompositionProjectedShadowCaster.Brush");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionProjectedShadowCaster.Brush.get
		// Forced skipping of method Windows.UI.Composition.CompositionProjectedShadowCaster.Brush.set
		// Forced skipping of method Windows.UI.Composition.CompositionProjectedShadowCaster.CastingVisual.get
		// Forced skipping of method Windows.UI.Composition.CompositionProjectedShadowCaster.CastingVisual.set
	}
}
