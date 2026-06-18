#nullable enable
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.X11;

// No portable programmatic API exists to toggle the on-screen keyboard on X11 desktops
// (it's environment-specific: GNOME a11y, squeekboard/Wayland, onboard). Honest no-op.
internal sealed class X11InputPaneExtension : IInputPaneExtension
{
	internal static X11InputPaneExtension Instance { get; } = new();

	private X11InputPaneExtension()
	{
	}

	public bool TryShow() => false;

	public bool TryHide() => false;
}
