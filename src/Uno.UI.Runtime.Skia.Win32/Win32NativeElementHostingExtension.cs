using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.GdiPlus;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.Win32;

public class Win32NativeElementHostingExtension(ContentPresenter presenter) : ContentPresenter.INativeElementHostingExtension
{
	private static HWND _enumProcRet;
	private static readonly SKPath _lastClipPath = new();

	private Rect _lastArrangeRect;

	private HWND Hwnd
	{
		get
		{
			if (presenter.XamlRoot is null)
			{
				throw new InvalidOperationException($"{nameof(XamlRoot)} is null.");
			}

			if (presenter.XamlRoot.HostWindow is not { } window)
			{
				throw new InvalidOperationException($"{nameof(presenter)}.{nameof(XamlRoot)}.{nameof(XamlRoot.HostWindow)} is null.");
			}

			if (window.NativeWindow is not Win32NativeWindow nativeWindow)
			{
				throw new InvalidOperationException($"{nameof(window.NativeWindow)} is not a {nameof(Win32NativeWindow)} instance.");
			}

			return (HWND)nativeWindow.Hwnd;
		}
	}

	public bool IsNativeElement(object content) => content is Win32NativeWindow;

	public void AttachNativeElement(object content)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		var oldExStyleVal = PInvoke.GetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		var oldStyleVal = PInvoke.GetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		PInvoke.SetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, oldExStyleVal | (int)WINDOW_EX_STYLE.WS_EX_LAYERED);
		PInvoke.SetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (oldStyleVal | (int)WINDOW_STYLE.WS_CLIPSIBLINGS) & ~(int)WINDOW_STYLE.WS_CAPTION); // removes the title bar and borders

		_ = PInvoke.ShowWindow((HWND)window.Hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);

		var oldParent = PInvoke.SetParent((HWND)window.Hwnd, Hwnd);
		if (oldParent == HWND.Null && Marshal.GetLastWin32Error() != 0)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		Win32WindowWrapper.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!.RenderingNegativePathChanged += OnRenderingNegativePathChanged;
	}

	private unsafe void OnRenderingNegativePathChanged(object? sender, SKPath path)
	{
		using var _1 = SkiaHelper.GetTempSKPath(out var arrangePath);
		using var _2 = SkiaHelper.GetTempSKPath(out var intersectionPath);

		arrangePath.AddRect(_lastArrangeRect.ToSKRect());
		path.Op(arrangePath, SKPathOp.Intersect, intersectionPath);
		intersectionPath.Transform(SKMatrix.CreateTranslation((float)-_lastArrangeRect.X, (float)-_lastArrangeRect.Y));
		_lastClipPath.Rewind();
		_lastClipPath.AddPath(intersectionPath);

		Debug.Assert(intersectionPath.FillType is SKPathFillType.Winding or SKPathFillType.EvenOdd);
		GpPath* gpPath = null;
		var status = PInvoke.GdipCreatePath(intersectionPath.FillType is SKPathFillType.Winding ? FillMode.FillModeWinding : FillMode.FillModeAlternate, ref gpPath);
		if (status != Status.Ok)
		{
			this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipCreatePath)} failed: {status}");
			return;
		}

		var iter = intersectionPath.CreateIterator(forceClose: true);
		SKPathVerb verb = default;
		var points = stackalloc SKPoint[4];
		var pointSpan = new Span<SKPoint>(points, 4);
		while (verb != SKPathVerb.Done)
		{
			verb = iter.Next(pointSpan);
			switch (verb)
			{
				case SKPathVerb.Move:
					break;
				case SKPathVerb.Line:
					status = PInvoke.GdipAddPathLine(gpPath, pointSpan[0].X, pointSpan[0].Y, pointSpan[1].X, pointSpan[1].Y);
					if (status != Status.Ok)
					{
						this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipAddPathLine)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Quad:
					// quadratic to cubic bézier
					var controlPoint1 = pointSpan[0] + new SKPoint((pointSpan[1].X - pointSpan[0].X) * 2 / 3, (pointSpan[1].Y - pointSpan[0].Y) * 2 / 3);
					var controlPoint2 = pointSpan[2] + new SKPoint((pointSpan[1].X - pointSpan[2].X) * 2 / 3, (pointSpan[1].Y - pointSpan[2].Y) * 2 / 3);
					status = PInvoke.GdipAddPathBezier(gpPath, pointSpan[0].X, pointSpan[0].Y, controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, pointSpan[2].X, pointSpan[2].Y);
					if (status != Status.Ok)
					{
						this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipAddPathBezier)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Conic:
					throw new NotImplementedException();
				case SKPathVerb.Cubic:
					status = PInvoke.GdipAddPathBezier(gpPath, pointSpan[0].X, pointSpan[0].Y, pointSpan[1].X, pointSpan[1].Y, pointSpan[2].X, pointSpan[2].Y, pointSpan[3].X, pointSpan[3].Y);
					if (status != Status.Ok)
					{
						this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipAddPathBezier)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Close:
					status = PInvoke.GdipClosePathFigure(gpPath);
					if (status != Status.Ok)
					{
						this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipClosePathFigure)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Done:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		GpRegion* region = default;
		status = PInvoke.GdipCreateRegionPath(gpPath, &region);
		if (status != Status.Ok)
		{
			this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipCreateRegionPath)} failed: {status}");
			return;
		}

		GpGraphics* graphics = default;
		status = PInvoke.GdipCreateFromHWND(Hwnd, ref graphics);
		if (status != Status.Ok)
		{
			this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipCreateFromHWND)} failed: {status}");
			return;
		}

		HRGN hrgn = default;
		status = PInvoke.GdipGetRegionHRgn(region, graphics, &hrgn);
		if (status != Status.Ok)
		{
			this.Log().Log(LogLevel.Error, status, static status => $"{nameof(PInvoke.GdipGetRegionHRgn)} failed: {status}");
			return;
		}

		// "After a successful call to SetWindowRgn, the system owns the region specified by the region handle hRgn. The system does not make a copy of the region. Thus, you should not make any further function calls with this region handle. In particular, do not delete this region handle. The system deletes the region handle when it no longer needed."
		_ = PInvoke.SetWindowRgn((HWND)((Win32NativeWindow)presenter.Content).Hwnd, new HRGN(hrgn), true) != 0
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowRgn)} failed: {Win32Helper.GetErrorMessage()}");
	}

	public void DetachNativeElement(object content)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_ = PInvoke.ShowWindow((HWND)window.Hwnd, SHOW_WINDOW_CMD.SW_HIDE);

		var oldParent = PInvoke.SetParent((HWND)window.Hwnd, HWND.Null);
		if (oldParent == HWND.Null && Marshal.GetLastWin32Error() != 0)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
		}

		Win32WindowWrapper.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!.RenderingNegativePathChanged -= OnRenderingNegativePathChanged;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_lastArrangeRect = arrangeRect;

		_ = PInvoke.SetWindowPos(
				(HWND)window.Hwnd,
				HWND.Null,
				(int)arrangeRect.X,
				(int)arrangeRect.Y,
				(int)arrangeRect.Width,
				(int)arrangeRect.Height,
				SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		OnRenderingNegativePathChanged(this, _lastClipPath);
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public unsafe object CreateSampleComponent(string text)
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				UseShellExecute = true
			}
		};

		process.Start();

		_enumProcRet = HWND.Null;
		HWND hwnd = default;
		var success = SpinWait.SpinUntil(() =>
		{
			PInvoke.EnumWindows(&EnumFunc, process.Id);
			hwnd = _enumProcRet;
			return hwnd != HWND.Null;
		}, TimeSpan.FromSeconds(5));

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
		static BOOL EnumFunc(HWND hwnd, LPARAM lParam)
		{
			uint processId = default;
			_ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);
			if (processId == lParam.Value)
			{
				_enumProcRet = hwnd;
				return false;
			}
			return true;
		}

		if (!success)
		{
			throw new InvalidOperationException("Could not find the HWND spawned by the created process.");
		}

		return new Win32NativeWindow(hwnd);
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		// no need to do anything here, airspace clipping logic will take care of it automatically
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		_ = PInvoke.SetLayeredWindowAttributes((HWND)window.Hwnd, new COLORREF(0), (byte)Math.Round(opacity * 255), LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA)
			|| this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.SetLayeredWindowAttributes)} failed: {Win32Helper.GetErrorMessage()}");
	}
}
