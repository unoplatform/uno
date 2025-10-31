using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11EGLRenderer : X11Renderer, IDisposable
	{
		private const uint DefaultFramebuffer = 0; // this is the glX buffer that was created in X11XamlRootHost, which will directly render on screen

		private readonly GRContext _grContext;
		private readonly X11Window _x11Window;
		private readonly IntPtr _libGles;
		private readonly IntPtr _libEgl;

		private GRBackendRenderTarget? _renderTarget;
		private IntPtr _eglDisplay;
		private IntPtr _glContext;
		private IntPtr _eglSurface;
		private IDisposable? _contextCurrentDisposable;

		public X11EGLRenderer(IXamlRootHost host, X11Window x11window) : base(host, x11window)
		{
			_x11Window = x11window;
			_libGles = NativeLibrary.Load("libGLESv2.so.2", Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
			_libEgl = NativeLibrary.Load("libEGL.so.1", Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories);
			_grContext = CreateGRGLContext();
		}

		public void Dispose() => _grContext.Dispose();

		protected override SKSurface UpdateSize(int width, int height, int depth)
		{
			_renderTarget?.Dispose();

			var skColorType = SKColorType.Rgba8888; // this is Rgba8888 regardless of SKImageInfo.PlatformColorType
			var grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

			var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, skColorType.ToGlSizedFormat());

			var glXInfo = _x11Window.glXInfo!.Value;
			_renderTarget = new GRBackendRenderTarget(width, height, glXInfo.sampleCount, glXInfo.stencilBits, glInfo);
			return SKSurface.Create(_grContext, _renderTarget, grSurfaceOrigin, skColorType);
		}

		protected override void MakeCurrent()
		{
			var glContext = EglHelper.EglGetCurrentContext();
			var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
			var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
			if (!EglHelper.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			}
			_contextCurrentDisposable = Disposable.Create(() =>
			{
				if (!EglHelper.EglMakeCurrent(_eglDisplay, drawSurface, readSurface, glContext))
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
					}
				}
			});
		}

		protected override void Flush()
		{
			if (!EglHelper.EglSwapBuffers(_eglDisplay, _eglSurface))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglSwapBuffers)} failed.");
				}
			}
			Debug.Assert(_contextCurrentDisposable is not null);
			_contextCurrentDisposable?.Dispose();
			_contextCurrentDisposable = null;
		}

		private unsafe GRContext CreateGRGLContext()
		{
			using var lockDisposable = X11Helper.XLock(_x11Window.Display);
			_eglDisplay = EglHelper.EglGetDisplay(_x11Window.Display);
			EglHelper.EglInitialize(_eglDisplay, out var major, out var minor);
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Found EGL version {major}.{minor}.");
			}

			int[] attribList =
			{
				EglHelper.EGL_RED_SIZE, 8,
				EglHelper.EGL_GREEN_SIZE, 8,
				EglHelper.EGL_BLUE_SIZE, 8,
				EglHelper.EGL_ALPHA_SIZE, 8,
				EglHelper.EGL_DEPTH_SIZE, 8,
				EglHelper.EGL_STENCIL_SIZE, 1,
				EglHelper.EGL_RENDERABLE_TYPE, EglHelper.EGL_OPENGL_ES2_BIT,
				EglHelper.EGL_NONE
			};

			var configs = new IntPtr[1];
			var success = EglHelper.EglChooseConfig(_eglDisplay, attribList, configs, configs.Length, out var numConfig);

			if (!success || numConfig < 1)
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglChooseConfig)} failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			// ANGLE implements GLES 3
			_glContext = EglHelper.EglCreateContext(_eglDisplay, configs[0], EglHelper.EGL_NO_CONTEXT, [EglHelper.EGL_CONTEXT_CLIENT_VERSION, 2, EglHelper.EGL_NONE]);
			if (_glContext == IntPtr.Zero)
			{
				throw new InvalidOperationException($"EGL context creation failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			var window = _x11Window.Window;
			var windowPtr = new IntPtr(&window);
			_eglSurface = EglHelper.EglCreatePlatformWindowSurface(_eglDisplay, configs[0], windowPtr, [EglHelper.EGL_NONE]);
			MakeCurrent();

			var glInterface = GRGlInterface.CreateGles(EglHelper.EglGetProcAddress);

			if (glInterface == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system");
			}

			var context = GRContext.CreateGl(glInterface);

			if (context == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system (failed to create context)");
			}

			_contextCurrentDisposable?.Dispose();

			return context;
		}

		// private IntPtr GetMethodProc(string name)
		// {
		// 	IntPtr handle;
		// 	if (NativeLibrary.TryGetExport(_libGles, name, out handle) || NativeLibrary.TryGetExport(_libEgl, name, out handle))
		// 	{
		// 		return handle;
		// 	}
		// 	else
		// 	{
		// 		this.Log().Error($"EGL initialization: a symbol with name='{name}' not found in GLES or EGL.");
		// 		return IntPtr.Zero;
		// 	}
		// }

		// private IDisposable MakeCurrentPrivate()
		// {
		// 	var glContext = EglHelper.EglGetCurrentContext();
		// 	var display = EglHelper.EglGetCurrentDisplay();
		// 	var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
		// 	var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
		// 	if (!EglHelper.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _glContext))
		// 	{
		// 		if (this.Log().IsEnabled(LogLevel.Error))
		// 		{
		// 			this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
		// 		}
		// 	}
		// 	return Disposable.Create(() =>
		// 	{
		// 		if (!EglHelper.EglMakeCurrent(display, drawSurface, readSurface, glContext))
		// 		{
		// 			if (this.Log().IsEnabled(LogLevel.Error))
		// 			{
		// 				this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
		// 			}
		// 		}
		// 	});
		// }
	}
}
