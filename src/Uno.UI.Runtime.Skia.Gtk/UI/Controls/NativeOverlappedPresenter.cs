using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class NativeOverlappedPresenter : INativeOverlappedPresenter
{
	private readonly UnoGtkWindow _gtkWindow;

	public NativeOverlappedPresenter(UnoGtkWindow gtkWindow)
	{
		_gtkWindow = gtkWindow;
	}

	public OverlappedPresenterState State => OverlappedPresenterState.Restored;
	public void Maximize() => _gtkWindow.Window.Maximize();
	public void Minimize(bool activateWindow) { }
	public void Restore(bool activateWindow) { }
	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar) { }
	public void SetIsAlwaysOnTop(bool isAlwaysOnTop) { }

	public void SetIsMaximizable(bool isMaximizable) { }
	public void SetIsMinimizable(bool isMinimizable) { }
	public void SetIsModal(bool isModal) { }
	public void SetIsResizable(bool isResizable) => _gtkWindow.Resizable = isResizable;
}
