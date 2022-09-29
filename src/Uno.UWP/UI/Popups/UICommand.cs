using System;

namespace Windows.UI.Popups;

/// <summary>
/// Represents a command in a context menu.
/// </summary>
public sealed partial class UICommand : IUICommand
{
	private string _label = "";

	/// <summary>
	/// Creates a new instance of the UICommand class.
	/// </summary>
	public UICommand()
		: this("", null, null)
	{
	}

	/// <summary>
	/// Creates a new instance of the UICommand class using the specified label.
	/// </summary>
	/// <param name="label">The label for the UICommand.</param>
	public UICommand(string label)
		: this(label, null, null)
	{
	}

	/// <summary>
	///  Creates a new instance of the UICommand class using the specified label and optional event handler.
	/// </summary>
	/// <param name="label">The label for the UICommand.</param>
	/// <param name="action">The event handler for the new command.</param>
	public UICommand(string label, UICommandInvokedHandler? action)
		: this(label, action, null)
	{
	}

	/// <summary>
	/// Creates a new instance of the UICommand class using the specified label, and optional event handler and command identifier.
	/// </summary>
	/// <param name="label">The label for the UICommand.</param>
	/// <param name="action">The event handler for the new command.</param>
	/// <param name="commandId">The command identifier for the new command.</param>
	public UICommand(string label, UICommandInvokedHandler? action, object? commandId)
	{
		Label = label ?? throw new ArgumentNullException(nameof(label));

		// These can be null
		Invoked = action;
		Id = commandId;
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
	public string Label
	{
		get => _label;
		set => _label = value ?? throw new ArgumentNullException(nameof(value));
	}
}
