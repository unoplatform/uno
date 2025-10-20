using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using MARGINS = Windows.Win32.UI.Controls.MARGINS;

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
	public void SetIsMaximizable(bool isMaximizable) => SetWindowStyle(WINDOW_STYLE.WS_MINIMIZEBOX, isMaximizable);

	public unsafe void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		var extendsIntoTitleBar = _window?.ExtendsContentIntoTitleBar ?? false;
		if (extendsIntoTitleBar)
		{
			hasTitleBar = false;
		}
		SetWindowStyle(WINDOW_STYLE.WS_CAPTION, hasTitleBar);
		SetWindowStyle(WINDOW_STYLE.WS_SIZEBOX, hasBorder);

		BOOL enable = false;
		PInvoke.DwmSetWindowAttribute(_hwnd, Windows.Win32.Graphics.Dwm.DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_ENABLED, &enable, (uint)Marshal.SizeOf(enable));

		// Remove the "thin titlebar" strip on top of the window
		MARGINS margins = new MARGINS()
		{
			cxLeftWidth = 0,
			cxRightWidth = 0,
			cyTopHeight = hasTitleBar ? 0 : -1,
			cyBottomHeight = 0
		};
		PInvoke.DwmExtendFrameIntoClientArea(_hwnd, margins);

		if (WasShown)
		{
			PInvoke.GetWindowRect(_hwnd, out var rcWindow);

			// Inform the application of the frame change.
			PInvoke.SetWindowPos(_hwnd,
				HWND.Null,
				rcWindow.left, rcWindow.top,
				0, 0,
				SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
		}
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
