namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Defines the automation landmark types for elements.
/// </summary>
public enum AutomationLandmarkType
{
	/// <summary>
	/// No landmark type specified
	/// </summary>
	None,

	/// <summary>
	/// Custom landmark type
	/// </summary>
	Custom,

	/// <summary>
	/// Form landmark type
	/// </summary>
	Form,

	/// <summary>
	/// Main page landmark type
	/// </summary>
	Main,

	/// <summary>
	/// Navigation landmark type
	/// </summary>
	Navigation,

	/// <summary>
	/// Search landmark type
	/// </summary>
	Search,
}
