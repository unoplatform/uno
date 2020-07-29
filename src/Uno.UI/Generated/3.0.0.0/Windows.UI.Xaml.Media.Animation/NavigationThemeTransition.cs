#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NavigationThemeTransition : global::Windows.UI.Xaml.Media.Animation.Transition
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo DefaultNavigationTransitionInfo
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo)this.GetValue(DefaultNavigationTransitionInfoProperty);
			}
			set
			{
				this.SetValue(DefaultNavigationTransitionInfoProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DefaultNavigationTransitionInfoProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DefaultNavigationTransitionInfo), typeof(global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo), 
			typeof(global::Windows.UI.Xaml.Media.Animation.NavigationThemeTransition), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NavigationThemeTransition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.NavigationThemeTransition", "NavigationThemeTransition.NavigationThemeTransition()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.NavigationThemeTransition.NavigationThemeTransition()
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.NavigationThemeTransition.DefaultNavigationTransitionInfo.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.NavigationThemeTransition.DefaultNavigationTransitionInfo.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.NavigationThemeTransition.DefaultNavigationTransitionInfoProperty.get
	}
}
