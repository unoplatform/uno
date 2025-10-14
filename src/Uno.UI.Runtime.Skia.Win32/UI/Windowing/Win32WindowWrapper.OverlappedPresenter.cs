using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper : INativeOverlappedPresenter
{
	private OverlappedPresenterState? _pendingState;

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

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		// Toggle the standard caption (title bar)
		SetWindowStyle(WINDOW_STYLE.WS_CAPTION, hasTitleBar);
		// Toggle the resize border (thick frame)
		SetWindowStyle(WINDOW_STYLE.WS_SIZEBOX, hasBorder);

		// Apply non-client frame changes immediately
		var success = PInvoke.SetWindowPos(
			_hwnd,
			HWND.Null,
			0,
			0,
			0,
			0,
			SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.SetWindowPos)} failed: {Win32Helper.GetErrorMessage()}");
		}

		// Ensure our cached bounds reflect the new client area
		OnWindowSizeOrLocationChanged();
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
