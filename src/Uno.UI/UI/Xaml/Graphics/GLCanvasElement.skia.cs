#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on skia-based targets and when running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets.
/// </remarks>
public abstract partial class GLCanvasElement : FrameworkElement
{
	internal delegate IntPtr GLGetProcAddress(string proc);

	private const int BytesPerPixel = 4;

	private readonly uint _width;
	private readonly uint _height;
	private readonly GLVisual _glVisual;

	private bool _renderDirty = true;

	private GL? _gl;
	private IntPtr _pixels;
	private uint _framebuffer;
	private uint _textureColorBuffer;
	private uint _renderBuffer;

	/// <param name="resolution">The resolution of the backing framebuffer.</param>
	protected GLCanvasElement(Size resolution)
	{
		_width = (uint)resolution.Width;
		_height = (uint)resolution.Height;

		_glVisual = new GLVisual(this, Visual.Compositor);
		Visual.Children.InsertAtTop(_glVisual);
	}

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
	public void Invalidate()
	{
		_renderDirty = true;
		_glVisual.Compositor.InvalidateRender(_glVisual);
	}

	private protected override unsafe void OnLoaded()
	{
		base.OnLoaded();

		if (ApiExtensibility.CreateInstance<INativeContext>(this, out var nativeContext))
		{
			_gl = GL.GetApi(nativeContext);
		}
		else if (ApiExtensibility.CreateInstance<GLGetProcAddress>(this, out var getProcAddress))
		{
			_gl = GL.GetApi(getProcAddress.Invoke);
		}
		else
		{
			throw new InvalidOperationException($"Couldn't create a {nameof(GL)} object for {nameof(GLCanvasElement)}. Make sure you are running on a platform with {nameof(GLCanvasElement)} support.");
		}

		using var _ = new GLStateDisposable(_gl);

		_pixels = Marshal.AllocHGlobal((int)(_width * _height * BytesPerPixel));
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

		Invalidate();
	}

	private unsafe void Render()
	{
		Debug.Assert(_renderDirty);
		_renderDirty = false;

		using var _ = new GLStateDisposable(_gl!);

		_gl!.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
		{
			_gl.Viewport(new global::System.Drawing.Size((int)_width, (int)_height));

			RenderOverride(_gl);

			// Can we do without this copy?
			_gl.ReadBuffer(GLEnum.ColorAttachment0);
			_gl.ReadPixels(0, 0, _width, _height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)_pixels);
		}
	}

	private protected override void OnUnloaded()
	{
		Marshal.FreeHGlobal(_pixels);

		if (_gl is { })
		{
			_gl.DeleteFramebuffer(_framebuffer);
			_gl.DeleteTexture(_textureColorBuffer);
			_gl.DeleteRenderbuffer(_renderBuffer);
		}
	}

	/// <summary>
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="availableSize"/> given. Subclasses of <see cref="SKCanvasElement"/>
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
	/// By default, <see cref="GLCanvasElement"/> uses all the <see cref="finalSize"/> given. Subclasses of <see cref="SKCanvasElement"/>
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
			throw new ArgumentException($"{nameof(SKCanvasElement)} cannot be arranged with infinite or NaN values, but received finalSize={finalSize}.");
		}
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

		public GLStateDisposable(GL gl)
		{
			_gl = gl;

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
		}
	}
}
