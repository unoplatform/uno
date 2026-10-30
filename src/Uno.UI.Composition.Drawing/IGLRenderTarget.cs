#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// An OpenGL color target: the per-frame framebuffer the backend renders into, plus the sample/stencil counts it
/// was created with. The GL device details live on <see cref="IGLDeviceContext"/>, not here.
/// </summary>
public interface IGLRenderTarget : IRenderTarget
{
	uint FramebufferId { get; }

	int SampleCount { get; }

	int StencilBits { get; }
}
