namespace Microsoft.UI.Input;

/// <summary>
/// Specifies the possible reasons for a focus navigation event.
/// </summary>
public enum FocusNavigationReason
{
	/// <summary>
	/// Programmatically perform focus navigation.
	/// </summary>
	Programmatic = 0,

	/// <summary>
	/// Restore focus to a previous state.
	/// </summary>
	Restore = 1,

	/// <summary>
	/// Navigate to the first element in the focus recipient.
	/// </summary>
	First = 2,

	/// <summary>
	/// Navigate to the last element in the focus recipient.
	/// </summary>
	Last = 3,

	/// <summary>
	/// Navigate focus left.
	/// </summary>
	Left = 4,

	/// <summary>
	/// Navigate focus up.
	/// </summary>
	Up = 5,

	/// <summary>
	/// Navigate focus right.
	/// </summary>
	Right = 6,

	/// <summary>
	/// Navigate focus down.
	/// </summary>
	Down = 7,
}
