#if ANDROID

using System;
using Android.Opengl;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.WinUI.Graphics3DGL;

public abstract partial class GLCanvasElement
{
	static GLCanvasElement()
	{
		ApiExtensibility.Register(typeof(INativeOpenGLWrapper), _ => new AndroidNativeOpenGLWrapper());
	}

	private class AndroidNativeOpenGLWrapper : INativeOpenGLWrapper
	{
		private EGLDisplay? _eglDisplay;
		private EGLSurface? _pBufferSurface;
		private EGLContext? _glContext;

		public void CreateContext(UIElement element)
		{
			_eglDisplay = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
			int[] pi32ConfigAttribs =
			{
				EGL14.EglRedSize, 8,
				EGL14.EglGreenSize, 8,
				EGL14.EglBlueSize, 8,
				EGL14.EglAlphaSize, 8,
				EGL14.EglDepthSize, 8,
				EGL14.EglStencilSize, 1,
				EGL14.EglSampleBuffers, 1,
				EGL14.EglNone
			};
			EGLConfig[] configs = new EGLConfig[1];
			int[] numConfigs = new int[1];
			var success = EGL14.EglChooseConfig(_eglDisplay, pi32ConfigAttribs, 0, configs, 0, configs.Length, numConfigs, 0);
			if (!success)
			{
				throw new InvalidOperationException($"{nameof(EGL14.EglChooseConfig)} failed.");
			}

			// over 95% of android phone have >= OpenGL ES 3.0 support https://developer.android.com/about/dashboards#OpenGL
			_glContext = EGL14.EglCreateContext(_eglDisplay, configs[0], EGL14.EglNoContext, new[] { EGL14.EglContextClientVersion, 2, EGL14.EglNone }, 0);
			_pBufferSurface = EGL14.EglCreatePbufferSurface(_eglDisplay, configs[0], new[] { EGL14.EglNone }, 0);

			if (_glContext is null)
			{
				throw new InvalidOperationException($"OpenGL context creation failed");
			}
			if (_pBufferSurface is null)
			{
				throw new InvalidOperationException($"EGL pbuffer surface creation failed");
			}
		}

		public object CreateGLSilkNETHandle() => GL.GetApi(new DefaultNativeContext("libGLESv2.so"));

		public void DestroyContext()
		{
			if (_eglDisplay is { } && _pBufferSurface is { })
			{
				EGL14.EglDestroySurface(_eglDisplay, _pBufferSurface);
			}
			if (_eglDisplay is { } && _glContext is { })
			{
				EGL14.EglDestroyContext(_eglDisplay, _glContext);
			}

			_pBufferSurface = null;
			_glContext = null;
			_eglDisplay = null;
		}
		public IDisposable MakeCurrent()
		{
			var glContext = EGL14.EglGetCurrentContext();
			var display = EGL14.EglGetCurrentDisplay();
			var readSurface = EGL14.EglGetCurrentSurface(EGL14.EglRead);
			var drawSurface = EGL14.EglGetCurrentSurface(EGL14.EglDraw);
			if (!EGL14.EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} failed.");
				}
			}
			return Disposable.Create(() =>
			{
				if (!EGL14.EglMakeCurrent(display, drawSurface, readSurface, glContext))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} failed.");
					}
				}
			});
		}
	}
}
#endif
