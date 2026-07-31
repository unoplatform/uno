#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL <see cref="IGraphicsContext"/> for X11 — names no GPU-library type. It makes the window's GLX
/// context current and hands the renderer a neutral <see cref="IGLRenderTarget"/> (the window's default
/// framebuffer + sample/stencil); the Skia backend builds its GRContext-GL against the current context.
/// <see cref="Present"/> swaps buffers and releases current. Chosen by negotiation when the window was created
/// with a GLX visual (see X11XamlRootHost.CreateGLXWindow); the software context is the fallback.
/// </summary>
internal sealed class X11OpenGLGraphicsContext : IGraphicsContext
{
	private const uint DefaultFramebuffer = 0; // the GLX buffer created in X11XamlRootHost, rendered directly on screen

	private readonly X11Window _x11Window;

	public X11OpenGLGraphicsContext(X11Window x11Window)
	{
		if (x11Window.glXInfo is null)
		{
			throw new NotSupportedException("The window has no GLX context; the OpenGL graphics context cannot be created.");
		}

		_x11Window = x11Window;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGL;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		var glXInfo = _x11Window.glXInfo!.Value;
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		return new X11GLRenderTarget(Math.Max(1, width), Math.Max(1, height), glXInfo.sampleCount, glXInfo.stencilBits);
	}

	public void Present()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		GlxInterface.glXSwapBuffers(_x11Window.Display, _x11Window.Window);
		GlxInterface.glXMakeCurrent(_x11Window.Display, X11Helper.None, IntPtr.Zero);
	}

	private void MakeCurrent()
	{
		if (!GlxInterface.glXMakeCurrent(_x11Window.Display, _x11Window.Window, _x11Window.glXInfo!.Value.context))
		{
			this.LogError()?.Error("glXMakeCurrent failed for the OpenGL graphics context.");
		}
	}

	public void Dispose() { }

	private sealed class X11GLRenderTarget(int width, int height, int sampleCount, int stencilBits) : IGLRenderTarget
	{
		public uint FramebufferId => DefaultFramebuffer;
		public int SampleCount => sampleCount;
		public int StencilBits => stencilBits;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}
