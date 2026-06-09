#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.WinUI.Graphics3DGL;

internal class WinUINativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Type _type = typeof(WinUINativeOpenGLWrapper);

	private static readonly Lazy<IntPtr> _opengl32 = new Lazy<IntPtr>(() =>
	{
		if (!NativeLibrary.TryLoad("opengl32.dll", _type.Assembly, DllImportSearchPath.UserDirectories, out var _handle))
		{
			if (_type.Log().IsEnabled(LogLevel.Error))
			{
				_type.Log().Error("opengl32.dll was not loaded successfully.");
			}
		}
		return _handle;
	});

	private readonly Func<Window> _getWindowFunc;
	private nint _hdc;
	private nint _glContext;

	public WinUINativeOpenGLWrapper(XamlRoot xamlRoot, Func<Window> getWindowFunc)
	{
		_getWindowFunc = getWindowFunc;

		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_getWindowFunc());

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
		// WindowsRenderingNativeMethods.PIXELFORMATDESCRIPTOR temp_pfd = default;
		// WindowsRenderingNativeMethods.DescribePixelFormat(_hdc, _pixelFormat, (uint)Marshal.SizeOf<WindowsRenderingNativeMethods.PIXELFORMATDESCRIPTOR>(), ref temp_pfd);

		if (pixelFormat == 0)
		{
			throw new InvalidOperationException("ChoosePixelFormat failed");
		}

		if (WindowsRenderingNativeMethods.SetPixelFormat(_hdc, pixelFormat, ref pfd) == 0)
		{
			throw new InvalidOperationException("SetPixelFormat failed");
		}

		_glContext = WindowsRenderingNativeMethods.wglCreateContext(_hdc);

		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException("wglCreateContext failed");
		}
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	public bool TryGetProcAddress(string proc, out nint addr)
	{
		if (_opengl32.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_opengl32.Value, proc, out addr))
		{
			return true;
		}

		addr = WindowsRenderingNativeMethods.wglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public nint GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var address))
		{
			return address;
		}

		throw new InvalidOperationException("No function was found with the name " + proc + ".");
	}

	public void Dispose()
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
}
