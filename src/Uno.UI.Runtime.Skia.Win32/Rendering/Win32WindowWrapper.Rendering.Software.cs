using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private class SoftwareRenderer : IRenderer
	{
		private readonly HWND _hwnd;
		private readonly Win32RenderPacer _pacer;

		private HBITMAP _hBitmap;

		public SoftwareRenderer(HWND hwnd)
		{
			_hwnd = hwnd;
			// BitBlt returns instantly, so the loop is paced here: to the display refresh when
			// SetFrameRateAsScreenRefreshRate is on, otherwise to the configured FrameRate.
			_pacer = new Win32RenderPacer(
				FeatureConfiguration.CompositionTarget.FrameRate,
				FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate);
		}

		public void StartPaint() { }
		public void EndPaint() { }

		// The DIB section backing the surface persists between frames (only recreated on resize) and is
		// blitted out by CopyPixels, so it retains the previous frame and the present can be clipped.
		bool IRenderer.SurfaceRetainsContents => true;

		unsafe SKSurface IRenderer.UpdateSize(int width, int height)
		{
			if (_hBitmap != HBITMAP.Null)
			{
				var success = PInvoke.DeleteObject(_hBitmap) == 1;
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
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

			return SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul), (IntPtr)bits);
		}

		void IRenderer.CopyPixels(int width, int height)
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
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
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
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			}, bitmapDc);

			if (PInvoke.SelectObject(bitmapDc, _hBitmap) == 0)
			{
				this.LogError()?.Error($"{nameof(PInvoke.SelectObject)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			var success2 = PInvoke.BitBlt(paintDc, 0, 0, width, height, bitmapDc, 0, 0, ROP_CODE.SRCCOPY);
			if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.BitBlt)} failed: {Win32Helper.GetErrorMessage()}"); }

			// BitBlt returns instantly, so block until the compositor's next vsync to pace the loop.
			_pacer.WaitForNextFrame();
		}

		bool IRenderer.IsSoftware() => true;

		void IRenderer.Reinitialize() { }

		void IRenderer.UpdateRefreshRate(double fps) => _pacer.UpdateTargetFps(fps);

		void IDisposable.Dispose()
		{
			_pacer.Dispose();
			var success = PInvoke.DeleteObject(_hBitmap) == 1;
			if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}
}
