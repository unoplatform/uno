using System;
using Silk.NET.OpenGL;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Window = Microsoft.UI.Xaml.Window;

#if WINAPPSDK
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Uno.Extensions;
using Uno.Logging;
#else
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.UI.Dispatching;
using Buffer = Windows.Storage.Streams.Buffer;
#endif


namespace Uno.WinUI.Graphics3DGL;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on WinUI and on skia-based targets running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets (and WinUI).
/// </remarks>
public abstract partial class GLCanvasElement : Grid
{
	private const int BytesPerPixel = 4;

	private readonly INativeOpenGLWrapper _nativeOpenGlWrapper;

	private readonly uint _width;
	private readonly uint _height;

	private readonly WriteableBitmap _backBuffer;

	// These are valid if and only if IsLoaded
	private GL? _gl;
	private uint _framebuffer;
	private uint _textureColorBuffer;
	private uint _renderBuffer;
#if WINAPPSDK
	private IntPtr _pixels;
#endif

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

	/// <param name="width">The width of the backing framebuffer.</param>
	/// <param name="height">The height of the backing framebuffer.</param>
	/// <param name="getWindowFunc">A function that returns the Window object that this element belongs to. This parameter is only used on WinUI. On Uno Platform, it can be set to null.</param>
#if WINAPPSDK
	protected GLCanvasElement(uint width, uint height, Func<Window> getWindowFunc)
#else
	protected GLCanvasElement(uint width, uint height, Func<Window>? getWindowFunc)
#endif
	{
		_width = width;
		_height = height;

#if WINAPPSDK
		_nativeOpenGlWrapper = new WinUINativeOpenGLWrapper(getWindowFunc);
#else
		if (!ApiExtensibility.CreateInstance<INativeOpenGLWrapper>(this, out _nativeOpenGlWrapper!))
		{
			throw new InvalidOperationException($"Couldn't create a {nameof(INativeOpenGLWrapper)} object for {nameof(GLCanvasElement)}. Make sure you are running on a platform with {nameof(GLCanvasElement)} support.");
		}
#endif

		_backBuffer = new WriteableBitmap((int)width, (int)height);

		Background = new ImageBrush
		{
			ImageSource = _backBuffer,
			RelativeTransform = new ScaleTransform { ScaleX = 1, ScaleY = -1, CenterX = 0.5, CenterY = 0.5 } // because OpenGL coordinates go bottom-to-top
		};

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	/// <summary>
	/// Invalidates the rendering, and queues a call to <see cref="RenderOverride"/>.
	/// <see cref="RenderOverride"/> will only be called once after <see cref="Invalidate"/> and the output will
	/// be saved. You need to call <see cref="Invalidate"/> everytime an update is needed. If drawing an
	/// animation, call <see cref="Invalidate"/> inside <see cref="RenderOverride"/> to continuously invalidate and update.
	/// </summary>
#if WINAPPSDK
	public void Invalidate() => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Render);
#else // WPF hangs if we attempt to enqueue on Low inside RenderOverride
	public void Invalidate() => NativeDispatcher.Main.Enqueue(Render, NativeDispatcherPriority.Idle);
#endif

	private unsafe void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
	{
		_nativeOpenGlWrapper.CreateContext(this);
		_gl = (GL)_nativeOpenGlWrapper.CreateGLSilkNETHandle();

#if WINAPPSDK
		_pixels = Marshal.AllocHGlobal((int)(_width * _height * BytesPerPixel));
#endif

		using (new GLStateDisposable(this))
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

	private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
	{
		global::System.Diagnostics.Debug.Assert(_gl is not null); // because OnLoaded creates _gl

#if WINAPPSDK
		Marshal.FreeHGlobal(_pixels);
#endif

		using (new GLStateDisposable(this))
		{
#if WINAPPSDK
			if (WindowsRenderingNativeMethods.wglGetCurrentContext() == 0)
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
#if WINAPPSDK
		_pixels = default;
#endif
	}

	private unsafe void Render()
	{
		if (!IsLoaded)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(_gl is not null); // because _gl exists if loaded

		using var _ = new GLStateDisposable(this);

		_gl!.BindFramebuffer(GLEnum.Framebuffer, _framebuffer);
		{
			_gl.Viewport(new System.Drawing.Size((int)_width, (int)_height));

			RenderOverride(_gl);

			_gl.ReadBuffer(GLEnum.ColorAttachment0);

#if WINAPPSDK
			_gl.ReadPixels(0, 0, _width, _height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)_pixels);
			using (var stream = _backBuffer.PixelBuffer.AsStream())
			{
				stream.Write(new ReadOnlySpan<byte>((void*)_pixels, (int)(_width * _height * BytesPerPixel)));
			}
#else
			Buffer.Cast(_backBuffer.PixelBuffer).ApplyActionOnRawBufferPtr(ptr =>
			{
				_gl.ReadPixels(0, 0, _width, _height,
#if ANDROID
					GLEnum.Rgba,
#else
					GLEnum.Bgra,
#endif
					GLEnum.UnsignedByte, (void*)ptr);
			});
			_backBuffer.PixelBuffer.Length = _width * _height * BytesPerPixel;
#endif
			_backBuffer.Invalidate();
		}
	}

	private readonly struct GLStateDisposable : IDisposable
	{
		private readonly GLCanvasElement _glCanvasElement;
		private readonly IDisposable _contextDisposable;

		public GLStateDisposable(GLCanvasElement glCanvasElement)
		{
			_glCanvasElement = glCanvasElement;
			var gl = _glCanvasElement._gl;
			global::System.Diagnostics.Debug.Assert(gl is not null);

			_contextDisposable = _glCanvasElement._nativeOpenGlWrapper.MakeCurrent();
		}

		public void Dispose()
		{
			var gl = _glCanvasElement._gl;
			global::System.Diagnostics.Debug.Assert(gl is not null);

			_contextDisposable.Dispose();
		}
	}
}
