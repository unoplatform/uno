#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToggleButton : global::Windows.UI.Xaml.Controls.Primitives.ButtonBase
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		// Skipping already declared property IsChecked
		// Skipping already declared property IsCheckedProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsThreeStateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsThreeState", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.ToggleButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.ToggleButton.ToggleButton()
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
		// Skipping already declared method Windows.UI.Xaml.Controls.Primitives.ToggleButton.OnToggle()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ToggleButton.IsThreeStateProperty.get
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.ToggleButton.Checked
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
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
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.ToggleButton.Unchecked
	}
}
