#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// The Linux framebuffer host's neutral graphics context. The frame loop and present are owned by
/// <see cref="FrameBufferRenderer"/> (DRM page-flip / fbdev vsync), which wires <see cref="SetAcquire"/> with its
/// orientation-aware, size-cached target creation; <see cref="Present"/> is a no-op (the renderer flips/blits).
/// </summary>
internal sealed class FrameBufferGraphicsContext : ISwapChain, IGLDeviceContext
{
	private Func<int, int, IRenderTarget>? _acquire;

	public FrameBufferGraphicsContext(GraphicsContextKind kind) => Kind = kind;

	public GraphicsContextKind Kind { get; }

	// The renderer composes into one persistent framebuffer (recreated only on resize), so the previous frame's
	// pixels survive and the compositor can repaint only the damaged region.
	public bool PreservesContents => true;

	// GL device face (used only when Kind == OpenGLES): supplies the GLES proc-address loader.
	public GLFlavor Flavor => GLFlavor.OpenGLES;
	public Func<string, nint> GetProcAddress => static name => EglHelper.EglGetProcAddress(name);

	internal void SetAcquire(Func<int, int, IRenderTarget> acquire) => _acquire = acquire;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> (_acquire ?? throw new InvalidOperationException("FrameBufferRenderer has not wired target acquisition."))(width, height);

	public void Present() { }

	public void Dispose() { }
}
