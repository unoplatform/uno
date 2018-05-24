#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class GradientStop : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Offset
		{
			get
			{
				return (double)this.GetValue(OffsetProperty);
			}
			set
			{
				this.SetValue(OffsetProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color Color
		{
			get
			{
				return (global::Windows.UI.Color)this.GetValue(ColorProperty);
			}
			set
			{
				this.SetValue(ColorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ColorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Color", typeof(global::Windows.UI.Color), 
			typeof(global::Windows.UI.Xaml.Media.GradientStop), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OffsetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Offset", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.GradientStop), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public GradientStop() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.GradientStop", "GradientStop.GradientStop()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.GradientStop()
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.Color.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.Color.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.Offset.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.Offset.set
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.ColorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.GradientStop.OffsetProperty.get
	}
}
