namespace Windows.UI.Popups;

public sealed partial class UICommandSeparator : IUICommand
{
	/// <summary>
	/// Creates a new instance of the UICommand class.
	/// </summary>
	public UICommandSeparator()
	{
	}

	/// <summary>
	/// Gets or sets the identifier of the command.
	/// </summary>
	public object? Id { get; set; }

	/// <summary>
	/// Gets or sets the handler for the event that is fired when the user invokes the command. 
	/// </summary>
	public UICommandInvokedHandler? Invoked { get; set; }

	/// <summary>
	/// Gets or sets the label for the command.
	/// </summary>
	public string Label { get; set; } = null!; // TODO: Revise nullability of Label.
}
