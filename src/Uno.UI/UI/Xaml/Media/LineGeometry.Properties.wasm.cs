using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	public partial class LineGeometry : Geometry
	{
		public static DependencyProperty StartPointProperty { get; } =
			DependencyProperty.Register(
				nameof(StartPoint), typeof(Point),
				typeof(LineGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));

		public static DependencyProperty EndPointProperty { get; } =
			DependencyProperty.Register(
				nameof(EndPoint), typeof(Point),
				typeof(LineGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));

		public Point StartPoint
		{
			get => (Point)GetValue(StartPointProperty);
			set => SetValue(StartPointProperty, value);
		}

		public Point EndPoint
		{
			get => (Point)GetValue(EndPointProperty);
			set => SetValue(EndPointProperty, value);
		}


		public LineGeometry()
		{
			InitPartials();
		}

		partial void InitPartials();
	}
}
