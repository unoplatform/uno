using System;
using Android.Opengl;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Skia;
using Uno.WinUI.Runtime.Skia.Android;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Neutral GLES <see cref="ISwapChain"/> for the Android Skia path. <c>GLSurfaceView</c> owns the EGL context and
/// swaps implicitly after <c>OnDrawFrame</c>, so this context wraps the ambient (already-current) EGL context and
/// its <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class AndroidGLGraphicsContext : ISwapChain, IGLDeviceContext
{
	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public Func<string, nint> GetProcAddress => AndroidNativeOpenGLWrapper.GetProcAddressStatic;

	// The implicit buffer swap leaves the default framebuffer undefined, so the frame is composed into a retained
	// FBO this host owns and blitted over at present; that copy is what carries the previous frame forward.
	public bool PreservesContents => _contentsPreserved;

	private readonly GLRetainedFramebuffer _retained = new(AndroidNativeOpenGLWrapper.GetProcAddressStatic);
	private bool _contentsPreserved;
	private AndroidGLRenderTarget? _target;
	private int _width, _height;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		var retained = _retained.TryResize(width, height, out _contentsPreserved);
		if (_target is null || width != _width || height != _height || (retained && _target.FramebufferId != _retained.FramebufferId))
		{
			// Read the default framebuffer + its sample/stencil counts (EGL context is current here) into a neutral target.
			var buffer = new int[3];
			GLES20.GlGetIntegerv(GLES20.GlFramebufferBinding, buffer, 0);
			GLES20.GlGetIntegerv(GLES20.GlStencilBits, buffer, 1);
			GLES20.GlGetIntegerv(GLES20.GlSamples, buffer, 2);

			// Single-sampled when retained: Skia's coverage AA does not need the surface's multisampling, and a
			// multisampled attachment would need resolving before the blit.
			_target = retained
				? new AndroidGLRenderTarget(_retained.FramebufferId, 0, _retained.StencilBits, width, height)
				: new AndroidGLRenderTarget((uint)buffer[0], buffer[2], buffer[1], width, height);
			_width = width;
			_height = height;
		}

		return _target;
	}

	// GLSurfaceView swaps the buffers implicitly once OnDrawFrame returns, so the retained frame has to reach the
	// default framebuffer before this returns.
	public void Present() => _retained.BlitToDefault();

	public void Dispose() => _retained.Dispose();

	// GLES default-framebuffer target; the backend builds GRContext-GLES against the current context.
	private sealed class AndroidGLRenderTarget(uint framebufferId, int sampleCount, int stencilBits, int width, int height) : IGLRenderTarget
	{
		public uint FramebufferId => framebufferId;
		public int SampleCount => sampleCount;
		public int StencilBits => stencilBits;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public void Dispose() { }
	}
}
