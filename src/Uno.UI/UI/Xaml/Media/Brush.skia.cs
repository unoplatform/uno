using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using Windows.UI;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal delegate void ColorSetterHandler(Color color);

		internal static IDisposable AssignAndObserveBrush(Brush b, ColorSetterHandler colorSetter, Action imageBrushCallback = null)
		{
			var disposables = new CompositeDisposable();

			if (b != null)
			{
				if (b is SolidColorBrush colorBrush)
				{
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
				}
				//else if (b is ImageBrush imageBrush)
				//{
				//	Action<_Image> action = _ => colorSetter(SolidColorBrushHelper.Transparent.Color);

				//	imageBrush.ImageChanged += action;

				//	disposables.Add(() => imageBrush.ImageChanged -= action);
				//}
				else if (b is AcrylicBrush acrylicBrush)
                {
					colorSetter(acrylicBrush.FallbackColorWithOpacity);

					acrylicBrush.RegisterDisposablePropertyChangedCallback(
						AcrylicBrush.FallbackColorProperty,
						(s, args) => colorSetter((s as AcrylicBrush).FallbackColorWithOpacity))
						.DisposeWith(disposables);

					acrylicBrush.RegisterDisposablePropertyChangedCallback(
						AcrylicBrush.OpacityProperty,
						(s, args) => colorSetter((s as AcrylicBrush).FallbackColorWithOpacity))
						.DisposeWith(disposables);

					return disposables;
				}
				else if (b is XamlCompositionBrushBase unsupportedCompositionBrush)
				{
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
				}
				else
				{
					colorSetter(SolidColorBrushHelper.Transparent.Color);
				}
			}
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return disposables;
		}

	}
}
