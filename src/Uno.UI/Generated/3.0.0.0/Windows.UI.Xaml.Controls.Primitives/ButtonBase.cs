#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ButtonBase : global::Windows.UI.Xaml.Controls.ContentControl
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object CommandParameter
		{
			get
			{
				return (object)this.GetValue(CommandParameterProperty);
			}
			set
			{
				this.SetValue(CommandParameterProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Windows.Input.ICommand Command
		{
			get
			{
				return (global::System.Windows.Input.ICommand)this.GetValue(CommandProperty);
			}
			set
			{
				this.SetValue(CommandProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ClickMode ClickMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.ClickMode)this.GetValue(ClickModeProperty);
			}
			set
			{
				this.SetValue(ClickModeProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsPointerOver
		{
			get
			{
				return (bool)this.GetValue(IsPointerOverProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsPressed
		{
			get
			{
				return (bool)this.GetValue(IsPressedProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ClickModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ClickMode", typeof(global::Windows.UI.Xaml.Controls.ClickMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ClickMode)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CommandParameterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CommandParameter", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CommandProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Command", typeof(global::System.Windows.Input.ICommand), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPointerOverProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPointerOver", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPressedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPressed", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected ButtonBase() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ButtonBase", "ButtonBase.ButtonBase()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.ButtonBase()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.ClickMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.ClickMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.IsPointerOver.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.IsPressed.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.Command.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.Command.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameter.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.Click.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.Click.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.ClickModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.IsPointerOverProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.IsPressedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.CommandProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler Click
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ButtonBase", "event RoutedEventHandler ButtonBase.Click");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ButtonBase", "event RoutedEventHandler ButtonBase.Click");
			}
		}
		#endif
	}
}
