using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		static Rectangle()
		{
			StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

#if __IOS__ || __MACOS__ || __SKIA__ || __ANDROID__ || __WASM__
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureRelativeShape(availableSize);
#endif

		#region RadiusY (DP)
		public static DependencyProperty RadiusYProperty { get; } = DependencyProperty.Register(
			"RadiusY",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}
		#endregion

		#region RadiusX (DP)
		public static DependencyProperty RadiusXProperty { get; } = DependencyProperty.Register(
			"RadiusX",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}
		#endregion

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
