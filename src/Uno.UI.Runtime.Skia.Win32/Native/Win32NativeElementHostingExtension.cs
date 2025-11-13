using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

using System;
using System.Diagnostics;
using System.Globalization;
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
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private static readonly SKPath _lastClipPath = new();
	private static readonly SKPoint[] _conicPoints = new SKPoint[32 * 3]; // 3 points per quad

	private readonly ContentPresenter _presenter;
	private readonly SKPath _tempPath = new();
	private Rect _lastArrangeRect;
	private string? _lastFinalSvgClipPath;
	private HRGN _lastClipHrgn;

	public Win32NativeElementHostingExtension(ContentPresenter presenter)
	{
		_presenter = presenter;
	}

	~Win32NativeElementHostingExtension()
	{
		if (!_lastClipHrgn.IsNull)
		{
			if (!PInvoke.DeleteObject(_lastClipHrgn))
			{
				typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	private HWND Hwnd
	{
		get
		{
			if (_presenter.XamlRoot is null)
			{
				throw new InvalidOperationException($"{nameof(XamlRoot)} is null.");
			}

			if (_presenter.XamlRoot.HostWindow is not { } window)
			{
				throw new InvalidOperationException($"{nameof(_presenter)}.{nameof(XamlRoot)}.{nameof(XamlRoot.HostWindow)} is null.");
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
			this.LogError()?.Error($"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		((Win32WindowWrapper)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RenderingNegativePathReevaluated += OnRenderingNegativePathReevaluated;
	}

	private unsafe void OnRenderingNegativePathReevaluated(object? sender, SKPath path)
	{
		if (path != _lastClipPath)
		{
			_lastClipPath.Rewind();
			_lastClipPath.AddPath(path);
		}

		_tempPath.Rewind();
		_tempPath.AddRect(_lastArrangeRect.ToSKRect());
		path.Op(_tempPath, SKPathOp.Intersect, _tempPath);
		_tempPath.Transform(SKMatrix.CreateTranslation((float)-_lastArrangeRect.X, (float)-_lastArrangeRect.Y));

		if (_tempPath.ToSvgPathData() is var svgPathData && svgPathData == _lastFinalSvgClipPath && !_lastClipHrgn.IsNull)
		{
			SetHrgnAndCache(this, _lastClipHrgn);
			return;
		}

		_lastFinalSvgClipPath = svgPathData;

		Debug.Assert(_tempPath.FillType is SKPathFillType.Winding or SKPathFillType.EvenOdd);
		GpPath* gpPath = null;
		var status = PInvoke.GdipCreatePath(_tempPath.FillType is SKPathFillType.Winding ? FillMode.FillModeWinding : FillMode.FillModeAlternate, ref gpPath);
		if (status != Status.Ok)
		{
			this.LogError()?.Error($"{nameof(PInvoke.GdipCreatePath)} failed: {status}");
			return;
		}

		var iter = _tempPath.CreateIterator(forceClose: true);
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
						this.LogError()?.Error($"{nameof(PInvoke.GdipAddPathLine)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Quad:
					{
						// quadratic to cubic bézier
						var controlPoint1 = pointSpan[0] + new SKPoint((pointSpan[1].X - pointSpan[0].X) * 2 / 3, (pointSpan[1].Y - pointSpan[0].Y) * 2 / 3);
						var controlPoint2 = pointSpan[2] + new SKPoint((pointSpan[1].X - pointSpan[2].X) * 2 / 3, (pointSpan[1].Y - pointSpan[2].Y) * 2 / 3);
						status = PInvoke.GdipAddPathBezier(gpPath, pointSpan[0].X, pointSpan[0].Y, controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, pointSpan[2].X, pointSpan[2].Y);
						if (status != Status.Ok)
						{
							this.LogError()?.Error($"{nameof(PInvoke.GdipAddPathBezier)} failed: {status}");
							return;
						}
					}
					break;
				case SKPathVerb.Conic:
					// https://api.skia.org/classSkPath.html
					// "conic weight determines the amount of influence conic control point has on the curve. w less than one represents an elliptical section. w greater than one represents a hyperbolic section. w equal to one represents a parabolic section."
					// "Two quad curves are sufficient to approximate an elliptical conic with a sweep of up to 90 degrees; in this case, set pow2 to one."
					var conicWeight = iter.ConicWeight();
					var quads = SKPath.ConvertConicToQuads(pointSpan[0], pointSpan[1], pointSpan[2], conicWeight, _conicPoints, conicWeight < 1 ? 1 : 5);
					for (int i = 0; i < quads; i++)
					{
						// https://api.skia.org/classSkPath.html
						// "Every third point in array shares last SkPoint of previous quad and first SkPoint of next quad"
						// In other words, if you have 2 quads, you will get 5 points: Q0_0 Q0_1 (Q0_2/Q1_0) Q1_1 Q1_2
						var p0 = _conicPoints[2 * i];
						var p1 = _conicPoints[2 * i + 1];
						var p2 = _conicPoints[2 * i + 2];
						// quadratic to cubic bézier
						var controlPoint1 = p0 + new SKPoint((p1.X - p0.X) * 2 / 3, (p1.Y - p0.Y) * 2 / 3);
						var controlPoint2 = p2 + new SKPoint((p1.X - p2.X) * 2 / 3, (p1.Y - p2.Y) * 2 / 3);
						status = PInvoke.GdipAddPathBezier(gpPath, p0.X, p0.Y, controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, p2.X, p2.Y);
						if (status != Status.Ok)
						{
							this.LogError()?.Error($"{nameof(PInvoke.GdipAddPathBezier)} failed: {status}");
							return;
						}
					}
					break;
				case SKPathVerb.Cubic:
					status = PInvoke.GdipAddPathBezier(gpPath, pointSpan[0].X, pointSpan[0].Y, pointSpan[1].X, pointSpan[1].Y, pointSpan[2].X, pointSpan[2].Y, pointSpan[3].X, pointSpan[3].Y);
					if (status != Status.Ok)
					{
						this.LogError()?.Error($"{nameof(PInvoke.GdipAddPathBezier)} failed: {status}");
						return;
					}
					break;
				case SKPathVerb.Close:
					status = PInvoke.GdipClosePathFigure(gpPath);
					if (status != Status.Ok)
					{
						this.LogError()?.Error($"{nameof(PInvoke.GdipClosePathFigure)} failed: {status}");
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
			this.LogError()?.Error($"{nameof(PInvoke.GdipCreateRegionPath)} failed: {status}");
			return;
		}

		GpGraphics* graphics = default;
		status = PInvoke.GdipCreateFromHWND(Hwnd, ref graphics);
		if (status != Status.Ok)
		{
			this.LogError()?.Error($"{nameof(PInvoke.GdipCreateFromHWND)} failed: {status}");
			return;
		}

		HRGN hrgn = default;
		status = PInvoke.GdipGetRegionHRgn(region, graphics, &hrgn);
		if (status != Status.Ok)
		{
			this.LogError()?.Error($"{nameof(PInvoke.GdipGetRegionHRgn)} failed: {status}");
			return;
		}

		SetHrgnAndCache(this, hrgn);

		static void SetHrgnAndCache(Win32NativeElementHostingExtension @this, HRGN hrgn)
		{
			var hwnd = (HWND)((Win32NativeWindow)@this._presenter.Content).Hwnd;

			// "After a successful call to SetWindowRgn, the system owns the region specified by the region handle hRgn. The system does not make a copy of the region. Thus, you should not make any further function calls with this region handle. In particular, do not delete this region handle. The system deletes the region handle when it no longer needed."
			if (PInvoke.SetWindowRgn(hwnd, new HRGN(hrgn), true) == 0)
			{
				@this.LogError()?.Error($"{nameof(PInvoke.SetWindowRgn)} failed: {Win32Helper.GetErrorMessage()}");
			}

			if (!@this._lastClipHrgn.IsNull)
			{
				if (!PInvoke.DeleteObject(@this._lastClipHrgn))
				{
					typeof(Win32WindowWrapper).LogError()?.Error($"{nameof(PInvoke.DeleteObject)} failed: {Win32Helper.GetErrorMessage()}");
				}
			}

			@this._lastClipHrgn = PInvoke.CreateRectRgn(0, 0, 0, 0);
			if (@this._lastClipHrgn.IsNull)
			{
				@this.LogError()?.Error($"{nameof(PInvoke.SetWindowRgn)} failed: {Win32Helper.GetErrorMessage()}");
				return;
			}

			if (PInvoke.GetWindowRgn(hwnd, @this._lastClipHrgn) == GDI_REGION_TYPE.RGN_ERROR)
			{
				// Do not report an error here as this will spam the console.
				// RGN_ERROR means that "The specified window does not have a region, or an error occurred while attempting to return the region."
				// @this.LogError()?.Error($"{nameof(PInvoke.GetWindowRgn)} failed: {Win32Helper.GetErrorMessage()}");
				@this._lastClipHrgn = HRGN.Null;
			}
		}
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
			this.LogError()?.Error($"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
		}

		((Win32WindowWrapper)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RenderingNegativePathReevaluated -= OnRenderingNegativePathReevaluated;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		var scale = _presenter.XamlRoot?.RasterizationScale ?? 1;

		_lastArrangeRect = new Rect(arrangeRect.X * scale, arrangeRect.Y * scale, arrangeRect.Width * scale, arrangeRect.Height * scale);

		var success = PInvoke.SetWindowPos(
				(HWND)window.Hwnd,
				HWND.Null,
				(int)_lastArrangeRect.X,
				(int)_lastArrangeRect.Y,
				(int)_lastArrangeRect.Width,
				(int)_lastArrangeRect.Height,
				SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		}

		_lastFinalSvgClipPath = null; // force reapply clip path after arranging
		OnRenderingNegativePathReevaluated(this, _lastClipPath);
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public unsafe object CreateSampleComponent(string text)
	{
		var windowTitle = Random.Shared.NextInt64().ToString(CultureInfo.InvariantCulture);
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-NoExit -Command \"$Host.UI.RawUI.WindowTitle = '{windowTitle}'\"",
				UseShellExecute = true
			}
		};

		process.Start();

		HWND hwnd = default;
		var success = SpinWait.SpinUntil(() =>
		{
			hwnd = PInvoke.FindWindow(null, windowTitle);
			return hwnd != HWND.Null;
		}, TimeSpan.FromSeconds(50));


		if (!success)
		{
			throw new InvalidOperationException("Could not find the HWND spawned by the created process.");
		}

		return new Win32NativeWindow(hwnd);
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is not Win32NativeWindow window)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		var success = PInvoke.SetLayeredWindowAttributes((HWND)window.Hwnd, new COLORREF(0), (byte)Math.Round(opacity * 255), LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetLayeredWindowAttributes)} failed: {Win32Helper.GetErrorMessage()}"); }
	}
}
