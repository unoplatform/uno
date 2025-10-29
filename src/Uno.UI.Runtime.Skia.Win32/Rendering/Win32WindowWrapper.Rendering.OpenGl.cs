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
		private readonly HWND _hwnd;
		private readonly HDC _hdc;
		private HGLRC _glContext; // recreated when window is extended into titlebar
		private readonly GRGlInterface _grGlInterface;
		private GRContext _grContext; // recreated when window is extended into titlebar
		private GRBackendRenderTarget? _renderTarget; // recreated on size updates
		private Win32Helper.WglCurrentContextDisposable? _paintDisposable;

		private GlRenderer(HWND hwnd, HDC hdc, HGLRC glContext, GRGlInterface grGlInterface, GRContext grContext)
		{
			_hwnd = hwnd;
			_hdc = hdc;
			_glContext = glContext;
			_grGlInterface = grGlInterface;
			_grContext = grContext;
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

			return new GlRenderer(hwnd, hdc, glContext, grGlInterface, grContext);
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
			return SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888); // BottomLeft to match GL's origin
		}

		void IRenderer.CopyPixels(int width, int height)
		{
			var success = PInvoke.SwapBuffers(_hdc);
			if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SwapBuffers)} failed: {Win32Helper.GetErrorMessage()}"); }
		}

		bool IRenderer.IsSoftware() => false;

		void IDisposable.Dispose()
		{
			ReleaseGlContext(_hwnd, _hdc, _glContext, _grGlInterface, _grContext, _renderTarget);
		}

		void IRenderer.OnWindowExtendedIntoTitleBar()
		{
			ReleaseGlContext(_hwnd, new HDC(IntPtr.Zero), _glContext, null, _grContext, _renderTarget);
			_glContext = PInvoke.wglCreateContext(_hdc);
			using var makeCurrentDisposable = new Win32Helper.WglCurrentContextDisposable(_hdc, _glContext);
			_grContext = GRContext.CreateGl(_grGlInterface);
		}
	}
}
