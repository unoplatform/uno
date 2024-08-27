#if !WINAPPSDK

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;

namespace Uno.WinUI.Graphics;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on skia-based targets and when running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets.
/// </remarks>
public abstract partial class GLCanvasElement : FrameworkElement
{
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

	/// <param name="width">The width of the backing framebuffer.</param>
	/// <param name="height">The height of the backing framebuffer.</param>
	protected GLCanvasElement(uint width, uint height, Window window)
	{
		_width = width;
		_height = height;

		_glVisual = new GLVisual(this, Visual.Compositor);
		Visual.Children.InsertAtTop(_glVisual);
	}

	public partial void Invalidate()
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
		else if (ApiExtensibility.CreateInstance<Uno.Graphics.GLGetProcAddress>(this, out var getProcAddress))
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

	protected override partial Size MeasureOverride(Size availableSize)
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

	protected override partial Size ArrangeOverride(Size finalSize)
	{
		if (finalSize.Width == Double.PositiveInfinity ||
			finalSize.Height == Double.PositiveInfinity ||
			double.IsNaN(finalSize.Width) ||
			double.IsNaN(finalSize.Height))
		{
			throw new ArgumentException($"{nameof(GLCanvasElement)} cannot be arranged with infinite or NaN values, but received finalSize={finalSize}.");
		}
		return finalSize;
	}
}
#endif
