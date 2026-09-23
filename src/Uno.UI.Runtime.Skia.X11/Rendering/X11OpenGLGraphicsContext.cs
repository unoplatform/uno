#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL <see cref="ISwapChain"/> for X11: makes the window's GLX context current and hands the
/// renderer a neutral <see cref="IGLRenderTarget"/>. <see cref="Present"/> swaps buffers and releases current.
/// </summary>
internal sealed class X11OpenGLGraphicsContext : ISwapChain, IGLDeviceContext, IX11GpuTeardownContext
{
	private const uint DefaultFramebuffer = 0; // the GLX buffer created in X11XamlRootHost, rendered directly on screen

	private readonly X11Window _x11Window;
	private X11GLRenderTarget? _target;
	// Whether the compositor acquired a target this tick; it skips drawing entirely when there is no recorded
	// frame yet or the bounds are empty, and swapping then shows an uninitialised back buffer.
	private bool _frameAcquired;

	public X11OpenGLGraphicsContext(X11Window x11Window)
	{
		if (x11Window.glXInfo is null)
		{
			throw new NotSupportedException("The window has no GLX context; the OpenGL graphics context cannot be created.");
		}

		_x11Window = x11Window;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGL;

	public Func<string, nint> GetProcAddress => X11NativeOpenGLWrapper.GetProcAddressStatic;

	// SwapBuffers leaves the default framebuffer undefined, so the frame is composed into a retained FBO this host
	// owns and blitted over at present; that copy is what carries the previous frame forward.
	public bool PreservesContents => _contentsPreserved;

	private readonly GLRetainedFramebuffer _retained = new(X11NativeOpenGLWrapper.GetProcAddressStatic);
	private bool _contentsPreserved;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		var glXInfo = _x11Window.glXInfo!.Value;
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		var retained = _retained.TryResize(width, height, out _contentsPreserved);
		var id = retained ? _retained.FramebufferId : DefaultFramebuffer;
		if (_target is null || _target.Width != width || _target.Height != height || _target.FramebufferId != id)
		{
			// Single-sampled: Skia's coverage AA does not need the window's multisampling, and a multisampled
			// attachment would need resolving before the blit.
			_target = new X11GLRenderTarget(width, height, retained ? 0 : glXInfo.sampleCount, retained ? _retained.StencilBits : glXInfo.stencilBits, id);
		}
		_frameAcquired = true;
		return _target;
	}

	public void Present()
	{
		if (!_frameAcquired)
		{
			return;
		}
		_frameAcquired = false;

		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		_retained.BlitToDefault();
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

	public void MakeCurrentForTeardown()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
	}

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		// The GL objects belong to this context, so they have to go while it is still current.
		MakeCurrent();
		_retained.Dispose();
		GlxInterface.glXMakeCurrent(_x11Window.Display, X11Helper.None, IntPtr.Zero);
	}

	private sealed class X11GLRenderTarget(int width, int height, int sampleCount, int stencilBits, uint framebufferId) : IGLRenderTarget
	{
		public uint FramebufferId => framebufferId;
		public int SampleCount => sampleCount;
		public int StencilBits => stencilBits;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}
