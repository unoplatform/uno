#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppBarSeparator : global::Windows.UI.Xaml.Controls.ICommandBarElement,global::Windows.UI.Xaml.Controls.ICommandBarElement2
	{
		#if false 
		[global::Uno.NotImplemented]
		public  bool IsCompact
		{
			get
			{
				return (bool)this.GetValue(IsCompactProperty);
			}
			set
			{
				this.SetValue(IsCompactProperty, value);
			}
		}
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public  int DynamicOverflowOrder
		{
			get
			{
				return (int)this.GetValue(DynamicOverflowOrderProperty);
			}
			set
			{
				this.SetValue(DynamicOverflowOrderProperty, value);
			}
		}
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public  bool IsInOverflow
		{
			get
			{
				return (bool)this.GetValue(IsInOverflowProperty);
			}
		}
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsCompactProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsCompact", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarSeparator), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DynamicOverflowOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DynamicOverflowOrder", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarSeparator), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsInOverflowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsInOverflow", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarSeparator), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false 
		[global::Uno.NotImplemented]
		public AppBarSeparator() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBarSeparator", "AppBarSeparator.AppBarSeparator()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.AppBarSeparator()
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.IsCompact.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.IsCompact.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.IsInOverflow.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.DynamicOverflowOrder.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.DynamicOverflowOrder.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.IsInOverflowProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.DynamicOverflowOrderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarSeparator.IsCompactProperty.get
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement2
	}
}
