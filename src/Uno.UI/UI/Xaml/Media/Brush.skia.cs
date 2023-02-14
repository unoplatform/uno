using System;
using System.Numerics;
using Uno.Disposables;
using Microsoft.UI.Composition;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media
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
			colorSetter(Colors.Transparent);

			if (brush == null)
			{
				return null;
			}

			var disposables = new CompositeDisposable();

			void UpdateColor(object sender, DependencyPropertyChangedEventArgs e) => colorSetter(Colors.Transparent);
			void UpdateColorWhenAnyChanged(DependencyObject source, params DependencyProperty[] properties)
			{
				foreach (var property in properties)
				{
					source
						.RegisterDisposablePropertyChangedCallback(property, UpdateColor)
						.DisposeWith(disposables);
				}
			}

			if (brush is SolidColorBrush colorBrush)
			{
				UpdateColorWhenAnyChanged(colorBrush, new[]
				{
					SolidColorBrush.ColorProperty,
					SolidColorBrush.OpacityProperty
				});
			}
			else if (brush is GradientBrush gradientBrush)
			{
				if (gradientBrush is LinearGradientBrush linearGradient)
				{
					UpdateColorWhenAnyChanged(linearGradient, new[]
					{
						LinearGradientBrush.StartPointProperty,
						LinearGradientBrush.EndPointProperty,
					});
				}

				UpdateColorWhenAnyChanged(gradientBrush, new[]
				{
					GradientBrush.GradientStopsProperty,
					GradientBrush.MappingModeProperty,
					GradientBrush.OpacityProperty,
					GradientBrush.SpreadMethodProperty,
					GradientBrush.RelativeTransformProperty,
				});
			}
			else if (brush is ImageBrush imageBrush)
			{
				imageBrush
					.Subscribe(_ => colorSetter(Colors.Transparent))
					.DisposeWith(disposables);
			}
			else if (brush is AcrylicBrush acrylicBrush)
			{
				UpdateColorWhenAnyChanged(acrylicBrush, new[]
				{
					AcrylicBrush.FallbackColorProperty,
					AcrylicBrush.OpacityProperty,
				});
			}

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
			else if (brush is XamlCompositionBrushBase unimplementedCompositionBrush)
			{
				return AssignAndObserveXamlCompositionBrush(unimplementedCompositionBrush, compositor, brushSetter);
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
				surfaceBrush.Surface = data.CompositionSurface;
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

		/// <summary>
		/// Apply fallback colour for unimplemented <see cref="XamlCompositionBrushBase"/> types. For implemented types a more specific method
		/// should be supplied.
		/// </summary>
		private static IDisposable AssignAndObserveXamlCompositionBrush(XamlCompositionBrushBase brush, Compositor compositor, BrushSetterHandler brushSetter)
		{
			var disposables = new CompositeDisposable();

			var compositionBrush = compositor.CreateColorBrush(brush.FallbackColorWithOpacity);

			brush.RegisterDisposablePropertyChangedCallback(
				AcrylicBrush.FallbackColorProperty,
				(s, colorArg) => compositionBrush.Color = brush.FallbackColorWithOpacity
			)
			.DisposeWith(disposables);

			brush.RegisterDisposablePropertyChangedCallback(
				AcrylicBrush.OpacityProperty,
				(s, colorArg) => compositionBrush.Color = brush.FallbackColorWithOpacity
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
