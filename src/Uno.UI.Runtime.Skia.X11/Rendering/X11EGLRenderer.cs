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

		private readonly GRGlInterface _glInterface;
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
			if (_eglDisplay == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(EglHelper.EglGetDisplay)} failed: {Enum.GetName(EglHelper.EglGetError())}");
			}

			var w = x11window.Window;
			(_eglSurface, _glContext, var major, var minor, _samples, _stencil)
				= EglHelper.InitializeGles2Context(_eglDisplay, new IntPtr(&w));

			this.LogInfo()?.Info($"Found EGL version {major}.{minor}.");

			MakeCurrent();

			this.LogInfo()?.Info($"Using {EglHelper.GetGlVersionString()} for rendering.");

			_glInterface = GRGlInterface.CreateGles(EglHelper.EglGetProcAddress);

			if (_glInterface == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system");
			}

			_grContext = GRContext.CreateGl(_glInterface);

			if (_grContext == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system (failed to create context)");
			}

			_contextCurrentDisposable!.Dispose();
		}

		public override void Dispose()
		{
			using var lockDisposable = X11Helper.XLock(_x11Window.Display);
			MakeCurrent();
			_grContext.Dispose();
			_glInterface.Dispose();
			if (!EglHelper.EglTerminate(_eglDisplay))
			{
				this.LogError()?.Error($"{nameof(EglHelper.EglTerminate)} failed.");
			}
			_contextCurrentDisposable!.Dispose();
		}

		protected override SKSurface UpdateSize(int width, int height)
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
