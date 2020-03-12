using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using CoreGraphics;
using Uno.Disposables;
using Windows.UI.Xaml.Media;

#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Windows.UI.Xaml.Media
{
	// iOS partial for SolidColorBrush
	public partial class Brush
	{
		internal static IDisposable AssignAndObserveBrush(Brush b, Action<CGColor> colorSetter)
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
			else if (b is ImageBrush imageBrush)
			{
				Action<_Image> action = _ => colorSetter(SolidColorBrushHelper.Transparent.Color);

				imageBrush.ImageChanged += action;

				disposables.Add(() => imageBrush.ImageChanged -= action);
			}
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return disposables;
		}
	}
}
