#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// A context that consumes the per-frame <c>MTLTexture</c> the native draw callback supplies (Skia-on-Metal),
/// as opposed to a swapchain-owning context (WebGPU on the <c>CAMetalLayer</c>) that sources its own drawable.
/// The host uses this seam to decide whether the native draw provides a texture or is switched to tick-only.
/// </summary>
internal interface IMacOSNativeTextureSink
{
	/// <summary>Pushes the texture for the frame about to be acquired.</summary>
	void SetCurrentTexture(nint texture);
}

/// <summary>
/// Neutral Skia-on-Metal <see cref="ISwapChain"/> for macOS — holds the device/queue and wraps the
/// per-frame native <c>MTLTexture</c> as an <see cref="IMetalRenderTarget"/>; the Skia backend builds its
/// GRContext-Metal + surface and flushes. The native MTKView owns the drawable and commits, so
/// <see cref="Present"/> is a no-op. Names no Skia type.
/// </summary>
internal sealed class MacOSMetalGraphicsContext : ISwapChain, IMacOSNativeTextureSink, IMetalDeviceContext
{
	private readonly nint _device;
	private readonly nint _queue;
	private nint _currentTexture;

	public MacOSMetalGraphicsContext(nint device, nint queue)
	{
		_device = device;
		_queue = queue;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	public nint Device => _device;
	public nint Queue => _queue;

	public void SetCurrentTexture(nint texture) => _currentTexture = texture;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> new MacOSMetalRenderTarget(_currentTexture, Math.Max(1, width), Math.Max(1, height));

	// The native MTKView owns the drawable and commits after drawInMTKView returns.
	public void Present() { }

	public void Dispose() { }

	private sealed class MacOSMetalRenderTarget(nint texture, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => texture;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}

/// <summary>
/// Neutral software (CPU-framebuffer) <see cref="ISwapChain"/> for macOS — owns a BGRA/RGBA buffer and
/// hands it to the backend as an <see cref="ISoftwareRenderTarget"/>; the Skia backend wraps it as its surface.
/// The native <c>SoftDraw</c> callback reads the rendered buffer back out of the acquired target, so
/// <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class MacOSSoftwareGraphicsContext : ISwapChain
{
	private nint _buffer;
	private int _width;
	private int _height;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_buffer == 0 || width != _width || height != _height)
		{
			if (_buffer != 0)
			{
				Marshal.FreeHGlobal(_buffer);
			}
			_width = width;
			_height = height;
			_buffer = Marshal.AllocHGlobal(width * height * 4);
		}

		return new MacOSSoftwareRenderTarget(_buffer, _width * 4, _width, _height);
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

