#nullable enable

using System;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL <see cref="IGraphicsContext"/> for X11: wraps the window's GLX context in a Skia GRContext-GL
/// and hands the renderer an SKCanvas-backed target; <see cref="Present"/> flushes and swaps buffers. Chosen by
/// negotiation when the window was created with a GLX visual (see X11XamlRootHost.CreateGLXWindow); the software
/// context is the fallback. (Intermediate: the SKSurface is built here rather than via a neutral GL render target
/// + backend GRContext — that neutralization is a follow-up.)
/// </summary>
internal sealed class X11OpenGLGraphicsContext : IGraphicsContext
{
	private const uint DefaultFramebuffer = 0; // the GLX buffer created in X11XamlRootHost, rendered directly on screen

	private readonly X11Window _x11Window;
	private readonly GRContext _grContext;
	private readonly GRGlInterface _glInterface;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private int _width;
	private int _height;

	public X11OpenGLGraphicsContext(X11Window x11Window)
	{
		if (x11Window.glXInfo is null)
		{
			throw new NotSupportedException("The window has no GLX context; the OpenGL graphics context cannot be created.");
		}

		_x11Window = x11Window;

		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		_glInterface = GRGlInterface.Create() ?? throw new NotSupportedException("OpenGL is not supported on this system.");
		_grContext = GRContext.CreateGl(_glInterface) ?? throw new NotSupportedException("Failed to create an OpenGL GRContext.");
		ReleaseCurrent();
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGL;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();

		if (_surface is null || width != _width || height != _height)
		{
			_width = width;
			_height = height;
			_renderTarget?.Dispose();
			_surface?.Dispose();

			var glXInfo = _x11Window.glXInfo!.Value;
			var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, SKColorType.Rgba8888.ToGlSizedFormat());
			_renderTarget = new GRBackendRenderTarget(width, height, glXInfo.sampleCount, glXInfo.stencilBits, glInfo);
			// BottomLeft to match OpenGL's origin (as in X11OpenGLRenderer).
			_surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
		}

		return new SkiaRenderTarget(_surface!.Canvas);
	}

	public void Present()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		_grContext.Flush();
		GlxInterface.glXSwapBuffers(_x11Window.Display, _x11Window.Window);
		ReleaseCurrent();
	}

	private void MakeCurrent()
	{
		if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, _x11Window.glXInfo!.Value.context))
		{
			this.LogError()?.Error("glXMakeCurrent failed for the OpenGL graphics context.");
		}
	}

	private void ReleaseCurrent()
		=> GlxInterface.glXMakeCurrent(_x11Window.Display, X11Helper.None, IntPtr.Zero);

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		_surface?.Dispose();
		_renderTarget?.Dispose();
		_grContext.Dispose();
		_glInterface.Dispose();
		ReleaseCurrent();
	}
}
