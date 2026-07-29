#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// An <see cref="IGraphicsContext"/> bound to a window that hands out the per-frame color target and presents
/// it. Uno's context factory creates these — it owns the window/swapchain binding for the chosen
/// <see cref="GraphicsContextKind"/> (GLX/EGL context, Vulkan surface, CPU framebuffer, …). The host drives
/// acquire → render → present without naming any GPU-library type, and the matched backend wraps the acquired
/// target inside <see cref="IRenderBackend.BeginPresent"/> (e.g. Skia wraps a CPU framebuffer as an SKSurface).
/// </summary>
public interface IPresentableGraphicsContext : IGraphicsContext
{
	/// <summary>Acquires the color target for the next frame, (re)creating the swapchain/framebuffer for the size.</summary>
	IRenderTarget AcquireRenderTarget(int width, int height);

	/// <summary>Presents the frame just rendered into the last-acquired target to the window.</summary>
	void Present();
}

/// <summary>
/// A CPU-framebuffer <see cref="IRenderTarget"/> (BGRA8888, premultiplied) that a software
/// <see cref="IPresentableGraphicsContext"/> hands over: the matched backend wraps <see cref="Pixels"/> /
/// <see cref="IRenderTarget.Width"/> / <see cref="RowBytes"/> into its own surface, with no GPU-library type
/// crossing the boundary. The buffer is owned by the context (valid until the next acquire/present cycle).
/// </summary>
public interface ISoftwareRenderTarget : IRenderTarget
{
	/// <summary>Pointer to the top-left pixel of the framebuffer.</summary>
	nint Pixels { get; }

	/// <summary>Bytes per row (stride) of the framebuffer.</summary>
	int RowBytes { get; }
}
