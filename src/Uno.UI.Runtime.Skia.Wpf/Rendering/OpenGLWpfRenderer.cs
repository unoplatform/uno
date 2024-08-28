#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Helpers;
using WinUI = Microsoft.UI.Xaml;
using WpfControl = global::System.Windows.Controls.Control;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal partial class OpenGLWpfRenderer : IWpfRenderer
{
	private const SKColorType colorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.TopLeft;

	private readonly WpfControl _hostControl;
	private readonly IWpfXamlRootHost _host;
	private WinUI.XamlRoot? _xamlRoot;
	private nint _hwnd;
	private nint _hdc;
	private int _pixelFormat;
	private nint _glContext;
	private GRContext? _grContext;
	private SKSurface? _surface;
	private GRBackendRenderTarget? _renderTarget;
	private WriteableBitmap? _backBuffer;

	public OpenGLWpfRenderer(IWpfXamlRootHost host)
	{
		_hostControl = host as WpfControl ?? throw new InvalidOperationException("Host should be a WPF control");
		_host = host;
	}

	public SKColor BackgroundColor { get; set; }

	public bool TryInitialize()
	{
		// Get the window from the wpf control
		var window = Window.GetWindow(_hostControl);
		if (window is null)
		{
			throw new InvalidOperationException("The host control is not associated with any Window");
		}

		var hwnd = new WindowInteropHelper(window).EnsureHandle();
		if (hwnd == IntPtr.Zero)
		{
			throw new InvalidOperationException("HWND should be initialized");
		}

		if (hwnd == _hwnd)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Surface already initialized on the same window");
			}

			return true;
		}
		else
		{
			Release();
		}

		_hwnd = hwnd;

		// Get the device context for the window
		_hdc = WpfRenderingNativeMethods.GetDC(_hwnd);

		// Set the pixel format
		WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = WpfRenderingNativeMethods.PFD_DRAW_TO_WINDOW | WpfRenderingNativeMethods.PFD_SUPPORT_OPENGL | WpfRenderingNativeMethods.PFD_DOUBLEBUFFER;
		pfd.iPixelType = WpfRenderingNativeMethods.PFD_TYPE_RGBA;
		pfd.cColorBits = 32;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 8;
		pfd.cDepthBits = 16;
		pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
		pfd.iLayerType = WpfRenderingNativeMethods.PFD_MAIN_PLANE;

		// Choose the best matching pixel format
		_pixelFormat = WpfRenderingNativeMethods.ChoosePixelFormat(_hdc, ref pfd);

		// To inspect the chosen pixel format:
		// NativeMethods.PIXELFORMATDESCRIPTOR temp_pfd = default;
		// NativeMethods.DescribePixelFormat(_hdc, _pixelFormat, (uint)Marshal.SizeOf<NativeMethods.PIXELFORMATDESCRIPTOR>(), ref temp_pfd);

		if (_pixelFormat == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"ChoosePixelFormat failed");
			}

			return false;
		}

		// Set the pixel format for the device context
		if (WpfRenderingNativeMethods.SetPixelFormat(_hdc, _pixelFormat, ref pfd) == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"SetPixelFormat failed");
			}

			return false;
		}

		// Create the OpenGL context
		_glContext = WpfRenderingNativeMethods.wglCreateContext(_hdc);

		if (_glContext == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"wglCreateContext failed");
			}

			return false;
		}

#pragma warning disable CA1806 // Do not ignore method results
		WpfRenderingNativeMethods.wglMakeCurrent(_hdc, _glContext);
#pragma warning restore CA1806 // Do not ignore method results

		var version = WpfRenderingNativeMethods.GetOpenGLVersion();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OpenGL Version: {version}");
		}


#pragma warning disable CA1806 // Do not ignore method results
		WpfRenderingNativeMethods.wglMakeCurrent(_hdc, _glContext);
#pragma warning restore CA1806 // Do not ignore method results

		return TryCreateGRGLContext(out _grContext);
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
			|| _glContext == 0
			|| _grContext is null)
		{
			return;
		}

		int width, height;

		_xamlRoot ??= WpfManager.XamlRootMap.GetRootForHost((IWpfXamlRootHost)_hostControl) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");
		var dpi = _xamlRoot!.RasterizationScale;
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

#pragma warning disable CA1806 // Do not ignore method results
		WpfRenderingNativeMethods.wglMakeCurrent(_hdc, _glContext);
#pragma warning restore CA1806 // Do not ignore method results

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

			_backBuffer = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Recreate render surface {width}x{height} colorType:{colorType} sample:{samples}");
			}
		}

		WpfRenderingNativeMethods.glClear(WpfRenderingNativeMethods.GL_COLOR_BUFFER_BIT | WpfRenderingNativeMethods.GL_STENCIL_BUFFER_BIT | WpfRenderingNativeMethods.GL_DEPTH_BUFFER_BIT);

		var canvas = _surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(BackgroundColor);
			canvas.SetMatrix(SKMatrix.CreateScale((float)dpiScaleX, (float)dpiScaleY));

			if (_host.RootElement?.Visual is { } rootVisual)
			{
				SkiaRenderHelper.RenderRootVisualAndClearNativeAreas(width, height, rootVisual, _surface);
			}
		}

		// update the control
		canvas.Flush();

		// Copy the contents of the back buffer to the screen
		if (_backBuffer != null)
		{
			_backBuffer.Lock();
			WpfRenderingNativeMethods.glReadPixels(0, 0, width, height, WpfRenderingNativeMethods.GL_BGRA_EXT, WpfRenderingNativeMethods.GL_UNSIGNED_BYTE, _backBuffer.BackBuffer);
			_backBuffer.AddDirtyRect(new Int32Rect(0, 0, width, height));
			_backBuffer.Unlock();

			drawingContext.DrawImage(_backBuffer, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
		}
	}

	public void Dispose() => Release();

	private (int framebuffer, int stencil, int samples) GetGLBuffers()
	{
		WpfRenderingNativeMethods.glGetIntegerv(WpfRenderingNativeMethods.GL_FRAMEBUFFER_BINDING, out var framebuffer);
		WpfRenderingNativeMethods.glGetIntegerv(WpfRenderingNativeMethods.GL_STENCIL_BITS, out var stencil);
		WpfRenderingNativeMethods.glGetIntegerv(WpfRenderingNativeMethods.GL_SAMPLES, out var samples);

		return (framebuffer, stencil, samples);
	}

	private bool TryCreateGRGLContext(out GRContext? context)
	{
		context = null;

		var glInterface = GRGlInterface.Create();

		if (glInterface is null)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("OpenGL is not supported in this system (Cannot create GRGlInterface)");
			}

			return false;
		}

		context = GRContext.CreateGl(glInterface);

		if (context is null)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"OpenGL is not supported in this system (failed to create GRContext)");
			}

			return false;
		}

		return true;
	}

	private void Release()
	{
		// Cleanup resources
#pragma warning disable CA1806 // Do not ignore method results
		WpfRenderingNativeMethods.wglDeleteContext(_glContext);
		WpfRenderingNativeMethods.ReleaseDC(_hwnd, _hdc);
#pragma warning restore CA1806 // Do not ignore method results

		_glContext = 0;
		_hwnd = 0;
		_hdc = IntPtr.Zero;

		_grContext?.Dispose();
		_grContext = null;

		_renderTarget?.Dispose();
		_renderTarget = null;

		_surface?.Dispose();
		_surface = null;

		_backBuffer = null;
	}
}
