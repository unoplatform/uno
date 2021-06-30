using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using Uno.Logging;
using WUX = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.Native;

namespace Uno.UI.Runtime.Skia
{
	class Renderer
	{
		private FrameBufferDevice _fbDev;
		private SKBitmap? bitmap;
		private int renderCount = 0;

		public Renderer()
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();

			var resolution = _fbDev.ScreenSize;

			WUX.Window.InvalidateRender
			+= () =>
			{
				Invalidate();
			};
			
			WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(resolution.Width, resolution.Height));
		}

		public Size PixelSize => _fbDev.ScreenSize;

		void Invalidate()
		{
			int width, height;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			var dpi = 1;

			var resolution = _fbDev.ScreenSize;

			width = (int)resolution.Width;
			height = (int)resolution.Height;

			var scaledWidth = (int)(width * dpi);
			var scaledHeight = (int)(height * dpi);

			var info = new SKImageInfo(scaledWidth, scaledHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(scaledWidth, scaledHeight, _fbDev.PixelFormat, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale((float)dpi);

				WUX.Window.Current.Compositor.Render(surface, info);

				_fbDev.VSync();
				Libc.memcpy(_fbDev.BufferAddress, bitmap.GetPixels(out _), new IntPtr(_fbDev.RowBytes * height));
			}
		}
	}
}
