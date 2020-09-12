#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConnectedAnimationService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionEasingFunction DefaultEasingFunction
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionEasingFunction ConnectedAnimationService.DefaultEasingFunction is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ConnectedAnimationService", "CompositionEasingFunction ConnectedAnimationService.DefaultEasingFunction");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan DefaultDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan ConnectedAnimationService.DefaultDuration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ConnectedAnimationService", "TimeSpan ConnectedAnimationService.DefaultDuration");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ConnectedAnimationService.DefaultDuration.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ConnectedAnimationService.DefaultDuration.set
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ConnectedAnimationService.DefaultEasingFunction.get
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ConnectedAnimationService.DefaultEasingFunction.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.ConnectedAnimation PrepareToAnimate( string key,  global::Windows.UI.Xaml.UIElement source)
		{
			throw new global::System.NotImplementedException("The member ConnectedAnimation ConnectedAnimationService.PrepareToAnimate(string key, UIElement source) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Animation.ConnectedAnimation GetAnimation( string key)
		{
			throw new global::System.NotImplementedException("The member ConnectedAnimation ConnectedAnimationService.GetAnimation(string key) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.Media.Animation.ConnectedAnimationService GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member ConnectedAnimationService ConnectedAnimationService.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
	}
}
