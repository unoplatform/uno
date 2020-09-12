#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionTarget 
	{
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.Rendered.add
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.Rendered.remove
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.Rendering.add
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.Rendering.remove
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.SurfaceContentsLost.add
		// Forced skipping of method Windows.UI.Xaml.Media.CompositionTarget.SurfaceContentsLost.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.UI.Xaml.Media.RenderedEventArgs> Rendered
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<RenderedEventArgs> CompositionTarget.Rendered");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<RenderedEventArgs> CompositionTarget.Rendered");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> Rendering
		{
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<object> CompositionTarget.Rendering");
			}
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<object> CompositionTarget.Rendering");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> SurfaceContentsLost
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<object> CompositionTarget.SurfaceContentsLost");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.CompositionTarget", "event EventHandler<object> CompositionTarget.SurfaceContentsLost");
			}
		}
		#endif
	}
}
