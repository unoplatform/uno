using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using Windows.Foundation.Collections;
using Windows.UI; // Required for WinUI 3+ Color

namespace Microsoft.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		/// <summary>
		/// Color action handler
		/// </summary>
		/// <remarks>
		/// This delegate is not an <see cref="Action{T}"/> to account for https://github.com/dotnet/runtime/issues/50757
		/// </remarks>
		internal delegate void ColorSetterHandler(Color color);

		internal static IDisposable AssignAndObserveBrush(Brush b, ColorSetterHandler colorSetter, Action imageBrushCallback = null)
		{
			if (b is SolidColorBrush colorBrush)
			{
				colorSetter(colorBrush.ColorWithOpacity);

				return WhenAnyChanged(new CompositeDisposable(2), colorBrush, (s, e) => colorSetter((s as SolidColorBrush).ColorWithOpacity), new[]
				{
					SolidColorBrush.ColorProperty,
					SolidColorBrush.OpacityProperty,
				});
			}
			else if (b is GradientBrush gb)
			{
				var disposables = new CompositeDisposable(4);

				colorSetter(gb.FallbackColorWithOpacity);

				WhenAnyChanged(disposables, gb, (s, e) => colorSetter((s as GradientBrush).FallbackColorWithOpacity), new[]
				{
					GradientBrush.FallbackColorProperty,
					GradientBrush.OpacityProperty,
				});

				var innerDisposable = new SerialDisposable();
				innerDisposable.Disposable = ObserveGradientBrushStops(gb.GradientStops, colorSetter);
				gb
					.RegisterDisposablePropertyChangedCallback(
						GradientBrush.GradientStopsProperty,
						(s, e) => innerDisposable.Disposable = ObserveGradientBrushStops((s as GradientBrush).GradientStops, colorSetter)
					)
					.DisposeWith(disposables);
				innerDisposable.DisposeWith(disposables);

				return disposables;
			}
			else if (b is AcrylicBrush ab)
			{
				Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException(
					"AcrylicBrush is ** not ** supported by the AssignAndObserveBrush. "
					+ "(Instead you have to use the AcrylicBrush.Subscribe().)"));
				return Disposable.Empty;
			}
			else if (b is ImageBrush)
			{
				Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException(
					"ImageBrush is ** not ** supported by the AssignAndObserveBrush. "
					+ "(Instead you have to use the ImageBrush.Subscribe().)"));
				return Disposable.Empty;
			}
			else if (b is XamlCompositionBrushBase unsupportedCompositionBrush)
			{
				colorSetter(unsupportedCompositionBrush.FallbackColorWithOpacity);

				return WhenAnyChanged(new CompositeDisposable(2), unsupportedCompositionBrush, (s, e) => colorSetter((s as XamlCompositionBrushBase).FallbackColorWithOpacity), new[]
				{
					XamlCompositionBrushBase.FallbackColorProperty,
					XamlCompositionBrushBase.OpacityProperty,
				});
			}
			else
			{
				colorSetter(Colors.Transparent);

				return Disposable.Empty;
			}
		}

		// TODO: Refactor brush handling to a cleaner unified approach - https://github.com/unoplatform/uno/issues/5192
		internal bool SupportsAssignAndObserveBrush => !(this is ImageBrush || this is AcrylicBrush);

		private static IDisposable ObserveGradientBrushStops(GradientStopCollection stops, ColorSetterHandler colorSetter)
		{
			var disposables = new CompositeDisposable();
			if (stops != null)
			{
				colorSetter(Colors.Transparent);

				stops.VectorChanged += OnVectorChanged;
				disposables.Add(() => stops.VectorChanged -= OnVectorChanged);
			}

			return disposables;

			void OnVectorChanged(IObservableVector<GradientStop> sender, IVectorChangedEventArgs e)
			{
				colorSetter(Colors.Transparent);
				foreach (var stop in stops.ToArray())
				{
					WhenAnyChanged(disposables, stop, (s, e) => colorSetter(Colors.Transparent), new[]
					{
						GradientStop.ColorProperty,
						GradientStop.OffsetProperty,
					});
				}
			}
		}

		private static CompositeDisposable WhenAnyChanged(CompositeDisposable disposables, DependencyObject source, PropertyChangedCallback callback, params DependencyProperty[] properties)
		{
			foreach (var property in properties)
			{
				source
					.RegisterDisposablePropertyChangedCallback(property, callback)
					.DisposeWith(disposables);
			}

			return disposables;
		}
	}
}
