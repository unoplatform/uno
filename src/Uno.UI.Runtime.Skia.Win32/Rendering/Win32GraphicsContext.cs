#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.OpenGL;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Host contexts whose present is timer-paced (software BitBlt; GL under a fixed FrameRate) can be retargeted
/// when the screen refresh rate changes. The WebGPU swapchain context does not implement this (the swapchain
/// paces itself), so <c>Win32WindowWrapper.DisplayInformation</c>'s <see langword="is"/> check simply skips it.
/// </summary>
internal interface IWin32PacedContext
{
	void UpdateRefreshRate(double fps);
}

/// <summary>
/// Neutral OpenGL <see cref="ISwapChain"/> for Win32 — owns the WGL context + <c>SwapBuffers</c> and names
/// no Skia type. <see cref="AcquireRenderTarget"/> makes the context current (the render happens on the render
/// thread while current) and hands the backend a neutral <see cref="IGLRenderTarget"/> (default framebuffer +
/// sample/stencil); the Skia backend builds its GRContext-GL against the current context. <see cref="Present"/>
/// swaps and releases current. Created by <see cref="Win32GraphicsContextFactory"/>; returns <see langword="null"/>
/// on failure so negotiation falls through to the software context.
/// </summary>
internal sealed class Win32OpenGLGraphicsContext : ISwapChain, IWin32PacedContext, IGLDeviceContext
{
	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate int WglSwapIntervalEXT(int interval);

	private readonly HWND _hwnd;
	private readonly HDC _hdc;
	private readonly HGLRC _glContext;
	// Non-null only when honoring a fixed FrameRate (SetFrameRateAsScreenRefreshRate = false); otherwise
	// wglSwapInterval(1) blocks SwapBuffers at the display refresh and paces the loop.
	private readonly Win32RenderPacer? _pacer;

