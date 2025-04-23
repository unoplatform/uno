namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Defines constants that customize the appearance 
/// of the Enter key on the virtual keyboard.
/// </summary>
public enum InputReturnType
{
	/// <summary>
	/// Default value.
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
	/// <remarks>Not supported on iOS.</remarks>
	Previous,

	/// <summary>
	/// Typically taking the user to the results of searching for the text they have typed.
	/// </summary>
	Search,

	/// <summary>
	/// Typically delivering the text to its target.
	/// </summary>
	Send,

	/// <summary>
	/// Typically used to indicate a "Continue" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	Continue,

	/// <summary>
	/// Typically used to indicate a "Join" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	Join,

	/// <summary>
	/// Typically used to indicate a "Route" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	Route,

	/// <summary>
	/// Typically used to indicate a "Google" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	Google,

	/// <summary>
	/// Typically used to indicate a "Yahoo" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	Yahoo,

	/// <summary>
	/// Typically used to indicate a "Emergency Call" action.
	/// </summary>
	/// <remarks>Only supported on Apple targets.</remarks>
	EmergencyCall
}
