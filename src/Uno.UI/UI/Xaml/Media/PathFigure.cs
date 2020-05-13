using Windows.Foundation;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Segments))]
	public partial class PathFigure : DependencyObject
	{
		public PathFigure() : base()
		{
			// This is done here to ensure that the Parent is set properly on the new PathFigureCollection.
			Segments = new PathSegmentCollection();
		}

		#region StartPoint

		public Point StartPoint
		{
			get => (Point)this.GetValue(StartPointProperty);
			set => this.SetValue(StartPointProperty, value);
		}

		public static DependencyProperty StartPointProperty { get; } =
			DependencyProperty.Register(
				"StartPoint", 
				typeof(Point),
				typeof(PathFigure),
				new FrameworkPropertyMetadata(
					defaultValue: new Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Segments

		public PathSegmentCollection Segments
		{
			get => (PathSegmentCollection)this.GetValue(SegmentsProperty);
			set => this.SetValue(SegmentsProperty, value);
		}
		
		public static DependencyProperty SegmentsProperty { get; } =
			DependencyProperty.Register(
				nameof(Segments),
				typeof(PathSegmentCollection),
				typeof(PathFigure),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext  | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region IsFilled

		public  bool IsFilled
		{
			get => (bool)this.GetValue(IsFilledProperty);
			set => this.SetValue(IsFilledProperty, value);
		}

		public static DependencyProperty IsFilledProperty { get; } =
			DependencyProperty.Register(
				"IsFilled", 
				typeof(bool),
				typeof(PathFigure),
				new FrameworkPropertyMetadata(
					defaultValue: true, 
					options: FrameworkPropertyMetadataOptions.AffectsRender
				)
			);

		#endregion

		#region IsClosed

		public bool IsClosed
		{
			get => (bool)this.GetValue(IsClosedProperty);
			set => this.SetValue(IsClosedProperty, value);
		}

		public static DependencyProperty IsClosedProperty { get; } =
			DependencyProperty.Register(
				"IsClosed", 
				typeof(bool),
				typeof(PathFigure),
				new FrameworkPropertyMetadata(
					defaultValue: false,
					options: FrameworkPropertyMetadataOptions.AffectsRender
				)
			);

		#endregion
	}
}
