using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Uno.Extensions;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media
{
	//Android partial for Brush
	public partial class Brush
	{
		/// <summary>
		/// Return a paint with Fill style
		/// </summary>
		/// <param name="destinationRect">RectF that will be drawn into - used by ImageBrush</param>
		/// <returns>A Paint with Fill style</returns>
		internal Paint GetFillPaint(Windows.Foundation.Rect destinationRect)
		{
			var paint = GetPaintInner(destinationRect);
			paint.SetStyle(Paint.Style.Fill);
			return paint;
		}

		/// <summary>
		/// Return a paint with Stroke style
		/// </summary>
		/// <param name="destinationRect">RectF that will be drawn into - used by ImageBrush</param>
		/// <returns>A Paint with Stroke style</returns>
		internal Paint GetStrokePaint(Windows.Foundation.Rect destinationRect)
		{
			var paint = GetPaintInner(destinationRect);
			paint.SetStyle(Paint.Style.Stroke);
			return paint;
		}

		protected virtual Paint GetPaintInner(Rect destinationRect) => throw new InvalidOperationException();

		internal static IDisposable AssignAndObserveBrush(Brush b, Action<Android.Graphics.Color> colorSetter, Action imageBrushCallback = null)
		{
			if (b is SolidColorBrush colorBrush)
			{
				var disposables = new CompositeDisposable(2);

				colorSetter(colorBrush.ColorWithOpacity);

				colorBrush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.ColorProperty,
					(s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
				).DisposeWith(disposables);

				colorBrush.RegisterDisposablePropertyChangedCallback(
					SolidColorBrush.OpacityProperty,
					(s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
				).DisposeWith(disposables);

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
			else if (b is ImageBrush imageBrush && imageBrushCallback != null)
			{
				var disposables = new CompositeDisposable(3);
				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.ImageSourceProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.StretchProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.RelativeTransformProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);
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
