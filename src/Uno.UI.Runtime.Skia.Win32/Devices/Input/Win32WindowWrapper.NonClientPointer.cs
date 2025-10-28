using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Uno.UI.Runtime.Skia.Win32.UI.Xaml.Window;
using Uno.UI.UI.Input.Internal;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
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
		PInvoke.GetWindowRect(hWnd, out var rcWindow);
		var scaling = (uint)(RasterizationScale * PInvoke.USER_DEFAULT_SCREEN_DPI);

		// Get the frame rectangle, adjusted for the style without a caption.
		var rcFrame = new RECT();
		var borderThickness = new RECT();
		if (Environment.OSVersion.Version < new Version(10, 0, 14393))
		{
			PInvoke.AdjustWindowRectEx(ref rcFrame, WINDOW_STYLE.WS_OVERLAPPEDWINDOW & ~WINDOW_STYLE.WS_CAPTION, false, 0);
			PInvoke.AdjustWindowRectEx(ref borderThickness, GetStyle(), false, 0);
		}
		else
		{
			PInvoke.AdjustWindowRectExForDpi(ref rcFrame, WINDOW_STYLE.WS_OVERLAPPEDWINDOW & ~WINDOW_STYLE.WS_CAPTION, false, 0, scaling);
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

		// Determine if the hit test is for resizing. Default middle (1,1).
		ushort uRow = 1;
		ushort uCol = 1;
		bool onResizeBorder = false;

		// Determine if the point is at the left or right of the window.
		if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + borderThickness.left)
		{
			uCol = 0;
		}
		else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - borderThickness.right)
		{
			uCol = 2;
		}

		// Determine if the point is at the top or bottom of the window.
		if (ptMouse.Y >= rcWindow.top && ptMouse.Y < rcWindow.top + borderThickness.top)
		{
			onResizeBorder = (ptMouse.Y < (rcWindow.top - rcFrame.top));

			// Two cases where we have a valid row 0 hit test:
			// - window resize border (top resize border hit)
			// - area below resize border that is actual titlebar (caption hit).
			if (onResizeBorder || uCol == 1)
			{
				uRow = 0;
			}
		}
		else if (ptMouse.Y < rcWindow.bottom && ptMouse.Y >= rcWindow.bottom - borderThickness.bottom)
		{
			uRow = 2;
		}

		bool hasCustomDragRects = false;
		if (_regionRects.TryGetValue(Microsoft.UI.Input.NonClientRegionKind.Caption, out var dragRects))
		{
			hasCustomDragRects = true;
			foreach (var rect in dragRects)
			{
				var scaledRect = new RectInt32
				(
					rcWindow.left + rect.X,
					rcWindow.top + rect.Y + titleBarHeightForDraggingRects,
					rect.Width,
					rect.Height
				);
				if (scaledRect.Contains(new PointInt32((int)(ptMouse.X), (int)(ptMouse.Y))))
				{
					return Win32NonClientHitTestKind.HTCAPTION;
				}
			}
		}

		var captionAreaHitTest = _window?.AppWindow.Presenter is FullScreenPresenter || hasCustomDragRects ? Win32NonClientHitTestKind.HTNOWHERE : Win32NonClientHitTestKind.HTCAPTION;
		ReadOnlySpan<Win32NonClientHitTestKind> hitZones = stackalloc Win32NonClientHitTestKind[]
		{
			Win32NonClientHitTestKind.HTTOPLEFT, onResizeBorder ? Win32NonClientHitTestKind.HTTOP : captionAreaHitTest, Win32NonClientHitTestKind.HTTOPRIGHT,
			Win32NonClientHitTestKind.HTLEFT, Win32NonClientHitTestKind.HTNOWHERE, Win32NonClientHitTestKind.HTRIGHT,
			Win32NonClientHitTestKind.HTBOTTOMLEFT, Win32NonClientHitTestKind.HTBOTTOM, Win32NonClientHitTestKind.HTBOTTOMRIGHT
		};

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
}
