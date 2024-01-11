using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using Avalonia.X11;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11Renderer(IXamlRootHost host, X11Window x11window)
	{
		private const int COLOR_DEPTH = 24;
		private const int BITMAP_PAD = 32;

		private SKBitmap? _bitmap;
		private int renderCount;
		internal void InvalidateRender()
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {renderCount++}");
			}

			XWindowAttributes attributes = default;
			XLib.XGetWindowAttributes(x11window.Display, x11window.Window, ref attributes);

			var width = attributes.width;
			var height = attributes.height;

			// TODO: make sure this works everywhere. AFAICT everyone is using 24 bit color with 32 bit_pad
			// endianness might come into play here?
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (_bitmap == null || info.Width != _bitmap.Width || info.Height != _bitmap.Height)
			{
				_bitmap?.Dispose();
				_bitmap = new SKBitmap(width, height);
			}

			using var surface = SKSurface.Create(info, _bitmap.GetPixels(out _));
			surface.Canvas.Clear(SKColors.Transparent);

			if (host.RootElement?.Visual is { } rootVisual)
			{
				host.RootElement.XamlRoot!.Compositor.RenderRootVisual(surface, rootVisual);
			}

			IntPtr ximage = X11Helper.XCreateImage(
				display: x11window.Display,
				visual: /* CopyFromParent */ 0,
				depth: COLOR_DEPTH,
				format: /* ZPixmap */ 2,
				offset: 0,
				data: _bitmap.GetPixels(),
				width: (uint)width,
				height: (uint)height,
				bitmap_pad: BITMAP_PAD,
				bytes_per_line: 0); // 0 bytes per line assume contiguous lines i.e. pad * width

			X11Helper.XPutImage(
				display: x11window.Display,
				drawable: x11window.Window,
				gc: X11Helper.XDefaultGC(x11window.Display, XLib.XDefaultScreen(x11window.Display)),
				image: ximage,
				srcx: 0,
				srcy: 0,
				destx: 0,
				desty: 0,
				width: (uint)width,
				height: (uint)height);

			unsafe
			{
				// XDestroyImage frees the buffer as well, so we unset it first
				var ptr = (XImage*)ximage.ToPointer();
				ptr->data = IntPtr.Zero;
			}
			XLib.XDestroyImage(ximage);

			XLib.XFlush(x11window.Display); // unnecessary on most X11 implementations
		}
	}
}
