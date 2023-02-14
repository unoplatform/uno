#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Media
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class XamlCompositionBrushBase : global::Microsoft.UI.Xaml.Media.Brush
	{
		// Skipping already declared property FallbackColor
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Composition.CompositionBrush CompositionBrush
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionBrush XamlCompositionBrushBase.CompositionBrush is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CompositionBrush%20XamlCompositionBrushBase.CompositionBrush");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Media.XamlCompositionBrushBase", "CompositionBrush XamlCompositionBrushBase.CompositionBrush");
			}
		}
		#endif
		// Skipping already declared property FallbackColorProperty
		// Skipping already declared method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.XamlCompositionBrushBase()
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.XamlCompositionBrushBase()
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColor.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColor.set
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.CompositionBrush.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.CompositionBrush.set
		// Skipping already declared method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.OnConnected()
		// Skipping already declared method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.OnDisconnected()
		// Forced skipping of method Microsoft.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColorProperty.get
	}
}
