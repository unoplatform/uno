#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The backend-facing GPU device handle for a negotiated kind (WebGPU Instance/Adapter/Device/Queue, a GL context,
/// a <c>VkDevice</c>+queue, an <c>MTLDevice</c>+queue). Produced by the host's <see cref="GraphicsContextFactory"/>
/// and handed to the matched backend, which downcasts it to its kind-specific device interface to bind. It is not a
/// resource factory (that is <see cref="IDrawingFactory"/>); the acquire/present loop is <see cref="ISwapChain"/>.
/// </summary>
public interface IGraphicsContext : IDisposable
{
	GraphicsContextKind Kind { get; }
}

/// <summary>
/// The host swapchain half of a graphics context: the per-frame acquire/present loop the host drives, used by the
/// framework's kind-neutral render loop. A render backend never sees it — acquire/present are the host's
/// window-swapchain concern; the backend consumes only the <see cref="IGraphicsContext"/> device handle.
/// </summary>
internal interface ISwapChain : IGraphicsContext
{
	/// <summary>Acquires the color target for the next frame (recreating the swapchain/framebuffer on resize).</summary>
	IRenderTarget AcquireRenderTarget(int width, int height);

	/// <summary>Presents the frame rendered into the last-acquired target to the window.</summary>
	void Present();
}

/// <summary>
/// The per-frame color attachment a backend renders into — a kind-matched view (WebGPU <c>TextureView</c>, Vulkan
/// <c>VkImageView</c>, Metal <c>MTLTexture</c>, GL framebuffer). Whether it is backed by the window swapchain or a
/// retained offscreen texture is an internal decision the backend never sees; the backend allocates its own
/// depth/stencil to match.
/// </summary>
public interface IRenderTarget : IDisposable
{
	int Width { get; }

	int Height { get; }

	GraphicsColorFormat ColorFormat { get; }

	/// <summary>
	/// True when the host guarantees this target keeps the previous frame's pixels until the next acquisition at the
	/// same size, so the compositor may repaint only the damaged region and let the rest survive. Retention is the
	/// host's business (reused CPU framebuffer or stable GPU image → true; an undefined surface each frame → false,
	/// the default, which forces a full repaint). The render backend never sees this.
	/// </summary>
	bool PreservesContents => false;
}
