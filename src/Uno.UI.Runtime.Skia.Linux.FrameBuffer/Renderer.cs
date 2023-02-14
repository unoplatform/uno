using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using SkiaSharp;
using Uno.Extensions;
using WUX = Microsoft.UI.Xaml;
using Uno.UI.Runtime.Skia.Native;
using Uno.Foundation.Logging;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia
{
	class Renderer
	{
		private FrameBufferDevice _fbDev;
		private SKBitmap? _bitmap;
		private bool _needsScanlineCopy;
		private int renderCount;
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
			if (_bitmap == null || info.Width != _bitmap.Width || info.Height != _bitmap.Height)
			{
				_bitmap = new SKBitmap(width, height, _fbDev.PixelFormat, SKAlphaType.Premul);

				_needsScanlineCopy = _fbDev.RowBytes != _bitmap.BytesPerPixel * width;

				WUX.Window.Current.OnNativeSizeChanged(new Size(rawScreenSize.Width / scale, rawScreenSize.Height / scale));
			}

			using (var surface = SKSurface.Create(info, _bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);

				surface.Canvas.Scale((float)scale);

				WUX.Window.Current.Compositor.Render(surface);

				_fbDev.VSync();

				if (_needsScanlineCopy)
				{
					var pixels = _bitmap.GetPixels(out _);
					var bitmapRowBytes = _bitmap.RowBytes;
					var bitmapBytesPerPixel = _bitmap.BytesPerPixel;

					for (int line = 0; line < height; line++)
					{
						Libc.memcpy(
							_fbDev.BufferAddress + line * _fbDev.RowBytes,
							pixels + line * bitmapRowBytes,
							new IntPtr(width * bitmapBytesPerPixel));
					}
				}
				else
				{
					Libc.memcpy(_fbDev.BufferAddress, _bitmap.GetPixels(out _), new IntPtr(_fbDev.RowBytes * height));
				}
			}
		}
	}
}
