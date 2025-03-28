namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify the ToggleState of a UI Automation element.
/// </summary>
public enum ToggleState
{
	/// <summary>
	/// The UI Automation element isn't selected, checked, marked, or otherwise activated.
	/// </summary>
	Off,

	/// <summary>
	/// The UI Automation element is selected, checked, marked, or otherwise activated.
	/// </summary>
	On,

	/// <summary>
	/// The UI Automation element is in an indeterminate state.
	/// </summary>
	Indeterminate,
}
