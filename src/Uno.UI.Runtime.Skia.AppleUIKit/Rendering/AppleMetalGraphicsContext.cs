#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// A context that consumes the per-frame <c>MTLTexture</c> the MTKView supplies (Skia-on-Metal), rather than a
/// swapchain-owning context that sources its own drawable.
/// </summary>
internal interface IAppleNativeTextureSink
{
	/// <summary>Pushes the texture for the frame about to be acquired.</summary>
	void SetCurrentTexture(nint texture);
}

/// <summary>
/// Neutral Skia-on-Metal <see cref="ISwapChain"/> for AppleUIKit: holds the MTKView's device/queue and wraps the
/// per-frame drawable <c>MTLTexture</c> as an <see cref="IMetalRenderTarget"/>. The MTKView owns and presents the
/// drawable, so <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class AppleMetalGraphicsContext : ISwapChain, IAppleNativeTextureSink, IMetalDeviceContext
{
	private readonly nint _device;
	private readonly nint _queue;
	private nint _currentTexture;

	public AppleMetalGraphicsContext(nint device, nint queue)
	{
		_device = device;
		_queue = queue;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	public nint Device => _device;
	public nint Queue => _queue;

	public void SetCurrentTexture(nint texture) => _currentTexture = texture;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> new AppleMetalRenderTarget(_currentTexture, Math.Max(1, width), Math.Max(1, height));

	// The MTKView owns the drawable and commits after its Draw returns.
	public void Present() { }

	public void Dispose() { }

	private sealed class AppleMetalRenderTarget(nint texture, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => texture;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		// Retained-layer partial repaint: the backend blits a persistent layer onto this frame's drawable each present.
		public bool PreservesContents => true;
		public void Dispose() { }
	}
}
