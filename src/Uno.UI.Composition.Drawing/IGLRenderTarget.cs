#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// An OpenGL color target: the per-frame framebuffer the backend renders into (the host swapchain's default
/// framebuffer), plus the sample/stencil counts its visual was created with. Pure surface — the GL device
/// details (flavor + proc loader) live on the context (<see cref="IGLDeviceContext"/>), not here. The host makes
/// its GL context <em>current</em> before the frame; the backend composes into this framebuffer and the swap is
/// the <see cref="ISwapChain"/>'s job. Mirrors <see cref="ISoftwareRenderTarget"/> for the CPU case.
/// </summary>
public interface IGLRenderTarget : IRenderTarget
{
	uint FramebufferId { get; }

	int SampleCount { get; }

	int StencilBits { get; }
}
