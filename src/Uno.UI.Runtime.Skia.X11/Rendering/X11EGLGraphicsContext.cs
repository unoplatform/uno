#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Neutral OpenGL ES <see cref="ISwapChain"/> for X11: creates an EGL display/context/surface for the window
/// and hands the renderer a neutral <see cref="IGLRenderTarget"/> flagged GLES. <see cref="Present"/> swaps
/// buffers and releases current.
/// </summary>
internal sealed unsafe class X11EGLGraphicsContext : ISwapChain, IGLDeviceContext
{
	private const uint DefaultFramebuffer = 0;

	private readonly X11Window _x11Window;
	private readonly IntPtr _eglDisplay;
	private readonly IntPtr _eglSurface;
	private readonly IntPtr _eglContext;
	private readonly int _samples;
	private readonly int _stencil;
	private X11EGLRenderTarget? _target;

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

	// The renderer draws into the default framebuffer, which SwapBuffers leaves undefined — no retention yet, so the
	// compositor repaints the whole frame. (Host-owned FBO retention to restore partial repaint is a follow-up.)
	public bool PreservesContents => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		MakeCurrent();
		if (_target is null || _target.Width != width || _target.Height != height)
		{
			_target = new X11EGLRenderTarget(width, height, _samples, _stencil);
		}
		return _target;
	}

	public void Present()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
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

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_x11Window.Display);
		if (!EglHelper.EglTerminate(_eglDisplay))
		{
			this.LogError()?.Error("EglTerminate failed.");
		}
	}

	private sealed class X11EGLRenderTarget(int width, int height, int sampleCount, int stencilBits) : IGLRenderTarget
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
