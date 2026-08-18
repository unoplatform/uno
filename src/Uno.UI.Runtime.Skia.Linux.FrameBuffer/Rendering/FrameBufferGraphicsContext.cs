#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// The Linux framebuffer host's neutral graphics context. It only reports the negotiated kind so
/// <see cref="GraphicsRegistry"/> can bind the backend renderer; the frame loop and present are owned by
/// <see cref="FrameBufferRenderer"/> (DRM page-flip / fbdev vsync), so acquire/present here throw.
/// </summary>
internal sealed class FrameBufferGraphicsContext : ISwapChain, IGLDeviceContext
{
	public FrameBufferGraphicsContext(GraphicsContextKind kind) => Kind = kind;

	public GraphicsContextKind Kind { get; }

	// GL device face (used only when Kind == OpenGLES): supplies the GLES proc-address loader.
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
