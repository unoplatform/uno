using System;
using Android.Opengl;
using Uno.UI.Composition.Drawing;
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

	public GLFlavor Flavor => GLFlavor.OpenGLES;
	public Func<string, nint> GetProcAddress => AndroidNativeOpenGLWrapper.GetProcAddressStatic;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		// Read the default framebuffer + its sample/stencil counts (EGL context is current here) into a neutral target.
		var buffer = new int[3];
		GLES20.GlGetIntegerv(GLES20.GlFramebufferBinding, buffer, 0);
		GLES20.GlGetIntegerv(GLES20.GlStencilBits, buffer, 1);
		GLES20.GlGetIntegerv(GLES20.GlSamples, buffer, 2);

		return new AndroidGLRenderTarget((uint)buffer[0], buffer[2], buffer[1], Math.Max(1, width), Math.Max(1, height));
	}

	// GLSurfaceView swaps the buffers implicitly once OnDrawFrame returns.
	public void Present() { }

	public void Dispose() { }

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
