using System;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.OpenGL;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Type _type = typeof(Win32NativeOpenGLWrapper);
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

	private HDC _hdc;
	private HGLRC _glContext;

	public Win32NativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		if (Win32WindowWrapper.XamlRootMap.GetHostForRoot(xamlRoot) is not { } wrapper)
		{
			throw new InvalidOperationException($"The XamlRoot and the XamlRootMap must be initialized before constructing a {_type.Name}.");
		}
		var hwnd = (HWND)(wrapper.NativeWindow as Win32NativeWindow)!.Hwnd;

		_hdc = PInvoke.GetDC(hwnd);
		if (_hdc == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}");
		}

		PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = PFD_FLAGS.PFD_DRAW_TO_WINDOW | PFD_FLAGS.PFD_SUPPORT_OPENGL | PFD_FLAGS.PFD_DOUBLEBUFFER;
		pfd.iPixelType = PFD_PIXEL_TYPE.PFD_TYPE_RGBA;
		pfd.cColorBits = 32;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 8;
		pfd.cDepthBits = 16;
		pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
		pfd.iLayerType = PFD_LAYER_TYPE.PFD_MAIN_PLANE;

		var pixelFormat = PInvoke.ChoosePixelFormat(_hdc, in pfd);
		if (pixelFormat == 0)
		{
			var choosePixelFormatError = Win32Helper.GetErrorMessage();
			var success = PInvoke.ReleaseDC(hwnd, _hdc) == 0;
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			throw new InvalidOperationException($"{nameof(PInvoke.ChoosePixelFormat)} failed: {choosePixelFormatError}");
		}

		// To inspect the chosen pixel format:
		// PIXELFORMATDESCRIPTOR temp_pfd = default;
		// unsafe
		// {
		// 	PInvoke.DescribePixelFormat(_hdc, pixelFormat, (uint)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(), &temp_pfd);
		// }

		if (PInvoke.SetPixelFormat(_hdc, pixelFormat, in pfd) == 0)
		{
			var setPixelFormatError = Win32Helper.GetErrorMessage();
			var success = PInvoke.ReleaseDC(hwnd, _hdc) == 0;
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			throw new InvalidOperationException($"{nameof(PInvoke.SetPixelFormat)} failed: {setPixelFormatError}");
		}

		_glContext = PInvoke.wglCreateContext(_hdc);
		if (_glContext == IntPtr.Zero)
		{
			var createContextError = Win32Helper.GetErrorMessage();
			var success = PInvoke.ReleaseDC(hwnd, _hdc) == 0;
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			throw new InvalidOperationException($"{nameof(PInvoke.wglCreateContext)} failed: {createContextError}");
		}
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	public bool TryGetProcAddress(string proc, out nint addr)
	{
		if (_opengl32.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_opengl32.Value, proc, out addr))
		{
			return true;
		}

		addr = PInvoke.wglGetProcAddress(proc);
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
		if (PInvoke.wglDeleteContext(_glContext) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.wglDeleteContext)} failed.");
			}
		}
		_glContext = default;
		_hdc = default;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = PInvoke.wglGetCurrentContext();
		var dc = PInvoke.wglGetCurrentDC();
		if (PInvoke.wglMakeCurrent(_hdc, _glContext) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.wglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (PInvoke.wglMakeCurrent(dc, glContext) == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(PInvoke.wglMakeCurrent)} failed.");
				}
			}
		});
	}
}
