#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Frame : global::Windows.UI.Xaml.Controls.ContentControl,global::Windows.UI.Xaml.Controls.INavigate
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Type SourcePageType
		{
			get
			{
				return (global::System.Type)this.GetValue(SourcePageTypeProperty);
			}
			set
			{
				this.SetValue(SourcePageTypeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int CacheSize
		{
			get
			{
				return (int)this.GetValue(CacheSizeProperty);
			}
			set
			{
				this.SetValue(CacheSizeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int BackStackDepth
		{
			get
			{
				return (int)this.GetValue(BackStackDepthProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool CanGoBack
		{
			get
			{
				return (bool)this.GetValue(CanGoBackProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool CanGoForward
		{
			get
			{
				return (bool)this.GetValue(CanGoForwardProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Type CurrentSourcePageType
		{
			get
			{
				return (global::System.Type)this.GetValue(CurrentSourcePageTypeProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry> BackStack
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>)this.GetValue(BackStackProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry> ForwardStack
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>)this.GetValue(ForwardStackProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BackStackDepthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BackStackDepth", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CacheSizeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CacheSize", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CanGoBackProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CanGoBack", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CanGoForwardProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CanGoForward", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CurrentSourcePageTypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CurrentSourcePageType", typeof(global::System.Type), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(global::System.Type)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SourcePageTypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SourcePageType", typeof(global::System.Type), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(global::System.Type)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BackStackProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BackStack", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ForwardStackProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ForwardStack", typeof(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>), 
			typeof(global::Windows.UI.Xaml.Controls.Frame), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Navigation.PageStackEntry>)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Frame() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "Frame.Frame()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.Frame()
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CacheSize.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CacheSize.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CanGoBack.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CanGoForward.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CurrentSourcePageType.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.SourcePageType.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.SourcePageType.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.BackStackDepth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.Navigated.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.Navigated.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.Navigating.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.Navigating.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.NavigationFailed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.NavigationFailed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.NavigationStopped.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.NavigationStopped.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void GoBack()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "void Frame.GoBack()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void GoForward()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "void Frame.GoForward()");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Navigate( global::System.Type sourcePageType,  object parameter)
		{
			throw new global::System.NotImplementedException("The member bool Frame.Navigate(Type sourcePageType, object parameter) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string GetNavigationState()
		{
			throw new global::System.NotImplementedException("The member string Frame.GetNavigationState() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void SetNavigationState( string navigationState)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "void Frame.SetNavigationState(string navigationState)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Navigate( global::System.Type sourcePageType)
		{
			throw new global::System.NotImplementedException("The member bool Frame.Navigate(Type sourcePageType) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.BackStack.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.ForwardStack.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool Navigate( global::System.Type sourcePageType,  object parameter,  global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo infoOverride)
		{
			throw new global::System.NotImplementedException("The member bool Frame.Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  void GoBack( global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfoOverride)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "void Frame.GoBack(NavigationTransitionInfo transitionInfoOverride)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void SetNavigationState( string navigationState,  bool suppressNavigate)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "void Frame.SetNavigationState(string navigationState, bool suppressNavigate)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.BackStackProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.ForwardStackProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CacheSizeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CanGoBackProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CanGoForwardProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.CurrentSourcePageTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.SourcePageTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Frame.BackStackDepthProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Navigation.NavigatedEventHandler Navigated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigatedEventHandler Frame.Navigated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigatedEventHandler Frame.Navigated");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Navigation.NavigatingCancelEventHandler Navigating
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigatingCancelEventHandler Frame.Navigating");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigatingCancelEventHandler Frame.Navigating");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Navigation.NavigationFailedEventHandler NavigationFailed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigationFailedEventHandler Frame.NavigationFailed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigationFailedEventHandler Frame.NavigationFailed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Navigation.NavigationStoppedEventHandler NavigationStopped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigationStoppedEventHandler Frame.NavigationStopped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Frame", "event NavigationStoppedEventHandler Frame.NavigationStopped");
			}
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.INavigate
	}
}
