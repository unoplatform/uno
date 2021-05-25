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
		internal delegate void ColorSetterHandler(CGColor color);

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
						OpacityProperty,
						(s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
					)
					.DisposeWith(disposables);

				return disposables;
			}

			else if (b is GradientBrush gradientBrush)
			{
				var disposables = new CompositeDisposable(2);
				colorSetter(gradientBrush.FallbackColorWithOpacity);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.FallbackColorProperty,
					(s, colorArg) => colorSetter((s as GradientBrush).FallbackColorWithOpacity)
				).DisposeWith(disposables);

				gradientBrush.RegisterDisposablePropertyChangedCallback(
					GradientBrush.OpacityProperty,
					(s, colorArg) => colorSetter((s as GradientBrush).FallbackColorWithOpacity)
				).DisposeWith(disposables);

				return disposables;
			}
			else if (b is ImageBrush imageBrush)
			{
				var disposables = new CompositeDisposable(5);
				void ImageChanged(_Image _) => colorSetter(SolidColorBrushHelper.Transparent.Color);

				imageBrush.ImageChanged += ImageChanged;

				Disposable.Create(() => imageBrush.ImageChanged -= ImageChanged)
					.DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.StretchProperty,
					(_, __) => imageBrushCallback?.Invoke()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.AlignmentXProperty,
					(_, __) => imageBrushCallback?.Invoke()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.AlignmentYProperty,
					(_, __) => imageBrushCallback?.Invoke()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.RelativeTransformProperty,
					(_, __) => imageBrushCallback?.Invoke()
				).DisposeWith(disposables);

				return disposables;
			}
			else if (b is AcrylicBrush acrylicBrush)
			{
				var disposables = new CompositeDisposable(2);
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
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return Disposable.Empty;
		}
	}
}
