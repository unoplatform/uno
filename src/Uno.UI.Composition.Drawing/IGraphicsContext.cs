#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A live GPU-API connection/device (WebGPU Instance/Adapter/Device/Queue, a GL context, a
/// <c>VkDevice</c>+queue, an <c>MTLDevice</c>). Created by the host's <see cref="GraphicsContextFactory"/> for a
/// negotiated kind; owns the window binding, swapchain/surface, present, and the dirty-rect blit internally.
/// A backend consumes it (downcasting to its kind-specific type) to build pipelines and render targets.
/// </summary>
/// <summary>
/// A thin init handle for a backend's GPU device/connection (instance/adapter/device/queue), produced by the
/// context factory and passed to the matched backend pair to bind it to the device. It is not a resource
/// factory — resource creation (textures, shaders, filters, …) lives on <see cref="IDrawingFactory"/>, and the
/// frame render target is a render-side concern.
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
}
