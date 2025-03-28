namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Enables Microsoft UI Automation client applications to direct the mouse or keyboard input to a specific UI element.
/// </summary>
public partial interface ISynchronizedInputProvider
{
	/// <summary>
	/// Cancels listening for input.
	/// </summary>
	void Cancel();

	/// <summary>
	/// Starts listening for input of the specified type.
	/// </summary>
	/// <param name="inputType">The type of input that is requested to be synchronized.</param>
	void StartListening(SynchronizedInputType inputType);
}
