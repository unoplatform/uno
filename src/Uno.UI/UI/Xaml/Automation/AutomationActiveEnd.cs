namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Specifies the active end of a text selection.
/// </summary>
public enum AutomationActiveEnd
{
	/// <summary>
	/// No active end is specified.
	/// </summary>
	None = 0,

	/// <summary>
	/// The start of the text range is the active end.
	/// </summary>
	Start = 1,

	/// <summary>
	/// The end of the text range is the active end.
	/// </summary>
	End = 2,
}
