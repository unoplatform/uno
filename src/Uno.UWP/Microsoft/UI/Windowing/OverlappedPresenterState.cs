namespace Microsoft.UI.Windowing;

/// <summary>
/// Defines constants that specify the state of a window in overlapped mode.
/// </summary>
public enum OverlappedPresenterState
{
	/// <summary>
	/// The window is maximized.
	/// </summary>
	Maximized = 0,

	/// <summary>
	/// The window is minimized.
	/// </summary>
	Minimized = 1,

	/// <summary>
	/// The window is restored to the size and position it had before it was minimized or maximized.
	/// </summary>
	Restored = 2,
}
