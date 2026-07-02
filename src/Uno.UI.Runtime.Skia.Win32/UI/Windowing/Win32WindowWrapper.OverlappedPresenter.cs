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
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using MARGINS = Windows.Win32.UI.Controls.MARGINS;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : INativeOverlappedPresenter
{
	private OverlappedPresenterState? _pendingState;
	private nuint _lastWindowSizeChange = 0;

	private bool _hasBorder;
	private bool _hasTitleBar;
	private WINDOW_STYLE _savedStyle;

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

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar) => UpdateClientAreaExtension();

	internal unsafe void UpdateClientAreaExtension()
	{
		if (_window is null)
		{
			throw new InvalidOperationException("Window was not set.");
		}

		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			_hasBorder = presenter.HasBorder;
			_hasTitleBar = presenter.HasTitleBar;
		}

		if (_window.ExtendsContentIntoTitleBar)
		{
			_hasTitleBar = false;
		}

		if (PInvoke.DwmIsCompositionEnabled(out var compositionEnabled) < 0 || !compositionEnabled)
		{
			_window.ExtendsContentIntoTitleBar = false;
			return;
		}

		PInvoke.GetWindowRect(_hwnd, out var windowRectangle);

		var extendContentIntoTitleBar = !_hasTitleBar;

		// Adjust sysmenu - this will completely avoid the "system" caption area, allowing the maximize snap layout popup to work properly.
		SetWindowStyle(WINDOW_STYLE.WS_SYSMENU, _hasTitleBar);

		if (extendContentIntoTitleBar && _window.AppWindow.Presenter is not FullScreenPresenter)
		{
			var margins = UpdateClientAreaExtensionMargins();
			ApplyFrameExtension(margins);

			int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
		}
		else
		{
			var margins = new MARGINS();
			ApplyFrameExtension(margins);

			int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
			PInvoke.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &cornerPreference, sizeof(int));
		}

		if (WasShown)
		{
			_forcePaintOnNextEraseBkgndOrNcPaint = true;
			// Inform the application of the frame change.
			PInvoke.SetWindowPos(_hwnd,
				HWND.Null,
				windowRectangle.left, windowRectangle.top,
				0, 0,
				SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
		}
	}

	// When a SystemBackdrop is active we must always extend the DWM frame across the whole client
	// area ("sheet of glass"), otherwise Mica/Acrylic only show under the non-client frame and the
	// client area renders as black. This wins over the title-bar-extension margins because the
	// {-1,-1,-1,-1} sentinel already covers them.
	private void ApplyFrameExtension(MARGINS margins)
	{
		if (HasActiveSystemBackdrop())
		{
			margins.cxLeftWidth = -1;
			margins.cxRightWidth = -1;
			margins.cyTopHeight = -1;
			margins.cyBottomHeight = -1;
		}

		PInvoke.DwmExtendFrameIntoClientArea(_hwnd, in margins);
	}

	// Gated on the controller IsSupported() checks (which require Windows 11 22621+) so we don't
	// force the "sheet of glass" frame extension on OS versions where SetSystemBackdrop is a no-op.
	private bool HasActiveSystemBackdrop()
		=> _window?.SystemBackdrop switch
		{
			Microsoft.UI.Xaml.Media.MicaBackdrop
				=> Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported(),
			Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop
				=> Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported(),
			_ => false,
		};

	private MARGINS UpdateClientAreaExtensionMargins()
	{
		RECT borderCaptionThickness = new RECT();

		var scaling = (uint)(RasterizationScale * PInvoke.USER_DEFAULT_SCREEN_DPI);
		var relativeScaling = RasterizationScale / GetPrimaryMonitorScale();

		if (Environment.OSVersion.Version < new Version(10, 0, 14393))
		{
			PInvoke.AdjustWindowRectEx(ref borderCaptionThickness, GetStyle(), false, 0);

			borderCaptionThickness.top = (int)(borderCaptionThickness.top * relativeScaling);
			borderCaptionThickness.right = (int)(borderCaptionThickness.right * relativeScaling);
			borderCaptionThickness.left = (int)(borderCaptionThickness.left * relativeScaling);
			borderCaptionThickness.bottom = (int)(borderCaptionThickness.bottom * relativeScaling);
		}
		else
		{
			PInvoke.AdjustWindowRectExForDpi(ref borderCaptionThickness, GetStyle(), false, 0, scaling);
		}

		borderCaptionThickness.top *= -1;

		bool hasSystemTitleBar = _hasTitleBar;

		if (!hasSystemTitleBar)
		{
			borderCaptionThickness.top = 1;
		}

		var defaultMargin = 1;

		MARGINS margins = new MARGINS();
		margins.cxLeftWidth = defaultMargin;
		margins.cxRightWidth = defaultMargin;
		margins.cyBottomHeight = defaultMargin;

		if (!hasSystemTitleBar && _window!.AppWindow.TitleBar.Height > 0)
		{
			borderCaptionThickness.top = (int)(_window.AppWindow.TitleBar.Height);
		}

		margins.cyTopHeight = _window!.AppWindow.TitleBar.ExtendsContentIntoTitleBar ? borderCaptionThickness.top : defaultMargin;

		return margins;
	}

	private WINDOW_STYLE GetStyle()
	{
		if (_window?.AppWindow.Presenter is FullScreenPresenter)
		{
			return _savedStyle;
		}
		else
		{
			return (WINDOW_STYLE)PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		}
	}

	// Insets a maximized custom-chrome window's proposed client rect back to the monitor work area. The OS
	// positions a maximized resizable window so its resize frame overhangs the monitor on every edge; custom
	// chrome draws into that area, so without correction the content and caption buttons spill off-screen.
	// Clamping the proposed rect (rgrc[0], a screen-space WINDOW rect) to the work area makes the client exactly
	// the visible, non-taskbar region - identical to a standard maximized window and to WinUI - with no
	// frame-thickness/DPI math (which can be off by a pixel). The monitor comes from the proposed rect, so this
	// is correct on a secondary monitor with a different DPI and during restore-from-minimized.
	private unsafe void ApplyMaximizedClientInset(ref RECT client)
	{
		var monitor = PInvoke.MonitorFromRect(in client, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
		if (monitor.Value == IntPtr.Zero)
		{
			return;
		}

		var monitorInfo = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
		if (!PInvoke.GetMonitorInfo(monitor, &monitorInfo))
		{
			return;
		}

		var work = monitorInfo.rcWork;
		client.left = Math.Max(client.left, work.left);
		client.top = Math.Max(client.top, work.top);
		client.right = Math.Min(client.right, work.right);
		client.bottom = Math.Min(client.bottom, work.bottom);

		ReserveAutoHideTaskbarEdges(ref client, monitorInfo.rcMonitor);
	}

	// An auto-hide taskbar reserves no work area, so the work-area clamp above leaves the client flush with that
	// monitor edge. A maximized window that fully covers the edge hosting an auto-hide taskbar is treated by
	// Windows as a full-screen app and the bar can no longer be revealed, so keep the client 1px short of any
	// such edge - matching the native WinUI windowing layer.
	private void ReserveAutoHideTaskbarEdges(ref RECT client, RECT rcMonitor)
	{
		const int reserve = 1;
		if (Win32AutoHideTaskbar.ExistsOnEdge(Win32AutoHideTaskbar.ABE_LEFT, rcMonitor)) { client.left += reserve; }
		if (Win32AutoHideTaskbar.ExistsOnEdge(Win32AutoHideTaskbar.ABE_TOP, rcMonitor)) { client.top += reserve; }
		if (Win32AutoHideTaskbar.ExistsOnEdge(Win32AutoHideTaskbar.ABE_RIGHT, rcMonitor)) { client.right -= reserve; }
		if (Win32AutoHideTaskbar.ExistsOnEdge(Win32AutoHideTaskbar.ABE_BOTTOM, rcMonitor)) { client.bottom -= reserve; }
	}

	private void SetWindowStyle(WINDOW_STYLE style, bool on)
	{
		var oldStyle = PInvoke.GetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
		if (oldStyle is 0 && Marshal.GetLastWin32Error() != (int)WIN32_ERROR.ERROR_SUCCESS && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.GetWindowLong)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		var actualStyle = on ? oldStyle | (int)style : oldStyle & ~(int)style;
		_savedStyle = (WINDOW_STYLE)actualStyle;

		var res = PInvoke.SetWindowLong(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, actualStyle);
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

	private void NotifyMinMaxSizeChange()
	{
		if (!PInvoke.GetWindowRect(_hwnd, out var rect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetWindowRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}
		// We are setting the window rect to itself to trigger a WM_GETMINMAXINFO
		var success = PInvoke.SetWindowPos(_hwnd, HWND.Null, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}"); }
	}
}
