#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A live GPU-API connection/device (WebGPU Instance/Adapter/Device/Queue, a GL context, a
/// <c>VkDevice</c>+queue, an <c>MTLDevice</c>). Created by the framework's per-kind provider from an
/// <see cref="INativeWindow"/>; owns the swapchain/surface, present, and the dirty-rect blit internally.
/// A backend consumes it (downcasting to its kind-specific type) to build pipelines and render targets.
/// </summary>
public interface IGraphicsContext : IDisposable
{
	GraphicsContextKind Kind { get; }

	/// <summary>Whether the context has been lost (device removed, surface invalidated) and must be recreated.</summary>
	bool IsLost { get; }

	/// <summary>
	/// Produces the color <see cref="IRenderTarget"/> the backend renders into, at the given pixel size (recreated
	/// on resize). Whether it is backed by the window swapchain or a retained offscreen texture — and the
	/// dirty-rect blit/present — is internal to the context; the backend only ever sees the returned view.
	/// </summary>
	IRenderTarget CreateRenderTarget(int width, int height);
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
