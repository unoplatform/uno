using System;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Windows.UI.Color> colorSetter)
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
			// ImageBrush not supported yet on Wasm
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return Disposable.Empty;
		}
	}
}
