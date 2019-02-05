#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Selector : global::Windows.UI.Xaml.Controls.ItemsControl
	{
		// Skipping already declared property SelectedItem
		// Skipping already declared property SelectedIndex
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool? IsSynchronizedWithCurrentItem
		{
			get
			{
				return (bool?)this.GetValue(IsSynchronizedWithCurrentItemProperty);
			}
			set
			{
				this.SetValue(IsSynchronizedWithCurrentItemProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSynchronizedWithCurrentItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSynchronizedWithCurrentItem", typeof(bool?), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(bool?)));
		#endif
		// Skipping already declared property SelectedIndexProperty
		// Skipping already declared property SelectedItemProperty
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedIndex.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedItem.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValue.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValue.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValuePath.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValuePath.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.IsSynchronizedWithCurrentItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.IsSynchronizedWithCurrentItem.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedIndexProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedItemProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValueProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.SelectedValuePathProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.Selector.IsSynchronizedWithCurrentItemProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static bool GetIsSelectionActive( global::Windows.UI.Xaml.DependencyObject element)
		{
			throw new global::System.NotImplementedException("The member bool Selector.GetIsSelectionActive(DependencyObject element) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared event Windows.UI.Xaml.Controls.Primitives.Selector.SelectionChanged
	}
}
