using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using WUX = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.Native;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia
{
	class Renderer
	{
		private readonly FrameBufferHost _host;
		private FrameBufferDevice _fbDev;
		private SKBitmap? bitmap;
		private int renderCount = 0;

		public Renderer(FrameBufferHost host)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();

			var resolution = _fbDev.ScreenSize;
			
			WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(resolution.Width, resolution.Height));
			_host = host;
		}

		public Size PixelSize => _fbDev.ScreenSize;

		internal void InvalidateRender() => Invalidate();

		void Invalidate()
		{
			int width, height;

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			var scale = _host.ScaleOverride ?? 1f;

			var resolution = _fbDev.ScreenSize;

			width = (int)resolution.Width;
			height = (int)resolution.Height;

			var scaledWidth = (int)(width * scale);
			var scaledHeight = (int)(height * scale);

			var info = new SKImageInfo(scaledWidth, scaledHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(scaledWidth, scaledHeight, _fbDev.PixelFormat, SKAlphaType.Premul);
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale(scale);

				WUX.Window.Current.Compositor.Render(surface);

				_fbDev.VSync();
				Libc.memcpy(_fbDev.BufferAddress, bitmap.GetPixels(out _), new IntPtr(_fbDev.RowBytes * height));
			}
		}
	}
}
