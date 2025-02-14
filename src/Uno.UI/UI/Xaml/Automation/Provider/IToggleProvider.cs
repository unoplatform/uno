namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support Microsoft UI Automation
/// client access to controls that can cycle through a set of states and
/// maintain a particular state. Implement this interface in order to 
/// support the capabilities that an automation client requests with a
/// GetPattern call and PatternInterface.Toggle.
/// </summary>
public partial interface IToggleProvider
{
	/// <summary>
	/// Gets the toggle state of the control.
	/// </summary>
	ToggleState ToggleState { get; }

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	void Toggle();
}
