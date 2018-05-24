#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Panel : global::Windows.UI.Xaml.FrameworkElement
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection ChildrenTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(ChildrenTransitionsProperty);
			}
			set
			{
				this.SetValue(ChildrenTransitionsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush Background
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(BackgroundProperty);
			}
			set
			{
				this.SetValue(BackgroundProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.UIElementCollection Children
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElementCollection Panel.Children is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsItemsHost
		{
			get
			{
				return (bool)this.GetValue(IsItemsHostProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BackgroundProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Background", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.Panel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ChildrenTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ChildrenTransitions", typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.Panel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsItemsHostProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsItemsHost", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Panel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected Panel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Panel", "Panel.Panel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.Panel()
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.Children.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.Background.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.Background.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.IsItemsHost.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.ChildrenTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.ChildrenTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.BackgroundProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.IsItemsHostProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Panel.ChildrenTransitionsProperty.get
	}
}
