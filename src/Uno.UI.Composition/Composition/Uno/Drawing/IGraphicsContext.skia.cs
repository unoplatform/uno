#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A live GPU-API connection/device (WebGPU Instance/Adapter/Device/Queue, a GL context, a
/// <c>VkDevice</c>+queue, an <c>MTLDevice</c>). Created by the framework's per-kind provider from an
/// <see cref="INativeWindow"/>; owns the swapchain/surface, present, and the dirty-rect blit internally.
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

	/// <summary>Whether the context has been lost (device removed, surface invalidated) and must be recreated.</summary>
	bool IsLost { get; }

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
