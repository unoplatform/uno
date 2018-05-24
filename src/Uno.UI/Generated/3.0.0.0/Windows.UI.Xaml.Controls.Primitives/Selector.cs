#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Selector : global::Windows.UI.Xaml.Controls.ItemsControl
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string SelectedValuePath
		{
			get
			{
				return (string)this.GetValue(SelectedValuePathProperty);
			}
			set
			{
				this.SetValue(SelectedValuePathProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  object SelectedValue
		{
			get
			{
				return (object)this.GetValue(SelectedValueProperty);
			}
			set
			{
				this.SetValue(SelectedValueProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object SelectedItem
		{
			get
			{
				return (object)this.GetValue(SelectedItemProperty);
			}
			set
			{
				this.SetValue(SelectedItemProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int SelectedIndex
		{
			get
			{
				return (int)this.GetValue(SelectedIndexProperty);
			}
			set
			{
				this.SetValue(SelectedIndexProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSynchronizedWithCurrentItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSynchronizedWithCurrentItem", typeof(bool?), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(bool?)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedIndexProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedIndex", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedItem", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedValuePathProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedValuePath", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedValueProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedValue", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.Selector), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static bool GetIsSelectionActive( global::Windows.UI.Xaml.DependencyObject element)
		{
			throw new global::System.NotImplementedException("The member bool Selector.GetIsSelectionActive(DependencyObject element) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.SelectionChangedEventHandler SelectionChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.Selector", "event SelectionChangedEventHandler Selector.SelectionChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.Selector", "event SelectionChangedEventHandler Selector.SelectionChanged");
			}
		}
		#endif
	}
}
