using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11SoftwareRenderer(IXamlRootHost host, X11Window x11window) : IX11Renderer
	{
		private const int ColorDepth = 24;
		private const int BitmapPad = 32;

		private SKBitmap? _bitmap;
		private SKSurface? _surface;
		private IntPtr? _xImage;
		private int renderCount;

		void IX11Renderer.InvalidateRender()
		{
			// Could this somehow race? i.e. Closed is completed right after the check?
			if (host is X11XamlRootHost x11Host && x11Host.Closed.IsCompleted)
			{
				return;
			}

			using var __ = X11Helper.XLock(x11window.Display);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			XWindowAttributes attributes = default;
			var _1 = XLib.XGetWindowAttributes(x11window.Display, x11window.Window, ref attributes);

			var width = attributes.width;
			var height = attributes.height;

			// TODO: make sure this works everywhere. AFAICT everyone is using 24 bit color with 32 bit_pad
			// endianness might come into play here?
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (_bitmap == null || _surface == null || info.Width != _bitmap.Width || info.Height != _bitmap.Height)
			{
				_bitmap?.Dispose();
				_surface?.Dispose();
				
				if (_xImage is { } xImage)
				{
					unsafe
					{
						// XDestroyImage frees the buffer as well, so we unset it first
						var ptr = (XImage*)xImage.ToPointer();
						ptr->data = IntPtr.Zero;
					}
					var _3 = XLib.XDestroyImage(xImage);
					_xImage = null;
				}

				_bitmap = new SKBitmap(width, height);
				_surface = SKSurface.Create(info, _bitmap.GetPixels(out _));
			}

			_surface.Canvas.Clear(SKColors.Transparent);

			if (host.RootElement?.Visual is { } rootVisual)
			{
				host.RootElement.XamlRoot!.Compositor.RenderRootVisual(_surface, rootVisual);
			}

			_xImage ??= X11Helper.XCreateImage(
				display: x11window.Display,
				visual: /* CopyFromParent */ 0,
				depth: ColorDepth,
				format: /* ZPixmap */ 2,
				offset: 0,
				data: _bitmap.GetPixels(),
				width: (uint)width,
				height: (uint)height,
				bitmap_pad: BitmapPad,
				bytes_per_line: 0); // 0 bytes per line assume contiguous lines i.e. pad * width

			var _2 = X11Helper.XPutImage(
				display: x11window.Display,
				drawable: x11window.Window,
				gc: X11Helper.XDefaultGC(x11window.Display, XLib.XDefaultScreen(x11window.Display)),
				image: _xImage.Value,
				srcx: 0,
				srcy: 0,
				destx: 0,
				desty: 0,
				width: (uint)width,
				height: (uint)height);

			var _4 = XLib.XFlush(x11window.Display); // unnecessary on most X11 implementations
		}
	}
}
