using Microsoft.UI.Xaml;
using OpenTK.Graphics.Egl;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.MacOS;

// ANGLE implements EGL 1.5
// https://registry.khronos.org/EGL/api/EGL/egl.h
// Permalink: https://github.com/KhronosGroup/EGL-Registry/blob/29c4314e0ef04c730992d295f91b76635019fbba/api/EGL/egl.h

public class MacOSNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const int EGL_DEFAULT_DISPLAY = 0;
	private const int EGL_NO_CONTEXT = 0;

	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface;

	public void CreateContext(UIElement element)
	{
		_eglDisplay = Egl.GetDisplay(EGL_DEFAULT_DISPLAY);
		int[] pi32ConfigAttribs =
		{
			Egl.RED_SIZE, 8,
			Egl.GREEN_SIZE, 8,
			Egl.BLUE_SIZE, 8,
			Egl.ALPHA_SIZE, 8,
			Egl.DEPTH_SIZE, 8,
			Egl.STENCIL_SIZE, 1,
			Egl.SAMPLES, 2,
			Egl.SAMPLE_BUFFERS, 1,
			Egl.NONE
		};

		var configs = new IntPtr[1];
		var success = Egl.ChooseConfig(_eglDisplay, pi32ConfigAttribs, configs, configs.Length, out var numConfig);

		if (!success)
		{
			throw new InvalidOperationException($"{nameof(Egl.ChooseConfig)} failed.");
		}

		// ANGLE implements GLES 3
		_glContext = Egl.CreateContext(_eglDisplay, configs[0], EGL_NO_CONTEXT, new[] { Egl.CONTEXT_CLIENT_VERSION, 3, Egl.NONE });
		_pBufferSurface = Egl.CreatePbufferSurface(_eglDisplay, configs[0], new[] { Egl.NONE });

		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"OpenGL context creation failed");
		}
		if (_pBufferSurface == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL pbuffer surface creation failed");
		}
	}
	public object CreateGLSilkNETHandle() => GL.GetApi(new DefaultNativeContext("libGLESv2.so"));
	public void DestroyContext()
	{
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			Egl.DestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			Egl.DestroyContext(_eglDisplay, _glContext);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
	}
	public IDisposable MakeCurrent()
	{
		var glContext = Egl.GetCurrentContext();
		var display = Egl.GetCurrentDisplay();
		var readSurface = Egl.GetCurrentSurface(Egl.READ);
		var drawSurface = Egl.GetCurrentSurface(Egl.DRAW);
		if (!Egl.MakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(Egl.MakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!Egl.MakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(Egl.MakeCurrent)} failed.");
				}
			}
		});
	}
	public static void Register() => ApiExtensibility.Register(typeof(INativeOpenGLWrapper), _ => new MacOSNativeOpenGLWrapper());
}
