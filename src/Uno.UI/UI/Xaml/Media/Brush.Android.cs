using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Uno.Extensions;
using Uno.Disposables;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Rect = Windows.Foundation.Rect;
using Windows.UI.Input.Spatial;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;

namespace Microsoft.UI.Xaml.Media
{
	//Android partial for Brush
	public partial class Brush
	{
		internal delegate void ColorSetterHandler(Android.Graphics.Color color);

		private static Paint.Style _strokeCache;
		private static Paint.Style _fillCache;

		private static Paint.Style SystemStroke => _strokeCache ??= Paint.Style.Stroke;
		private static Paint.Style SystemFill => _fillCache ??= Paint.Style.Fill;

		/// <summary>
		/// Return a paint with Fill style
		/// </summary>
		/// <param name="destinationRect">RectF that will be drawn into - used by ImageBrush</param>
		/// <returns>A Paint with Fill style</returns>
		internal Paint GetFillPaint(Windows.Foundation.Rect destinationRect)
		{
			var paint = GetPaintInner(destinationRect);
			paint?.SetStyle(SystemFill);
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
			paint?.SetStyle(SystemStroke);
			return paint;
		}

		protected virtual Paint GetPaintInner(Rect destinationRect) => throw new InvalidOperationException();

		internal static IDisposable AssignAndObserveBrush(Brush b, ColorSetterHandler colorSetter, Action imageBrushCallback = null)
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
				var disposables = new CompositeDisposable(5);
				imageBrushCallback();

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.ImageSourceProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.StretchProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.AlignmentXProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.AlignmentYProperty,
					(_, __) => imageBrushCallback()
				).DisposeWith(disposables);

				imageBrush.RegisterDisposablePropertyChangedCallback(
					ImageBrush.RelativeTransformProperty,
					(_, __) => imageBrushCallback()
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
					(s, args) => colorSetter((s as XamlCompositionBrushBase).FallbackColorWithOpacity))
					.DisposeWith(disposables);

				unsupportedCompositionBrush.RegisterDisposablePropertyChangedCallback(
					OpacityProperty,
					(s, args) => colorSetter((s as XamlCompositionBrushBase).FallbackColorWithOpacity))
					.DisposeWith(disposables);

				return disposables;
			}
			else
			{
				colorSetter(SolidColorBrushHelper.Transparent.Color);
			}

			return Disposable.Empty;
		}

		internal static Drawable GetBackgroundDrawable(Brush background, Windows.Foundation.Rect drawArea, Paint fillPaint, Path maskingPath = null, bool antiAlias = true)
		{
			if (background is ImageBrush)
			{
				throw new InvalidOperationException($"This method should not be called for ImageBrush, use BorderLayerRenderer.DispatchSetImageBrushAsBackground instead");
			}

			if (maskingPath == null)
			{
				if (Brush.GetColorWithOpacity(background) is { } color)
				{
					return new ColorDrawable(color);
				}

				if (fillPaint != null)
				{
					var linearDrawable = new PaintDrawable();
					var drawablePaint = linearDrawable.Paint;
					drawablePaint.Color = fillPaint.Color;
					drawablePaint.SetShader(fillPaint.Shader);

					return linearDrawable;
				}

				return null;
			}

			var drawable = new PaintDrawable();

			BrushNative.BuildBackgroundCornerRadius(drawable, maskingPath, fillPaint, antiAlias, (float)drawArea.Width, (float)drawArea.Height);

			return drawable;
		}
	}
}
