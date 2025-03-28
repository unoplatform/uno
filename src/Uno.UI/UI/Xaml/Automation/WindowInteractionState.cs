namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Defines values that specify the current state of the window for purposes of user or programmatic interaction.
/// </summary>
public enum WindowInteractionState
{
	/// <summary>
	/// The window is running. This doesn't guarantee that 
	/// the window is responding or ready for user interaction.
	/// </summary>
	Running = 0,

	/// <summary>
	/// The window is closing.
	/// </summary>
	Closing = 1,

	/// <summary>
	/// The window is ready for user interaction.
	/// </summary>
	ReadyForUserInteraction = 2,

	/// <summary>
	/// The window is blocked by a modal window.
	/// </summary>
	BlockedByModalWindow = 3,

	/// <summary>
	/// The window is not responding.
	/// </summary>
	NotResponding = 4,
}
