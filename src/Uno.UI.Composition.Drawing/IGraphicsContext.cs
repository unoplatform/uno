#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The backend-facing GPU device handle for a negotiated kind (WebGPU Instance/Adapter/Device/Queue, a GL
/// context, a <c>VkDevice</c>+queue, an <c>MTLDevice</c>+queue). Produced by the host's
/// <see cref="GraphicsContextFactory"/> and handed to the matched backend, which downcasts it to its
/// kind-specific device interface to bind. It is not a resource factory (that is <see cref="IDrawingFactory"/>);
/// the per-frame acquire/present swapchain loop is the host-facing <see cref="ISwapChain"/>; the frame render
/// target is a render-side concern.
/// </summary>
public interface IGraphicsContext : IDisposable
{
	GraphicsContextKind Kind { get; }
}

/// <summary>
/// The host swapchain half of a graphics context: the per-frame acquire/present loop the host drives. This is a
/// <em>host-facing</em> seam — the framework's kind-neutral render loop uses it. A render backend never sees it
/// (it consumes only the <see cref="IGraphicsContext"/> device handle, which it narrows to its own device
/// interface); acquire/present are the host's window-swapchain concern, not the backend's.
/// </summary>
internal interface ISwapChain : IGraphicsContext
{
	/// <summary>Acquires the color target for the next frame (recreating the swapchain/framebuffer on resize).</summary>
	IRenderTarget AcquireRenderTarget(int width, int height);

	/// <summary>Presents the frame rendered into the last-acquired target to the window.</summary>
	void Present();
}

/// <summary>
/// The per-frame color attachment a backend renders into — a kind-matched view (WebGPU <c>TextureView</c>,
/// Vulkan <c>VkImageView</c>, Metal <c>MTLTexture</c>, GL framebuffer). Whether it is backed by the window
/// swapchain (direct) or a retained offscreen texture (then the framework blits with dirty rects) is an
/// internal decision the backend never sees. The backend allocates its own depth/stencil to match.
/// </summary>
public interface IRenderTarget : IDisposable
{
	int Width { get; }

	int Height { get; }

	GraphicsColorFormat ColorFormat { get; }

	/// <summary>
	/// True when the host guarantees this target keeps the previous frame's pixels until the next acquisition at the
	/// same size — so the compositor may repaint only the damaged region (as an initial clip) and let the rest
	/// survive. Retention is entirely the host's business: a host that reuses one CPU framebuffer, or hands back a
	/// stable GPU image (or an intermediate it blits to its swapchain), reports true; a host whose surface is
	/// undefined each frame (a rotated/destroyed swapchain back buffer) reports false (the default) and gets a full
	/// repaint. The render backend never sees this — it just executes the clip+clear+replay it is handed.
	/// </summary>
	bool PreservesContents => false;
}
