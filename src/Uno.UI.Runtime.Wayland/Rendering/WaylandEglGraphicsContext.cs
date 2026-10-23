#nullable enable

using System;
using System.Globalization;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// OpenGL ES graphics context: an EGL window surface on a wl_egl_window. The frame is composed into a retained
/// framebuffer, blitted to the default framebuffer and swapped at present. The context is current on the owner
/// thread only between <see cref="AcquireRenderTarget"/> and <see cref="Present"/>.
/// </summary>
internal sealed class WaylandEglGraphicsContext : IWaylandGraphicsContext, IGLDeviceContext, IWaylandGpuTeardownContext
{
	private const int EglNone = 0x3038;
	private const int EglSurfaceType = 0x3033;
	private const int EglWindowBit = 4;
	private const int EglRenderableType = 0x3040;
	private const int EglGles2Bit = 4;
	private const int EglRedSize = 0x3024;
	private const int EglGreenSize = 0x3023;
	private const int EglBlueSize = 0x3022;
	private const int EglAlphaSize = 0x3021;
	private const int EglStencilSize = 0x3026;
	private const int EglSampleBuffers = 0x3032;
	private const int EglSamples = 0x3031;
	private const int EglMinSwapInterval = 0x303B;
	private const int EglContextClientVersion = 0x3098;
	private const int EglContextLost = 0x300E;
	private const uint EglGlesApi = 0x30A0;
	private const uint DefaultFramebuffer = 0;

	private readonly WaylandCommitGate _prepareCommit;
	private readonly GLRetainedFramebuffer _retained = new(WaylandRenderNative.eglGetProcAddress);
	private IntPtr _display;
	private IntPtr _window;
	private IntPtr _context;
	private IntPtr _eglSurface;
	private int _samples;
	private int _stencil;
	private int _windowWidth = 1;
	private int _windowHeight = 1;
	private bool _contentsPreserved;
	private bool _frameAcquired;
	private bool _surfaceless = true;
	private bool _windowBound;
	private bool _swapIntervalSet;
	private bool _disposed;
	private EglRenderTarget? _target;

