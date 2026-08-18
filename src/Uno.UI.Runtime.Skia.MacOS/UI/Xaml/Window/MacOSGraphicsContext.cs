#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// A context that consumes the per-frame <c>MTLTexture</c> the native draw callback supplies (Skia-on-Metal),
/// as opposed to a swapchain-owning context (WebGPU) that sources its own drawable.
/// </summary>
internal interface IMacOSNativeTextureSink
{
	/// <summary>Pushes the texture for the frame about to be acquired.</summary>
	void SetCurrentTexture(nint texture);
}

/// <summary>
/// Neutral Skia-on-Metal <see cref="ISwapChain"/> for macOS: wraps the per-frame native <c>MTLTexture</c> as an
/// <see cref="IMetalRenderTarget"/>. The native MTKView owns the drawable and commits, so <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class MacOSMetalGraphicsContext : ISwapChain, IMacOSNativeTextureSink, IMetalDeviceContext
{
	private readonly nint _device;
	private readonly nint _queue;
	private nint _currentTexture;
	private MacOSMetalRenderTarget? _target;

	public MacOSMetalGraphicsContext(nint device, nint queue)
	{
		_device = device;
		_queue = queue;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	// The native MTKView presents to a per-frame drawable with no host-retained surface, so the back buffer is
	// undefined each frame and the compositor repaints the whole frame.
	public bool PreservesContents => false;

	public nint Device => _device;
	public nint Queue => _queue;

	public void SetCurrentTexture(nint texture) => _currentTexture = texture;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		if (_target is null || _target.Width != width || _target.Height != height)
		{
			_target = new MacOSMetalRenderTarget(this, width, height);
		}
		return _target;
	}

	// The native MTKView owns the drawable and commits after drawInMTKView returns.
	public void Present() { }

	public void Dispose() { }

	// Reads the per-frame texture live off the context, so the cached target reflects each SetCurrentTexture swap.
	private sealed class MacOSMetalRenderTarget(MacOSMetalGraphicsContext owner, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => owner._currentTexture;
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

