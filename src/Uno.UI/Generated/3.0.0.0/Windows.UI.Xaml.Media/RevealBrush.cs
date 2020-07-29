#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RevealBrush : global::Windows.UI.Xaml.Media.XamlCompositionBrushBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.ApplicationTheme TargetTheme
		{
			get
			{
				return (global::Windows.UI.Xaml.ApplicationTheme)this.GetValue(TargetThemeProperty);
			}
			set
			{
				this.SetValue(TargetThemeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color Color
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(ColorProperty);
			}
			set
			{
				this.SetValue(ColorProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AlwaysUseFallback
		{
			get
			{
				return (bool)this.GetValue(AlwaysUseFallbackProperty);
			}
			set
			{
				this.SetValue(AlwaysUseFallbackProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty AlwaysUseFallbackProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(AlwaysUseFallback), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.RevealBrush), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Color), typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Media.RevealBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty StateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"State", typeof(global::Windows.UI.Xaml.Media.RevealBrushState), 
			typeof(global::Windows.UI.Xaml.Media.RevealBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.RevealBrushState)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty TargetThemeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TargetTheme), typeof(global::Windows.UI.Xaml.ApplicationTheme), 
			typeof(global::Windows.UI.Xaml.Media.RevealBrush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.ApplicationTheme)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		protected RevealBrush() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.RevealBrush", "RevealBrush.RevealBrush()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.RevealBrush()
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.Color.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.Color.set
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.TargetTheme.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.TargetTheme.set
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.AlwaysUseFallback.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.AlwaysUseFallback.set
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.ColorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.TargetThemeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.AlwaysUseFallbackProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBrush.StateProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetState( global::Windows.UI.Xaml.UIElement element,  global::Windows.UI.Xaml.Media.RevealBrushState value)
		{
			element.SetValue(StateProperty, value);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Media.RevealBrushState GetState( global::Windows.UI.Xaml.UIElement element)
		{
			return (global::Windows.UI.Xaml.Media.RevealBrushState)element.GetValue(StateProperty);
		}
		#endif
	}
}
