#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PaneThemeTransition : global::Microsoft.UI.Xaml.Media.Animation.Transition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.Primitives.EdgeTransitionLocation Edge
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.Primitives.EdgeTransitionLocation)this.GetValue(EdgeProperty);
			}
			set
			{
				this.SetValue(EdgeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty EdgeProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Edge), typeof(global::Microsoft.UI.Xaml.Controls.Primitives.EdgeTransitionLocation), 
			typeof(global::Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.Primitives.EdgeTransitionLocation)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PaneThemeTransition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition", "PaneThemeTransition.PaneThemeTransition()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition.PaneThemeTransition()
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition.Edge.get
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition.Edge.set
		// Forced skipping of method Microsoft.UI.Xaml.Media.Animation.PaneThemeTransition.EdgeProperty.get
	}
}
