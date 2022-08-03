using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using WUX = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.Native;
using Uno.Foundation.Logging;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia
{
	class Renderer
	{
		private FrameBufferDevice _fbDev;
		private SKBitmap? bitmap;
		private int renderCount = 0;
		private DisplayInformation? _displayInformation;

		public Renderer()
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();

			WUX.Window.Current.ToString();
		}

		public FrameBufferDevice FrameBufferDevice => _fbDev;

		internal void InvalidateRender() => Invalidate();

		void Invalidate()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			_displayInformation ??= DisplayInformation.GetForCurrentView();

			var scale = _displayInformation.RawPixelsPerViewPixel;

			var rawScreenSize = _fbDev.ScreenSize;

			int width = (int)rawScreenSize.Width;
			int height = (int)rawScreenSize.Height;

			var info = new SKImageInfo(width, height, _fbDev.PixelFormat, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (bitmap == null || info.Width != bitmap.Width || info.Height != bitmap.Height)
			{
				bitmap = new SKBitmap(width, height, _fbDev.PixelFormat, SKAlphaType.Premul);

				WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(rawScreenSize.Width / scale, rawScreenSize.Height / scale));
			}

			using (var surface = SKSurface.Create(info, bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale((float)scale);

				WUX.Window.Current.Compositor.Render(surface);

				_fbDev.VSync();
				Libc.memcpy(_fbDev.BufferAddress, bitmap.GetPixels(out _), new IntPtr(_fbDev.RowBytes * height));
			}
		}
	}
}
