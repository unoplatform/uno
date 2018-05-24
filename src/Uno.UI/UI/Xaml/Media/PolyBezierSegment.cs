namespace Windows.UI.Xaml.Media
{
	public  partial class PolyBezierSegment : PathSegment
	{
		public PolyBezierSegment()
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
				typeof(PolyBezierSegment), 
				new FrameworkPropertyMetadata(
					defaultValue: null, 
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		#endregion
	}
}
