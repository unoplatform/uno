using Windows.UI.Xaml.Markup;
using Windows.Foundation;
using Uno.Extensions;
using System;

namespace Windows.UI.Xaml.Media;

public partial class LinearGradientBrush : GradientBrush
{
	public LinearGradientBrush()
	{
	}

	public LinearGradientBrush(
		GradientStopCollection gradientStopCollection,
		double angle)
	{
		GradientStops = gradientStopCollection;

		var rad = MathEx.ToRadians(angle);
		EndPoint = new Point(Math.Cos(rad), Math.Sin(rad));
	}

	public Point StartPoint
	{
		get => (Point)GetValue(StartPointProperty);
		set => SetValue(StartPointProperty, value);
	}

	public static DependencyProperty StartPointProperty { get; } = DependencyProperty.Register(
		nameof(StartPoint),
		typeof(Point),
		typeof(LinearGradientBrush),
		new FrameworkPropertyMetadata(default(Point))
	);

	public Point EndPoint
	{
		get => (Point)GetValue(EndPointProperty);
		set => SetValue(EndPointProperty, value);
	}

	public static DependencyProperty EndPointProperty { get; } = DependencyProperty.Register(
		nameof(EndPoint),
		typeof(Point),
		typeof(LinearGradientBrush),
		new FrameworkPropertyMetadata(new Point(1, 1))
	);
}
