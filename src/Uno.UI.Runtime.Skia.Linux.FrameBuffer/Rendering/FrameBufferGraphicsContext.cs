#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// The Linux framebuffer host's neutral graphics context. It reports the negotiated kind (OpenGLES for the DRM/GBM
/// GLES path, Software for the fbdev CPU path) so <see cref="GraphicsRegistry"/> can bind the backend renderer — but
/// its per-frame acquire/present are NOT the framebuffer frame path. Unlike the other hosts, the framebuffer's
/// present is a DRM page-flip vsync loop (present-then-render across two threads) owned by
/// <see cref="FrameBufferRenderer"/>, which does not fit the <c>AcquireRenderTarget → render → Present</c> contract;
/// the FrameBufferRenderer keeps owning its own target + present and drives the neutral render seam directly. This
/// host also has no WebGPU/Skia fork (the DRM-vs-fbdev choice is the platform GPU-init), so it names no backend.
/// </summary>
internal sealed class FrameBufferGraphicsContext : ISwapChain
{
	public FrameBufferGraphicsContext(GraphicsContextKind kind) => Kind = kind;

	public GraphicsContextKind Kind { get; }

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> throw new NotSupportedException(
			"The Linux framebuffer frame loop is owned by FrameBufferRenderer (DRM page-flip / fbdev vsync); it does not acquire targets through the context.");

	public void Present()
		=> throw new NotSupportedException(
			"The Linux framebuffer present is owned by FrameBufferRenderer (DRM page-flip / fbdev blit).");

	public void Dispose() { }
}
