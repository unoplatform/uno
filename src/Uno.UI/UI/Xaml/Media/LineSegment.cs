using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial class LineSegment : PathSegment
	{
		public LineSegment()
		{
		}

		#region Point

		public Point Point
		{
			get => (Point)this.GetValue(PointProperty);
			set => this.SetValue(PointProperty, value);
		}

		public static DependencyProperty PointProperty { get; } =
			DependencyProperty.Register(
				"Point",
				typeof(Point),
				typeof(LineSegment),
				new FrameworkPropertyMetadata(
					defaultValue: default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
