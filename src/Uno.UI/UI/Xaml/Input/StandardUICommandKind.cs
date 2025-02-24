namespace Windows.UI.Xaml.Input;

/// <summary>
/// Specifies the set of platform commands (with pre-defined properties such as icon,
/// keyboard accelerator, and description) that can be used with a StandardUICommand.
/// </summary>
public enum StandardUICommandKind
{
	/// <summary>
	/// No command. Default.
	/// </summary>
	None,

	/// <summary>
	/// Specifies the cut command.
	/// </summary>
	Cut,

	/// <summary>
	/// Specifies the copy command.
	/// </summary>
	Copy,

	/// <summary>
	/// Specifies the paste command.
	/// </summary>
	Paste,

	/// <summary>
	/// Specifies the select all command.
	/// </summary>
	SelectAll,

	/// <summary>
	/// Specifies the delete command.
	/// </summary>
	Delete,

	/// <summary>
	/// Specifies the share command.
	/// </summary>
	Share,

	/// <summary>
	/// Specifies the save command.
	/// </summary>
	Save,

	/// <summary>
	/// Specifies the open command.
	/// </summary>
	Open,

	/// <summary>
	/// Specifies the close command.
	/// </summary>
	Close,

	/// <summary>
	/// Specifies the pause command.
	/// </summary>
	Pause,

	/// <summary>
	/// Specifies the play command.
	/// </summary>
	Play,

	/// <summary>
	/// Specifies the stop command.
	/// </summary>
	Stop,

	/// <summary>
	/// Specifies the forward command.
	/// </summary>
	Forward,

	/// <summary>
	/// Specifies the backward command.
	/// </summary>
	Backward,

	/// <summary>
	/// Specifies the undo command.
	/// </summary>
	Undo,

	/// <summary>
	/// Specifies the redo command.
	/// </summary>
	Redo,
}
