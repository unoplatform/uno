using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Silk.NET.OpenGL;

#if WINAPPSDK
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
#endif

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
	private const int BytesPerPixel = 4;

	private readonly uint _width;
	private readonly uint _height;

	// These are valid if and only if IsLoaded
	private GL? _gl;
	private uint _framebuffer;
	private uint _textureColorBuffer;
	private uint _renderBuffer;
	private IntPtr _pixels;

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

	private GLStateDisposable CreateGlStateDisposable()
#if WINAPPSDK
		=> new GLStateDisposable(_gl!, _hdc, _glContext);
#else
		=> new GLStateDisposable(_gl!);
#endif

	private unsafe void OnLoadedShared()
	{
		Debug.Assert(_gl is not null);

		_pixels = Marshal.AllocHGlobal((int)(_width * _height * BytesPerPixel));

		using (CreateGlStateDisposable())
		{
			_framebuffer = _gl.GenBuffer();
			_gl.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
			{
				_textureColorBuffer = _gl.GenTexture();
				_gl.BindTexture(GLEnum.Texture2D, _textureColorBuffer);
				{
					_gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgb, _width, _height, 0, GLEnum.Rgb, GLEnum.UnsignedByte, (void*)0);
					_gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.Linear);
					_gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
					_gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, _textureColorBuffer, 0);
				}
				_gl.BindTexture(GLEnum.Texture2D, 0);

				_renderBuffer = _gl.GenRenderbuffer();
				_gl.BindRenderbuffer(GLEnum.Renderbuffer, _renderBuffer);
				{
					_gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Depth24Stencil8, _width, _height);
					_gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, _renderBuffer);
				}
				_gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

				if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
				{
					throw new InvalidOperationException("Offscreen framebuffer is not complete");
				}

				Init(_gl);
			}
			_gl.BindFramebuffer(GLEnum.Framebuffer, 0);
		}

		Invalidate();
	}

	private void OnUnloadedShared()
	{
		Debug.Assert(_gl is not null); // because OnLoaded creates _gl

		Marshal.FreeHGlobal(_pixels);

		using (CreateGlStateDisposable())
		{
#if WINAPPSDK
			if (NativeMethods.wglMakeCurrent(_hdc, _glContext) != 1)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Skipping the disposing step because the window is closing. If it's not closing, then this is unexpected.");
				}
				return;
			}
#endif
			OnDestroy(_gl);
			_gl.DeleteFramebuffer(_framebuffer);
			_gl.DeleteTexture(_textureColorBuffer);
			_gl.DeleteRenderbuffer(_renderBuffer);
			_gl.Dispose();
		}

		_gl = default;
		_framebuffer = default;
		_textureColorBuffer = default;
		_renderBuffer = default;
		_pixels = default;

#if WINAPPSDK
		NativeMethods.wglDeleteContext(_glContext);
		_glContext = default;
#endif
	}

	private unsafe void Render()
	{
		if (!IsLoaded)
		{
			return;
		}

		Debug.Assert(_gl is not null); // because _gl exists if loaded

		using var _ = CreateGlStateDisposable();

		_gl!.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
		{
			_gl.Viewport(new global::System.Drawing.Size((int)_width, (int)_height));

			RenderOverride(_gl);

			// Can we do without this copy?
			_gl.ReadBuffer(GLEnum.ColorAttachment0);
			_gl.ReadPixels(0, 0, _width, _height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)_pixels);

#if WINAPPSDK
			using (var stream = _backBuffer.PixelBuffer.AsStream())
			{
				stream.Write(new ReadOnlySpan<byte>((void*)_pixels, (int)(_width * _height * BytesPerPixel)));
			}
			_backBuffer.Invalidate();
#endif
		}
	}

	/// <summary>
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="availableSize"/> given. Subclasses of <see cref="GLCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if availableSize is infinite (e.g. if inside a StackPanel).</remarks>
	protected override Size MeasureOverride(Size availableSize)
	{
		if (availableSize.Width == Double.PositiveInfinity ||
		    availableSize.Height == Double.PositiveInfinity ||
		    double.IsNaN(availableSize.Width) ||
		    double.IsNaN(availableSize.Height))
		{
			throw new ArgumentException($"{nameof(GLCanvasElement)} cannot be measured with infinite or NaN values, but received availableSize={availableSize}.");
		}
		return availableSize;
	}

	/// <summary>
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="finalSize"/> given. Subclasses of <see cref="GLCanvasElement"/>
	/// should override this method if they need something different.
	/// </summary>
	/// <remarks>An exception will be thrown if <see cref="finalSize"/> is infinite (e.g. if inside a StackPanel).</remarks>
	protected override Size ArrangeOverride(Size finalSize)
	{
		if (finalSize.Width == Double.PositiveInfinity ||
		    finalSize.Height == Double.PositiveInfinity ||
		    double.IsNaN(finalSize.Width) ||
		    double.IsNaN(finalSize.Height))
		{
			throw new ArgumentException($"{nameof(GLCanvasElement)} cannot be arranged with infinite or NaN values, but received finalSize={finalSize}.");
		}
#if WINAPPSDK
		_image.Arrange(new Rect(new Point(), finalSize));
#endif
		return finalSize;
	}

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
