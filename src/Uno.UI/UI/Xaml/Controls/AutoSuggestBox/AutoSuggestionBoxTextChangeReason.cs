namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Values used to indicate the reason for the text changing in the AutoSuggestBox.
/// </summary>
public enum AutoSuggestionBoxTextChangeReason
{
	/// <summary>
	/// The user edited the text.
	/// </summary>
	UserInput,

	/// <summary>
	/// The text was changed via code.
	/// </summary>
	ProgrammaticChange,

	/// <summary>
	/// The user selected one of the items in the auto-suggestion box.
	/// </summary>
	SuggestionChosen,
}
