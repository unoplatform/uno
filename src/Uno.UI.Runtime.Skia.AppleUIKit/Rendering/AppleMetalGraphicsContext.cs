#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// A context that consumes the per-frame <c>MTLTexture</c> the MTKView supplies (Skia-on-Metal), as opposed to a
/// swapchain-owning context (WebGPU on the <c>CAMetalLayer</c>) that sources its own drawable. Mirrors the macOS
/// host's <c>IMacOSNativeTextureSink</c>.
/// </summary>
internal interface IAppleNativeTextureSink
{
	/// <summary>Pushes the texture for the frame about to be acquired.</summary>
	void SetCurrentTexture(nint texture);
}

/// <summary>
/// Neutral Skia-on-Metal <see cref="ISwapChain"/> for AppleUIKit — holds the MTKView's device/queue and wraps
/// the per-frame drawable <c>MTLTexture</c> as an <see cref="IMetalRenderTarget"/>; the Skia backend builds its
/// GRContext-Metal + surface and flushes. The MTKView owns the drawable and presents it (PresentDrawable/Commit), so
/// <see cref="Present"/> is a no-op. Names no Skia type. Mirrors the macOS host's Metal context.
/// </summary>
internal sealed class AppleMetalGraphicsContext : ISwapChain, IAppleNativeTextureSink
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

	public bool IsLost => false;

	public void SetCurrentTexture(nint texture) => _currentTexture = texture;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> new AppleMetalRenderTarget(_currentTexture, _device, _queue, Math.Max(1, width), Math.Max(1, height));

	// The MTKView owns the drawable and commits after its Draw returns.
	public void Present() { }

	public void Dispose() { }

	private sealed class AppleMetalRenderTarget(nint texture, nint device, nint queue, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => texture;
		public nint Device => device;
		public nint Queue => queue;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}
