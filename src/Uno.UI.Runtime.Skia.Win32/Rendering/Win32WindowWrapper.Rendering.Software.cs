using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private class SoftwareRenderer(HWND hwnd) : IRenderer
	{
		// Pacing fallback when DwmFlush hangs/fails repeatedly (DWM paused, RDP reconnect,
		// fast user switch, GPU TDR). After this many consecutive failures we stop calling
		// DwmFlush and use a fixed sleep to keep the render loop bounded.
		private const int DwmFlushFailureThreshold = 3;
		private const int FallbackPacingMs = 16; // ~60 Hz

		private HBITMAP _hBitmap;
		private int _consecutiveDwmFlushFailures;
		private bool _dwmFlushDegraded;

		public void StartPaint() { }
		public void EndPaint() { }

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
			var paintDc = PInvoke.GetDC(hwnd);
			if (paintDc == new HDC(IntPtr.Zero))
			{
				this.LogError()?.Error($"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}
			using var endPaintDisposable = new DisposableStruct<HWND, HDC>(static (hwnd, lpPaint) =>
			{
				var success = PInvoke.ReleaseDC(hwnd, lpPaint) == 1;
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			}, hwnd, paintDc);

			var bitmapDc = PInvoke.CreateCompatibleDC(paintDc);
			if (bitmapDc == new HDC(IntPtr.Zero))
			{
				this.LogError()?.Error($"{nameof(PInvoke.CreateCompatibleDC)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}
			using var bitmapDcDisposable = new DisposableStruct<HDC>(static bitmapDc =>
			{
				var success = PInvoke.DeleteObject(new HGDIOBJ(bitmapDc.Value)) == 1;
				if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }
			}, bitmapDc);

			if (PInvoke.SelectObject(bitmapDc, _hBitmap) == 0)
			{
				this.LogError()?.Error($"{nameof(PInvoke.SelectObject)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			var success2 = PInvoke.BitBlt(paintDc, 0, 0, width, height, bitmapDc, 0, 0, ROP_CODE.SRCCOPY);
			if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.BitBlt)} failed: {Win32Helper.GetErrorMessage()}"); }

			// Block until the DWM compositor's next VSync to pace frames.
			// Without this, BitBlt returns instantly (unlike GL's SwapBuffers
			// which blocks for VSync), causing the render loop to spin at
			// hundreds of fps — wasting CPU and reporting misleading frame rates.
			//
			// Fallback: after DwmFlushFailureThreshold consecutive failures (DWM hung
			// during RDP reconnect, fast user switch, or a GPU TDR), give up on
			// DwmFlush and use a fixed sleep. We never re-enable DwmFlush in this
			// renderer instance — once degraded, stay degraded for the lifetime of
			// the window. Better to lose vsync alignment than to risk hanging the
			// render thread on each frame.
			if (_dwmFlushDegraded)
			{
				Thread.Sleep(FallbackPacingMs);
				return;
			}

			var dwmFlushResult = PInvoke.DwmFlush();
			if (dwmFlushResult.Failed)
			{
				_consecutiveDwmFlushFailures++;
				this.LogError()?.Error($"{nameof(PInvoke.DwmFlush)} failed: {dwmFlushResult} (failure {_consecutiveDwmFlushFailures}/{DwmFlushFailureThreshold})");

				if (_consecutiveDwmFlushFailures >= DwmFlushFailureThreshold)
				{
					_dwmFlushDegraded = true;
					this.LogWarn()?.Warn(
						$"{nameof(PInvoke.DwmFlush)} failed {DwmFlushFailureThreshold} times consecutively; " +
						$"falling back to {FallbackPacingMs} ms sleep-based pacing. " +
						"Frame timing will not be vsync-aligned for the rest of this window's lifetime.");
				}

				// Pace the failing call so we don't spin at full speed.
				Thread.Sleep(FallbackPacingMs);
			}
			else
			{
				_consecutiveDwmFlushFailures = 0;
			}
		}

		bool IRenderer.IsSoftware() => true;

		void IRenderer.Reinitialize() { }

		void IDisposable.Dispose()
		{
			var success = PInvoke.DeleteObject(_hBitmap) == 1;
			if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}
}
