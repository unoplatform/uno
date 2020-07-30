using System;
using SkiaSharp;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	internal class UnoDrawingArea : Gtk.DrawingArea
	{
		private SKBitmap bitmap;
		private int renderCount;
		private int InvalidateRenderCount;

		public UnoDrawingArea()
		{
			WUX.Window.Current.InvalidateRender
				+= () =>
				{
					QueueDrawArea(0, 0, 10000, 1000);
				};
		}

		protected override bool OnDrawn(Cairo.Context cr)
		{
			int width, height;

			Console.WriteLine($"Render {renderCount++}");

			width = (int)AllocatedWidth;
			height = (int)AllocatedHeight;

			var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out var len)))
			{
				surface.Canvas.Clear(SKColors.White);

				WUX.Window.Current.Compositor.Render(surface, info);

				using (var gtkSurface = new Cairo.ImageSurface(
					bitmap.GetPixels(out _),
					Cairo.Format.Argb32,
					bitmap.Width, bitmap.Height,
					bitmap.Width * 4))
				{
					gtkSurface.MarkDirty();
					cr.SetSourceSurface(gtkSurface, 0, 0);
					cr.Paint();
				}
			}

			return true;
		}
	}
}
