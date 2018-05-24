#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Shapes
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Rectangle : global::Windows.UI.Xaml.Shapes.Shape
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double RadiusY
		{
			get
			{
				return (double)this.GetValue(RadiusYProperty);
			}
			set
			{
				this.SetValue(RadiusYProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double RadiusX
		{
			get
			{
				return (double)this.GetValue(RadiusXProperty);
			}
			set
			{
				this.SetValue(RadiusXProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RadiusXProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RadiusX", typeof(double), 
			typeof(global::Windows.UI.Xaml.Shapes.Rectangle), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RadiusYProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RadiusY", typeof(double), 
			typeof(global::Windows.UI.Xaml.Shapes.Rectangle), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Rectangle() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Shapes.Rectangle", "Rectangle.Rectangle()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.Rectangle()
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusX.get
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusX.set
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusY.get
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusY.set
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusXProperty.get
		// Forced skipping of method Windows.UI.Xaml.Shapes.Rectangle.RadiusYProperty.get
	}
}
