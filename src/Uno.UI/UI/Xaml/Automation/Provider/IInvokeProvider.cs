namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a method to support Microsoft UI Automation access to controls that 
/// initiate or perform a single, unambiguous action and do not maintain state 
/// when activated. Implement this interface in order to support the capabilities
/// that an automation client requests with a GetPattern call and 
/// PatternInterface.Invoke.
/// </summary>
public partial interface IInvokeProvider
{
	/// <summary>
	/// Sends a request to initiate or perform the single, unambiguous action of the
	/// provider control. For example, the invoke action for a Button is click.
	/// </summary>
	void Invoke();
}
