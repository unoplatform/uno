using System;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Color> colorSetter)
		{
			var disposables = new CompositeDisposable();

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
			// ImageBrush not supported yet on Wasm
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return disposables;
		}
	}
}
