using System;
using System.Drawing;
using System.Globalization;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11
{
	// https://github.com/gamedevtech/X11OpenGLWindow/blob/4a3d55bb7aafd135670947f71bd2a3ee691d3fb3/README.md
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
		private X11AirspaceRenderHelper? _airspaceHelper;
		private SKColor _background = SKColors.White;

		public void SetBackgroundColor(SKColor color) => _background = color;

		public X11OpenGLRenderer(IXamlRootHost host, X11Window x11window)
		{
			_host = host;
			_x11Window = x11window;
			_grContext = CreateGRGLContext();
		}

		public void Dispose() => _grContext.Dispose();

		void IX11Renderer.Render()
		{
			using var lockDiposable = X11Helper.XLock(_x11Window.Display);

			if (_host is X11XamlRootHost { Closed.IsCompleted: true })
			{
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render {_renderCount++}");
			}

			if (_x11Window.glXInfo is not { } glXInfo)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"No glX information associated with this OpenGL renderer, so it cannot be used.");
				}
				return;
			}

			if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, glXInfo.context))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"glXMakeCurrent failed for renderCount {_renderCount} and Window {_x11Window.Window.GetHashCode().ToString("X", CultureInfo.InvariantCulture)}");
				}
				return;
			}

			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attributes);

			var width = attributes.width;
			var height = attributes.height;

			if (_surface == null || _airspaceHelper == null || _renderTarget == null || _lastSize != new Size(width, height))
			{
				_renderTarget?.Dispose();
				_surface?.Dispose();
				_airspaceHelper?.Dispose();

				var skColorType = SKColorType.Rgba8888; // this is Rgba8888 regardless of SKImageInfo.PlatformColorType
				var grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

				var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, skColorType.ToGlSizedFormat());

				_renderTarget = new GRBackendRenderTarget(width, height, glXInfo.sampleCount, glXInfo.stencilBits, glInfo);
				_surface = SKSurface.Create(_grContext, _renderTarget, grSurfaceOrigin, skColorType);
				_airspaceHelper = new X11AirspaceRenderHelper(_x11Window.Display, _x11Window.Window, width, height);
				_lastSize = new Size(width, height);
			}

			var canvas = _surface.Canvas;
			using (new SKAutoCanvasRestore(canvas, true))
			{
				canvas.Clear(_background);

				var scale = _host.RootElement?.XamlRoot is { } root
					? root.RasterizationScale
					: 1;
				_surface.Canvas.Scale((float)scale);

				if (_host.RootElement?.Visual is { } rootVisual)
				{
					var path = SkiaRenderHelper.RenderRootVisualAndReturnPath(width, height, rootVisual, _surface);
					if (path is { })
					{
						_airspaceHelper.XShapeClip(path);
					}
				}
			}

			_surface.Canvas.Flush();

			GlxInterface.glXSwapBuffers(_x11Window.Display, _x11Window.Window);

			_ = XLib.XFlush(_x11Window.Display); // unnecessary on most X11 implementations
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
