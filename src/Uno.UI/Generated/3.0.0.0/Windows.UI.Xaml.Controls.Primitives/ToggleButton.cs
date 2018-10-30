#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleButton : global::Windows.UI.Xaml.Controls.Primitives.ButtonBase
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsThreeState
		{
			get
			{
				return (bool)this.GetValue(IsThreeStateProperty);
			}
			set
			{
				this.SetValue(IsThreeStateProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool? IsChecked
		{
			get
			{
				return (bool?)this.GetValue(IsCheckedProperty);
			}
			set
			{
				this.SetValue(IsCheckedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsCheckedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsChecked", typeof(bool?), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ToggleButton), 
			new FrameworkPropertyMetadata(default(bool?)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsThreeStateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsThreeState", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ToggleButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ToggleButton() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "ToggleButton.ToggleButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.ToggleButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsChecked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsChecked.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsThreeState.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsThreeState.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Checked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Checked.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Unchecked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Unchecked.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Indeterminate.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.Indeterminate.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsThreeStateProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler Checked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Checked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Checked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler Indeterminate
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Indeterminate");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Indeterminate");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler Unchecked
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Unchecked");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ToggleButton", "event RoutedEventHandler ToggleButton.Unchecked");
			}
		}
		#endif
	}
}
