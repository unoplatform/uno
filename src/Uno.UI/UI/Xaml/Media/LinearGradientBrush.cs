using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Uno.Extensions;
using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media;

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

	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			_compositionBrush = compositor.CreateLinearGradientBrush();
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		base.SynchronizeCompositionBrush();
		if (_compositionBrush is CompositionLinearGradientBrush compositionBrush)
		{
			compositionBrush.StartPoint = StartPoint.ToVector2();
			compositionBrush.EndPoint = EndPoint.ToVector2();

			compositionBrush.RelativeTransformMatrix = RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
			compositionBrush.ExtendMode = ConvertGradientExtendMode(SpreadMethod);
			compositionBrush.MappingMode = ConvertBrushMappingMode(MappingMode);
			ConvertGradientColorStops(compositionBrush.Compositor, compositionBrush, GradientStops, Opacity);
		}
	}
}
