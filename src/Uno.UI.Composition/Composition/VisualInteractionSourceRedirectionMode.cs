#nullable enable

namespace Windows.UI.Composition.Interactions;

public enum VisualInteractionSourceRedirectionMode
{
	/// <summary>
	/// Redirection is off, all input goes to the UI thread.
	/// </summary>
	Off = 0,

	/// <summary>
	/// Pointer input goes to the UI thread, Precision Touchpad input goes to the compositor.
	/// </summary>
	CapableTouchpadOnly = 1,

	/// <summary>
	/// Pointer input goes to the UI thread, mouse wheel input goes to the compositor.
	/// </summary>
	PointerWheelOnly = 2,

	/// <summary>
	/// Pointer input goes to the UI thread, Precision Touchpad and mouse wheel input goes to the compositor.
	/// </summary>
	CapableTouchpadAndPointerWheel = 3,
}
