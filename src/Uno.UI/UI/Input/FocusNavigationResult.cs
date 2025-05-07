namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the possible results of a focus navigation event.
/// </summary>
public enum FocusNavigationResult
{
	/// <summary>
	/// Event was not subscribed or the event ran into an error. This is the default value.
	/// </summary>
	NotMoved = 0,

	/// <summary>
	/// Focus successfully moved to another component.
	/// </summary>
	Moved = 1,

	/// <summary>
	/// No focusable element was found.
	/// </summary>
	NoFocusableElements = 2,
}
