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

	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	// --- Win32 constants
	private const int WM_NCHITTEST = 0x0084;
	private const int GWLP_WNDPROC = -4;
	private const int HTCLIENT = 1;
	private const int HTCAPTION = 2;
	private const int HTSYSMENU = 3;
	private const int HTMINBUTTON = 8;
	private const int HTMAXBUTTON = 9;
	private const int HTLEFT = 10;
	private const int HTRIGHT = 11;
	private const int HTTOP = 12;
	private const int HTBOTTOM = 15;
	private const int HTCLOSE = 20;

	// 🪟 Implement the INativeInputNonClientPointerSource contract
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
		var scaling = (uint)(RasterizationScale * StandardDpi);

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

	private bool TryHandleNonClientHitTest(HWND hWnd, WPARAM wParam, LPARAM lParam, out IntPtr result)
	{
		var defResult = PInvoke.DefWindowProc(hWnd, WM_NCHITTEST, wParam, lParam);

		if ((int)defResult == HTCLIENT && _regionRects.Count > 0)
		{
			int x = (short)(lParam.Value.ToInt32() & 0xFFFF);
			int y = (short)((lParam.Value.ToInt32() >> 16) & 0xFFFF);

			var p = new System.Drawing.Point { X = x, Y = y };
			PInvoke.ScreenToClient(hWnd, ref p);

			foreach (var kv in _regionRects)
			{
				foreach (var rect in kv.Value)
				{
					var contains = p.X >= rect.X && p.X < rect.X + rect.Width &&
								   p.Y >= rect.Y && p.Y < rect.Y + rect.Height;
					if (contains)
					{
						result = RegionKindToHitTest(kv.Key);
						return true;
					}
				}
			}
		}

		result = (IntPtr)defResult;
		return true;
	}

	private static int RegionKindToHitTest(NonClientRegionKind kind) => kind switch
	{
		NonClientRegionKind.Close => HTCLOSE,
		NonClientRegionKind.Maximize => HTMAXBUTTON,
		NonClientRegionKind.Minimize => HTMINBUTTON,
		NonClientRegionKind.Icon => HTSYSMENU,
		NonClientRegionKind.Caption => HTCAPTION,
		NonClientRegionKind.TopBorder => HTTOP,
		NonClientRegionKind.LeftBorder => HTLEFT,
		NonClientRegionKind.BottomBorder => HTBOTTOM,
		NonClientRegionKind.RightBorder => HTRIGHT,
		NonClientRegionKind.Passthrough => HTCLIENT,
		_ => HTCLIENT
	};
}