	public WaylandEglGraphicsContext(IntPtr wlDisplay, WlSurface surface, WaylandCommitGate prepareCommit)
	{
		_prepareCommit = prepareCommit;
		try
		{
			_display = WaylandEglDisplay.Acquire(wlDisplay);
			Require(WaylandRenderNative.eglBindAPI(EglGlesApi) != 0, "eglBindAPI");
			int[] attributes = [EglSurfaceType, EglWindowBit, EglRenderableType, EglGles2Bit, EglRedSize, 8, EglGreenSize, 8, EglBlueSize, 8, EglAlphaSize, 8, EglStencilSize, 8, EglNone];
			Require(WaylandRenderNative.eglChooseConfig(_display, attributes, null, 0, out var count) != 0 && count > 0, "eglChooseConfig");
			var configs = new IntPtr[count];
			Require(WaylandRenderNative.eglChooseConfig(_display, attributes, configs, configs.Length, out count) != 0, "eglChooseConfig");
			var config = WaylandEglConfig.Select(configs.AsSpan(0, count), candidate =>
				WaylandRenderNative.eglGetConfigAttrib(_display, candidate, EglMinSwapInterval, out var interval) != 0 ? interval : null,
				candidate => WaylandRenderNative.eglGetConfigAttrib(_display, candidate, EglSampleBuffers, out var buffers) != 0 ? buffers : null,
				candidate => WaylandRenderNative.eglGetConfigAttrib(_display, candidate, EglSamples, out var samples) != 0 ? samples : null);
			Require(WaylandRenderNative.eglGetConfigAttrib(_display, config, EglSamples, out _samples) != 0, "EGL_SAMPLES");
			Require(WaylandRenderNative.eglGetConfigAttrib(_display, config, EglStencilSize, out _stencil) != 0, "EGL_STENCIL_SIZE");
			_window = WaylandRenderNative.wl_egl_window_create(surface.Handle, _windowWidth, _windowHeight);
			Require(_window != IntPtr.Zero, "wl_egl_window_create");
			_context = WaylandRenderNative.eglCreateContext(_display, config, IntPtr.Zero, [EglContextClientVersion, 2, EglNone]);
			Require(_context != IntPtr.Zero, "eglCreateContext");
			_eglSurface = WaylandRenderNative.eglCreateWindowSurface(_display, config, _window, [EglNone]);
			Require(_eglSurface != IntPtr.Zero, "eglCreateWindowSurface");
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	/// <summary>Raised once on the owner thread when the GPU reset the context; it cannot render anymore.</summary>
	public event EventHandler? ContextLost;

	public bool IsLost { get; private set; }

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public Func<string, nint> GetProcAddress => WaylandRenderNative.eglGetProcAddress;

	// A swap leaves the default framebuffer undefined; the retained framebuffer carries the previous frame forward.
	public bool PreservesContents => _contentsPreserved;

	public bool Committed { get; private set; }

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		if (width != _windowWidth || height != _windowHeight)
		{
			WaylandRenderNative.wl_egl_window_resize(_window, width, height, 0, 0);
			_windowWidth = width;
			_windowHeight = height;
		}
		MakeCurrent(window: false);
		var retained = _retained.TryResize(width, height, out _contentsPreserved);
		if (!retained)
		{
			// Without a retained framebuffer the frame is drawn straight into the window's back buffer.
			MakeCurrent(window: true);
		}
		var id = retained ? _retained.FramebufferId : DefaultFramebuffer;
		if (_target is null || _target.Width != width || _target.Height != height || _target.FramebufferId != id)
		{
			// Single-sampled when retained: the blit cannot resolve a multisampled attachment.
			_target = new EglRenderTarget(width, height, retained ? 0 : _samples, retained ? _retained.StencilBits : _stencil, id);
		}
		_frameAcquired = true;
		return _target;
	}

	public void Present()
	{
		Committed = false;
		if (!_frameAcquired)
		{
			return;
		}
		_frameAcquired = false;
		try
		{
			Committed = _prepareCommit(_windowWidth, _windowHeight, () =>
			{
				MakeCurrent(window: true);
				_retained.BlitToDefault();
				if (WaylandRenderNative.eglSwapBuffers(_display, _eglSurface) != 0)
				{
					return true;
				}
				OnFailure("eglSwapBuffers");
				return false;
			});
		}
		finally
		{
			_ = WaylandRenderNative.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			_windowBound = false;
		}
	}

	public bool TryMakeCurrentForTeardown()
		=> !IsLost && _eglSurface != IntPtr.Zero && WaylandRenderNative.eglMakeCurrent(_display, _eglSurface, _eglSurface, _context) != 0;

	// Binding the window surface makes Mesa take a back buffer, and a resize only applies to the next one: a frame
	// that is drawn but not committed would then hold an old-size buffer for the next commit. So the window is bound
	// only to blit and swap, and drawing into the retained framebuffer runs surfaceless when EGL supports it.
	private void MakeCurrent(bool window)
	{
		if (_windowBound && window)
		{
			return;
		}
		if (!window && _surfaceless)
		{
			if (WaylandRenderNative.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, _context) != 0)
			{
				_windowBound = false;
				return;
			}
			_surfaceless = false;
		}
		if (WaylandRenderNative.eglMakeCurrent(_display, _eglSurface, _eglSurface, _context) == 0)
		{
			throw new InvalidOperationException(OnFailure("eglMakeCurrent"));
		}
		_windowBound = true;
		if (!_swapIntervalSet)
		{
			// Frame callbacks pace presentation; a swap interval would make eglSwapBuffers block while the surface is hidden.
			Require(WaylandRenderNative.eglSwapInterval(_display, 0) != 0, "eglSwapInterval");
			_swapIntervalSet = true;
		}
	}

	private string OnFailure(string operation)
	{
		var error = WaylandRenderNative.eglGetError();
		var message = string.Create(CultureInfo.InvariantCulture, $"{operation} failed: 0x{error:X}.");
		if (error == EglContextLost)
		{
			if (!IsLost)
			{
				IsLost = true;
				ContextLost?.Invoke(this, EventArgs.Empty);
			}
		}
		else if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error(message);
		}
		return message;
	}

	private static void Require(bool success, string operation)
	{
		if (!success)
		{
			throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"{operation} failed: 0x{WaylandRenderNative.eglGetError():X}."));
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			// The GL objects belong to this context, so they have to go while it is still current.
			if (_display != IntPtr.Zero && _context != IntPtr.Zero && TryMakeCurrentForTeardown())
			{
				_retained.Dispose();
			}
		}
		finally
		{
			if (_display != IntPtr.Zero)
			{
				_ = WaylandRenderNative.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (_eglSurface != IntPtr.Zero)
				{
					_ = WaylandRenderNative.eglDestroySurface(_display, _eglSurface);
				}
				if (_context != IntPtr.Zero)
				{
					_ = WaylandRenderNative.eglDestroyContext(_display, _context);
				}
			}
			if (_window != IntPtr.Zero)
			{
				WaylandRenderNative.wl_egl_window_destroy(_window);
			}
			if (_display != IntPtr.Zero)
			{
				WaylandEglDisplay.Release(_display);
				_display = IntPtr.Zero;
			}
		}
	}

	private sealed class EglRenderTarget(int width, int height, int sampleCount, int stencilBits, uint framebufferId) : IGLRenderTarget
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
