using System;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private class SoftwareRenderer : IRenderer
	{
		// Pacing fallback when DwmFlush hangs/fails repeatedly (DWM paused, RDP reconnect,
		// fast user switch, GPU TDR). After this many consecutive failures we stop calling
		// DwmFlush and pace via FramePacer instead.
		private const int DwmFlushFailureThreshold = 3;

		private readonly HWND _hwnd;
		private readonly FramePacer _framePacer;
		private readonly AutoResetEvent _frameDeadlineReached = new(false);

		private HBITMAP _hBitmap;
		private int _consecutiveDwmFlushFailures;
		private bool _dwmFlushDegraded;

		public SoftwareRenderer(HWND hwnd)
		{
			_hwnd = hwnd;
			// Self-correcting absolute-time pacer used as the fallback when DwmFlush is degraded.
			// Initialized at FeatureConfiguration.CompositionTarget.FrameRate; retargeted to the
			// screen refresh rate via UpdateRefreshRate when SetFrameRateAsScreenRefreshRate is on.
			_framePacer = new FramePacer(
				FeatureConfiguration.CompositionTarget.FrameRate,
				() => _frameDeadlineReached.Set());
		}

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
			// Anchor the absolute target tick at the start of every frame so a subsequent
			// FramePacer.RequestFrame schedules wake-up at the next deadline (no drift).
			_framePacer.OnFrameStart();

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

			// Pace the frame. Normally DwmFlush blocks until the DWM compositor's next VSync
			// (BitBlt itself returns instantly, so without pacing the render loop spins at
			// hundreds of fps — wasting CPU and reporting misleading frame rates).
			//
			// Fallback: after DwmFlushFailureThreshold consecutive failures (DWM hung during
			// RDP reconnect, fast user switch, or a GPU TDR), give up on DwmFlush and pace via
			// FramePacer's self-correcting absolute-time scheduling. We never re-enable
			// DwmFlush in this renderer instance — once degraded, stay degraded for the
			// lifetime of the window. Better to lose vsync alignment than to risk hanging the
			// render thread on each frame.
			var paceViaFramePacer = _dwmFlushDegraded;

			if (!_dwmFlushDegraded)
			{
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
							$"falling back to FramePacer-driven pacing at {_framePacer.TargetIntervalMs:F1} ms. " +
							"Frame timing will not be vsync-aligned for the rest of this window's lifetime.");
					}

					// Pace this failed frame via FramePacer; subsequent frames will also use the
					// degraded branch above and pace the same way.
					paceViaFramePacer = true;
				}
				else
				{
					_consecutiveDwmFlushFailures = 0;
				}
			}

			if (paceViaFramePacer)
			{
				_framePacer.RequestFrame();
				_frameDeadlineReached.WaitOne();
			}
		}

		bool IRenderer.IsSoftware() => true;

		void IRenderer.Reinitialize() { }

		void IRenderer.UpdateRefreshRate(double fps) => _framePacer.UpdateTargetFps(fps);

		void IDisposable.Dispose()
		{
			_framePacer.Dispose();
			_frameDeadlineReached.Dispose();
			var success = PInvoke.DeleteObject(_hBitmap) == 1;
			if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}
}
