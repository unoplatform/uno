using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial class BezierSegment : PathSegment
	{
		public BezierSegment()
		{
		}

		#region Point1

		public Point Point1
		{
			get => (Point)this.GetValue(Point1Property);
			set => this.SetValue(Point1Property, value);
		}

		public static DependencyProperty Point1Property { get; } =
			DependencyProperty.Register(
				"Point1",
				typeof(Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Point2

		public Point Point2
		{
			get => (Point)this.GetValue(Point2Property);
			set => this.SetValue(Point2Property, value);
		}

		public static DependencyProperty Point2Property { get; } =
			DependencyProperty.Register(
				"Point2",
				typeof(Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Point3

		public Point Point3
		{
			get => (Point)this.GetValue(Point3Property);
			set => this.SetValue(Point3Property, value);
		}

		public static DependencyProperty Point3Property { get; } =
			DependencyProperty.Register(
				"Point3",
				typeof(Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
