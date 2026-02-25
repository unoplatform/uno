namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Specifies the position of the caret in a line of text.
/// </summary>
public enum AutomationCaretPosition
{
	/// <summary>
	/// The caret position is unknown.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// The caret is at the end of the line.
	/// </summary>
	EndOfLine = 1,

	/// <summary>
	/// The caret is at the beginning of the line.
	/// </summary>
	BeginningOfLine = 2,
}
