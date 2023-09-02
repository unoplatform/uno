using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Uno.Extensions;
using Uno.Disposables;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Rect = Windows.Foundation.Rect;
using Windows.UI.Input.Spatial;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Windows.UI.Xaml.Media.Imaging;

using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;
using System.Runtime.CompilerServices;

namespace Windows.UI.Xaml.Media
{
	//Android partial for Brush
	public partial class Brush
	{
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
