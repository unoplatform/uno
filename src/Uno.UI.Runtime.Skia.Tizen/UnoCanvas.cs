using System;
using System.Collections.Generic;
using System.Text;
using ElmSharp;
using SkiaSharp;
using SkiaSharp.Views.Tizen.NUI;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class UnoCanvas : SKCanvasView
	{
		public UnoCanvas()
		{
			PaintSurface += UnoCanvas_PaintSurface;
			Relayout += UnoCanvas_Resized;
		}

		internal void InvalidateRender() => Invalidate();

		private void UnoCanvas_Resized(object sender, EventArgs e)
		{
			var c = (SKCanvasView)sender;
			
			// control is not yet fully initialized
			if (c.Size.Width <= 0 || c.Size.Height <= 0)
			{
				return;
			}
			var scale = (float)SkiaSharp.Views.Tizen.ScalingInfo.ScalingFactor;

			WUX.Window.Current.OnNativeSizeChanged(
				new Windows.Foundation.Size(
				c.Size.Width / scale,
				c.Size.Height / scale));
		}

		private void UnoCanvas_PaintSurface(object sender, SkiaSharp.Views.Tizen.SKPaintSurfaceEventArgs e)
		{
			e.Surface.Canvas.Clear(SKColors.Blue);

			var scale = (float)SkiaSharp.Views.Tizen.ScalingInfo.ScalingFactor;
			e.Surface.Canvas.Scale(scale);

			WUX.Window.Current.Compositor.Render(e.Surface);
		}
	}
}
