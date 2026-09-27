#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// Neutral Skia-on-Metal <see cref="ISwapChain"/> for macOS: the frame composes into a texture this context owns
/// and keeps, and <see cref="Present"/> blits that onto the window layer's drawable.
///
/// The drawable is deliberately acquired inside <see cref="Present"/> rather than before the frame: a CAMetalLayer
/// vends only three, so holding one across the frame's CPU work starves the pool and makes every later
/// <c>nextDrawable</c> block for ~1s and then return nil. Owning the target is also what lets this report
/// <see cref="PreservesContents"/>, so the compositor can repaint only the damaged region.
/// </summary>
internal sealed class MacOSMetalGraphicsContext : ISwapChain, IMetalDeviceContext
{
	private readonly nint _window;
	private readonly nint _device;
	private readonly nint _queue;
	private nint _texture;
	private MacOSMetalRenderTarget? _target;
	private int _width;
	private int _height;

	public MacOSMetalGraphicsContext(nint window, nint device, nint queue)
	{
		_window = window;
		_device = device;
		_queue = queue;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	/// <summary>The frame composes into a texture kept across frames, so last frame's pixels are still there.</summary>
	public bool PreservesContents => true;

	public nint Device => _device;
	public nint Queue => _queue;

	/// <summary>Whether the last <see cref="Present"/> reached the screen; false when the layer vended no drawable.</summary>
	internal bool LastPresentSucceeded { get; private set; }

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_texture == 0 || width != _width || height != _height)
		{
			ReleaseTexture();

			_texture = NativeUno.uno_window_create_render_texture(_window, width, height);
			_width = width;
			_height = height;
			_target = _texture == 0 ? null : new MacOSMetalRenderTarget(this, width, height);
		}

		return _target ?? throw new InvalidOperationException("Failed to allocate the Metal render texture.");
	}

	public void Present()
		=> LastPresentSucceeded = _texture != 0 && NativeUno.uno_window_present_texture(_window, _texture);

	public void Dispose() => ReleaseTexture();

	private void ReleaseTexture()
	{
		if (_texture != 0)
		{
			NativeUno.uno_window_release_texture(_texture);
			_texture = 0;
		}

		_target = null;
	}

	private sealed class MacOSMetalRenderTarget(MacOSMetalGraphicsContext owner, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => owner._texture;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}

/// <summary>
/// Neutral software (CPU-framebuffer) <see cref="ISwapChain"/> for macOS: owns a buffer and hands it to the backend as an
/// <see cref="ISoftwareRenderTarget"/>. The native <c>SoftDraw</c> callback reads it back, so <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class MacOSSoftwareGraphicsContext : ISwapChain
{
	private nint _buffer;
	private int _width;
	private int _height;
	private MacOSSoftwareRenderTarget? _target;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	// Reuses one persistent CPU buffer across frames (reallocated only on resize), so the compositor can repaint
	// only the damaged region.
	public bool PreservesContents => true;

	/// <summary>The buffer last handed to the backend, for the native SoftDraw callback to read back.</summary>
	internal ISoftwareRenderTarget? CurrentTarget => _target;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_target is null || width != _width || height != _height)
		{
			if (_buffer != 0)
			{
				Marshal.FreeHGlobal(_buffer);
			}
			_width = width;
			_height = height;
			_buffer = Marshal.AllocHGlobal(width * height * 4);
			_target = new MacOSSoftwareRenderTarget(_buffer, _width * 4, _width, _height);
		}

		return _target;
	}

	// The native SoftDraw callback blits the buffer to the window; nothing to present here.
	public void Present() { }

	public void Dispose()
	{
		if (_buffer != 0)
		{
			Marshal.FreeHGlobal(_buffer);
			_buffer = 0;
		}
	}

	private sealed class MacOSSoftwareRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}

