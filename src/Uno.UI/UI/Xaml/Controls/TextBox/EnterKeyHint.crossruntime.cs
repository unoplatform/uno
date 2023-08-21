namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Defines constants that customize the appearance 
/// of the Enter key on the virtual keyboard.
/// </summary>
public enum EnterKeyHint
{
	/// <summary>
	/// Default value, matches "" (empty string) in HTML.
	/// </summary>
	Default,

	/// <summary>
	/// Typically indicating inserting a new line
	/// </summary>
	Enter,

	/// <summary>
	/// There is nothing more to input and the input 
	/// method editor (IME) will be closed.
	/// </summary>
	Done,

	/// <summary>
	/// Typically meaning to take the user to the target 
	/// of the text they typed.
	/// </summary>
	Go,

	/// <summary>
	/// Typically taking the user to the next field that will accept text.
	/// </summary>
	Next,

	/// <summary>
	/// Typically taking the user to the previous field that will accept text.
	/// </summary>
	Previous,

	/// <summary>
	/// Typically taking the user to the results of searching for the text they have typed.
	/// </summary>
	Search,

	/// <summary>
	/// Typically delivering the text to its target.
	/// </summary>
	Send,
}
