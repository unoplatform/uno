#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LoopingSelector : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ShouldLoop
		{
			get
			{
				return (bool)this.GetValue(ShouldLoopProperty);
			}
			set
			{
				this.SetValue(ShouldLoopProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<object> Items
		{
			get
			{
				return (global::System.Collections.Generic.IList<object>)this.GetValue(ItemsProperty);
			}
			set
			{
				this.SetValue(ItemsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ItemWidth
		{
			get
			{
				return (int)this.GetValue(ItemWidthProperty);
			}
			set
			{
				this.SetValue(ItemWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DataTemplate ItemTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(ItemTemplateProperty);
			}
			set
			{
				this.SetValue(ItemTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int ItemHeight
		{
			get
			{
				return (int)this.GetValue(ItemHeightProperty);
			}
			set
			{
				this.SetValue(ItemHeightProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ItemHeight), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ItemTemplate), typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ItemWidth), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Items), typeof(global::System.Collections.Generic.IList<object>), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<object>)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedIndexProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedIndex), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedItem), typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ShouldLoopProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ShouldLoop), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Primitives.LoopingSelector), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ShouldLoop.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ShouldLoop.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.Items.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.Items.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedIndex.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedItem.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ShouldLoopProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedIndexProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.SelectedItemProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.LoopingSelector.ItemTemplateProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.UI.Xaml.Controls.SelectionChangedEventHandler SelectionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.LoopingSelector", "event SelectionChangedEventHandler LoopingSelector.SelectionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.LoopingSelector", "event SelectionChangedEventHandler LoopingSelector.SelectionChanged");
			}
		}
		#endif
	}
}
