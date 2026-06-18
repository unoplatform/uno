using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.OpenGL;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private class GlRenderer : IRenderer
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate int WglSwapIntervalEXT(int interval);

		private readonly HWND _hwnd;
		private readonly HDC _hdc;
		private HGLRC _glContext; // recreated when window is extended into titlebar
		private readonly GRGlInterface _grGlInterface;
		private GRContext _grContext; // recreated when window is extended into titlebar
		private GRBackendRenderTarget? _renderTarget; // recreated on size updates
		private Win32Helper.WglCurrentContextDisposable? _paintDisposable;
		// Non-null only when honoring a fixed FrameRate (SetFrameRateAsScreenRefreshRate = false);
		// otherwise wglSwapInterval(1) blocks SwapBuffers at the display refresh and paces the loop.
		private readonly Win32RenderPacer? _pacer;

		// The GL back buffer is a double-buffered swapchain that is not preserved across SwapBuffers, so
		// the composition renders onto a persistent GPU layer (returned from UpdateSize as the surface to
		// draw on) that is blitted to the back buffer each frame in CopyPixels. This is what makes
		// damage-region rendering correct on the GL backend (mirrors the X11 OpenGL renderer).
		private SKSurface? _swapchainSurface; // owned here; wraps the GL framebuffer
		private SKSurface? _layer; // non-owning reference; the caller (Win32WindowWrapper) owns/disposes it as _surface

		private GlRenderer(HWND hwnd, HDC hdc, HGLRC glContext, GRGlInterface grGlInterface, GRContext grContext, Win32RenderPacer? pacer)
		{
			_hwnd = hwnd;
			_hdc = hdc;
			_glContext = glContext;
			_grGlInterface = grGlInterface;
			_grContext = grContext;
			_pacer = pacer;
		}

		public static unsafe GlRenderer? TryCreateGlRenderer(HWND hwnd)
		{
			var hdc = PInvoke.GetDC(hwnd);
			if (hdc == IntPtr.Zero)
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}");
				ReleaseGlContext(hwnd, hdc, HGLRC.Null, null, null, null);
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
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.ChoosePixelFormat)} failed: {Win32Helper.GetErrorMessage()}");
				ReleaseGlContext(hwnd, hdc, HGLRC.Null, null, null, null);
				return null;
			}

			if (typeof(GlRenderer).Log().IsDebugEnabled())
			{
				PIXELFORMATDESCRIPTOR chosenPfd = default;
				typeof(GlRenderer).LogDebug()?.Debug(
					PInvoke.DescribePixelFormat(hdc, pixelFormat, (uint)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(), &chosenPfd) == 0
						? $"{nameof(PInvoke.DescribePixelFormat)} failed: {Win32Helper.GetErrorMessage()}"
						: $"{nameof(PInvoke.ChoosePixelFormat)} chose a PFD with {chosenPfd.cColorBits} ColorBits {{ R{chosenPfd.cRedBits} G{chosenPfd.cGreenBits} B{chosenPfd.cBlueBits} A{chosenPfd.cAlphaBits} }}, {chosenPfd.cDepthBits} DepthBits and {chosenPfd.cStencilBits} StencilBits.");
			}

			// Set the pixel format for the device context
			if (!PInvoke.SetPixelFormat(hdc, pixelFormat, pfd))
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.SetPixelFormat)} failed: {Win32Helper.GetErrorMessage()}");
				ReleaseGlContext(hwnd, hdc, HGLRC.Null, null, null, null);
				return null;
			}

			// Create the OpenGL context
			var glContext = PInvoke.wglCreateContext(hdc);

			if (glContext == HGLRC.Null)
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.wglCreateContext)} failed: {Win32Helper.GetErrorMessage()}");
				ReleaseGlContext(hwnd, hdc, HGLRC.Null, null, null, null);
				return null;
			}

			using var makeCurrentDisposable = new Win32Helper.WglCurrentContextDisposable(hdc, glContext);

			if (typeof(GlRenderer).Log().IsDebugEnabled())
			{
				var version = PInvoke.glGetString(/* GL_VERSION */ 0x1F02);
				typeof(GlRenderer).LogDebug()?.Debug(
					version is null
						? $"{nameof(PInvoke.glGetString)} failed with error code {PInvoke.glGetError().ToString("X", CultureInfo.InvariantCulture)}"
						: $"OpenGL Version: {Marshal.PtrToStringUTF8((IntPtr)version)}");
			}

			var followRefreshRate = FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate;
			// Swap interval 1 blocks SwapBuffers at the refresh; for a fixed FrameRate use 0 and let
			// the timer pace the loop instead.
			SetSwapInterval(followRefreshRate ? 1 : 0);

			var grGlInterface = GRGlInterface.Create();

			if (grGlInterface is null)
			{
				typeof(GlRenderer).LogError()?.Error("OpenGL is not supported in this system (Cannot create GRGlInterface)");
				ReleaseGlContext(hwnd, hdc, glContext, null, null, null);
				return null;
			}

			if (GRContext.CreateGl(grGlInterface) is not { } grContext)
			{
				typeof(GlRenderer).LogError()?.Error("OpenGL is not supported in this system (failed to create GRContext)");
				ReleaseGlContext(hwnd, hdc, glContext, grGlInterface, null, null);
				return null;
			}

			// Detach the GL context from the calling thread so the render thread can make it
			// current later (WglCurrentContextDisposable doesn't restore to "no context").
			if (!PInvoke.wglMakeCurrent(default, HGLRC.Null))
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.wglMakeCurrent)} (detach) failed: {Win32Helper.GetErrorMessage()}");
			}

			var pacer = followRefreshRate
				? null
				: new Win32RenderPacer(FeatureConfiguration.CompositionTarget.FrameRate, followRefreshRate: false);
			return new GlRenderer(hwnd, hdc, glContext, grGlInterface, grContext, pacer);
		}

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
					typeof(GlRenderer).LogWarn()?.Warn(
						$"Failed to set GL swap interval {interval} via wglSwapIntervalEXT; the render loop may run unthrottled on this driver.");
				}
			}
		}

		private static void ReleaseGlContext(HWND hwnd, HDC hdc, HGLRC glContext, GRGlInterface? grGlInterface, GRContext? grContext, GRBackendRenderTarget? renderTarget)
		{
			renderTarget?.Dispose();
			grContext?.Dispose();
			grGlInterface?.Dispose();

			if (glContext != HGLRC.Null)
			{
				var success = PInvoke.wglDeleteContext(glContext);
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.wglDeleteContext)} failed: {Win32Helper.GetErrorMessage()}"); }
			}

			if (hdc != new HDC(IntPtr.Zero))
			{
				var success = PInvoke.ReleaseDC(hwnd, hdc) == 1;
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			}
		}

		void IRenderer.StartPaint()
		{
			Debug.Assert(_paintDisposable == null);
			_paintDisposable = new Win32Helper.WglCurrentContextDisposable(_hdc, _glContext);
		}

		void IRenderer.EndPaint()
		{
			Debug.Assert(_paintDisposable != null);
			_paintDisposable?.Dispose();
			_paintDisposable = null;
		}

		SKSurface IRenderer.UpdateSize(int width, int height)
		{
			using var makeCurrentDisposable = new Win32Helper.WglCurrentContextDisposable(_hdc, _glContext);

			int framebuffer = default, stencil = default, samples = default;
			PInvoke.glGetIntegerv(/* GL_FRAMEBUFFER_BINDING */ 0x8CA6, ref framebuffer);
			PInvoke.glGetIntegerv(/* GL_STENCIL_BITS */ 0x0D57, ref stencil);
			PInvoke.glGetIntegerv(/* GL_SAMPLES */ 0x80A9, ref samples);

			_renderTarget?.Dispose();
			_renderTarget = new GRBackendRenderTarget(width, height, samples, stencil, new GRGlFramebufferInfo((uint)framebuffer, SKColorType.Rgba8888.ToGlSizedFormat()));

			_swapchainSurface?.Dispose();
			_swapchainSurface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888); // BottomLeft to match GL's origin

			// Composition renders onto the retained layer; CopyPixels blits it to the back buffer above.
			var info = new SKImageInfo(Math.Max(1, width), Math.Max(1, height), SKColorType.Rgba8888, SKAlphaType.Premul);
			var layer = SKSurface.Create(_grContext, budgeted: true, info)
				?? throw new InvalidOperationException("Failed to create the damage-region retained layer surface.");
			layer.Canvas.Clear(SKColors.Transparent);
			_layer = layer;
			return layer;
		}

		void IRenderer.CopyPixels(int width, int height)
		{
			_pacer?.OnFrameStart();

			// The GL context is current for the whole frame (StartPaint..EndPaint). Blit the retained layer
			// onto the (non-retaining) back buffer, then present.
			if (_layer is { } layer && _swapchainSurface is { } swapchain)
			{
				layer.Draw(swapchain.Canvas, 0, 0, null);
				swapchain.Canvas.Flush();
			}

			var success = PInvoke.SwapBuffers(_hdc);
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SwapBuffers)} failed: {Win32Helper.GetErrorMessage()}"); }

			// Fixed-FrameRate path: SwapBuffers ran with swap interval 0 (non-blocking), so pace the
			// loop with the timer. When following the refresh, swap interval 1 already blocked above.
			_pacer?.WaitForNextFrame();
		}

		bool IRenderer.IsSoftware() => false;

		void IDisposable.Dispose()
		{
			_pacer?.Dispose();
			// _layer is owned by the caller (disposed as _surface); only dispose what we own here.
			_swapchainSurface?.Dispose();
			_swapchainSurface = null;
			ReleaseGlContext(_hwnd, _hdc, _glContext, _grGlInterface, _grContext, _renderTarget);
		}

		void IRenderer.Reinitialize()
		{
			_swapchainSurface?.Dispose();
			_swapchainSurface = null;
			_layer = null; // owned and already disposed by the caller (as _surface)
			ReleaseGlContext(_hwnd, new HDC(IntPtr.Zero), _glContext, null, _grContext, _renderTarget);
			// ReleaseGlContext disposed the render target; null it so the next UpdateSize (which
			// recreates it) doesn't dispose the same instance again.
			_renderTarget = null;
			_glContext = PInvoke.wglCreateContext(_hdc);
			if (_glContext == HGLRC.Null)
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(PInvoke.wglCreateContext)} failed during {nameof(IRenderer.Reinitialize)}: {Win32Helper.GetErrorMessage()}");
				return;
			}
			using var makeCurrentDisposable = new Win32Helper.WglCurrentContextDisposable(_hdc, _glContext);
			SetSwapInterval(_pacer is null ? 1 : 0);
			if (GRContext.CreateGl(_grGlInterface) is not { } grContext)
			{
				typeof(GlRenderer).LogError()?.Error($"{nameof(GRContext)}.{nameof(GRContext.CreateGl)} failed during {nameof(IRenderer.Reinitialize)}.");
				return;
			}
			_grContext = grContext;
		}

		// Following the refresh: swap interval 1 paces SwapBuffers, nothing to retarget. Fixed
		// FrameRate uses a static timer rate. UpdateRefreshRate only fires in the former case.
		void IRenderer.UpdateRefreshRate(double fps) { }
	}
}
