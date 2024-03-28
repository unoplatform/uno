using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Line
	{
		#region X1 (DP)
		public double X1
		{
			get => (double)GetValue(X1Property);
			set => SetValue(X1Property, value);
		}

		public static DependencyProperty X1Property { get; } = DependencyProperty.Register(
			"X1",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

		#region X2 (DP)
		public double X2
		{
			get => (double)GetValue(X2Property);
			set => SetValue(X2Property, value);
		}

		public static DependencyProperty X2Property { get; } = DependencyProperty.Register(
			"X2",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

		#region Y1 (DP)
		public double Y1
		{
			get => (double)GetValue(Y1Property);
			set => SetValue(Y1Property, value);
		}

		public static DependencyProperty Y1Property { get; } = DependencyProperty.Register(
			"Y1",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

		#region Y2 (DP)
		public double Y2
		{
			get => (double)GetValue(Y2Property);
			set => SetValue(Y2Property, value);
		}

		public static DependencyProperty Y2Property { get; } = DependencyProperty.Register(
			"Y2",
			typeof(double),
			typeof(Line),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
