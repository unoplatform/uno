#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;

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
internal sealed class FrameBufferGraphicsContext : ISwapChain, IGLDeviceContext
{
	public FrameBufferGraphicsContext(GraphicsContextKind kind) => Kind = kind;

	public GraphicsContextKind Kind { get; }

	// GL device face (used only when Kind == OpenGLES — the DRM/GBM GLES path). The framebuffer surface itself is
	// handed over by FrameBufferRenderer's own target; this supplies the loader the neutral present builds from.
	public GLFlavor Flavor => GLFlavor.OpenGLES;
	public Func<string, nint> GetProcAddress => static name => EglHelper.EglGetProcAddress(name);

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> throw new NotSupportedException(
			"The Linux framebuffer frame loop is owned by FrameBufferRenderer (DRM page-flip / fbdev vsync); it does not acquire targets through the context.");

	public void Present()
		=> throw new NotSupportedException(
			"The Linux framebuffer present is owned by FrameBufferRenderer (DRM page-flip / fbdev blit).");

	public void Dispose() { }
}
