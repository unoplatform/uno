using System.Runtime.InteropServices;
using Windows.Foundation;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;
using Uno.UI.Runtime.Skia;
using Boolean = Silk.NET.OpenGL.Boolean;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class GLCanvasElement : FrameworkElement
{
	private const int BytesPerPixel = 4;

	private readonly uint _width;
	private readonly uint _height;
	private readonly IntPtr _pixels;
	private readonly GLVisual _glVisual;

	private bool _firstLoad = true;

	private GL? _gl;
	private uint _framebuffer;
	private uint _textureColorBuffer;
	private uint _renderBuffer;

	protected GLCanvasElement(Size resolution)
	{
		_width = (uint)resolution.Width;
		_height = (uint)resolution.Height;
		_pixels = Marshal.AllocHGlobal((int)(_width * _height * BytesPerPixel));

		_glVisual = new GLVisual(this, Visual.Compositor);
		Visual.Children.InsertAtTop(_glVisual);
	}

	~GLCanvasElement()
	{
		Marshal.FreeHGlobal(_pixels);

		if (_gl is { })
		{
			_gl.DeleteFramebuffer(_framebuffer);
			_gl.DeleteTexture(_textureColorBuffer);
			_gl.DeleteRenderbuffer(_renderBuffer);
		}
	}

	protected abstract void Init(GL gl);
	protected abstract void OnDestroy(GL gl);
	protected abstract void RenderOverride(GL gl);

	public void Invalidate() => _glVisual.Compositor.InvalidateRender(_glVisual);

	private unsafe protected override void OnLoaded()
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

		if (_firstLoad)
		{
			_firstLoad = false;

			using var _ = new GLStateDisposable(_gl);

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

					Init(_gl);
				}
				_gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

				if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
				{
					throw new InvalidOperationException("Offscreen framebuffer is not complete");
				}
			}
			_gl.BindFramebuffer(GLEnum.Framebuffer, 0);
		}

		Render();
	}

	private unsafe void Render()
	{
		if (!IsLoaded)
		{
			return;
		}

		using var _ = new GLStateDisposable(_gl!);

		_gl!.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
		{
			_gl.Viewport(new System.Drawing.Size((int)_width, (int)_height));

			_gl.Enable(EnableCap.DepthTest);
			_gl.DepthMask(true);

			RenderOverride(_gl);

			// Can we do without this copy?
			_gl.ReadBuffer(GLEnum.ColorAttachment0);
			_gl.ReadPixels(0, 0, _width, _height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)_pixels);
		}
	}

	/// <summary>
	/// By default, SKCanvasElement uses all the <see cref="availableSize"/> given. Subclasses of SKCanvasElement
	/// should override this method if they need something different.
	/// </summary>
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
			if (_depthTestEnabled)
			{
				_gl.Enable(EnableCap.DepthTest);
			}
			else
			{
				_gl.Disable(EnableCap.DepthTest);
			}

			_gl.DepthMask(_depthTestMask);
		}
	}
}
