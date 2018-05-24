#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RectangleGeometry : global::Windows.UI.Xaml.Media.Geometry
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Rect Rect
		{
			get
			{
				return (global::Windows.Foundation.Rect)this.GetValue(RectProperty);
			}
			set
			{
				this.SetValue(RectProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RectProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Rect", typeof(global::Windows.Foundation.Rect), 
			typeof(global::Windows.UI.Xaml.Media.RectangleGeometry), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Rect)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public RectangleGeometry() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.RectangleGeometry", "RectangleGeometry.RectangleGeometry()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.RectangleGeometry.RectangleGeometry()
		// Forced skipping of method Windows.UI.Xaml.Media.RectangleGeometry.Rect.get
		// Forced skipping of method Windows.UI.Xaml.Media.RectangleGeometry.Rect.set
		// Forced skipping of method Windows.UI.Xaml.Media.RectangleGeometry.RectProperty.get
	}
}
