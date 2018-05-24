#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PathFigure : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point StartPoint
		{
			get
			{
				return (global::Windows.Foundation.Point)this.GetValue(StartPointProperty);
			}
			set
			{
				this.SetValue(StartPointProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.PathSegmentCollection Segments
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.PathSegmentCollection)this.GetValue(SegmentsProperty);
			}
			set
			{
				this.SetValue(SegmentsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsFilled
		{
			get
			{
				return (bool)this.GetValue(IsFilledProperty);
			}
			set
			{
				this.SetValue(IsFilledProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsClosed
		{
			get
			{
				return (bool)this.GetValue(IsClosedProperty);
			}
			set
			{
				this.SetValue(IsClosedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsClosedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsClosed", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.PathFigure), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsFilledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsFilled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.PathFigure), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SegmentsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Segments", typeof(global::Windows.UI.Xaml.Media.PathSegmentCollection), 
			typeof(global::Windows.UI.Xaml.Media.PathFigure), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.PathSegmentCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StartPointProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"StartPoint", typeof(global::Windows.Foundation.Point), 
			typeof(global::Windows.UI.Xaml.Media.PathFigure), 
			new FrameworkPropertyMetadata(default(global::Windows.Foundation.Point)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PathFigure() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.PathFigure", "PathFigure.PathFigure()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.PathFigure()
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.Segments.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.Segments.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.StartPoint.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.StartPoint.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsClosed.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsClosed.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsFilled.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsFilled.set
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.SegmentsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.StartPointProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsClosedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.PathFigure.IsFilledProperty.get
	}
}
