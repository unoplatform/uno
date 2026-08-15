using System;
using Android.Opengl;
using Uno.UI.Composition.Drawing;
using Uno.WinUI.Runtime.Skia.Android;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Neutral GLES <see cref="ISwapChain"/> for the Android Skia path. Uno does not own the GL device here —
/// <c>GLSurfaceView</c> creates the EGL context, runs its own render thread, and swaps implicitly after
/// <c>OnDrawFrame</c> returns. So this context WRAPS the ambient EGL context (already current on the GLSurfaceView
/// render thread when acquired) and its <see cref="Present"/> is a no-op (GLSurfaceView calls <c>eglSwapBuffers</c>).
/// This mirrors the browser WebGL context (also a framework-created context, framework-driven loop, implicit present)
/// — see WasmGLGraphicsContext. The Skia backend builds its GRContext-GLES against the current context. Names no Skia type.
/// </summary>
internal sealed class AndroidGLGraphicsContext : ISwapChain, IGLDeviceContext
{
	public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;

	public GLFlavor Flavor => GLFlavor.OpenGLES;
	public Func<string, nint> GetProcAddress => AndroidNativeOpenGLWrapper.GetProcAddressStatic;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		// The EGL context is current on the GLSurfaceView render thread; read the default framebuffer + its
		// sample/stencil counts and hand a neutral target.
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
