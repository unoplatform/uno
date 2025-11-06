using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Uno.UI.Runtime.Skia.Win32.UI.Xaml.Window;
using Uno.UI.UI.Input.Internal;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

partial class Win32WindowWrapper : INativeInputNonClientPointerSource
{
	private readonly Dictionary<NonClientRegionKind, RectInt32[]> _regionRects = new();
	public void ClearAllRegionRects()
		=> _regionRects.Clear();

	public void ClearRegionRects(NonClientRegionKind region)
		=> _regionRects.Remove(region);

	public RectInt32[] GetRegionRects(NonClientRegionKind region)
		=> _regionRects.TryGetValue(region, out var list) ? list : Array.Empty<RectInt32>();

	public void SetRegionRects(NonClientRegionKind region, RectInt32[] rects)
	{
		if (rects.Length == 0)
		{
			_regionRects.Remove(region);
			return;
		}

		_regionRects[region] = rects;
	}

	private Win32NonClientHitTestKind NonClientAreaHitTest(HWND hWnd, WPARAM wParam, LPARAM lParam)
	{
		// Get the point coordinates for the hit test (screen space).
		var ptMouse = PointFromLParam(lParam);

		// Get the window rectangle.
		PInvoke.GetWindowRect(hWnd, out var windowRectangle);

		var scaling = (uint)(RasterizationScale * PInvoke.USER_DEFAULT_SCREEN_DPI);
		var relativeScaling = RasterizationScale / GetPrimaryMonitorScale();

		// Get the frame rectangle, adjusted for the style without a caption.
		var frameRectangle = new RECT();
		var borderThickness = new RECT();
		WINDOW_STYLE frameStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW & ~WINDOW_STYLE.WS_CAPTION;

		if (Environment.OSVersion.Version < new Version(10, 0, 14393))
		{
			PInvoke.AdjustWindowRectEx(ref frameRectangle, frameStyle, false, 0);
			PInvoke.AdjustWindowRectEx(ref borderThickness, GetStyle(), false, 0);

			frameRectangle.top = (int)(frameRectangle.top * relativeScaling);
			frameRectangle.right = (int)(frameRectangle.right * relativeScaling);
			frameRectangle.left = (int)(frameRectangle.left * relativeScaling);
			frameRectangle.bottom = (int)(frameRectangle.bottom * relativeScaling);

			borderThickness.top = (int)(borderThickness.top * relativeScaling);
			borderThickness.right = (int)(borderThickness.right * relativeScaling);
			borderThickness.left = (int)(borderThickness.left * relativeScaling);
			borderThickness.bottom = (int)(borderThickness.bottom * relativeScaling);
		}
		else
		{
			PInvoke.AdjustWindowRectExForDpi(ref frameRectangle, frameStyle, false, 0, scaling);
			PInvoke.AdjustWindowRectExForDpi(ref borderThickness, GetStyle(), false, 0, scaling);
		}

		borderThickness.left *= -1;
		borderThickness.top *= -1;

		var appWindowTitleBar = _window?.AppWindow.TitleBar;
		if (appWindowTitleBar?.Height > 0 && _hasTitleBar)
		{
			borderThickness.top = appWindowTitleBar.Height;
		}

		var titleBarHeightForDraggingRects = _hasTitleBar ? borderThickness.top : 0;

		bool hasCustomDragRects = false;
		// Go through all the available rects
		foreach (var region in _regionRects)
		{
			if (region.Key == NonClientRegionKind.Caption && region.Value.Length > 0)
			{
				hasCustomDragRects = true;
			}

			foreach (var rect in region.Value)
			{
				var adjustedRect = new RectInt32(
					rect.X + windowRectangle.left,
					titleBarHeightForDraggingRects + rect.Y + windowRectangle.top,
					rect.Width,
					rect.Height);
				if (adjustedRect.Contains(new PointInt32(ptMouse.X, ptMouse.Y)))
				{
					return RegionKindToHitTest(region.Key);
				}
			}
		}

		var shouldFallbackToDefaultCaptionHitTest = hasCustomDragRects || _hasTitleBar;

		// Determine if the hit test is for resizing. Default middle (1,1).
		ushort uRow = 1;
		ushort uCol = 1;
		bool onResizeBorder = false;

		// Determine if the point is at the left or right of the window.
		if (ptMouse.X >= windowRectangle.left && ptMouse.X < windowRectangle.left + borderThickness.left)
		{
			uCol = 0;
		}
		else if (ptMouse.X < windowRectangle.right && ptMouse.X >= windowRectangle.right - borderThickness.right)
		{
			uCol = 2;
		}

		// Determine if the point is at the top or bottom of the window.
		if (ptMouse.Y >= windowRectangle.top && ptMouse.Y < windowRectangle.top + borderThickness.top)
		{
			onResizeBorder = (ptMouse.Y < (windowRectangle.top - frameRectangle.top));

			// Two cases where we have a valid row 0 hit test:
			// - window resize border (top resize border hit)
			// - area below resize border that is actual titlebar (caption hit).
			if (onResizeBorder || uCol == 1)
			{
				uRow = 0;
			}
		}
		else if (ptMouse.Y < windowRectangle.bottom && ptMouse.Y >= windowRectangle.bottom - borderThickness.bottom)
		{
			uRow = 2;
		}

		var captionAreaHitTest = _window?.AppWindow.Presenter is FullScreenPresenter || shouldFallbackToDefaultCaptionHitTest ? Win32NonClientHitTestKind.HTNOWHERE : Win32NonClientHitTestKind.HTCAPTION;
		ReadOnlySpan<Win32NonClientHitTestKind> hitZones =
		[
			Win32NonClientHitTestKind.HTTOPLEFT, onResizeBorder ? Win32NonClientHitTestKind.HTTOP : captionAreaHitTest, Win32NonClientHitTestKind.HTTOPRIGHT,
			Win32NonClientHitTestKind.HTLEFT, Win32NonClientHitTestKind.HTNOWHERE, Win32NonClientHitTestKind.HTRIGHT,
			Win32NonClientHitTestKind.HTBOTTOMLEFT, Win32NonClientHitTestKind.HTBOTTOM, Win32NonClientHitTestKind.HTBOTTOMRIGHT
		];

		var zoneIndex = uRow * 3 + uCol;

		return hitZones[zoneIndex];
	}

