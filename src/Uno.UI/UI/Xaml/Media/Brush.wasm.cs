using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using System.Drawing;

namespace Windows.UI.Xaml.Media
{
	public abstract partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Color> colorSetter)
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

		protected Color GetColorWithOpacity(Color referenceColor)
		{
			return Color.FromArgb((byte)(Opacity * referenceColor.A), referenceColor.R, referenceColor.G, referenceColor.B);
		}
	}
}
