using System;
using System.Collections.Generic;
using System.Text;
using ElmSharp;
using SkiaSharp;
using SkiaSharp.Views.Tizen;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class UnoCanvas : SKCanvasView
	{
		public UnoCanvas(EvasObject parent) : base(parent)
		{
			PaintSurface += UnoCanvas_PaintSurface;
			Resized += UnoCanvas_Resized;
		}

		internal void InvalidateRender() => Invalidate();

		private void UnoCanvas_Resized(object sender, EventArgs e)
		{
			var c = (SKCanvasView)sender;

			var geometry = c.Geometry;

			// control is not yet fully initialized
			if (geometry.Width <= 0 || geometry.Height <= 0)
			{
				return;
			}

			var scale = (float)ScalingInfo.ScalingFactor;

			WUX.Window.Current.OnNativeSizeChanged(
				new Windows.Foundation.Size(
				geometry.Width / scale,
				geometry.Height / scale));
		}

		private void UnoCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			e.Surface.Canvas.Clear(SKColors.Blue);

			var scale = (float)ScalingInfo.ScalingFactor;
			e.Surface.Canvas.Scale(scale);

			WUX.Window.Current.Compositor.Render(e.Surface);
		}
	}
}
