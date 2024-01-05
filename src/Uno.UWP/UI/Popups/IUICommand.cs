namespace Windows.UI.Popups;

/// <summary>
/// Represents a command in a context menu or message dialog box.
/// </summary>
public partial interface IUICommand
{
	/// <summary>
	/// Gets or sets the identifier of the command.
	/// </summary>
	object? Id { get; set; }

	/// <summary>
	/// Gets or sets the handler for the event that is fired when the user invokes the command. 
	/// </summary>
	UICommandInvokedHandler? Invoked { get; set; }

	/// <summary>
	/// Gets or sets the label for the command.
	/// </summary>
	string Label { get; set; }
}
