namespace Windows.UI.Xaml.Media
{
	public partial class PolyLineSegment : PathSegment
	{
		public PolyLineSegment()
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
				typeof(PolyLineSegment),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
