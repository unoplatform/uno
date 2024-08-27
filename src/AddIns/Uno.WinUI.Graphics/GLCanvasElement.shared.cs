using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Silk.NET.OpenGL;

namespace Uno.WinUI.Graphics;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on WinUI and on skia-based targets running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets (and WinUI).
/// </remarks>
public abstract partial class GLCanvasElement : UserControl
{
	/// <summary>
	/// Use this function for the initial setup, e.g. setting up VAOs, VBOs, EBOs, etc.
	/// </summary>
	/// <remarks>
	/// <see cref="Init"/> might be called multiple times. Every call to <see cref="Init"/> except the first one
	/// will be preceded by a call to <see cref="OnDestroy"/>.
	/// </remarks>
	protected abstract void Init(GL gl);
	/// <summary>
	/// Use this function for cleaning up previously allocated resources.
	/// </summary>
	/// /// <remarks>
	/// <see cref="OnDestroy"/> might be called multiple times. Every call to <see cref="OnDestroy"/> will be preceded by
	/// a call to <see cref="Init"/>.
	/// </remarks>
	protected abstract void OnDestroy(GL gl);
	/// <summary>
	/// The rendering logic goes this.
	/// </summary>
	/// <remarks>
	/// Before <see cref="RenderOverride"/> is called, the OpenGL viewport is set to the resolution that was provided to
	/// the <see cref="GLCanvasElement"/> constructor.
	/// </remarks>
	/// <remarks>
	/// Due to the fact that both <see cref="GLCanvasElement"/> and the skia rendering engine used by Uno both use OpenGL,
	/// you must make sure to restore all the OpenGL state values to their original values at the end of <see cref="RenderOverride"/>.
	/// For example, make sure to save the values for the initially-bound OpenGL VAO if you intend to bind your own VAO
	/// and bind the original VAO at the end of the method. Similarly, make sure to disable depth testing at
	/// the end if you choose to enable it.
	/// Some of this may be done for you automatically.
	/// </remarks>
	protected abstract void RenderOverride(GL gl);

	/// <summary>
	/// Invalidates the rendering, and calls <see cref="RenderOverride"/> in the next rendering cycle.
	/// <see cref="RenderOverride"/> will only be called once after <see cref="Invalidate"/> and the output will
	/// be saved. You need to call <see cref="Invalidate"/> everytime an update is needed. If drawing an
	/// animation, call <see cref="Invalidate"/> inside <see cref="RenderOverride"/> to continuously invalidate and update.
	/// </summary>
	public partial void Invalidate();

	/// <summary>
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="availableSize"/> given. Subclasses of <see cref="GLCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if availableSize is infinite (e.g. if inside a StackPanel).</remarks>
	protected override partial Size MeasureOverride(Size availableSize);

	/// <summary>
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="finalSize"/> given. Subclasses of <see cref="GLCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if <see cref="finalSize"/> is infinite (e.g. if inside a StackPanel).</remarks>
	protected override partial Size ArrangeOverride(Size finalSize);

	private readonly struct GLStateDisposable : IDisposable
	{
		private readonly GL _gl;
		private readonly int _oldArrayBuffer;
		private readonly int _oldVertexArray;
		private readonly int _oldFramebuffer;
		private readonly int _oldTextureColorBuffer;
		private readonly int _oldRbo;
		private readonly bool _depthTestEnabled;
		private readonly bool _depthTestMask;
		private readonly int[] _oldViewport = new int[4];

#if WINAPPSDK
		private readonly IntPtr _dc;
		private readonly IntPtr _glContext;
#endif

#if WINAPPSDK
		public GLStateDisposable(GL gl, IntPtr dc, IntPtr glContext)
#else
		public GLStateDisposable(GL gl)
#endif
		{
			_gl = gl;

#if WINAPPSDK
			_glContext = NativeMethods.wglGetCurrentContext();
			_dc = NativeMethods.wglGetCurrentDC();
			NativeMethods.wglMakeCurrent(dc, glContext);
#endif

			_depthTestEnabled = gl.GetBoolean(GLEnum.DepthTest);
			_depthTestMask = gl.GetBoolean(GLEnum.DepthWritemask);
			_oldArrayBuffer = gl.GetInteger(GLEnum.ArrayBufferBinding);
			_oldVertexArray = gl.GetInteger(GLEnum.VertexArrayBinding);
			_oldFramebuffer = gl.GetInteger(GLEnum.FramebufferBinding);
			_oldTextureColorBuffer = gl.GetInteger(GLEnum.TextureBinding2D);
			_oldRbo = gl.GetInteger(GLEnum.RenderbufferBinding);
			gl.GetInteger(GLEnum.Viewport, new Span<int>(_oldViewport));
		}

		public void Dispose()
		{
			_gl.BindVertexArray((uint)_oldVertexArray);
			_gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)_oldArrayBuffer);
			_gl.BindFramebuffer(GLEnum.Framebuffer, (uint)_oldFramebuffer);
			_gl.BindTexture(GLEnum.Texture2D, (uint)_oldTextureColorBuffer);
			_gl.BindRenderbuffer(GLEnum.Renderbuffer, (uint)_oldRbo);
			_gl.Viewport(_oldViewport[0], _oldViewport[1], (uint)_oldViewport[2], (uint)_oldViewport[3]);
			_gl.DepthMask(_depthTestMask);
			if (_depthTestEnabled)
			{
				_gl.Enable(EnableCap.DepthTest);
			}
			else
			{
				_gl.Disable(EnableCap.DepthTest);
			}

#if WINAPPSDK
			NativeMethods.wglMakeCurrent(_dc, _glContext);
#endif
		}
	}
}