	private Win32OpenGLGraphicsContext(HWND hwnd, HDC hdc, HGLRC glContext, Win32RenderPacer? pacer)
	{
		_hwnd = hwnd;
		_hdc = hdc;
		_glContext = glContext;
		_pacer = pacer;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.OpenGL;

	public GLFlavor Flavor => GLFlavor.OpenGL;
	public Func<string, nint> GetProcAddress => Win32NativeOpenGLWrapper.GetProcAddressStatic;

	public static unsafe Win32OpenGLGraphicsContext? TryCreate(HWND hwnd)
	{
		var hdc = PInvoke.GetDC(hwnd);
		if (hdc == IntPtr.Zero)
		{
			typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}");
			ReleaseGlContext(hwnd, hdc, HGLRC.Null);
			return null;
		}

		PIXELFORMATDESCRIPTOR pfd = new()
		{
			nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
			nVersion = 1,
			dwFlags = PFD_FLAGS.PFD_DRAW_TO_WINDOW | PFD_FLAGS.PFD_SUPPORT_OPENGL | PFD_FLAGS.PFD_DOUBLEBUFFER,
			iPixelType = PFD_PIXEL_TYPE.PFD_TYPE_RGBA,
			cColorBits = 32,
			cRedBits = 8,
			cGreenBits = 8,
			cBlueBits = 8,
			cAlphaBits = 8,
			cDepthBits = 16,
			cStencilBits = 1 // anything > 0 is fine, we will most likely get 8
		};

		// Choose the best matching pixel format
		var pixelFormat = PInvoke.ChoosePixelFormat(hdc, pfd);

		if (pixelFormat == 0)
		{
			typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.ChoosePixelFormat)} failed: {Win32Helper.GetErrorMessage()}");
			ReleaseGlContext(hwnd, hdc, HGLRC.Null);
			return null;
		}

		if (typeof(Win32OpenGLGraphicsContext).Log().IsDebugEnabled())
		{
			PIXELFORMATDESCRIPTOR chosenPfd = default;
			typeof(Win32OpenGLGraphicsContext).LogDebug()?.Debug(
				PInvoke.DescribePixelFormat(hdc, pixelFormat, (uint)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(), &chosenPfd) == 0
					? $"{nameof(PInvoke.DescribePixelFormat)} failed: {Win32Helper.GetErrorMessage()}"
					: $"{nameof(PInvoke.ChoosePixelFormat)} chose a PFD with {chosenPfd.cColorBits} ColorBits {{ R{chosenPfd.cRedBits} G{chosenPfd.cGreenBits} B{chosenPfd.cBlueBits} A{chosenPfd.cAlphaBits} }}, {chosenPfd.cDepthBits} DepthBits and {chosenPfd.cStencilBits} StencilBits.");
		}

		// Set the pixel format for the device context
		if (!PInvoke.SetPixelFormat(hdc, pixelFormat, pfd))
		{
			typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.SetPixelFormat)} failed: {Win32Helper.GetErrorMessage()}");
			ReleaseGlContext(hwnd, hdc, HGLRC.Null);
			return null;
		}

		// Create the OpenGL context
		var glContext = PInvoke.wglCreateContext(hdc);

		if (glContext == HGLRC.Null)
		{
			typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.wglCreateContext)} failed: {Win32Helper.GetErrorMessage()}");
			ReleaseGlContext(hwnd, hdc, HGLRC.Null);
			return null;
		}

		using var makeCurrentDisposable = new Win32Helper.WglCurrentContextDisposable(hdc, glContext);

		var versionPtr = PInvoke.glGetString(/* GL_VERSION */ 0x1F02);
		var versionString = versionPtr is null ? null : Marshal.PtrToStringUTF8((IntPtr)versionPtr);

		if (typeof(Win32OpenGLGraphicsContext).Log().IsDebugEnabled())
		{
			typeof(Win32OpenGLGraphicsContext).LogDebug()?.Debug(
				versionString is null
					? $"{nameof(PInvoke.glGetString)} failed with error code {PInvoke.glGetError().ToString("X", CultureInfo.InvariantCulture)}"
					: $"OpenGL Version: {versionString}");
		}

		// The renderer's GL backend needs OpenGL 2.0+. When the session has no usable GPU driver (common under RDP,
		// some VMs, or a fresh/headless Windows install), wglCreateContext still succeeds but yields the Microsoft
		// software rasterizer reporting version "1.1" — which GRGlInterface can't consume. DECLINE the OpenGL kind
		// here (return null) so negotiation falls through to the software context, instead of committing to a GL
		// context that then throws "GRGlInterface create failed" on every frame of the render loop.
		if (!IsUsableGlVersion(versionString))
		{
			typeof(Win32OpenGLGraphicsContext).LogInfo()?.Info(
				$"OpenGL version '{versionString ?? "(unknown)"}' is below the 2.0 the renderer requires (likely the software fallback with no GPU driver); declining OpenGL so a software context is used instead.");
			_ = PInvoke.wglMakeCurrent(default, HGLRC.Null);
			ReleaseGlContext(hwnd, hdc, glContext);
			return null;
		}

		var followRefreshRate = FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate;
		// Swap interval 1 blocks SwapBuffers at the refresh; for a fixed FrameRate use 0 and let
		// the timer pace the loop instead.
		SetSwapInterval(followRefreshRate ? 1 : 0);

		// Detach the GL context from the calling thread so the render thread can make it
		// current later (WglCurrentContextDisposable doesn't restore to "no context").
		if (!PInvoke.wglMakeCurrent(default, HGLRC.Null))
		{
			typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} (detach) failed: {Win32Helper.GetErrorMessage()}");
		}

		var pacer = followRefreshRate
			? null
			: new Win32RenderPacer(FeatureConfiguration.CompositionTarget.FrameRate, followRefreshRate: false);
		return new Win32OpenGLGraphicsContext(hwnd, hdc, glContext, pacer);
	}

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		// Make the GL context current on the render thread; it stays current through the backend's draw and
		// is released in Present. Read the default framebuffer + its sample/stencil counts and hand a neutral
		// target; the Skia backend builds GRContext-GL against the current context.
		if (!PInvoke.wglMakeCurrent(_hdc, _glContext))
		{
			this.LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} failed: {Win32Helper.GetErrorMessage()}");
		}

		int framebuffer = default, stencil = default, samples = default;
		PInvoke.glGetIntegerv(/* GL_FRAMEBUFFER_BINDING */ 0x8CA6, ref framebuffer);
		PInvoke.glGetIntegerv(/* GL_STENCIL_BITS */ 0x0D57, ref stencil);
		PInvoke.glGetIntegerv(/* GL_SAMPLES */ 0x80A9, ref samples);

		return new Win32GLRenderTarget((uint)framebuffer, samples, stencil, Math.Max(1, width), Math.Max(1, height));
	}

	public void Present()
	{
		_pacer?.OnFrameStart();

		var success = PInvoke.SwapBuffers(_hdc);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SwapBuffers)} failed: {Win32Helper.GetErrorMessage()}"); }

		// Fixed-FrameRate path: SwapBuffers ran with swap interval 0 (non-blocking), so pace the
		// loop with the timer. When following the refresh, swap interval 1 already blocked above.
		_pacer?.WaitForNextFrame();

		// Release current so the context is free for the next frame's make-current.
		if (!PInvoke.wglMakeCurrent(default, HGLRC.Null))
		{
			this.LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} (detach) failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	// Following the refresh: swap interval 1 paces SwapBuffers, nothing to retarget. Fixed
	// FrameRate uses a static timer rate. UpdateRefreshRate only fires in the former case.
	public void UpdateRefreshRate(double fps) { }

	// Sets the GL swap interval: 1 blocks SwapBuffers until the next display refresh (vsync),
	// 0 doesn't block (used when a fixed FrameRate is paced by the timer instead). Some drivers
	// default to 0, letting the render loop spin. Per-context, so re-apply whenever an HGLRC
	// is created.
	private static void SetSwapInterval(int interval)
	{
		var wglSwapIntervalAddr = PInvoke.wglGetProcAddress("wglSwapIntervalEXT");
		if (wglSwapIntervalAddr != IntPtr.Zero)
		{
			var wglSwapInterval = Marshal.GetDelegateForFunctionPointer<WglSwapIntervalEXT>(wglSwapIntervalAddr);
			if (wglSwapInterval(interval) == 0)
			{
				typeof(Win32OpenGLGraphicsContext).LogWarn()?.Warn(
					$"Failed to set GL swap interval {interval} via wglSwapIntervalEXT; the render loop may run unthrottled on this driver.");
			}
		}
	}

	// GL_VERSION starts with "<major>.<minor>[…]" (desktop WGL has no "OpenGL ES" prefix). The renderer's GL
	// backend needs major >= 2; the Microsoft software fallback reports "1.1.0". A null/unparseable string is
	// treated as unusable so we decline rather than commit to a context we can't verify.
	private static bool IsUsableGlVersion(string? version)
	{
		if (string.IsNullOrEmpty(version))
		{
			return false;
		}

		var dot = version.IndexOf('.');
		var major = dot > 0 ? version.AsSpan(0, dot) : version.AsSpan();
		return int.TryParse(major, out var majorVersion) && majorVersion >= 2;
	}

	private static void ReleaseGlContext(HWND hwnd, HDC hdc, HGLRC glContext)
	{
		if (glContext != HGLRC.Null)
		{
			var success = PInvoke.wglDeleteContext(glContext);
			if (!success) { typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.wglDeleteContext)} failed: {Win32Helper.GetErrorMessage()}"); }
		}

		if (hdc != new HDC(IntPtr.Zero))
		{
			var success = PInvoke.ReleaseDC(hwnd, hdc) == 1;
			if (!success) { typeof(Win32OpenGLGraphicsContext).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}

	public void Dispose()
	{
		_pacer?.Dispose();
		ReleaseGlContext(_hwnd, _hdc, _glContext);
	}

	// BottomLeft origin (GL) is handled by the backend for an IGLRenderTarget.
	private sealed class Win32GLRenderTarget(uint framebufferId, int sampleCount, int stencilBits, int width, int height) : IGLRenderTarget
	{
		public uint FramebufferId => framebufferId;
		public int SampleCount => sampleCount;
		public int StencilBits => stencilBits;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		// Retained-layer partial repaint: the backend blits a persistent layer here each present (see X11GLRenderTarget).
		public bool PreservesContents => true;
		public void Dispose() { }
	}
}

