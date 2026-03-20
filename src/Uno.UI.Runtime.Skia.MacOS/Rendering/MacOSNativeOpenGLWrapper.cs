using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.MacOS;

// ANGLE implements EGL 1.5
// https://registry.khronos.org/EGL/api/EGL/egl.h
// Permalink: https://github.com/KhronosGroup/EGL-Registry/blob/29c4314e0ef04c730992d295f91b76635019fbba/api/EGL/egl.h

internal partial class MacOSNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface;

	public MacOSNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		_eglDisplay = EglHelper.EglGetDisplay(EglHelper.EGL_DEFAULT_DISPLAY);
		(_pBufferSurface, _glContext, var major, var minor, _, _)
			= EglHelper.InitializeGles2Context(_eglDisplay);
		this.LogInfo()?.Info($"Created a {nameof(MacOSNativeOpenGLWrapper)} instance using EGL {major}.{minor}.");
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
		addr = EglHelper.EglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public void Dispose()
	{
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			EglHelper.EglDestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			EglHelper.EglDestroyContext(_eglDisplay, _glContext);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = EglHelper.EglGetCurrentContext();
		var display = EglHelper.EglGetCurrentDisplay();
		var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
		var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
		if (!EglHelper.EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!EglHelper.EglMakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			}
		});
	}

	public static void Register() => ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new MacOSNativeOpenGLWrapper(xamlRoot));
}
