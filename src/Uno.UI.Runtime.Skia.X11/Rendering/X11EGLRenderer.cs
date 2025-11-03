using System;
using System.Diagnostics;
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
		private readonly IntPtr _eglDisplay;
		private readonly IntPtr _glContext;
		private readonly IntPtr _eglSurface;
		private readonly int _samples;
		private readonly int _stencil;

		private GRBackendRenderTarget? _renderTarget;
		private IDisposable? _contextCurrentDisposable;

		public unsafe X11EGLRenderer(IXamlRootHost host, X11Window x11window) : base(host, x11window)
		{
			_eglDisplay = EglHelper.EglGetDisplay(x11window.Display);
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

			if (!EglHelper.EglGetConfigAttrib(_eglDisplay, configs[0], EglHelper.EGL_SAMPLES, out _samples))
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglGetConfigAttrib)} failed to get {nameof(EglHelper.EGL_SAMPLES)}: {Enum.GetName(EglHelper.EglGetError())}");
			}
			if (!EglHelper.EglGetConfigAttrib(_eglDisplay, configs[0], EglHelper.EGL_STENCIL_SIZE, out _stencil))
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglGetConfigAttrib)} failed to get {nameof(EglHelper.EGL_STENCIL_SIZE)}: {Enum.GetName(EglHelper.EglGetError())}");
			}

			// ANGLE implements GLES 3
			_glContext = EglHelper.EglCreateContext(_eglDisplay, configs[0], EglHelper.EGL_NO_CONTEXT, [EglHelper.EGL_CONTEXT_CLIENT_VERSION, 2, EglHelper.EGL_NONE]);
			if (_glContext == IntPtr.Zero)
			{
				throw new InvalidOperationException($"EGL context creation failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			var window = x11window.Window;
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

			var glGetString = (delegate* unmanaged[Cdecl]<int, byte*>)EglHelper.EglGetProcAddress("glGetString");

			var glVersionBytePtr = glGetString(/* GL_VERSION */ 0x1F02);
			var glVersionString = Marshal.PtrToStringUTF8((IntPtr)glVersionBytePtr);

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Using {glVersionString} for rendering.");
			}

			_contextCurrentDisposable!.Dispose();

			_grContext = context;
		}

		public void Dispose() => _grContext.Dispose();

		protected override SKSurface UpdateSize(int width, int height, int depth)
		{
			_renderTarget?.Dispose();

			var skColorType = SKColorType.Rgba8888; // this is Rgba8888 regardless of SKImageInfo.PlatformColorType
			var grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

			var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, skColorType.ToGlSizedFormat());

			_renderTarget = new GRBackendRenderTarget(width, height, _samples, _stencil, glInfo);
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
	}
}
