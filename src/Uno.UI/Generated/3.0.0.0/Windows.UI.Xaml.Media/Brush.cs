#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Brush : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Transform Transform
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Transform)this.GetValue(TransformProperty);
			}
			set
			{
				this.SetValue(TransformProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Transform RelativeTransform
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Transform)this.GetValue(RelativeTransformProperty);
			}
			set
			{
				this.SetValue(RelativeTransformProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Opacity
		{
			get
			{
				return (double)this.GetValue(OpacityProperty);
			}
			set
			{
				this.SetValue(OpacityProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OpacityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Opacity", typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Brush), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RelativeTransformProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RelativeTransform", typeof(global::Windows.UI.Xaml.Media.Transform), 
			typeof(global::Windows.UI.Xaml.Media.Brush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Transform)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TransformProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Transform", typeof(global::Windows.UI.Xaml.Media.Transform), 
			typeof(global::Windows.UI.Xaml.Media.Brush), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Transform)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected Brush() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Brush", "Brush.Brush()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.Brush()
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.Opacity.get
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.Opacity.set
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.Transform.get
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.Transform.set
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.RelativeTransform.get
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.RelativeTransform.set
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.OpacityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.TransformProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Brush.RelativeTransformProperty.get
	}
}
