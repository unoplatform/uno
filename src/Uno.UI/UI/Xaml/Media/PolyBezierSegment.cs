using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Points))]
	public partial class PolyBezierSegment : PathSegment
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
					propertyChangedCallback: OnPointsChanged,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		private static void OnPointsChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyobject is PolyBezierSegment segment)
			{
				if (args.OldValue is PointCollection oldCollection)
				{
					oldCollection.UnRegisterChangedListener(segment.OnPointCollectionChanged);
				}
				if (args.NewValue is PointCollection newCollection)
				{
					newCollection.RegisterChangedListener(segment.OnPointCollectionChanged);
				}
			}
		}

		private void OnPointCollectionChanged() => this.InvalidateMeasure();

		#endregion
	}
}
