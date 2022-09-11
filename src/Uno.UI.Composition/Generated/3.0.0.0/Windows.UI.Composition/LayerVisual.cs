#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LayerVisual : global::Windows.UI.Composition.ContainerVisual
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionEffectBrush Effect
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionEffectBrush LayerVisual.Effect is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.LayerVisual", "CompositionEffectBrush LayerVisual.Effect");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionShadow Shadow
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionShadow LayerVisual.Shadow is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.LayerVisual", "CompositionShadow LayerVisual.Shadow");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.LayerVisual.Effect.get
		// Forced skipping of method Windows.UI.Composition.LayerVisual.Effect.set
		// Forced skipping of method Windows.UI.Composition.LayerVisual.Shadow.get
		// Forced skipping of method Windows.UI.Composition.LayerVisual.Shadow.set
	}
}
