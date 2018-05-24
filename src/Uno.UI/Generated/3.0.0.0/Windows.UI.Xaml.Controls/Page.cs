#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Page : global::Windows.UI.Xaml.Controls.UserControl
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.AppBar TopAppBar
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.AppBar)this.GetValue(TopAppBarProperty);
			}
			set
			{
				this.SetValue(TopAppBarProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Navigation.NavigationCacheMode NavigationCacheMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member NavigationCacheMode Page.NavigationCacheMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Page", "NavigationCacheMode Page.NavigationCacheMode");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.AppBar BottomAppBar
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.AppBar)this.GetValue(BottomAppBarProperty);
			}
			set
			{
				this.SetValue(BottomAppBarProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Frame Frame
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Frame)this.GetValue(FrameProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BottomAppBarProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BottomAppBar", typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			typeof(global::Windows.UI.Xaml.Controls.Page), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.AppBar)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FrameProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Frame", typeof(global::Windows.UI.Xaml.Controls.Frame), 
			typeof(global::Windows.UI.Xaml.Controls.Page), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Frame)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TopAppBarProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopAppBar", typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			typeof(global::Windows.UI.Xaml.Controls.Page), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.AppBar)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Page() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Page", "Page.Page()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.Page()
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.Frame.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.NavigationCacheMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.NavigationCacheMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.TopAppBar.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.TopAppBar.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.BottomAppBar.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.BottomAppBar.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnNavigatedFrom( global::Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Page", "void Page.OnNavigatedFrom(NavigationEventArgs e)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnNavigatedTo( global::Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Page", "void Page.OnNavigatedTo(NavigationEventArgs e)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnNavigatingFrom( global::Windows.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Page", "void Page.OnNavigatingFrom(NavigatingCancelEventArgs e)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.FrameProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.TopAppBarProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Page.BottomAppBarProperty.get
	}
}
