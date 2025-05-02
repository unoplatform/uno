using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11SoftwareRenderer(IXamlRootHost host, X11Window x11window) : IX11Renderer
	{
		private const int BitmapPad = 32;

		private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
		private SKBitmap? _bitmap;
		private SKSurface? _surface;
		private X11AirspaceRenderHelper? _airspaceHelper;
		private IntPtr? _xImage;
		private int _renderCount;
		private IntPtr? _gc;
		private SKColor _background = SKColors.White;

		public void SetBackgroundColor(SKColor color) => _background = color;

		void IX11Renderer.Render(SKPicture picture, SKPath nativeClippingPath, float scaleX, float scaleY)
		{
			using var fpsDisposable = _fpsHelper.BeginFrame();

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {_renderCount++}");
			}

			var display = x11window.Display;
			var window = x11window.Window;
			using var lockDisposable = X11Helper.XLock(display);

			if (host is X11XamlRootHost { Closed.IsCompleted: true })
			{
				return;
			}

			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(display, window, ref attributes);
			var width = attributes.width;
			var height = attributes.height;

			// endianness might come into play here?
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

			// reset the bitmap if the size has changed
			if (_bitmap == null || _airspaceHelper == null || _surface == null || width != _bitmap.Width || height != _bitmap.Height)
			{
				_bitmap?.Dispose();
				_surface?.Dispose();
				_airspaceHelper?.Dispose();

				if (_xImage is { } xImage)
				{
					unsafe
					{
						// XDestroyImage frees the buffer as well, so we unset it first
						var ptr = (XImage*)xImage.ToPointer();
						ptr->data = IntPtr.Zero;
					}
					_ = XLib.XDestroyImage(xImage);
					_xImage = null;
				}

				_bitmap = new SKBitmap(width, height);
				_surface = SKSurface.Create(info, _bitmap.GetPixels(out _));
				_airspaceHelper = new X11AirspaceRenderHelper(display, window, width, height);
			}

			var canvas = _surface.Canvas;
			var saveCount = canvas.Save();
			canvas.Clear(_background);
			canvas.Scale(scaleX, scaleY);
			canvas.DrawPicture(picture);
			_fpsHelper.DrawFps(canvas);
			canvas.RestoreToCount(saveCount);
			canvas.Flush();

			_xImage ??= X11Helper.XCreateImage(
				display: display,
				visual: /* CopyFromParent */ 0,
				depth: (uint)attributes.depth,
				format: /* ZPixmap */ 2,
				offset: 0,
				data: _bitmap.GetPixels(),
				width: (uint)width,
				height: (uint)height,
				bitmap_pad: BitmapPad,
				bytes_per_line: 0); // 0 bytes per line assume contiguous lines i.e. pad * width

			_ = X11Helper.XPutImage(
				display: display,
				drawable: window,
				gc: _gc ??= X11Helper.XCreateGC(display, window, 0, 0),
				image: _xImage.Value,
				srcx: 0,
				srcy: 0,
				destx: 0,
				desty: 0,
				width: (uint)width,
				height: (uint)height);

			_airspaceHelper.XShapeClip(nativeClippingPath);
			_ = XLib.XFlush(display); // unnecessary on most X11 implementations
		}
	}
}
