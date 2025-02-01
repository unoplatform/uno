namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the InputFocusController.GotFocus and InputFocusController.LostFocus events.
/// </summary>
public partial class FocusChangedEventArgs
{
	internal FocusChangedEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets whether the InputFocusController.GotFocus and InputFocusController.LostFocus events were handled.
	/// </summary>
	public bool Handled { get; set; }
}
