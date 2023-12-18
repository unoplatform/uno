namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the AutoSuggestBox.QuerySubmitted event.
/// </summary>
public partial class AutoSuggestBoxQuerySubmittedEventArgs : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxQuerySubmittedEventArgs class.
	/// </summary>
	public AutoSuggestBoxQuerySubmittedEventArgs() : base()
	{
	}

	internal AutoSuggestBoxQuerySubmittedEventArgs(object chosenSuggestion, string queryText)
	{
		ChosenSuggestion = chosenSuggestion;
		QueryText = queryText;
	}

	/// <summary>
	/// Gets the suggested result that the user chose.
	/// </summary>
	public object ChosenSuggestion { get; }

	/// <summary>
	/// Gets the query text of the current search.
	/// </summary>
	public string QueryText { get; }
}
