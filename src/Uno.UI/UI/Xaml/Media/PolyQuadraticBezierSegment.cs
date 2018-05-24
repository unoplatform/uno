namespace Windows.UI.Xaml.Media
{
	public partial class PolyQuadraticBezierSegment : PathSegment
	{
		public PolyQuadraticBezierSegment()
		{
			Points = new PointCollection();
		}

		#region Points

		public PointCollection Points
		{
			get => (PointCollection)this.GetValue(PointsProperty);
			set => this.SetValue(PointsProperty, value);
		}

		public static DependencyProperty PointsProperty { get; } =
			DependencyProperty.Register(
				nameof(Points),
				typeof(PointCollection),
				typeof(PolyQuadraticBezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
