#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia;
using Uno.UI.Helpers;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL ES <see cref="ISwapChain"/> for X11: creates an EGL display/context/surface for the window
/// and hands the renderer a neutral <see cref="IGLRenderTarget"/> flagged GLES. <see cref="Present"/> swaps
/// buffers and releases current.
/// </summary>
internal sealed unsafe class X11EGLGraphicsContext : ISwapChain, IGLDeviceContext, IX11GpuTeardownContext
{
	private const uint DefaultFramebuffer = 0;

	private readonly X11Window _x11Window;
	private readonly IntPtr _eglDisplay;
	private readonly IntPtr _eglSurface;
	private readonly IntPtr _eglContext;
	private readonly int _samples;
	private readonly int _stencil;
	private X11EGLRenderTarget? _target;
	// Whether the compositor acquired a target this tick; it skips drawing entirely when there is no recorded
	// frame yet or the bounds are empty, and swapping then shows an uninitialised back buffer.
	private bool _frameAcquired;

	public X11EGLGraphicsContext(X11Window x11Window)
	{
		_x11Window = x11Window;

		_eglDisplay = EglHelper.EglGetDisplay(x11Window.Display);
		if (_eglDisplay == IntPtr.Zero)
		{
			throw new NotSupportedException($"EglGetDisplay failed: {Enum.GetName(EglHelper.EglGetError())}");
		}

		var w = x11Window.Window;
		(_eglSurface, _eglContext, _, _, _samples, _stencil) = EglHelper.InitializeGles2Context(_eglDisplay, new IntPtr(&w));
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public Func<string, nint> GetProcAddress => EglHelper.EglGetProcAddress;

	// SwapBuffers leaves the default framebuffer undefined, so the frame is composed into a retained FBO this host
	// owns and blitted over at present; that copy is what carries the previous frame forward.
	public bool PreservesContents => _contentsPreserved;

	private readonly GLRetainedFramebuffer _retained = new(EglHelper.EglGetProcAddress);
	private bool _contentsPreserved;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		var retained = _retained.TryResize(width, height, out _contentsPreserved);
		var id = retained ? _retained.FramebufferId : DefaultFramebuffer;
		if (_target is null || _target.Width != width || _target.Height != height || _target.FramebufferId != id)
		{
			// Single-sampled: Skia's coverage AA does not need the window's multisampling, and a multisampled
			// attachment would need resolving before the blit.
			_target = new X11EGLRenderTarget(width, height, retained ? 0 : _samples, retained ? _retained.StencilBits : _stencil, id);
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
		if (!EglHelper.EglSwapBuffers(_eglDisplay, _eglSurface))
		{
			this.LogError()?.Error("EglSwapBuffers failed.");
		}
		EglHelper.EglMakeCurrent(_eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
	}

	private void MakeCurrent()
	{
		if (!EglHelper.EglMakeCurrent(_eglDisplay, _eglSurface, _eglSurface, _eglContext))
		{
			this.LogError()?.Error("EglMakeCurrent failed for the OpenGL ES graphics context.");
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
		EglHelper.EglMakeCurrent(_eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		if (!EglHelper.EglTerminate(_eglDisplay))
		{
			this.LogError()?.Error("EglTerminate failed.");
		}
	}

	private sealed class X11EGLRenderTarget(int width, int height, int sampleCount, int stencilBits, uint framebufferId) : IGLRenderTarget
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
