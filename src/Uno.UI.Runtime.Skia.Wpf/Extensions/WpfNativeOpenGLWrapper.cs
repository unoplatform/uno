#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal class WpfNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private nint _hdc;
	private nint _glContext;

	public void CreateContext(UIElement element)
	{
		if (element.XamlRoot?.HostWindow?.NativeWindow is not WpfWindow wpfWindow)
		{
			throw new InvalidOperationException($"The XamlRoot and its NativeWindow must be initialzied on the element before calling {nameof(CreateContext)}.");
		}
		var hwnd = new WindowInteropHelper(wpfWindow).Handle;

		_hdc = WpfRenderingNativeMethods.GetDC(hwnd);

		WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = WpfRenderingNativeMethods.PFD_DRAW_TO_WINDOW | WpfRenderingNativeMethods.PFD_SUPPORT_OPENGL | WpfRenderingNativeMethods.PFD_DOUBLEBUFFER;
		pfd.iPixelType = WpfRenderingNativeMethods.PFD_TYPE_RGBA;
		pfd.cColorBits = 32;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 8;
		pfd.cDepthBits = 16;
		pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
		pfd.iLayerType = WpfRenderingNativeMethods.PFD_MAIN_PLANE;

		var pixelFormat = WpfRenderingNativeMethods.ChoosePixelFormat(_hdc, ref pfd);

		// To inspect the chosen pixel format:
		// WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR temp_pfd = default;
		// WpfRenderingNativeMethods.DescribePixelFormat(_hdc, _pixelFormat, (uint)Marshal.SizeOf<WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR>(), ref temp_pfd);

		if (pixelFormat == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"ChoosePixelFormat failed");
			}
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}

		if (WpfRenderingNativeMethods.SetPixelFormat(_hdc, pixelFormat, ref pfd) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"SetPixelFormat failed");
			}
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}

		_glContext = WpfRenderingNativeMethods.wglCreateContext(_hdc);

		if (_glContext == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"wglCreateContext failed");
			}
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}
	}

	public object CreateGLSilkNETHandle() => GL.GetApi(new WpfGlNativeContext());

	public void DestroyContext()
	{
		WpfRenderingNativeMethods.wglDeleteContext(_glContext);
		_glContext = default;
		_hdc = default;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = WpfRenderingNativeMethods.wglGetCurrentContext();
		var dc = WpfRenderingNativeMethods.wglGetCurrentDC();
		WpfRenderingNativeMethods.wglMakeCurrent(_hdc, _glContext);
		return Disposable.Create(() => WpfRenderingNativeMethods.wglMakeCurrent(dc, glContext));
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	private class WpfGlNativeContext : INativeContext
	{
		private readonly UnmanagedLibrary _l;

		public WpfGlNativeContext()
		{
			_l = new UnmanagedLibrary("opengl32.dll");
			if (_l.Handle == IntPtr.Zero)
			{
				throw new PlatformNotSupportedException("Unable to load opengl32.dll. Make sure you're running on a system with OpenGL support");
			}
		}

		public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
		{
			if (_l.TryLoadFunction(proc, out addr))
			{
				return true;
			}

			addr = WpfRenderingNativeMethods.wglGetProcAddress(proc);
			return addr != IntPtr.Zero;
		}

		public nint GetProcAddress(string proc, int? slot = null)
		{
			if (TryGetProcAddress(proc, out var address, slot))
			{
				return address;
			}

			throw new InvalidOperationException("No function was found with the name " + proc + ".");
		}

		public void Dispose() => _l.Dispose();
	}
}