	private static Win32NonClientHitTestKind RegionKindToHitTest(NonClientRegionKind kind) => kind switch
	{
		NonClientRegionKind.Close => Win32NonClientHitTestKind.HTCLOSE,
		NonClientRegionKind.Maximize => Win32NonClientHitTestKind.HTMAXBUTTON,
		NonClientRegionKind.Minimize => Win32NonClientHitTestKind.HTMINBUTTON,
		NonClientRegionKind.Icon => Win32NonClientHitTestKind.HTSYSMENU,
		NonClientRegionKind.Caption => Win32NonClientHitTestKind.HTCAPTION,
		NonClientRegionKind.TopBorder => Win32NonClientHitTestKind.HTTOP,
		NonClientRegionKind.LeftBorder => Win32NonClientHitTestKind.HTLEFT,
		NonClientRegionKind.BottomBorder => Win32NonClientHitTestKind.HTBOTTOM,
		NonClientRegionKind.RightBorder => Win32NonClientHitTestKind.HTRIGHT,
		NonClientRegionKind.Passthrough => Win32NonClientHitTestKind.HTCLIENT,
		_ => Win32NonClientHitTestKind.HTCLIENT
	};

	private bool ShouldRedirectNonClientInput(HWND hWnd, WPARAM wParam, IntPtr lParam)
	{
		var hitTestResult = NonClientAreaHitTest(hWnd, wParam, lParam);

		return hitTestResult
			is Win32NonClientHitTestKind.HTMINBUTTON
			or Win32NonClientHitTestKind.HTMAXBUTTON
			or Win32NonClientHitTestKind.HTCLOSE
			or Win32NonClientHitTestKind.HTHELP
			or Win32NonClientHitTestKind.HTMENU
			or Win32NonClientHitTestKind.HTSYSMENU;
	}

	private unsafe float GetPrimaryMonitorScale()
	{
		// Get the handle to the primary monitor
		var primaryMonitor = PInvoke.MonitorFromPoint(new System.Drawing.Point(0, 0), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);

		// Try getting the DPI using GetDpiForMonitor (Windows 8.1+)
		if (PInvoke.GetDpiForMonitor(primaryMonitor, Windows.Win32.UI.HiDpi.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY) == 0)
		{
			return dpiX / (float)PInvoke.USER_DEFAULT_SCREEN_DPI;
		}
		else
		{
			// Fallback for older Windows versions
			var hdc = PInvoke.GetDC(HWND.Null);
			int dpi = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.LOGPIXELSX);
			_ = PInvoke.ReleaseDC(HWND.Null, hdc);
			return dpi / (float)PInvoke.USER_DEFAULT_SCREEN_DPI;
		}
	}
}
