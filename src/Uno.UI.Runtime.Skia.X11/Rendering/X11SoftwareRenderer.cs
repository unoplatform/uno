using System;
using SkiaSharp;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11SoftwareRenderer : X11Renderer
	{
		private const int BitmapPad = 32;

		private readonly IntPtr _gc;
		private SKBitmap? _bitmap;
		private IntPtr? _xImage;
		private int _width;
		private int _height;
		private readonly uint _depth;

		public X11SoftwareRenderer(IXamlRootHost host, X11Window x11Window) : base(host, x11Window)
		{
			using var lockDisposable = X11Helper.XLock(_x11Window.Display);
			_gc = X11Helper.XCreateGC(x11Window.Display, x11Window.Window, 0, 0);
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attributes);
			_depth = (uint)attributes.depth;
		}

		protected override SKSurface UpdateSize(int width, int height)
		{
			using var lockDisposable = X11Helper.XLock(_x11Window.Display);

			_width = width;
			_height = height;

			_bitmap?.Dispose();
			_bitmap = new SKBitmap(width, height);

			if (_xImage is { } xImage)
			{
				unsafe
				{
					// XDestroyImage frees the buffer as well, so we unset it first
					var ptr = (XImage*)xImage.ToPointer();
					ptr->data = IntPtr.Zero;
				}
				_ = XLib.XDestroyImage(xImage);
			}

			_xImage = X11Helper.XCreateImage(
				display: _x11Window.Display,
				visual: /* CopyFromParent */ 0,
				depth: _depth,
				format: /* ZPixmap */ 2,
				offset: 0,
				data: _bitmap.GetPixels(),
				width: (uint)width,
				height: (uint)height,
				bitmap_pad: BitmapPad,
				bytes_per_line: 0); // 0 bytes per line assume contiguous lines i.e. pad * width

			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			return SKSurface.Create(info, _bitmap.GetPixels(out _));
		}

		protected override void Flush()
		{
			_ = X11Helper.XPutImage(
				display: _x11Window.Display,
				drawable: _x11Window.Window,
				gc: _gc,
				image: _xImage!.Value,
				srcx: 0,
				srcy: 0,
				destx: 0,
				desty: 0,
				width: (uint)_width,
				height: (uint)_height);
		}

		public override void Dispose()
		{
			_bitmap?.Dispose();
			if (_xImage is { } xImage)
			{
				unsafe
				{
					// XDestroyImage frees the buffer as well, so we unset it first
					var ptr = (XImage*)xImage.ToPointer();
					ptr->data = IntPtr.Zero;
				}
				_ = XLib.XDestroyImage(xImage);
			}
		}
	}
}
