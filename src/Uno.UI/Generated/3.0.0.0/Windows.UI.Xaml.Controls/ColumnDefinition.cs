#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColumnDefinition : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.GridLength Width
		{
			get
			{
				return (global::Windows.UI.Xaml.GridLength)this.GetValue(WidthProperty);
			}
			set
			{
				this.SetValue(WidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double MinWidth
		{
			get
			{
				return (double)this.GetValue(MinWidthProperty);
			}
			set
			{
				this.SetValue(MinWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double MaxWidth
		{
			get
			{
				return (double)this.GetValue(MaxWidthProperty);
			}
			set
			{
				this.SetValue(MaxWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double ActualWidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ColumnDefinition.ActualWidth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ColumnDefinition), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ColumnDefinition), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty WidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Width", typeof(global::Windows.UI.Xaml.GridLength), 
			typeof(global::Windows.UI.Xaml.Controls.ColumnDefinition), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.GridLength)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ColumnDefinition() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ColumnDefinition", "ColumnDefinition.ColumnDefinition()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.ColumnDefinition()
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.Width.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.Width.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MaxWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MaxWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MinWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MinWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.ActualWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.WidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MaxWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ColumnDefinition.MinWidthProperty.get
	}
}
