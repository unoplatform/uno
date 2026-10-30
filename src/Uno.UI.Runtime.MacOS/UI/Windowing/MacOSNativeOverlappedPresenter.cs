using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.MacOS;

internal class MacOSNativeOverlappedPresenter : INativeOverlappedPresenter
{
	private readonly MacOSWindowNative _window;
	private readonly Action _onBorderAndTitleBarChanged;

	public MacOSNativeOverlappedPresenter(MacOSWindowNative window, Action onBorderAndTitleBarChanged)
	{
		_window = window;
		_onBorderAndTitleBarChanged = onBorderAndTitleBarChanged;
	}

	// The handle is read on each call: it's zero once the window has closed, and the NSWindow is freed soon after.
	private void WithHandle(Action<nint> action)
	{
		if (_window.Handle is var handle && handle != nint.Zero)
		{
			action(handle);
		}
	}

	public OverlappedPresenterState State => _window.Handle is var handle && handle != nint.Zero
		? (OverlappedPresenterState)NativeUno.uno_window_get_overlapped_presenter_state(handle)
		: OverlappedPresenterState.Restored;

	public void Maximize() => WithHandle(NativeUno.uno_window_maximize);

	public void Minimize(bool activateWindow) => WithHandle(handle => NativeUno.uno_window_minimize(handle, activateWindow));

	public void Restore(bool activateWindow) => WithHandle(handle => NativeUno.uno_window_restore(handle, activateWindow));

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar) => WithHandle(handle =>
	{
		NativeUno.uno_window_set_border_and_title_bar(handle, hasBorder, hasTitleBar);
		_onBorderAndTitleBarChanged();
	});

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop) => WithHandle(handle => NativeUno.uno_window_set_always_on_top(handle, isAlwaysOnTop));

	public void SetIsMaximizable(bool isMaximizable) => WithHandle(handle => NativeUno.uno_window_set_maximizable(handle, isMaximizable));

	public void SetIsMinimizable(bool isMinimizable) => WithHandle(handle => NativeUno.uno_window_set_minimizable(handle, isMinimizable));

	public void SetIsModal(bool isModal) => WithHandle(handle =>
	{
		// we cannot set `modalPanel`, it's a readonly property, subclasses of NSPanel are modal, NSWindow are not
		// so we warn if we try to set a value that does not match the native window
		if (!NativeUno.uno_window_set_modal(handle, isModal))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Cannot change NSWindow {handle} `modalPanel` after creation (readonly).");
			}
		}
	});

	public void SetIsResizable(bool isResizable) => WithHandle(handle => NativeUno.uno_window_set_resizable(handle, isResizable));

	public void SetSizeConstraints(int? preferredMinimumWidth, int? preferredMinimumHeight, int? preferredMaximumWidth, int? preferredMaximumHeight) => WithHandle(handle =>
	{
		double minWidth = preferredMinimumWidth ?? 0.0;
		double minHeight = preferredMinimumHeight ?? 0.0;

		// For unset, macOS uses FLT_MAX.
		double maxWidth = preferredMaximumWidth ?? float.MaxValue;
		double maxHeight = preferredMaximumHeight ?? float.MaxValue;
		NativeUno.uno_window_set_min_size(handle, minWidth, minHeight);
		NativeUno.uno_window_set_max_size(handle, maxWidth, maxHeight);
	});
}
