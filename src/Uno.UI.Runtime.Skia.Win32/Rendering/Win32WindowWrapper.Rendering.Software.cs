using System;
using System.Runtime.InteropServices;
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
		private HBITMAP _hBitmap;

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
		}

		bool IRenderer.IsSoftware() => true;

		void IRenderer.OnWindowExtendedIntoTitleBar() { }

		void IDisposable.Dispose()
		{
			var success = PInvoke.DeleteObject(_hBitmap) == 1;
			if (!success) { typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}"); }
		}
	}
}
