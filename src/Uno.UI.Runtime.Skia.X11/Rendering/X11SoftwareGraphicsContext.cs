#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Software (CPU-framebuffer) X11 graphics context: allocates a BGRA buffer, wraps it in an
/// <see cref="XImage"/>, and blits it to the window with <c>XPutImage</c>. The backend renders into the
/// handed-over <see cref="ISoftwareRenderTarget"/>.
/// </summary>
internal sealed class X11SoftwareGraphicsContext : ISwapChain
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
	private X11SoftwareRenderTarget? _target;

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

	// Reuses one persistent CPU buffer across frames (reallocated only on resize), so the compositor can repaint
	// only the damaged region.
	public bool PreservesContents => true;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_target is null || width != _width || height != _height)
		{
			Reallocate(width, height);
			_target = new X11SoftwareRenderTarget(_buffer, _width * 4, _width, _height);
		}

		return _target;
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
