namespace Windows.UI.Xaml.Hosting;

/// <summary>
/// Defines values that represent the reasons that the Windows.UI.Xaml.UIElement got focus
/// in a desktop application that uses a DesktopWindowXamlSource object to host XAML-based UI.
/// The XamlSourceFocusNavigationRequest.Reason property returns one of these values.
/// </summary>
public enum XamlSourceFocusNavigationReason
{
	/// <summary>
	/// The focus was set programmatically.
	/// </summary>
	Programmatic = 0,

	/// <summary>
	/// The focus was restored after a task switch, such as pressing Alt + Tab.
	/// </summary>
	Restore = 1,

	/// <summary>
	/// The focus was set in response to the user navigating to the next element
	/// by using a bidirectional navigation experience (for example, by pressing Tab).
	/// </summary>
	First = 3,

	/// <summary>
	/// The focus was set in response to the user navigating to the previous element
	/// by using a bidirectional navigation experience (for example, by pressing Shift-Tab).
	/// </summary>
	Last = 4,

	/// <summary>
	/// The focus was set in response to the user navigating left by using
	/// a 4-direction navigation experience (for example, by using keyboard arrow keys).
	/// </summary>
	Left = 7,

	/// <summary>
	/// The focus was set in response to the user navigating up by using
	/// a 4-direction navigation experience (for example, by using keyboard arrow keys).
	/// </summary>
	Up = 8,

	/// <summary>
	/// The focus was set in response to the user navigating right by using
	/// a 4-direction navigation experience (for example, by using keyboard arrow keys).
	/// </summary>
	Right = 9,

	/// <summary>
	/// The focus was set in response to the user navigating down by using
	/// a 4-direction navigation experience (for example, by using keyboard arrow keys).
	/// </summary>
	Down = 10,
}
