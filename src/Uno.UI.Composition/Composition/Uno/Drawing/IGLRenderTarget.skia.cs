#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// An OpenGL color target: the framebuffer the backend renders into (the host swapchain's default framebuffer),
/// plus the sample/stencil counts its visual was created with. The host makes its GL context <em>current</em>
/// before handing this over, so the backend builds its <c>GRContext</c>-GL against the current context; the
/// swap/present is the <see cref="IGraphicsContext"/>'s job. Mirrors <see cref="ISoftwareRenderTarget"/> for the
/// CPU-framebuffer case — the host stays free of any GPU-library type.
/// </summary>
public interface IGLRenderTarget : IRenderTarget
{
	uint FramebufferId { get; }

	int SampleCount { get; }

	int StencilBits { get; }
}
