using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeOverlappedPresenter : INativeOverlappedPresenter
{
	private readonly nint _handle;

	public MacOSNativeOverlappedPresenter(MacOSWindowNative window)
	{
		_handle = window.Handle;
	}

	public OverlappedPresenterState State => (OverlappedPresenterState)NativeUno.uno_window_get_overlapped_presenter_state(_handle);

	public void Maximize() => NativeUno.uno_window_maximize(_handle);

	public void Minimize(bool activateWindow) => NativeUno.uno_window_minimize(_handle, activateWindow);

	public void Restore(bool activateWindow) => NativeUno.uno_window_restore(_handle, activateWindow);

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar) => NativeUno.uno_window_set_border_and_title_bar(_handle, hasBorder, hasTitleBar);

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop) => NativeUno.uno_window_set_always_on_top(_handle, isAlwaysOnTop);

	public void SetIsMaximizable(bool isMaximizable) => NativeUno.uno_window_set_maximizable(_handle, isMaximizable);

	public void SetIsMinimizable(bool isMinimizable) => NativeUno.uno_window_set_minimizable(_handle, isMinimizable);

	public void SetIsModal(bool isModal)
	{
		// we cannot set `modalPanel`, it's a readonly property, subclasses of NSPanel are modal, NSWindow are not
		// so we warn if we try to set a value that does not match the native window
		if (!NativeUno.uno_window_set_modal(_handle, isModal))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Cannot change NSWindow {_handle} `modalPanel` after creation (readonly).");
			}
		}
	}

	public void SetIsResizable(bool isResizable) => NativeUno.uno_window_set_resizable(_handle, isResizable);

	public void SetPreferredMaximumSize(int? preferredMaximumWidth, int? preferredMinimumHeight) { }

	public void SetPreferredMinimumSize(int? preferredMinimumWidth, int? preferredMaximumHeight) { }
}
