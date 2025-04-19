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
using System.Runtime.InteropServices.JavaScript;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia
{
	class Renderer
	{
		private readonly IXamlRootHost _host;
		private FrameBufferDevice _fbDev;
		private SKBitmap? _bitmap;
		private bool _needsScanlineCopy;
		private int renderCount;
		private DisplayInformation? _displayInformation;

		public Renderer(IXamlRootHost host)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();

			_host = host;
		}

		public FrameBufferDevice FrameBufferDevice => _fbDev;

		internal void InvalidateRender() => Invalidate();

		void Invalidate()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			_displayInformation ??= DisplayInformation.GetForCurrentViewSafe();

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

				FrameBufferWindowWrapper.Instance.RaiseNativeSizeChanged(new Size(rawScreenSize.Width / scale, rawScreenSize.Height / scale));
			}

			using (var surface = SKSurface.Create(info, _bitmap.GetPixels(out _)))
			{
				surface.Canvas.Clear(SKColors.White);
				surface.Canvas.Scale((float)scale);

				if (_host.RootElement?.Visual is { } rootVisual)
				{
					_host.RootElement.XamlRoot!.Compositor.RenderRootVisual(surface.Canvas, rootVisual);
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Unable to render frame, _host.RootElement?.Visual is null");
					}
				}

				_fbDev.VSync();

				if (_needsScanlineCopy)
				{
					var pixels = _bitmap.GetPixels(out _);
					var bitmapRowBytes = _bitmap.RowBytes;
					var bitmapBytesPerPixel = _bitmap.BytesPerPixel;

					for (int line = 0; line < height; line++)
					{
#pragma warning disable CA1806 // Do not ignore method results
						Libc.memcpy(
							_fbDev.BufferAddress + line * _fbDev.RowBytes,
							pixels + line * bitmapRowBytes,
							new IntPtr(width * bitmapBytesPerPixel));
#pragma warning restore CA1806 // Do not ignore method results
					}
				}
				else
				{
#pragma warning disable CA1806 // Do not ignore method results
					Libc.memcpy(_fbDev.BufferAddress, _bitmap.GetPixels(out _), new IntPtr(_fbDev.RowBytes * height));
#pragma warning restore CA1806 // Do not ignore method results
				}
			}
		}
	}
}
