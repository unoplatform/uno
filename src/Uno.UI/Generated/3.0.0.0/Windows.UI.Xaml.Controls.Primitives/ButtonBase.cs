#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ButtonBase : global::Windows.UI.Xaml.Controls.ContentControl
	{
		// Skipping already declared property CommandParameter
		// Skipping already declared property Command
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		// Skipping already declared property IsPointerOver
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsPressed
		{
			get
			{
				return (bool)this.GetValue(IsPressedProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ClickModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ClickMode", typeof(global::Windows.UI.Xaml.Controls.ClickMode), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.ClickMode)));
		#endif
		// Skipping already declared property CommandParameterProperty
		// Skipping already declared property CommandProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPointerOverProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPointerOver", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPressedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPressed", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ButtonBase), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.ButtonBase.ButtonBase()
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
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.ButtonBase.Click
	}
}
