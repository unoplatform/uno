#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Grid : global::Windows.UI.Xaml.Controls.Panel
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ColumnDefinitionCollection ColumnDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member ColumnDefinitionCollection Grid.ColumnDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.RowDefinitionCollection RowDefinitions
		{
			get
			{
				throw new global::System.NotImplementedException("The member RowDefinitionCollection Grid.RowDefinitions is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness Padding
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(PaddingProperty);
			}
			set
			{
				this.SetValue(PaddingProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.CornerRadius CornerRadius
		{
			get
			{
				return (global::Windows.UI.Xaml.CornerRadius)this.GetValue(CornerRadiusProperty);
			}
			set
			{
				this.SetValue(CornerRadiusProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness BorderThickness
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(BorderThicknessProperty);
			}
			set
			{
				this.SetValue(BorderThicknessProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush BorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(BorderBrushProperty);
			}
			set
			{
				this.SetValue(BorderBrushProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double RowSpacing
		{
			get
			{
				return (double)this.GetValue(RowSpacingProperty);
			}
			set
			{
				this.SetValue(RowSpacingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double ColumnSpacing
		{
			get
			{
				return (double)this.GetValue(ColumnSpacingProperty);
			}
			set
			{
				this.SetValue(ColumnSpacingProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColumnProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Column", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColumnSpanProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"ColumnSpan", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Row", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RowSpanProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"RowSpan", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderBrushProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderBrush", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderThicknessProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderThickness", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CornerRadiusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CornerRadius", typeof(global::Windows.UI.Xaml.CornerRadius), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.CornerRadius)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Padding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColumnSpacingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ColumnSpacing", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RowSpacingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RowSpacing", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.Grid), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Grid() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Grid", "Grid.Grid()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.Grid()
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowDefinitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnDefinitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderThickness.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderThickness.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.CornerRadius.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.CornerRadius.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.Padding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowSpacing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowSpacing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnSpacing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnSpacing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowSpacingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnSpacingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.BorderThicknessProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.CornerRadiusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.PaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.GetRow(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.SetRow(Windows.UI.Xaml.FrameworkElement, int)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.GetColumn(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.SetColumn(Windows.UI.Xaml.FrameworkElement, int)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.RowSpanProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.GetRowSpan(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.SetRowSpan(Windows.UI.Xaml.FrameworkElement, int)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.ColumnSpanProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.GetColumnSpan(Windows.UI.Xaml.FrameworkElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.Grid.SetColumnSpan(Windows.UI.Xaml.FrameworkElement, int)
	}
}
