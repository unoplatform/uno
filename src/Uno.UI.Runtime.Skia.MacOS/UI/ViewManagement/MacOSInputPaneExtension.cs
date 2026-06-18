#nullable enable
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.MacOS;

// macOS has no public API to invoke the Keyboard Viewer / Accessibility Keyboard; it is
// user-controlled. Honest no-op.
internal sealed class MacOSInputPaneExtension : IInputPaneExtension
{
	internal static MacOSInputPaneExtension Instance { get; } = new();

	private MacOSInputPaneExtension()
	{
	}

	public bool TryShow() => false;

	public bool TryHide() => false;
}
