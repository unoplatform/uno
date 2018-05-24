#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geometry : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Rect Bounds
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect Geometry.Bounds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Media.Geometry Empty
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geometry Geometry.Empty is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static double StandardFlatteningTolerance
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geometry.StandardFlatteningTolerance is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TransformProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Transform", typeof(global::Windows.UI.Xaml.Media.Transform), 
			typeof(global::Windows.UI.Xaml.Media.Geometry), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Transform)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Transform.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Transform.set
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Bounds.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.Empty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.StandardFlatteningTolerance.get
		// Forced skipping of method Windows.UI.Xaml.Media.Geometry.TransformProperty.get
	}
}
