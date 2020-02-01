namespace Windows.UI.Xaml.Media
{
	public partial class BezierSegment : PathSegment
	{
		#region Point1

		public Foundation.Point Point1
		{
			get => (Foundation.Point)this.GetValue(Point1Property);
			set => this.SetValue(Point1Property, value);
		}

		public static DependencyProperty Point1Property { get; } =
			DependencyProperty.Register(
				"Point1",
				typeof(Foundation.Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Foundation.Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Point2

		public Foundation.Point Point2
		{
			get => (Foundation.Point)this.GetValue(Point2Property);
			set => this.SetValue(Point2Property, value);
		}

		public static DependencyProperty Point2Property { get; } =
			DependencyProperty.Register(
				"Point2",
				typeof(Foundation.Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Foundation.Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion

		#region Point3

		public Foundation.Point Point3
		{
			get => (Foundation.Point)this.GetValue(Point3Property);
			set => this.SetValue(Point3Property, value);
		}

		public static DependencyProperty Point3Property { get; } =
			DependencyProperty.Register(
				"Point3",
				typeof(Foundation.Point),
				typeof(BezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: new Foundation.Point(),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
