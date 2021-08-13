using System;
using System.Numerics;
using Uno.Disposables;
using Windows.UI.Composition;
using Windows.UI;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal delegate void BrushSetterHandler(CompositionBrush brush);

		/// <summary>
		/// Registers observers for property changes on the specified Brush.
		/// Note that skia implementation of this method should only be used for property observervation.
		/// </summary>
		internal static IDisposable AssignAndObserveBrush(Brush brush, Action<Color> colorSetter, Action imageBrushCallback = null)
		{
			if (brush == null)
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);

				return null;
			}

			var disposables = new CompositeDisposable();
			if (brush is SolidColorBrush colorBrush)
			{
				brush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.ColorProperty,
					(s, colorArg) => colorSetter(SolidColorBrushHelper.Transparent.Color)
				)
				.DisposeWith(disposables);

				brush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.OpacityProperty,
					(s, colorArg) => colorSetter(SolidColorBrushHelper.Transparent.Color)
				)
				.DisposeWith(disposables);
			}
			else if (brush is GradientBrush gradientBrush)
			{
				if (gradientBrush is LinearGradientBrush linearGradient)
				{
					gradientBrush.RegisterDisposablePropertyChangedCallback(
							LinearGradientBrush.StartPointProperty,
							(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
						)
					.DisposeWith(disposables);

					gradientBrush.RegisterDisposablePropertyChangedCallback(
							LinearGradientBrush.EndPointProperty,
							(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
						)
					.DisposeWith(disposables);
				}

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						GradientBrush.GradientStopsProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						GradientBrush.MappingModeProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						GradientBrush.OpacityProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						GradientBrush.SpreadMethodProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
						GradientBrush.RelativeTransformProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);
			}
			else if (brush is ImageBrush imageBrush)
			{
				imageBrush
					.Subscribe(_ => colorSetter(SolidColorBrushHelper.Transparent.Color))
					.DisposeWith(disposables);
			}
			else if (brush is AcrylicBrush acrylicBrush)
			{
				acrylicBrush.RegisterDisposablePropertyChangedCallback(
						AcrylicBrush.FallbackColorProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);

				acrylicBrush.RegisterDisposablePropertyChangedCallback(
						AcrylicBrush.OpacityProperty,
						(s, e) => colorSetter(SolidColorBrushHelper.Transparent.Color)
					)
				.DisposeWith(disposables);
			}

			colorSetter(SolidColorBrushHelper.Transparent.Color);

			return disposables;
		}

		internal static IDisposable AssignAndObserveBrush(Brush brush, Compositor compositor, BrushSetterHandler brushSetter, Action imageBrushCallback = null)
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
			else if (brush is AcrylicBrush acrylicBrush)
			{
				return AssignAndObserveAcrylicBrush(acrylicBrush, compositor, brushSetter);
			}
			else if (brush is XamlCompositionBrushBase xamlCompositionBrushBase)
			{
				return AssignAndObserveXamlCompositionBrush(xamlCompositionBrushBase, compositor, brushSetter);
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

			brush.Subscribe(data =>
			{
				surfaceBrush.Surface = data.Value;
			}).DisposeWith(disposables);

			brushSetter(surfaceBrush);

			return disposables;
		}

		private static IDisposable AssignAndObserveAcrylicBrush(AcrylicBrush acrylicBrush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = compositor.CreateColorBrush(acrylicBrush.FallbackColorWithOpacity);

			acrylicBrush.RegisterDisposablePropertyChangedCallback(
				AcrylicBrush.FallbackColorProperty,
				(s, colorArg) => compositionBrush.Color = acrylicBrush.FallbackColorWithOpacity
			)
			.DisposeWith(disposables);

			acrylicBrush.RegisterDisposablePropertyChangedCallback(
				AcrylicBrush.OpacityProperty,
				(s, colorArg) => compositionBrush.Color = acrylicBrush.FallbackColorWithOpacity
			)
			.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AssignAndObserveXamlCompositionBrush(XamlCompositionBrushBase brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = brush.CompositionBrush;

			//brush.RegisterDisposablePropertyChangedCallback(
			//	XamlCompositionBrushBase.CompositionBrushProperty,
			//	(s, e) => brushSetter(((CompositionBrush)e.NewValue))
			//)
			//.DisposeWith(disposables);

			brushSetter(compositionBrush);

			return disposables;
		}

		private static IDisposable AssignAndObservePlaceholderBrush(Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = compositor.CreateColorBrush(SolidColorBrushHelper.Transparent.Color);

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

		private static void ConvertGradientColorStops(Compositor compositor, CompositionGradientBrush compositionBrush, GradientStopCollection gradientStops, double opacity)
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
}