/// <summary>
/// Neutral software (CPU-framebuffer) <see cref="ISwapChain"/> for Win32 — owns the DIB section + the
/// <c>BitBlt</c> present, and names no Skia type. <see cref="AcquireRenderTarget"/> (re)creates the DIB on resize
/// and hands the backend a neutral <see cref="ISoftwareRenderTarget"/>; the Skia backend wraps it as its surface.
/// </summary>
internal sealed class Win32SoftwareGraphicsContext : ISwapChain, IWin32PacedContext
{
	private readonly HWND _hwnd;
	private readonly Win32RenderPacer _pacer;

	private HBITMAP _hBitmap;
	private nint _bits;
	private int _width;
	private int _height;

	public Win32SoftwareGraphicsContext(HWND hwnd)
	{
		_hwnd = hwnd;
		// BitBlt returns instantly, so the loop is paced here: to the display refresh when
		// SetFrameRateAsScreenRefreshRate is on, otherwise to the configured FrameRate.
		_pacer = new Win32RenderPacer(
			FeatureConfiguration.CompositionTarget.FrameRate,
			FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate);
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public unsafe IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_hBitmap == HBITMAP.Null || width != _width || height != _height)
		{
			if (_hBitmap != HBITMAP.Null)
			{
				var deleted = PInvoke.DeleteObject(_hBitmap) == 1;
				if (!deleted) { typeof(Win32SoftwareGraphicsContext).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
			}

			BITMAPINFO bitmapinfo = new BITMAPINFO
			{
				bmiHeader = new BITMAPINFOHEADER
				{
					biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
					biWidth = width,
					biHeight = -height, // the negative is to deal with bottom-up coords
					biPlanes = 1,
					biBitCount = 32,
					biCompression = /* BI_RGB */ 0x0000,
				}
			};
			void* bits;
			_hBitmap = PInvoke.CreateDIBSection(new HDC(IntPtr.Zero), &bitmapinfo, DIB_USAGE.DIB_RGB_COLORS, &bits, HANDLE.Null, 0);
			if (_hBitmap == HBITMAP.Null)
			{
				throw new InvalidOperationException($"{nameof(PInvoke.CreateDIBSection)} failed: {Win32Helper.GetErrorMessage()}");
			}
			_bits = (nint)bits;
			_width = width;
			_height = height;
		}

		// Hand the CPU framebuffer to the backend as a neutral target; the Skia backend wraps it as its surface.
		return new Win32SoftwareRenderTarget(_bits, _width * 4, _width, _height);
	}

