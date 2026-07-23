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
/// A framework-owned factory for one <see cref="GraphicsContextKind"/> (shipped as a modular
/// <c>Uno.Graphics.&lt;kind&gt;</c> package, pulled in transitively by the backends that prefer it). It owns
/// the platform-specific context+surface creation so backend implementors never touch graphics init.
/// </summary>
public interface IGraphicsContextProvider
{
	GraphicsContextKind Kind { get; }

	/// <summary>
	/// Attempts to create a context for <paramref name="window"/> satisfying <paramref name="requirements"/>.
	/// Returns <see langword="null"/> if the API is unavailable or the requirements can't be met — in which
	/// case it must have fully cleaned up (a null return means "as if never attempted"), letting negotiation
	/// fall through to the next context kind.
	/// </summary>
	IGraphicsContext? TryCreate(INativeWindow window, in GraphicsRequirements requirements);
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
