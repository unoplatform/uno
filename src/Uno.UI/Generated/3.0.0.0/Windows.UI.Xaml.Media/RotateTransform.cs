#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RotateTransform : global::Windows.UI.Xaml.Media.Transform
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double CenterY
		{
			get
			{
				return (double)this.GetValue(CenterYProperty);
			}
			set
			{
				this.SetValue(CenterYProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double CenterX
		{
			get
			{
				return (double)this.GetValue(CenterXProperty);
			}
			set
			{
				this.SetValue(CenterXProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Angle
		{
			get
			{
				return (double)this.GetValue(AngleProperty);
			}
			set
			{
				this.SetValue(AngleProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AngleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Angle", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.RotateTransform), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CenterXProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CenterX", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.RotateTransform), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CenterYProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CenterY", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.RotateTransform), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public RotateTransform() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.RotateTransform", "RotateTransform.RotateTransform()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.RotateTransform()
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterX.get
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterX.set
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterY.get
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterY.set
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.Angle.get
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.Angle.set
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterXProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.CenterYProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.RotateTransform.AngleProperty.get
	}
}
