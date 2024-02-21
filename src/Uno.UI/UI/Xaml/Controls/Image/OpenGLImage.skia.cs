using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Imaging;
using Silk.NET.OpenGL;
using Uno.Disposables;
namespace Microsoft.UI.Xaml.Controls;

public abstract class OpenGLImage : Image
{
	private const int BytesPerPixel = 4;

	private readonly uint _width;
	private readonly uint _height;
	private bool _firstLoad = true;

	private GL _gl;
	private uint _framebuffer;
	private uint _textureColorBuffer;
	private GLImageSource _writableBitmap;
	private unsafe readonly void* _pixels;

	unsafe protected OpenGLImage(Size resolution)
	{
		_width = (uint)resolution.Width;
		_height = (uint)resolution.Height;
		_pixels = (void*)Marshal.AllocHGlobal((int)(_width * _height * BytesPerPixel));
	}

	unsafe ~OpenGLImage()
	{
		Marshal.FreeHGlobal((IntPtr)_pixels);
	}

	protected abstract void OnLoad(GL gl);
	protected abstract void OnDestroy(GL gl);
	protected abstract void RenderOverride(GL gl);

	private unsafe protected override void OnLoaded()
	{
		base.OnLoaded();

		_gl = XamlRoot!.GetGL() as GL ?? throw new InvalidOperationException("Couldn't get the Silk.NET GL handle.");

		if (_firstLoad)
		{
			_firstLoad = false;

			using var _1 = XamlRoot?.LockGL();
			using var _2 = RestoreGLState();

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

				var rbo = _gl.GenRenderbuffer();
				_gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
				{
					_gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Depth24Stencil8, _width, _height);
					_gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, rbo);

					OnLoad(_gl);
				}
				_gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

				if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
				{
					throw new InvalidOperationException("Offscreen framebuffer is not complete");
				}
			}
			_gl.BindFramebuffer(GLEnum.Framebuffer, 0);

			_writableBitmap = new GLImageSource(_width, _height, _pixels);
			Source = _writableBitmap;
		}

		Render();
	}

	private unsafe void Render()
	{
		if (!IsLoaded)
		{
			return;
		}

		using var _1 = XamlRoot!.LockGL();
		using var _2 = RestoreGLState();

		_gl.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
		{
			_gl.Viewport(new System.Drawing.Size((int)_width, (int)_height));
			RenderOverride(_gl);

			_gl.ReadBuffer(GLEnum.ColorAttachment0);
			_gl.ReadPixels(0,  0, _width, _height, GLEnum.Bgra, GLEnum.UnsignedByte, _pixels);
			_writableBitmap.Render();
		}

		Invalidate();
	}

	private IDisposable RestoreGLState()
	{
		_gl.GetInteger(GLEnum.ArrayBufferBinding, out var oldArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out var oldVertexArray);
		_gl.GetInteger(GLEnum.FramebufferBinding, out var oldFramebuffer);
		_gl.GetInteger(GLEnum.TextureBinding2D, out var oldTextureColorBuffer);
		_gl.GetInteger(GLEnum.RenderbufferBinding, out var oldRbo);
		return Disposable.Create(() =>
		{
			_gl.BindVertexArray((uint)oldVertexArray);
			_gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)oldArrayBuffer);
			_gl.BindFramebuffer(GLEnum.Framebuffer, (uint)oldFramebuffer);
			_gl.BindTexture(GLEnum.Texture2D, (uint)oldTextureColorBuffer);
			_gl.BindRenderbuffer(GLEnum.Renderbuffer, (uint)oldRbo);
		});
	}

	public void Invalidate() => DispatcherQueue.TryEnqueue(Render);
}
