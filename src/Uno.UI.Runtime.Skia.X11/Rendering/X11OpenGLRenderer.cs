using System;
using System.Globalization;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	// https://github.com/gamedevtech/X11OpenGLWindow/blob/4a3d55bb7aafd135670947f71bd2a3ee691d3fb3/README.md
	// https://learnopengl.com/Advanced-OpenGL/Framebuffers
	internal class X11OpenGLRenderer : X11Renderer, IDisposable
	{
		private const uint DefaultFramebuffer = 0; // this is the glX buffer that was created in X11XamlRootHost, which will directly render on screen

		private readonly GRContext _grContext;
		private readonly GRGlInterface _glInterface;

		private GRBackendRenderTarget? _renderTarget;

		public X11OpenGLRenderer(IXamlRootHost host, X11Window x11window) : base(host, x11window)
		{
			(_glInterface, _grContext) = CreateGRGLContext();
		}

		public override void Dispose()
		{
			using var lockDisposable = X11Helper.XLock(_x11Window.Display);
			MakeCurrent();
			using var makeCurrentDisposable = new DisposableStruct<X11Window>(static x11Window =>
			{
				if (!GlxInterface.glXMakeCurrent(x11Window.Display, X11Helper.None, IntPtr.Zero))
				{
					throw new NotSupportedException($"glXMakeCurrent failed for Window {x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
				}
			}, _x11Window);

			_grContext.Dispose();
			_glInterface.Dispose();
		}

		protected override SKSurface UpdateSize(int width, int height)
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
			if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, _x11Window.glXInfo!.Value.context))
			{
				this.LogError()?.Error($"glXMakeCurrent failed for Window {_x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
			}
		}

		protected override void Flush()
		{
			GlxInterface.glXSwapBuffers(_x11Window.Display, _x11Window.Window);
			if (!GlxInterface.glXMakeCurrent(_x11Window.Display, X11Helper.None, IntPtr.Zero))
			{
				throw new NotSupportedException($"glXMakeCurrent failed for Window {_x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
			}
		}

		private unsafe (GRGlInterface, GRContext) CreateGRGLContext()
		{
			if (_x11Window.glXInfo is not { } glXInfo)
			{
				throw new NotSupportedException($"No glX information associated with this OpenGL renderer, so it cannot be used.");
			}

			MakeCurrent();
			using var makeCurrentDisposable = new DisposableStruct<X11Window>(static x11Window =>
			{
				if (!GlxInterface.glXMakeCurrent(x11Window.Display, X11Helper.None, IntPtr.Zero))
				{
					throw new NotSupportedException($"glXMakeCurrent failed for Window {x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
				}
			}, _x11Window);

			var glInterface = GRGlInterface.Create();

			if (glInterface == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system");
			}

			var context = GRContext.CreateGl(glInterface);

			if (context == null)
			{
				throw new NotSupportedException($"OpenGL is not supported in this system (failed to create context)");
			}

			var glGetString = (delegate* unmanaged[Cdecl]<int, byte*>)GlxInterface.glXGetProcAddress("glGetString");

			var glVersionBytePtr = glGetString(/* GL_VERSION */ 0x1F02);
			var glVersionString = Marshal.PtrToStringUTF8((IntPtr)glVersionBytePtr);

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"Using OpenGL {glVersionString} for rendering.");
			}

			return (glInterface, context);
		}
	}
}
