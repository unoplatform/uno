#nullable enable


using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.OpenGL;

#if WINAPPSDK
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
#else
using System.Windows.Interop;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using WpfWindow = System.Windows.Window;
#endif

#if WINAPPSDK
using WpfRenderingNativeMethods = Uno.WinUI.Graphics3DGL.WindowsRenderingNativeMethods;
#else
#endif

#if WINAPPSDK
namespace Uno.WinUI.Graphics3DGL;
#else
namespace Uno.UI.Runtime.Skia.Wpf.Extensions;
#endif

#if WINAPPSDK
internal class WinUINativeOpenGLWrapper(Func<Window> getWindowFunc)
#else
internal class WpfNativeOpenGLWrapper
#endif
	: INativeOpenGLWrapper
{
	private nint _hdc;
	private nint _glContext;

	public void CreateContext(UIElement element)
	{
#if WINAPPSDK
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(getWindowFunc());
#else
		if (element.XamlRoot?.HostWindow?.NativeWindow is not WpfWindow wpfWindow)
		{
			throw new InvalidOperationException($"The XamlRoot and its NativeWindow must be initialized on the element before calling {nameof(CreateContext)}.");
		}
		var hwnd = new WindowInteropHelper(wpfWindow).Handle;
#endif

		_hdc = WindowsRenderingNativeMethods.GetDC(hwnd);

		WindowsRenderingNativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = WindowsRenderingNativeMethods.PFD_DRAW_TO_WINDOW | WindowsRenderingNativeMethods.PFD_SUPPORT_OPENGL | WindowsRenderingNativeMethods.PFD_DOUBLEBUFFER;
		pfd.iPixelType = WindowsRenderingNativeMethods.PFD_TYPE_RGBA;
		pfd.cColorBits = 32;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 8;
		pfd.cDepthBits = 16;
		pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
		pfd.iLayerType = WindowsRenderingNativeMethods.PFD_MAIN_PLANE;

		var pixelFormat = WindowsRenderingNativeMethods.ChoosePixelFormat(_hdc, ref pfd);

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

		if (WindowsRenderingNativeMethods.SetPixelFormat(_hdc, pixelFormat, ref pfd) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"SetPixelFormat failed");
			}
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}

		_glContext = WindowsRenderingNativeMethods.wglCreateContext(_hdc);

		if (_glContext == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"wglCreateContext failed");
			}
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}
	}

	public object CreateGLSilkNETHandle() => GL.GetApi(new WindowsGlNativeContext());

	public void DestroyContext()
	{
		if (WindowsRenderingNativeMethods.wglDeleteContext(_glContext) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(WindowsRenderingNativeMethods.wglDeleteContext)} failed.");
			}
		}
		_glContext = default;
		_hdc = default;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = WindowsRenderingNativeMethods.wglGetCurrentContext();
		var dc = WindowsRenderingNativeMethods.wglGetCurrentDC();
		if (WindowsRenderingNativeMethods.wglMakeCurrent(_hdc, _glContext) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(WindowsRenderingNativeMethods.wglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (WindowsRenderingNativeMethods.wglMakeCurrent(dc, glContext) == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(WindowsRenderingNativeMethods.wglMakeCurrent)} failed.");
				}
			}
		});
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	private class WindowsGlNativeContext : INativeContext
	{
		private readonly UnmanagedLibrary _l;

		public WindowsGlNativeContext()
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

			addr = WindowsRenderingNativeMethods.wglGetProcAddress(proc);
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
