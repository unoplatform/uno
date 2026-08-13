#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// The software (CPU-framebuffer) X11 graphics context. Uno owns the window binding and present here — it
/// allocates a BGRA buffer, wraps it in an <see cref="XImage"/>, and blits it to the window with
/// <c>XPutImage</c> — while naming no Skia type. The matched backend wraps the handed-over
/// <see cref="ISoftwareRenderTarget"/> into its own surface to render into.
/// </summary>
internal sealed class X11SoftwareGraphicsContext : IGraphicsContext
{
	private const int BitmapPad = 32;

	private readonly IntPtr _display;
	private readonly IntPtr _window;
	private readonly IntPtr _gc;
	private readonly uint _depth;

	private IntPtr _buffer;
	private IntPtr _xImage;
	private int _width;
	private int _height;

	public X11SoftwareGraphicsContext(X11Window window)
	{
		_display = window.Display;
		_window = window.Window;

		using var lockDisposable = X11Helper.XLock(_display);
		_gc = X11Helper.XCreateGC(_display, _window, 0, 0);
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(_display, _window, ref attributes);
		_depth = (uint)attributes.depth;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_buffer == IntPtr.Zero || width != _width || height != _height)
		{
			Reallocate(width, height);
		}

		return new X11SoftwareRenderTarget(_buffer, _width * 4, _width, _height);
	}

	public void Present()
	{
		if (_xImage == IntPtr.Zero)
		{
			return;
		}

		using var lockDisposable = X11Helper.XLock(_display);
		_ = X11Helper.XPutImage(_display, _window, _gc, _xImage, 0, 0, 0, 0, (uint)_width, (uint)_height);
	}

	private void Reallocate(int width, int height)
	{
		using var lockDisposable = X11Helper.XLock(_display);

		DestroyImage();

		_width = width;
		_height = height;
		_buffer = Marshal.AllocHGlobal(width * 4 * height);

		// bytes_per_line: 0 → X computes width * 4 from bitmap_pad=32 for a 32-bit ZPixmap, matching the
		// width*4 stride the backend uses when wrapping the buffer.
		_xImage = X11Helper.XCreateImage(_display, IntPtr.Zero, _depth, /* ZPixmap */ 2, 0, _buffer, (uint)width, (uint)height, BitmapPad, 0);
	}

	private unsafe void DestroyImage()
	{
		if (_xImage != IntPtr.Zero)
		{
			// XDestroyImage frees the data buffer too, but we own that buffer, so detach it first.
			((XImage*)_xImage)->data = IntPtr.Zero;
			_ = XLib.XDestroyImage(_xImage);
			_xImage = IntPtr.Zero;
		}

		if (_buffer != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_buffer);
			_buffer = IntPtr.Zero;
		}
	}

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_display);
		DestroyImage();
	}

	private sealed class X11SoftwareRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}
