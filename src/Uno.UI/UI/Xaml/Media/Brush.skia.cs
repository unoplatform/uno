using System;
using System.Numerics;
using Uno.Disposables;
using Windows.UI.Composition;
using Windows.UI;
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
using System.Collections.Generic;
using Uno;

namespace Windows.UI.Xaml.Media
{
	public partial class Brush
	{
		internal delegate void BrushSetterHandler(CompositionBrush brush);

		internal static IDisposable AssignAndObserveBrush(Brush brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			if (brush == null)
			{
				brushSetter(null);

				return null;
			}

			if (brush is SolidColorBrush colorBrush)
			{
				return AssignAndObserveSolidColorBrush(colorBrush, compositor, brushSetter);
			}
			else if (brush is GradientBrush gradientBrush)
			{
				return AssignAndObserveGradientBrush(gradientBrush, compositor, brushSetter);
			}
			else if (brush is ImageBrush imageBrush)
			{
				return AsssignAndObserveImageBrush(imageBrush, compositor, brushSetter);
			}
			else if (brush is RadialGradientBrush radialGradientBrush)
			{
				return AssignAndObserveRadialGradientBrush(radialGradientBrush, compositor, brushSetter);
			}
			else if (brush is XamlCompositionBrushBase compositionBrush)
			{
				return AssignAndObserveXamlCompositionBrush(compositionBrush, compositor, brushSetter);
			}
			else
			{
				return AssignAndObservePlaceholderBrush(compositor, brushSetter);
			}
		}

		private static IDisposable AssignAndObserveSolidColorBrush(SolidColorBrush brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = compositor.CreateColorBrush(brush.ColorWithOpacity);

			brush.RegisterDisposablePropertyChangedCallback(
				SolidColorBrush.ColorProperty,
				(s, colorArg) => compositionBrush.Color = brush.ColorWithOpacity
			)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
				SolidColorBrush.OpacityProperty,
				(s, colorArg) => compositionBrush.Color = brush.ColorWithOpacity
			)
			.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AssignAndObserveGradientBrush(GradientBrush gradientBrush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = CreateCompositionGradientBrush(gradientBrush, compositor);

			if (gradientBrush is LinearGradientBrush linearGradient)
			{
				var clgb = (CompositionLinearGradientBrush)compositionBrush;

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						LinearGradientBrush.StartPointProperty,
						(s, e) => clgb.StartPoint = (Vector2)e.NewValue
					)
				.DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						LinearGradientBrush.EndPointProperty,
						(s, e) => clgb.EndPoint = (Vector2)e.NewValue
					)
				.DisposeWith(disposables);
			}

			gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.GradientStopsProperty,
					(s, e) => ConvertGradientColorStops(
						compositionBrush.Compositor,
						compositionBrush,
						(GradientStopCollection)e.NewValue,
						((GradientBrush)s).Opacity)
				)
			.DisposeWith(disposables);

			gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.MappingModeProperty,
					(s, e) => compositionBrush.MappingMode = (CompositionMappingMode)e.NewValue
				)
			.DisposeWith(disposables);

			gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.OpacityProperty,
					(s, e) => ConvertGradientColorStops(
						compositionBrush.Compositor,
						compositionBrush,
						((GradientBrush)s).GradientStops,
						(double)e.NewValue)
				)
			.DisposeWith(disposables);

			gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.SpreadMethodProperty,
					(s, e) => compositionBrush.ExtendMode = ConvertGradientExtendMode((GradientSpreadMethod)e.NewValue)
				)
			.DisposeWith(disposables);

			gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.RelativeTransformProperty,
					(s, e) => compositionBrush.RelativeTransformMatrix = ((Transform)e.NewValue)?.MatrixCore ?? Matrix3x2.Identity
				)
			.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AsssignAndObserveImageBrush(ImageBrush brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var surfaceBrush = compositor.CreateSurfaceBrush();

			Action onInvalidateRender = () =>
			{
				if (brush.ImageDataCache is { } data)
				{
					surfaceBrush.Stretch = (CompositionStretch)brush.Stretch;
					surfaceBrush.HorizontalAlignmentRatio = GetHorizontalAlignmentRatio(brush.AlignmentX);
					surfaceBrush.VerticalAlignmentRatio = GetVerticalAlignmentRatio(brush.AlignmentY);
					surfaceBrush.Surface = data.CompositionSurface;
				}
			};

			onInvalidateRender();
			brush.InvalidateRender += onInvalidateRender;
			brushSetter(surfaceBrush);
			return new DisposableAction(() => brush.InvalidateRender -= onInvalidateRender);
		}

		private static float GetHorizontalAlignmentRatio(AlignmentX alignmentX)
		{
			return alignmentX switch
			{
				AlignmentX.Left => 0.0f,
				AlignmentX.Center => 0.5f,
				AlignmentX.Right => 1.0f,
				_ => 0.5f, // this should never happen.
			};
		}

		private static float GetVerticalAlignmentRatio(AlignmentY alignmentY)
		{
			return alignmentY switch
			{
				AlignmentY.Top => 0.0f,
				AlignmentY.Center => 0.5f,
				AlignmentY.Bottom => 1.0f,
				_ => 0.5f, // this should never happen.
			};
		}

		private static IDisposable AssignAndObserveRadialGradientBrush(RadialGradientBrush brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = CreateCompositionRadialGradientBrush(brush, compositor);

			brush.RegisterDisposablePropertyChangedCallback(
					RadialGradientBrush.MappingModeProperty,
					(s, e) => compositionBrush.MappingMode = (CompositionMappingMode)e.NewValue
				)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
					RadialGradientBrush.OpacityProperty,
					(s, e) => ConvertGradientColorStops(
						compositionBrush.Compositor,
						compositionBrush,
						((RadialGradientBrush)s).GradientStops,
						(double)e.NewValue)
				)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
					RadialGradientBrush.SpreadMethodProperty,
					(s, e) => compositionBrush.ExtendMode = ConvertGradientExtendMode((GradientSpreadMethod)e.NewValue)
				)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
					RadialGradientBrush.RelativeTransformProperty,
					(s, e) => compositionBrush.RelativeTransformMatrix = ((Transform)e.NewValue)?.MatrixCore ?? Matrix3x2.Identity
				)
			.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AssignAndObserveXamlCompositionBrush(XamlCompositionBrushBase brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			if (brush.CompositionBrush is null)
			{
				brush.OnConnectedInternal();
			}

			var compositionBrush = brush.CompositionBrush ?? compositor.CreateColorBrush(brush.FallbackColorWithOpacity);

			brush.RegisterDisposablePropertyChangedCallback(
				XamlCompositionBrushBase.FallbackColorProperty,
				(s, colorArg) => compositionBrush.TrySetColorFromBrush(brush)
			)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
				XamlCompositionBrushBase.OpacityProperty,
				(s, colorArg) => compositionBrush.TrySetColorFromBrush(brush)
			)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
				XamlCompositionBrushBase.CompositionBrushProperty,
				(s, brushArg) => { if (brush.CompositionBrush is CompositionBrush compBrush) brushSetter(compBrush); }
			)
			.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AssignAndObservePlaceholderBrush(Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = compositor.CreateColorBrush(Colors.Transparent);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static CompositionGradientBrush CreateCompositionGradientBrush(GradientBrush gradientBrush, Compositor compositor)
		{
			CompositionGradientBrush compositionBrush;
			if (gradientBrush is LinearGradientBrush linearGradient)
			{
				CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
				compositionLinearGradientBrush.StartPoint = linearGradient.StartPoint.ToVector2();
				compositionLinearGradientBrush.EndPoint = linearGradient.EndPoint.ToVector2();

				compositionBrush = compositionLinearGradientBrush;
			}
			else
			{
				return null;
			}

			compositionBrush.RelativeTransformMatrix = gradientBrush.RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;
			compositionBrush.ExtendMode = ConvertGradientExtendMode(gradientBrush.SpreadMethod);
			compositionBrush.MappingMode = ConvertBrushMappingMode(gradientBrush.MappingMode);
			ConvertGradientColorStops(compositor, compositionBrush, gradientBrush.GradientStops, gradientBrush.Opacity);

			return compositionBrush;
		}

		private static CompositionRadialGradientBrush CreateCompositionRadialGradientBrush(RadialGradientBrush radialGradientBrush, Compositor compositor)
		{
			var compositionBrush = compositor.CreateRadialGradientBrush();
			compositionBrush.EllipseCenter = radialGradientBrush.Center.ToVector2();
			compositionBrush.EllipseRadius = new Vector2((float)radialGradientBrush.RadiusX, (float)radialGradientBrush.RadiusY);
			ConvertGradientColorStops(compositor, compositionBrush, radialGradientBrush.GradientStops, radialGradientBrush.Opacity);
			compositionBrush.GradientOriginOffset = radialGradientBrush.GradientOrigin.ToVector2();
			compositionBrush.InterpolationSpace = radialGradientBrush.InterpolationSpace;
			compositionBrush.MappingMode = ConvertBrushMappingMode(radialGradientBrush.MappingMode);
			compositionBrush.RelativeTransformMatrix = radialGradientBrush.RelativeTransform?.MatrixCore ?? Matrix3x2.Identity;

			return compositionBrush;
		}

		private static void ConvertGradientColorStops(Compositor compositor, CompositionGradientBrush compositionBrush, IEnumerable<GradientStop> gradientStops, double opacity)
		{
			compositionBrush.ColorStops.Clear();

			foreach (var stop in gradientStops)
			{
				compositionBrush.ColorStops.Add(compositor.CreateColorGradientStop((float)stop.Offset, stop.Color.WithOpacity(opacity)));
			}
		}

		private static CompositionGradientExtendMode ConvertGradientExtendMode(GradientSpreadMethod spreadMethod)
		{
			switch (spreadMethod)
			{
				case GradientSpreadMethod.Repeat:
					return CompositionGradientExtendMode.Wrap;
				case GradientSpreadMethod.Reflect:
					return CompositionGradientExtendMode.Mirror;
				case GradientSpreadMethod.Pad:
				default:
					return CompositionGradientExtendMode.Clamp;
			}
		}

		private static CompositionMappingMode ConvertBrushMappingMode(BrushMappingMode mappingMode)
		{
			switch (mappingMode)
			{
				case BrushMappingMode.Absolute:
					return CompositionMappingMode.Absolute;
				case BrushMappingMode.RelativeToBoundingBox:
				default:
					return CompositionMappingMode.Relative;
			}
		}
	}

	internal static class BrushExtensions
	{
		internal static void TrySetColorFromBrush(this CompositionBrush brush, XamlCompositionBrushBase srcBrush)
		{
			if (brush is CompositionColorBrush colorBrush)
			{
				colorBrush.Color = srcBrush.FallbackColor;
			}
		}
	}
}
