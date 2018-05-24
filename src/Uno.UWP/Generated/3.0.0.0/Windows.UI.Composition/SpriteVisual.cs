#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpriteVisual : global::Windows.UI.Composition.ContainerVisual
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Composition.CompositionBrush Brush
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionBrush SpriteVisual.Brush is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpriteVisual", "CompositionBrush SpriteVisual.Brush");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Composition.CompositionShadow Shadow
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionShadow SpriteVisual.Shadow is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.SpriteVisual", "CompositionShadow SpriteVisual.Shadow");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.SpriteVisual.Brush.get
		// Forced skipping of method Windows.UI.Composition.SpriteVisual.Brush.set
		// Forced skipping of method Windows.UI.Composition.SpriteVisual.Shadow.get
		// Forced skipping of method Windows.UI.Composition.SpriteVisual.Shadow.set
	}
}
