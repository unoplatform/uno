#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LineSegment : global::Windows.UI.Xaml.Media.PathSegment
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Point
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(PointProperty);
			}
			set
			{
				this.SetValue(PointProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PointProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Point", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.LineSegment), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public LineSegment() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.LineSegment", "LineSegment.LineSegment()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.LineSegment.LineSegment()
		// Forced skipping of method Windows.UI.Xaml.Media.LineSegment.Point.get
		// Forced skipping of method Windows.UI.Xaml.Media.LineSegment.Point.set
		// Forced skipping of method Windows.UI.Xaml.Media.LineSegment.PointProperty.get
	}
}
