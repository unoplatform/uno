using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

using System;
using System.Collections.Generic;
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
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;


namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{

	// All currently-attached extensions across all hosts. Filtered by XamlRoot when we need
	// to find a child's siblings to compute its z-order anchor.
	private static readonly List<Win32NativeElementHostingExtension> _attached = new();

	private readonly ContentPresenter _presenter;
	private Rect _lastArrangeRect;
	private Rect _pendingArrangeRect;
	private bool _arrangePending;
	private string? _lastFinalSvgClipPath;
	private HRGN _lastClipHrgn;
	private bool _showWindowOnNextRender;
	private int _zIndex;

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
		// WS_EX_LAYERED is intentionally never set — and actively cleared — on the hosted child window. It is
		// the only way to give a classic Win32 child window per-window alpha, but a layered child is excluded
		// from the OS's normal hit-testing and input routing (WindowFromPoint, mouse buttons, WM_SETCURSOR all
		// resolve to the parent), which prevents hosted content such as WebView2 from receiving focus/clicks.
		// Clearing it also covers a window that arrives already layered (e.g. re-attached after a prior attach
		// set it). Native element opacity is therefore unsupported here (as on X11); see ChangeNativeElementOpacity.
		PInvoke.SetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, oldExStyleVal & ~(int)WINDOW_EX_STYLE.WS_EX_LAYERED);
		PInvoke.SetWindowLong((HWND)window.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (oldStyleVal | (int)WINDOW_STYLE.WS_CLIPSIBLINGS) & ~(int)WINDOW_STYLE.WS_CAPTION); // removes the title bar and borders

		_ = PInvoke.ShowWindow((HWND)window.Hwnd, SHOW_WINDOW_CMD.SW_HIDE);
		_showWindowOnNextRender = true; // only show the window after the first render that has consumed an arrange, to avoid the split-second flicker between showing the window and positioning/clipping it correctly.

		var oldParent = PInvoke.SetParent((HWND)window.Hwnd, Hwnd);
		if (oldParent == HWND.Null && Marshal.GetLastWin32Error() != 0)
		{
			this.LogError()?.Error($"{nameof(PInvoke.SetParent)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		_attached.Add(this);
		((Win32WindowWrapper)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RenderingNegativePathReevaluated += OnRenderingNegativePathReevaluated;
	}

	private unsafe void OnRenderingNegativePathReevaluated(object? sender, IGeometry path)
	{
		Debug.Assert(Uno.UI.Dispatching.NativeDispatcher.Main.HasThreadAccess,
			$"{nameof(OnRenderingNegativePathReevaluated)} must run on the UI thread.");

		if (_presenter.Content is not Win32NativeWindow window)
		{
			return;
		}

		var consumedArrange = false;
		if (_arrangePending)
		{
			_arrangePending = false;
			consumedArrange = true;
			_lastArrangeRect = _pendingArrangeRect;
			_lastFinalSvgClipPath = null; // force clip recomputation for the new arrange rect

			var posSuccess = PInvoke.SetWindowPos(
				(HWND)window.Hwnd,
				HWND.Null,
				(int)_lastArrangeRect.X,
				(int)_lastArrangeRect.Y,
				(int)_lastArrangeRect.Width,
				(int)_lastArrangeRect.Height,
				SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
			if (!posSuccess)
			{
				this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
			}
		}

		try
		{
			ApplyClipPath(path);
		}
		finally
		{
			// Reveal the window only after position + clip are applied for the first time,
			// to avoid a split-second flash of an unpositioned / unclipped window. Gating on
			// consumedArrange ensures we never show before the first ArrangeNativeElement has
			// been applied (e.g., if a render fires between Attach and the initial arrange).
			if (_showWindowOnNextRender && consumedArrange)
			{
				_showWindowOnNextRender = false;
				_ = PInvoke.ShowWindow((HWND)window.Hwnd, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);

				// SW_SHOWNOACTIVATE restores the window using its saved WINDOWPLACEMENT.rcNormalPosition,
				// which still holds the old screen coordinates from before re-parenting. Those coordinates
				// are now misinterpreted as parent-relative, placing the window at the wrong position.
				// Re-applying SetWindowPos after ShowWindow overrides that restoration.
				var posSuccess = PInvoke.SetWindowPos(
					(HWND)window.Hwnd,
					HWND.Null,
					(int)_lastArrangeRect.X,
					(int)_lastArrangeRect.Y,
					(int)_lastArrangeRect.Width,
					(int)_lastArrangeRect.Height,
					SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
				if (!posSuccess)
				{
					this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
				}
				ApplyZOrder((HWND)window.Hwnd);
			}
		}
	}

	private void ApplyZOrder(HWND childHwnd)
	{
		// SetWindowPos's hWndInsertAfter is "the window to precede the positioned window in the
		// Z order" — i.e., the one that ends up *above* us, not below us. Place the child just
		// below the lowest-zIndex sibling that's still higher than us; if none, HWND_TOP puts us
		// at the top of the chain.
		Win32NativeElementHostingExtension? higher = null;
		foreach (var ext in _attached)
		{
			if (ext._presenter.XamlRoot != _presenter.XamlRoot)
			{
				continue;
			}
			if (ext._zIndex > _zIndex && (higher is null || ext._zIndex < higher._zIndex))
			{
				higher = ext;
			}
		}

		var insertAfter = higher is null
			? default(HWND) // HWND_TOP
			: (HWND)((Win32NativeWindow)higher._presenter.Content).Hwnd;

		if (!PInvoke.SetWindowPos(childHwnd, insertAfter, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
		{
			this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} (z-order) failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	// Feeds a neutral geometry's flattened contours into a GDI+ path as line-only figures (each contour closed).
	private sealed unsafe class GdiPathSink : IFlattenedPathSink
	{
		private readonly GpPath* _gpPath;
		private Vector2 _last;

		public GdiPathSink(GpPath* gpPath) => _gpPath = gpPath;

		public Status? Error { get; private set; }

		public void BeginContour(Vector2 start) => _last = start;

		public void LineTo(Vector2 point)
		{
			if (Error is null)
			{
				var status = PInvoke.GdipAddPathLine(_gpPath, _last.X, _last.Y, point.X, point.Y);
				if (status != Status.Ok)
				{
					Error = status;
				}
			}
			_last = point;
		}

		public void EndContour(bool closed)
		{
			if (Error is null)
			{
				var status = PInvoke.GdipClosePathFigure(_gpPath);
				if (status != Status.Ok)
				{
					Error = status;
				}
			}
		}
	}

	private unsafe void ApplyClipPath(IGeometry path)
	{
		// Neutral clip: intersect with the arrange rect and translate to local, all through the IGeometry seam
		// (was a Skia path op + matrix). The host stays backend-agnostic — no Skia types. Geometry comes from the
		// global GeometryFactory seam (backend-independent); the renderer consumes whatever neutral geometry it gets.
		using var rectGeometry = GeometryFactory.Current.CreateRectangleGeometry(_lastArrangeRect);
		using var intersected = path.Combine(rectGeometry, GeometryCombineMode.Intersect);
		using var localClip = intersected.Transform(Matrix3x2.CreateTranslation((float)-_lastArrangeRect.X, (float)-_lastArrangeRect.Y));

		if (localClip.ToSvgPathData() is var svgPathData && svgPathData == _lastFinalSvgClipPath && !_lastClipHrgn.IsNull)
		{
			SetHrgnAndCache(this, _lastClipHrgn);
			return;
		}

		_lastFinalSvgClipPath = svgPathData;

		GpPath* gpPath = null;
		var status = PInvoke.GdipCreatePath(localClip.FillRule == GeometryFillRule.EvenOdd ? FillMode.FillModeAlternate : FillMode.FillModeWinding, ref gpPath);
		if (status != Status.Ok)
		{
			this.LogError()?.Error($"{nameof(PInvoke.GdipCreatePath)} failed: {status}");
			return;
		}

		// StreamFlattened subdivides curves to polylines, so the GDI+ path is line-only figures (a window region
		// is polygonal regardless). Each contour is closed to form a fillable figure.
		var sink = new GdiPathSink(gpPath);
		localClip.StreamFlattened(sink);
		if (sink.Error is { } err)
		{
			this.LogError()?.Error($"Building the GDI+ clip path failed: {err}");
			return;
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

		// WS_CHILD stays set across detach/re-attach cycles.

		_attached.Remove(this);
		((Win32WindowWrapper)XamlRootMap.GetHostForRoot(_presenter.XamlRoot!)!).RenderingNegativePathReevaluated -= OnRenderingNegativePathReevaluated;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
		if (content is not Win32NativeWindow)
		{
			throw new ArgumentException($"content is not a {nameof(Win32NativeWindow)} instance.", nameof(content));
		}

		var scale = _presenter.XamlRoot?.RasterizationScale ?? 1;

		var x = arrangeRect.X * scale;
		var y = arrangeRect.Y * scale;
		var width = arrangeRect.Width * scale;
		var height = arrangeRect.Height * scale;

		// Stash the intended rect and defer SetWindowPos to OnRenderingNegativePathReevaluated,
		// which fires between the Skia picture playback and CopyPixels. Moving the child HWND
		// at that point keeps its position update temporally adjacent to the parent framebuffer
		// present, so DWM is more likely to see both updates in the same compose cycle — rather
		// than compositing the child at a new position over the parent's previous frame (visible
		// as flicker when scrolling a native element).
		_pendingArrangeRect = new Rect(
			double.IsFinite(x) ? x : 0,
			double.IsFinite(y) ? y : 0,
			double.IsFinite(width) ? width : 0,
			double.IsFinite(height) ? height : 0);
		_arrangePending = true;
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		return new Size(
			double.IsFinite(availableSize.Width) ? availableSize.Width : 0,
			double.IsFinite(availableSize.Height) ? availableSize.Height : 0);
	}

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
		// Native element opacity is not supported on Win32. Per-window alpha for a classic Win32 child
		// window requires WS_EX_LAYERED, which excludes the child from the OS's normal hit-testing and
		// input routing and thus breaks focus/clicks for hosted content such as WebView2 (see
		// AttachNativeElement). X11 likewise does not support opacity for hosted child windows.
	}

	public bool SupportsZIndex() => true;

	public void SetZIndex(object content, int zIndex)
	{
		_zIndex = zIndex;
		if (content is Win32NativeWindow window)
		{
			ApplyZOrder((HWND)window.Hwnd);
		}
	}
}