	public void Present()
	{
		_pacer.OnFrameStart();

		var paintDc = PInvoke.GetDC(_hwnd);
		if (paintDc == new HDC(IntPtr.Zero))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}
		using var endPaintDisposable = new DisposableStruct<HWND, HDC>(static (hwnd, lpPaint) =>
		{
			var success = PInvoke.ReleaseDC(hwnd, lpPaint) == 1;
			if (!success) { typeof(Win32SoftwareGraphicsContext).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
		}, _hwnd, paintDc);

		var bitmapDc = PInvoke.CreateCompatibleDC(paintDc);
		if (bitmapDc == new HDC(IntPtr.Zero))
		{
			this.LogError()?.Error($"{nameof(PInvoke.CreateCompatibleDC)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}
		using var bitmapDcDisposable = new DisposableStruct<HDC>(static bitmapDc =>
		{
			var success = PInvoke.DeleteDC(bitmapDc);
			if (!success) { typeof(Win32SoftwareGraphicsContext).LogError()?.Error($"{nameof(PInvoke.DeleteDC)} failed: {Win32Helper.GetErrorMessage()}"); }
		}, bitmapDc);

		if (PInvoke.SelectObject(bitmapDc, _hBitmap) == 0)
		{
			this.LogError()?.Error($"{nameof(PInvoke.SelectObject)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var success2 = PInvoke.BitBlt(paintDc, 0, 0, _width, _height, bitmapDc, 0, 0, ROP_CODE.SRCCOPY);
		if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.BitBlt)} failed: {Win32Helper.GetErrorMessage()}"); }

		// BitBlt returns instantly, so block until the compositor's next vsync to pace the loop.
		_pacer.WaitForNextFrame();
	}

	public void UpdateRefreshRate(double fps) => _pacer.UpdateTargetFps(fps);

	public void Dispose()
	{
		_pacer.Dispose();
		if (_hBitmap != HBITMAP.Null)
		{
			var success = PInvoke.DeleteObject(_hBitmap) == 1;
			if (!success) { typeof(Win32SoftwareGraphicsContext).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}

	private sealed class Win32SoftwareRenderTarget(nint pixels, int rowBytes, int width, int height) : ISoftwareRenderTarget
	{
		public nint Pixels => pixels;
		public int RowBytes => rowBytes;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}

