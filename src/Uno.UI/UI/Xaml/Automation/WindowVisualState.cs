namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify the visual state of a window for the IWindowProvider pattern.
/// </summary>
public enum WindowVisualState
{
	/// <summary>
	/// Specifies that the window is normal (restored).
	/// </summary>
	Normal = 0,

	/// <summary>
	/// Specifies that the window is maximized.
	/// </summary>
	Maximized = 1,

	/// <summary>
	/// Specifies that the window is minimized.
	/// </summary>
	Minimized = 2,
}

