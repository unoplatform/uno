#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL <see cref="ISwapChain"/> for X11: makes the window's GLX context current and hands the
/// renderer a neutral <see cref="IGLRenderTarget"/>. <see cref="Present"/> swaps buffers and releases current.
/// </summary>
internal sealed class X11OpenGLGraphicsContext : ISwapChain, IGLDeviceContext
{
	private const uint DefaultFramebuffer = 0; // the GLX buffer created in X11XamlRootHost, rendered directly on screen

	private readonly X11Window _x11Window;
	private X11GLRenderTarget? _target;

	public X11OpenGLGraphicsContext(X11Window x11Window)
	{
		if (x11Window.glXInfo is null)
		{
			throw new NotSupportedException("The window has no GLX context; the OpenGL graphics context cannot be created.");
		}

		_x11Window = x11Window;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGL;

	public GLFlavor Flavor => GLFlavor.OpenGL;
	public Func<string, nint> GetProcAddress => X11NativeOpenGLWrapper.GetProcAddressStatic;

	// The renderer draws into the default framebuffer, which SwapBuffers leaves undefined — no retention yet, so the
	// compositor repaints the whole frame. (Host-owned FBO retention to restore partial repaint is a follow-up.)
	public bool PreservesContents => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		var glXInfo = _x11Window.glXInfo!.Value;
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		if (_target is null || _target.Width != width || _target.Height != height)
		{
			_target = new X11GLRenderTarget(width, height, glXInfo.sampleCount, glXInfo.stencilBits);
		}
		return _target;
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
