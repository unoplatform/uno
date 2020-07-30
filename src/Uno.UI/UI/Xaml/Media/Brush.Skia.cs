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
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Color> colorSetter, Action imageBrushCallback = null)
		{
			var disposables = new CompositeDisposable();

			if (b != null)
			{
				var colorBrush = b as SolidColorBrush;
				var imageBrush = b as ImageBrush;

				if (colorBrush != null)
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
				//else if (imageBrush != null)
				//{
				//	Action<_Image> action = _ => colorSetter(SolidColorBrushHelper.Transparent.Color);

				//	imageBrush.ImageChanged += action;

				//	disposables.Add(() => imageBrush.ImageChanged -= action);
				//}
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
