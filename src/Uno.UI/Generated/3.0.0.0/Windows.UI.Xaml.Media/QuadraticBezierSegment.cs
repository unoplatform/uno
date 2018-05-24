#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class QuadraticBezierSegment : global::Windows.UI.Xaml.Media.PathSegment
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Point2
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(Point2Property);
			}
			set
			{
				this.SetValue(Point2Property, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Point1
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(Point1Property);
			}
			set
			{
				this.SetValue(Point1Property, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty Point1Property { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Point1", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.QuadraticBezierSegment), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty Point2Property { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Point2", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.QuadraticBezierSegment), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public QuadraticBezierSegment() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.QuadraticBezierSegment", "QuadraticBezierSegment.QuadraticBezierSegment()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.QuadraticBezierSegment()
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point1.get
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point1.set
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point2.get
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point2.set
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point1Property.get
		// Forced skipping of method Windows.UI.Xaml.Media.QuadraticBezierSegment.Point2Property.get
	}
}
