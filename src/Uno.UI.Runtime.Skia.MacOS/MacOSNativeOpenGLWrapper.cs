using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.MacOS;

// ANGLE implements EGL 1.5
// https://registry.khronos.org/EGL/api/EGL/egl.h
// Permalink: https://github.com/KhronosGroup/EGL-Registry/blob/29c4314e0ef04c730992d295f91b76635019fbba/api/EGL/egl.h

internal partial class MacOSNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Lazy<IntPtr> _libGLES = new Lazy<IntPtr>(() =>
	{
		if (!NativeLibrary.TryLoad("libGLESv2.dylib", typeof(MacOSNativeOpenGLWrapper).Assembly, DllImportSearchPath.UserDirectories, out var _handle))
		{
			if (typeof(MacOSNativeOpenGLWrapper).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSNativeOpenGLWrapper).Log().Error("libGLESv2.dylib was not loaded successfully.");
			}
		}
		return _handle;
	});

	private const int EGL_DEFAULT_DISPLAY = 0;
	private const int EGL_NO_CONTEXT = 0;
	private const int EGL_ALPHA_SIZE = 0x3021;
	private const int EGL_BLUE_SIZE = 0x3022;
	private const int EGL_GREEN_SIZE = 0x3023;
	private const int EGL_RED_SIZE = 0x3024;
	private const int EGL_DEPTH_SIZE = 0x3025;
	private const int EGL_STENCIL_SIZE = 0x3026;
	private const int EGL_NONE = 0x3038;
	private const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
	private const int EGL_DRAW = 0x3059;
	private const int EGL_READ = 0x305A;

	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface;

	public MacOSNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		_eglDisplay = EglGetDisplay(EGL_DEFAULT_DISPLAY);
		EglInitialize(_eglDisplay, out var major, out var minor);
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Found EGL version {major}.{minor}.");
		}

		int[] attribList =
		{
			EGL_RED_SIZE, 8,
			EGL_GREEN_SIZE, 8,
			EGL_BLUE_SIZE, 8,
			EGL_ALPHA_SIZE, 8,
			EGL_DEPTH_SIZE, 8,
			EGL_STENCIL_SIZE, 1,
			EGL_NONE
		};

		var configs = new IntPtr[1];
		var success = EglChooseConfig(_eglDisplay, attribList, configs, configs.Length, out var numConfig);

		if (!success || numConfig < 1)
		{
			throw new InvalidOperationException($"{nameof(EglChooseConfig)} failed: {EglGetError()}");
		}

		// ANGLE implements GLES 3
		_glContext = EglCreateContext(_eglDisplay, configs[0], EGL_NO_CONTEXT, new[] { EGL_CONTEXT_CLIENT_VERSION, 3, EGL_NONE });
		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL context creation failed: {EglGetError()}");
		}

		_pBufferSurface = EglCreatePbufferSurface(_eglDisplay, configs[0], new[] { EGL_NONE });
		if (_pBufferSurface == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL pbuffer surface creation failed: {EglGetError()}");
		}
	}

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"A procedure named {proc} was not found in libGLES");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		if (_libGLES.Value == IntPtr.Zero)
		{
			addr = IntPtr.Zero;
			return false;
		}
		return NativeLibrary.TryGetExport(_libGLES.Value, proc, out addr);
	}

	public void Dispose()
	{
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			EglDestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			EglDestroyContext(_eglDisplay, _glContext);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = EglGetCurrentContext();
		var display = EglGetCurrentDisplay();
		var readSurface = EglGetCurrentSurface(EGL_READ);
		var drawSurface = EglGetCurrentSurface(EGL_DRAW);
		if (!EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!EglMakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglMakeCurrent)} failed.");
				}
			}
		});
	}

	public static void Register() => ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new MacOSNativeOpenGLWrapper(xamlRoot));
}
