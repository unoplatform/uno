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

	// Only the software path composes into one persistent CPU buffer, so only it keeps the previous frame's pixels
	// for a partial repaint. The DRM/GLES path presents through 2-3 rotating GBM buffers, where the acquired one
	// still holds the content of a flip or two ago. The mouse cursor is composed into that buffer too, so while it
	// is drawn the frame has to be repainted whole, otherwise every position it passed through stays behind.
	public bool PreservesContents => Kind == GraphicsContextKind.Software && !ComposesCursorOverlay;

	/// <summary>Set by <see cref="FrameBufferRenderer"/> each frame: whether it composes the cursor into the target.</summary>
	internal bool ComposesCursorOverlay { get; set; }

	// GL device face (used only when Kind == OpenGLES): supplies the GLES proc-address loader.
	public Func<string, nint> GetProcAddress => static name => EglHelper.EglGetProcAddress(name);

	internal void SetAcquire(Func<int, int, IRenderTarget> acquire) => _acquire = acquire;

	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> (_acquire ?? throw new InvalidOperationException("FrameBufferRenderer has not wired target acquisition."))(width, height);

	public void Present() { }

	public void Dispose() { }
}
