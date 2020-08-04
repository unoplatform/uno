using System;
using System.IO;
using SkiaSharp;
using Uno.Extensions;
using Uno.Logging;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	internal class UnoDrawingArea : Gtk.DrawingArea
	{
		private SKBitmap bitmap;
		private int renderCount;

		public UnoDrawingArea()
		{
			WUX.Window.InvalidateRender
				+= () =>
				{
					Invalidate();
				};
		}

		private void Invalidate()
			=> QueueDrawArea(0, 0, 10000, 10000);

		private void Screen_MonitorsChanged(object sender, EventArgs e)
		{
			UpdateDpi();
			Invalidate();
		}

		private void UpdateDpi()
		{
		}

		protected override bool OnDrawn(Cairo.Context cr)
		{
			int width, height;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			var dpi = (Window.Screen?.Resolution ?? 1) / 96.0;

			width = (int)AllocatedWidth;
			height = (int)AllocatedHeight;

			var scaledWidth = (int)(width * dpi);
			var scaledHeight = (int)(height * dpi);

			var info = new SKImageInfo(scaledWidth, scaledHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(scaledWidth, scaledHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale((float)dpi);

				WUX.Window.Current.Compositor.Render(surface, info);

				using (var gtkSurface = new Cairo.ImageSurface(
					bitmap.GetPixels(out _),
					Cairo.Format.Argb32,
					bitmap.Width, bitmap.Height,
					bitmap.Width * 4))
				{
					gtkSurface.MarkDirty();
					cr.Scale(1 / dpi, 1 / dpi);
					cr.SetSourceSurface(gtkSurface, 0, 0);
					cr.Paint();
				}
			}

			return true;
		}

		internal void TakeScreenshot(string filePath)
		{
			using Stream memStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			using SKManagedWStream wstream = new SKManagedWStream(memStream);

			bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100);
		}
	}
}
