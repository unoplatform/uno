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
	/// <summary>
	/// Acquires the color target for the next frame. The swapchain caches the target while the requested size is
	/// unchanged and recreates it on resize (or when it becomes invalid), so callers may acquire every frame.
	/// </summary>
	IRenderTarget AcquireRenderTarget(int width, int height);

	/// <summary>Presents the frame rendered into the last-acquired target to the window.</summary>
	void Present();

	/// <summary>
	/// True when this swapchain keeps the previous frame's pixels at the same size, letting the compositor repaint
	/// only the damaged region. A reused CPU framebuffer or a host-retained GPU surface returns true; a swapchain
	/// whose back buffer is undefined each frame returns false (the default), forcing a full repaint.
	/// </summary>
	bool PreservesContents => false;
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
}
