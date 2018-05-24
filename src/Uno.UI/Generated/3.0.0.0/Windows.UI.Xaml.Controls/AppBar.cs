#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppBar 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsSticky
		{
			get
			{
				return (bool)this.GetValue(IsStickyProperty);
			}
			set
			{
				this.SetValue(IsStickyProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsOpen
		{
			get
			{
				return (bool)this.GetValue(IsOpenProperty);
			}
			set
			{
				this.SetValue(IsOpenProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.AppBarClosedDisplayMode ClosedDisplayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.AppBarClosedDisplayMode)this.GetValue(ClosedDisplayModeProperty);
			}
			set
			{
				this.SetValue(ClosedDisplayModeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.AppBarTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppBarTemplateSettings AppBar.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsStickyProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSticky", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ClosedDisplayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ClosedDisplayMode", typeof(global::Windows.UI.Xaml.Controls.AppBarClosedDisplayMode), 
			typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.AppBarClosedDisplayMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.AppBar), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public AppBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "AppBar.AppBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.AppBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsSticky.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsSticky.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Closed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Closed.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnClosed( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "void AppBar.OnClosed(object e)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnOpened( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "void AppBar.OnOpened(object e)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.ClosedDisplayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.ClosedDisplayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Opening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Opening.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Closing.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.Closing.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnClosing( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "void AppBar.OnClosing(object e)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnOpening( object e)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "void AppBar.OnOpening(object e)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.ClosedDisplayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBar.IsStickyProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Closed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Closed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Closed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Opened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Opened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Opened");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Closing
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Closing");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Closing");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> Opening
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Opening");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBar", "event EventHandler<object> AppBar.Opening");
			}
		}
		#endif
	}
}
