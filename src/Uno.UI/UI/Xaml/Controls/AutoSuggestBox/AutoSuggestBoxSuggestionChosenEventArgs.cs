namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the SuggestionChosen event.
/// </summary>
public partial class AutoSuggestBoxSuggestionChosenEventArgs : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxSuggestionChosenEventArgs class.
	/// </summary>
	public AutoSuggestBoxSuggestionChosenEventArgs() : base()
	{
	}

	internal AutoSuggestBoxSuggestionChosenEventArgs(object selectedItem)
	{
		SelectedItem = selectedItem;
	}

	/// <summary>
	/// Gets a reference to the selected item.
	/// </summary>
	public object SelectedItem { get; }

}
