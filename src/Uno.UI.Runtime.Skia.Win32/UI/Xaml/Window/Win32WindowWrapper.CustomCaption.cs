using System;
using System.Drawing;
using Microsoft.UI.Windowing;
using Uno.UI.Runtime.Skia.Win32.UI.Xaml.Window;
using Uno.UI.UI.Input.Internal;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Uno.UI.Runtime.Skia.Win32;

partial class Win32WindowWrapper
{
	private static System.Drawing.Point PointFromLParam(LPARAM lParam)
	{
		return new System.Drawing.Point((int)(lParam.Value) & 0xffff, (int)(lParam.Value) >> 16);
	}

	private Win32NonClientHitTestKind HitTestNCA(HWND hWnd, WPARAM wParam, LPARAM lParam)
	{
		// Get the point coordinates for the hit test (screen space).
		var ptMouse = PointFromLParam(lParam);

		// Get the window rectangle.
		PInvoke.GetWindowRect(hWnd, out var rcWindow);
		var scaling = (uint)(RasterizationScale * StandardDpi);
		var relativeScaling = RasterizationScale / RasterizationScale;

		///////
		// TODO: If custom drag are was provided, use that instead
		///////

		// Get the frame rectangle, adjusted for the style without a caption.
		var rcFrame = new RECT();
		var borderThickness = new RECT();
		//if (Win32Platform.WindowsVersion < PlatformConstants.Windows10_1607)
		//{
		//	AdjustWindowRectEx(ref rcFrame, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW & ~WindowStyles.WS_CAPTION), false, 0);

		//	rcFrame.top = (int)(rcFrame.top * relativeScaling);
		//	rcFrame.right = (int)(rcFrame.right * relativeScaling);
		//	rcFrame.left = (int)(rcFrame.left * relativeScaling);
		//	rcFrame.bottom = (int)(rcFrame.bottom * relativeScaling);

		//	AdjustWindowRectEx(ref borderThickness, (uint)GetStyle(), false, 0);

		//	borderThickness.top = (int)(borderThickness.top * relativeScaling);
		//	borderThickness.right = (int)(borderThickness.right * relativeScaling);
		//	borderThickness.left = (int)(borderThickness.left * relativeScaling);
		//	borderThickness.bottom = (int)(borderThickness.bottom * relativeScaling);
		//}
		//else
		//{
		PInvoke.AdjustWindowRectExForDpi(ref rcFrame, WINDOW_STYLE.WS_OVERLAPPEDWINDOW & ~WINDOW_STYLE.WS_CAPTION, false, 0, scaling);
		PInvoke.AdjustWindowRectExForDpi(ref borderThickness, GetStyle(), false, 0, scaling);
		//}

		borderThickness.left *= -1;
		borderThickness.top *= -1;

		//TODO
		//var appWindowTitleBar = _window!.AppWindow.TitleBar;
		//if (appWindowTitleBar.Height > 0)
		//{
		//	borderThickness.top = (int)(appWindowTitleBar.Height * scaling);
		//}

		// Determine if the hit test is for resizing. Default middle (1,1).
		ushort uRow = 1;
		ushort uCol = 1;
		bool onResizeBorder = false;

		// Determine if the point is at the left or right of the window.
		if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + borderThickness.left)
		{
			uCol = 0; // left side
		}
		else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - borderThickness.right)
		{
			uCol = 2; // right side
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

		var captionAreaHitTest = _window?.AppWindow.Presenter is FullScreenPresenter ? Win32NonClientHitTestKind.HTNOWHERE : Win32NonClientHitTestKind.HTCAPTION;
		ReadOnlySpan<Win32NonClientHitTestKind> hitZones = stackalloc Win32NonClientHitTestKind[]
		{
			Win32NonClientHitTestKind.HTTOPLEFT, onResizeBorder ? Win32NonClientHitTestKind.HTTOP : captionAreaHitTest,
			Win32NonClientHitTestKind.HTTOPRIGHT, Win32NonClientHitTestKind.HTLEFT, Win32NonClientHitTestKind.HTNOWHERE, Win32NonClientHitTestKind.HTRIGHT,
			Win32NonClientHitTestKind.HTBOTTOMLEFT, Win32NonClientHitTestKind.HTBOTTOM, Win32NonClientHitTestKind.HTBOTTOMRIGHT
		};

		var zoneIndex = uRow * 3 + uCol;

		return hitZones[zoneIndex];
	}

	private LRESULT CustomCaptionProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, out bool handled)
	{
		LRESULT lRet = default;

		handled = PInvoke.DwmDefWindowProc(hWnd, msg, wParam, lParam, out var result);

		switch (msg)
		{
			case PInvoke.WM_DWMCOMPOSITIONCHANGED:
				// TODO handle composition changed.
				break;

			case PInvoke.WM_NCHITTEST:
				if (lRet == IntPtr.Zero)
				{
					var hittestResult = HitTestNCA(hWnd, wParam, lParam);
					if (hittestResult is Win32NonClientHitTestKind.HTNOWHERE or Win32NonClientHitTestKind.HTCAPTION)
					{
						var visualHittestResult = HitTestVisual(lParam);
						if (visualHittestResult != Win32NonClientHitTestKind.HTNOWHERE)
						{
							hittestResult = visualHittestResult;
						}
					}

					if (hittestResult != Win32NonClientHitTestKind.HTNOWHERE)
					{
						lRet = new LRESULT((IntPtr)hittestResult);
						handled = true;
					}
				}
				break;

				//// Normally, Avalonia doesn't handles non-client input as a special NonClientLeftButtonDown, ignoring move and up events.
				//// What makes it a problem, Avalonia has to mark templated caption buttons as a non-client area.
				//// Meaning, these buttons no longer can accept normal client input.
				//// These messages are needed to explicitly fake this normal client input from non-client messages.
				//// For both WM_NCMOUSE and WM_NCPOINTERUPDATE
				//case PInvoke.WM_NCMOUSEMOVE when !IsMouseInPointerEnabled:
				//case PInvoke.WM_NCLBUTTONDOWN when !IsMouseInPointerEnabled:
				//case PInvoke.WM_NCLBUTTONUP when !IsMouseInPointerEnabled:
				//	if (lRet == IntPtr.Zero
				//		&& ShouldRedirectNonClientInput(hWnd, wParam, lParam))
				//	{
				//		e = new RawPointerEventArgs(
				//			_mouseDevice,
				//			unchecked((uint)GetMessageTime()),
				//			Owner,
				//			(WindowsMessage)msg switch
				//			{
				//				PInvoke.WM_NCMOUSEMOVE => RawPointerEventType.Move,
				//				PInvoke.WM_NCLBUTTONDOWN => RawPointerEventType.LeftButtonDown,
				//				PInvoke.WM_NCLBUTTONUP => RawPointerEventType.LeftButtonUp,
				//				_ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
				//			},
				//			PointToClient(PointFromLParam(lParam)),
				//			RawInputModifiers.None);
				//	}
				//	break;
				//case PInvoke.WM_NCPOINTERUPDATE when _wmPointerEnabled:
				//case PInvoke.WM_NCPOINTERDOWN when _wmPointerEnabled:
				//case PInvoke.WM_NCPOINTERUP when _wmPointerEnabled:
				//	if (lRet == IntPtr.Zero
				//		&& ShouldRedirectNonClientInput(hWnd, wParam, lParam))
				//	{
				//		uint timestamp = 0;
				//		GetDevicePointerInfo(wParam, out var device, out var info, out var point, out var modifiers, ref timestamp);
				//		var eventType = (WindowsMessage)msg switch
				//		{
				//			PInvoke.WM_NCPOINTERUPDATE => RawPointerEventType.Move,
				//			PInvoke.WM_NCPOINTERDOWN => RawPointerEventType.LeftButtonDown,
				//			PInvoke.WM_NCPOINTERUP => RawPointerEventType.LeftButtonUp,
				//			_ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
				//		};
				//		e = CreatePointerArgs(device, timestamp, eventType, point, modifiers, info.pointerId);
				//	}
				//	break;
		}

		//if (e is not null && Input is not null)
		//{
		//	Input(e);
		//	if (e.Handled)
		//	{
		//		callDwp = false;
		//		return IntPtr.Zero;
		//	}
		//}

		return lRet;
	}

	public Windows.Foundation.Point PointToClient(System.Drawing.Point point)
	{
		var p = new System.Drawing.Point(point.X, point.Y);
		PInvoke.ScreenToClient(_hwnd, ref p);
		return new Windows.Foundation.Point((float)(p.X / RasterizationScale), (float)(p.Y /RasterizationScale));
	}

	private Win32NonClientHitTestKind HitTestVisual(IntPtr lParam)
	{
		var position = PointToClient(PointFromLParam(lParam));
		//if (_window is { } window)
		//{
		//	var visual = window.GetVisualAt(position, x =>
		//	{
		//		if (x is IInputElement ie && (!ie.IsHitTestVisible || !ie.IsEffectivelyVisible))
		//		{
		//			return false;
		//		}

		//		return true;
		//	});

		//	if (visual != null)
		//	{
		//		var hitTest = Win32Properties.GetNonClientHitTestResult(visual);
		//		return (Win32NonClientHitTestKind)hitTest;
		//	}
		//}
		return Win32NonClientHitTestKind.HTNOWHERE;
	}

	private bool ShouldRedirectNonClientInput(HWND hWnd, WPARAM wParam, LPARAM lParam)
	{
		// We touched frame borders or caption, don't redirect.
		if (HitTestNCA(hWnd, wParam, lParam) is not (Win32NonClientHitTestKind.HTNOWHERE or Win32NonClientHitTestKind.HTCAPTION))
			return false;

		// Redirect only for buttons.
		return HitTestVisual(lParam)
			is Win32NonClientHitTestKind.HTMINBUTTON
			or Win32NonClientHitTestKind.HTMAXBUTTON
			or Win32NonClientHitTestKind.HTCLOSE
			or Win32NonClientHitTestKind.HTHELP
			or Win32NonClientHitTestKind.HTMENU
			or Win32NonClientHitTestKind.HTSYSMENU;
	}
}
