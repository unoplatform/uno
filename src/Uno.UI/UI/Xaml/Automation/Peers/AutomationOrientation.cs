namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Specifies the orientation direction in which a control can be presented. Values are used by GetOrientation.
/// </summary>
public enum AutomationOrientation
{
	/// <summary>
	/// The control does not have an orientation.
	/// </summary>
	None,

	/// <summary>
	/// The control is presented horizontally.
	/// </summary>
	Horizontal,

	/// <summary>
	/// The control is presented vertically.
	/// </summary>
	Vertical,
}
