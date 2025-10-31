using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using MARGINS = Windows.Win32.UI.Controls.MARGINS;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : INativeOverlappedPresenter
{
	private OverlappedPresenterState? _pendingState;
	private nuint _lastWindowSizeChange = 0;

	private bool _hasBorder;
	private bool _hasTitleBar;
	protected override IDisposable ApplyFullScreenPresenter()
	{
		// The WasShown guards are so that if a call to ApplyFullScreenPresenter is made before
		// the window is shown, we postpone the logic below until ShowCore is called. Otherwise,
		// it doesn't work.
		if (WasShown)
		{
			SetWindowStyle(WINDOW_STYLE.WS_DLGFRAME, false);
			_ = PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);
		}

		return Disposable.Create(() =>
		{
			if (WasShown)
			{
				SetWindowStyle(WINDOW_STYLE.WS_DLGFRAME, true);
				_ = PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
			}
		});
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(this);
		return base.ApplyOverlappedPresenter(presenter);
	}

	public void SetIsModal(bool isModal) { /* TODO: Implement modal */ }

	public void SetIsResizable(bool isResizable) => SetWindowStyle(WINDOW_STYLE.WS_SIZEBOX, isResizable);

	public void SetIsMinimizable(bool isMinimizable) => SetWindowStyle(WINDOW_STYLE.WS_MINIMIZEBOX, isMinimizable);

	public void SetIsMaximizable(bool isMaximizable) => SetWindowStyle(WINDOW_STYLE.WS_MAXIMIZEBOX, isMaximizable);

	private void UpdateWindowState(WPARAM wParam)
	{
		if (wParam.Value != _lastWindowSizeChange)
		{
			// The window state changed, notify the AppWindow.
			_window?.AppWindow.OnAppWindowChanged(AppWindowChangedEventArgs.PresenterChangedEventArgs);
			_lastWindowSizeChange = wParam.Value;
		}
	}

	public unsafe void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		if (_window is null)
		{
			throw new InvalidOperationException("Window was not set.");
		}

		var extendsIntoTitleBar = _window.ExtendsContentIntoTitleBar;
		if (extendsIntoTitleBar)
		{
			hasTitleBar = false;
		}

		_hasBorder = hasBorder;
		_hasTitleBar = hasTitleBar;

		if (PInvoke.DwmIsCompositionEnabled(out var compositionEnabled) < 0 || !compositionEnabled)
		{
			_window.ExtendsContentIntoTitleBar = false;
			return;
		}

		PInvoke.GetWindowRect(_hwnd, out var rcWindow);

		var extendContentIntoTitleBar = !hasTitleBar;

		if (extendContentIntoTitleBar && _window.AppWindow.Presenter is not FullScreenPresenter)
		{
			var margins = GetMargins();
			PInvoke.DwmExtendFrameIntoClientArea(_hwnd, in margins);

			int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
		}
		else
		{
			var margins = new MARGINS();
			PInvoke.DwmExtendFrameIntoClientArea(_hwnd, in margins);

			int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
		}

		if (extendContentIntoTitleBar)
		{
			PInvoke.SetWindowText(_hwnd, string.Empty);
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_SMALL, IntPtr.Zero);
			PInvoke.SendMessage(_hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, IntPtr.Zero);

			int transparent = 0x00000000; // ARGB (A=0 -> transparent)
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, &transparent, sizeof(int));
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR, &transparent, sizeof(int));

			int backdropNone = 1; // 1 == Mica, 2 == Acrylic, 3 == Tabbed
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &backdropNone, sizeof(int));
		}

		// Inform the application of the frame change.
		PInvoke.SetWindowPos(_hwnd,
			HWND.Null,
			rcWindow.left, rcWindow.top,
			0, 0,
			SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
		_renderer.OnWindowExtendedIntoTitleBar();
	}

	private MARGINS GetMargins()
	{
		RECT borderThickness = new RECT();
		RECT borderCaptionThickness = new RECT();

		var scaling = (uint)(RasterizationScale * PInvoke.USER_DEFAULT_SCREEN_DPI);
		if (Environment.OSVersion.Version < new Version(10, 0, 14393))
		{
			PInvoke.AdjustWindowRectEx(ref borderCaptionThickness, GetStyle(), false, 0);
			PInvoke.AdjustWindowRectEx(ref borderThickness, GetStyle() & ~WINDOW_STYLE.WS_CAPTION, false, 0);
		}
		else
		{
			PInvoke.AdjustWindowRectExForDpi(ref borderCaptionThickness, GetStyle(), false, 0, scaling);
			PInvoke.AdjustWindowRectExForDpi(ref borderThickness, GetStyle() & ~WINDOW_STYLE.WS_CAPTION, false, 0, scaling);
		}

		borderThickness.left *= -1;
		borderThickness.top *= -1;
		borderCaptionThickness.left *= -1;
		borderCaptionThickness.top *= -1;

		bool wantsTitleBar = (!_window?.AppWindow.TitleBar.ExtendsContentIntoTitleBar) ?? true;

		if (!wantsTitleBar)
		{
			borderCaptionThickness.top = 1;
		}

		var defaultMargin = 1;

		MARGINS margins = new MARGINS();
		margins.cxLeftWidth = defaultMargin;
		margins.cxRightWidth = defaultMargin;
		margins.cyBottomHeight = defaultMargin;
		margins.cyTopHeight = defaultMargin;

		return margins;
	}

	private WINDOW_STYLE GetStyle()
	{
		//if (_isFullScreenActive)
		//{
		//	return _savedWindowInfo.Style;
		//}
		//else
		//{
		return (WINDOW_STYLE)PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		//}
	}

	private void SetWindowStyle(WINDOW_STYLE style, bool on)
	{
		var oldStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		if (oldStyle is 0 && Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.GetWindowLong)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var res = PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, on ? oldStyle | (int)style : oldStyle & ~(int)style);
		if (res is 0 && Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.SetWindowLong)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
		if (!PInvoke.SetWindowPos(_hwnd, isAlwaysOnTop ? HWND.HWND_TOPMOST : HWND.HWND_NOTOPMOST, 0, 0, 0, 0,
				SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE))
		{
			this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	public void Maximize()
	{
		if (WasShown)
		{
			PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);
		}
		else
		{
			_pendingState = OverlappedPresenterState.Maximized;
		}
	}

	public void Minimize(bool activateWindow)
	{
		if (WasShown)
		{
			PInvoke.ShowWindow(_hwnd, activateWindow ? SHOW_WINDOW_CMD.SW_MINIMIZE : SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE);
		}
		else
		{
			_pendingState = OverlappedPresenterState.Minimized;
		}
	}

	public void Restore(bool activateWindow)
	{
		if (WasShown)
		{
			PInvoke.ShowWindow(_hwnd, activateWindow ? SHOW_WINDOW_CMD.SW_SHOWNORMAL : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
		}
		else
		{
			_pendingState = OverlappedPresenterState.Restored;
		}
	}

	public void SetSizeConstraints(int? preferredMinimumWidth, int? preferredMinimumHeight, int? preferredMaximumWidth, int? preferredMaximumHeight) => NotifyMinMaxSizeChange();

	public OverlappedPresenterState State
	{
		get
		{
			WINDOWPLACEMENT placement = default;
			if (!PInvoke.GetWindowPlacement(_hwnd, ref placement))
			{
				this.LogError()?.Error($"{nameof(PInvoke.GetWindowPlacement)} failed: {Win32Helper.GetErrorMessage()}");
				return OverlappedPresenterState.Restored;
			}

			switch (placement.showCmd)
			{
				case SHOW_WINDOW_CMD.SW_FORCEMINIMIZE:
				case SHOW_WINDOW_CMD.SW_MINIMIZE:
				case SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE:
				case SHOW_WINDOW_CMD.SW_SHOWMINIMIZED:
					return OverlappedPresenterState.Minimized;
				case SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED:
					return OverlappedPresenterState.Maximized;
				case SHOW_WINDOW_CMD.SW_HIDE:
				case SHOW_WINDOW_CMD.SW_SHOWNORMAL:
				case SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE:
				case SHOW_WINDOW_CMD.SW_SHOW:
				case SHOW_WINDOW_CMD.SW_SHOWNA:
				case SHOW_WINDOW_CMD.SW_RESTORE:
				case SHOW_WINDOW_CMD.SW_SHOWDEFAULT:
					return OverlappedPresenterState.Restored;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
