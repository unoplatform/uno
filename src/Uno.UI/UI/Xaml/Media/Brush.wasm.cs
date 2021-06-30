using System;
using System.Diagnostics;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using Windows.UI; // Required for WinUI 3+ Color

namespace Windows.UI.Xaml.Media
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
				var disposables = new CompositeDisposable(2);
				colorSetter(colorBrush.ColorWithOpacity);

				colorBrush.RegisterDisposablePropertyChangedCallback(
						SolidColorBrush.ColorProperty,
						(s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
					)
					.DisposeWith(disposables);

				colorBrush.RegisterDisposablePropertyChangedCallback(
						SolidColorBrush.OpacityProperty,
						(s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
					)
					.DisposeWith(disposables);

				return disposables;
			}

			if (b is GradientBrush gb)
			{
				var disposables = new CompositeDisposable(2);

				colorSetter(gb.FallbackColorWithOpacity);

				gb.RegisterDisposablePropertyChangedCallback(
						GradientBrush.FallbackColorProperty,
						(s, colorArg) => colorSetter((s as GradientBrush).FallbackColorWithOpacity)
					)
					.DisposeWith(disposables);

				gb.RegisterDisposablePropertyChangedCallback(
						GradientBrush.OpacityProperty,
						(s, colorArg) => colorSetter((s as GradientBrush).FallbackColorWithOpacity)
					)
					.DisposeWith(disposables);

				return disposables;
			}

			if (b is AcrylicBrush ab)
			{
				Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException(
					"AcrylicBrush is ** not ** supported by the AssignAndObserveBrush. "
					+ "(Instead you have to use the AcrylicBrush.Subscribe().)"));
				return Disposable.Empty;
			}

			if (b is ImageBrush)
			{
				Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException(
					"ImageBrush is ** not ** supported by the AssignAndObserveBrush. "
					+ "(Instead you have to use the ImageBrush.Subscribe().)"));
				return Disposable.Empty;
			}

			if (b is XamlCompositionBrushBase unsupportedCompositionBrush)
			{
				// 
				var disposables = new CompositeDisposable(2);
				colorSetter(unsupportedCompositionBrush.FallbackColorWithOpacity);

				unsupportedCompositionBrush.RegisterDisposablePropertyChangedCallback(
						XamlCompositionBrushBase.FallbackColorProperty,
						(s, colorArg) => colorSetter((s as XamlCompositionBrushBase).FallbackColorWithOpacity)
					)
					.DisposeWith(disposables);

				unsupportedCompositionBrush.RegisterDisposablePropertyChangedCallback(
						OpacityProperty,
						(s, colorArg) => colorSetter((s as XamlCompositionBrushBase).FallbackColorWithOpacity)
					)
					.DisposeWith(disposables);

				return disposables;
			}

			colorSetter(SolidColorBrushHelper.Transparent.Color);
			return Disposable.Empty;
		}

		// TODO: Refactor brush handling to a cleaner unified approach - https://github.com/unoplatform/uno/issues/5192
		internal bool SupportsAssignAndObserveBrush => !(this is ImageBrush || this is AcrylicBrush);
	}

}
