#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Navigation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameNavigationOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo TransitionInfoOverride
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationTransitionInfo FrameNavigationOptions.TransitionInfoOverride is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Navigation.FrameNavigationOptions", "NavigationTransitionInfo FrameNavigationOptions.TransitionInfoOverride");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsNavigationStackEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FrameNavigationOptions.IsNavigationStackEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Navigation.FrameNavigationOptions", "bool FrameNavigationOptions.IsNavigationStackEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public FrameNavigationOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Navigation.FrameNavigationOptions", "FrameNavigationOptions.FrameNavigationOptions()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Navigation.FrameNavigationOptions.FrameNavigationOptions()
		// Forced skipping of method Windows.UI.Xaml.Navigation.FrameNavigationOptions.IsNavigationStackEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.FrameNavigationOptions.IsNavigationStackEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Navigation.FrameNavigationOptions.TransitionInfoOverride.get
		// Forced skipping of method Windows.UI.Xaml.Navigation.FrameNavigationOptions.TransitionInfoOverride.set
	}
}
