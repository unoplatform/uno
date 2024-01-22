using System;
using System.Drawing;
using System.Globalization;
using SkiaSharp;
using Uno.Foundation.Logging;
using Avalonia.X11;
using Avalonia.X11.Glx;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	// https://github.com/gamedevtech/X11OpenGLWindow
	// https://learnopengl.com/Advanced-OpenGL/Framebuffers
	internal class X11OpenGLRenderer : IX11Renderer, IDisposable
	{
		private const uint DefaultFramebuffer = 0; // this is the glX buffer that was created in X11XamlRootHost, which will directly render on screen

		private readonly GRContext _grContext;
		private readonly X11Window _x11Window;
		private readonly IXamlRootHost _host;

		private int _renderCount;
		private Size _lastSize;
		private GRBackendRenderTarget? _renderTarget;
		private SKSurface? _surface;

		public X11OpenGLRenderer(IXamlRootHost host, X11Window x11window)
		{
			_host = host;
			_x11Window = x11window;
			_grContext = CreateGRGLContext();
		}

		public void Dispose() => _grContext?.Dispose();

		void IX11Renderer.InvalidateRender()
		{
			using var _1 = X11Helper.XLock(_x11Window.Display);

			this.Log().Trace($"Render {_renderCount++}");

			if (_x11Window.glXInfo is not { } glXInfo)
			{
				this.Log().Error($"No glX information associated with this OpenGL renderer, so it cannot be used.");
				return;
			}

			if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, glXInfo.context))
			{
				this.Log().Error($"glXMakeCurrent failed for renderCount {_renderCount} and Window {_x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
				return;
			}

			XWindowAttributes attributes = default;
			var _2 = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attributes);

			var width = attributes.width;
			var height = attributes.height;

			if (_surface == null || _renderTarget == null || _lastSize != new Size(width, height))
			{
				_renderTarget?.Dispose();
				_surface?.Dispose();

				var skColorType = SKColorType.Rgba8888; // this is Rgba8888 regardless of SKImageInfo.PlatformColorType
				var grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

				var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, skColorType.ToGlSizedFormat());

				_renderTarget = new GRBackendRenderTarget(width, height, glXInfo.sampleCount, glXInfo.stencilBits, glInfo);
				_surface = SKSurface.Create(_grContext, _renderTarget, grSurfaceOrigin, skColorType);
				_lastSize = new Size(width, height);
			}

			var canvas = _surface.Canvas;
			using (new SKAutoCanvasRestore(canvas, true))
			{
				canvas.Clear(SKColors.Transparent);

				if (_host.RootElement?.Visual is { } rootVisual)
				{
					_host.RootElement.XamlRoot!.Compositor.RenderRootVisual(_surface, rootVisual);
				}
			}

			_surface.Canvas.Flush();

			GlxInterface.glXSwapBuffers(_x11Window.Display, _x11Window.Window);

			var _3 = XLib.XFlush(_x11Window.Display); // unnecessary on most X11 implementations
		}

		private GRContext CreateGRGLContext()
		{
			if (_x11Window.glXInfo is not { } glXInfo)
			{
				throw new NotSupportedException($"No glX information associated with this OpenGL renderer, so it cannot be used.");
			}

			if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, glXInfo.context))
			{
				throw new NotSupportedException($"glXMakeCurrent failed for Window {_x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
			}

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

			return context;
		}
	}
}
