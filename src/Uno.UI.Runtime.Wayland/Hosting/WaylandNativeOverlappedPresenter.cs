#nullable enable

using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;

namespace Uno.UI.Runtime.Wayland;

// xdg-shell has no always-on-top or modality requests; minimized state is not reported by the compositor.
internal sealed class WaylandNativeOverlappedPresenter(WaylandWindowWrapper wrapper) : INativeOverlappedPresenter
{
	public OverlappedPresenterState State => wrapper.PresenterState;

	public void SetIsResizable(bool isResizable) => wrapper.SetIsResizable(isResizable);

	public void SetIsModal(bool isModal)
	{
		// TODO: modal windows (X11 does not support them either).
	}

	public void SetIsMinimizable(bool isMinimizable) => wrapper.SetIsMinimizable(isMinimizable);

	public void SetIsMaximizable(bool isMaximizable) => wrapper.SetIsMaximizable(isMaximizable);

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
	}

	public void Maximize() => wrapper.Maximize();

	public void Minimize(bool activateWindow) => wrapper.Minimize();

	public void Restore(bool activateWindow) => wrapper.Restore(activateWindow);

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar) => wrapper.SetHasTitleBar(hasTitleBar);

	public void SetSizeConstraints(int? minWidth, int? minHeight, int? maxWidth, int? maxHeight)
		=> wrapper.SetSizeConstraints(minWidth, minHeight, maxWidth, maxHeight);
}
