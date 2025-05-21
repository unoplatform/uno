using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class NativeOverlappedPresenter : INativeOverlappedPresenter
{
	private readonly UnoWpfWindow _wpfWindow;
	private bool _isMinimizable = true;
	private bool _isMaximizable = true;
	private bool _isResizable = true;

	public NativeOverlappedPresenter(UnoWpfWindow wpfWindow)
	{
		_wpfWindow = wpfWindow;
	}

	public OverlappedPresenterState State => _wpfWindow.WindowState switch
	{
		System.Windows.WindowState.Maximized => OverlappedPresenterState.Maximized,
		System.Windows.WindowState.Minimized => OverlappedPresenterState.Minimized,
		System.Windows.WindowState.Normal => OverlappedPresenterState.Restored,
		_ => throw new InvalidOperationException($"Unknown window state: {_wpfWindow.WindowState}")
	};

	public void Maximize() => _wpfWindow.WindowState = System.Windows.WindowState.Maximized;
	public void Minimize(bool activateWindow)
	{
		_wpfWindow.WindowState = System.Windows.WindowState.Minimized;
		if (activateWindow)
		{
			_wpfWindow.Activate();
		}
	}

	public void Restore(bool activateWindow)
	{
		_wpfWindow.WindowState = System.Windows.WindowState.Normal;
		if (activateWindow)
		{
			_wpfWindow.Activate();
		}
	}

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		_wpfWindow.WindowStyle = hasBorder ? System.Windows.WindowStyle.SingleBorderWindow : System.Windows.WindowStyle.None;
		// TODO: HasTitleBar support
	}

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop) => _wpfWindow.Topmost = isAlwaysOnTop;

	public void SetIsMaximizable(bool isMaximizable)
	{
		_isMaximizable = isMaximizable;
		SetSizing();
	}

	public void SetIsMinimizable(bool isMinimizable)
	{
		_isMinimizable = isMinimizable;
		SetSizing();
	}

	public void SetIsResizable(bool isResizable)
	{
		_isResizable = isResizable;
		SetSizing();
	}

	public void SetIsModal(bool isModal)
	{
		// TODO: Implement modal
	}

	private void SetSizing()
	{
		if (!_isResizable)
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.NoResize;
		}
		else if (_isMinimizable && !_isMaximizable)
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.CanMinimize;
		}
		else
		{
			_wpfWindow.ResizeMode = System.Windows.ResizeMode.CanResize;
		}
	}

	public void SetPreferredMaximumSize(int? preferredMaximumWidth, int? preferredMinimumHeight) { }

	public void SetPreferredMinimumSize(int? preferredMinimumWidth, int? preferredMinimumHeight) { }
}
