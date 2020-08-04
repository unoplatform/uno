#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class XamlCompositionBrushBase : global::Windows.UI.Xaml.Media.Brush
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color FallbackColor
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(FallbackColorProperty);
			}
			set
			{
				this.SetValue(FallbackColorProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionBrush CompositionBrush
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionBrush XamlCompositionBrushBase.CompositionBrush is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.XamlCompositionBrushBase", "CompositionBrush XamlCompositionBrushBase.CompositionBrush");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FallbackColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FallbackColor), typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Media.XamlCompositionBrushBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected XamlCompositionBrushBase() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.XamlCompositionBrushBase", "XamlCompositionBrushBase.XamlCompositionBrushBase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.XamlCompositionBrushBase()
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColor.get
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColor.set
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.CompositionBrush.get
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.CompositionBrush.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void OnConnected()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.XamlCompositionBrushBase", "void XamlCompositionBrushBase.OnConnected()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected virtual void OnDisconnected()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.XamlCompositionBrushBase", "void XamlCompositionBrushBase.OnDisconnected()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.XamlCompositionBrushBase.FallbackColorProperty.get
	}
}
