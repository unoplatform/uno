#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class VariableSizedWrapGrid : global::Windows.UI.Xaml.Controls.Panel
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.VerticalAlignment VerticalChildrenAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.VerticalAlignment)this.GetValue(VerticalChildrenAlignmentProperty);
			}
			set
			{
				this.SetValue(VerticalChildrenAlignmentProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaximumRowsOrColumns
		{
			get
			{
				return (int)this.GetValue(MaximumRowsOrColumnsProperty);
			}
			set
			{
				this.SetValue(MaximumRowsOrColumnsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double ItemWidth
		{
			get
			{
				return (double)this.GetValue(ItemWidthProperty);
			}
			set
			{
				this.SetValue(ItemWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double ItemHeight
		{
			get
			{
				return (double)this.GetValue(ItemHeightProperty);
			}
			set
			{
				this.SetValue(ItemHeightProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.HorizontalAlignment HorizontalChildrenAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.HorizontalAlignment)this.GetValue(HorizontalChildrenAlignmentProperty);
			}
			set
			{
				this.SetValue(HorizontalChildrenAlignmentProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ColumnSpanProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"ColumnSpan", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty HorizontalChildrenAlignmentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HorizontalChildrenAlignment), typeof(global::Windows.UI.Xaml.HorizontalAlignment), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.HorizontalAlignment)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ItemHeight), typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ItemWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ItemWidth), typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty MaximumRowsOrColumnsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MaximumRowsOrColumns), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Orientation), typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty RowSpanProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"RowSpan", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty VerticalChildrenAlignmentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(VerticalChildrenAlignment), typeof(global::Windows.UI.Xaml.VerticalAlignment), 
			typeof(global::Windows.UI.Xaml.Controls.VariableSizedWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.VerticalAlignment)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.VariableSizedWrapGrid()
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.VariableSizedWrapGrid()
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.HorizontalChildrenAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.HorizontalChildrenAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.VerticalChildrenAlignment.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.VerticalChildrenAlignment.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.MaximumRowsOrColumns.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.MaximumRowsOrColumns.set
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ItemWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.OrientationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.HorizontalChildrenAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.VerticalChildrenAlignmentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.MaximumRowsOrColumnsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.RowSpanProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static int GetRowSpan( global::Windows.UI.Xaml.UIElement element)
		{
			return (int)element.GetValue(RowSpanProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetRowSpan( global::Windows.UI.Xaml.UIElement element,  int value)
		{
			element.SetValue(RowSpanProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.VariableSizedWrapGrid.ColumnSpanProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static int GetColumnSpan( global::Windows.UI.Xaml.UIElement element)
		{
			return (int)element.GetValue(ColumnSpanProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetColumnSpan( global::Windows.UI.Xaml.UIElement element,  int value)
		{
			element.SetValue(ColumnSpanProperty, value);
		}
		#endif
	}
}
