#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.Foundation.Logging;
using Windows.Graphics.Display;
using WpfControl = global::System.Windows.Controls.Control;
using WinUI = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal partial class OpenGLWpfRenderer : IWpfRenderer
{
	private const SKColorType colorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.TopLeft;

	private readonly WpfControl _hostControl;
	private readonly IWpfHost _host;
	private DisplayInformation? _displayInformation;
	private nint _hwnd;
	private nint _hdc;
	private nint _glContext;
	private GRContext? _grContext;
	private SKSurface? _surface;
	private GRBackendRenderTarget? _renderTarget;
	private WriteableBitmap? _backBuffer;

	public OpenGLWpfRenderer(IWpfHost host)
	{
		_hostControl = host as WpfControl ?? throw new InvalidOperationException("Host should be a WPF control");
		_host = host;
	}

	public void Initialize()
	{
		// Get the window from the wpf control
		_hwnd = new WindowInteropHelper(Window.GetWindow(_hostControl)).Handle;

		// Get the device context for the window
		_hdc = NativeMethods.GetDC(_hwnd);

		// Set the pixel format
		NativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = NativeMethods.PFD_DRAW_TO_WINDOW | NativeMethods.PFD_SUPPORT_OPENGL | NativeMethods.PFD_DOUBLEBUFFER;
		pfd.iPixelType = NativeMethods.PFD_TYPE_RGBA;
		pfd.cColorBits = 24;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 0;
		pfd.cDepthBits = 16;
		pfd.iLayerType = NativeMethods.PFD_MAIN_PLANE;

		// Choose the best matching pixel format
		var pixelFormat = NativeMethods.ChoosePixelFormat(_hdc, ref pfd);

		if (pixelFormat == 0)
		{
			throw new Exception("ChoosePixelFormat failed!");
		}

		// Set the pixel format for the device context
		if (NativeMethods.SetPixelFormat(_hdc, pixelFormat, ref pfd) == 0)
		{
			throw new Exception("SetPixelFormat failed!");
		}

		// Create the OpenGL context
		_glContext = NativeMethods.wglCreateContext(_hdc);

		if (_glContext == IntPtr.Zero)
		{
			throw new Exception("wglCreateContext failed!");
		}

		NativeMethods.wglMakeCurrent(_hdc, _glContext);

		var version = NativeMethods.GetOpenGLVersion();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OpenGL Version: {version}");
		}
	}

	public void Render(DrawingContext drawingContext)
	{
		if (_hostControl.ActualWidth == 0
				|| _hostControl.ActualHeight == 0
				|| double.IsNaN(_hostControl.ActualWidth)
				|| double.IsNaN(_hostControl.ActualHeight)
				|| double.IsInfinity(_hostControl.ActualWidth)
				|| double.IsInfinity(_hostControl.ActualHeight)
				|| _hostControl.Visibility != Visibility.Visible
				|| _hdc == 0
				|| _glContext == 0)
		{
			return;
		}

		int width, height;

		_displayInformation ??= DisplayInformation.GetForCurrentView();

		var dpi = _displayInformation.RawPixelsPerViewPixel;
		double dpiScaleX = dpi;
		double dpiScaleY = dpi;
		if (_host.IgnorePixelScaling)
		{
			width = (int)_hostControl.ActualWidth;
			height = (int)_hostControl.ActualHeight;
		}
		else
		{
			var matrix = PresentationSource.FromVisual(_hostControl).CompositionTarget.TransformToDevice;
			dpiScaleX = matrix.M11;
			dpiScaleY = matrix.M22;
			width = (int)(_hostControl.ActualWidth * dpiScaleX);
			height = (int)(_hostControl.ActualHeight * dpiScaleY);
		}

		NativeMethods.wglMakeCurrent(_hdc, _glContext);

		// create the contexts if not done already
		_grContext ??= TryBuildGRContext();

		if (_renderTarget == null || _surface == null || _renderTarget.Width != width || _renderTarget.Height != height)
		{
			// create or update the dimensions
			_renderTarget?.Dispose();

			var (framebuffer, stencil, samples) = GetGLBuffers();
			var maxSamples = _grContext.GetMaxSurfaceSampleCount(colorType);

			if (samples > maxSamples)
			{
				samples = maxSamples;
			}

			var glInfo = new GRGlFramebufferInfo((uint)framebuffer, colorType.ToGlSizedFormat());

			_renderTarget = new GRBackendRenderTarget(width, height, samples, stencil, glInfo);

			// create the surface
			_surface?.Dispose();
			_surface = SKSurface.Create(_grContext, _renderTarget, surfaceOrigin, colorType);

			_backBuffer = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Recreate render surface {width}x{height} colorType:{colorType} sample:{samples}");
			}
		}

		NativeMethods.glClear(NativeMethods.GL_COLOR_BUFFER_BIT | NativeMethods.GL_STENCIL_BUFFER_BIT | NativeMethods.GL_DEPTH_BUFFER_BIT);

		var canvas = _surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(BackgroundColor);
			_surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));

			if (!_host.IsIsland)
			{
				WinUI.Window.Current.Compositor.Render(_surface);
			}
			else
			{
				if (_host.RootElement?.Visual != null)
				{
					WinUI.Window.Current.Compositor.RenderVisual(_surface, _host.RootElement?.Visual!);
				}
			}
		}

		// update the control
		canvas.Flush();

		// Copy the contents of the back buffer to the screen
		if (_backBuffer != null)
		{
			_backBuffer.Lock();
			NativeMethods.glReadPixels(0, 0, width, height, NativeMethods.GL_BGRA_EXT, NativeMethods.GL_UNSIGNED_BYTE, _backBuffer.BackBuffer);
			_backBuffer.AddDirtyRect(new Int32Rect(0, 0, width, height));
			_backBuffer.Unlock();

			drawingContext.DrawImage(_backBuffer, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
		}
	}

	private (int framebuffer, int stencil, int samples) GetGLBuffers()
	{
		NativeMethods.glGetIntegerv(NativeMethods.GL_FRAMEBUFFER_BINDING, out var framebuffer);
		NativeMethods.glGetIntegerv(NativeMethods.GL_STENCIL, out var stencil);
		NativeMethods.glGetIntegerv(NativeMethods.GL_SAMPLES, out var samples);

		return (framebuffer, stencil, samples);
	}

	private GRContext TryBuildGRContext()
		=> CreateGRGLContext();

	internal static GRContext CreateGRGLContext()
	{
		var glInterface = GRGlInterface.Create()
			?? throw new NotSupportedException($"OpenGL is not supported in this system");

		var context = GRContext.CreateGl(glInterface)
			?? throw new NotSupportedException($"OpenGL is not supported in this system (failed to create context)");

		return context;
	}

	public void Dispose()
	{
		// Cleanup resources
		NativeMethods.wglDeleteContext(_glContext);
		NativeMethods.ReleaseDC(_hwnd, _hdc);
	}

	public SKSize CanvasSize
		=> _backBuffer == null ? SKSize.Empty : new SKSize(_backBuffer.PixelWidth, _backBuffer.PixelHeight);

	public SKColor BackgroundColor { get; set; }
}
