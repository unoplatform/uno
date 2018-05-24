#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppBarToggleButton : global::Windows.UI.Xaml.Controls.ICommandBarElement,global::Windows.UI.Xaml.Controls.ICommandBarElement2
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Label
		{
			get
			{
				return (string)this.GetValue(LabelProperty);
			}
			set
			{
				this.SetValue(LabelProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.IconElement Icon
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.IconElement)this.GetValue(IconProperty);
			}
			set
			{
				this.SetValue(IconProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.CommandBarLabelPosition LabelPosition
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CommandBarLabelPosition)this.GetValue(LabelPositionProperty);
			}
			set
			{
				this.SetValue(LabelPositionProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string KeyboardAcceleratorTextOverride
		{
			get
			{
				return (string)this.GetValue(KeyboardAcceleratorTextOverrideProperty);
			}
			set
			{
				this.SetValue(KeyboardAcceleratorTextOverrideProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.AppBarToggleButtonTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppBarToggleButtonTemplateSettings AppBarToggleButton.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsCompact
		{
			get
			{
				return (bool)this.GetValue(IsCompactProperty);
			}
			set
			{
				this.SetValue(IsCompactProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int DynamicOverflowOrder
		{
			get
			{
				return (int)this.GetValue(DynamicOverflowOrderProperty);
			}
			set
			{
				this.SetValue(DynamicOverflowOrderProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsInOverflow
		{
			get
			{
				return (bool)this.GetValue(IsInOverflowProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsCompactProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsCompact", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LabelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Label", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DynamicOverflowOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DynamicOverflowOrder", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsInOverflowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsInOverflow", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LabelPositionProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LabelPosition", typeof(global::Windows.UI.Xaml.Controls.CommandBarLabelPosition), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CommandBarLabelPosition)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"KeyboardAcceleratorTextOverride", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarToggleButton), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public AppBarToggleButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBarToggleButton", "AppBarToggleButton.AppBarToggleButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.AppBarToggleButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.Label.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.Label.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.Icon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.Icon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IsCompact.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IsCompact.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.LabelPosition.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.LabelPosition.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IsInOverflow.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.DynamicOverflowOrder.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.DynamicOverflowOrder.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.KeyboardAcceleratorTextOverride.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.KeyboardAcceleratorTextOverride.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.KeyboardAcceleratorTextOverrideProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.LabelPositionProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IsInOverflowProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.DynamicOverflowOrderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.LabelProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarToggleButton.IsCompactProperty.get
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement2
	}
}
