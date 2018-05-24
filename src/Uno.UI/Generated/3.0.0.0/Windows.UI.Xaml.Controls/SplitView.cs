#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SplitView : global::Windows.UI.Xaml.Controls.Control
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.SplitViewPanePlacement PanePlacement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.SplitViewPanePlacement)this.GetValue(PanePlacementProperty);
			}
			set
			{
				this.SetValue(PanePlacementProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush PaneBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(PaneBackgroundProperty);
			}
			set
			{
				this.SetValue(PaneBackgroundProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement Pane
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(PaneProperty);
			}
			set
			{
				this.SetValue(PaneProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double OpenPaneLength
		{
			get
			{
				return (double)this.GetValue(OpenPaneLengthProperty);
			}
			set
			{
				this.SetValue(OpenPaneLengthProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsPaneOpen
		{
			get
			{
				return (bool)this.GetValue(IsPaneOpenProperty);
			}
			set
			{
				this.SetValue(IsPaneOpenProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.SplitViewDisplayMode DisplayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.SplitViewDisplayMode)this.GetValue(DisplayModeProperty);
			}
			set
			{
				this.SetValue(DisplayModeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement Content
		{
			get
			{
				return (global::Windows.UI.Xaml.UIElement)this.GetValue(ContentProperty);
			}
			set
			{
				this.SetValue(ContentProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double CompactPaneLength
		{
			get
			{
				return (double)this.GetValue(CompactPaneLengthProperty);
			}
			set
			{
				this.SetValue(CompactPaneLengthProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.SplitViewTemplateSettings TemplateSettings
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.SplitViewTemplateSettings)this.GetValue(TemplateSettingsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		public static global::Windows.UI.Xaml.DependencyProperty CompactPaneLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CompactPaneLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Content", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DisplayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayMode", typeof(global::Windows.UI.Xaml.Controls.SplitViewDisplayMode), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.SplitViewDisplayMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPaneOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPaneOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OpenPaneLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OpenPaneLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneBackgroundProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneBackground", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PanePlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PanePlacement", typeof(global::Windows.UI.Xaml.Controls.SplitViewPanePlacement), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.SplitViewPanePlacement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaneProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Pane", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TemplateSettingsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TemplateSettings", typeof(global::Windows.UI.Xaml.Controls.Primitives.SplitViewTemplateSettings), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.SplitViewTemplateSettings)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.SplitView), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public SplitView() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "SplitView.SplitView()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.SplitView()
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.Content.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.Content.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.Pane.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.Pane.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.IsPaneOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.IsPaneOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.OpenPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.OpenPaneLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.CompactPaneLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.CompactPaneLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PanePlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PanePlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.DisplayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.DisplayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneBackground.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneBackground.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneClosing.add
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneClosing.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneClosed.add
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneClosed.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneOpening.add
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneOpening.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneOpened.add
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneOpened.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.ContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.IsPaneOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.OpenPaneLengthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.CompactPaneLengthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PanePlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.DisplayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.TemplateSettingsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SplitView.PaneBackgroundProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.SplitView, object> PaneClosed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneClosed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneClosed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.SplitView, global::Windows.UI.Xaml.Controls.SplitViewPaneClosingEventArgs> PaneClosing
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> SplitView.PaneClosing");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> SplitView.PaneClosing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.SplitView, object> PaneOpened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneOpened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneOpened");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.SplitView, object> PaneOpening
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneOpening");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SplitView", "event TypedEventHandler<SplitView, object> SplitView.PaneOpening");
			}
		}
		#endif
	}
}
