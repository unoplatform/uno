#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RowDefinition : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double MinHeight
		{
			get
			{
				return (double)this.GetValue(MinHeightProperty);
			}
			set
			{
				this.SetValue(MinHeightProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double MaxHeight
		{
			get
			{
				return (double)this.GetValue(MaxHeightProperty);
			}
			set
			{
				this.SetValue(MaxHeightProperty, value);
			}
		}
		#endif
		// Skipping already declared property Height
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double ActualHeight
		{
			get
			{
				throw new global::System.NotImplementedException("The member double RowDefinition.ActualHeight is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property HeightProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.RowDefinition), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MinHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MinHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.RowDefinition), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.RowDefinition.RowDefinition()
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.RowDefinition()
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.Height.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.Height.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MaxHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MaxHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MinHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MinHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.ActualHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.HeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MaxHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RowDefinition.MinHeightProperty.get
	}
}
